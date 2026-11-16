using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ClearLend.Domain.Common;
using ClearLend.Application.Behaviors;
using ClearLend.Application.Borrowers.Profile;
using ClearLend.Application.Borrowers.Register;
using ClearLend.Application.Identity.Login;
using ClearLend.Application.Identity.Register;

namespace ClearLend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddClearLendApplication(this IServiceCollection services)
    {
        // Only the current borrower registration/login/profile slice is composed here.
        // Future operations remain in the solution but are not activated until their repositories exist.
        services.AddMediatR(_ => { });
        services.AddTransient<IRequestHandler<RegisterUserCommand, DomainResult<RegisterUserResult>>, RegisterUserHandler>();
        services.AddTransient<IRequestHandler<RegisterBorrowerCommand, DomainResult<RegisterBorrowerResult>>, RegisterBorrowerHandler>();
        services.AddTransient<IRequestHandler<LoginCommand, DomainResult<LoginResult>>, LoginHandler>();
        services.AddTransient<IRequestHandler<GetMyBorrowerProfileQuery, DomainResult<BorrowerProfileResult>>, GetMyBorrowerProfileHandler>();
        services.AddValidatorsFromAssemblyContaining<RegisterUserCommandValidator>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        return services;
    }
}
