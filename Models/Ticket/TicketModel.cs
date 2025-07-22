using System.ComponentModel.DataAnnotations.Schema;
using EventManagement.Models.Event;

namespace EventManagement.Models.Ticket;

[Table("Tickets")]
public class TicketModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;       // e.g. VIP, Regular
    public string Description { get; set; } = string.Empty;
    public float Price { get; set; }
    public int Count { get; set; }

    public Guid EventId { get; set; }
    public EventModel EventModel { get; set; } = default!;
}