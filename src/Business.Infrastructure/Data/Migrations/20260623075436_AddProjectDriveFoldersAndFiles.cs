using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectDriveFoldersAndFiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectFolders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ProjectFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFolders_ProjectFolders_ParentFolderId",
                        column: x => x.ParentFolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectFolders_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectDriveFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    table.PrimaryKey("PK_ProjectDriveFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDriveFiles_ProjectFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "ProjectFolders",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectDriveFiles_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDriveFiles_FolderId",
                table: "ProjectDriveFiles",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDriveFiles_ProjectId_FolderId_CreatedAt",
                table: "ProjectDriveFiles",
                columns: new[] { "ProjectId", "FolderId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ParentFolderId",
                table: "ProjectFolders",
                column: "ParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ProjectId_Name",
                table: "ProjectFolders",
                columns: new[] { "ProjectId", "Name" },
                unique: true,
                filter: "[ParentFolderId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ProjectId_ParentFolderId_Name",
                table: "ProjectFolders",
                columns: new[] { "ProjectId", "ParentFolderId", "Name" },
                unique: true,
                filter: "[ParentFolderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFolders_ProjectId_ParentFolderId_SortOrder",
                table: "ProjectFolders",
                columns: new[] { "ProjectId", "ParentFolderId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDriveFiles");

            migrationBuilder.DropTable(
                name: "ProjectFolders");
        }
    }
}
