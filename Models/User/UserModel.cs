using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models.User;

[Table("Users")]
public class UserModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Email { get; init; } = string.Empty;
    
    public string PasswordHash { get; set; } = string.Empty;
    
    public bool isEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<UserRoleModel> UserRoles { get; set; } = new List<UserRoleModel>();
}