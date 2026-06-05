using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class StockItem : BaseEntity
{
    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

    [Required(ErrorMessage = "Stok türü zorunludur.")]
    [StringLength(220, ErrorMessage = "Stok türü en fazla 220 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Kalınlık en fazla 80 karakter olabilir.")]
    public string? Thickness { get; set; }

    [StringLength(120, ErrorMessage = "Ölçü en fazla 120 karakter olabilir.")]
    public string? Dimensions { get; set; }

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal? Quantity { get; set; }

    [StringLength(80, ErrorMessage = "Miktar metni en fazla 80 karakter olabilir.")]
    public string? QuantityText { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }
    public StockStatus Status { get; set; } = StockStatus.InStock;
    [StringLength(160, ErrorMessage = "Konum en fazla 160 karakter olabilir.")]
    public string? Location { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}
