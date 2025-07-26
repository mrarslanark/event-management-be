using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class Event
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column(TypeName = "varchar(150)")]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "varchar(255)")]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [Column(TypeName = "varchar(2000)")]
    public string Description { get; set; } = string.Empty;

    // 🆕 Event Type (Genre)
    public Guid EventTypeId { get; set; }
    public EventType EventType { get; init; } = null!;

    // 🆕 Tickets (1:N)
    public List<Ticket> Tickets { get; set; } = new();

    // 🆕 Optional Fields
    public Guid CreatedByUserId { get; init; }
    public User CreatedByUser { get; init; } = null!;

    public bool IsPublished { get; set; }

    [Column(TypeName = "varchar(2083)")]
    public string? BannerUrl { get; set; }

    public int? MaxAttendees { get; set; }  // Optional limit

    public List<string> Tags { get; set; } = []; // stored as JSON

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}