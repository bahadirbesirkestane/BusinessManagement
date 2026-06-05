using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class RecordFile : BaseEntity
{
    public RecordOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    [Required]
    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(260)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string RelativePath { get; set; } = string.Empty;

    [StringLength(120)]
    public string? ContentType { get; set; }

    [Range(0, long.MaxValue)]
    public long Size { get; set; }

    [StringLength(500, ErrorMessage = "Dosya açıklaması en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}
