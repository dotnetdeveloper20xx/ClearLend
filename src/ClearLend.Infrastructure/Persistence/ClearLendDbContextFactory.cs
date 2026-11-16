using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClearLend.Infrastructure.Persistence;

// Keeps EF migration generation independent from runtime-only application services.
public sealed class ClearLendDbContextFactory : IDesignTimeDbContextFactory<ClearLendDbContext>
{
    public ClearLendDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ClearLendDbContext>()
            .UseSqlServer("Server=.;Database=ClearLend;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True")
            .Options;

        return new ClearLendDbContext(options);
    }
}
