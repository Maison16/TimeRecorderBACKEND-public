using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.DataBaseContext;

namespace TimeRecorderBACKEND.Services
{
    public class DayOffBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public DayOffBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await UpdateDayOffStatusesAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextRun = DateTime.Today.AddDays(1); 
                TimeSpan delay = nextRun - now;

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await UpdateDayOffStatusesAsync(stoppingToken);
            }
        }

        private async Task UpdateDayOffStatusesAsync(CancellationToken stoppingToken)
        {
            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                WorkTimeDbContext context = scope.ServiceProvider.GetRequiredService<WorkTimeDbContext>();

                DateTime today = DateTime.Today;

                List<DayOffRequest> requests = await context.DayOffRequests
                    .Where(r => r.Status == DayOffStatus.Approved || r.Status == DayOffStatus.Pending && r.DateEnd.Date < today)
                    .ToListAsync(stoppingToken);

                foreach (DayOffRequest req in requests)
                {
                    if(req.Status == DayOffStatus.Approved)
                    {
                        req.Status = DayOffStatus.Executed;
                    }
                    else if (req.Status == DayOffStatus.Pending)
                    {
                        req.Status = DayOffStatus.Rejected;
                    }
                }
                if (requests.Count > 0)
                {
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
