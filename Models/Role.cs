using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class Role
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column(TypeName = "varchar(255)")]
    public string Name { get; init; } = string.Empty;
    
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}