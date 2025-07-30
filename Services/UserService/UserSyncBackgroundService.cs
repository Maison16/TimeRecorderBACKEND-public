using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Enums;

public class UserSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private TimeSpan _checkInterval = TimeSpan.FromMinutes(5); 

    public UserSyncBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        DateTime lastSyncDate = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                WorkTimeDbContext dbContext = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();
                IUserService userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                TimeRecorderBACKEND.Models.Settings? settings = await dbContext.Settings.FirstOrDefaultAsync(stoppingToken);
                if (settings == null)
                {
                    await Task.Delay(_checkInterval, stoppingToken);
                    continue;
                }

                int syncHour = settings.SyncUsersHour;
                DateTime now = DateTime.Now;

                bool shouldSync = false;
                switch (settings.SyncUsersFrequency)
                {
                    case SyncFrequency.Daily:
                        shouldSync = now.Hour == syncHour && lastSyncDate.Date != now.Date;
                        break;
                    case SyncFrequency.Weekly:
                        shouldSync = settings.SyncUsersDays.Contains((SyncDayOfWeek)now.DayOfWeek)
                            && now.Hour == syncHour
                            && lastSyncDate.Date != now.Date;
                        break;
                }

                if (shouldSync)
                {
                    await userService.SyncUsersAsync();
                    lastSyncDate = now;
                }
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}