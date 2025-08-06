using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Graph.Models;
using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.Services;

namespace TimeRecorderBACKEND.Controllers
{
    /// <summary>
    /// Controller for managing work logs.
    /// Provides endpoints for creating, retrieving, updating, deleting, restoring, and filtering work logs.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee, Admin")]
    public class WorkLogController : ControllerBase
    {
        private readonly IWorkLogService _workLogService;
        private readonly ITeamsService _teamsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkLogController"/> class.
        /// </summary>
        /// <param name="workLogService">Service for work log operations.</param>
        /// <param name="teamsService">Service for Teams notifications.</param>
        public WorkLogController(IWorkLogService workLogService, ITeamsService teamsService)
        {
            _workLogService = workLogService;
            _teamsService = teamsService;
        }

        /// <summary>
        /// Gets a work log by its ID.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>The work log details if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<WorkLogDto>> GetById(int id)
        {
            WorkLogDto? log = await _workLogService.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }
            return Ok(log);
        }

        /// <summary>
        /// Gets filtered work logs by optional user ID, type, project, and other parameters.
        /// </summary>
        /// <param name="userId">User ID (optional).</param>
        /// <param name="type">Work log type (optional).</param>
        /// <param name="isClose">Whether the work log is closed (optional).</param>
        /// <param name="startDay">Start day filter (optional).</param>
        /// <param name="isDeleted">Whether the work log is deleted (optional).</param>
        /// <param name="Name">User's name filter (optional).</param>
        /// <param name="Surname">User's surname filter (optional).</param>
        /// <param name="pageNumber">Page number for pagination (optional).</param>
        /// <param name="pageSize">Page size for pagination (optional).</param>
        /// <returns>List of filtered work logs with user details.</returns>
        [HttpGet("filter")] 
        public async Task<ActionResult<IEnumerable<WorkLogDtoWithUserNameAndSurname>>> GetSpecific( 
            [FromQuery] Guid? userId = null, 
            [FromQuery] WorkLogType? type = null, 
            [FromQuery] bool? isClose = null, 
            [FromQuery] DateTime? startDay = null, 
            [FromQuery] bool? isDeleted = false, 
            [FromQuery] string? Name = null, 
            [FromQuery] string? Surname = null, 
            [FromQuery] int? pageNumber = null, 
            [FromQuery] int? pageSize = null) 
        { 
            if (userId == null) 
            { 
                string? claimId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
            if (string.IsNullOrWhiteSpace(claimId) || !Guid.TryParse(claimId, out Guid parsedId))
            {
            return Unauthorized("User ID not found in token.");
            }

                userId = parsedId;
            }

        IEnumerable<WorkLogDtoWithUserNameAndSurname> logs = await _workLogService.GetSpecific(userId, type, isClose, isDeleted, startDay, Name, Surname, pageNumber, pageSize);
        return Ok(logs);
        }
        /// <summary>
        /// Gets filtered work logs for multiple users by their IDs and other filters.
        /// </summary>
        /// <param name="userIds">List of user IDs.</param>
        /// <param name="type">Work log type (optional).</param>
        /// <param name="isClose">Whether the work log is closed (optional).</param>
        /// <param name="startDay">Start day filter (optional).</param>
        /// <param name="isDeleted">Whether the work log is deleted (optional).</param>
        /// <param name="Name">User's name filter (optional).</param>
        /// <param name="Surname">User's surname filter (optional).</param>
        /// <param name="pageNumber">Page number for pagination (optional).</param>
        /// <param name="pageSize">Page size for pagination (optional).</param>
        /// <returns>List of filtered work logs with user details.</returns>
        [HttpPost("filter-multi")]
        public async Task<ActionResult<IEnumerable<WorkLogDtoWithUserNameAndSurname>>> GetSpecificForUsers(
            [FromBody] List<Guid> userIds,
            [FromQuery] WorkLogType? type = null,
            [FromQuery] bool? isClose = null,
            [FromQuery] DateTime? startDay = null,
            [FromQuery] bool? isDeleted = false,
            [FromQuery] string? Name = null,
            [FromQuery] string? Surname = null,
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null)
        {
            IEnumerable<WorkLogDtoWithUserNameAndSurname> logs = await _workLogService.GetSpecificForUsers(userIds, type, isClose, isDeleted, startDay, Name, Surname, pageNumber, pageSize);
            return Ok(logs);
        }
        /// <summary>
        /// Starts a work log for a user.
        /// </summary>
        /// <param name="userId">User ID (optional).</param>
        /// <param name="type">Type of work log.</param>
        /// <param name="startTime">Start time (optional, for past logs).</param>
        /// <returns>The created work log.</returns>
        [HttpPost("start")]
        public async Task<ActionResult<WorkLogDto>> StartWorkLog([FromQuery] Guid? userId, WorkLogType type, [FromQuery] DateTime? startTime = null)
        {
            try
            {
                Guid parsedId;
                if (userId == null)
                {
                    string? claimId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

                    if (string.IsNullOrWhiteSpace(claimId) || !Guid.TryParse(claimId, out parsedId))
                    {
                        return Unauthorized("User ID not found in token.");
                    }

                    userId = parsedId;
                }
                else
                {
                    parsedId = userId.Value;
                }
                WorkLogDto log;
                if (startTime.HasValue)
                {
                    log = await _workLogService.CreatePastWorkLogAsync(parsedId, type, startTime.Value);
                }
                else
                {
                    log = await _workLogService.StartWorkLogAsync(parsedId, type);
                }
                return CreatedAtAction(nameof(GetById), new { id = log.Id }, log);

            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Ends a work log by setting its end time to now.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>The updated work log if found; otherwise, 404 Not Found.</returns>
        [HttpPost("end/{id:int}")]
        public async Task<ActionResult<WorkLogDto>> EndWorkLog(int id)
        {
            try
            {
                WorkLogDto? log = await _workLogService.EndWorkLogAsync(id);
                if (log == null)
                {
                    return NotFound();
                }
                return Ok(log);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing work log. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <param name="dto">Updated work log data.</param>
        /// <returns>The updated work log if found; otherwise, 404 Not Found.</returns>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WorkLogDto>> Update(int id, WorkLogDto dto)
        {
            WorkLogDto? log = await _workLogService.UpdateAsync(id, dto);
            if (log == null)
            {
                return NotFound();
            }
            return Ok(log);
        }

        /// <summary>
        /// Deletes a work log. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>No content if deleted; otherwise, 404 Not Found.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            bool deleted = await _workLogService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }

        /// <summary>
        /// Automatically closes open breaks that exceed the specified maximum break minutes. Only accessible by Admins.
        /// </summary>
        /// <param name="maxBreakMinutes">Maximum allowed break duration in minutes.</param>
        /// <returns>Confirmation message.</returns>
        [HttpPost("auto-close-breaks")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AutoCloseOpenBreaks([FromQuery] int maxBreakMinutes)
        {
            await _workLogService.AutoCloseOpenBreaksAsync(maxBreakMinutes);
            return Ok("Breaks has been automaticly ended");
        }


        /// <summary>
        /// Automatically marks unfinished work logs that exceed the specified maximum work hours. Only accessible by Admins.
        /// </summary>
        /// <param name="maxWorkHours">Maximum allowed work duration in hours.</param>
        /// <returns>Confirmation message.</returns>
        [HttpPost("auto-mark-unfinished")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AutoMarkUnfinishedWorkLogs([FromQuery] int maxWorkHours)
        {
            await _workLogService.AutoMarkUnfinishedWorkLogsAsync(maxWorkHours);
            return Ok("Unended worklogs status has been changed");
        }

        /// <summary>
        /// Restores a deleted work log. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>The restored work log if found; otherwise, 404 Not Found.</returns>
        [HttpPost("restore/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<WorkLogDto>> RestoreWorkLog(int id)
        {
            try
            {
                WorkLogDto? restoredLog = await _workLogService.RestoreAsync(id);
                if (restoredLog == null)
                {
                    return NotFound(new { error = "Work log not found or is not deleted." });
                }
                return Ok(restoredLog);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Confirms a past work log entry by its ID. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>The confirmed work log if found; otherwise, 404 Not Found.</returns>
        [HttpPost("confirm-past/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ConfirmPastWorkLog(int id)
        {
            WorkLogDto? result = await _workLogService.ConfirmPastWorkLogAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Rejects a past work log entry by its ID. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Work log ID.</param>
        /// <returns>The rejected work log if found; otherwise, 404 Not Found.</returns>
        [HttpPost("reject-past/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectPastWorkLog(int id)
        {
            WorkLogDto? result = await _workLogService.RejectPastWorkLogAsync(id);
            return result == null ? NotFound() : Ok(result);
        }
    }
}
