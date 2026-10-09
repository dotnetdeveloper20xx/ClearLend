using FluentValidation;
using ClearLend.Domain.Borrowers;

namespace ClearLend.Application.Borrowers.Register;

public sealed class RegisterBorrowerCommandValidator : AbstractValidator<RegisterBorrowerCommand>
{
    public RegisterBorrowerCommandValidator()
    {
        RuleFor(x => x.IdentityProviderSubject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.GivenName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.FamilyName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConsentPolicyVersion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ConsentStatus)
            .Must(status => status is ConsentStatus.Granted or ConsentStatus.Declined)
            .WithMessage("Registration consent must be granted or declined.");
    }
}
