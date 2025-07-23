using EventManagement.Models.Event;
using EventManagement.Models.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Data;

public static class DbSeeder
{
    public static async Task Seed(IConfiguration config, AppDbContext db, IPasswordHasher<UserModel> hasher)
    {
        // Add Roles to Database
        await AddRoles(db);

        // Add EventModel Types to Database
        await AddEventTypes(db);
        
        // Add first admin
        await AddFirstAdmin(config, db, hasher);
    }

    private static async Task AddRoles(AppDbContext db)
    {
        var roles = new[] { "Admin", "Manager", "UserModel" };
        foreach (var roleName in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new RoleModel() { Name = roleName });
            }
        }

        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Seeded Roles: {string.Join(", ", roles)}");
    }

    private static async Task AddEventTypes(AppDbContext db)
    {
        var eventTypes = new[] { "Music", "Sports", "Conference", "Workshop" };
        foreach (var name in eventTypes)
        {
            if (!await db.EventTypes.AnyAsync(et => et.Name == name))
            {
                db.EventTypes.Add(new EventTypeModel { Name = name });
            }
        }
        
        await db.SaveChangesAsync();
        Console.WriteLine($"✅ Seeded EventModel Types: {string.Join(", ", eventTypes)}");
    }

    private static async Task AddFirstAdmin(IConfiguration config, AppDbContext db, IPasswordHasher<UserModel> hasher)
    {
        var admin = config.GetSection("Admin");
        var email = admin["Email"];
        var password = admin["Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new Exception("Seed admin credentials not provided.");

        var existingAdmin = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.RoleModel)
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (existingAdmin == null)
        {
            var adminUser = new UserModel
            {
                Email = email,
                PasswordHash = hasher.HashPassword(null!, password)
            };

            db.Users.Add(adminUser);
            await db.SaveChangesAsync();

            var userRoles = await db.Roles.ToListAsync();

            var adminRole = userRoles.FirstOrDefault(r => r.Name == "Admin");
            var userRole = userRoles.FirstOrDefault(r => r.Name == "UserModel");

            if (adminRole is null || userRole is null)
                throw new Exception("Required roles not found in DB. Please seed roles first.");

            db.UserRoles.AddRange(new UserRoleModel { UserId = adminUser.Id, RoleId = adminRole.Id },
                new UserRoleModel { UserId = adminUser.Id, RoleId = userRole.Id });
            await db.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded first Admin: {email}");
        }
    }
}