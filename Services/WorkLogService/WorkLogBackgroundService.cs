using Microsoft.EntityFrameworkCore;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.Services;

public class WorkLogBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

    public WorkLogBackgroundService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    { 
        while (!stoppingToken.IsCancellationRequested)
        {
            await PerformWorkLogTasksAsync();
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PerformWorkLogTasksAsync()
    {
        using (IServiceScope scope = _serviceProvider.CreateScope())
        {
            IWorkLogService workLogService = scope.ServiceProvider.GetRequiredService<IWorkLogService>();
            WorkTimeDbContext dbContext = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();
            ITeamsService teamsService = scope.ServiceProvider.GetRequiredService<ITeamsService>();
            Settings? settings = await dbContext.Settings.FirstOrDefaultAsync();
            ArgumentNullException.ThrowIfNull(settings, nameof(settings));

            await workLogService.AutoMarkUnfinishedWorkLogsAsync(settings.MaxWorkHoursDuringOneDay);

            DateTime now = DateTime.Now;

        }
    }
}
