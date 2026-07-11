using System.ComponentModel.DataAnnotations;

namespace JobMatcher.API.Models.Persistence;

public class UserEntity
{
    [Key]
    public int Id { get; set; }
    public string Login { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}