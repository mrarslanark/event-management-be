using EventManagement.Models;

namespace EventManagement.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (db.Events.Any()) return;
        db.Events.AddRange(
            new Event
            {
                Name = "Tech Expo",
                Location = "Dubai",
                Date = DateTime.UtcNow.AddMonths(1),
                PricePerPerson = 199.99M,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Event
            {
                Name = "Startup Summit",
                Location = "Berlin",
                Date = DateTime.UtcNow.AddMonths(2),
                PricePerPerson = 149.99M,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        db.SaveChanges();
    }
}