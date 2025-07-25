using EventManagement.Data;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repositories;

public class EventRepository(AppDbContext db) : IEventRepository
{
    public async Task<List<Event>> GetAllEvents()
    {
        return await db.Events
            .Include(e => e.Tickets)
            .Include(e => e.EventType)
            .ToListAsync();
    }

    public async Task<Event?> GetEventById(Guid id)
    {
        return await db.Events
            .Include(e => e.Tickets)
            .Include(e => e.EventType)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task CreateEvent(Event ev)
    {
        db.Events.Add(ev);
        await db.SaveChangesAsync();
    }

    public async Task UpdateEvent(Event ev)
    {
        db.Events.Update(ev);
        await db.SaveChangesAsync();
    }

    public async Task DeleteEvent(Event ev)
    {
        db.Events.Remove(ev);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAllEvents(List<Event> events)
    {
        db.Events.RemoveRange(events);
        await db.SaveChangesAsync();
    }
}