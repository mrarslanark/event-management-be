using EventManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Routes;

public static class KycRoutes
{
    public static void MapKycEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/kyc/approve/{userId:guid}", async (
            Guid userId,
            AppDbContext db,
            HttpContext http) =>
        {
            if (!http.User.IsInRole("Admin"))
                return Results.Forbid();

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
        }).RequireAuthorization(policy => policy.RequireRole("Admin"));
    }
}