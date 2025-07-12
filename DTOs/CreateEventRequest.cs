namespace EventManagement.DTOs;

public class CreateEventRequest
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public string Description { get; set; } = string.Empty;
    public Guid EventTypeId { get; set; }

    public List<TicketRequest> Tickets { get; set; } = new();

    public bool IsPublished { get; set; } = false;
    public string? BannerUrl { get; set; } = string.Empty;
    public int? MaxAttendees { get; set; }
    public List<string> Tags { get; set; } = [];
}