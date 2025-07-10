using EventManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Data;

public static class DbSeeder
{
    public static async Task Seed(AppDbContext db, IPasswordHasher<User> hasher)
    {
        // Add Roles to Database
        var roles = new[] { "Admin", "Manager", "User" };
        foreach (var roleName in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new Role() { Name = roleName });
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Seeded Roles: {string.Join(", ", roles)}");

        // Add first admin
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            throw new Exception("Seed admin credentials not provided.");

        var existingAdmin = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == adminEmail);

        if (existingAdmin == null)
        {
            var adminUser = new User
            {
                Email = adminEmail,
                PasswordHash = hasher.HashPassword(null!, adminPassword)
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();

            var userRoles = await db.Roles.ToListAsync();

            var adminRole = userRoles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = userRoles.FirstOrDefault(r => r.Name == "User");

            if (adminRole is null || userRole is null)
                throw new Exception("Required roles not found in DB. Please seed roles first.");

            db.UserRoles.AddRange(new UserRole { UserId = adminUser.Id, RoleId = adminRole.Id },
                new UserRole { UserId = adminUser.Id, RoleId = userRole.Id });
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded first Admin: {adminEmail}");
        }
    }
}