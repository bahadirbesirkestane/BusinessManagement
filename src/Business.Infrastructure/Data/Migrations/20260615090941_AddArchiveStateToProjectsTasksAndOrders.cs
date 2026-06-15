using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArchiveStateToProjectsTasksAndOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "PurchaseOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "PurchaseOrders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ProjectTasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "ProjectTasks",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "Projects",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Projects",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_IsArchived_Status_CreatedAt",
                table: "PurchaseOrders",
                columns: new[] { "IsArchived", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_IsArchived_Status_CreatedAt",
                table: "ProjectTasks",
                columns: new[] { "IsArchived", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_IsArchived_Status_CreatedAt",
                table: "Projects",
                columns: new[] { "IsArchived", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_IsArchived_Status_CreatedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_IsArchived_Status_CreatedAt",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_Projects_IsArchived_Status_CreatedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Projects");
        }
    }
}
