using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph.Models;
using System.Data;
using System.Security.Claims;
using TimeRecorderBACKEND.DataBaseContext;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;
using Microsoft.AspNetCore.SignalR;

namespace TimeRecorderBACKEND.Services
{
    public class WorkLogService : IWorkLogService
    {
        private readonly WorkTimeDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;
        private readonly IHubContext<WorkStatusHub> _hubContext;
        public readonly string _adminEmail;

        public WorkLogService(WorkTimeDbContext context, IEmailService emailService, IUserService userService, IHubContext<WorkStatusHub> hubContext, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _userService = userService;
            _hubContext = hubContext;
            _adminEmail = configuration["AdminEmail"] ?? throw new ArgumentException("AdminEmail not set");
        }
        public async Task<IEnumerable<WorkLogDtoWithUserNameAndSurname>> GetSpecific(
            Guid? userId = null,
            WorkLogType? type = null,
            bool? isClose = null,
            bool? isDeleted = false,
            DateTime? startDay = null,
            string? firstName = null,
            string? lastName = null,
            int? pageNumber = null,
            int? pageSize = null)
        {
            IQueryable<WorkLog> query;

            if (isDeleted == true)
            {
                query = _context.WorkLogs
                    .Where(x => x.ExistenceStatus == ExistenceStatus.Deleted)
                    .Include(x => x.User);
            }
            else
            {
                query = _context.WorkLogs
                    .Where(x => x.ExistenceStatus == ExistenceStatus.Exist)
                    .Include(x => x.User);
            }

            if (userId != null)
            {
                query = query.Where(w => w.UserId == userId.Value);
            }
            if (type != null)
            {
                query = query.Where(w => w.Type == type.Value);
            }
            if (isClose != null)
            {
                query = isClose.Value
                    ? query.Where(w => w.EndTime != null)
                    : query.Where(w => w.EndTime == null);
            }
            if (startDay != null)
            {
                query = query.Where(w => w.StartTime.Date == startDay.Value.Date);
            }
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                query = query.Where(w => w.User.Name.Contains(firstName));
            }
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                query = query.Where(w => w.User.Surname.Contains(lastName));
            }

            query = query.OrderByDescending(w => w.StartTime);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                query = query
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value);
            }

            return await query
                .Select(w => new WorkLogDtoWithUserNameAndSurname
                {
                    Id = w.Id,
                    Status = w.Status,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    Type = w.Type,
                    UserId = w.UserId,
                    Duration = w.Duration ?? 0,
                    CreatedAt = w.CreatedAt,
                    UserName = w.User != null ? w.User.Name : null,
                    UserSurname = w.User != null ? w.User.Surname : null
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<WorkLogDtoWithUserNameAndSurname>> GetSpecificForUsers(
    List<Guid>? userIds = null,
    WorkLogType? type = null,
    bool? isClose = null,
    bool? isDeleted = false,
    DateTime? startDay = null,
    string? firstName = null,
    string? lastName = null,
    int? pageNumber = null,
    int? pageSize = null)
        {
            IQueryable<WorkLog> query;

            if (isDeleted == true)
            {
                query = _context.WorkLogs
                    .Where(x => x.ExistenceStatus == ExistenceStatus.Deleted)
                    .Include(x => x.User);
            }
            else
            {
                query = _context.WorkLogs
                    .Where(x => x.ExistenceStatus == ExistenceStatus.Exist)
                    .Include(x => x.User);
            }

            if (userIds != null && userIds.Any())
            {
                query = query.Where(w => userIds.Contains(w.UserId));
            }
            if (type != null)
            {
                query = query.Where(w => w.Type == type.Value);
            }
            if (isClose != null)
            {
                query = isClose.Value
                    ? query.Where(w => w.EndTime != null)
                    : query.Where(w => w.EndTime == null);
            }
            if (startDay != null)
            {
                query = query.Where(w => w.StartTime.Date == startDay.Value.Date);
            }
            if (!string.IsNullOrWhiteSpace(firstName))
            {
                query = query.Where(w => w.User.Name.Contains(firstName));
            }
            if (!string.IsNullOrWhiteSpace(lastName))
            {
                query = query.Where(w => w.User.Surname.Contains(lastName));
            }

            query = query.OrderByDescending(w => w.StartTime);

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                query = query
                    .Skip((pageNumber.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value);
            }

            return await query
                .Select(w => new WorkLogDtoWithUserNameAndSurname
                {
                    Id = w.Id,
                    Status = w.Status,
                    StartTime = w.StartTime,
                    EndTime = w.EndTime,
                    Type = w.Type,
                    UserId = w.UserId,
                    Duration = w.Duration ?? 0,
                    CreatedAt = w.CreatedAt,
                    UserName = w.User != null ? w.User.Name : null,
                    UserSurname = w.User != null ? w.User.Surname : null
                })
                .ToListAsync();
        }
        public async Task<WorkLogDto?> GetByIdAsync(int id)
        {
            WorkLog? workLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workLog == null)
            {
                return null;
            }
            return ToDto(workLog);
        }

        public async Task<WorkLogDto> StartWorkLogAsync(Guid userId, WorkLogType type)
        {
            if (type != WorkLogType.Work && type != WorkLogType.Break)
            {
                throw new ArgumentException("Invalid work log type. Only 'Work' and 'Break' are allowed.");
            }

            UserDto? user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found.");
            }

            if (type == WorkLogType.Break)
            {
                WorkLog? activeWorkLog = await _context.WorkLogs
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.Type == WorkLogType.Work && w.EndTime == null && w.ExistenceStatus==ExistenceStatus.Exist);
                Settings settings = await _context.Settings.FirstOrDefaultAsync();
                int usedBreakMinutes = await GetUsedBreakMinutesTodayAsync(userId);
                int breakTimeLeft = settings.MaxBreakTime - usedBreakMinutes;
                if (breakTimeLeft <=0)
                {
                    throw new InvalidOperationException("Whole break time has been used.");
                }
                if (activeWorkLog == null)
                {
                    throw new InvalidOperationException("Cannot start a break without an active work session.");
                }
                
                activeWorkLog.EndTime = DateTime.Now;
                activeWorkLog.Status = WorkLogStatus.Finished;

                WorkLog breakLog = new WorkLog
                {
                    UserId = userId,
                    StartTime = DateTime.Now,
                    Type = WorkLogType.Break
                };

                _context.WorkLogs.Add(breakLog);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new
                {
                    userId,
                    status = type == WorkLogType.Break ? "break_started" : "work_started"
                });
                return ToDto(breakLog);
            }
            else if (type == WorkLogType.Work)
            {
                WorkLog? activeBreakLog = await _context.WorkLogs
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.Type == WorkLogType.Break && w.EndTime == null && w.ExistenceStatus == ExistenceStatus.Exist);

                if(activeBreakLog != null)
                {
                    activeBreakLog.EndTime = DateTime.Now;
                    activeBreakLog.Status = WorkLogStatus.Finished;
                }

                WorkLog workLog = new WorkLog
                {
                    UserId = userId,
                    StartTime = DateTime.Now,
                    Type = WorkLogType.Work
                };

                _context.WorkLogs.Add(workLog);
                await _context.SaveChangesAsync();
                await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new
                {
                    userId,
                    status = type == WorkLogType.Work ? "work_started" : "break_started"
                });
                return ToDto(workLog);
            }

            throw new InvalidOperationException("Unexpected work log type.");
        }

        public async Task<WorkLogDto?> UpdateAsync(int id, WorkLogDto dto)
        {
            WorkLog? workLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.Id == id && w.ExistenceStatus == ExistenceStatus.Exist);

            if (workLog == null)
            {
                return null;
            }

            workLog.StartTime = dto.StartTime;
            workLog.EndTime = dto.EndTime;
            workLog.Status = dto.Status;
            workLog.Type = dto.Type;
            await _context.SaveChangesAsync();
            return ToDto(workLog);
        }
        public async Task<WorkLogDto?> EndWorkLogAsync(int id)
        {
            WorkLog? workLog = await _context.WorkLogs.FirstOrDefaultAsync(w => w.Id == id && w.ExistenceStatus==ExistenceStatus.Exist);
            if (workLog == null)
            {
                return null;
            }
            if (workLog.EndTime != null)
            {
                throw new InvalidOperationException("Work log is already ended.");
            }
            Models.User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == workLog.UserId && u.ExistenceStatus == ExistenceStatus.Exist);
            if (user == null)
            {
                throw new InvalidOperationException("User not found.");
            }
            switch (workLog.Type)
            {
                case WorkLogType.Break:
                    if (await HasOtherActiveBreak(workLog.UserId, workLog.Id))
                        throw new InvalidOperationException("Another active break is already in progress.");
                    break;

                case WorkLogType.Work:
                    if (await HasActiveBreak(workLog.UserId))
                        throw new InvalidOperationException("Cannot end work while a break is active.");
                    break;
            }

            workLog.EndTime = DateTime.Now;
            if (workLog.Status == WorkLogStatus.Started)
            {
                workLog.Status = WorkLogStatus.Finished;
            }
            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(workLog.UserId.ToString()).SendAsync("WorkStatusChanged", new
            {
                userId = workLog.UserId,
                status = "work_ended"
            });
            return ToDto(workLog);
        }
        public async Task<bool> DeleteAsync(int id)
        {
            WorkLog? workLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.Id == id);
            if (workLog == null)
            {
                return false;
            }
            workLog.ExistenceStatus = ExistenceStatus.Deleted;
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task AutoCloseOpenBreaksAsync(int maxBreakMinutes)
        {
            DateTime now = DateTime.Now;

            List<WorkLog> openBreaks = await _context.WorkLogs
                .Include(w => w.User)
                .Where(w =>
                    w.Type == WorkLogType.Break &&
                    w.EndTime == null &&
                    EF.Functions.DateDiffMinute(w.StartTime, now) > maxBreakMinutes)
                .ToListAsync();

            foreach (WorkLog breakLog in openBreaks)
            {
                UserDto? userDto = await _userService.GetByIdAsync(breakLog.UserId);
                if (userDto != null)
                {
                    await _emailService.SendAsync(
                        userDto.Email,
                        "The break has been automatically ended",
                        $"Your break started at {breakLog.StartTime} has been automatically ended after {EF.Functions.DateDiffMinute(breakLog.StartTime, now)} minutes."
                    );
                    await _emailService.SendAsync(
                        _adminEmail,
                        $"The break has been automatically ended for user: {userDto.Name} {userDto.Surname}",
                        $"User's break with email {userDto.Email} started at {breakLog.StartTime} has been automatically ended after {EF.Functions.DateDiffMinute(breakLog.StartTime, now)} minutes."
                    );
                    await _hubContext.Clients.User(breakLog.UserId.ToString()).SendAsync("WorkStatusChanged", new
                    {
                        userId = breakLog.UserId,
                        status = "break_ended"
                    });
                }
            }
            await _context.SaveChangesAsync();

        }
        public async Task AutoMarkUnfinishedWorkLogsAsync(int maxWorkHours)
        {
            DateTime dayStart = DateTime.Now.Date;
            DateTime dayEnd = DateTime.Now.Date.AddDays(1);

            List<WorkLog> workLogs = await _context.WorkLogs
                .Where(w => w.Type == WorkLogType.Work
                    && w.StartTime >= dayStart
                    && w.StartTime < dayEnd
                    && w.ExistenceStatus == ExistenceStatus.Exist)
                .ToListAsync();

            List<WorkLog> result = new List<WorkLog>();
            IEnumerable<IGrouping<Guid, WorkLog>> grouped = workLogs.GroupBy(w => w.UserId);

            foreach (var group in grouped)
            {
                int totalMinutes = group.Sum(w =>
                    w.EndTime.HasValue
                        ? (int)(w.EndTime.Value - w.StartTime).TotalMinutes
                        : (int)(DateTime.Now - w.StartTime).TotalMinutes);

                if (totalMinutes > maxWorkHours * 60)
                {
                    result.AddRange(group.Where(w => w.EndTime == null));
                }
            }

            foreach (WorkLog? w in result)
            {
                UserDto? userDto = await _userService.GetByIdAsync(w.UserId);
                if (userDto != null && w.Status != WorkLogStatus.RequiresAttention)
                {
                    w.Status = WorkLogStatus.RequiresAttention;
                    await _emailService.SendAsync(
                        userDto.Email,
                        "Unended WorkLog!",
                        $"Your WorkLog started at {w.StartTime} wasn't finish in {maxWorkHours} hours. Contact with admin {_adminEmail}!"
                    );
                    await _emailService.SendAsync(
                        _adminEmail,
                        $"User's {userDto.Name} {userDto.Surname} unended WorkLog!",
                        $"User worklog with email {userDto.Email} starrted at {w.StartTime} wasn't finish in {maxWorkHours} hours."
                    );
                    await _hubContext.Clients.User(w.UserId.ToString()).SendAsync("WorkStatusChanged", new
                    {
                        userId = w.UserId,
                        status = "auto_work_ended"
                    });
                }
            }
            await _context.SaveChangesAsync();
        }
        
        private static WorkLogDto ToDto(WorkLog w) 
        {
            return new WorkLogDto
            {
                Id = w.Id,
                Status = w.Status,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                Type = w.Type,
                UserId = w.UserId,
                CreatedAt = w.CreatedAt,
                Duration = w.Duration ?? 0
            };
        }
        private Task<bool> HasActiveBreak(Guid userId)
        {
            return _context.WorkLogs.AnyAsync(w =>
                w.UserId == userId &&
                w.Type == WorkLogType.Break &&
                w.EndTime == null &&
                w.ExistenceStatus == ExistenceStatus.Exist);
        }
        private Task<bool> HasOtherActiveBreak(Guid userId, int currentBreakId)
        {
            return _context.WorkLogs.AnyAsync(w =>
                w.UserId == userId &&
                w.Type == WorkLogType.Break &&
                w.EndTime == null &&
                w.ExistenceStatus == ExistenceStatus.Exist &&
                w.Id != currentBreakId);
        }

        public async Task<WorkLogDto?> RestoreAsync(int id)
        {
            WorkLog? workLog = await _context.WorkLogs
                .FirstOrDefaultAsync(w => w.Id == id && w.ExistenceStatus == ExistenceStatus.Deleted);

            if (workLog == null)
            {
                return null;
            }

            workLog.ExistenceStatus = ExistenceStatus.Exist;
            await _context.SaveChangesAsync();

            return ToDto(workLog);
        }

        public async Task<List<Guid>> GetUnfinishedUsersAsync()
        {
            DateTime now = DateTime.Now;

            List<Guid> unfinishedUserIds = await _context.WorkLogs
                .Where(w => w.EndTime == null && w.Type == WorkLogType.Work)
                .Select(w => w.UserId)
                .Distinct()
                .ToListAsync();

            return unfinishedUserIds;
        }

        public async Task<int> GetUsedBreakMinutesTodayAsync(Guid userId)
        {
            DateTime today = DateTime.Today;
            var breaks = await _context.WorkLogs
                .Where(w => w.UserId == userId
                    && w.Type == WorkLogType.Break
                    && w.StartTime.Date == today
                    && w.EndTime != null)
                .ToListAsync();

            int usedMinutes = breaks.Sum(b => (int)((b.EndTime.Value - b.StartTime).TotalMinutes));
            return usedMinutes;
        }
        public async Task<int> GetUsedWorkMinutesTodayAsync(Guid userId)
        {
            DateTime today = DateTime.Today;
            List<WorkLog> workLogs = await _context.WorkLogs
                .Where(w => w.UserId == userId
                    && w.Type == WorkLogType.Work
                    && w.StartTime.Date == today
                    && w.EndTime != null)
                .ToListAsync();
            int usedMinutes = workLogs.Sum(w => (int)((w.EndTime.Value - w.StartTime).TotalMinutes));
            return usedMinutes;
        }
        public async Task<List<Guid>> GetUsersWithoutStartedWorkTodayAsync()
        {
            DateTime today = DateTime.Today;

            //get all users
            List<Guid> allUserIds = await _context.Users
                .Where(u => u.ExistenceStatus == ExistenceStatus.Exist)
                .Select(u => u.Id)
                .ToListAsync();
            // get users who have started work today
            List<Guid> usersWithStartedWork = await _context.WorkLogs
                .Where(w => w.Type == WorkLogType.Work
                    && w.StartTime.Date == today
                    && w.ExistenceStatus == ExistenceStatus.Exist)
                .Select(w => w.UserId)
                .Distinct()
                .ToListAsync();
            // get users who are on confirmed day off today
            List<Guid> usersOnConfirmedDayOff = await _context.DayOffRequests
                .Where(d => d.Status == DayOffStatus.Approved
                    && d.ExistenceStatus == ExistenceStatus.Exist
                    && d.DateStart.Date <= today
                    && d.DateEnd.Date >= today)
                .Select(d => d.UserId)
                .Distinct()
                .ToListAsync();
            //  WITHOUT those who have started work today and those who are on confirmed day off
            List<Guid> usersWithoutStartedWork = allUserIds
                .Except(usersWithStartedWork)
                .Except(usersOnConfirmedDayOff)
                .ToList();

            return usersWithoutStartedWork;
        }

        public async Task<List<Guid>> GetUsersWithUnfinishedWorkTodayAsync(int maxWorkHours)
        {
            DateTime today = DateTime.Today;

            List<WorkLog> workLogs = await _context.WorkLogs
                .Where(w => w.Type == WorkLogType.Work
                    && w.StartTime.Date == today
                    && w.ExistenceStatus == ExistenceStatus.Exist)
                .ToListAsync();

            List<Guid> userIds = workLogs
                .GroupBy(w => w.UserId)
                .Where(g => g.Sum(w =>
                    w.EndTime.HasValue
                        ? (int)(w.EndTime.Value - w.StartTime).TotalMinutes
                        : (int)(DateTime.Now - w.StartTime).TotalMinutes
                ) > maxWorkHours * 60)
                .Select(g => g.Key)
                .ToList();
            List<Guid> result = userIds
       .Where(userId => workLogs.Any(w => w.UserId == userId && w.EndTime == null))
       .ToList();


            return userIds;
        }

        public async Task<List<Guid>> GetUsersWithLongActiveBreakAsync(int maxBreakMinutes)
        {
            DateTime today = DateTime.Today;
            DateTime now = DateTime.Now;

            List<WorkLog> activeBreaks = await _context.WorkLogs
                .Where(w => w.Type == WorkLogType.Break
                    && w.EndTime == null
                    && w.StartTime.Date == today
                    && w.ExistenceStatus == ExistenceStatus.Exist)
                .ToListAsync();

            List<Guid> usersWithLongBreaks = activeBreaks
                .GroupBy(w => w.UserId)
                .Where(g => g.Sum(b => (int)(now - b.StartTime).TotalMinutes) > maxBreakMinutes)
                .Select(g => g.Key)
                .ToList();

            return usersWithLongBreaks;
        }

        public async Task<WorkLogDto> CreatePastWorkLogAsync(Guid userId, WorkLogType type, DateTime startTime)
        {
            if (type != WorkLogType.Work && type != WorkLogType.Break)
                throw new ArgumentException("Invalid work log type. Only 'Work' and 'Break' are allowed.");

            UserDto? user = await _userService.GetByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("User not found.");

            DateTime now = DateTime.Now;

            // Cant create a work log in the future
            if (startTime > now)
                throw new InvalidOperationException("Cannot create a work log in the future.");

            // Cant create a work log for a time more than 2 hours ago
            if ((now - startTime).TotalHours > 2)
                throw new InvalidOperationException("Cannot create a work log for a time more than 2 hours ago.");

            if (type == WorkLogType.Break)
            {
                // Is Work Active?
                WorkLog? activeWorkLog = await _context.WorkLogs
                    .Where(w => w.UserId == userId
                                && w.Type == WorkLogType.Work
                                && w.StartTime <= startTime
                                && (w.EndTime == null || w.EndTime > startTime)
                                && w.ExistenceStatus == ExistenceStatus.Exist)
                    .FirstOrDefaultAsync();

                if (activeWorkLog == null)
                    throw new InvalidOperationException("Cannot start a break without an active work session at the specified time.");

                Settings settings = await _context.Settings.FirstOrDefaultAsync();
                int usedBreakMinutes = await GetUsedBreakMinutesTodayAsync(userId);
                int breakTimeLeft = settings.MaxBreakTime - usedBreakMinutes;
                if (breakTimeLeft <= 0)
                    throw new InvalidOperationException("Whole break time has been used.");
            }

            if (type == WorkLogType.Work)
            {
                //Is Break Active?
                WorkLog? activeBreakLog = await _context.WorkLogs
                    .Where(w => w.UserId == userId
                                && w.Type == WorkLogType.Break
                                && w.StartTime <= startTime
                                && (w.EndTime == null || w.EndTime > startTime)
                                && w.ExistenceStatus == ExistenceStatus.Exist)
                    .FirstOrDefaultAsync();

                if (activeBreakLog != null)
                {
                    activeBreakLog.EndTime = startTime;
                    activeBreakLog.Status = WorkLogStatus.Finished;
                }
            }

            WorkLog workLog = new WorkLog
            {
                UserId = userId,
                StartTime = startTime,
                Type = type,
                Status = WorkLogStatus.RequiresAttention
            };

            _context.WorkLogs.Add(workLog);
            await _emailService.SendAsync(
                _adminEmail,
                $"Work log started with changed hours {user.Name} {user.Surname}",
$"User's {(workLog.Type == WorkLogType.Work ? "Work" : "Break")} with email {user.Email} started at {workLog.StartTime:dd.MM.yyyy HH:mm} has been set {(int)(DateTime.Now - workLog.StartTime).TotalMinutes} minutes after this date.");
            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(userId.ToString()).SendAsync("WorkStatusChanged", new
            {
                userId,
                status = type == WorkLogType.Work ? "work_started" : "break_started"
            });
            return ToDto(workLog);
        }

        public async Task<WorkLogDto?> ConfirmPastWorkLogAsync(int workLogId)
        {
            WorkLog? workLog = await _context.WorkLogs.FirstOrDefaultAsync(w => w.Id == workLogId && w.Status == WorkLogStatus.RequiresAttention);
            if (workLog == null)
            {
                return null;
            }

            workLog.Status = WorkLogStatus.Finished;
            workLog.EndTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return ToDto(workLog);
        }
        public async Task<WorkLogDto?> RejectPastWorkLogAsync(int workLogId)
        {
            WorkLog? workLog = await _context.WorkLogs.FirstOrDefaultAsync(w => w.Id == workLogId && w.Status == WorkLogStatus.RequiresAttention);
            if (workLog == null)
            {
                return null;
            }

            workLog.Status = WorkLogStatus.Finished;
            workLog.StartTime = workLog.CreatedAt;
            workLog.EndTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return ToDto(workLog);
        }
    }
}
