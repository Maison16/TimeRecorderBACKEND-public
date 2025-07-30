using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Services;
using Newtonsoft.Json;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Models;
using Microsoft.AspNetCore.SignalR;

public class TeamsProactiveThreadService : BackgroundService
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TeamsProactiveThreadService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _botAppId;
    private readonly IHubContext<WorkStatusHub> _hubContext;
    private bool isNewThreadSend = false; 
    public TeamsProactiveThreadService(
    IBotFrameworkHttpAdapter adapter,
    IConfiguration configuration,
    ILogger<TeamsProactiveThreadService> logger,
    IServiceProvider serviceProvider,
    IHubContext<WorkStatusHub> hubContext)
    {
        _adapter = adapter;
        _configuration = configuration;
        _botAppId = _configuration["Teams:BotAppId"];
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            DateTime now = DateTime.Now;
            using IServiceScope scope = _serviceProvider.CreateScope();
            WorkTimeDbContext context = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();
            IWorkLogService workLogService = scope.ServiceProvider.GetRequiredService<IWorkLogService>();
            ITeamsService teamsService = scope.ServiceProvider.GetRequiredService<ITeamsService>();
            IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            Settings? settings = await context.Settings.FirstOrDefaultAsync(stoppingToken);
            ArgumentNullException.ThrowIfNull(settings, nameof(settings));
            LastWorkOnDayMassageDate? lastDateEntity = await context.LastWorkOnDayMassageDate.FirstOrDefaultAsync(stoppingToken);
            DateTime? lastThreadDate = lastDateEntity?.LastMessageDate;

            await NotifyUsersWithoutStartAsync(now, context, workLogService, teamsService, settings, stoppingToken);
            await NotifyUsersWithUnfinishedAsync(now, context, workLogService, teamsService, settings, stoppingToken);
            await NotifyUsersWithLongBreakAsync(now, context, workLogService, teamsService, settings, stoppingToken);
            await CreateTeamsThread(now, context, lastDateEntity, lastThreadDate, emailService, settings, stoppingToken);

            await context.SaveChangesAsync(stoppingToken);
        }
    }

    private async Task NotifyUsersWithoutStartAsync(DateTime now, WorkTimeDbContext context, IWorkLogService workLogService, ITeamsService teamsService, Settings settings, CancellationToken stoppingToken)
    {
        if (now.Hour < settings.LatestStartMoment)
        {
            return;
        }
        if(DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday)
        {
            return;
        }
        List<Guid> usersWithoutStart = await workLogService.GetUsersWithoutStartedWorkTodayAsync();
        foreach (Guid userId in usersWithoutStart)
        {
            bool alreadySent = await context.UserNotificationLogs
                .AnyAsync(x => x.UserId == userId && x.NotificationType == "not_started" && x.DateSent.Date == now.Date, stoppingToken);
            if (alreadySent)
                continue;

            await teamsService.SendPrivateMessageAsync(userId.ToString(), "You didn't start work today!");
            await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new { userId, status = "not_started" });
            context.UserNotificationLogs.Add(new UserNotificationLog
            {
                UserId = userId,
                NotificationType = "not_started",
                DateSent = now
            });
        }
    }

    private async Task NotifyUsersWithUnfinishedAsync(DateTime now, WorkTimeDbContext context, IWorkLogService workLogService, ITeamsService teamsService, Settings settings, CancellationToken stoppingToken)
    {
        List<Guid> usersWithUnfinished = await workLogService.GetUsersWithUnfinishedWorkTodayAsync(settings.MaxWorkHoursDuringOneDay);
        foreach (Guid userId in usersWithUnfinished)
        {
            bool alreadySent = await context.UserNotificationLogs
                .AnyAsync(x => x.UserId == userId && x.NotificationType == "unfinished" && x.DateSent.Date == now.Date, stoppingToken);
            if (alreadySent)
                continue;

            await teamsService.SendPrivateMessageAsync(userId.ToString(), "You didn't end work today!");
            await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new { userId, status = "unfinished" });
            context.UserNotificationLogs.Add(new UserNotificationLog
            {
                UserId = userId,
                NotificationType = "unfinished",
                DateSent = now
            });
        }
    }

    private async Task NotifyUsersWithLongBreakAsync(DateTime now, WorkTimeDbContext context, IWorkLogService workLogService, ITeamsService teamsService, Settings settings, CancellationToken stoppingToken)
    {
        List<Guid> usersWithLongBreaks = await workLogService.GetUsersWithLongActiveBreakAsync(settings.MaxBreakTime);
        foreach (Guid userId in usersWithLongBreaks)
        {
            bool alreadySent = await context.UserNotificationLogs
                .AnyAsync(x => x.UserId == userId && x.NotificationType == "long_break" && x.DateSent.Date == now.Date, stoppingToken);
            if (alreadySent)
                continue;

            await teamsService.SendPrivateMessageAsync(userId.ToString(), $"Your break is longer than {settings.MaxBreakTime} minutes!");
            await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new { userId, status = "long_break", maxBreakTime = settings.MaxBreakTime });
            context.UserNotificationLogs.Add(new UserNotificationLog
            {
                UserId = userId,
                NotificationType = "long_break",
                DateSent = now
            });
        }
    }

    private async Task CreateTeamsThread(DateTime now, WorkTimeDbContext context, LastWorkOnDayMassageDate? lastDateEntity, DateTime? lastThreadDate, IEmailService emailService, Settings settings, CancellationToken stoppingToken)
    {
        if (now.Hour < 18 || (lastThreadDate != null && lastThreadDate.Value.Date == now.Date))
            return;

        lastDateEntity.LastMessageDate = now.Date;
        context.LastWorkOnDayMassageDate.Update(lastDateEntity);
        string? teamId = _configuration["Teams:TeamId"];
        string? channelId = _configuration["Teams:ChannelId"];
        string serviceUrl = "https://smba.trafficmanager.net/emea/";

        ConversationReference conversationReference = new ConversationReference
        {
            ChannelId = channelId,
            ServiceUrl = serviceUrl,
            Conversation = new ConversationAccount
            {
                Id = channelId,
                IsGroup = true,
                ConversationType = "channel",
                TenantId = null
            },
            Bot = new ChannelAccount
            {
                Id = _botAppId
            }
        };

        await ((CloudAdapterBase)_adapter).ContinueConversationAsync(
            _botAppId,
            conversationReference,
            async (botContext, token) =>
            {
                string message = $"Work in day: {now.AddDays(1):dd.MM.yyyy}";
                Activity activity = MessageFactory.Text(message);

                activity.ChannelData = new
                {
                    channel = new { id = channelId },
                    team = new { id = teamId }
                };

                await botContext.SendActivityAsync(activity, token);
            },
            stoppingToken
        );

        if (!isNewThreadSend)
        {
            await _hubContext.Clients.All.SendAsync("WorkStatusChanged", new { status = "new_thread", date = now.Date });
            foreach (User user in context.Users)
            {
                emailService.SendAsync(
                   user.Email,
                   "Workday Summary",
                   $"Workday thread for {now.AddDays(1):dd.MM.yyyy} has been posted in Teams channel.");
            }
            isNewThreadSend = true;
        }
        if (lastDateEntity == null)
        {
            context.LastWorkOnDayMassageDate.Add(new LastWorkOnDayMassageDate { LastMessageDate = now.Date });
        }
        isNewThreadSend = false;
    }
}