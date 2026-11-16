using FluentValidation;

namespace ClearLend.Application.Lenders.Register;

public sealed class RegisterLenderCommandValidator : AbstractValidator<RegisterLenderCommand>
{
    public RegisterLenderCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(4);
        RuleFor(x => x.LegalBusinessName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactGivenName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactFamilyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConsentPolicyVersion).NotEmpty().MaximumLength(100);
    }
}
