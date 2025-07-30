using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Services;

namespace TimeRecorderBACKEND.Controllers
{
    /// <summary>
    /// Controller for retrieving work time and day off summaries.
    /// Provides endpoints for summary data in JSON and CSV formats.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee, Admin")]
    public class SummaryController : ControllerBase
    {
        private readonly ISummaryService _summaryService;
        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryController"/> class.
        /// </summary>
        /// <param name="summaryService">Service for summary operations.</param>
        public SummaryController(ISummaryService summaryService)
        {
            _summaryService = summaryService;
        }
        /// <summary>
        /// Gets a full summary for a user or project within a specified date range.
        /// </summary>
        /// <param name="dateFrom">Start date for the summary (optional).</param>
        /// <param name="dateTo">End date for the summary (optional).</param>
        /// <param name="userId">User ID to filter by (optional).</param>
        /// <param name="projectId">Project ID to filter by (optional).</param>
        /// <returns>The summary data for the specified criteria.</returns>
        [HttpGet]
        public async Task<ActionResult<SummaryDto>> GetFullSummary(
            [FromQuery] DateTime? dateFrom = null,
            [FromQuery] DateTime? dateTo = null,
            [FromQuery] Guid? userId = null,
            [FromQuery] int? projectId = null)
        {
            SummaryDto summary = await _summaryService.GetFullSummaryAsync(dateFrom, dateTo, userId, projectId);
            return Ok(summary);
        }
        /// <summary>
        /// Gets a CSV file containing summaries for all users within a specified date range.
        /// </summary>
        /// <param name="from">Start date for the summary (optional, defaults to today).</param>
        /// <param name="to">End date for the summary (optional, defaults to today).</param>
        /// <returns>CSV file with summary data for all users.</returns>
        [HttpGet("csv")]
        public async Task<IActionResult> GetSummaryCsv([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            DateTime start = from ?? DateTime.Today;
            DateTime end = to ?? DateTime.Today;
            List<SummaryDto> summaries = await _summaryService.GetFullSummaryForAllAsync(start, end);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("User,WorkMinutes,BreakMinutes,BreakCount,ApprovedDaysOff");

            foreach (var summary in summaries)
            {
                sb.AppendLine($"{summary.UserName} {summary.UserSurname},{summary.TotalWorkTimeMinutes},{summary.TotalBreakTimeMinutes},{summary.BreakCount}, {summary.ApprovedDaysOff}");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"summary_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
        }
    }
}