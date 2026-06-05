using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MakeProjectTasksFlexible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_Projects_ProjectId",
                table: "ProjectTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectTasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "ProjectTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualCustomerName",
                table: "ProjectTasks",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManualProjectName",
                table: "ProjectTasks",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTasks_CustomerId",
                table: "ProjectTasks",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Customers_CustomerId",
                table: "ProjectTasks",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Projects_ProjectId",
                table: "ProjectTasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_Customers_CustomerId",
                table: "ProjectTasks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProjectTasks_Projects_ProjectId",
                table: "ProjectTasks");

            migrationBuilder.DropIndex(
                name: "IX_ProjectTasks_CustomerId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ManualCustomerName",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ManualProjectName",
                table: "ProjectTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProjectId",
                table: "ProjectTasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectTasks_Projects_ProjectId",
                table: "ProjectTasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
