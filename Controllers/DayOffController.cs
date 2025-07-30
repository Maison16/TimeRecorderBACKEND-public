using Microsoft.AspNetCore.Mvc;
using TimeRecorderBACKEND.Services;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph.Models;

namespace TimeRecorderBACKEND.Controllers
{
    /// <summary>
    /// Controller for managing day off requests.
    /// Provides endpoints for requesting, approving, cancelling, editing, filtering, restoring, and deleting day off requests.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee, Admin")]

    public class DayOffController : ControllerBase
    {
        private readonly IDayOffService _dayOffService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DayOffController"/> class.
        /// </summary>
        /// <param name="dayOffService">Service for day off operations.</param>
        public DayOffController(IDayOffService dayOffService)
        {
            _dayOffService = dayOffService;
        }

        /// <summary>
        /// Requests a day off for a user.
        /// </summary>
        /// <param name="model">Day off request details.</param>
        /// <returns>The created day off request.</returns>
        [HttpPost]
        public async Task<ActionResult<DayOffRequestDto>> RequestDayOff([FromBody] RequestDayOffModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {   
                Guid parsedId;
                if (model.UserId == null)
                {
                    string? claimId = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;
                    if (string.IsNullOrWhiteSpace(claimId) || !Guid.TryParse(claimId, out parsedId))
                    {
                        return Unauthorized("User ID not found in token.");
                    }
                }
                else
                {
                    parsedId = model.UserId.Value;
                }
                DayOffRequestDto result = await _dayOffService.RequestDayOffAsync(parsedId, model.DateStart, model.DateEnd, model.Reason);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Approves or rejects a day off request. Only accessible by Admins.
        /// </summary>
        /// <param name="requestId">ID of the day off request.</param>
        /// <param name="decision">Decision status (Approved, Rejected, Cancelled).</param>
        /// <returns>The updated day off request.</returns>
        [HttpPost("decision/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DayOffRequestDto>> ApproveDayOff(int requestId, DayOffStatus decision)
        {
            if (decision != DayOffStatus.Approved && decision != DayOffStatus.Rejected && decision != DayOffStatus.Cancelled)
            {
                return BadRequest("Wrong DayOffStatus.");
            }

            DayOffRequestDto result = await _dayOffService.ChangeDayOffStatusAsync(requestId, decision, User);
            return Ok(result);
        }
        /// <summary>
        /// Cancels a day off request.
        /// </summary>
        /// <param name="requestId">ID of the day off request.</param>
        /// <returns>The updated day off request.</returns>
        [HttpPost("cancel/{requestId}")]
        public async Task<ActionResult<DayOffRequestDto>> CancelDayOff(int requestId)
        {
            DayOffRequestDto result = await _dayOffService.ChangeDayOffStatusAsync(requestId, DayOffStatus.Cancelled, User);
            return Ok(result);
        }
        /// <summary>
        /// Gets all day off requests for a specific user.
        /// </summary>
        /// <param name="userId">User ID (optional, if not provided, uses current user).</param>
        /// <returns>List of day off requests for the user.</returns>
        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<DayOffRequestDto>>> GetUserDayOffs([FromQuery] Guid? userId)
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
                }
                else
                {
                    parsedId = userId.Value;
                }
                IEnumerable<DayOffRequestDto> result = await _dayOffService.GetUserDayOffsAsync(parsedId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message }); // 401
            }
        }
        /// <summary>
        /// Gets filtered day off requests.
        /// </summary>
        /// <param name="userId">User ID to filter by.</param>
        /// <param name="Name">User's name to filter by.</param>
        /// <param name="Surname">User's surname to filter by.</param>
        /// <param name="statuses">Statuses to filter by.</param>
        /// <param name="dateStart">Start date filter.</param>
        /// <param name="dateEnd">End date filter.</param>
        /// <param name="isdDeleted">Include deleted requests.</param>
        /// <param name="pageNumber">Page number for pagination.</param>
        /// <param name="pageSize">Page size for pagination.</param>
        /// <returns>List of filtered day off requests with user details.</returns>
        [HttpGet("filter")]
        public async Task<ActionResult<IEnumerable<DayOffRequestDtoWithUserNameAndSurname>>> GetSpecific(
        [FromQuery] Guid? userId,
        [FromQuery] string? Name,
        [FromQuery] string? Surname,
        [FromQuery] DayOffStatus[]? statuses,
        [FromQuery] DateTime? dateStart,
        [FromQuery] DateTime? dateEnd,
        [FromQuery] bool? isdDeleted = false,
        [FromQuery] int? pageNumber = null,
        [FromQuery] int? pageSize = null)
        {
            try
            {
                IEnumerable<DayOffRequestDtoWithUserNameAndSurname> resultWithDeleted = await _dayOffService.Filter(userId, Name, Surname, statuses, dateStart, dateEnd, isdDeleted, pageNumber, pageSize);
                return Ok(resultWithDeleted);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Edits a day off request.
        /// </summary>
        /// <param name="requestId">ID of the day off request.</param>
        /// <param name="newStartDate">New start date.</param>
        /// <param name="newEndDate">New end date.</param>
        /// <param name="newReason">New reason for the day off.</param>
        /// <returns>The updated day off request.</returns>
        [HttpPut("{requestId}")]
        public async Task<ActionResult<DayOffRequestDto>> EditDayOff(int requestId, DateTime newStartDate, DateTime newEndDate, string? newReason)
        {
            try
            {
                if (newEndDate < newStartDate)
                {
                    return BadRequest("End date cannot be before start date.");
                }
                DayOffRequestDto result = await _dayOffService.EditDayOffRequestAsync(requestId, newStartDate, newEndDate, newReason, User);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message }); // 400
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message }); // 409
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message }); // 500
            }
        }
        /// <summary>
        /// Gets a specific day off request by its ID.
        /// </summary>
        /// <param name="request">ID of the day off request.</param>
        /// <returns>The day off request details.</returns>
        [HttpGet("{requestId}")]
        public async Task<ActionResult<DayOffRequestDto>> GetDayOffById(int request)
        {
            try
            {
                DayOffRequestDto? result = await _dayOffService.GetDayOffRequestByIdAsync(request);
                if (result == null)
                {
                    return NotFound(new { error = "Day off request not found." }); // 404
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred.", detail = ex.Message }); // 500
            }
        }

        /// <summary>
        /// Deletes a day off request. Only accessible by Admins.
        /// </summary>
        /// <param name="requestId">ID of the day off request.</param>
        /// <returns>No content if successful.</returns>
        [HttpDelete("{requestId}")]
        [Authorize(Roles = "Admin")]
        // Endpoint to delete a day off request
        public async Task<IActionResult> DeleteDayOff(int requestId)
        {
            try
            {
                await _dayOffService.DeleteDayOffRequestAsync(requestId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message }); // 404
            }
        }


        /// <summary>
        /// Restores a deleted day off request. Only accessible by Admins.
        /// </summary>
        /// <param name="requestId">ID of the day off request.</param>
        /// <returns>The restored day off request.</returns>
        [HttpPost("restore/{requestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DayOffRequestDto>> RestoreDayOff(int requestId)
        {
            try
            {
                DayOffRequestDto result = await _dayOffService.RestoreDayOffRequestAsync(requestId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return NotFound(new { error = ex.Message }); // 404
            }
        }
    }
}
