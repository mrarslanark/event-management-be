using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class Event
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Column(TypeName = "varchar(150)")]
    public string Name { get; set; } = string.Empty; // Required
    
    [Column(TypeName = "varchar(255)")]
    public string Location { get; set; } = string.Empty; // Optional - Only required when event is going live

    public DateTime StartTime { get; set; } // Optional - Only required when event is going live
    public DateTime EndTime { get; set; } // Optional - Only required when event is going live

    [Column(TypeName = "varchar(2000)")]
    public string Description { get; set; } = string.Empty; // Optional - Only required when event is going live

    // 🆕 Event Type (Genre)
    public Guid EventTypeId { get; set; } // Required
    public EventType EventType { get; init; } = null!; // Required

    // 🆕 Tickets (1:N)
    public List<Ticket> Tickets { get; set; } = new(); // Optional - Only required when event is going live
    
    public Guid CreatedByUserId { get; init; } // Required
    public User CreatedByUser { get; init; } = null!; // Required

    public bool IsPublished { get; set; } // Required

    [Column(TypeName = "varchar(2083)")]
    public string? BannerUrl { get; set; }  // Optional

    public int? MaxAttendees { get; set; }  // Optional

    public List<string> Tags { get; set; } = []; // Stored as JSON - Optional

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow; // Required
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Required
}