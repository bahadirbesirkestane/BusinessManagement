using Business.Domain.Common;

namespace Business.Domain.Entities;

public class ProjectTaskAssignment : BaseEntity
{
    public Guid ProjectTaskId { get; set; }
    public ProjectTask ProjectTask { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
}
