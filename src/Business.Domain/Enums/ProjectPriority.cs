using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum ProjectPriority
{
    [Display(Name = "Düşük")]
    Low = 0,
    [Display(Name = "Normal")]
    Normal = 1,
    [Display(Name = "Yüksek")]
    High = 2,
    [Display(Name = "Kritik")]
    Critical = 3
}
