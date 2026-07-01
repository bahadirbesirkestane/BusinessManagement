using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class CompanyFolder : BaseEntity
{
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? ParentFolderId { get; set; }
    public CompanyFolder? ParentFolder { get; set; }

    [Required(ErrorMessage = "Klasör adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Klasör adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, int.MaxValue, ErrorMessage = "Sıralama değeri negatif olamaz.")]
    public int SortOrder { get; set; }

    public ICollection<CompanyFolder> ChildFolders { get; set; } = new List<CompanyFolder>();
    public ICollection<CompanyFile> Files { get; set; } = new List<CompanyFile>();
}
