using EventManagement.Models;

namespace EventManagement.Repositories.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserById(Guid id);
    Task<User?> GetUserByEmail(string email);
    Task<User?> GetUserByEmailVerificationToken(string token);
    Task AddUser(User user);
    Task<List<string>> GetUserRoles(Guid userId);
    Task AssignRole(UserRole userRole);
    Task AssignRoles(List<UserRole> userRoles);
    Task AssignRoleToUser(User user, UserRole userRole);
    Task<RefreshToken?> GetRefreshToken(string token);
    Task SaveRefreshToken(RefreshToken token);
    Task RevokeRefreshToken(RefreshToken token);
    Task<Role?> GetRoleByName(string name);
    Task VerifyEmail(User user);
}