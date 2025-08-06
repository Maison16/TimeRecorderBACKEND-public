using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;

namespace TimeRecorderBACKEND.Services
{
    public interface IWorkLogService
    {
        Task<WorkLogDto?> GetByIdAsync(int id);
        Task<IEnumerable<WorkLogDtoWithUserNameAndSurname>> GetSpecific(
            Guid? userId = null,
            WorkLogType? type = null,
            bool? isClose = null,
            bool? isDeleted = false,
            DateTime? startDay = null,
            string? firstName = null,
            string? lastName = null,
            int? pageNumber = null,
            int? pageSize = null);

        Task<IEnumerable<WorkLogDtoWithUserNameAndSurname>> GetSpecificForUsers(
            List<Guid>? userIds = null,
            WorkLogType? type = null,
            bool? isClose = null,
            bool? isDeleted = false,
            DateTime? startDay = null,
            string? firstName = null,
            string? lastName = null,
            int? pageNumber = null,
            int? pageSize = null);
        Task<WorkLogDto> StartWorkLogAsync(Guid userId, WorkLogType type);
        Task<WorkLogDto?> UpdateAsync(int id, WorkLogDto dto);
        Task<WorkLogDto?> EndWorkLogAsync(int id);
        Task<bool> DeleteAsync(int id);
        Task AutoCloseOpenBreaksAsync(int maxBreakMinutes);
        Task AutoMarkUnfinishedWorkLogsAsync(int maxWorkHours);
        Task<WorkLogDto?> RestoreAsync(int id);
        Task<List<Guid>> GetUnfinishedUsersAsync();
        Task<int> GetUsedBreakMinutesTodayAsync(Guid userId);
        Task<List<Guid>> GetUsersWithoutStartedWorkTodayAsync();
        Task<List<Guid>> GetUsersWithUnfinishedWorkTodayAsync(int maxWorkHours);
        Task<List<Guid>> GetUsersWithLongActiveBreakAsync(int maxBreakTime);
        Task<WorkLogDto> CreatePastWorkLogAsync(Guid userId, WorkLogType type, DateTime startTime);
        Task<WorkLogDto?> ConfirmPastWorkLogAsync(int workLogId);
        Task<WorkLogDto?> RejectPastWorkLogAsync(int workLogId);
    }
}
