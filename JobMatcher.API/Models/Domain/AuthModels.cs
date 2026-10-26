using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Domain;

public class RegisterRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Login { get; set; } = "";

    // BCrypt only uses the first 72 bytes of a password
    [Required, StringLength(72, MinimumLength = 6)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, MaxLength(50)]
    public string Login { get; set; } = "";

    [Required, MaxLength(72)]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Login { get; set; } = "";
}
