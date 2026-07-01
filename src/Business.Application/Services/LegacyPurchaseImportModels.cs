namespace Business.Application.Services;

public sealed class LegacyPurchaseImportResult
{
    public int CreatedProjects { get; set; }
    public int ReusedProjects { get; set; }
    public int ImportedOrders { get; set; }
    public int SkippedOrders { get; set; }
    public int MatchedSuppliers { get; set; }
    public int UnmatchedSuppliers { get; set; }
    public List<string> Messages { get; } = [];
}
