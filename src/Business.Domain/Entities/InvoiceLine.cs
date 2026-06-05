using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

    [Required(ErrorMessage = "Satır açıklaması zorunludur.")]
    [StringLength(420, ErrorMessage = "Satır açıklaması en fazla 420 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal Quantity { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [Range(0, 999999999, ErrorMessage = "Birim fiyat 0'dan küçük olamaz.")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır.")]
    public decimal VatRate { get; set; }

    [Range(0, 999999999, ErrorMessage = "İskonto 0'dan küçük olamaz.")]
    public decimal DiscountAmount { get; set; }

    [Range(0, 999999999, ErrorMessage = "Satır toplamı 0'dan küçük olamaz.")]
    public decimal LineTotal { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}
