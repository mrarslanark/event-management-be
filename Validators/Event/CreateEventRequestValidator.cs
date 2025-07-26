using EventManagement.Requests.Event;
using FluentValidation;

namespace EventManagement.Validators.Event;

public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        
        RuleFor(x => x.Location)
            .MaximumLength(255).WithMessage("Location must not exceed 255 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(3_000).WithMessage("Description must not exceed 3,000 characters.");
        
        RuleFor(x => x.EventTypeId)
            .NotEmpty().WithMessage("Event Type ID is required.");
    }
}