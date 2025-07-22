using EventManagement.Requests;
using FluentValidation;

namespace EventManagement.Validators;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        
        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(255).WithMessage("Location must not exceed 255 characters.");

        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.");
        
        RuleFor(x => x.EndTime)
            .NotEmpty().WithMessage("End time is required.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(3_000).WithMessage("Description must not exceed 3,000 characters.");
        
        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event Type ID is required.");

        RuleFor(x => x.Tickets)
            .NotEmpty().WithMessage("Tickets are required.");
        
        RuleFor(x => x.MaxAttendees)
            .NotEmpty().WithMessage("Max attendees count is required.");
    }
}