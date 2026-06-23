using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class ProjectFolder : BaseEntity
{
    [Required]
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? ParentFolderId { get; set; }
    public ProjectFolder? ParentFolder { get; set; }

    [Required(ErrorMessage = "Klasör adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Klasör adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Sıralama değeri negatif olamaz.")]
    public int SortOrder { get; set; }

    public ICollection<ProjectFolder> ChildFolders { get; set; } = new List<ProjectFolder>();
    public ICollection<ProjectDriveFile> Files { get; set; } = new List<ProjectDriveFile>();
}
