using EventManagement.DTOs;
using FluentValidation;

namespace EventManagement.Validators;

public class UserRegisterValidator : AbstractValidator<UserRegister>
{
    public UserRegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress();
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8);
    }
}