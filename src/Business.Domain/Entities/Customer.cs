using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class Customer : BaseEntity
{
    [Required(ErrorMessage = "Müşteri adı zorunludur.")]
    [StringLength(220, ErrorMessage = "Müşteri adı en fazla 220 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Vergi no en fazla 40 karakter olabilir.")]
    public string? TaxNumber { get; set; }

    [StringLength(120, ErrorMessage = "Vergi dairesi en fazla 120 karakter olabilir.")]
    public string? TaxOffice { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta girin.")]
    [StringLength(180, ErrorMessage = "E-posta en fazla 180 karakter olabilir.")]
    public string? Email { get; set; }

    [StringLength(80, ErrorMessage = "Telefon en fazla 80 karakter olabilir.")]
    public string? Phone { get; set; }

    [StringLength(160, ErrorMessage = "Yetkili adı en fazla 160 karakter olabilir.")]
    public string? ContactPerson { get; set; }

    [StringLength(1000, ErrorMessage = "Adres en fazla 1000 karakter olabilir.")]
    public string? Address { get; set; }

    [StringLength(240, ErrorMessage = "Web sitesi en fazla 240 karakter olabilir.")]
    public string? Website { get; set; }

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? PaymentTerm { get; set; }
    public bool IsActive { get; set; } = true;
    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
