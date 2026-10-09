using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ClearLend.Application.Behaviors;

namespace ClearLend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddClearLendApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }
}
