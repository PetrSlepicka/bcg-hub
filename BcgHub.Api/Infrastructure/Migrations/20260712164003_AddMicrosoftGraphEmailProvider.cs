using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMicrosoftGraphEmailProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MicrosoftDeltaLink",
                table: "EmailAccountSettings",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MicrosoftMailboxAddress",
                table: "EmailAccountSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProtectedMicrosoftRefreshToken",
                table: "EmailAccountSettings",
                type: "character varying(8000)",
                maxLength: 8000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Provider",
                table: "EmailAccountSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MicrosoftDeltaLink",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "MicrosoftMailboxAddress",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "ProtectedMicrosoftRefreshToken",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "Provider",
                table: "EmailAccountSettings");
        }
    }
}
