using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Models;
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

            // Fixing CS1503: Convert nullable DateTime to non-nullable DateTime
            SummaryListDto result = await _summaryService.GetDailySummariesAsync(start, end);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Name,Surname,Email,Date,WorkMinutes,BreakMinutes,BreakCount");

            // Assuming summaries should be result.Summaries
            foreach (SummaryDto summary in result.Summaries)
            {
                sb.AppendLine($"{summary.UserName},{summary.UserSurname},{summary.UserEmail},{summary.Date:yyyy-MM-dd},{summary.TotalWorkTimeMinutes},{summary.TotalBreakTimeMinutes},{summary.BreakCount}");
            }

            byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
            return File(bytes, "text/csv", $"summary_{start:yyyyMMdd}_{end:yyyyMMdd}.csv");
        }

        /// <summary>
        /// Gets daily summaries for a user or project within a specified date range.
        /// </summary>
        /// <param name="dateFrom">Start date for the summaries.</param>
        /// <param name="dateTo">End date for the summaries.</param>
        /// <param name="usersId">Usesr ID to filter by (optional). If not typed select all users</param>
        /// <param name="projectId">Project ID to filter by (optional).</param>
        /// <returns>A list of daily summaries for the specified criteria.</returns>
        [HttpGet("daily")]
        public async Task<ActionResult<SummaryListDto>> GetDailySummaries(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            [FromQuery] List<Guid>? usersId = null,
            [FromQuery] int? projectId = null)
        {
            SummaryListDto result = await _summaryService.GetDailySummariesAsync(dateFrom, dateTo, usersId, projectId);
            return Ok(result);
        }
    }
}