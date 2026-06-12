using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum RecordVisibility
{
    [Display(Name = "Genel")]
    General = 0,

    [Display(Name = "Sadece admin")]
    AdminOnly = 1
}
