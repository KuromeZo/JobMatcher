using JobMatcher.API.Infrastructure;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobMatcher.API.Controllers;

// Request models are validated by [ApiController]; invalid ones get a 400 ValidationProblemDetails
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitPolicies.Auth)]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);
        if (result == null)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Login already exists");

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _authService.LoginAsync(request, ct);
        if (result == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid login or password");

        return Ok(result);
    }
}
