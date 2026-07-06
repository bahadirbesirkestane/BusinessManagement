using Business.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace Business.Domain.Entities;

public class TaskEmailNotificationSetting : BaseEntity
{
    [StringLength(2000, ErrorMessage = "Alıcı e-posta adresleri en fazla 2000 karakter olabilir.")]
    public string? RecipientEmails { get; set; }
}
