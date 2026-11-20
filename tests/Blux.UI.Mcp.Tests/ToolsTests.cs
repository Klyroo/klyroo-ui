using System.Text.Json;
using ModelContextProtocol;

namespace Blux.UI.Mcp.Tests;

/// <summary>The tool methods are called directly: the protocol layer is covered by ServerTests.</summary>
public class ToolsTests
{
    private static DocsTools Tools => TestSupport.Tools;

    private static string FirstResultName(string output)
    {
        var line = output.Split('\n').First(l => l.StartsWith("- **", StringComparison.Ordinal));
        return line["- **".Length..line.IndexOf("**", 4, StringComparison.Ordinal)];
    }

    [Fact]
    public void The_server_exposes_the_documented_tools()
    {
        string[] expected =
            ["search-components", "get-component", "get-component-metadata", "list-examples", "get-example",
             "search-documentation", "get-documentation-page", "search-icons", "get-installation-guide"];
        Assert.Equal(expected.Order(), DocsTools.Names.Order());
    }

    // ---- search-components ----

    [Theory]
    [InlineData("button", "Button")]
    [InlineData("Button", "Button")]
    [InlineData("BxButton", "Button")]
    [InlineData("dropdown menu", "DropdownMenu")]
    [InlineData("dropdown-menu", "DropdownMenu")]
    [InlineData("tabs", "Tabs")]
    [InlineData("modal", "Modal")]
    [InlineData("input", "Input")]
    [InlineData("select", "Select")]
    [InlineData("chat message", "ChatMessage")]
    public void Search_ranks_the_exact_name_first(string query, string expected)
    {
        Assert.Equal(expected, FirstResultName(Tools.SearchComponents(query)));
    }

    [Theory]
    [InlineData("dialog", "Modal")]
    [InlineData("popup window", "Modal")]
    [InlineData("snackbar", "Toast")]
    [InlineData("notifications", "Toast")]
    [InlineData("date picker", "InputDate")]
    [InlineData("autocomplete", "InputMenu")]
    [InlineData("otp code", "PinInput")]
    [InlineData("drag and drop upload", "FileUpload")]
    [InlineData("login form", "AuthForm")]
    [InlineData("star rating", "InputRating")]
    [InlineData("wizard", "Stepper")]
    [InlineData("rich text editor", "Editor")]
    [InlineData("breadcrumbs", "Breadcrumb")]
    public void Search_finds_alternate_names_and_intents(string query, string expected)
    {
        Assert.Equal(expected, FirstResultName(Tools.SearchComponents(query)));
    }

    [Fact]
    public void An_exact_name_beats_components_that_merely_contain_it()
    {
        // "Page" also appears in PageHero, PageHeader... but the Page component comes first.
        Assert.Equal("Page", FirstResultName(Tools.SearchComponents("page")));
        // Input is a prefix of InputDate, InputMenu...
        var names = Tools.SearchComponents("input", limit: 6).Split('\n').Where(l => l.StartsWith("- **")).ToList();
        Assert.StartsWith("- **Input**", names[0]);
        Assert.All(names.Skip(1), n => Assert.Contains("Input", n));
    }

    [Fact]
    public void Search_without_a_query_lists_every_component_by_family()
    {
        var output = Tools.SearchComponents();

        Assert.Contains($"{TestSupport.Bundle.Components.Count} components", output);
        foreach (var group in TestSupport.Bundle.Groups) Assert.Contains($"## {group}", output);
        foreach (var component in TestSupport.Bundle.Components) Assert.Contains($"`{component.Slug}`", output);
    }

    [Fact]
    public void Search_can_be_restricted_to_a_family_and_limited()
    {
        var overlays = Tools.SearchComponents(group: "overlay");
        Assert.Contains("**Modal**", overlays);
        Assert.DoesNotContain("**Button**", overlays);

        var limited = Tools.SearchComponents("input", limit: 3);
        Assert.Equal(3, limited.Split('\n').Count(l => l.StartsWith("- **")));
    }

    [Fact]
    public void Search_rejects_an_unknown_family_and_reports_no_match()
    {
        var error = Assert.Throws<McpException>(() => Tools.SearchComponents(group: "Widgets"));
        Assert.Contains("Elements", error.Message);

        Assert.Contains("No component matches", Tools.SearchComponents("zzzzqqqq"));
    }

    [Fact]
    public void Every_alternate_name_points_at_a_component_that_exists()
    {
        var slugs = TestSupport.Bundle.Components.Select(c => c.Slug).ToHashSet();
        foreach (var (alias, targets) in DocsService.Aliases)
        {
            Assert.All(targets, slug => Assert.True(slugs.Contains(slug), $"alias '{alias}' points to unknown slug '{slug}'"));
        }
    }

    // ---- get-component ----

    [Fact]
    public void Get_component_returns_usage_examples_and_api_by_default()
    {
        var output = Tools.GetComponent("button");

        Assert.StartsWith("# Button", output);
        Assert.Contains("## Usage", output);
        Assert.Contains("## Examples", output);
        Assert.Contains("## API", output);
        Assert.DoesNotContain("## Theme", output);
        Assert.Contains("`Variant`: `ButtonVariant` = `ButtonVariant.Solid`", output);
        Assert.Contains("<BxButton", output);
        Assert.Contains("/components/button", output);
    }

    [Fact]
    public void Get_component_returns_only_the_sections_asked_for()
    {
        var api = Tools.GetComponent("Button", "api");
        Assert.Contains("## API", api);
        Assert.DoesNotContain("## Usage", api);
        Assert.DoesNotContain("## Examples", api);

        var theme = Tools.GetComponent("button", "theme");
        Assert.Contains("## Theme", theme);
        Assert.Contains("o.Button", theme);
        Assert.Contains("`solid` on `base`", theme);
        Assert.DoesNotContain("## API", theme);

        var two = Tools.GetComponent("button", "theme, usage");
        Assert.True(two.IndexOf("## Usage", StringComparison.Ordinal) < two.IndexOf("## Theme", StringComparison.Ordinal));
    }

    [Fact]
    public void Get_component_accepts_the_name_slug_or_type_of_a_component()
    {
        var expected = Tools.GetComponent("dropdown-menu", "usage");
        Assert.Equal(expected, Tools.GetComponent("DropdownMenu", "usage"));
        Assert.Equal(expected, Tools.GetComponent("BxDropdownMenu", "usage"));
        Assert.Equal(expected, Tools.GetComponent("dropdown_menu", "usage"));
    }

    [Fact]
    public void Get_component_with_an_unknown_name_lists_close_matches()
    {
        var error = Assert.Throws<McpException>(() => Tools.GetComponent("Buton"));
        Assert.Contains("No component named 'Buton'", error.Message);
        Assert.Contains("Button", error.Message);

        var menu = Assert.Throws<McpException>(() => Tools.GetComponent("dropdown"));
        Assert.Contains("DropdownMenu", menu.Message);

        var intent = Assert.Throws<McpException>(() => Tools.GetComponent("date picker"));
        Assert.Contains("InputDate", intent.Message);
    }

    [Fact]
    public void Get_component_rejects_an_unknown_section()
    {
        var error = Assert.Throws<McpException>(() => Tools.GetComponent("button", "api,changelog"));
        Assert.Contains("changelog", error.Message);
        Assert.Contains("usage, examples, api, theme", error.Message);
    }

    [Fact]
    public void A_component_without_a_theme_says_so()
    {
        Assert.Contains("no theme of its own", Tools.GetComponent("theme", "theme"));
    }

    // ---- get-component-metadata ----

    [Fact]
    public void Metadata_is_compact_json_by_default()
    {
        using var doc = JsonDocument.Parse(Tools.GetComponentMetadata("button"));
        var type = doc.RootElement.GetProperty("components")[0];

        Assert.Equal("BxButton", type.GetProperty("type").GetString());
        var parameters = type.GetProperty("parameters").EnumerateArray().Select(p => p.GetString()).ToList();
        Assert.Contains("Variant: ButtonVariant = ButtonVariant.Solid", parameters);
        Assert.Contains("OnClick: EventCallback<MouseEventArgs>", type.GetProperty("events").EnumerateArray().Select(p => p.GetString()));
        Assert.Contains("ChildContent", type.GetProperty("slots").EnumerateArray().Select(p => p.GetString()));
        Assert.Contains("Base", type.GetProperty("ui").GetProperty("slots").EnumerateArray().Select(p => p.GetString()));
        Assert.False(type.TryGetProperty("theme", out _));
        Assert.False(type.TryGetProperty("summary", out _));
    }

    [Fact]
    public void Full_metadata_has_descriptions_values_and_theme()
    {
        using var doc = JsonDocument.Parse(Tools.GetComponentMetadata("button", full: true));
        var type = doc.RootElement.GetProperty("components")[0];

        var variant = type.GetProperty("parameters").EnumerateArray().Single(p => p.GetProperty("name").GetString() == "Variant");
        Assert.Equal("ButtonVariant", variant.GetProperty("type").GetString());
        Assert.Contains("Ghost", variant.GetProperty("values").EnumerateArray().Select(v => v.GetString()));
        Assert.Equal("Button", type.GetProperty("theme").GetProperty("property").GetString());
        Assert.True(type.GetProperty("summary").GetString()!.Length > 10);
    }

    [Fact]
    public void Metadata_of_an_unknown_component_fails_helpfully()
    {
        var error = Assert.Throws<McpException>(() => Tools.GetComponentMetadata("slidover"));
        Assert.Contains("Slideover", error.Message);
    }

    // ---- examples ----

    [Fact]
    public void List_examples_lists_all_or_those_of_one_component()
    {
        var all = Tools.ListExamples();
        Assert.Contains("ButtonVariantsExample", all);
        Assert.Contains("ModalNestedExample", all);

        var button = Tools.ListExamples("button");
        Assert.Contains("ButtonVariantsExample", button);
        Assert.DoesNotContain("ModalNestedExample", button);

        var nested = Tools.ListExamples(query: "nested");
        Assert.Contains("ModalNestedExample", nested);
        Assert.DoesNotContain("ButtonVariantsExample", nested);
    }

    [Fact]
    public void Get_example_returns_the_source_with_or_without_the_suffix()
    {
        var output = Tools.GetExample("ButtonVariantsExample");

        Assert.Contains("```razor", output);
        Assert.Contains("<BxButton", output);
        Assert.Equal(output, Tools.GetExample("ButtonVariants"));
        Assert.Equal(output, Tools.GetExample("buttonvariants"));
    }

    [Fact]
    public void Get_example_with_an_unknown_name_suggests_close_ones()
    {
        var error = Assert.Throws<McpException>(() => Tools.GetExample("ButtonVariant"));
        Assert.Contains("ButtonVariantsExample", error.Message);

        var nothing = Assert.Throws<McpException>(() => Tools.GetExample("Qqqq"));
        Assert.Contains("list-examples", nothing.Message);
    }

    // ---- documentation ----

    [Fact]
    public void Search_documentation_finds_guides_and_components()
    {
        var theming = Tools.SearchDocumentation("global theme AddBluxUi");
        Assert.Contains("/docs/theming", theming);

        var render = Tools.SearchDocumentation("static rendering");
        Assert.Contains("/docs/render-modes", render);

        var components = Tools.SearchDocumentation("native modal dialog", "components");
        Assert.Contains("/components/modal", components);
        Assert.DoesNotContain("/docs/", components);

        var guides = Tools.SearchDocumentation("theme", "guides");
        Assert.Contains("/docs/theming", guides);
        Assert.DoesNotContain("/components/", guides);
    }

    [Fact]
    public void Search_documentation_validates_the_section_and_reports_no_result()
    {
        Assert.Throws<McpException>(() => Tools.SearchDocumentation("x", "blog"));
        Assert.Contains("Nothing found", Tools.SearchDocumentation("zzzzqqqq"));
    }

    [Fact]
    public void Get_documentation_page_returns_a_guide_by_path()
    {
        var expected = Tools.GetDocumentationPage("/docs/theming");

        Assert.Contains("# Theming", expected);
        Assert.Contains("o.Button.Slot", expected);
        Assert.Equal(expected, Tools.GetDocumentationPage("docs/theming"));
        Assert.Equal(expected, Tools.GetDocumentationPage("theming"));
        Assert.Contains("# Introduction", Tools.GetDocumentationPage("/docs"));
    }

    [Fact]
    public void Get_documentation_page_can_filter_by_heading()
    {
        var output = Tools.GetDocumentationPage("/docs/theming", "global, per instance");

        Assert.Contains("## Global theme", output);
        Assert.Contains("## Per instance", output);
        Assert.DoesNotContain("## Design tokens", output);

        var error = Assert.Throws<McpException>(() => Tools.GetDocumentationPage("/docs/theming", "nothing like this"));
        Assert.Contains("Design tokens", error.Message);
    }

    [Fact]
    public void Get_documentation_page_serves_component_pages_and_explains_unknown_paths()
    {
        var page = Tools.GetDocumentationPage("/components/modal");
        Assert.Contains("# Modal", page);
        Assert.Contains("## API", page);

        var error = Assert.Throws<McpException>(() => Tools.GetDocumentationPage("/docs/nope"));
        Assert.Contains("/docs/installation", error.Message);

        Assert.Throws<McpException>(() => Tools.GetDocumentationPage("/components/nope"));
    }

    // ---- icons ----

    [Fact]
    public void Search_icons_finds_built_in_names()
    {
        Assert.Contains("- search", Tools.SearchIcons("search"));
        var chevrons = Tools.SearchIcons("chevron");
        Assert.Contains("- chevron-down", chevrons);
        Assert.Contains("- chevron-up", chevrons);
        var description = typeof(DocsTools).GetMethod(nameof(DocsTools.SearchIcons))!
            .GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false).Cast<System.ComponentModel.DescriptionAttribute>().Single().Description;
        Assert.Contains("Iconify", description);
        Assert.Contains("No built-in icon", Tools.SearchIcons("zzzzqqqq"));
        Assert.Equal(5, Tools.SearchIcons(limit: 5).Split('\n').Count(l => l.StartsWith("- ")));
    }

    // ---- installation ----

    [Theory]
    [InlineData("auto", "precompiled")]
    [InlineData("auto", "tailwind")]
    [InlineData("server", "precompiled")]
    [InlineData("server", "tailwind")]
    [InlineData("wasm", "precompiled")]
    [InlineData("wasm", "tailwind")]
    [InlineData("ssr", "precompiled")]
    [InlineData("ssr", "tailwind")]
    public void Installation_guide_covers_every_render_mode_and_css_mode(string renderMode, string css)
    {
        var output = Tools.GetInstallationGuide(renderMode, css);

        Assert.Contains("dotnet add package Blux.UI", output);
        Assert.Contains("AddBluxUi", output);
        Assert.Contains("BxColorModeScript", output);
        Assert.Equal(css == "tailwind", output.Contains("obj/blux/blux.theme.css"));
        Assert.Equal(css == "precompiled", output.Contains("_content/Blux.UI/blux.css"));
        Assert.Equal(renderMode is "auto" or "wasm", output.Contains("`.Client` project"));
    }

    [Fact]
    public void Installation_guide_defaults_to_auto_and_precompiled_and_rejects_unknown_modes()
    {
        Assert.Equal(Tools.GetInstallationGuide("auto", "precompiled"), Tools.GetInstallationGuide());
        Assert.Contains("Interactive Auto", Tools.GetInstallationGuide());

        Assert.Contains("auto, server, wasm, ssr", Assert.Throws<McpException>(() => Tools.GetInstallationGuide("blazor")).Message);
        Assert.Contains("precompiled, tailwind", Assert.Throws<McpException>(() => Tools.GetInstallationGuide(css: "sass")).Message);
    }

    [Fact]
    public void Installation_snippets_match_the_installation_guide_of_the_docs_site()
    {
        var guide = TestSupport.Bundle.Guides.Single(g => g.Path == "/docs/installation").Markdown;

        foreach (var line in InstallationGuide.StylesheetHead.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("<link") || l.StartsWith("<BxColorModeScript")))
        {
            Assert.Contains(line, guide);
        }
        foreach (var line in InstallationGuide.TailwindImport.Split('\n')) Assert.Contains(line.Trim(), guide);
    }
}
