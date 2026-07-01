using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyDriveArchive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompanyFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ParentFolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFolders_CompanyFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "CompanyFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyFolders_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CompanyFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FolderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_CompanyFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyFiles_CompanyFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "CompanyFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CompanyFiles_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFiles_DepartmentId_FolderId_CreatedAt",
                table: "CompanyFiles",
                columns: new[] { "DepartmentId", "FolderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFiles_FolderId",
                table: "CompanyFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFolders_DepartmentId_Name",
                table: "CompanyFolders",
                columns: new[] { "DepartmentId", "Name" },
                unique: true,
                filter: "[ParentFolderId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFolders_DepartmentId_ParentFolderId_Name",
                table: "CompanyFolders",
                columns: new[] { "DepartmentId", "ParentFolderId", "Name" },
                unique: true,
                filter: "[ParentFolderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFolders_DepartmentId_ParentFolderId_SortOrder",
                table: "CompanyFolders",
                columns: new[] { "DepartmentId", "ParentFolderId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyFolders_ParentFolderId",
                table: "CompanyFolders",
                column: "ParentFolderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyFiles");

            migrationBuilder.DropTable(
                name: "CompanyFolders");
        }
    }
}
