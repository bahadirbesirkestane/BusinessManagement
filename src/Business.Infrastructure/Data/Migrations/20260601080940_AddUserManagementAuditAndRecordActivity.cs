using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserManagementAuditAndRecordActivity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Suppliers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Suppliers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "StockItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "StockItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "PurchaseOrders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ProjectUpdates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ProjectTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ProjectTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "ProjectCostItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "ProjectCostItems",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Materials",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Materials",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "MaterialRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "MaterialRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Invoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Invoices",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "InvoiceLines",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "InvoiceLines",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByUserId",
                table: "Customers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedByUserId",
                table: "Customers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RecordComments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommentText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordComments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecordFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerType = table.Column<int>(type: "int", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    RelativePath = table.Column<string>(type: "nvarchar(520)", maxLength: 520, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecordFiles", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000001"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000002"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000003"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000004"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000005"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000006"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000007"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000008"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000009"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000010"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000011"),
                columns: new[] { "CreatedByUserId", "UpdatedByUserId" },
                values: new object[] { null, null });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_AssignedToUserId",
                table: "ProjectTasks",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RecordComments_OwnerType_OwnerId_CreatedAt",
                table: "RecordComments",
                columns: new[] { "OwnerType", "OwnerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RecordFiles_OwnerType_OwnerId_CreatedAt",
                table: "RecordFiles",
                columns: new[] { "OwnerType", "OwnerId", "CreatedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "RecordComments");

            migrationBuilder.DropTable(
                name: "RecordFiles");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_AssignedToUserId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "StockItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ProjectUpdates");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ProjectCostItems");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ProjectCostItems");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Materials");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MaterialRequests");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "InvoiceLines");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Customers");
        }
    }
}
