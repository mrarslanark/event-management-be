using Carter;
using EventManagement.Data;
using EventManagement.Helpers;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
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
    private static async Task<IResult> KycApproved(Guid userId, IAuthRepository repo)
    {
        var user = await repo.GetUserById(userId);
        if (user is null)
            throw new KeyNotFoundException("user not found");
        
        var managerRole = await repo.GetRoleByName("Manager");
        if (managerRole is null)
            throw new ArgumentException("'Manager' role not found in database.");

        var isManager = user.UserRoles.Any(ur => ur.RoleId == managerRole.Id);
        if (isManager)
            return Results.Ok(new { message = $"User {user.Email} already has 'Manager' role." });

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = managerRole.Id
        };
        await repo.AssignRoleToUser(user, userRole);
        return ApiResponse.Success(message: $"User {user.Email} has been promoted to 'Manager'.");
    }
}