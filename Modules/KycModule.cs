using Carter;
using EventManagement.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Modules;

public class KycModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/kyc/approve/{userId:guid}", KycApproved);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("/kyc/approve/{userId:guid}")]
    private static async Task<IResult> KycApproved(Guid userId, AppDbContext db)
    {
        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
            return Results.NotFound(new { message = "User not found." });

        var managerRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Manager");
        if (managerRole is null)
            return Results.BadRequest(new { message = "'Manager' role not found in database." });

        var alreadyManager = user.UserRoles.Any(ur => ur.RoleId == managerRole.Id);
        if (alreadyManager)
            return Results.Ok(new { message = $"User {user.Email} already has 'Manager' role." });

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = managerRole.Id
        });

        await db.SaveChangesAsync();
        return Results.Ok(new { message = $"User {user.Email} has been promoted to 'Manager'." });
    }
}