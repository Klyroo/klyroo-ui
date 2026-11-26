using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

public class NavigationTests : KlyrooTestContext
{
    private static readonly KlyTabItem[] Tabs =
    [
        new("Account") { Value = "account" },
        new("Password") { Value = "password", Disabled = false },
    ];

    [Fact]
    public void Tabs_select_the_first_tab_by_default_and_hide_other_panels()
    {
        var cut = Render<KlyTabs>(p => p.Add(t => t.Items, Tabs));

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
        var cut = Render<KlyTabs>(p => p.Add(t => t.Items, Tabs).Add(t => t.Value, value).Add(t => t.ValueChanged, (string? v) => value = v));

        cut.Find("kly-tabs").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "password", Sequence = 1 });

        Assert.Equal("password", value);
        Assert.Equal("true", cut.FindAll("[role=tab]")[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Accordion_renders_closed_panels_findable_and_open_ones_visible()
    {
        KlyAccordionItem[] items = [new("One") { Value = "one" }, new("Two") { Value = "two" }];
        var cut = Render<KlyAccordion>(p => p.Add(a => a.Items, items).Add(a => a.Value, ["two"]).Add(a => a.HeadingLevel, 4));

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
        KlyAccordionItem[] items = [new("One"), new("Two"), new("Three")];
        var cut = Render<KlyAccordion>(p => p.Add(a => a.Items, items));

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
        var cut = Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, people).Add(t => t.Caption, "People")
            .AddChildContent<KlyColumn<Person, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name).Add(x => x.Sortable, true))
            .AddChildContent<KlyColumn<Person, int>>(c => c.Add(x => x.Title, "Age").Add(x => x.Field, x => x.Age)));

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
            b.OpenComponent<KlyColumn<Person, string>>(0);
            b.AddAttribute(1, "Title", "Name");
            b.AddAttribute(2, "Field", (Func<Person, string>)(x => x.Name));
            b.CloseComponent();
            if (showAge)
            {
                b.OpenComponent<KlyColumn<Person, int>>(3);
                b.AddAttribute(4, "Title", "Age");
                b.AddAttribute(5, "Field", (Func<Person, int>)(x => x.Age));
                b.CloseComponent();
            }
            b.OpenComponent<KlyTemplateColumn<Person>>(6);
            b.AddAttribute(7, "Title", "Notes");
            b.AddAttribute(8, "Template", (RenderFragment<Person>)(x => t => t.AddContent(0, "-")));
            b.CloseComponent();
        };
        var cut = Render<KlyTable<Person>>(p => p.Add(t => t.Items, people).Add(t => t.ChildContent, Columns()));
        Assert.Equal(["Name", "Notes"], cut.FindAll("th").Select(th => th.TextContent.Trim()));

        showAge = true;
        cut.Render(p => p.Add(t => t.ChildContent, Columns()));

        Assert.Equal(["Name", "Age", "Notes"], cut.FindAll("th").Select(th => th.TextContent.Trim()));
    }

    [Fact]
    public void Column_without_field_or_template_is_rejected()
    {
        var field = Assert.Throws<InvalidOperationException>(() => Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, Array.Empty<Person>())
            .AddChildContent<KlyColumn<Person, string>>(c => c.Add(x => x.Title, "Empty"))));
        var template = Assert.Throws<InvalidOperationException>(() => Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, Array.Empty<Person>())
            .AddChildContent<KlyTemplateColumn<Person>>(c => c.Add(x => x.Title, "Empty"))));

        Assert.Contains("needs a Field", field.Message);
        Assert.Contains("needs a Template", template.Message);
    }

    [Fact]
    public void A_template_column_renders_the_markup_of_each_item_and_has_no_sort_button()
    {
        Person[] people = [new("Zoe", 31), new("Al", 45)];
        var cut = Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, people)
            .AddChildContent<KlyTemplateColumn<Person>>(c => c.Add(x => x.Title, "Who").Add(x => x.Template, x => b => b.AddMarkupContent(0, $"<b>{x.Name}</b>"))));

        Assert.Equal(["Zoe", "Al"], cut.FindAll("tbody td b").Select(b => b.TextContent));
        Assert.Empty(cut.FindAll("th button"));
        Assert.Null(cut.Find("th").GetAttribute("aria-sort"));
    }

    [Fact]
    public void A_column_with_a_field_and_a_template_shows_the_template_and_sorts_by_the_field()
    {
        Person[] people = [new("Zoe", 31), new("Al", 45), new("Mia", 22)];
        var cut = Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, people)
            .AddChildContent<KlyColumn<Person, int>>(c => c.Add(x => x.Title, "Age").Add(x => x.Field, x => x.Age).Add(x => x.Sortable, true)
                .Add(x => x.Template, x => b => b.AddContent(0, $"{x.Name} ({x.Age})"))));

        cut.Find("th button").Click();

        Assert.Equal(["Mia (22)", "Zoe (31)", "Al (45)"], cut.FindAll("tbody td").Select(td => td.TextContent.Trim()));
    }

    [Fact]
    public void A_column_sorts_numbers_and_dates_by_value_not_by_text()
    {
        Person[] people = [new("A", 9), new("B", 10), new("C", 2)];
        var cut = Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, people)
            .AddChildContent<KlyColumn<Person, int>>(c => c.Add(x => x.Title, "Age").Add(x => x.Field, x => x.Age).Add(x => x.Sortable, true)));

        cut.Find("th button").Click();

        Assert.Equal(["2", "9", "10"], cut.FindAll("tbody td").Select(td => td.TextContent.Trim()));
    }

    private sealed record Booking(string Name, DateOnly Day, DateTime At);

    [Fact]
    public void A_column_sorts_dates_by_value_not_by_text()
    {
        // The texts of these dates sort the other way round in most cultures ("1/5/2026" before "12/31/2025", "05.01.2026" before "31.12.2025"):
        // only a sort by the DateOnly or DateTime itself puts December 2025 first.
        Booking[] bookings =
        [
            new("late", new DateOnly(2026, 1, 5), new DateTime(2026, 1, 5, 9, 30, 0)),
            new("early", new DateOnly(2025, 12, 31), new DateTime(2025, 12, 31, 18, 0, 0)),
            new("middle", new DateOnly(2026, 1, 2), new DateTime(2026, 1, 2, 7, 15, 0)),
        ];

        var byDay = Render<KlyTable<Booking>>(p => p
            .Add(t => t.Items, bookings)
            .AddChildContent<KlyColumn<Booking, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name))
            .AddChildContent<KlyColumn<Booking, DateOnly>>(c => c.Add(x => x.Title, "Day").Add(x => x.Field, x => x.Day).Add(x => x.Sortable, true)));
        byDay.FindAll("th button")[0].Click();
        Assert.Equal(["early", "middle", "late"], byDay.FindAll("tbody tr").Select(r => r.QuerySelector("td")!.TextContent.Trim()));
        byDay.FindAll("th button")[0].Click();
        Assert.Equal(["late", "middle", "early"], byDay.FindAll("tbody tr").Select(r => r.QuerySelector("td")!.TextContent.Trim()));

        var byTime = Render<KlyTable<Booking>>(p => p
            .Add(t => t.Items, bookings)
            .AddChildContent<KlyColumn<Booking, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name))
            .AddChildContent<KlyColumn<Booking, DateTime>>(c => c.Add(x => x.Title, "At").Add(x => x.Field, x => x.At).Add(x => x.Sortable, true)));
        byTime.FindAll("th button")[0].Click();
        Assert.Equal(["early", "middle", "late"], byTime.FindAll("tbody tr").Select(r => r.QuerySelector("td")!.TextContent.Trim()));
    }

    [Fact]
    public void Table_without_items_shows_the_empty_state()
    {
        var cut = Render<KlyTable<Person>>(p => p
            .Add(t => t.Items, Array.Empty<Person>())
            .AddChildContent<KlyColumn<Person, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name)));

        Assert.Equal("No data", cut.Find("td").TextContent.Trim());
        Assert.Equal("1", cut.Find("td").GetAttribute("colspan"));
    }
}
