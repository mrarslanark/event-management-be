using EventManagement.Data;
using EventManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Routes;

public static class EventRoutes
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events", async (AppDbContext db) =>
            {
                var events = await db.Events.ToListAsync();
                return Results.Ok(events);
            })
            .RequireAuthorization()
            .WithName("GetEvents");

        app.MapPost("/events", async (AppDbContext db, Event newEvent) =>
            {
                newEvent.Id = Guid.NewGuid();
                newEvent.CreatedAt = DateTime.UtcNow;
                newEvent.UpdatedAt = DateTime.UtcNow;

                db.Events.Add(newEvent);
                await db.SaveChangesAsync();

                return Results.Created($"/events/{newEvent.Id}", newEvent);
            })
            .WithName("PostEvent");

        app.MapPut("/events/{id:guid}", async (Guid id, Event updatedEvent, AppDbContext db) =>
        {
            var existingEvent = await db.Events.FindAsync(id);
            if (existingEvent is null)
            {
                return Results.NotFound(new { message = $"Event with ID {id} not found." });
            }

            existingEvent.Name = updatedEvent.Name;
            existingEvent.Description = updatedEvent.Description;
            existingEvent.Date = updatedEvent.Date;
            existingEvent.PricePerPerson = updatedEvent.PricePerPerson;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return Results.Ok(existingEvent);
        }).WithName("PutEvent");

        app.MapDelete("/events/{id:guid}", async (Guid id, AppDbContext db) =>
        {
            var existingEvent = await db.Events.FindAsync(id);
            if (existingEvent is null)
            {
                return Results.NotFound(new { message = $"Event with ID {id} not found." });
            }

            var eventName = existingEvent.Name;

            db.Events.Remove(existingEvent);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = $"The {eventName} was deleted." });
        }).WithName("DeleteEvent");

        app.MapDelete("/events", async (AppDbContext db) =>
        {
            var events = await db.Events.ToListAsync();
            if (events.Count == 0)
            {
                return Results.NotFound(new { message = "No events found" });
            }

            db.Events.RemoveRange(events);
            await db.SaveChangesAsync();

            return Results.Ok(new { message = $"All {events.Count} events have been deleted." });
        }).WithName("DeleteEvents");
    }
}