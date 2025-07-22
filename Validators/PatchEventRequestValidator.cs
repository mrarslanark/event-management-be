using EventManagement.Requests;
using FluentValidation;

namespace EventManagement.Validators;

public class PatchEventRequestValidator : AbstractValidator<PatchEventRequest>
{
    public PatchEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(255).WithMessage("Name must not exceed 255 characters.");
        
        RuleFor(x => x.Location)
            .MaximumLength(255).WithMessage("Location must not exceed 255 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(3_000).WithMessage("Description must not exceed 3,000 characters.");
    }
}