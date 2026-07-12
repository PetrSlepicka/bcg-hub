using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BcgHub.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPohodaOrderImport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PohodaOrderId",
                table: "Orders",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PohodaOrderId",
                table: "Orders",
                column: "PohodaOrderId",
                unique: true,
                filter: "\"PohodaOrderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_PohodaOrderId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PohodaOrderId",
                table: "Orders");
        }
    }
}
