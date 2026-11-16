using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClearLend.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMembership : Migration
    {
        private static readonly string[] StaffMembershipAccountRoleIndexColumns = ["UserAccountId", "Role"];
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "MustChangePassword",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StaffMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DisabledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffMemberships_UserAccountId_Role",
                table: "StaffMemberships",
                columns: StaffMembershipAccountRoleIndexColumns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffMemberships");

            migrationBuilder.DropColumn(
                name: "MustChangePassword",
                table: "AspNetUsers");
        }
    }
}
