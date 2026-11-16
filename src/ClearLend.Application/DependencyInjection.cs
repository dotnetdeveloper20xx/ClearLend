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
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        return services;
    }
}
