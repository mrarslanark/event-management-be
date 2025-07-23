using System.ComponentModel.DataAnnotations.Schema;

namespace EventManagement.Models.Event;

[Table("EventTypes")]
public class EventTypeModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<EventModel> Events { get; set; } = new List<EventModel>();
}