using System.Security.Claims;
using Carter;
using EventManagement.Data;
using EventManagement.Helpers;
using EventManagement.Models.Event;
using EventManagement.Models.Ticket;
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
        return ApiResponse.Success(events);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> CreateEvent(CreateEventRequest request,
        IValidator<CreateEventRequest> eventValidator, IValidator<CreateTicketRequest> ticketValidator,
        AppDbContext db, HttpContext http)
    {
        var eventValidation = await eventValidator.ValidateAsync(request);
        if (!eventValidation.IsValid)
            throw new ValidationException(eventValidation.Errors);

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            throw new UnauthorizedAccessException("Invalid User");

        var eventType = await db.EventTypes.FindAsync(request.EventTypeId);
        if (eventType is null)
            throw new ArgumentException("Invalid Event Type ID.");

        // Validate every ticket
        foreach (var ticket in request.Tickets)
        {
            var ticketValidation = await ticketValidator.ValidateAsync(ticket);
            if (!ticketValidation.IsValid)
                throw new ValidationException(ticketValidation.Errors);
        }

        var eventEntity = new EventModel
        {
            Name = request.Name,
            Location = request.Location,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Description = request.Description,
            EventTypeModelId = request.EventTypeId,
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

        var data = new
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
        };
        return ApiResponse.Created($"/events/{eventEntity.Id}", data);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> UpdateEvent(Guid id, PatchEventRequest request,
        IValidator<PatchEventRequest> validator, AppDbContext db,
        HttpContext http)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            throw new UnauthorizedAccessException("Invalid User");

        var eventEntity = await db.Events
            .Include(e => e.Tickets)
            .Include(e => e.EventTypeModel)
            .Include(e => e.CreatedByUserModel)
            .FirstOrDefaultAsync(e => e.Id == id);
        if (eventEntity is null)
            throw new KeyNotFoundException("Event not found");

        if (!http.User.IsInRole("Admin") && eventEntity.CreatedByUserId.ToString() != userId)
            throw new UnauthorizedAccessException("Unauthorized Action");
        
        if (request.Name is not null) eventEntity.Name = request.Name;
        if (request.Location is not null) eventEntity.Location = request.Location;
        if (request.StartTime.HasValue) eventEntity.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) eventEntity.EndTime = request.EndTime.Value;
        if (request.Description is not null) eventEntity.Description = request.Description;
        if (request.EventTypeId.HasValue) eventEntity.EventTypeModelId = request.EventTypeId.Value;
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

        var data = new
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
        };
        return ApiResponse.Success(data);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> DeleteEvent(Guid id, AppDbContext db)
    {
        var existingEvent = await db.Events.FindAsync(id);
        if (existingEvent is null)
            throw new KeyNotFoundException($"Event with ID {id} not found.");

        var eventName = existingEvent.Name;

        db.Events.Remove(existingEvent);
        await db.SaveChangesAsync();

        return ApiResponse.Success(null, $"The {eventName} was deleted.");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> DeleteAllEvents(AppDbContext db)
    {
        var events = await db.Events.ToListAsync();
        if (events.Count == 0)
            throw new KeyNotFoundException("No events found");

        db.Events.RemoveRange(events);
        await db.SaveChangesAsync();

        return ApiResponse.Success(null, $"All {events.Count} events have been deleted.");
    }
}