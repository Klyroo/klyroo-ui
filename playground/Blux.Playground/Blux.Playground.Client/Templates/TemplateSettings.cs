namespace Blux.Playground.Client.Templates;

/// <summary>
/// What the /templates page reads from the configuration of the docs host: <c>Blux:Templates:RepositoryBase</c>, the address the template
/// repositories are published below (for example <c>https://github.com/acme</c>). Unset until they exist: the page then says so instead of linking.
/// </summary>
public sealed record TemplateSettings(string? RepositoryBase = null)
{
    private Uri? Base =>
        string.IsNullOrWhiteSpace(RepositoryBase) ? null
        : Uri.TryCreate(RepositoryBase.Trim().TrimEnd('/') + "/", UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? uri : null;

    public bool IsPublished => Base is not null;

    /// <summary>The address of the repository of a template, or null while the repositories are not published.</summary>
    public string? SourceUrl(TemplateInfo template) => Base is { } b ? new Uri(b, template.RepositoryName).ToString() : null;

    /// <summary>"owner/repository" for <c>gh repo create --template</c>, or null when the base is not an owner address on GitHub.</summary>
    public string? GitHubTemplate(TemplateInfo template) =>
        Base is { Host: "github.com" } b && b.AbsolutePath.Trim('/') is { Length: > 0 } owner && !owner.Contains('/') ? $"{owner}/{template.RepositoryName}" : null;
}
