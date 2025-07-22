using System.ComponentModel.DataAnnotations.Schema;
using EventManagement.Models.User;

namespace EventManagement.Models;

[Table("RefreshTokens")]
public class RefreshTokenModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; set; }
    public UserModel? User { get; set; }
}