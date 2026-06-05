using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Business.Web.Extensions;

public static class EnumDisplayExtensions
{
    public static string ToDisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        var display = member?.GetCustomAttribute<DisplayAttribute>();

        return display?.Name ?? value.ToString();
    }
}
