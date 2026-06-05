using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum RecordOwnerType
{
    [Display(Name = "Proje")]
    Project = 1,

    [Display(Name = "Görev")]
    ProjectTask = 2,

    [Display(Name = "Sipariş")]
    PurchaseOrder = 3,

    [Display(Name = "Malzeme Talebi")]
    MaterialRequest = 4,

    [Display(Name = "Fatura")]
    Invoice = 5
}
