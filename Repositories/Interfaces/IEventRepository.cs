using EventManagement.Models;

namespace EventManagement.Repositories.Interfaces;

public interface IEventRepository
{
    Task<List<Event>> GetAllEvents();
    Task<Event?> GetEventById(Guid id);
    Task CreateEvent(Event ev);
    Task UpdateEvent(Event ev);
    Task DeleteEvent(Event id);
    Task DeleteAllEvents(List<Event> events);
    Task<EventType?> GetEventTypeById(Guid id);
    Task RemoveTickets(List<Ticket> tickets);
}