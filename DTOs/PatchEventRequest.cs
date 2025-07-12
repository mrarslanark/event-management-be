namespace EventManagement.DTOs;

public class PatchEventRequest
{
    public string? Name { get; set; }
    public string? Location { get; set; }

    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public string? Description { get; set; }
    public Guid? EventTypeId { get; set; }

    public bool? IsPublished { get; set; }
    public string? BannerUrl { get; set; }
    public int? MaxAttendees { get; set; }
    public List<string>? Tags { get; set; }

    public List<TicketRequest>? Tickets { get; set; }
}