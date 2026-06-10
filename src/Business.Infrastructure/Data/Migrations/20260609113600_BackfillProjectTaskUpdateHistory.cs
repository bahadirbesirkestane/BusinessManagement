using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Business.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260609113600_BackfillProjectTaskUpdateHistory")]
public partial class BackfillProjectTaskUpdateHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO ProjectTaskUpdates (Id, ProjectTaskId, Title, Description, CreatedAt, UpdatedAt, CreatedByUserId, UpdatedByUserId)
            SELECT NEWID(), Id, N'Görev oluşturuldu', Title, CreatedAt, NULL, CreatedByUserId, NULL
            FROM ProjectTasks
            WHERE NOT EXISTS (
                SELECT 1
                FROM ProjectTaskUpdates
                WHERE ProjectTaskUpdates.ProjectTaskId = ProjectTasks.Id
            )
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE ProjectTaskUpdates
            FROM ProjectTaskUpdates
            INNER JOIN ProjectTasks ON ProjectTasks.Id = ProjectTaskUpdates.ProjectTaskId
            WHERE ProjectTaskUpdates.Title = N'Görev oluşturuldu'
              AND ProjectTaskUpdates.Description = ProjectTasks.Title
              AND ProjectTaskUpdates.UpdatedAt IS NULL
              AND ProjectTaskUpdates.UpdatedByUserId IS NULL
            """);
    }
}
