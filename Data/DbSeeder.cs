using EventManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Data;

public static class DbSeeder
{
    public static async Task Seed(AppDbContext db)
    {
        var roles = new[] { "Admin", "Manager", "User" };

        foreach (var roleName in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new Role() { Name = roleName });
            }
        }

        await db.SaveChangesAsync();
    }
}