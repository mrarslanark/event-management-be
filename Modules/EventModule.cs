using System.Security.Claims;
using Carter;
using EventManagement.Helpers;
using EventManagement.Models;
using EventManagement.Repositories.Interfaces;
using EventManagement.Requests.Event;
using EventManagement.Requests.Ticket;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;

namespace EventManagement.Modules;

public class EventModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/events", GetAllEvents);
        app.MapGet("/events/types", GetAllEventTypes);
        app.MapGet("/events/{id:guid}", GetEventById);
        app.MapPost("/events", CreateEvent);
        app.MapPatch("/events/{id:guid}", UpdateEvent);
        app.MapDelete("/events/{id:guid}", DeleteEvent);
        app.MapDelete("/events", DeleteAllEvents);
    }

    private static async Task<IResult> GetAllEvents(IEventRepository repo)
    {
        var events = await repo.GetAllEvents();
        return ApiResponse.Success(events);
    }

    private static async Task<IResult> GetEventById(IEventRepository repo, Guid id)
    {
        var eventById = await repo.GetEventById(id);
        if (eventById == null)
            throw new KeyNotFoundException("Event not found");
        return ApiResponse.Success(eventById);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> CreateEvent(
        CreateEventRequest request,
        IValidator<CreateEventRequest> eventValidator,
        IValidator<CreateTicketRequest> ticketValidator,
        HttpContext http,
        IEventRepository repo,
        IAuthRepository authRepo)
    {
        var eventValidation = await eventValidator.ValidateAsync(request);
        if (!eventValidation.IsValid)
            throw new ValidationException(eventValidation.Errors);

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            throw new UnauthorizedAccessException("Invalid User");

        // var eventType = await db.EventTypes.FindAsync(request.EventTypeId);
        var eventType = await repo.GetEventTypeById(request.EventTypeId);
        if (eventType is null)
            throw new ArgumentException("Invalid Event Type ID.");

        // Validate every ticket
        foreach (var ticket in request.Tickets)
        {
            var ticketValidation = await ticketValidator.ValidateAsync(ticket);
            if (!ticketValidation.IsValid)
                throw new ValidationException(ticketValidation.Errors);
        }

        var eventEntity = new Event
        {
            Name = request.Name,
            EventTypeId = request.EventTypeId,
            IsPublished = false,
            CreatedByUserId = Guid.Parse(userId),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await repo.CreateEvent(eventEntity);

        var typeById = await repo.GetEventTypeById(eventEntity.EventTypeId);
        var userById = await authRepo.GetUserById(eventEntity.CreatedByUserId);

        var data = new
        {
            eventEntity.Id,
            eventEntity.Name,
            Type = typeById?.Name,
            CreatedBy = userById?.Email,
            eventEntity.CreatedAt,
            eventEntity.UpdatedAt
        };
        return ApiResponse.Created($"/events/{eventEntity.Id}", data);
    }

    [Authorize(Roles = "Admin,Manager")]
    private static async Task<IResult> UpdateEvent(
        Guid id,
        PatchEventRequest request,
        IValidator<PatchEventRequest> validator,
        HttpContext http,
        IEventRepository repo)
    {
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        var userId = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null)
            throw new UnauthorizedAccessException("Invalid User");

        var eventEntity = await repo.GetEventById(id);
        if (eventEntity is null)
            throw new KeyNotFoundException("Event not found");

        if (!http.User.IsInRole("Admin") && eventEntity.CreatedByUserId.ToString() != userId)
            throw new UnauthorizedAccessException("Unauthorized Action");

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
            await repo.RemoveTickets(eventEntity.Tickets);
            eventEntity.Tickets = request.Tickets.Select(t => new Ticket
            {
                Name = t.Name,
                Description = t.Description,
                Price = t.Price,
                Count = t.Count,
                EventId = eventEntity.Id
            }).ToList();
        }

        eventEntity.UpdatedAt = DateTime.UtcNow;
        await repo.UpdateEvent(eventEntity);

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
            eventType = eventEntity.EventType.Name,
            createdBy = eventEntity.CreatedByUser.Email,
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
    private static async Task<IResult> DeleteEvent(Guid id, IEventRepository repo)
    {
        var existingEvent = await repo.GetEventById(id);
        if (existingEvent is null)
            throw new KeyNotFoundException($"Event with ID {id} not found.");

        var eventName = existingEvent.Name;

        await repo.DeleteEvent(existingEvent);

        return ApiResponse.Success(message: $"The {eventName} was deleted.");
    }

    [Authorize(Roles = "Admin")]
    private static async Task<IResult> DeleteAllEvents(IEventRepository repo)
    {
        await repo.DeleteAllEvents();
        return ApiResponse.Success(message: "All events have been deleted.");
    }

    private static async Task<IResult> GetAllEventTypes(IEventRepository repo)
    {
        var eventTypes = await repo.GetAllEventTypes();
        if (eventTypes.Count == 0)
            throw new KeyNotFoundException("No event types found");
        
        return ApiResponse.Success(eventTypes);
    }
}