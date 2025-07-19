using EventManagement.Data;
using EventManagement.Models;
using EventManagement.Modules;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Routes;

public static class UserRoutes
{
    public static void MapUserRoutes(this IEndpointRouteBuilder app)
    {
        app.MapPost("/users/admin", async (
            RegisterRequest request,
            AppDbContext db,
            IPasswordHasher<User> hasher,
            IConfiguration config,
            HttpContext http) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest(new { message = "Email and password are required" });

            var isEmailValid = System.Net.Mail.MailAddress.TryCreate(request.Email, out _);
            if (!isEmailValid)
                return Results.BadRequest(new { message = "Invalid email format" });

            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser is not null)
                return Results.Conflict(new { message = "Email already exists" });

            var newUser = new User
            {
                Email = request.Email,
                PasswordHash = hasher.HashPassword(null!, request.Password),
            };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();

            var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User");

            if (adminRole is null || userRole is null)
                return Results.BadRequest(new { message = "Required roles are missing in the database." });

            db.UserRoles.AddRange(new UserRole { UserId = newUser.Id, RoleId = adminRole.Id }, new UserRole { UserId = newUser.Id, RoleId = userRole.Id });
            await db.SaveChangesAsync();
            
            var tokenString = AuthModule.GenerateToken(newUser.Id.ToString(), newUser.Email, ["Admin", "User"]);
            return Results.Created($"/users/{newUser.Id}", new
            {
                token = tokenString,
                email = newUser.Email,
                createdAt = DateTimeOffset.UtcNow,
                updatedAt = DateTimeOffset.UtcNow,
            });
        }).RequireAuthorization("AdminOnly");
    }
}