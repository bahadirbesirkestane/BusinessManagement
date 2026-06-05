using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class RecordComment : BaseEntity
{
    public RecordOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    [Required(ErrorMessage = "Yorum metni zorunludur.")]
    [StringLength(2000, ErrorMessage = "Yorum en fazla 2000 karakter olabilir.")]
    public string CommentText { get; set; } = string.Empty;
    public bool IsInternal { get; set; } = true;
}
