using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Material : BaseEntity
{
    [Required(ErrorMessage = "Malzeme adı zorunludur.")]
    [StringLength(220, ErrorMessage = "Malzeme adı en fazla 220 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;
    public MaterialCategory Category { get; set; } = MaterialCategory.Other;
    [StringLength(120, ErrorMessage = "Kategori adı en fazla 120 karakter olabilir.")]
    public string? CategoryName { get; set; }

    [StringLength(120, ErrorMessage = "Tür en fazla 120 karakter olabilir.")]
    public string? Type { get; set; }

    [StringLength(120, ErrorMessage = "Kalite en fazla 120 karakter olabilir.")]
    public string? Grade { get; set; }

    [StringLength(120, ErrorMessage = "Yüzey en fazla 120 karakter olabilir.")]
    public string? Surface { get; set; }

    [StringLength(120, ErrorMessage = "Ölçü en fazla 120 karakter olabilir.")]
    public string? Dimensions { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [StringLength(2000, ErrorMessage = "Açıklama en fazla 2000 karakter olabilir.")]
    public string? Description { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<MaterialRequest> MaterialRequests { get; set; } = new List<MaterialRequest>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
}
