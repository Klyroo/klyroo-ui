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
    DocTableRow[]? TableRows = null,
    string? Callout = null,
    string? LiveDemoKey = null,
    string? LiveDemoTitle = null);

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
/// Every library exposes 12 deeply detailed pages including dedicated interactive Live Examples, exact enum/option tables, callouts, and full API reference.
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
            Lead: $"Deploy {name} across Blazor Web (SSR, Server, WebAssembly, Auto), .NET MAUI Blazor Hybrid (iOS, Android, macOS Catalyst, Windows), native C# MAUI, and NativeAOT binaries.",
            Sections:
            [
                new("maui-hybrid", ".NET MAUI Blazor Hybrid",
                    Paragraphs:
                    [
                        $"{uiPkg} declares explicit <SupportedPlatform> items for browser, ios, android, maccatalyst, and windows so multi-targeted .NET MAUI projects build with zero CA1416 platform-compatibility warnings.",
                        "In MAUI Blazor Hybrid there is no server circuit or HTTP prerender phase: components execute directly inside the native process and render into the local BlazorWebView."
                    ],
                    CodeSamples:
                    [
                        new("MauiProgram.cs", $"""
                            var builder = MauiApp.CreateBuilder();
                            builder.UseMauiApp<App>();
                            builder.Services.AddMauiBlazorWebView();

                            // Register {name} services after AddMauiBlazorWebView
                            {diCall}

                            return builder.Build();
                            """)
                    ],
                    Callout: "Always call the library registration method after AddMauiBlazorWebView() so NavigationManager and WebView services are available in DI."),
                new("native-maui", "Native C# .NET MAUI (Headless .Core)",
                    Paragraphs:
                    [
                        $"If your MAUI application uses XAML or C# Markup instead of BlazorWebView, install only {corePkg}. It carries zero dependency on ASP.NET Core or Razor and integrates directly into MVVM ViewModels, background sync services, or platform handlers."
                    ],
                    CodeSamples:
                    [
                        new("Terminal", $"dotnet add package {corePkg}"),
                        new("MainViewModel.cs", coreUsage)
                    ]),
                new("native-aot", "NativeAOT & IL Trimming Verification",
                    Paragraphs:
                    [
                        $"{corePkg} is built with <IsAotCompatible>true</IsAotCompatible>, <IsTrimmable>true</IsTrimmable>, and <TreatWarningsAsErrors>true</TreatWarningsAsErrors>. Every JSON serializer uses source-generated contexts and avoids unannotated reflection."
                    ],
                    CodeSamples:
                    [
                        new("Publish NativeAOT", "dotnet publish -c Release -p:PublishAot=true")
                    ],
                    TableHeaders: ["Target", "Package", "Status"],
                    TableRows:
                    [
                        new("Blazor Server / WASM / Auto / SSR", uiPkg, "Supported"),
                        new(".NET MAUI Hybrid (iOS, Android, Mac, Win)", uiPkg, "Supported (<SupportedPlatform>)"),
                        new("Native C# MAUI / Minimal APIs / Workers", corePkg, "NativeAOT & Trimmable")
                    ])
            ]);
}
