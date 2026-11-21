using Blux.UI.Tools;
using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tools.Tests;

public class DataListEmptyTests : BluxToolsTestContext
{
    [Fact]
    public void Renders_the_localized_default_texts_and_the_search_icon()
    {
        var cut = Render<BxDataListEmpty>();

        Assert.Equal("No results", cut.Find("[data-slot=title]").TextContent);
        Assert.Equal("Try changing your search or your filters.", cut.Find("[data-slot=description]").TextContent);
        var icon = cut.Find("svg");
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        // The icon comes from the registrar of this package, not from the ones of Blux.UI: it is found, not the empty placeholder.
        Assert.Null(icon.GetAttribute("data-bx-missing-icon"));
        Assert.NotEmpty(icon.InnerHtml);
    }

    [Fact]
    public void Follows_the_culture_of_the_user()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = Render<BxDataListEmpty>();

            Assert.Equal("Aucun résultat", cut.Find("[data-slot=title]").TextContent);
            Assert.Equal("Essayez de modifier votre recherche ou vos filtres.", cut.Find("[data-slot=description]").TextContent);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [Fact]
    public void A_locale_set_by_a_cascading_value_wins_over_the_culture()
    {
        var cut = Render<CascadingValue<BxLocale>>(p => p
            .Add(c => c.Value, BxLocales.De)
            .AddChildContent<BxDataListEmpty>());

        Assert.Equal("Keine Ergebnisse", cut.Find("[data-slot=title]").TextContent);
    }

    [Fact]
    public void Parameters_replace_the_defaults_and_empty_strings_hide_the_parts()
    {
        var cut = Render<BxDataListEmpty>(p => p.Add(c => c.Title, "Nothing here").Add(c => c.Description, "").Add(c => c.Icon, ""));

        Assert.Equal("Nothing here", cut.Find("[data-slot=title]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=description]"));
        Assert.Empty(cut.FindAll("svg"));
    }

    [Fact]
    public void Actions_and_content_render_in_their_slots()
    {
        var cut = Render<BxDataListEmpty>(p => p
            .Add(c => c.Actions, (RenderFragment)(b => { b.OpenElement(0, "button"); b.AddContent(1, "Clear filters"); b.CloseElement(); }))
            .AddChildContent("<em>extra</em>"));

        Assert.Equal("Clear filters", cut.Find("[data-slot=actions] button").TextContent);
        Assert.Equal("extra", cut.Find("em").TextContent);
    }

    [Fact]
    public void Class_Ui_and_additional_attributes_reach_the_elements()
    {
        var cut = Render<BxDataListEmpty>(p => p
            .Add(c => c.Class, "my-root")
            .Add(c => c.Ui, new DataListEmptyUi { Title = "my-title" })
            .AddUnmatched("data-testid", "empty"));

        var root = cut.Find("[data-testid=empty]");
        Assert.Contains("my-root", root.ClassName);
        Assert.Contains("flex", root.ClassName);
        Assert.Contains("my-title", cut.Find("[data-slot=title]").ClassName);
    }

    [Fact]
    public void Slots_come_from_the_theme_in_the_options_of_Tools()
    {
        using var context = new TestCustomized();
        var cut = context.Render<BxDataListEmpty>();

        Assert.Contains("py-3", cut.Find("[data-slot=root]").ClassName);
        Assert.DoesNotContain("py-12", cut.Find("[data-slot=root]").ClassName);
    }

    private sealed class TestCustomized : BluxToolsTestContext
    {
        public TestCustomized() : base(t => t.DataListEmpty.Slot("root", "py-3")) { }
    }

    [Fact]
    public void A_message_override_reaches_the_component()
    {
        using var context = new TestMessages();
        var cut = context.Render<BxDataListEmpty>();

        Assert.Equal("Nothing found", cut.Find("[data-slot=title]").TextContent);
    }

    private sealed class TestMessages : BluxToolsTestContext
    {
        public TestMessages() : base(t => t.Messages("en", m => m with { NoResults = "Nothing found" })) { }
    }
}
