using EventManagement.Data;
using EventManagement.DTOs.Event;
using EventManagement.DTOs.Ticket;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Repositories;

public class EventRepository(AppDbContext db) : IEventRepository
{
    public async Task<List<EventDto>> GetAllEvents()
    {
        return await db.Events
            .Include(e => e.Tickets)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Location = e.Location,
                StartTime = e.StartTime,
                EndTime = e.EndTime,
                BannerUrl = e.BannerUrl,
                IsPublished = e.IsPublished,
                MaxAttendees = e.MaxAttendees,
                CreatedBy = e.CreatedByUser.Email,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                Type = e.EventType.Name,
                Tags = e.Tags,
                Tickets = e.Tickets.Select(t => new TicketDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    Price = t.Price,
                    Count = t.Count,
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<List<EventType>> GetAllEventTypes()
    {
        return await db.EventTypes.ToListAsync();
    }

    public async Task<Event?> GetEventById(Guid id)
    {
        return await db.Events
            .Include(e => e.Tickets)
            .Include(e => e.EventType)
            .Include(e => e.CreatedByUser)
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

    public async Task DeleteAllEvents()
    {
        var events = await db.Events.ToListAsync();
        db.Events.RemoveRange(events);
        await db.SaveChangesAsync();
    }

    public async Task<EventType?> GetEventTypeById(Guid id)
    {
        return await db.EventTypes.FindAsync(id);
    }

    public Task RemoveTickets(List<Ticket> tickets)
    {
        db.Tickets.RemoveRange(tickets);
        return Task.CompletedTask;
    }
}