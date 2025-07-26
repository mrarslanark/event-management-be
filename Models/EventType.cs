using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models;

public class EventType
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    [Column(TypeName = "varchar(255)")]
    public string Name { get; init; } = string.Empty;
    
    public ICollection<Event> Events { get; init; } = new List<Event>();
}