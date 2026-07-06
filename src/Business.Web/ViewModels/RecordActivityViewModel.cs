using Business.Domain.Entities;
using Business.Domain.Enums;

namespace Business.Web.ViewModels;

public class RecordActivityViewModel
{
    public RecordOwnerType OwnerType { get; set; }
    public Guid OwnerId { get; set; }
    public IReadOnlyList<RecordComment> Comments { get; set; } = [];
    public IReadOnlyList<RecordFile> Files { get; set; } = [];
    public IReadOnlyDictionary<string, string> UserNames { get; set; } = new Dictionary<string, string>();
    public bool CanDeleteComments { get; set; }
    public bool CanDeleteFiles { get; set; }
    public bool ShowTaskEmailOptions { get; set; }
    public bool TaskMailRecipientsConfigured { get; set; }
    public string? TaskMailRecipientSummary { get; set; }
    public long TaskMailAttachmentLimitBytes { get; set; }
    public string TaskMailAttachmentLimitLabel { get; set; } = "25 MB";

    public bool ShowDownloadButton => OwnerType is RecordOwnerType.Project or RecordOwnerType.ProjectTask or RecordOwnerType.PurchaseOrder or RecordOwnerType.MaterialRequest;
}
