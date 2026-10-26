using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    // HMAC-SHA256 needs a key of at least 256 bits
    [Required, MinLength(32)]
    public string Key { get; set; } = "";

    [Required]
    public string Issuer { get; set; } = "";

    [Required]
    public string Audience { get; set; } = "";

    [Range(1, 720)]
    public int ExpiresInHours { get; set; } = 24;
}
