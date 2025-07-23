using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models.User;

[Table("Roles")]
public class RoleModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    public ICollection<UserRoleModel> UserRoles { get; set; } = new List<UserRoleModel>();
}