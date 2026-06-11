using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderTemplateLineSuppliersAndApply : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpectedArrivalOffsetDays",
                table: "PurchaseOrderTemplateLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupplierId",
                table: "PurchaseOrderTemplateLines",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTemplateLines_SupplierId",
                table: "PurchaseOrderTemplateLines",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderTemplateLines_Suppliers_SupplierId",
                table: "PurchaseOrderTemplateLines",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderTemplateLines_Suppliers_SupplierId",
                table: "PurchaseOrderTemplateLines");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderTemplateLines_SupplierId",
                table: "PurchaseOrderTemplateLines");

            migrationBuilder.DropColumn(
                name: "ExpectedArrivalOffsetDays",
                table: "PurchaseOrderTemplateLines");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "PurchaseOrderTemplateLines");
        }
    }
}
