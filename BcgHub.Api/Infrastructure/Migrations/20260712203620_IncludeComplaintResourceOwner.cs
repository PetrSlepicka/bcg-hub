using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncludeComplaintResourceOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Comments_ComplaintId", table: "Comments");
            migrationBuilder.DropIndex(name: "IX_Attachments_ComplaintId", table: "Attachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\", \"ComplaintId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\", \"ComplaintId\") = 1");

            migrationBuilder.CreateIndex(name: "IX_Comments_ComplaintId_CreatedAtUtc", table: "Comments", columns: new[] { "ComplaintId", "CreatedAtUtc" });
            migrationBuilder.CreateIndex(name: "IX_Attachments_ComplaintId_CreatedAtUtc", table: "Attachments", columns: new[] { "ComplaintId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_Comments_ComplaintId_CreatedAtUtc", table: "Comments");
            migrationBuilder.DropIndex(name: "IX_Attachments_ComplaintId_CreatedAtUtc", table: "Attachments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Comments_ExactlyOneOwner",
                table: "Comments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Attachments_ExactlyOneOwner",
                table: "Attachments",
                sql: "num_nonnulls(\"BusinessPartnerId\", \"ContactPersonId\", \"OrderId\", \"WorkflowStepId\", \"TransportQuoteId\", \"CommunicationId\", \"EmailMessageId\") = 1");

            migrationBuilder.CreateIndex(name: "IX_Comments_ComplaintId", table: "Comments", column: "ComplaintId");
            migrationBuilder.CreateIndex(name: "IX_Attachments_ComplaintId", table: "Attachments", column: "ComplaintId");
        }
    }
}
