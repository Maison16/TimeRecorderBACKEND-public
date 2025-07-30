using Microsoft.EntityFrameworkCore;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;
using ZiggyCreatures.Caching.Fusion;

namespace TimeRecorderBACKEND.Services
{
    public class SummaryService : ISummaryService
    {
        private readonly WorkTimeDbContext _context;
        private readonly IFusionCache _cache;

        public SummaryService(WorkTimeDbContext context, IFusionCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<SummaryDto> GetFullSummaryAsync(
      DateTime? dateFrom = null,
      DateTime? dateTo = null,
      Guid? userId = null,
      int? projectId = null
  )
        {
            string cacheKey = $"summary_{userId}_{dateFrom}_{dateTo}_{projectId}";
            return await _cache.GetOrSetAsync(
                cacheKey,
                async _ =>
                {
                    Console.BackgroundColor = ConsoleColor.Blue;
                    Console.WriteLine("Fetching summary from db...");
                    Console.ResetColor();
                    IQueryable<WorkLog> workLogs = _context.WorkLogs.Include(w => w.User).Where(w => w.ExistenceStatus == ExistenceStatus.Exist);
                    if (dateFrom != null)
                        workLogs = workLogs.Where(w => w.StartTime >= dateFrom.Value.Date);
                    if (dateTo != null)
                        workLogs = workLogs.Where(w => w.StartTime <= dateTo.Value.Date.AddDays(1).AddTicks(-1));
                    if (userId != null)
                        workLogs = workLogs.Where(w => w.UserId == userId.Value);
                    if (projectId != null)
                        workLogs = workLogs.Where(w => w.User.ProjectId == projectId.Value);

                    List<WorkLog> workLogList = await workLogs.ToListAsync();

                    int totalWork = 0;
                    int totalBreak = 0;
                    int breakCount = 0;

                    foreach (WorkLog log in workLogList)
                    {
                        if (log.Type == WorkLogType.Break)
                        {
                            breakCount++;
                            totalBreak += log.Duration ?? 0;
                            continue;
                        }
                        totalWork += log.Duration ?? 0;
                    }

                    IQueryable<DayOffRequest> dayOffQuery = _context.DayOffRequests.Where(d => d.ExistenceStatus == ExistenceStatus.Exist);
                    if (dateFrom.HasValue)
                        dayOffQuery = dayOffQuery.Where(x => x.DateStart >= dateFrom.Value.Date);
                    if (dateTo.HasValue)
                        dayOffQuery = dayOffQuery.Where(x => x.DateEnd <= dateTo.Value.Date);
                    if (userId.HasValue)
                        dayOffQuery = dayOffQuery.Where(x => x.UserId == userId.Value);

                    List<DayOffRequest> requests = await dayOffQuery.ToListAsync();
                    int approvedDaysOff = 0;
                    int rejectedDaysOff = 0;
                    int pendingDaysOff = 0;
                    int cancelledDaysOff = 0;
                    int dayOffRequestCount = 0;
                    int executedDaysOff = 0;
                    foreach (DayOffRequest request in requests)
                    {
                        dayOffRequestCount++;
                        int days = request.DateStart.Date == request.DateEnd.Date ? 1 : (int)(request.DateEnd - request.DateStart).TotalDays + 1;
                        switch (request.Status)
                        {
                            case DayOffStatus.Approved:
                                approvedDaysOff += days;
                                break;
                            case DayOffStatus.Rejected:
                                rejectedDaysOff += days;
                                break;
                            case DayOffStatus.Pending:
                                pendingDaysOff += days;
                                break;
                            case DayOffStatus.Cancelled:
                                cancelledDaysOff += days;
                                break;
                            case DayOffStatus.Executed:
                                executedDaysOff += days;
                                break;
                        }
                    }

                    return new SummaryDto
                    {
                        TotalWorkTimeMinutes = totalWork,
                        TotalBreakTimeMinutes = totalBreak,
                        WorkLogCount = workLogList.Count,
                        BreakCount = breakCount,
                        DayOffRequestCount = dayOffRequestCount,
                        ApprovedDaysOff = approvedDaysOff,
                        RejectedDaysOff = rejectedDaysOff,
                        PendingDaysOff = pendingDaysOff,
                        CancelledDaysOff = cancelledDaysOff,
                        ExecutedDaysOff = executedDaysOff
                    };
                },
                TimeSpan.FromMinutes(5) 
            );
        }

        public async Task<List<SummaryDto>> GetFullSummaryForAllAsync(DateTime from, DateTime to)
        {
            List<User> users = await _context.Users.Where(u => u.ExistenceStatus == ExistenceStatus.Exist).ToListAsync();
            List<SummaryDto> result = new List<SummaryDto>();

            foreach (var user in users)
            {
                SummaryDto summary = await GetFullSummaryAsync(from, to, user.Id);
                summary.UserName = user.Name;
                summary.UserSurname = user.Surname;
                result.Add(summary);
            }
            return result;
        }
    }
}