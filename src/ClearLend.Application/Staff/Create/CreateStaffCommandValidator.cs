using FluentValidation;

namespace ClearLend.Application.Staff.Create;

public sealed class CreateStaffCommandValidator : AbstractValidator<CreateStaffCommand>
{
    public CreateStaffCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4);
        RuleFor(x => x.Roles).NotEmpty();
        RuleForEach(x => x.Roles).IsInEnum();
    }
}
