using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Carter;
using EventManagement.Exceptions;
using EventManagement.Helpers;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using EventManagement.Requests;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        IPasswordHasher<User> hasher,
        IAuthRepository repo)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // Find user from the database
        var user = await repo.GetUserByEmail(request.Email);
        if (user is null)
            throw new ArgumentException("Invalid Credentials");

        // Validate user password
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result is not PasswordVerificationResult.Success)
            throw new UnauthorizedAccessException("Invalid Credentials");

        // Get roles and generate access and refresh token
        var roles = await repo.GetUserRoles(user.Id);
        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, repo);
        if (accessToken is null || refreshToken is null)
            throw new UnauthorizedAccessException("Invalid Credentials");

        var data = new
        {
            id = user.Id,
            token = accessToken,
            refreshToken,
            email = user.Email,
            roles,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        };
        return ApiResponse.Success(data);
    }

    private static async Task<IResult> Register(
        IConfiguration config, 
        UserRegisterRequest request,
        IValidator<UserRegisterRequest> validator,
        IPasswordHasher<User> hasher,
        IAuthRepository repo)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existingUser = await repo.GetUserByEmail(request.Email);
        if (existingUser is not null)
            throw new ConflictException("User already exists, Login");

        var emailToken = Guid.NewGuid().ToString();

        var user = new User
        {
            Email = request.Email,
            PasswordHash = hasher.HashPassword(null!, request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsEmailVerified = false,
            EmailVerificationToken = emailToken,
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        await repo.AddUser(user);

        // TODO: Send to email through third party service
        Console.WriteLine($"Simulated verification token: {emailToken}");

        var role = await repo.GetRoleByName("User");
        if (role is null)
            throw new ArgumentException("Default role 'User' not found in database.");

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        await repo.AssignRole(userRole);

        var roles = await repo.GetUserRoles(user.Id);

        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, repo);
        if (accessToken is null || refreshToken is null)
            throw new UnauthorizedAccessException("Invalid Credentials");

        var data = new
        {
            id = user.Id,
            token = accessToken,
            refreshToken,
            email = user.Email,
            roles,
            createdAt = user.CreatedAt,
            updatedAt = user.UpdatedAt
        };
        return ApiResponse.Created($"/users/{user.Id}", data);
    }

    private static async Task<IResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        IValidator<VerifyEmailRequest> validator, 
        IAuthRepository repo)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var user = await repo.GetUserByEmailVerificationToken(request.Token);
        if (user is null)
            throw new ArgumentException("Invalid or expired verification token");

        if (user.IsEmailVerified)
            return Results.Ok(new { message = "Email is already verified" });

        await repo.VerifyEmail(user);

        return ApiResponse.Success(message: "Email Verified Successfully");
    }

    private static async Task<IResult> RefreshToken(
        IConfiguration config, 
        RefreshTokenRequest request,
        IValidator<RefreshTokenRequest> validator,
        IAuthRepository repo)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var existingToken = await repo.GetRefreshToken(request.RefreshToken);
        if (existingToken is null || existingToken.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired");

        var user = existingToken.User;
        if (user is null)
            throw new ArgumentException("Invalid refresh token.");

        // Revoke old token
        await repo.RevokeRefreshToken(existingToken);

        var roles = await repo.GetUserRoles(user.Id);
        var (accessToken, refreshToken) = await GenerateTokensAsync(config, user.Id, user.Email, roles, repo);
        if (accessToken is null || refreshToken is null)
            throw new ArgumentException("Unable to refresh token.");

        var data = new
        {
            token = accessToken,
            refreshToken
        };
        return ApiResponse.Success(data);
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

    private static string GenerateRefreshToken(IConfiguration config)
    {
        var size = config.GetValue<int>("Jwt:RefreshTokenSize");
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(size));
    }

    private static async Task<(string? accessToken, string? refreshToken)> GenerateTokensAsync(
        IConfiguration config,
        Guid userId,
        string email, 
        List<string> roles, 
        IAuthRepository repo)
    {
        var accessToken = GenerateToken(config, userId.ToString(), email, roles);
        var refreshToken = GenerateRefreshToken(config);

        if (accessToken is null)
            return (null, null);
        
        var expiryDays = config.GetValue<int>("Jwt:RefreshTokenExpiryDays");

        var token = new RefreshToken
        {
            Token = refreshToken,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
            IsRevoked = false
        };
        await repo.SaveRefreshToken(token);

        return (accessToken, refreshToken);
    }
}