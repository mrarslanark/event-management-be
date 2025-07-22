using System.Text.Json;
using EventManagement.Models;
using EventManagement.Models.Event;
using EventManagement.Models.Ticket;
using EventManagement.Models.User;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<EventModel> Events => Set<EventModel>();
    public DbSet<TicketModel> Tickets => Set<TicketModel>();
    public DbSet<EventTypeModel> EventTypes => Set<EventTypeModel>();
    public DbSet<UserModel> Users => Set<UserModel>();
    public DbSet<RoleModel> Roles => Set<RoleModel>();
    public DbSet<UserRoleModel> UserRoles => Set<UserRoleModel>();
    public DbSet<RefreshTokenModel> RefreshTokens => Set<RefreshTokenModel>();
    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleModel>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRoleModel>()
            .HasOne(ur => ur.UserModel)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        modelBuilder.Entity<UserRoleModel>()
            .HasOne(ur => ur.RoleModel)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        modelBuilder.Entity<EventModel>()
            .HasOne(e => e.CreatedByUserModel)
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<EventModel>()
            .Property(e => e.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, new JsonSerializerOptions()),
                v => JsonSerializer.Deserialize<List<string>>(v, new JsonSerializerOptions()) ?? new List<string>()
            );
        
        modelBuilder.Entity<RefreshTokenModel>()
            .HasIndex(rt => rt.Token)
            .IsUnique();
    }
}