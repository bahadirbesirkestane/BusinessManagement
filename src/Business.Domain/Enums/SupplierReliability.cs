using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum SupplierReliability
{
    [Display(Name = "Bilinmiyor")]
    Unknown = 0,
    [Display(Name = "1 Yıldız")]
    OneStar = 1,
    [Display(Name = "2 Yıldız")]
    TwoStars = 2,
    [Display(Name = "3 Yıldız")]
    ThreeStars = 3,
    [Display(Name = "4 Yıldız")]
    FourStars = 4,
    [Display(Name = "5 Yıldız")]
    FiveStars = 5
}
