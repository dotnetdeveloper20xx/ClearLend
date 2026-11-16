using ClearLend.Application.Abstractions;
using ClearLend.Infrastructure.Borrowers;
using ClearLend.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ClearLend.Infrastructure.Persistence;

public static class PersistenceDependencyInjection
{
    public static IServiceCollection AddClearLendPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClearLend")
            ?? throw new InvalidOperationException("The ClearLend database connection string is not configured.");

        services.AddDbContext<ClearLendDbContext>(options => options.UseSqlServer(connectionString));
        services.AddIdentityCore<ClearLendIdentityUser>(options =>
            options.User.RequireUniqueEmail = true)
            .AddEntityFrameworkStores<ClearLendDbContext>()
            .AddDefaultTokenProviders();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IIdentityAccountService, AspNetIdentityAccountService>();
        services.AddScoped<IIdentityAuthenticator, AspNetIdentityAuthenticator>();
        services.AddScoped<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddScoped<IUserAccountRepository, SqlUserAccountRepository>();
        services.AddScoped<IBorrowerProfileRepository, SqlBorrowerProfileRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        return services;
    }
}
