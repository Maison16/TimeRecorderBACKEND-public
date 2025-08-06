using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Services;
using ZiggyCreatures.Caching.Fusion;

public class TeamsPresenceBackgroundService : BackgroundService
{
    private readonly GraphServiceClient _graphClient;
    private readonly IFusionCache _cache;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<WorkStatusHub> _hubContext;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);

    public TeamsPresenceBackgroundService(GraphServiceClient graphClient, IFusionCache cache, IServiceProvider serviceProvider, IHubContext<WorkStatusHub> hubContext)
    {
        _graphClient = graphClient;
        _cache = cache;
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Microsoft.Graph.Models.UserCollectionResponse? users = await _graphClient.Users.GetAsync();
                List<Microsoft.Graph.Models.User>? userList = users.Value;

                Dictionary<string, string> presenceDict = new Dictionary<string, string>();

                foreach (Microsoft.Graph.Models.User user in userList)
                {
                    try
                    {
                        Microsoft.Graph.Models.Presence? presence = await _graphClient.Users[user.Id].Presence.GetAsync(cancellationToken: stoppingToken);
                        presenceDict[user.Id] = presence.Availability;
                    }
                    catch(Exception ex) 
                    {
                        Console.WriteLine($"[TeamsPresenceBackgroundService] UserId: {user.Id}, Exception: {ex}");
                    }
                }

                await _cache.SetAsync("TeamsPresence_All", presenceDict, TimeSpan.FromMinutes(2), token: stoppingToken);

                Dictionary<string, string> prevPresenceDict = await _cache.GetOrDefaultAsync<Dictionary<string, string>>("TeamsPresence_Prev") ?? new Dictionary<string, string>();
                HashSet<string> notifiedUserIds = await _cache.GetOrDefaultAsync<HashSet<string>>("TeamsPresence_Notified") ?? new HashSet<string>();

                foreach (KeyValuePair<string, string> kv in presenceDict)
                {
                    string userId = kv.Key;
                    string currentStatus = kv.Value;
                    prevPresenceDict.TryGetValue(userId, out string prevStatus);

                    if ((string.Equals(prevStatus, "Away", StringComparison.OrdinalIgnoreCase) || string.Equals(prevStatus, "BeRightBack", StringComparison.OrdinalIgnoreCase))
                        && !(string.Equals(currentStatus, "Away", StringComparison.OrdinalIgnoreCase) || string.Equals(currentStatus, "BeRightBack", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (notifiedUserIds.Remove(userId))
                        {
                            Console.WriteLine($"[TeamsPresenceBackgroundService] User {userId} is active again, notification state reset.");
                        }
                    }
                }
                await _cache.SetAsync("TeamsPresence_Notified", notifiedUserIds, TimeSpan.FromMinutes(5));
                await _cache.SetAsync("TeamsPresence_Prev", presenceDict, TimeSpan.FromMinutes(5));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamsPresenceBackgroundService] Error: {ex.Message}");
            }

            List<string> usersBRB = await GetBeRightBackOrAwayUserIdsAsync();

            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                ITeamsService teamsService = scope.ServiceProvider.GetRequiredService<ITeamsService>();
                IWorkLogService workLogService = scope.ServiceProvider.GetRequiredService<IWorkLogService>();

                HashSet<string> notifiedUserIds = await _cache.GetOrDefaultAsync<HashSet<string>>("TeamsPresence_Notified") ?? new HashSet<string>();
                HashSet<string> newlyNotified = new HashSet<string>();
                List<string> toStartBreak = new List<string>();

                foreach (string userId in usersBRB)
                {
                    if (Guid.TryParse(userId, out Guid userGuid))
                    {
                        IEnumerable<WorkLogDtoWithUserNameAndSurname> activeWorkLogs = await workLogService.GetSpecific(userGuid, TimeRecorderBACKEND.Enums.WorkLogType.Work, false);
                        if (activeWorkLogs.Any())
                        {
                            if (notifiedUserIds.Contains(userId))
                            {
                                toStartBreak.Add(userId);
                            }
                            else
                            {
                                await teamsService.SendPrivateMessageAsync(userId, "Are you still here?");
                                newlyNotified.Add(userId);
                                notifiedUserIds.Add(userId);
                                await _hubContext.Clients.User(userId).SendAsync("WorkStatusChanged", new { userId, status = "still_here" });
                            }
                        }
                    }
                }

                foreach (string userId in toStartBreak)
                {
                    if (Guid.TryParse(userId, out Guid userGuid))
                    {
                        try
                        {
                            await workLogService.StartWorkLogAsync(userGuid, TimeRecorderBACKEND.Enums.WorkLogType.Break);
                            await teamsService.SendPrivateMessageAsync(userId, "Your break has been started automatically due to your absence.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TeamsPresenceBackgroundService] Error starting break for {userId}: {ex.Message}");
                        }
                    }
                }

                HashSet<string> stillNotified = new HashSet<string>(usersBRB);
                await _cache.SetAsync("TeamsPresence_Notified", stillNotified, TimeSpan.FromMinutes(10));
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    public async Task<List<string>> GetBeRightBackOrAwayUserIdsAsync()
    {
        Dictionary<string, string>? presenceDict = await _cache.GetOrDefaultAsync<Dictionary<string, string>>("TeamsPresence_All");
        if (presenceDict != null)
        {
            return presenceDict
                .Where(kv =>
                    string.Equals(kv.Value, "BeRightBack", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kv.Value, "Away", StringComparison.OrdinalIgnoreCase))
                .Select(kv => kv.Key)
                .ToList();
        }
        return new List<string>();
    }
}