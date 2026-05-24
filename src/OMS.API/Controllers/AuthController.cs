using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;

namespace OMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    private readonly IAuthService _authService;
    private readonly bool _useZitadelAuth;

    public AuthController(IAuthService authService, IConfiguration configuration) {
        _authService = authService;
        _useZitadelAuth = configuration.GetValue<bool>("Authentication:UseZitadelAuth");
    }

    /// <summary>Login with email and password (disabled when Zitadel auth is active)</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) {
        if (_useZitadelAuth)
            return NotFound(new { error = "In-app authentication is disabled. Use Zitadel authentication.", statusCode = 404 });

        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Register a new user (disabled when Zitadel auth is active)</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request) {
        if (_useZitadelAuth)
            return NotFound(new { error = "In-app registration is disabled. Use Zitadel for user management.", statusCode = 404 });

        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>Login with Google OAuth token (disabled when Zitadel auth is active)</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request) {
        if (_useZitadelAuth)
            return NotFound(new { error = "Google authentication is disabled. Use Zitadel for external identity providers.", statusCode = 404 });

        var result = await _authService.GoogleLoginAsync(request);
        return Ok(result);
    }

    /// <summary>Login with Zitadel access token (only available when Zitadel auth is active)</summary>
    [HttpPost("zitadel")]
    public async Task<ActionResult<AuthResponse>> ZitadelLogin([FromBody] ZitadelLoginRequest request) {
        if (!_useZitadelAuth)
            return NotFound(new { error = "Zitadel authentication is not enabled. Use in-app authentication.", statusCode = 404 });

        var result = await _authService.ZitadelLoginAsync(request);
        return Ok(result);
    }
}
