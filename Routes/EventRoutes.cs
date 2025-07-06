using EventManagement.Models;

namespace EventManagement.Routes;

public static class EventRoutes
{
    private static readonly List<Event> Events =
    [
        new()
        {
            Name = "Tech Summit", Location = "Dubai", Date = DateTime.UtcNow.AddMonths(1), PricePerPerson = 199.99M,
            Description = "Description"
        },

        new()
        {
            
            Name = "Startup Expo", Location = "Berlin", Date = DateTime.UtcNow.AddMonths(2), PricePerPerson = 149.99M,
            Description = "Description"
        }
    ];

    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/events", () => Results.Ok(Events)).WithName("GetEvents");

        app.MapPost("/events", (Event newEvent) =>
            {
                newEvent.Id = Guid.NewGuid();
                newEvent.CreatedAt = DateTime.UtcNow;
                newEvent.UpdatedAt = DateTime.UtcNow;
                Events.Add(newEvent);
                return Results.Created($"/events/{newEvent.Id}", newEvent);
            })
            .WithName("PostEvent");
    }
}