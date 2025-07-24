using Carter;
using EventManagement.Data;
using EventManagement.Helpers;
using EventManagement.Models.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Modules;

public class KycModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/kyc/approve/{userId:guid}", KycApproved);
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> KycApproved(Guid userId, AppDbContext db)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            throw new KeyNotFoundException("user not found");

        var managerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
        if (managerRole is null)
            throw new ArgumentException("'Manager' role not found in database.");

        var alreadyManager = user.UserRoles.Any(ur => ur.RoleId == managerRole.Id);
        if (alreadyManager)
            return Results.Ok(new { message = $"User {user.Email} already has 'Manager' role." });

        user.UserRoles.Add(new UserRoleModel
        {
            UserId = user.Id,
            RoleId = managerRole.Id
        });

        await db.SaveChangesAsync();
        return ApiResponse.Success(message: $"User {user.Email} has been promoted to 'Manager'.");
    }
}