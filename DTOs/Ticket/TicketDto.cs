namespace EventManagement.DTOs.Ticket;

public class TicketDto
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Count { get; set; }
}