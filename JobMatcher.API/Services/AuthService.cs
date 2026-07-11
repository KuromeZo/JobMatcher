using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobMatcher.API.Data;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JobMatcher.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Login == request.Login);
        if (exists)
        {
            _logger.LogWarning("Registration failed - login already exists: {Login}", request.Login);
            return null;
        }

        var user = new UserEntity
        {
            Login = request.Login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User registered: {Login}", request.Login);
        return GenerateToken(user);
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login);
        if (user == null)
        {
            _logger.LogWarning("Login failed - user not found: {Login}", request.Login);
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed - wrong password: {Login}", request.Login);
            return null;
        }

        _logger.LogInformation("User logged in: {Login}", request.Login);
        return GenerateToken(user);
    }

    private AuthResponse GenerateToken(UserEntity user)
    {
        var jwtKey = _config["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Login = user.Login
        };
    }
}