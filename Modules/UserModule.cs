using Carter;
using EventManagement.Exceptions;
using EventManagement.Helpers;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace EventManagement.Modules;

public class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/users/admin", CreateAdminUser);
    }

    private static async Task<IResult> CreateAdminUser(
        RegisterRequest request,
        IPasswordHasher<User> hasher,
        IConfiguration config,
        HttpContext http,
        IAuthRepository repo)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Email and password are required");

        var isEmailValid = System.Net.Mail.MailAddress.TryCreate(request.Email, out _);
        if (!isEmailValid)
            throw new ArgumentException("Invalid email address");

        var existingUser = await repo.GetUserByEmail(request.Email);
        if (existingUser is not null)
            throw new ConflictException("Email already exists");

        var newUser = new User
        {
            Email = request.Email,
            PasswordHash = hasher.HashPassword(null!, request.Password),
        };
        await repo.AddUser(newUser);
        
        var adminRole = await repo.GetRoleByName("Admin");
        var userRole = await repo.GetRoleByName("User");

        if (adminRole is null || userRole is null)
            throw new ArgumentException("Required roles are missing in the database.");

        List<UserRole> roles =
        [
            new() { UserId = newUser.Id, RoleId = adminRole.Id },
            new() { UserId = newUser.Id, RoleId = userRole.Id }
        ];
        await repo.AssignRoles(roles);

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