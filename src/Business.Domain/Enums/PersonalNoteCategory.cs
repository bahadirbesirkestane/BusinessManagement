using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum PersonalNoteCategory
{
    [Display(Name = "Genel")]
    General = 1,

    [Display(Name = "Toplantı")]
    Meeting = 2,

    [Display(Name = "Görüşme")]
    Discussion = 3,

    [Display(Name = "E-posta")]
    Email = 4
}
