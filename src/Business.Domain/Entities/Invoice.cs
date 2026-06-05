using Business.Domain.Common;
using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    [Required(ErrorMessage = "Fatura numarası zorunludur.")]
    [StringLength(60, ErrorMessage = "Fatura numarası en fazla 60 karakter olabilir.")]
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType Type { get; set; } = InvoiceType.Sales;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public DateTime IssueDate { get; set; } = DateTime.Today;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    [Range(0, 999999999, ErrorMessage = "Ara toplam 0'dan küçük olamaz.")]
    public decimal SubTotal { get; set; }

    [Range(0, 999999999, ErrorMessage = "KDV toplamı 0'dan küçük olamaz.")]
    public decimal VatTotal { get; set; }

    [Range(0, 999999999, ErrorMessage = "İskonto toplamı 0'dan küçük olamaz.")]
    public decimal DiscountTotal { get; set; }

    [Range(0, 999999999, ErrorMessage = "Genel toplam 0'dan küçük olamaz.")]
    public decimal GrandTotal { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string Currency { get; set; } = "TRY";

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? PaymentTerm { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
}
