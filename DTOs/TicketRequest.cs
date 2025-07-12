namespace EventManagement.DTOs;

public class TicketRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Count { get; set; }
}