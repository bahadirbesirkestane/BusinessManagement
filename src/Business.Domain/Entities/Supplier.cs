using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Supplier : BaseEntity
{
    [Required(ErrorMessage = "Tedarikçi adı zorunludur.")]
    [StringLength(220, ErrorMessage = "Tedarikçi adı en fazla 220 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(120, ErrorMessage = "Tür en fazla 120 karakter olabilir.")]
    public string? Type { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(180, ErrorMessage = "E-posta en fazla 180 karakter olabilir.")]
    public string? Email { get; set; }

    [StringLength(80, ErrorMessage = "Telefon en fazla 80 karakter olabilir.")]
    public string? Phone { get; set; }

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? PaymentTerm { get; set; }

    [StringLength(1000, ErrorMessage = "Adres en fazla 1000 karakter olabilir.")]
    public string? Address { get; set; }

    [StringLength(240, ErrorMessage = "Web sitesi en fazla 240 karakter olabilir.")]
    public string? Website { get; set; }
    public SupplierReliability Reliability { get; set; } = SupplierReliability.Unknown;
    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
