using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskCategoriesAssignmentsAndProjectTimeline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks");

            migrationBuilder.AddColumn<int>(
                name: "ProgressPercent",
                table: "ProjectTasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResponsibleUserId",
                table: "ProjectTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SubmittedForReviewAt",
                table: "ProjectTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskCategoryId",
                table: "ProjectTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaskCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectTaskAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTaskAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectTaskAssignments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProjectTaskAssignments_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "TaskCategories",
                columns: new[] { "Id", "Color", "CreatedAt", "CreatedByUserId", "IsActive", "Name", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { new Guid("7c49d41c-1dc3-45cf-8d4b-000000000001"), "#2563eb", new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), null, true, "Satış", null, null },
                    { new Guid("7c49d41c-1dc3-45cf-8d4b-000000000002"), "#7c3aed", new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), null, true, "Teklif", null, null },
                    { new Guid("7c49d41c-1dc3-45cf-8d4b-000000000003"), "#16a34a", new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), null, true, "Üretim", null, null },
                    { new Guid("7c49d41c-1dc3-45cf-8d4b-000000000004"), "#ea580c", new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc), null, true, "Montaj", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_ResponsibleUserId",
                table: "ProjectTasks",
                column: "ResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_TaskCategoryId",
                table: "ProjectTasks",
                column: "TaskCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAssignments_ProjectTaskId_UserId",
                table: "ProjectTaskAssignments",
                columns: new[] { "ProjectTaskId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTaskAssignments_UserId",
                table: "ProjectTaskAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCategories_Name",
                table: "TaskCategories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_ResponsibleUserId",
                table: "ProjectTasks",
                column: "ResponsibleUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_TaskCategories_TaskCategoryId",
                table: "ProjectTasks",
                column: "TaskCategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_ResponsibleUserId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_TaskCategories_TaskCategoryId",
                table: "ProjectTasks");

            migrationBuilder.DropTable(
                name: "ProjectTaskAssignments");

            migrationBuilder.DropTable(
                name: "TaskCategories");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_ResponsibleUserId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_TaskCategoryId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ProgressPercent",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ResponsibleUserId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "SubmittedForReviewAt",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "TaskCategoryId",
                table: "ProjectTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_AspNetUsers_AssignedToUserId",
                table: "ProjectTasks",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
