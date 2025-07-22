using System.Security.Claims;
using Carter;
using EventManagement.Data;
using EventManagement.Models;
using EventManagement.Models.Event;
using EventManagement.Models.Ticket;
using EventManagement.Requests;
using EventManagement.Requests.Event;
using EventManagement.Requests.Ticket;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Modules;

public class EventModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/events", GetAllEvents);
        app.MapPost("/events", CreateEvent);
        app.MapPatch("/events/{id:guid}", UpdateEvent);
        app.MapDelete("/events/{id:guid}", DeleteEvent);
        app.MapDelete("/events", DeleteAllEvents);
    }

    private static async Task<IResult> GetAllEvents(AppDbContext db)
    {
        var events = await db.Events.ToListAsync();
        return Results.Ok(events);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> CreateEvent(CreateEventRequest request,
        IValidator<CreateEventRequest> eventValidator, IValidator<CreateTicketRequest> ticketValidator,
        AppDbContext db, HttpContext http)
    {
        var eventValidation = await eventValidator.ValidateAsync(request);
        if (!eventValidation.IsValid)
            return Results.ValidationProblem(eventValidation.ToDictionary());

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        var eventType = await db.EventTypes.FindAsync(request.EventTypeId);
        if (eventType is null)
            return Results.BadRequest(new { message = "Invalid EventModel Type ID." });

        // Validate every ticket
        foreach (var ticket in request.Tickets)
        {
            var ticketValidation = await ticketValidator.ValidateAsync(ticket);
            if (!ticketValidation.IsValid)
                return Results.ValidationProblem(ticketValidation.ToDictionary());
        }

        var eventEntity = new EventModel()
        {
            Name = request.Name,
            Location = request.Location,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Description = request.Description,
            EventTypeId = request.EventTypeId,
            IsPublished = request.IsPublished,
            BannerUrl = request.BannerUrl,
            MaxAttendees = request.MaxAttendees,
            Tags = request.Tags,
            CreatedByUserId = Guid.Parse(userId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tickets = request.Tickets.Select(t => new TicketModel
            {
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                Count = t.Count
            }).ToList()
        };
        db.Events.Add(eventEntity);
        await db.SaveChangesAsync();

        return Results.Created($"/events/{eventEntity.Id}", new
        {
            id = eventEntity.Id,
            request.Name,
            request.Location,
            request.StartTime,
            request.EndTime,
            request.Description,
            EventType = eventType.Name,
            request.IsPublished,
            request.BannerUrl,
            request.MaxAttendees,
            request.Tags,
            CreatedByUserId = Guid.Parse(userId).ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tickets = request.Tickets.Select(t => new TicketModel
            {
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                Count = t.Count
            }).ToList()
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> UpdateEvent(Guid id, PatchEventRequest request,
        IValidator<PatchEventRequest> validator, AppDbContext db,
        HttpContext http)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return Results.ValidationProblem(validation.ToDictionary());

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            return Results.Unauthorized();

        var eventEntity = await db.Events
            .Include(e => e.Tickets)
            .Include(e => e.EventTypeModel)
            .Include(e => e.CreatedByUserModel)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (eventEntity is null)
            return Results.NotFound(new { message = "EventModel not found." });

        if (!http.User.IsInRole("Admin") && eventEntity.CreatedByUserId.ToString() != userId)
            return Results.Forbid();

        if (request.Name is not null) eventEntity.Name = request.Name;
        if (request.Location is not null) eventEntity.Location = request.Location;
        if (request.StartTime.HasValue) eventEntity.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) eventEntity.EndTime = request.EndTime.Value;
        if (request.Description is not null) eventEntity.Description = request.Description;
        if (request.EventTypeId.HasValue) eventEntity.EventTypeId = request.EventTypeId.Value;
        if (request.IsPublished.HasValue) eventEntity.IsPublished = request.IsPublished.Value;
        if (request.BannerUrl is not null) eventEntity.BannerUrl = request.BannerUrl;
        if (request.MaxAttendees.HasValue) eventEntity.MaxAttendees = request.MaxAttendees;
        if (request.Tags is not null) eventEntity.Tags = request.Tags;

        if (request.Tickets is not null)
        {
            db.Tickets.RemoveRange(eventEntity.Tickets);
            eventEntity.Tickets = request.Tickets.Select(t => new TicketModel
            {
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                Count = t.Count,
                EventId = eventEntity.Id
            }).ToList();
        }

        eventEntity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            eventEntity.Id,
            eventEntity.Name,
            eventEntity.Location,
            eventEntity.Description,
            eventEntity.StartTime,
            eventEntity.EndTime,
            eventEntity.IsPublished,
            eventEntity.BannerUrl,
            eventEntity.MaxAttendees,
            eventEntity.Tags,
            eventType = eventEntity.EventTypeModel.Name,
            createdBy = eventEntity.CreatedByUserModel.Email,
            tickets = eventEntity.Tickets.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Price,
                t.Count
            }),
            eventEntity.UpdatedAt
        });
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> DeleteEvent(Guid id, AppDbContext db)
    {
        var existingEvent = await db.Events.FindAsync(id);
        if (existingEvent is null)
            return Results.NotFound(new { message = $"EventModel with ID {id} not found." });

        var eventName = existingEvent.Name;

        db.Events.Remove(existingEvent);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"The {eventName} was deleted." });
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> DeleteAllEvents(AppDbContext db)
    {
        var events = await db.Events.ToListAsync();
        if (events.Count == 0)
            return Results.NotFound(new { message = "No events found" });

        db.Events.RemoveRange(events);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = $"All {events.Count} events have been deleted." });
    }
}