using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Services;
using System;
using System.Data;
using AdaptiveCards;
using TimeRecorderBACKEND.Models;
using System.Collections.Generic; // Added for List<Attachment>
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using System.Linq; // Added for OrderByDescending
using System.Collections; // Added for IDictionary

public class TeamsBot : TeamsActivityHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly string timeRegisterChannelId;

    public TeamsBot(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        timeRegisterChannelId = _configuration["Teams:ChannelId"] ?? throw new InvalidOperationException("Teams:ChannelId is missing in configuration.");
    }

    protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
    {
        Console.WriteLine($" Recive msg: Text='{turnContext.Activity.Text}', Type={turnContext.Activity.Conversation.ConversationType}, Channel={turnContext.Activity.ChannelId}, From={turnContext.Activity.From?.Name}, AadObjectId={turnContext.Activity.From?.AadObjectId}");

        string userMessage = turnContext.Activity.Text?.Trim().ToLower();
        var channelData = turnContext.Activity.GetChannelData<Microsoft.Bot.Schema.Teams.TeamsChannelData>();
        string? actualChannelId = channelData?.Channel?.Id;

        if (actualChannelId != timeRegisterChannelId)
        {
            Console.WriteLine($"Msg from diffrent channel {actualChannelId}");
            return;
        }
        if (turnContext.Activity.Value is not null)
        {
            string confirmation = null;
            string confirmedAction = null;
            string confirmedTime = null; 

            if (turnContext.Activity.Value is IDictionary<string, object> dict)
            {
                confirmation = dict.TryGetValue("confirmation", out var confirmationObj) ? confirmationObj?.ToString() : null;
                confirmedAction = dict.TryGetValue("action", out var actionObj) ? actionObj?.ToString() : null;
                confirmedTime = dict.TryGetValue("time", out var timeObj) ? timeObj?.ToString() : null; 
            }
            else
            {
                dynamic value = turnContext.Activity.Value;
                confirmation = value.confirmation;
                confirmedAction = value.action;
                confirmedTime = value.time; 
            }

            string infoText;
            if (confirmation == "yes")
            {
                string apiResult = await CallApiBasedOnAction(confirmedAction, turnContext, cancellationToken, confirmedTime); 
                infoText = $"✅ {apiResult}";
            }
            else
            {
                infoText = "❌ Operation cancelled.";
            }

            AdaptiveCard infoCard = new AdaptiveCard("1.4")
            {
                Body = {
                    new AdaptiveTextBlock(infoText)
                    {
                        Weight = AdaptiveTextWeight.Bolder,
                        Size = AdaptiveTextSize.Medium,
                        Color = AdaptiveTextColor.Good
                    }
                }
            };

            Attachment infoAttachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = infoCard
            };

            Activity updateActivity = new Activity
            {
                Id = turnContext.Activity.ReplyToId,
                Type = ActivityTypes.Message,
                Attachments = new List<Attachment> { infoAttachment },
                Conversation = turnContext.Activity.Conversation
            };

            await turnContext.UpdateActivityAsync(updateActivity, cancellationToken);
            return;
        }

        (string action, DateTime? extractedTime) aiResponse = await InterpretMessageWithAI(userMessage);
        string action = aiResponse.action;
        DateTime? extractedTime = aiResponse.extractedTime;

        if (action == "summary")
        {
            string? userAadId = turnContext.Activity.From?.AadObjectId;
            if (string.IsNullOrEmpty(userAadId))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I can't identify user."), cancellationToken);
                return;
            }

            Guid userId = Guid.Parse(userAadId);
            ISummaryService? summaryService = (ISummaryService)_serviceProvider.GetService(typeof(ISummaryService));
            DateTime today = DateTime.Today;
            SummaryDto summary = await summaryService.GetFullSummaryAsync(today, today, userId);

            int workMinutes = summary.TotalWorkTimeMinutes;
            int workHours = workMinutes / 60;
            int workRestMinutes = workMinutes % 60;

            string workTimeText = workHours > 0
                ? $"{workHours} h {workRestMinutes} min"
                : $"{workRestMinutes} min";

            string summaryText = $"Day summary:\n" +
                $"- Work time: {workTimeText}\n" +
                $"- Breaks count: {summary.BreakCount}\n" +
                $"- Breaks time: {summary.TotalBreakTimeMinutes} min\n";


            await turnContext.SendActivityAsync(MessageFactory.Text(summaryText), cancellationToken);
            return;
        }
        else if (action == "hint")
        {
            string hintText =
                "Allowed commands:\n" +
                "- start: starts/resume work\n" +
                "- break: starts break\n" +
                "- end: end work\n" +
                "- summary: day summary\n" +
                "- hint: command list\n" +
                "- You can also specify past times, e.g., 'start 2 hours ago', 'break at 9 AM'"; 

            await turnContext.SendActivityAsync(MessageFactory.Text(hintText), cancellationToken);
            return;
        }

        string confirmationMessage;
        object actionData;

        if (action == "past_start" || action == "past_break")
        {
            if (!extractedTime.HasValue)
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("I understand you want to log past time, but I couldn't determine the exact time. Please be more specific (e.g., 'start 2 hours ago' or 'break at 10:30')."), cancellationToken);
                return;
            }
            confirmationMessage = $"Are you sure you want to {action.Replace("past_", "")} work at {extractedTime.Value.ToShortTimeString()}?";
            actionData = new { confirmation = "yes", action = action, time = extractedTime.Value.ToString("dd.MM.yyyy HH:mm:ss") };
        }
        else if (action == "start" || action == "break" || action == "end")
        {
            confirmationMessage = $"Are you sure you want to: {action}?";
            actionData = new { confirmation = "yes", action = action };
        }
        else
        {
            await turnContext.SendActivityAsync(MessageFactory.Text("I'm not sure what you mean. Please use 'start', 'break', 'end', 'summary', or 'hint'. You can also specify past times."), cancellationToken);
            return;
        }


        AdaptiveCard card = new AdaptiveCard("1.4")
        {
            Body = {
                new AdaptiveTextBlock(confirmationMessage)
                {
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium
                }
            },
            Actions = {
                new AdaptiveSubmitAction
                {
                    Title = "Yes",
                    Data = actionData
                },
                new AdaptiveSubmitAction
                {
                    Title = "No",
                    Data = new { confirmation = "no", action = action, time = extractedTime?.ToString("dd.MM.yyyy HH:mm:ss") }
                }
            }
        };

        Attachment attachment = new Attachment
        {
            ContentType = AdaptiveCard.ContentType,
            Content = card
        };

        IMessageActivity reply = MessageFactory.Attachment(attachment);
        await turnContext.SendActivityAsync(reply, cancellationToken);
    }

    private async Task<(string action, DateTime? extractedTime)> InterpretMessageWithAI(string message)
    {
        HttpClient httpClient = _httpClientFactory.CreateClient("AiClient");
        string? endpoint = _configuration["ai:endpoint"];
        string? apiKey = _configuration["ai:apiKey"];
        Console.BackgroundColor = ConsoleColor.DarkGreen;
        Console.WriteLine($"AI Endpoint: {endpoint}");
        Console.ResetColor();
        string prompt = $@"
Interpret the user's intent as one of the actions: 'start', 'break', 'end', 'summary', 'hint', 'past_start', 'past_break'.
If the intent is 'past_start' or 'past_break', always extract the time from the user's message.
- If the user specifies a number of minutes or hours ago (e.g. '8 minutes ago', '2 hours ago', '19 minut temu', '7 godzin temu'), subtract that value from the current time.
- If the user specifies an exact time (e.g. 'at 10:15', 'o 10:15', 'o godzinie 13:45', 'at 8:00', 'break at 14:15', 'przerwa o 14:15', 'zacząłem o 7:00', 'I started at 7:00', 'at 7:00 am', 'at 5:00 pm'), use today's date with that time in Warsaw local time.
- If the user says 'now', 'teraz', 'w tej chwili', 'today', use the current time.
Never use ISO 8601 format or the 'Z' suffix. Always return the full date and time in the format 'dd.MM.yyyy HH:mm:ss'.
If the time cannot be extracted, return only the action and set 'time' to null.

Return the response in JSON format with fields 'action' and 'time'. If time is not available, 'time' should be null.

Examples:
- 'zacząłem o 13:45' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(13).AddMinutes(45):dd.MM.yyyy HH:mm:ss}"" }}
- 'zacząłem o godzinie 8:00' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(8):dd.MM.yyyy HH:mm:ss}"" }}
- 'I started at 10:15' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(10).AddMinutes(15):dd.MM.yyyy HH:mm:ss}"" }}
- 'I started at 7:00 am' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(7):dd.MM.yyyy HH:mm:ss}"" }}
- 'I started at 5:00 pm' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(17):dd.MM.yyyy HH:mm:ss}"" }}
- 'break at 14:15' => {{ ""action"": ""past_break"", ""time"": ""{DateTime.Today.AddHours(14).AddMinutes(15):dd.MM.yyyy HH:mm:ss}"" }}
- 'przerwa o 14:15' => {{ ""action"": ""past_break"", ""time"": ""{DateTime.Today.AddHours(14).AddMinutes(15):dd.MM.yyyy HH:mm:ss}"" }}
- 'zacząłem o 7:00' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Today.AddHours(7):dd.MM.yyyy HH:mm:ss}"" }}
- 'I started 8 minutes ago' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Now.AddMinutes(-8):dd.MM.yyyy HH:mm:ss}"" }}
- 'zacząłem 19 minut temu' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Now.AddMinutes(-19):dd.MM.yyyy HH:mm:ss}"" }}
- 'I started 2 hours ago' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Now.AddHours(-2):dd.MM.yyyy HH:mm:ss}"" }}
- 'break 5 minutes ago' => {{ ""action"": ""past_break"", ""time"": ""{DateTime.Now.AddMinutes(-5):dd.MM.yyyy HH:mm:ss}"" }}
- 'przerwa 15 minut temu' => {{ ""action"": ""past_break"", ""time"": ""{DateTime.Now.AddMinutes(-15):dd.MM.yyyy HH:mm:ss}"" }}
- 'zacząłem teraz' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Now:dd.MM.yyyy HH:mm:ss}"" }}
- 'I started now' => {{ ""action"": ""past_start"", ""time"": ""{DateTime.Now:dd.MM.yyyy HH:mm:ss}"" }}
- 'break now' => {{ ""action"": ""past_break"", ""time"": ""{DateTime.Now:dd.MM.yyyy HH:mm:ss}"" }}
- 'I am going back from break' => {{ ""action"": ""start"", ""time"": null }}
- 'I am going to start' => {{ ""action"": ""start"", ""time"": null }}
- 'I am going on break' => {{ ""action"": ""break"", ""time"": null }}
- 'zacznij pracę' => {{ ""action"": ""start"", ""time"": null }}
- 'rozpocznij przerwę' => {{ ""action"": ""break"", ""time"": null }}
- '{message}' =>";

        var requestBody = new
        {
            messages = new[]
            {
                new { role = "system", content = "Jesteś asystentem do rozpoznawania poleceń do rejestracji czasu pracy. Odpowiadaj tylko w formacie JSON z polami 'action' i 'time'. 'action' to jedno słowo: start, break, end, summary, hint, past_start, past_break. 'time' to data i czas w formacie lokalnym 'yyyy-MM-dd HH:mm:ss' (czas Warszawa) lub null." },                
                new { role = "user", content = prompt }
            },
            max_tokens = 256, 
            temperature = 0.20
        };

        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"AI API call failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
            return ("unknown", null);
        }

        string json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"AI Raw Response: {json}"); 
            
        try
        {
            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            if (!root.TryGetProperty("choices", out JsonElement choicesElement) || choicesElement.GetArrayLength() == 0)
            {
                return ("unknown", null);
            }

            JsonElement messageElement = choicesElement[0].GetProperty("message");
            string? content = messageElement.GetProperty("content").GetString();

            if (string.IsNullOrEmpty(content))
            {
                return ("unknown", null);
            }

            using JsonDocument contentDoc = JsonDocument.Parse(content);
            string? aiAction = contentDoc.RootElement.GetProperty("action").GetString()?.Trim().ToLower();
            string? aiTime = contentDoc.RootElement.TryGetProperty("time", out JsonElement timeElement) ? timeElement.GetString() : null;

            DateTime? parsedTime = null;
            if (!string.IsNullOrEmpty(aiTime) && DateTime.TryParse(aiTime, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime tempTime))
            {
                parsedTime = DateTime.SpecifyKind(tempTime, DateTimeKind.Local);
            }

            if (aiAction != "start" && aiAction != "break" && aiAction != "end" && aiAction != "summary" && aiAction != "hint" && aiAction != "past_start" && aiAction != "past_break")
            {
                aiAction = "unknown";
            }

            return (aiAction ?? "unknown", parsedTime);
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Error parsing AI response JSON: {ex.Message} - Raw JSON: {json}");
            return ("unknown", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error in InterpretMessageWithAI: {ex.Message}");
            return ("unknown", null);
        }
    }


    private async Task<string> CallApiBasedOnAction(string action, ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken, string? timeString = null)
    {
        string? userAadId = turnContext.Activity.From?.AadObjectId;
        if (string.IsNullOrEmpty(userAadId))
        {
            return "I can't identify user.";
        }

        IWorkLogService? workLogService = (IWorkLogService)_serviceProvider.GetService(typeof(IWorkLogService));
        Guid userId = Guid.Parse(userAadId);
        IUserService userService = (IUserService)_serviceProvider.GetService(typeof(IUserService));
        UserDto? user = await userService.GetByIdAsync(userId);

        DateTime? parsedTime = null;
        if (!string.IsNullOrEmpty(timeString))
        {
            if (DateTime.TryParseExact(timeString, "dd.MM.yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime dbTime))
            {
                parsedTime = DateTime.SpecifyKind(dbTime, DateTimeKind.Local);
            }
            else if (DateTime.TryParseExact(timeString, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime localTime))
            {
                parsedTime = DateTime.SpecifyKind(localTime, DateTimeKind.Local);
            }
            else if (DateTime.TryParseExact(timeString, "MM/dd/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out DateTime usTime))
            {
                parsedTime = DateTime.SpecifyKind(usTime, DateTimeKind.Local);
            }
            else if (DateTime.TryParse(timeString, out DateTime fallbackTime))
            {
                parsedTime = DateTime.SpecifyKind(fallbackTime, DateTimeKind.Local);
            }
        }

        switch (action)
        {
            case "past_start":
                if (!parsedTime.HasValue) return $"Failed to log past work: no valid time provided. Otrzymany string: {timeString}";
                try
                {
                    WorkLogDto result = await workLogService.CreatePastWorkLogAsync(userId, WorkLogType.Work, parsedTime.Value);
                    return $"User {user.Name} {user.Surname} successfully logged work starting at {parsedTime.Value:HH:mm}.";
                }
                catch (Exception ex)
                {
                    return $"Error logging past work: {ex.Message}";
                }

            case "start":
                try
                {
                    WorkLogDto result = await workLogService.StartWorkLogAsync(userId, WorkLogType.Work);
                    return $"User {user.Name} {user.Surname} started work at {DateTime.Now:HH:mm}.";
                }
                catch (Exception ex)
                {
                    return $"Error during starting work: {ex.Message}";
                }

            case "break":
                try
                {
                    WorkLogDto result = await workLogService.StartWorkLogAsync(userId, WorkLogType.Break);

                    ISettingsService? settingsService = (ISettingsService)_serviceProvider.GetService(typeof(ISettingsService));

                    Settings? settings = await settingsService.GetSettingsAsync();
                    int maxBreakMinutes = settings?.MaxBreakTime ?? 30;

                    int usedBreakMinutes = await workLogService.GetUsedBreakMinutesTodayAsync(userId);
                    int remainingBreakMinutes = Math.Max(0, maxBreakMinutes - usedBreakMinutes);

                    return $"User {user.Name} {user.Surname} started break at {DateTime.Now:HH:mm}. You have {remainingBreakMinutes} minutes left today!";
                }
                catch (Exception ex)
                {
                    return $"Error during starting break: {ex.Message}";
                }

            case "past_break":
                if (!parsedTime.HasValue) return "Failed to log past break: no valid time provided.";
                try
                {
                    WorkLogDto result = await workLogService.CreatePastWorkLogAsync(userId, WorkLogType.Break, parsedTime.Value);
                    return $"User {user.Name} {user.Surname} successfully logged break starting at {parsedTime.Value:HH:mm}.";
                }
                catch (Exception ex)
                {
                    return $"Error logging past break: {ex.Message}";
                }

            case "end":
                try
                {
                    IEnumerable<WorkLogDtoWithUserNameAndSurname> workLogs = await workLogService.GetSpecific(userId, null, false);
                    WorkLogDtoWithUserNameAndSurname? lastWorkLog = workLogs.OrderByDescending(w => w.StartTime).FirstOrDefault();
                    if (lastWorkLog == null)
                        return "There is no active worklog to end.";

                    WorkLogDto? result = await workLogService.EndWorkLogAsync(lastWorkLog.Id);
                    return $"User {user.Name} {user.Surname} ended their work.";
                }
                catch (Exception ex)
                {
                    return $"Error during ending work: {ex.Message}";
                }

            default:
                return "I don't understand. Type start work, start break";
        }
    }
}