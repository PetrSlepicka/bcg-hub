using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSmtpAndEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProtectedSmtpPassword",
                table: "EmailAccountSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenderAddress",
                table: "EmailAccountSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "EmailAccountSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SmtpPort",
                table: "EmailAccountSettings",
                type: "integer",
                nullable: false,
                defaultValue: 587);

            migrationBuilder.AddColumn<string>(
                name: "SmtpServer",
                table: "EmailAccountSettings",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SmtpUseSsl",
                table: "EmailAccountSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SmtpUsername",
                table: "EmailAccountSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ComplaintId",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ComplaintId",
                table: "Attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Complaints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Complaints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Complaints_BusinessPartners_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "BusinessPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Complaints_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Subject = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    BodyHtml = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTemplates_Users_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_ComplaintId",
                table: "Comments",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_ComplaintId",
                table: "Attachments",
                column: "ComplaintId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_CustomerId",
                table: "Complaints",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_OrderId",
                table: "Complaints",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Complaints_Status_ReportedOn",
                table: "Complaints",
                columns: new[] { "Status", "ReportedOn" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailTemplates_UserAccountId_Name",
                table: "EmailTemplates",
                columns: new[] { "UserAccountId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_Complaints_ComplaintId",
                table: "Attachments",
                column: "ComplaintId",
                principalTable: "Complaints",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Complaints_ComplaintId",
                table: "Comments",
                column: "ComplaintId",
                principalTable: "Complaints",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_Complaints_ComplaintId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Complaints_ComplaintId",
                table: "Comments");

            migrationBuilder.DropTable(
                name: "Complaints");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Comments_ComplaintId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_ComplaintId",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "ProtectedSmtpPassword",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SenderAddress",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SmtpPort",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SmtpServer",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUseSsl",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "SmtpUsername",
                table: "EmailAccountSettings");

            migrationBuilder.DropColumn(
                name: "ComplaintId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ComplaintId",
                table: "Attachments");
        }
    }
}
