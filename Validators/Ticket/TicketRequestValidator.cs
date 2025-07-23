using EventManagement.Requests.Ticket;
using FluentValidation;

namespace EventManagement.Validators.Ticket;

public class TicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public TicketRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Ticket Name is required.")
            .MaximumLength(255).WithMessage("Ticket Name must not exceed 255 characters.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Ticket Description is required.")
            .MaximumLength(3_000).WithMessage("Ticket Description must not exceed 3,000 characters.");
        
        RuleFor(x => x.Price)
            .NotEmpty().WithMessage("Ticket Price is required.");
        
        RuleFor(x => x.Count)
            .NotEmpty().WithMessage("Ticket Count is required.");
    }
}