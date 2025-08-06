using TimeRecorderBACKEND.Dtos;

namespace TimeRecorderBACKEND.Services
{
    public interface ISummaryService
    {
        Task<SummaryDto> GetFullSummaryAsync(
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        Guid? userId = null,
        int? projectId = null);

        Task<SummaryListDto> GetDailySummariesAsync(DateTime dateFrom, DateTime dateTo, List<Guid>? userIds = null, int? projectId = null);
    }

}