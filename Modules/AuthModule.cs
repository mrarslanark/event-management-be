using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Carter;
using EventManagement.Data;
using EventManagement.Models;
using EventManagement.Models.User;
using EventManagement.Requests;
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

    private static async Task<IResult> Login(
        IConfiguration config, 
        UserLoginRequest request,
        IValidator<UserLoginRequest> validator,
        AppDbContext db, 
        IPasswordHasher<UserModel> hasher)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        // Find user from the database
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result is not PasswordVerificationResult.Success)
            return Results.BadRequest(new { message = "Invalid Credentials" });

        var roles = await GetRoles(db, user.Id.ToString());
        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, db);
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

    private static async Task<IResult> Register(
        IConfiguration config, 
        UserRegisterRequest request,
        IValidator<UserRegisterRequest> validator,
        AppDbContext db, 
        IPasswordHasher<UserModel> hasher)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser is not null)
            return Results.Conflict(new { message = "UserModel already exists, Login" });

        var emailToken = Guid.NewGuid().ToString();

        var user = new UserModel
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

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "UserModel");
        if (role is null)
            return Results.BadRequest(new { message = "Default role 'UserModel' not found in database." });

        db.UserRoles.Add(new UserRoleModel
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await db.SaveChangesAsync();

        var roles = await GetRoles(db, user.Id.ToString());

        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, db);
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

    private static async Task<IResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        IValidator<VerifyEmailRequest> validator, 
        AppDbContext db)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

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

    private static async Task<IResult> RefreshToken(
        IConfiguration config, 
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator, 
        AppDbContext db)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var existingToken = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);
        if (existingToken is null || existingToken.ExpiresAt < DateTime.UtcNow)
            return Results.Unauthorized();

        var user = existingToken.User;
        if (user is null)
            return Results.BadRequest(new { message = "Invalid refresh token." });

        var userId = user.Id.ToString();

        // Revoke old token
        existingToken.IsRevoked = true;
        await db.SaveChangesAsync();

        var roles = await GetRoles(db, userId);
        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, db);
        if (accessToken is null || refreshToken is null)
            return Results.BadRequest(new { message = "Unable to refresh token." });

        return Results.Ok(new
        {
            token = accessToken,
            refreshToken
        });
    }

    private static async Task<List<string>> GetRoles(AppDbContext db, string id)
    {
        return await db.UserRoles
            .Where(userRole => userRole.UserId.ToString() == id)
            .Include(ur => ur.RoleModel)
            .Select(ur => ur.RoleModel.Name)
            .ToListAsync();
    }

    public static string? GenerateToken(
        IConfiguration config, 
        string userId, 
        string email, 
        List<string> roles)
    {
        var jwt = config.GetSection("Jwt");
        var authKey = jwt["Key"];
        var expiration = jwt.GetValue<int>("ExpiryMinutes");
        
        if (authKey is null)
            return null;
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey));
        var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            expires: DateTime.UtcNow.AddMinutes(expiration),
            signingCredentials: signingCredentials,
            claims: claims
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return tokenString;
    }

    private static string? GenerateRefreshToken(IConfiguration config)
    {
        var size = config.GetValue<int>("Jwt:RefreshTokenSize");
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(size));
    }

    private static async Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(IConfiguration config,
        Guid userId,
        string email, List<string> roles, AppDbContext db)
    {
        var accessToken = GenerateToken(config, userId.ToString(), email, roles);
        var refreshToken = GenerateRefreshToken(config);

        if (accessToken is null || refreshToken is null)
            return (null, null);
        
        var expiryDays = config.GetValue<int>("Jwt:RefreshTokenExpiryDays");

        db.RefreshTokens.Add(new RefreshTokenModel
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