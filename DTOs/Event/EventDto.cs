using EventManagement.DTOs.Ticket;

namespace EventManagement.DTOs.Event;

public class EventDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? BannerUrl { get; set; }
    public bool IsPublished { get; set; }
    public int? MaxAttendees { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public List<TicketDto> Tickets { get; set; } = [];
    public string? Type { get; set; }
}