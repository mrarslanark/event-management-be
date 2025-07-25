using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Email { get; init; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}