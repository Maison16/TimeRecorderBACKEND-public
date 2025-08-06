using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.Services;

namespace TimeRecorderBACKEND.Controllers
{
    /// <summary>
    /// Controller for managing users.
    /// Provides endpoints for retrieving users, user profiles, synchronizing users, and managing user-project assignments.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee, Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        /// <summary>
        /// Initializes a new instance of the <see cref="UserController"/> class.
        /// </summary>
        /// <param name="userService">Service for user operations.</param>
        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Gets all users.
        /// </summary>
        /// <returns>List of all users.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            IEnumerable<UserDto> users = await _userService.GetAllAsync();
            return Ok(users);
        }

        /// <summary>
        /// Gets a user by their ID.
        /// </summary>
        /// <param name="id">User ID.</param>
        /// <returns>User details if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetById(Guid id)
        {
            UserDto? user = await _userService.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        /// <summary>
        /// Synchronizes users from Microsoft Graph. Only accessible by Admins.
        /// </summary>
        /// <returns>Result of the synchronization process.</returns>
        [HttpPost("sync")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SyncUsers()
        {
            bool result = await _userService.SyncUsersAsync();
            if (result)
            {
                return Ok("Users synchronized successfully.");
            }
            else
            {
                return Ok("No new users to synchronize.");
            }
        }

        /// <summary>
        /// Gets the profile of the currently authenticated user.
        /// </summary>
        /// <returns>User profile with roles if found; otherwise, 404 Not Found.</returns>
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            UserInfoDto user =_userService.GetUserProfile();
            if (user == null)
            {
                return NotFound("User profile not found.");
            }
            return Ok(user);
        }

        /// <summary>
        /// Assigns a project to a user. Only accessible by Admins.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="projectId">Project ID.</param>
        /// <returns>Confirmation message if successful; otherwise, 404 Not Found.</returns>
        [HttpPost("{userId:guid}/assign-project/{projectId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignProject(Guid userId, int projectId)
        {
            bool result = await _userService.AssignProjectAsync(userId, projectId);
            if (!result)
            {
                return NotFound("User or Project not found");
            }
            return Ok("User assigned to project");
        }

        /// <summary>
        /// Unassigns a project from a user. Only accessible by Admins.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <returns>Confirmation message if successful; otherwise, 404 Not Found.</returns>
        [HttpPost("{userId:guid}/unassign-project")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnassignProject(Guid userId)
        {
            bool result = await _userService.UnassignProjectAsync(userId);
            if (!result)
            {
                return NotFound("User not found");
            }
            return Ok("Project unassigned from user");
        }

        /// <summary>
        /// Gets the project assigned to a user.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <returns>Project details if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{userId:guid}/project")]
        public async Task<ActionResult<ProjectDto>> GetUserProject(Guid userId)
        {
            ProjectDto? project = await _userService.GetUserProjectAsync(userId);
            if (project == null)
            {
                return NotFound("Project not found for the user");
            }
            return Ok(project);
        }

        /// <summary>
        /// Gets all users with their associated projects. Only accessible by Admins.
        /// </summary>
        /// <returns>List of users with project details.</returns>
        [HttpGet("with-projects")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDtoWithProject>>> GetAllWithProjects(
            [FromQuery] int? pageNumber = null,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? search = null,
            [FromQuery] bool onlyWithoutProject = false)
        {
            IEnumerable<UserDtoWithProject> usersWithProjects = await _userService.GetAllUsersWithProjectsAsync(pageNumber, pageSize, search, onlyWithoutProject);
            return Ok(usersWithProjects);
        }
        /// <summary>
        /// Gets all users assigned to a specific project.
        /// </summary>
        /// <param name="projectId">Project ID.</param>
        /// <returns>List of users assigned to the project.</returns>
        [HttpGet("by-project/{projectId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersByProject(int projectId)
        {
            IEnumerable<UserDto> users = await _userService.GetUsersByProjectAsync(projectId);
            return Ok(users);
        }
    }
}