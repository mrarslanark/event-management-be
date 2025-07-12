namespace EventManagement.Models;

public class EventType
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Event> Events { get; set; } = new List<Event>();
}