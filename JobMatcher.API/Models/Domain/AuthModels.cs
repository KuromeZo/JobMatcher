namespace JobMatcher.API.Models.Domain;

public class RegisterRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Login { get; set; } = "";
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string Token { get; set; } = "";
    public string Login { get; set; } = "";
}