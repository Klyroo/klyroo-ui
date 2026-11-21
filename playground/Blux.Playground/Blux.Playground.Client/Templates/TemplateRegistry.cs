namespace Blux.Playground.Client.Templates;

public enum TemplateStatus
{
    /// <summary>The template exists in <c>templates/</c> and runs as a live demo of the docs site.</summary>
    Available,

    /// <summary>Planned: listed on the page, with no demo and no source.</summary>
    ComingSoon,
}

/// <summary>
/// A project template of the gallery. <see cref="WebProject"/> and <see cref="WebAssembly"/> are what the docs host needs to run the demo
/// (an available template only), the rest is what the /templates page shows.
/// </summary>
public sealed record TemplateInfo(
    string Name,
    string Title,
    string Description,
    IReadOnlyList<string> Features,
    TemplateStatus Status,
    string? WebProject = null,
    string? WebAssembly = null)
{
    public bool HasDemo => Status == TemplateStatus.Available;

    /// <summary>The path of the live demo below the docs site, with a trailing slash. Relative to the site root, never to the page.</summary>
    public string DemoPath => $"templates/{Name}/demo/";

    /// <summary>The name of the repository the template is published as, below <see cref="TemplateSettings.RepositoryBase"/>.</summary>
    public string RepositoryName => $"blux-template-{Name}";

    /// <summary>The path of the screenshot in the static files of the site, for "light" or "dark".</summary>
    public string Screenshot(string mode) => $"img/template-{Name}-{mode}.png";
}

/// <summary>Every template of the gallery, in the order of the page. Shared by the page and by the supervisor of the demos of the docs host.</summary>
public static class TemplateRegistry
{
    public static IReadOnlyList<TemplateInfo> All { get; } =
    [
        new("starter", "Starter",
            "A minimal app: header with navigation, language select and color mode, a home page, an about page and a footer.",
            ["Server first, then WebAssembly, on one set of pages", "A MAUI Blazor Hybrid head in the same solution", "Tailwind compiled by the build, with no Node"],
            TemplateStatus.Available, WebProject: "src/Starter.Web", WebAssembly: "Starter.Web"),
        Planned("dashboard", "Dashboard",
            "An admin app with a collapsible sidebar, data tables and charts.",
            "Sidebar layout with a search palette", "Sortable and filterable data tables", "Charts and summary cards"),
        new("landing", "Landing",
            "A product site with a hero, features, testimonials, pricing with a monthly and yearly switch, a FAQ and a footer.",
            ["Rendered by the server: no circuit, no WebAssembly", "Pricing switch with no script: radio buttons and CSS", "Copy in one typed C# file"],
            TemplateStatus.Available, WebProject: "src/Landing.Web", WebAssembly: "Landing.Web"),
        Planned("saas", "SaaS",
            "The screens around a subscription product: pricing, sign-in and account settings.",
            "Sign-in and sign-up forms", "A pricing page", "Account settings"),
        new("docs", "Docs",
            "A documentation site with navigation, search and Markdown content.",
            ["Pages written in Markdown, rendered by the server", "Navigation, table of contents and search built from the files", "Highlighted code with a copy button, and callouts"],
            TemplateStatus.Available, WebProject: "src/Docs.Web", WebAssembly: "Docs.Web"),
        Planned("portfolio", "Portfolio",
            "A personal site with a project list, an about page and a contact form.",
            "A project grid", "An about page", "A contact form"),
        Planned("changelog", "Changelog",
            "A release notes page built from the releases of a GitHub repository.",
            "Fetched at runtime and cached", "One entry per version", "A feed of the latest releases"),
        Planned("chat", "Chat",
            "A chat app with a conversation list and streamed replies.",
            "Any OpenAI compatible service through IChatClient", "Conversations stored in SQLite", "Reasoning and tool call display"),
        Planned("editor", "Editor",
            "A rich text editor with a toolbar, a document list and autosave.",
            "Toolbar and bubble menu", "Mentions and slash commands", "A MAUI Blazor Hybrid head"),
        Planned("calendar", "Calendar",
            "A scheduler with month, week and day views.",
            "Month, week and day views", "Create and move events", "A MAUI Blazor Hybrid head"),
    ];

    public static TemplateInfo? Find(string name) => All.FirstOrDefault(t => t.Name == name);

    private static TemplateInfo Planned(string name, string title, string description, params string[] features) =>
        new(name, title, description, features, TemplateStatus.ComingSoon);
}
