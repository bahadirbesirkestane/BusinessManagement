using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectTemplateTaskDefaultsAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultAssignedUserId",
                table: "ProjectTemplateTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultResponsibleUserId",
                table: "ProjectTemplateTasks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultStartOffsetDays",
                table: "ProjectTemplateTasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskCategoryId",
                table: "ProjectTemplateTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ProjectTemplates",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_DefaultAssignedUserId",
                table: "ProjectTemplateTasks",
                column: "DefaultAssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_DefaultResponsibleUserId",
                table: "ProjectTemplateTasks",
                column: "DefaultResponsibleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTemplateTasks_TaskCategoryId",
                table: "ProjectTemplateTasks",
                column: "TaskCategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTemplateTasks_AspNetUsers_DefaultAssignedUserId",
                table: "ProjectTemplateTasks",
                column: "DefaultAssignedUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTemplateTasks_AspNetUsers_DefaultResponsibleUserId",
                table: "ProjectTemplateTasks",
                column: "DefaultResponsibleUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTemplateTasks_TaskCategories_TaskCategoryId",
                table: "ProjectTemplateTasks",
                column: "TaskCategoryId",
                principalTable: "TaskCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTemplateTasks_AspNetUsers_DefaultAssignedUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTemplateTasks_AspNetUsers_DefaultResponsibleUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTemplateTasks_TaskCategories_TaskCategoryId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTemplateTasks_DefaultAssignedUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTemplateTasks_DefaultResponsibleUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTemplateTasks_TaskCategoryId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "DefaultAssignedUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "DefaultResponsibleUserId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "DefaultStartOffsetDays",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "TaskCategoryId",
                table: "ProjectTemplateTasks");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ProjectTemplates");
        }
    }
}
