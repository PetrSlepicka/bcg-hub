using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPohodaSyncState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PohodaSyncStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastAttemptStartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAttemptCompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSuccessfulSyncUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastRunId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    LastTrigger = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastError = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    LastImportedCount = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedCount = table.Column<int>(type: "integer", nullable: false),
                    LastUnchangedCount = table.Column<int>(type: "integer", nullable: false),
                    LastWarningCount = table.Column<int>(type: "integer", nullable: false),
                    LastErrorCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PohodaSyncStates", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PohodaSyncStates");
        }
    }
}
