using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMaterialCategoryDefinitionsAndListFilters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Materials",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaterialCategoryDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialCategoryDefinitions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MaterialCategoryDefinitions",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "Description", "IsActive", "Name", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000001"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Paslanmaz", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000002"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Çelik", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000003"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Alüminyum", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000004"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Bronz", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000005"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Plastikler", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000006"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Rulman", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000007"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Motor", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000008"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Redüktör", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000009"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Cıvata", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000010"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Kılavuz", null, null },
                    { new Guid("9f1f88a7-7732-4c70-b2a1-000000000011"), new DateTime(2026, 5, 26, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Paslanmaz saç yüzey", null, null }
                });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000001"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000002"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000003"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000004"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000005"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000006"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000007"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000008"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000009"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000010"),
                column: "CategoryName",
                value: null);

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000011"),
                column: "CategoryName",
                value: null);

            migrationBuilder.Sql("""
                UPDATE Materials SET CategoryName =
                    CASE Category
                        WHEN 0 THEN N'Paslanmaz'
                        WHEN 1 THEN N'Çelik'
                        WHEN 2 THEN N'Alüminyum'
                        WHEN 3 THEN N'Bronz'
                        WHEN 4 THEN N'Plastikler'
                        WHEN 5 THEN N'Rulman'
                        WHEN 6 THEN N'Motor'
                        WHEN 7 THEN N'Redüktör'
                        WHEN 8 THEN N'Cıvata'
                        WHEN 9 THEN N'Kılavuz'
                        WHEN 10 THEN N'Paslanmaz saç yüzey'
                        ELSE CategoryName
                    END
                WHERE CategoryName IS NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialCategoryDefinitions_Name",
                table: "MaterialCategoryDefinitions",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaterialCategoryDefinitions");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Materials");
        }
    }
}
