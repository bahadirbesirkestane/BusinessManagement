using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderRequesterUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RequestedByUserId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_RequestedByUserId",
                table: "PurchaseOrders",
                column: "RequestedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_RequestedByUserId",
                table: "PurchaseOrders",
                column: "RequestedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_AspNetUsers_RequestedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_RequestedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "RequestedByUserId",
                table: "PurchaseOrders");
        }
    }
}
