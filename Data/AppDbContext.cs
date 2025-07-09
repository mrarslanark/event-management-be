using EventManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();
    public DbSet<User> Users => Set<User>();
}