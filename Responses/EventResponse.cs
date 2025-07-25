using System.ComponentModel.DataAnnotations;
using EventManagement.Models;

namespace EventManagement.Responses;

public class EventResponse
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(255)]
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    

    // 🆕 Tickets (1:N)
    public List<Ticket> Tickets { get; set; } = new();

    // 🆕 Optional Fields
    public Guid CreatedByUserId { get; init; }

    public bool IsPublished { get; set; }

    [MaxLength(2083)]
    public string? BannerUrl { get; set; }

    public int? MaxAttendees { get; set; }  // Optional limit

    public List<string> Tags { get; set; } = []; // stored as JSON

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public EvenTypeDto EventType { get; set; }

    public class EvenTypeDto
    {
        public Guid Id { get; set; }
    }
}