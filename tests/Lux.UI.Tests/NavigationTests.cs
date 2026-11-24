using Microsoft.AspNetCore.Components;

namespace Lux.UI.Tests;

public class NavigationTests : LuxTestContext
{
    private static readonly LxTabItem[] Tabs =
    [
        new("Account") { Value = "account" },
        new("Password") { Value = "password", Disabled = false },
    ];

    [Fact]
    public void Tabs_select_the_first_tab_by_default_and_hide_other_panels()
    {
        var cut = Render<LxTabs>(p => p.Add(t => t.Items, Tabs));

        var tabs = cut.FindAll("[role=tab]");
        Assert.Equal("true", tabs[0].GetAttribute("aria-selected"));
        Assert.Equal("-1", tabs[1].GetAttribute("tabindex"));
        var panels = cut.FindAll("[role=tabpanel]");
        Assert.False(panels[0].HasAttribute("hidden"));
        Assert.True(panels[1].HasAttribute("hidden"));
        Assert.Equal(tabs[0].Id, panels[0].GetAttribute("aria-labelledby"));
    }

    [Fact]
    public void Tabs_bound_value_follows_the_browser()
    {
        string? value = "account";
        var cut = Render<LxTabs>(p => p.Add(t => t.Items, Tabs).Add(t => t.Value, value).Add(t => t.ValueChanged, (string? v) => value = v));

        cut.Find("lx-tabs").TriggerEvent("onbxchange", new LxChangeEventArgs { Value = "password", Sequence = 1 });

        Assert.Equal("password", value);
        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Accordion_renders_closed_panels_findable_and_open_ones_visible()
    {
        LxAccordionItem[] items = [new("One") { Value = "one" }, new("Two") { Value = "two" }];
        var cut = Render<LxAccordion>(p => p.Add(a => a.Items, items).Add(a => a.Value, ["two"]).Add(a => a.HeadingLevel, 4));

        var triggers = cut.FindAll("[data-slot=trigger]");
        Assert.Equal("false", triggers[0].GetAttribute("aria-expanded"));
        Assert.Equal("true", triggers[1].GetAttribute("aria-expanded"));
        var regions = cut.FindAll("[role=region]");
        Assert.Equal("until-found", regions[0].GetAttribute("hidden"));
        Assert.False(regions[1].HasAttribute("hidden"));
        Assert.Equal(2, cut.FindAll("h4").Count);
    }

    [Fact]
    public void Accordion_triggers_control_their_own_panels()
    {
        LxAccordionItem[] items = [new("One"), new("Two"), new("Three")];
        var cut = Render<LxAccordion>(p => p.Add(a => a.Items, items));

        var triggers = cut.FindAll("[data-slot=trigger]");
        Assert.Equal(3, triggers.Select(t => t.Id).Distinct().Count());
        foreach (var trigger in triggers)
        {
            var panel = cut.Find($"#{trigger.GetAttribute("aria-controls")}");
            Assert.Equal(trigger.Id, panel.GetAttribute("aria-labelledby"));
        }
    }

    private sealed record Person(string Name, int Age);

    [Fact]
    public void Table_renders_registered_columns_and_sorts()
    {
        Person[] people = [new("Zoe", 31), new("Al", 45), new("Mia", 22)];
        var cut = Render<LxTable<Person>>(p => p
            .Add(t => t.Items, people).Add(t => t.Caption, "People")
            .AddChildContent<LxColumn<Person>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name).Add(x => x.Sortable, true))
            .AddChildContent<LxColumn<Person>>(c => c.Add(x => x.Title, "Age").Add(x => x.Field, x => x.Age)));

        Assert.Equal(["Name", "Age"], cut.FindAll("th").Select(th => th.TextContent.Trim()));
        Assert.Equal("none", cut.Find("th").GetAttribute("aria-sort"));
        Assert.Equal(["Zoe", "Al", "Mia"], FirstColumn());

        cut.Find("th button").Click();
        Assert.Equal(["Al", "Mia", "Zoe"], FirstColumn());
        Assert.Equal("ascending", cut.Find("th").GetAttribute("aria-sort"));

        cut.Find("th button").Click();
        Assert.Equal(["Zoe", "Mia", "Al"], FirstColumn());

        IEnumerable<string> FirstColumn() => cut.FindAll("tbody tr").Select(tr => tr.QuerySelector("td")!.TextContent.Trim());
    }

    [Fact]
    public void Table_column_added_by_a_condition_takes_its_markup_position()
    {
        Person[] people = [new("Zoe", 31)];
        var showAge = false;
        RenderFragment Columns() => b =>
        {
            b.OpenComponent<LxColumn<Person>>(0);
            b.AddAttribute(1, "Title", "Name");
            b.AddAttribute(2, "Field", (Func<Person, object?>)(x => x.Name));
            b.CloseComponent();
            if (showAge)
            {
                b.OpenComponent<LxColumn<Person>>(3);
                b.AddAttribute(4, "Title", "Age");
                b.AddAttribute(5, "Field", (Func<Person, object?>)(x => x.Age));
                b.CloseComponent();
            }
            b.OpenComponent<LxColumn<Person>>(6);
            b.AddAttribute(7, "Title", "Notes");
            b.AddAttribute(8, "Template", (RenderFragment<Person>)(x => t => t.AddContent(0, "-")));
            b.CloseComponent();
        };
        var cut = Render<LxTable<Person>>(p => p.Add(t => t.Items, people).Add(t => t.ChildContent, Columns()));
        Assert.Equal(["Name", "Notes"], cut.FindAll("th").Select(th => th.TextContent.Trim()));

        showAge = true;
        cut.Render(p => p.Add(t => t.ChildContent, Columns()));

        Assert.Equal(["Name", "Age", "Notes"], cut.FindAll("th").Select(th => th.TextContent.Trim()));
    }

    [Fact]
    public void Column_without_field_or_template_is_rejected()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Render<LxTable<Person>>(p => p
            .Add(t => t.Items, Array.Empty<Person>())
            .AddChildContent<LxColumn<Person>>(c => c.Add(x => x.Title, "Empty"))));

        Assert.Contains("needs a Field or a Template", ex.Message);
    }

    [Fact]
    public void Table_without_items_shows_the_empty_state()
    {
        var cut = Render<LxTable<Person>>(p => p
            .Add(t => t.Items, Array.Empty<Person>())
            .AddChildContent<LxColumn<Person>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name)));

        Assert.Equal("No data", cut.Find("td").TextContent.Trim());
        Assert.Equal("1", cut.Find("td").GetAttribute("colspan"));
    }
}
