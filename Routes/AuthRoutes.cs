using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace EventManagement.Routes;

public static class AuthRoutes
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/login", (UserLogin login, IConfiguration config) =>
        {
            if (login.Username != "admin" || login.Password != "password")
                return Results.Unauthorized();
            
            var authKey = Environment.GetEnvironmentVariable("AUTH_KEY");
            var issuer = Environment.GetEnvironmentVariable("AUTH_ISSUER");
            var audience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE");
            var expiration = Environment.GetEnvironmentVariable("AUTH_EXPIRE_MINUTES");
            
            if (authKey == null || expiration == null) return Results.Unauthorized();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey));
            var signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(expiration)),
                signingCredentials: signingCredentials
            );
            
            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return Results.Ok(new { token = tokenString });
        });
    }
}

public record UserLogin(string Username, string Password);