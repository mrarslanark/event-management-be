using Carter;
using EventManagement.Data;
using EventManagement.Exceptions;
using EventManagement.Helpers;
using EventManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Modules;

public class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/admin", CreateAdminUser);
    }

    private static async Task<IResult> CreateAdminUser(
        RegisterRequest request,
        AppDbContext db,
        IPasswordHasher<User> hasher,
        IConfiguration config,
        HttpContext http)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email and password are required");

        var isEmailValid = System.Net.Mail.MailAddress.TryCreate(request.Email, out _);
        if (!isEmailValid)
            throw new ArgumentException("Invalid email address");

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser is not null)
            throw new ConflictException("Email already exists");

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
            throw new ArgumentException("Required roles are missing in the database.");

        db.UserRoles.AddRange(new UserRole { UserId = newUser.Id, RoleId = adminRole.Id },
            new UserRole { UserId = newUser.Id, RoleId = userRole.Id });
        await db.SaveChangesAsync();

        var tokenString = AuthModule.GenerateToken(config, newUser.Id.ToString(), newUser.Email, ["Admin", "User"]);
        var data = new
        {
            token = tokenString,
            email = newUser.Email,
            createdAt = DateTimeOffset.UtcNow,
            updatedAt = DateTimeOffset.UtcNow,
        };
        return ApiResponse.Created($"/users/{newUser.Id}", data);
    }
}