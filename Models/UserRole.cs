namespace EventManagement.Models;

public class UserRole
{
    public Guid UserId   { get; init; }
    public User User { get; init; } = null!;
    public Guid RoleId { get; init; }
    public Role Role { get; init; } = null!;
}