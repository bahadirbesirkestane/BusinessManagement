using System.ComponentModel.DataAnnotations;

namespace Business.Web.ViewModels;

public sealed class TaskEmailSettingsViewModel : IValidatableObject
{
    [Display(Name = "Alıcı e-posta adresleri")]
    [StringLength(2000, ErrorMessage = "Alıcı e-posta adresleri en fazla 2000 karakter olabilir.")]
    public string? RecipientEmails { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(RecipientEmails))
        {
            yield break;
        }

        var parts = RecipientEmails
            .Split(['\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var validator = new EmailAddressAttribute();
        foreach (var part in parts)
        {
            if (!validator.IsValid(part))
            {
                yield return new ValidationResult($"'{part}' geçerli bir e-posta adresi değil.", [nameof(RecipientEmails)]);
            }
        }
    }
}
