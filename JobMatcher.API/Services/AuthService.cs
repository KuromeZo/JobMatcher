using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JobMatcher.API.Configuration;
using JobMatcher.API.Data;
using JobMatcher.API.Models.Domain;
using JobMatcher.API.Models.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace JobMatcher.API.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwt, ILogger<AuthService> logger)
    {
        _db = db;
        _jwt = jwt.Value;
        _logger = logger;
    }

    /// <summary>Returns null if the login is already taken.</summary>
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Login == request.Login, ct))
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
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("User registered: {Login}", request.Login);
        return GenerateToken(user);
    }

    /// <summary>Returns null if the login or password is wrong.</summary>
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Login == request.Login, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login failed for {Login}", request.Login);
            return null;
        }

        _logger.LogInformation("User logged in: {Login}", request.Login);
        return GenerateToken(user);
    }

    private AuthResponse GenerateToken(UserEntity user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Login)
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_jwt.ExpiresInHours),
            signingCredentials: credentials
        );

        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Login = user.Login
        };
    }
}
