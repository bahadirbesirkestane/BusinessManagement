using Business.Domain.Entities;

namespace Business.Web.ViewModels;

public class StockMaterialGroupViewModel
{
    public Material Material { get; set; } = default!;
    public IReadOnlyList<StockItem> Items { get; set; } = [];
    public decimal TotalQuantity { get; set; }
    public string UnitText { get; set; } = string.Empty;
    public int LowStockCount { get; set; }
}
