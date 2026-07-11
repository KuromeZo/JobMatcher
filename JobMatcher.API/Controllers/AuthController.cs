using JobMatcher.API.Models.Domain;
using JobMatcher.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace JobMatcher.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Login and password are required");

        if (request.Password.Length < 6)
            return BadRequest("Password must be at least 6 characters");

        var result = await _authService.RegisterAsync(request);
        if (result == null)
            return Conflict("Login already exists");

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Login and password are required");

        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized("Invalid login or password");

        return Ok(result);
    }
}