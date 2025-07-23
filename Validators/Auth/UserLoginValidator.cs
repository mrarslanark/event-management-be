using EventManagement.Requests;
using FluentValidation;

namespace EventManagement.Validators.Auth;

public class UserLoginValidator : AbstractValidator<UserLoginRequest>
{
    private const string Message = "Invalid Credentials";

    public UserLoginValidator()
    {
        RuleFor(u => u.Email)
            .NotEmpty().WithMessage(Message)
            .EmailAddress().WithMessage(Message);

        RuleFor(u => u.Password)
            .NotEmpty().WithMessage(Message)
            .MinimumLength(8).WithMessage(Message);
    }
}