using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectPlanningGantt2Infrastructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId",
                table: "ProjectTasks");

            migrationBuilder.AddColumn<bool>(
                name: "IsMilestone",
                table: "ProjectTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OutlineLevel",
                table: "ProjectTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentTaskId",
                table: "ProjectTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ProjectTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WbsCode",
                table: "ProjectTasks",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProjectTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTemplateTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentTemplateTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    WbsCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    OutlineLevel = table.Column<int>(type: "int", nullable: false),
                    DefaultDurationDays = table.Column<int>(type: "int", nullable: true),
                    DefaultPriority = table.Column<int>(type: "int", nullable: false),
                    IsMilestone = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTemplateTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTemplateTasks_ProjectTemplateTasks_ParentTemplateTaskId",
                        column: x => x.ParentTemplateTaskId,
                        principalTable: "ProjectTemplateTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTemplateTasks_ProjectTemplates_ProjectTemplateId",
                        column: x => x.ProjectTemplateId,
                        principalTable: "ProjectTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ParentTaskId",
                table: "ProjectTasks",
                column: "ParentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId_ParentTaskId_SortOrder",
                table: "ProjectTasks",
                columns: new[] { "ProjectId", "ParentTaskId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplates_Name",
                table: "ProjectTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_ParentTemplateTaskId",
                table: "ProjectTemplateTasks",
                column: "ParentTemplateTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_ProjectTemplateId_ParentTemplateTaskId_SortOrder",
                table: "ProjectTemplateTasks",
                columns: new[] { "ProjectTemplateId", "ParentTemplateTaskId", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_ParentTaskId",
                table: "ProjectTasks",
                column: "ParentTaskId",
                principalTable: "ProjectTasks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_ProjectTasks_ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "ProjectTemplateTasks");

            migrationBuilder.DropTable(
                name: "ProjectTemplates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ProjectId_ParentTaskId_SortOrder",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "IsMilestone",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "OutlineLevel",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "WbsCode",
                table: "ProjectTasks");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ProjectId",
                table: "ProjectTasks",
                column: "ProjectId");
        }
    }
}
