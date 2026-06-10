using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PurchaseOrderTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DefaultScope = table.Column<int>(type: "int", nullable: false),
                    DefaultStatus = table.Column<int>(type: "int", nullable: false),
                    DefaultSupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DefaultPaymentTerm = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DefaultCurrency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    DefaultVatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTemplates_Suppliers_DefaultSupplierId",
                        column: x => x.DefaultSupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderTemplateLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    QuantityText = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Quality = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OrderTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderTemplateLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTemplateLines_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderTemplateLines_PurchaseOrderTemplates_PurchaseOrderTemplateId",
                        column: x => x.PurchaseOrderTemplateId,
                        principalTable: "PurchaseOrderTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTemplateLines_MaterialId",
                table: "PurchaseOrderTemplateLines",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTemplateLines_PurchaseOrderTemplateId_SortOrder",
                table: "PurchaseOrderTemplateLines",
                columns: new[] { "PurchaseOrderTemplateId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTemplates_DefaultSupplierId",
                table: "PurchaseOrderTemplates",
                column: "DefaultSupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderTemplates_Name",
                table: "PurchaseOrderTemplates",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderTemplateLines");

            migrationBuilder.DropTable(
                name: "PurchaseOrderTemplates");
        }
    }
}
