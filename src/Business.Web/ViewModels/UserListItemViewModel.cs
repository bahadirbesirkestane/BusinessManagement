namespace Business.Web.ViewModels;

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public string RolesText { get; set; } = string.Empty;
}
