using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramModuleRecipientsForMaterialRequestsAndPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramNotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TelegramNotificationSettingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramNotificationRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelegramNotificationRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TelegramNotificationRecipients_TelegramNotificationSettings_TelegramNotificationSettingId",
                        column: x => x.TelegramNotificationSettingId,
                        principalTable: "TelegramNotificationSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramNotificationRecipients_TelegramNotificationSettingId_Module_UserId",
                table: "TelegramNotificationRecipients",
                columns: new[] { "TelegramNotificationSettingId", "Module", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelegramNotificationRecipients_UserId",
                table: "TelegramNotificationRecipients",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramNotificationRecipients");
        }
    }
}
