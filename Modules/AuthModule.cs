using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Carter;
using EventManagement.Data;
using EventManagement.DTOs;
using EventManagement.Models;
using EventManagement.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Modules;

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", Login);
        app.MapPost("/register", Register);
        app.MapPost("/verify-email", VerifyEmail);
        app.MapPost("/refresh-token", RefreshToken);
    }

    private static async Task<IResult> Login(UserLogin request, AppDbContext db, IPasswordHasher<User> hasher)
    {
        var isEmailValid = VerifyEmailAddress(request.Email);
        if (!isEmailValid)
            return Results.BadRequest(new { message = "Invalid Email Address" });

        // Find user from the database
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result is not PasswordVerificationResult.Success)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        var roles = await GetRoles(db, user.Id.ToString());
        var (accessToken, refreshToken) = await GenerateTokensAsync(user.Id, user.Email, roles, db);
        if (accessToken is null || refreshToken is null)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        return Results.Ok(new
        {
            uid = user.Id,
            token = accessToken,
            refreshToken,
            email = user.Email,
            roles,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        });
    }

    private static async Task<IResult> Register(UserRegister request, IValidator<UserRegister> validator,
        AppDbContext db, IPasswordHasher<User> hasher)
    {
        // if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        //     return Results.BadRequest(new { message = "Invalid Credentials" });
        //
        // var isEmailValid = VerifyEmailAddress(request.Email);
        // if (!isEmailValid)
        //     return Results.BadRequest(new { message = "Invalid Email Address" });

        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser is not null)
            return Results.Conflict(new { message = "User already exists, Login" });

        var emailToken = Guid.NewGuid().ToString();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hasher.HashPassword(null!, request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            isEmailVerified = false,
            EmailVerificationToken = emailToken
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

        Console.WriteLine($"Simulated verification token: {emailToken}");

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "User");
        if (role is null)
            return Results.BadRequest(new { message = "Default role 'User' not found in database." });

        db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var roles = await GetRoles(db, user.Id.ToString());

        var (accessToken, refreshToken) = await GenerateTokensAsync(user.Id, user.Email, roles, db);
        if (accessToken is null || refreshToken is null)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        return Results.Created($"/users/{user.Id}", new
        {
            token = accessToken,
            refreshToken,
            email = user.Email,
            roles,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        });
    }

    private static async Task<IResult> VerifyEmail([FromBody] VerifyEmailRequest request, AppDbContext db)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token);
        if (user is null)
            return Results.BadRequest(new { message = "Invalid or expired token." });

        if (user.isEmailVerified)
            return Results.Ok(new { message = "Email is already verified" });

        user.isEmailVerified = true;
        user.EmailVerificationToken = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Email Verified successfully" });
    }

    private static async Task<IResult> RefreshToken(RefreshTokenRequest request, AppDbContext db)
    {
        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);
        if (existingToken is null || existingToken.ExpiresAt < DateTime.UtcNow)
        {
            return Results.Unauthorized();
        }

        var user = existingToken.User;
        if (user is null)
            return Results.BadRequest(new { message = "Invalid refresh token." });

        var userId = user.Id.ToString();

        // Revoke old token
        existingToken.IsRevoked = true;
        await db.SaveChangesAsync();

        var roles = await GetRoles(db, userId);
        var (accessToken, refreshToken) = await GenerateTokensAsync(user.Id, user.Email, roles, db);
        if (accessToken is null || refreshToken is null)
            return Results.BadRequest(new { message = "Unable to refresh token." });

        return Results.Ok(new
        {
            token = accessToken,
            refreshToken
        });
    }

    private static bool VerifyEmailAddress(string email)
    {
        return System.Net.Mail.MailAddress.TryCreate(email, out _);
    }

    private static async Task<List<string>> GetRoles(AppDbContext db, string id)
    {
        return await db.UserRoles
            .Where(userRole => userRole.UserId.ToString() == id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();
    }

    public static string? GenerateToken(string userId, string email, List<string> roles)
    {
        var authKey = Environment.GetEnvironmentVariable("AUTH_KEY");
        var issuer = Environment.GetEnvironmentVariable("AUTH_ISSUER");
        var audience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE");
        var expiration = Environment.GetEnvironmentVariable("AUTH_EXPIRE_MINUTES");

        if (authKey == null || expiration == null) return null;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer,
            audience,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(expiration)),
            signingCredentials: signingCredentials,
            claims: claims
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }

    private static string? GenerateRefreshToken()
    {
        var refreshTokenSize = Environment.GetEnvironmentVariable("AUTH_REFRESH_TOKEN_SIZE");
        if (refreshTokenSize is null || !int.TryParse(refreshTokenSize, out var size) || size <= 0)
            return null;
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(size));
    }

    private static async Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(Guid userId,
        string email, List<string> roles, AppDbContext db)
    {
        var accessToken = GenerateToken(userId.ToString(), email, roles);
        var refreshToken = GenerateRefreshToken();

        if (accessToken is null || refreshToken is null)
            return (null, null);

        var refreshTokenExpiryDays = Environment.GetEnvironmentVariable("AUTH_REFRESH_TOKEN_EXPIRE_DAYS");
        if (refreshTokenExpiryDays is null || !int.TryParse(refreshTokenExpiryDays, out var expiryDays) ||
            expiryDays <= 0)
            return (null, null);

        db.RefreshTokens.Add(new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        });
        await db.SaveChangesAsync();

        return (accessToken, refreshToken);
    }
}