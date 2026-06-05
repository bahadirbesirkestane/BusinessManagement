using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum CostItemType
{
    [Display(Name = "Malzeme")]
    Material = 0,
    [Display(Name = "İşçilik")]
    Labor = 1,
    [Display(Name = "Dış Hizmet")]
    OutsourcedService = 2,
    [Display(Name = "Nakliye")]
    Shipping = 3,
    [Display(Name = "Genel Gider")]
    Overhead = 4,
    [Display(Name = "Diğer")]
    Other = 99
}
