using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class PersonalNote : BaseEntity
{
    [Required(ErrorMessage = "Kayıt sahibi zorunludur.")]
    [StringLength(450)]
    public string OwnerUserId { get; set; } = string.Empty;

    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ProjectTaskId { get; set; }
    public ProjectTask? ProjectTask { get; set; }

    public PersonalNoteCategory Category { get; set; } = PersonalNoteCategory.General;

    [Required(ErrorMessage = "Not başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Not başlığı en fazla 220 karakter olabilir.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Not içeriği zorunludur.")]
    [StringLength(4000, ErrorMessage = "Not içeriği en fazla 4000 karakter olabilir.")]
    public string Content { get; set; } = string.Empty;

    public DateTime? ReminderAt { get; set; }
}
