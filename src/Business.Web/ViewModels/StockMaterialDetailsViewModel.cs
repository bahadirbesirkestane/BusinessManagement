using Business.Domain.Entities;

namespace Business.Web.ViewModels;

public class StockMaterialDetailsViewModel
{
    public Material Material { get; set; } = default!;
    public IReadOnlyList<IGrouping<string, StockItem>> Groups { get; set; } = [];
}
