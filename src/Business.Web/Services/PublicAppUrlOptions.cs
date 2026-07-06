namespace Business.Web.Services;

public sealed class PublicAppUrlOptions
{
    public const string SectionName = "Application";

    public string PublicBaseUrl { get; init; } = string.Empty;
}
