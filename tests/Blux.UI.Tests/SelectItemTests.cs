using Microsoft.AspNetCore.Components;

namespace Blux.UI.Tests;

/// <summary>Count, Icon and Group of <see cref="BxSelectItem"/> in every component that lists them, and the markup staying as it was without them.</summary>
public class SelectItemTests : BluxTestContext
{
    public static TheoryData<string> Kinds => new("select", "selectmenu", "inputmenu", "listbox");

    private static readonly BxSelectItem[] Plain = [new("a", "Alpha"), new("b", "Beta") { Disabled = true }, new("c", "Gamma")];

    private IRenderedComponent<IComponent> RenderList(string kind, IReadOnlyList<BxSelectItem> items) => kind switch
    {
        "select" => Render<BxSelect>(p => p.Add(s => s.Items, items).Add(s => s.Name, "n")),
        "selectmenu" => Render<BxSelectMenu>(p => p.Add(s => s.Items, items).Add(s => s.Name, "n")),
        "inputmenu" => Render<BxInputMenu>(p => p.Add(s => s.Items, items).Add(s => s.Name, "n")),
        "listbox" => Render<BxListbox>(p => p.Add(s => s.Items, items).Add(s => s.Name, "n")),
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Items_without_the_new_properties_render_as_they_always_did(string kind)
    {
        var cut = RenderList(kind, Plain);

        // No group, no count, no leading icon, no marker: only the label and the check mark in each row.
        Assert.Empty(cut.FindAll("[role=group]"));
        Assert.Empty(cut.FindAll("[data-slot=group]"));
        Assert.Empty(cut.FindAll("[data-slot=group-label]"));
        Assert.Empty(cut.FindAll("[data-slot=count]"));
        Assert.Empty(cut.FindAll("[data-has-count]"));
        var options = cut.FindAll("[role=option]:not([data-slot=create])");
        Assert.Equal(3, options.Count);
        Assert.All(options, option =>
        {
            Assert.Equal(["SPAN", "svg"], option.Children.Select(c => c.TagName).ToArray());
            Assert.Equal(option.GetAttribute("data-label"), option.Children[0].TextContent);
            Assert.Equal(["data-value", "data-label"], option.Attributes.Select(a => a.Name).Where(n => n.StartsWith("data-")).ToArray());
        });
        // The listbox holds the options directly.
        Assert.All(options, option => Assert.Equal("listbox", option.ParentElement!.GetAttribute("role")));
        Assert.Equal("true", options[1].GetAttribute("aria-disabled"));
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Count_is_formatted_for_the_locale_at_the_end_of_the_row_and_marks_the_row(string kind)
    {
        var cut = RenderList(kind, [new("a", "Alpha") { Count = 1204 }, new("b", "Beta") { Count = 0 }, new("c", "Gamma")]);

        var options = cut.FindAll("[role=option]:not([data-slot=create])");
        var count = options[0].QuerySelector("[data-slot=count]")!;
        Assert.Equal("1,204", count.TextContent);
        Assert.Equal("0", options[1].QuerySelector("[data-slot=count]")!.TextContent);
        Assert.Null(options[2].QuerySelector("[data-slot=count]"));
        Assert.True(options[0].HasAttribute("data-has-count"));
        Assert.False(options[2].HasAttribute("data-has-count"));
        // Between the label and the check mark, after a separator that is only for assistive technology (so the name reads "Alpha, 1,204").
        Assert.Equal(", ", options[0].Children[1].TextContent);
        Assert.Contains("sr-only", options[0].Children[1].ClassName);
        Assert.Equal(count, options[0].Children[2]);
        // The typeahead and the filter keep reading the label only.
        Assert.Equal(["SPAN", "SPAN", "SPAN", "svg"], options[0].Children.Select(c => c.TagName).ToArray());
        Assert.Equal("Alpha", options[0].GetAttribute("data-label"));
        Assert.Contains("ms-auto", count.ClassName);
        Assert.Contains("tabular-nums", count.ClassName);
    }

    [Fact]
    public void Count_follows_the_locale_of_the_app()
    {
        var cut = Render<BxApp>(p => p.Add(a => a.Locale, BxLocales.Fr).Add(a => a.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<BxListbox>(0);
            b.AddAttribute(1, nameof(BxListbox.Items), (IReadOnlyList<BxSelectItem>)[new("a", "Alpha") { Count = 1204 }]);
            b.CloseComponent();
        })));

        // A narrow no-break space in French, not a comma.
        Assert.Equal(1204.ToString("N0", BxLocales.Fr.Culture), cut.Find("[data-slot=count]").TextContent);
        Assert.NotEqual("1,204", cut.Find("[data-slot=count]").TextContent);
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Icon_is_drawn_before_the_label_and_hidden_from_assistive_technology(string kind)
    {
        var cut = RenderList(kind, [new("a", "Alpha") { Icon = "star" }, new("b", "Beta")]);

        var options = cut.FindAll("[role=option]:not([data-slot=create])");
        Assert.Equal(["svg", "SPAN", "svg"], options[0].Children.Select(c => c.TagName).ToArray());
        var icon = options[0].Children[0];
        Assert.Equal("true", icon.GetAttribute("aria-hidden"));
        Assert.Contains("size-4", icon.GetAttribute("class"));
        Assert.False(icon.HasAttribute("data-bx-missing-icon"));
        // The row is still named by its label.
        Assert.Equal("Alpha", options[0].GetAttribute("data-label"));
        Assert.Equal(["SPAN", "svg"], options[1].Children.Select(c => c.TagName).ToArray());
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Groups_are_named_groups_of_options_in_the_listbox(string kind)
    {
        var cut = RenderList(kind, [
            new("fr", "France") { Group = "Europe" },
            new("jp", "Japan") { Group = "Asia" },
            new("xx", "Elsewhere"),
            new("de", "Germany") { Group = "Europe" },
            new("yy", "Nowhere"),
            new("kr", "Korea") { Group = "Asia" },
        ]);

        var listbox = cut.Find("[role=listbox]");
        var groups = cut.FindAll("[role=group]");
        Assert.Equal(2, groups.Count);
        // Items without a group first, in their order, then the groups in the order of their first item with all their items:
        // an ungrouped item is never between two groups, where it would look like part of one.
        Assert.Equal(["option", "option", "group", "group"], listbox.Children.Select(c => c.GetAttribute("role")).ToArray());
        Assert.Equal(["xx", "yy"], listbox.Children.Take(2).Select(c => c.GetAttribute("data-value")));
        Assert.Equal(["Europe", "Asia"], groups.Select(g => g.QuerySelector("[data-slot=group-label]")!.TextContent));
        Assert.Equal(["fr", "de"], groups[0].QuerySelectorAll("[role=option]").Select(o => o.GetAttribute("data-value")));
        Assert.Equal(["jp", "kr"], groups[1].QuerySelectorAll("[role=option]").Select(o => o.GetAttribute("data-value")));
        Assert.Equal(6, cut.FindAll("[role=option]:not([data-slot=create])").Count);

        // Each group is named by its heading, and the heading is not an option, not focusable, not announced as one.
        foreach (var group in groups)
        {
            var heading = group.QuerySelector("[data-slot=group-label]")!;
            Assert.Equal(heading.Id, group.GetAttribute("aria-labelledby"));
            Assert.False(string.IsNullOrEmpty(heading.Id));
            Assert.Equal("presentation", heading.GetAttribute("role"));
            Assert.False(heading.HasAttribute("tabindex"));
            Assert.Equal(group, heading.ParentElement);
        }
    }

    [Theory]
    [MemberData(nameof(Kinds))]
    public void Group_ids_are_unique_across_instances(string kind)
    {
        BxSelectItem[] items = [new("a", "Alpha") { Group = "One" }, new("b", "Beta") { Group = "Two" }];
        var first = RenderList(kind, items);
        var second = RenderList(kind, items);

        var ids = first.FindAll("[data-slot=group-label]").Concat(second.FindAll("[data-slot=group-label]")).Select(h => h.Id).ToList();
        Assert.Equal(4, ids.Distinct().Count());
    }

    [Fact]
    public void Group_slots_take_classes_from_Ui()
    {
        BxSelectItem[] items = [new("a", "Alpha") { Group = "One", Count = 1, Icon = "star" }];

        var select = Render<BxSelect>(p => p.Add(s => s.Items, items).Add(s => s.Ui, new SelectUi { Group = "g-1", GroupLabel = "gl-1", ItemCount = "ic-1", ItemLeadingIcon = "il-1" }));
        Assert.Contains("g-1", select.Find("[role=group]").ClassName);
        Assert.Contains("gl-1", select.Find("[data-slot=group-label]").ClassName);
        Assert.Contains("ic-1", select.Find("[data-slot=count]").ClassName);
        Assert.Contains("il-1", select.Find("[role=option] svg").GetAttribute("class"));

        var menu = Render<BxSelectMenu>(p => p.Add(s => s.Items, items).Add(s => s.Ui, new SelectMenuUi { Group = "g-2", GroupLabel = "gl-2", ItemCount = "ic-2", ItemLeadingIcon = "il-2" }));
        Assert.Contains("g-2", menu.Find("[role=group]").ClassName);
        Assert.Contains("gl-2", menu.Find("[data-slot=group-label]").ClassName);
        Assert.Contains("ic-2", menu.Find("[data-slot=count]").ClassName);

        var input = Render<BxInputMenu>(p => p.Add(s => s.Items, items).Add(s => s.Ui, new InputMenuUi { Group = "g-3", GroupLabel = "gl-3", ItemCount = "ic-3", ItemLeadingIcon = "il-3" }));
        Assert.Contains("g-3", input.Find("[role=group]").ClassName);
        Assert.Contains("ic-3", input.Find("[data-slot=count]").ClassName);

        var list = Render<BxListbox>(p => p.Add(s => s.Items, items).Add(s => s.Ui, new ListboxUi { Group = "g-4", GroupLabel = "gl-4", ItemCount = "ic-4", ItemLeadingIcon = "il-4" }));
        Assert.Contains("g-4", list.Find("[role=group]").ClassName);
        Assert.Contains("gl-4", list.Find("[data-slot=group-label]").ClassName);
    }

    [Fact]
    public void Selected_state_and_the_hidden_values_do_not_depend_on_groups()
    {
        BxSelectItem[] items = [new("a", "Alpha") { Group = "One" }, new("b", "Beta") { Group = "Two" }];

        var select = Render<BxSelect>(p => p.Add(s => s.Items, items).Add(s => s.Value, "b").Add(s => s.Name, "n"));
        Assert.Equal(["false", "true"], select.FindAll("[role=option]").Select(o => o.GetAttribute("aria-selected")));
        Assert.Equal("Beta", select.Find("[data-slot=label]").GetAttribute("data-label"));

        var menu = Render<BxSelectMenu>(p => p.Add(s => s.Items, items).Add(s => s.Value, (IReadOnlyList<string>)["a"]).Add(s => s.Multiple, true).Add(s => s.Create, true));
        Assert.Equal(["true", "false"], menu.FindAll("[role=option]:not([data-slot=create])").Select(o => o.GetAttribute("aria-selected")));
        // The create option is a child of the listbox, not of a group.
        Assert.Equal("listbox", menu.Find("[data-slot=create]").ParentElement!.GetAttribute("role"));
    }

    [Fact]
    public void CommandPalette_shows_a_count_before_the_shortcut_and_does_not_search_it()
    {
        var cut = Render<BxCommandPalette>(p => p.Add(c => c.Groups, (IReadOnlyList<BxCommandGroup>)
        [
            new("Mailboxes", [new BxCommandItem { Label = "Inbox", Count = 1204, Shortcut = "G I" }, new BxCommandItem { Label = "Starred" }]),
        ]));

        var options = cut.FindAll("[role=option]");
        var count = options[0].QuerySelector("[data-slot=count]")!;
        Assert.Equal("1,204", count.TextContent);
        Assert.Equal("kbd", count.NextElementSibling!.LocalName);
        Assert.Equal(", ", count.PreviousElementSibling!.TextContent);
        Assert.Contains("sr-only", count.PreviousElementSibling.ClassName);
        Assert.Null(options[1].QuerySelector("[data-slot=count]"));
        Assert.DoesNotContain("1204", options[0].GetAttribute("data-search"));
        Assert.Contains("tabular-nums", count.ClassName);

        var ui = Render<BxCommandPalette>(p => p
            .Add(c => c.Groups, (IReadOnlyList<BxCommandGroup>)[new(null, [new BxCommandItem { Label = "Inbox", Count = 3 }])])
            .Add(c => c.Ui, new CommandPaletteUi { ItemCount = "mine" }));
        Assert.Contains("mine", ui.Find("[data-slot=count]").ClassName);
    }

    [Fact]
    public void Select_and_SelectMenu_get_an_empty_trigger_slot_for_the_icon_of_the_selected_item_only_when_an_item_has_an_icon()
    {
        BxSelectItem[] withIcon = [new("a", "Alpha") { Icon = "star" }, new("b", "Beta")];

        var select = Render<BxSelect>(p => p.Add(s => s.Items, withIcon).Add(s => s.Value, "a"));
        var slot = select.Find("[role=combobox] [data-slot=selectedIcon]");
        // The browser fills it (Blazor renders no children there, so there is nothing to diff); the options say which have an icon.
        Assert.Empty(slot.Children);
        Assert.Equal("true", slot.GetAttribute("aria-hidden"));
        Assert.Equal(["", null], select.FindAll("[role=option]").Select(o => o.GetAttribute("data-has-icon")));

        var menu = Render<BxSelectMenu>(p => p.Add(s => s.Items, withIcon));
        Assert.Empty(menu.Find("button [data-slot=selectedIcon]").Children);

        Assert.Empty(Render<BxSelect>(p => p.Add(s => s.Items, Plain)).FindAll("[data-slot=selectedIcon]"));
        Assert.Empty(Render<BxSelectMenu>(p => p.Add(s => s.Items, Plain)).FindAll("[data-slot=selectedIcon]"));
        Assert.Empty(Render<BxSelect>(p => p.Add(s => s.Items, Plain)).FindAll("[data-has-icon]"));
    }

    [Fact]
    public void An_item_record_without_the_new_properties_equals_the_one_that_it_was()
    {
        // The new properties default to null, so existing items keep their equality and their markup.
        Assert.Equal(new BxSelectItem("a", "A") { Disabled = true }, new BxSelectItem("a", "A") { Disabled = true, Count = null, Icon = null, Group = null });
        Assert.NotEqual(new BxSelectItem("a", "A"), new BxSelectItem("a", "A") { Count = 0 });
    }
}
