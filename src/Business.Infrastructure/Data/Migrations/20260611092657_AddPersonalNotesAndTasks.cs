using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalNotesAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonalNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ReminderAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalNotes_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalNotes_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PersonalNotes_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PersonalNotes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PersonalTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonalTasks_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonalTasks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PersonalTasks_ProjectTasks_ProjectTaskId",
                        column: x => x.ProjectTaskId,
                        principalTable: "ProjectTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PersonalTasks_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_CustomerId",
                table: "PersonalNotes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_OwnerUserId_Category_CreatedAt",
                table: "PersonalNotes",
                columns: new[] { "OwnerUserId", "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_OwnerUserId_CreatedAt",
                table: "PersonalNotes",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_OwnerUserId_CustomerId",
                table: "PersonalNotes",
                columns: new[] { "OwnerUserId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_OwnerUserId_ProjectId",
                table: "PersonalNotes",
                columns: new[] { "OwnerUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_OwnerUserId_ProjectTaskId",
                table: "PersonalNotes",
                columns: new[] { "OwnerUserId", "ProjectTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_ProjectId",
                table: "PersonalNotes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalNotes_ProjectTaskId",
                table: "PersonalNotes",
                column: "ProjectTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_CustomerId",
                table: "PersonalTasks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_OwnerUserId_CreatedAt",
                table: "PersonalTasks",
                columns: new[] { "OwnerUserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_OwnerUserId_CustomerId",
                table: "PersonalTasks",
                columns: new[] { "OwnerUserId", "CustomerId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_OwnerUserId_ProjectId",
                table: "PersonalTasks",
                columns: new[] { "OwnerUserId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_OwnerUserId_ProjectTaskId",
                table: "PersonalTasks",
                columns: new[] { "OwnerUserId", "ProjectTaskId" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_OwnerUserId_Status_DueDate",
                table: "PersonalTasks",
                columns: new[] { "OwnerUserId", "Status", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_ProjectId",
                table: "PersonalTasks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalTasks_ProjectTaskId",
                table: "PersonalTasks",
                column: "ProjectTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonalNotes");

            migrationBuilder.DropTable(
                name: "PersonalTasks");
        }
    }
}
