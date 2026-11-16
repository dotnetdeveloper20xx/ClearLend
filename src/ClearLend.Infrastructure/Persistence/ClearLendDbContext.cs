using ClearLend.Domain.Borrowers;
using ClearLend.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using ClearLend.Infrastructure.Identity;

namespace ClearLend.Infrastructure.Persistence;

public sealed class ClearLendDbContext(DbContextOptions<ClearLendDbContext> options) : IdentityDbContext<ClearLendIdentityUser>(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<BorrowerProfile> BorrowerProfiles => Set<BorrowerProfile>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("UserAccounts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(x => x.Value, value => UserAccountId.From(value).Value);
            entity.Property(x => x.IdentityProviderSubject).HasConversion(x => x.Value, value => IdentityProviderSubject.Create(value).Value!);
            entity.Property(x => x.EmailAddress).HasConversion(x => x.Value, value => EmailAddress.Create(value).Value!);
            entity.Property(x => x.Status).HasConversion<string>();
            entity.HasIndex(x => x.EmailAddress).IsUnique();
            entity.HasIndex(x => x.IdentityProviderSubject).IsUnique();
        });

        builder.Entity<BorrowerProfile>(entity =>
        {
            entity.ToTable("BorrowerProfiles");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasConversion(x => x.Value, value => BorrowerProfileId.From(value).Value);
            entity.Property(x => x.UserAccountId).HasConversion(x => x.Value, value => UserAccountId.From(value).Value);
            entity.Property(x => x.State).HasConversion<string>();
            entity.HasIndex(x => x.UserAccountId).IsUnique();
            entity.OwnsOne(x => x.Name, name =>
            {
                name.Property(x => x!.GivenName).HasColumnName("GivenName");
                name.Property(x => x!.FamilyName).HasColumnName("FamilyName");
            });
            entity.OwnsOne(x => x.LatestConsent, consent =>
            {
                consent.Property(x => x.Id).HasColumnName("ConsentId");
                consent.Property(x => x.Status).HasConversion<string>();
                consent.Property(x => x.PolicyVersion).HasMaxLength(100);
                consent.Property(x => x.RecordedAt);
            });
            entity.Ignore(x => x.ConsentHistory);
        });
    }
}
