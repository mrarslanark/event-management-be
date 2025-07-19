using EventManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Carter;
using DotNetEnv;
using EventManagement.Models;
using EventManagement.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCarter();
builder.Services.AddValidatorsFromAssemblyContaining<UserRegisterValidator>();

var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPass = Environment.GetEnvironmentVariable("DB_PASS");
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");

var authKey = Environment.GetEnvironmentVariable("AUTH_KEY");
var authIssuer = Environment.GetEnvironmentVariable("AUTH_ISSUER");
var authAudience = Environment.GetEnvironmentVariable("AUTH_AUDIENCE");

// Database Connection
var connectionString = $"server={dbHost};port={dbPort};database={dbName};user={dbUser};password={dbPass};";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Authentication
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = authIssuer,
        ValidAudience = authAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authKey))
    };

    options.Events = new JwtBearerEvents()
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Auth failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// Add services to the container.
builder.Services.AddAuthorization();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
    db.Database.Migrate();
    await DbSeeder.Seed(db, hasher);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Run();