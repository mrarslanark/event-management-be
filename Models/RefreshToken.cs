using System.ComponentModel.DataAnnotations.Schema;


namespace EventManagement.Models;

public class RefreshToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column(TypeName = "varchar(512)")]
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool IsRevoked { get; set; }
    public Guid UserId { get; init; }
    public User? User { get; init; }
}