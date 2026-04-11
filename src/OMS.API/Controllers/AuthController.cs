using Microsoft.AspNetCore.Mvc;
using OMS.Application.DTOs;
using OMS.Application.Interfaces;

namespace OMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase {
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) {
        _authService = authService;
    }

    /// <summary>Login with email and password</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request) {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Register a new user</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request) {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(Register), result);
    }

    /// <summary>Login with Google OAuth token</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> GoogleLogin([FromBody] GoogleLoginRequest request) {
        var result = await _authService.GoogleLoginAsync(request);
        return Ok(result);
    }
}
