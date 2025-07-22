using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models.User;

[Table("UserRoles")]
public class UserRoleModel
{
    public Guid UserId   { get; set; }
    public UserModel UserModel { get; set; } = default!;
    
    public Guid RoleId { get; set; }
    public RoleModel RoleModel { get; set; } = default!;
    
}