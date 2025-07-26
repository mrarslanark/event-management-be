using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class User
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column(TypeName = "varchar(320)")]
    public string Email { get; init; } = string.Empty;
    
    [Column(TypeName = "varchar(512)")]
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool IsEmailVerified { get; set; }
    
    [Column(TypeName = "varchar(256)")]
    public string? EmailVerificationToken { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}