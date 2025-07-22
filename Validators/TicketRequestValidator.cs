using EventManagement.Requests;
using EventManagement.Requests.Ticket;
using FluentValidation;

namespace EventManagement.Validators;

public class TicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public TicketRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("TicketModel Name is required.")
            .MaximumLength(255).WithMessage("TicketModel Name must not exceed 255 characters.");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("TicketModel Description is required.")
            .MaximumLength(3_000).WithMessage("TicketModel Description must not exceed 3,000 characters.");
        
        RuleFor(x => x.Price)
            .NotEmpty().WithMessage("TicketModel Price is required.");
        
        RuleFor(x => x.Count)
            .NotEmpty().WithMessage("TicketModel Count is required.");
    }
}