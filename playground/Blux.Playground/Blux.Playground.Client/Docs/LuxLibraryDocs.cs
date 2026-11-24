using Blux.UI;

namespace Blux.Playground.Client.Docs;

public sealed record DocCodeSample(string Title, string Code);

public sealed record DocTableRow(string Col1, string Col2, string Col3);

public sealed record DocSection(
    string Id,
    string Heading,
    string[]? Paragraphs = null,
    string[]? Bullets = null,
    DocCodeSample[]? CodeSamples = null,
    string[]? TableHeaders = null,
    DocTableRow[]? TableRows = null);

public sealed record DocPageSpec(
    string Slug,
    string Title,
    string Group,
    string Lead,
    DocSection[] Sections,
    Type[]? ApiTypes = null);

public sealed record LibraryDocTree(
    string LibrarySlug,
    string LibraryName,
    string AccentHex,
    DocPageSpec[] Pages);

/// <summary>
/// Exhaustive, independent English multi-page documentation for each LuxStack library.
/// Every library exposes 11 dedicated pages scoped strictly to that library with minimal cross-library references.
/// </summary>
public static partial class LuxLibraryDocs
{
    public static IReadOnlyList<LibraryDocTree> All { get; } =
    [
        BuildQuery(),
        BuildTable(),
        BuildForm(),
        BuildDashboard(),
        BuildDisplay(),
        BuildSpreadsheet(),
        BuildAgent(),
    ];

    public static LibraryDocTree? FindLibrary(string? slug) =>
        string.IsNullOrWhiteSpace(slug)
            ? null
            : All.FirstOrDefault(l => string.Equals(l.LibrarySlug, slug.Trim(), StringComparison.OrdinalIgnoreCase));

    public static DocPageSpec? FindPage(LibraryDocTree lib, string? pageSlug)
    {
        var key = string.IsNullOrWhiteSpace(pageSlug) ? "" : pageSlug.Trim().ToLowerInvariant();
        return lib.Pages.FirstOrDefault(p => string.Equals(p.Slug, key, StringComparison.OrdinalIgnoreCase));
    }

    public static string PageHref(string librarySlug, string pageSlug) =>
        string.IsNullOrEmpty(pageSlug) ? $"/{librarySlug}/docs" : $"/{librarySlug}/docs/{pageSlug}";

    public static (BxContentSurroundLink? Previous, BxContentSurroundLink? Next) Surround(LibraryDocTree lib, string pageSlug)
    {
        var idx = Array.FindIndex(lib.Pages, p => string.Equals(p.Slug, pageSlug, StringComparison.OrdinalIgnoreCase));
        if (idx < 0) return (null, null);
        BxContentSurroundLink ToLink(DocPageSpec p) => new(p.Title, PageHref(lib.LibrarySlug, p.Slug)) { Description = p.Lead };
        return (
            idx > 0 ? ToLink(lib.Pages[idx - 1]) : null,
            idx < lib.Pages.Length - 1 ? ToLink(lib.Pages[idx + 1]) : null);
    }

    private static DocPageSpec MauiAndAotPage(string name, string corePkg, string uiPkg, string diCall, string coreUsage) =>
        new(
            Slug: "maui-and-aot",
            Title: "MAUI & NativeAOT",
            Group: "Platforms",
            Lead: $"Deploy {name} on Blazor Web, .NET MAUI Blazor Hybrid (iOS, Android, macOS Catalyst, Windows), native C# MAUI, and NativeAOT binaries.",
            Sections:
            [
                new("maui-hybrid", ".NET MAUI Blazor Hybrid",
                    Paragraphs:
                    [
                        $"{uiPkg} declares explicit <SupportedPlatform> items for browser, ios, android, maccatalyst, and windows so it compiles cleanly in multi-targeted .NET MAUI projects with zero platform analyzer warnings."
                    ],
                    CodeSamples:
                    [
                        new("MauiProgram.cs", $"""
                            var builder = MauiApp.CreateBuilder();
                            builder.UseMauiApp<App>();
                            builder.Services.AddMauiBlazorWebView();

                            // Register {name}
                            {diCall}
                            """)
                    ]),
                new("native-maui", "Native C# .NET MAUI (Headless .Core)",
                    Paragraphs:
                    [
                        $"For XAML or C# Markup MAUI applications without BlazorWebView, install only {corePkg}. It carries zero ASP.NET Core or Razor dependencies and plugs straight into MVVM ViewModels."
                    ],
                    CodeSamples:
                    [
                        new("Terminal", $"dotnet add package {corePkg}"),
                        new("ViewModel.cs", coreUsage)
                    ]),
                new("native-aot", "NativeAOT & IL Trimming",
                    Paragraphs:
                    [
                        $"{corePkg} ships with <IsAotCompatible>true</IsAotCompatible>, <IsTrimmable>true</IsTrimmable>, and <TreatWarningsAsErrors>true</TreatWarningsAsErrors>."
                    ],
                    CodeSamples:
                    [
                        new("Publish NativeAOT", "dotnet publish -c Release -p:PublishAot=true")
                    ])
            ]);
}
