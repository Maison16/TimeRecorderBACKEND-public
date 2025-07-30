using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.DataBaseContext;

public class NotificationCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationCleanupBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            WorkTimeDbContext dbContext = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();

            DateTime threshold = DateTime.Now.Date.AddDays(-7);

            List<UserNotificationLog> oldNotifications = await dbContext.UserNotificationLogs
                .Where(x => x.DateSent < threshold)
                .ToListAsync(stoppingToken);

            if (oldNotifications.Count > 0)
            {
                dbContext.UserNotificationLogs.RemoveRange(oldNotifications);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}