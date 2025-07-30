using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Services;

namespace TimeRecorderBACKEND.Controllers
{
    /// <summary>
    /// Controller for managing projects.
    /// Provides endpoints for retrieving, creating, updating, and deleting projects.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Employee, Admin")]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectService _projectService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController"/> class.
        /// </summary>
        /// <param name="projectService">Service for project operations.</param>
        public ProjectController(IProjectService projectService)
        {
            _projectService = projectService;
        }

        /// <summary>
        /// Gets all projects.
        /// </summary>
        /// <returns>List of all projects.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll()
        {
            IEnumerable<ProjectDto> projects = await _projectService.GetAllAsync();
            return Ok(projects);
        }

        /// <summary>
        /// Gets a project by its ID.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <returns>The project details if found; otherwise, 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProjectDto>> GetById(int id)
        {
            ProjectDto? project = await _projectService.GetByIdAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }

        /// <summary>
        /// Creates a new project. Only accessible by Admins.
        /// </summary>
        /// <param name="dto">Project data transfer object.</param>
        /// <returns>The created project.</returns>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProjectDto>> Create(ProjectDto dto)
        {
            ProjectDto project = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }

        /// <summary>
        /// Updates an existing project. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <param name="dto">Updated project data.</param>
        /// <returns>The updated project if found; otherwise, 404 Not Found.</returns>

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ProjectDto>> Update(int id, ProjectDto dto)
        {
            ProjectDto? project = await _projectService.UpdateAsync(id, dto);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }

        /// <summary>
        /// Deletes a project. Only accessible by Admins.
        /// </summary>
        /// <param name="id">Project ID.</param>
        /// <returns>No content if deleted(204); otherwise, 404 Not Found.</returns>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            bool deleted = await _projectService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}
