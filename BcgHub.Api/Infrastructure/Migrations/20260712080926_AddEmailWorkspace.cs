using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments");

            migrationBuilder.AddColumn<Guid>(
                name: "EmailMessageId",
                table: "Comments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EmailMessageId",
                table: "Attachments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Comments_EmailMessageId_CreatedAtUtc",
                table: "Comments",
                columns: new[] { "EmailMessageId", "CreatedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");

            migrationBuilder.CreateIndex(
                name: "IX_Attachments_EmailMessageId_CreatedAtUtc",
                table: "Attachments",
                columns: new[] { "EmailMessageId", "CreatedAtUtc" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_Attachments_EmailMessages_EmailMessageId",
                table: "Attachments",
                column: "EmailMessageId",
                principalTable: "EmailMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_EmailMessages_EmailMessageId",
                table: "Comments",
                column: "EmailMessageId",
                principalTable: "EmailMessages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attachments_EmailMessages_EmailMessageId",
                table: "Attachments");

            migrationBuilder.DropForeignKey(
                name: "FK_Comments_EmailMessages_EmailMessageId",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Comments_EmailMessageId_CreatedAtUtc",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments");

            migrationBuilder.DropIndex(
                name: "IX_Attachments_EmailMessageId_CreatedAtUtc",
                table: "Attachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "EmailMessageId",
                table: "Attachments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\") = 1");
        }
    }
}
