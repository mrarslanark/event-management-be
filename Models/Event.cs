namespace EventManagement.Models;

public class Event
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Description { get; set; } = string.Empty;

    // 🆕 Event Type (Genre)
    public Guid EventTypeId { get; set; }
    public EventType EventType { get; set; } = default!;

    // 🆕 Tickets (1:N)
    public List<Ticket> Tickets { get; set; } = new();

    // 🆕 Optional Fields
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = default!;

    public bool IsPublished { get; set; }

    public string? BannerUrl { get; set; } = null;

    public int? MaxAttendees { get; set; }  // Optional limit

    public List<string> Tags { get; set; } = new(); // stored as JSON

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}