using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TimeRecorderBACKEND.Models;
using TimeRecorderBACKEND.Services;

/// <summary>
/// Controller for managing application settings.
/// Provides endpoints for retrieving and updating global settings.
/// Only accessible by users with the Admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsController"/> class.
    /// </summary>
    /// <param name="settingsService">Service for settings operations.</param>
    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }
    /// <summary>
    /// Gets the current application settings.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <returns>The current settings if found; otherwise, 404 Not Found.</returns>
    [HttpGet]
    public async Task<ActionResult<Settings>> Get()
    {
        var settings = await _settingsService.GetSettingsAsync();
        if (settings == null)
            return NotFound();
        return settings;
    }
    /// <summary>
    /// Updates the application settings.
    /// Only accessible by users with the Admin role.
    /// </summary>
    /// <param name="updated">The updated settings object.</param>
    /// <returns>No content if update is successful; otherwise, 404 Not Found.</returns>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] Settings updated)
    {
        var result = await _settingsService.UpdateSettingsAsync(updated);
        if (!result)
            return NotFound();
        return NoContent();
    }
}