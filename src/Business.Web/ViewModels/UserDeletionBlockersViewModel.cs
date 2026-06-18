namespace Business.Web.ViewModels;

public sealed class UserDeletionBlockersViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserDisplayName { get; set; } = string.Empty;
    public bool WasDeactivated { get; set; }
    public IReadOnlyList<UserDeletionBlockerItemViewModel> Items { get; set; } = [];
}

public sealed class UserDeletionBlockerItemViewModel
{
    public int RowNumber { get; set; }
    public Guid? RecordId { get; set; }
    public string? RelationType { get; set; }
    public bool CanClearRelation { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string RecordKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
