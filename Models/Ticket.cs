using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class Ticket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column(TypeName = "varchar(150)")]
    public string Name { get; init; } = string.Empty;
    
    [Column(TypeName = "varchar(3000)")]
    public string Description { get; init; } = string.Empty;
    public float Price { get; init; }
    public int Count { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    public Guid EventId { get; init; }
}