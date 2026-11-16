using FluentValidation;

namespace ClearLend.Application.Identity.Register;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320);
    }
}
