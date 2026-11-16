using ClearLend.Application.Abstractions;
using ClearLend.Infrastructure.Borrowers;
using ClearLend.Infrastructure.Identity;
using ClearLend.Infrastructure.Lenders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ClearLend.Infrastructure.Persistence;

public static class PersistenceDependencyInjection
{
    public static IServiceCollection AddClearLendPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClearLend")
            ?? throw new InvalidOperationException("The ClearLend database connection string is not configured.");

        services.AddDbContext<ClearLendDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
            if (configuration["ASPNETCORE_ENVIRONMENT"] == "Development")
                options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
        });
        services.AddIdentityCore<ClearLendIdentityUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 4;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
        })
            .AddEntityFrameworkStores<ClearLendDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IIdentityAccountService, AspNetIdentityAccountService>();
        services.AddScoped<IIdentityAuthenticator, AspNetIdentityAuthenticator>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUserAccountRepository, SqlUserAccountRepository>();
        services.AddScoped<ILenderProfileRepository, SqlLenderProfileRepository>();
        services.AddScoped<IBorrowerProfileRepository, SqlBorrowerProfileRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
