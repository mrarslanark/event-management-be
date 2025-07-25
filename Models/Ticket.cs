namespace EventManagement.Models;

public class Ticket
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;       // e.g. VIP, Regular
    public string Description { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Count { get; set; }

    public Guid EventId { get; set; }
}