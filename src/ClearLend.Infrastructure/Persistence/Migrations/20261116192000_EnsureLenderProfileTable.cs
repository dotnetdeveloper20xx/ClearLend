using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearLend.Infrastructure.Persistence.Migrations;

public partial class EnsureLenderProfileTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
IF OBJECT_ID(N'[LenderProfiles]', N'U') IS NULL
BEGIN
    CREATE TABLE [LenderProfiles] (
        [Id] uniqueidentifier NOT NULL,
        [UserAccountId] uniqueidentifier NOT NULL,
        [LegalBusinessName] nvarchar(200) NOT NULL,
        [TradingName] nvarchar(200) NULL,
        [BusinessRegistrationNumber] nvarchar(100) NULL,
        [ContactGivenName] nvarchar(max) NOT NULL,
        [ContactFamilyName] nvarchar(max) NOT NULL,
        [State] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [ConsentId] uniqueidentifier NOT NULL,
        [LatestConsent_Status] nvarchar(max) NOT NULL,
        [LatestConsent_PolicyVersion] nvarchar(100) NOT NULL,
        [LatestConsent_RecordedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_LenderProfiles] PRIMARY KEY ([Id])
    );
END
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LenderProfiles_UserAccountId' AND object_id = OBJECT_ID(N'[LenderProfiles]'))
    CREATE UNIQUE INDEX [IX_LenderProfiles_UserAccountId] ON [LenderProfiles] ([UserAccountId]);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("IF OBJECT_ID(N'[LenderProfiles]', N'U') IS NOT NULL DROP TABLE [LenderProfiles];");
    }
}
