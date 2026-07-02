namespace Business.Infrastructure.Identity;

public static class AppThemes
{
    public const string Current = "current";
    public const string Soft = "soft";
    public const string Dark = "dark";

    public static IReadOnlyList<KeyValuePair<string, string>> Options { get; } =
    [
        new(Current, "Mevcut Tema"),
        new(Soft, "Yumuşak Açık"),
        new(Dark, "Koyu")
    ];

    public static string ResolveForRender(string? value, bool isAuthenticated)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Current;
        }

        return NormalizeSelection(value);
    }

    public static string NormalizeSelection(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            Soft => Soft,
            Dark => Dark,
            _ => Current
        };
    }
}
