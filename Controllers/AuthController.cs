using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TimeRecorderBACKEND.Dtos;
using TimeRecorderBACKEND.Services;

/// <summary>
/// Controller responsible for user authentication.
/// Provides endpoints for login, logout, and authentication status check.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="authService">Service handling authentication logic.</param>
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    /// <summary>
    /// Authenticates the user based on the provided JWT token.
    /// Sets the authentication cookie if the token is valid.
    /// </summary>
    /// <param name="request">Object containing the JWT token.</param>
    /// <returns>Returns 200 OK if authentication is successful, otherwise 401 Unauthorized.</returns>
    [HttpPost("login")]
    public IActionResult Login([FromBody] TokenRequest request)
    {
        if (!_authService.ValidateToken(request.Token, out ClaimsPrincipal? principal))
        {
            return Unauthorized();
        }

        UserInfoDto? userInfo = principal != null
            ? _authService.GetUserInfo(principal)
            : null;

        _authService.SetAuthCookie(Response, request.Token);

        return Ok(userInfo);
    }
    /// <summary>
    /// Logs out the currently authenticated user.
    /// Removes the authentication cookie.
    /// </summary>
    /// <returns>Returns 200 OK with a logout confirmation message.</returns>
    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _authService.RemoveAuthCookie(Response);
        return Ok(new { message = "Logged out successfully" });
    }
    /// <summary>
    /// Checks the authentication status of the current user.
    /// </summary>
    /// <returns>Returns user information if authenticated.</returns>
    [HttpGet("check")]
    [Authorize]
    public IActionResult CheckAuth()
    {
        return Ok(_authService.GetUserInfo(User));
    }
}
