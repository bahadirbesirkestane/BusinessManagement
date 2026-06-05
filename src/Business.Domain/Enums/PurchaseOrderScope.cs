using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum PurchaseOrderScope
{
    [Display(Name = "Genel")]
    General = 0,
    [Display(Name = "Proje")]
    Project = 1
}
