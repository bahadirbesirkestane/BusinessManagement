using Business.Domain.Entities;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class PersonalNoteIndexViewModel
{
    public string? TitleFilter { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTaskId { get; set; }
    public IReadOnlyList<PersonalNoteListItemViewModel> Notes { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Customers { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Projects { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> ProjectTasks { get; set; } = [];
}

public class PersonalNoteListItemViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public PersonalNoteCategory Category { get; set; }
    public string CategoryText { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectTaskTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReminderAt { get; set; }
}

public class PersonalNoteFormViewModel
{
    public Guid Id { get; set; }

    public Guid? CustomerId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? ProjectTaskId { get; set; }

    [Display(Name = "Not türü")]
    public PersonalNoteCategory Category { get; set; } = PersonalNoteCategory.General;

    [Required(ErrorMessage = "Not başlığı zorunludur.")]
    [StringLength(220, ErrorMessage = "Not başlığı en fazla 220 karakter olabilir.")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Not içeriği zorunludur.")]
    [StringLength(4000, ErrorMessage = "Not içeriği en fazla 4000 karakter olabilir.")]
    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Display(Name = "Hatırlatma tarihi")]
    public DateTime? ReminderAt { get; set; }

    public IReadOnlyList<PersonalNoteLookupItemViewModel> Customers { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> Projects { get; set; } = [];
    public IReadOnlyList<PersonalNoteLookupItemViewModel> ProjectTasks { get; set; } = [];
}

public class PersonalNoteDetailsViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public PersonalNoteCategory Category { get; set; }
    public string CategoryText { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectTaskTitle { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ReminderAt { get; set; }
}

public class PersonalNoteLookupItemViewModel
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
