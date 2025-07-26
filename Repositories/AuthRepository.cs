using EventManagement.Data;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repositories;

public class AuthRepository(AppDbContext db) : IAuthRepository
{
    public async Task<User?> GetUserById(Guid userId)
    {
        return await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
    public async Task<User?> GetUserByEmail(string email)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
    
    public async Task<User?> GetUserByEmailVerificationToken(string token)
    {
        return await db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);
    }

    public async Task AddUser(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task<List<string>> GetUserRoles(Guid userId)
    {
        return await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public async Task AssignRole(UserRole userRole)
    {
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync();
    }

    public async Task AssignRoles(List<UserRole> userRoles)
    {
        db.UserRoles.AddRange(userRoles);
        await db.SaveChangesAsync();
    }
    
    public async Task AssignRoleToUser(User user, UserRole userRole)
    {
        user.UserRoles.Add(userRole);
        await db.SaveChangesAsync();
    }
    
    
    public async Task<RefreshToken?> GetRefreshToken(string token)
    {
        return await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);
    }

    public async Task SaveRefreshToken(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public async Task RevokeRefreshToken(RefreshToken token)
    {
        token.IsRevoked = true;
        await db.SaveChangesAsync();
    }

    public async Task<Role?> GetRoleByName(string name)
    {
        return await db.Roles.FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task VerifyEmail(User user)
    {
        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}