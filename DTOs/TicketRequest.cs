using System.ComponentModel.DataAnnotations;

namespace EventManagement.DTOs;

public class TicketRequest
{
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(3000)]
    public string Description { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Count { get; set; }
}