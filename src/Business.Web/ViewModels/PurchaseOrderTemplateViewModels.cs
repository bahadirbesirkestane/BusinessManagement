using Business.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public class PurchaseOrderTemplateListItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string ScopeText { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public int LineCount { get; set; }
}

public class PurchaseOrderTemplateFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Şablon adı zorunludur.")]
    [StringLength(180, ErrorMessage = "Şablon adı en fazla 180 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(40, ErrorMessage = "Şablon kodu en fazla 40 karakter olabilir.")]
    public string? Code { get; set; }

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir.")]
    public string? Description { get; set; }

    public PurchaseOrderScope DefaultScope { get; set; } = PurchaseOrderScope.General;
    public PurchaseOrderStatus DefaultStatus { get; set; } = PurchaseOrderStatus.Requested;
    public Guid? DefaultSupplierId { get; set; }

    [StringLength(80, ErrorMessage = "Vade en fazla 80 karakter olabilir.")]
    public string? DefaultPaymentTerm { get; set; }

    [StringLength(3, MinimumLength = 3, ErrorMessage = "Para birimi 3 karakter olmalıdır.")]
    public string DefaultCurrency { get; set; } = "TRY";

    [Range(0, 100, ErrorMessage = "KDV oranı 0 ile 100 arasında olmalıdır.")]
    public decimal? DefaultVatRate { get; set; }

    public bool IsActive { get; set; } = true;
}

public class PurchaseOrderTemplateLineFormViewModel
{
    public Guid? Id { get; set; }
    public Guid PurchaseOrderTemplateId { get; set; }
    public Guid? MaterialId { get; set; }

    [Required(ErrorMessage = "Sipariş içeriği zorunludur.")]
    [StringLength(600, ErrorMessage = "Sipariş içeriği en fazla 600 karakter olabilir.")]
    public string Content { get; set; } = string.Empty;

    [Range(0, 999999999, ErrorMessage = "Miktar 0'dan küçük olamaz.")]
    public decimal? Quantity { get; set; }

    [StringLength(80, ErrorMessage = "Miktar metni en fazla 80 karakter olabilir.")]
    public string? QuantityText { get; set; }

    [StringLength(40, ErrorMessage = "Birim en fazla 40 karakter olabilir.")]
    public string? Unit { get; set; }

    [StringLength(120, ErrorMessage = "Kalite en fazla 120 karakter olabilir.")]
    public string? Quality { get; set; }

    [Range(0, 999999999, ErrorMessage = "Birim fiyat 0'dan küçük olamaz.")]
    public decimal? UnitPrice { get; set; }

    [Range(0, 999999999, ErrorMessage = "Sipariş tutarı 0'dan küçük olamaz.")]
    public decimal? OrderTotal { get; set; }

    [StringLength(2000, ErrorMessage = "Not en fazla 2000 karakter olabilir.")]
    public string? Notes { get; set; }
}

public class PurchaseOrderTemplateLineItemViewModel
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? MaterialId { get; set; }
    public string? MaterialName { get; set; }
    public decimal? Quantity { get; set; }
    public string? QuantityText { get; set; }
    public string? Unit { get; set; }
    public string? Quality { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? OrderTotal { get; set; }
    public string? Notes { get; set; }
}

public class PurchaseOrderTemplateDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public string ScopeText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string? SupplierName { get; set; }
    public string? DefaultPaymentTerm { get; set; }
    public string DefaultCurrency { get; set; } = "TRY";
    public decimal? DefaultVatRate { get; set; }
    public IReadOnlyList<PurchaseOrderTemplateLineItemViewModel> Lines { get; set; } = [];
    public PurchaseOrderTemplateLineFormViewModel LineForm { get; set; } = new();
    public IReadOnlyList<ProjectTemplateLookupItemViewModel> Materials { get; set; } = [];
    public bool OpenLineForm { get; set; }
    public string LineFormMode { get; set; } = "create";
}
