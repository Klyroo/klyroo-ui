using System.Text.Json;
using System.Text.RegularExpressions;
using Blux.Playground.Client.Docs;
using Blux.UI.BundleGenerator;
using Blux.UI.Mcp.Markdown;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp.Tests;

/// <summary>The docs bundle is generated from the docs catalog and the library: it must hold all of it.</summary>
public class BundleTests
{
    [Fact]
    public void Every_catalog_component_is_in_the_bundle_with_parameters()
    {
        var bundle = TestSupport.Bundle;

        Assert.Equal(Catalog.Components.Length, bundle.Components.Count);
        foreach (var doc in Catalog.Components)
        {
            var component = Assert.Single(bundle.Components, c => c.Slug == doc.Slug);
            Assert.Equal(doc.Name, component.Name);
            Assert.Equal(doc.Group, component.Group);
            Assert.Equal(doc.Description, component.Description);
            Assert.Equal(doc.Api.Length, component.Api.Count);
            Assert.Contains(component.Api, api => api.Parameters.Count > 0);
        }
    }

    [Fact]
    public void Every_example_has_its_source()
    {
        foreach (var doc in Catalog.Components)
        {
            var component = TestSupport.Bundle.Components.Single(c => c.Slug == doc.Slug);
            Assert.Equal(doc.Examples.Length, component.Examples.Count);
            foreach (var example in component.Examples)
            {
                Assert.False(string.IsNullOrWhiteSpace(example.Source), $"{example.Id} has no source");
                Assert.Contains("<", example.Source);
            }
        }
    }

    [Fact]
    public void An_example_shown_on_several_pages_has_one_source()
    {
        foreach (var group in TestSupport.Service.AllExamples.GroupBy(e => e.Example.Id))
        {
            Assert.Single(group.Select(e => e.Example.Source).Distinct());
        }
    }

    [Fact]
    public void Parameters_carry_types_defaults_descriptions_and_allowed_values()
    {
        var button = TestSupport.Service.FindComponent("button")!.Api[0];

        var variant = Assert.Single(button.Parameters, p => p.Name == "Variant");
        Assert.Equal("ButtonVariant", variant.Type);
        Assert.Equal("ButtonVariant.Solid", variant.Default);
        Assert.Equal(["Solid", "Outline", "Soft", "Subtle", "Ghost", "Link"], variant.Values);

        var color = Assert.Single(button.Parameters, p => p.Name == "Color");
        Assert.Contains("primary", color.Values!);

        Assert.Contains(button.Parameters, p => p.Name == "Loading" && p.Description!.Contains("spinner"));
        Assert.Contains(button.Events, e => e.Name == "OnClick" && e.Type.StartsWith("EventCallback<"));
        Assert.Contains(button.Slots, s => s.Name == "ChildContent");
        Assert.DoesNotContain(button.Parameters, p => p.Type.StartsWith("RenderFragment") || p.Type.StartsWith("EventCallback"));
    }

    [Fact]
    public void Two_way_bindings_are_marked_bindable_and_their_Changed_callback_is_not_listed_as_an_event()
    {
        var modal = TestSupport.Service.FindComponent("modal")!.Api[0];

        Assert.Contains(modal.Parameters, p => p.Name == "Open" && p.Bindable);
        Assert.DoesNotContain(modal.Events, e => e.Name == "OpenChanged");
    }

    [Fact]
    public void Generic_components_use_their_type_parameter_names()
    {
        var table = TestSupport.Service.FindComponent("table")!;

        Assert.Equal("BxTable<TItem>", table.Api[0].Name);
        Assert.Contains(table.Api[0].Parameters, p => p.Name == "Items" && p.Type == "IEnumerable<TItem>");
        Assert.Equal(table, TestSupport.Service.FindComponent("BxColumn"));
    }

    [Fact]
    public void Ui_slots_and_theme_come_from_the_library()
    {
        var button = TestSupport.Service.FindComponent("button")!.Api[0];

        Assert.Equal("ButtonUi", button.Ui!.Type);
        Assert.Equal(["Base", "Label", "LeadingIcon", "TrailingIcon"], button.Ui.Slots.Select(s => s.Name));
        Assert.Equal("Button", button.Theme!.Property);
        Assert.Contains("base", button.Theme.Slots.Keys);
        Assert.Contains("solid", button.Theme.Variants["variant"].Keys);
        Assert.Equal("md", button.Theme.Defaults["size"]);
    }

    [Fact]
    public void The_theme_of_a_component_that_names_it_differently_is_found_through_its_source()
    {
        // BxToaster resolves Options.Toast.
        Assert.Equal("Toast", TestSupport.Service.FindComponent("toast")!.Api[0].Theme!.Property);
    }

    [Fact]
    public void Built_in_icons_are_listed()
    {
        Assert.Contains("search", TestSupport.Bundle.Icons);
        Assert.Contains("chevron-down", TestSupport.Bundle.Icons);
    }

    [Fact]
    public void Every_guide_of_the_catalog_is_in_the_bundle_as_markdown()
    {
        Assert.Equal(Catalog.Guides.Select(g => g.Href), TestSupport.Bundle.Guides.Select(g => g.Path));
        Assert.All(TestSupport.Bundle.Guides, g =>
        {
            Assert.StartsWith("# ", g.Markdown);
            Assert.True(g.Markdown.Length > 200, $"{g.Path} is almost empty");
        });
        Assert.Contains("AddBluxUi", TestSupport.Bundle.Guides.Single(g => g.Path == "/docs/installation").Markdown);
    }

    [Fact]
    public void Building_the_bundle_again_gives_the_embedded_one()
    {
        var rebuilt = BundleBuilder.Build(TestSupport.RepoRoot, [typeof(Blux.UI.Tools.BluxToolsOptions)]);

        Assert.Equal(
            JsonSerializer.Serialize(TestSupport.Bundle, DocsBundle.JsonOptions),
            JsonSerializer.Serialize(rebuilt, DocsBundle.JsonOptions));
    }

    [Fact]
    public void An_unrelated_extension_given_to_the_builder_does_not_change_the_bundle()
    {
        var withExtension = BundleBuilder.Build(TestSupport.RepoRoot, [typeof(Blux.UI.Tools.BluxToolsOptions), typeof(SisterOptions)]);

        Assert.Equal(
            JsonSerializer.Serialize(TestSupport.Bundle, DocsBundle.JsonOptions),
            JsonSerializer.Serialize(withExtension, DocsBundle.JsonOptions));
    }

    public sealed class SisterOptions
    {
        public Blux.UI.Theming.ComponentTheme Sister { get; } = new Blux.UI.Theming.ComponentTheme("sister", "root").Slot("root", "flex");

        // Same property name as the Button theme of core.
        public Blux.UI.Theming.ComponentTheme Button { get; } = new Blux.UI.Theming.ComponentTheme("sister-button", "root").Slot("root", "grid");
    }

    public sealed class FakeGauge : Blux.UI.Theming.BxComponentBase;

    public sealed class FakeButton : Blux.UI.Theming.BxComponentBase;

    public sealed class FakeNamedSister : Blux.UI.Theming.BxComponentBase;

    [Fact]
    public void A_component_of_an_extension_assembly_maps_to_its_own_theme_even_when_core_has_one_of_the_same_name()
    {
        var root = Path.Combine(Path.GetTempPath(), "blux-bundle-" + Guid.NewGuid().ToString("N"));
        var components = Path.Combine(root, "src", typeof(FakeGauge).Assembly.GetName().Name!, "Components");
        Directory.CreateDirectory(components);
        try
        {
            File.WriteAllText(Path.Combine(components, "FakeGauge.razor.cs"),
                "var theme = Options.Extension<Blux.UI.Mcp.Tests.BundleTests.SisterOptions>().Sister; var core = Options.Card;");
            File.WriteAllText(Path.Combine(components, "FakeButton.razor.cs"),
                "var theme = Options.Extension<SisterOptions>().Button;");
            File.WriteAllText(Path.Combine(components, "FakeNamedSister.razor.cs"), "// no theme access");

            Type[] extensions = [typeof(SisterOptions)];

            var gauge = BundleBuilder.ThemeFor(typeof(FakeGauge), root, extensions);
            Assert.NotNull(gauge);
            Assert.Equal("sister", gauge.Name);
            Assert.Equal("Sister", gauge.Property);
            Assert.Equal("flex", gauge.Slots["root"]);

            var button = BundleBuilder.ThemeFor(typeof(FakeButton), root, extensions);
            Assert.Equal("sister-button", button!.Name);
            Assert.Equal("grid", button.Slots["root"]);

            // No access in the source and no theme of that name: nothing to describe.
            Assert.Null(BundleBuilder.ThemeFor(typeof(FakeNamedSister), root, extensions));
            // Without the extension registered, only the plain core access of the source (Options.Card) is left.
            Assert.Equal("Card", BundleBuilder.ThemeFor(typeof(FakeGauge), root)!.Property);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void A_core_component_still_maps_to_its_core_theme_when_an_extension_has_the_same_name()
    {
        var button = BundleBuilder.ThemeFor(typeof(Blux.UI.BxButton), TestSupport.RepoRoot, [typeof(SisterOptions)]);

        Assert.Equal("Button", button!.Property);
        Assert.NotEqual("sister-button", button.Name);
    }

    [Fact]
    public void The_tool_has_the_version_of_the_library()
    {
        string Read(string tag) =>
            Regex.Match(File.ReadAllText(Path.Combine(TestSupport.RepoRoot, "build", "Version.props")), $"<{tag}>([^<]*)</{tag}>").Groups[1].Value;

        var library = Read("VersionPrefix") + (Read("VersionSuffix") is { Length: > 0 } suffix ? "-" + suffix : "");
        Assert.Equal(library, TestSupport.Bundle.Version);

        var tool = System.Diagnostics.FileVersionInfo.GetVersionInfo(TestSupport.ToolAssembly()).ProductVersion!.Split('+')[0];
        Assert.Equal(library, tool);
    }

    [Fact]
    public void The_skill_component_reference_is_up_to_date()
    {
        var committed = File.ReadAllText(Path.Combine(TestSupport.RepoRoot, SkillReference.RelativePath)).Replace("\r\n", "\n");

        Assert.True(
            committed == SkillReference.Components(TestSupport.Bundle),
            $"{SkillReference.RelativePath} is stale. Run: dotnet run --project build/Blux.UI.DocsBundle -- --skill-reference");
    }
}
