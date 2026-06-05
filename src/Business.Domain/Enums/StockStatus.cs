using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Enums;

public enum StockStatus
{
    [Display(Name = "Stokta")]
    InStock = 0,
    [Display(Name = "Rezerve")]
    Reserved = 1,
    [Display(Name = "Az Stok")]
    LowStock = 2,
    [Display(Name = "Stok Yok")]
    OutOfStock = 3,
    [Display(Name = "Hurda")]
    Scrapped = 4
}
