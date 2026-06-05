using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncMaterialCategoryNameSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490001",
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Yönetici", "YÖNETİCİ" });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490002",
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "Müdür", "MÜDÜR" });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490003",
                column: "Name",
                value: "Satın Alma");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490004",
                column: "Name",
                value: "Proje Kullanıcısı");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000002"),
                column: "Name",
                value: "Çelik");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000003"),
                column: "Name",
                value: "Alüminyum");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000008"),
                column: "Name",
                value: "Redüktör");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000009"),
                column: "Name",
                value: "Cıvata");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000010"),
                column: "Name",
                value: "Kılavuz");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000011"),
                column: "Name",
                value: "Paslanmaz saç yüzey");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000001"),
                column: "CategoryName",
                value: "Paslanmaz");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000002"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Çelik", "Çelik" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000003"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Alüminyum", "Alüminyum" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000004"),
                column: "CategoryName",
                value: "Bronz");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000005"),
                column: "CategoryName",
                value: "Plastikler");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000006"),
                column: "CategoryName",
                value: "Rulman");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000007"),
                column: "CategoryName",
                value: "Motor");

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000008"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Redüktör", "Redüktör" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000009"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Cıvata", "Cıvata" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000010"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Kılavuz", "Kılavuz" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000011"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { "Paslanmaz saç yüzey", "Paslanmaz saç yüzey" });

            migrationBuilder.UpdateData(
                table: "TaskCategories",
                keyColumn: "Id",
                keyValue: new Guid("7c49d41c-1dc3-45cf-8d4b-000000000001"),
                column: "Name",
                value: "Satış");

            migrationBuilder.UpdateData(
                table: "TaskCategories",
                keyColumn: "Id",
                keyValue: new Guid("7c49d41c-1dc3-45cf-8d4b-000000000003"),
                column: "Name",
                value: "Üretim");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490001",
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "YÃ¶netici", "YÃ–NETÄ°CÄ°" });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490002",
                columns: new[] { "Name", "NormalizedName" },
                values: new object[] { "MÃ¼dÃ¼r", "MÃœDÃœR" });

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490003",
                column: "Name",
                value: "SatÄ±n Alma");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490004",
                column: "Name",
                value: "Proje KullanÄ±cÄ±sÄ±");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000002"),
                column: "Name",
                value: "Ã‡elik");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000003"),
                column: "Name",
                value: "AlÃ¼minyum");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000008"),
                column: "Name",
                value: "RedÃ¼ktÃ¶r");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000009"),
                column: "Name",
                value: "CÄ±vata");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000010"),
                column: "Name",
                value: "KÄ±lavuz");

            migrationBuilder.UpdateData(
                table: "MaterialCategoryDefinitions",
                keyColumn: "Id",
                keyValue: new Guid("9f1f88a7-7732-4c70-b2a1-000000000011"),
                column: "Name",
                value: "Paslanmaz saÃ§ yÃ¼zey");

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
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "Ã‡elik" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000003"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "AlÃ¼minyum" });

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
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "RedÃ¼ktÃ¶r" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000009"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "CÄ±vata" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000010"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "KÄ±lavuz" });

            migrationBuilder.UpdateData(
                table: "Materials",
                keyColumn: "Id",
                keyValue: new Guid("2d222bd2-4b85-45ad-ae70-000000000011"),
                columns: new[] { "CategoryName", "Name" },
                values: new object[] { null, "Paslanmaz saÃ§ yÃ¼zey" });

            migrationBuilder.UpdateData(
                table: "TaskCategories",
                keyColumn: "Id",
                keyValue: new Guid("7c49d41c-1dc3-45cf-8d4b-000000000001"),
                column: "Name",
                value: "SatÄ±ÅŸ");

            migrationBuilder.UpdateData(
                table: "TaskCategories",
                keyColumn: "Id",
                keyValue: new Guid("7c49d41c-1dc3-45cf-8d4b-000000000003"),
                column: "Name",
                value: "Ãœretim");
        }
    }
}
