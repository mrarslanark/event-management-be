using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManagement.Data;
using EventManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Routes;

public static class AuthRoutes
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login",
            async (UserLogin request, AppDbContext db, IPasswordHasher<User> hasher, IConfiguration config) =>
            {
                var isEmailValid = VerifyEmailAddress(request.Email);
                if (!isEmailValid)
                {
                    return Results.BadRequest(new { message = "Invalid Email Address" });
                }

                // Find user from database
                var user = await db.Users.FirstOrDefaultAsync(u => u.EmailAddress == request.Email);
                if (user is null)
                    return Results.BadRequest(new { message = "Invalid Credentials" });

                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
                if (result is not PasswordVerificationResult.Success)
                    return Results.BadRequest(new { message = "Invalid Credentials" });

                var tokenString = GenerateToken(user.Id.ToString(), user.EmailAddress);
                return Results.Ok(new
                {
                    token = tokenString,
                    email = user.EmailAddress,
                    createdAt = user.CreatedAt,
                    updatedAt = user.UpdatedAt
                });
            });

        app.MapPost("/register", async (UserRegister request, AppDbContext db, IPasswordHasher<User> hasher) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(new { message = "Invalid Credentials" });
            }

            var isEmailValid = VerifyEmailAddress(request.Email);
            if (!isEmailValid)
            {
                return Results.BadRequest(new { message = "Invalid Email Address" });
            }

            var existingUser = await db.Users.FirstOrDefaultAsync(u => u.EmailAddress == request.Email);
            if (existingUser is not null)
            {
                return Results.Conflict(new { message = "User already exists, Login" });
            }

            var user = new User { EmailAddress = request.Email };
            user.PasswordHash = hasher.HashPassword(user, request.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var tokenString = GenerateToken(user.Id.ToString(), user.EmailAddress);

            return Results.Created($"/users/{user.Id}", new
            {
                token = tokenString,
                email = user.EmailAddress,
                createdAt = user.CreatedAt,
                updatedAt = user.UpdatedAt
            });
        });
    }

    private static bool VerifyEmailAddress(string email)
    {
        return System.Net.Mail.MailAddress.TryCreate(email, out _);
    }

    private static string? GenerateToken(string userId, string emailAddress)
    {
        var authKey = Environment.GetEnvironmentVariable("AUTH_KEY");
        var issuer = Environment.GetEnvironmentVariable("AUTH_ISSUER");
        var audience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE");
        var expiration = Environment.GetEnvironmentVariable("AUTH_EXPIRE_MINUTES");

        if (authKey == null || expiration == null) return null;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(expiration)),
            signingCredentials: signingCredentials,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, emailAddress)
            ]
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }
}

public record UserLogin(string Email, string Password);

public record UserRegister(string Email, string Password);