using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearLend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLenderProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LenderProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LegalBusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BusinessRegistrationNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContactGivenName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactFamilyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ConsentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LatestConsent_Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LatestConsent_PolicyVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LatestConsent_RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LenderProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LenderProfiles_UserAccountId",
                table: "LenderProfiles",
                column: "UserAccountId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LenderProfiles");
        }
    }
}
