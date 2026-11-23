using AngleSharp.Dom;
using Blux.UI.Tools.Query;

namespace Blux.UI.Tools.Tests;

/// <summary>Row actions: the column, the accessible names, buttons and overflow menu, groups, submenus, the sheet, the run loop, confirmations and the static rendering.</summary>
public class DataListRowActionsTests : BluxToolsTestContext
{
    private static readonly DataListSchema<Person> Labelled = DataListSchema.For<Person>(s => s
        .RowKey(p => p.Id)
        .RowLabel(p => p.Name)
        .Field(p => p.Name, f => f.Sortable())
        .Field(p => p.Age, f => f.Sortable())
        .DefaultSort(p => p.Name)
        .DefaultPageSize(10));

    private static DataListRowAction<Person> Act(string key) =>
        new() { Key = key, Label = key, OnExecute = _ => Task.CompletedTask };

    private IRenderedComponent<DataListRowActionsHarness> RenderList(IReadOnlyList<DataListRowAction<Person>> actions, Action<ComponentParameterCollectionBuilder<DataListRowActionsHarness>>? more = null) =>
        Render<DataListRowActionsHarness>(p =>
        {
            p.Add(c => c.Schema, Labelled).Add(c => c.Actions, actions);
            more?.Invoke(p);
        });

    private static IElement Row(IRenderedComponent<DataListRowActionsHarness> cut, int index) => cut.FindAll("tbody tr[data-slot=row]")[index];

    [Fact]
    public void The_column_is_added_with_a_header_only_assistive_technology_reads()
    {
        var cut = RenderList([Act("copy")]);

        Assert.Equal(3, cut.FindAll("thead th").Count);
        Assert.Equal("actions", cut.FindAll("thead th")[2].GetAttribute("data-key"));
        Assert.Equal("Actions", cut.FindAll("thead th")[2].TextContent.Trim());
        Assert.Equal(10, cut.FindAll("[data-slot=row-actions]").Count);
    }

    [Fact]
    public void A_list_without_row_actions_has_no_column()
    {
        var cut = RenderList([]);

        Assert.Equal(2, cut.FindAll("thead th").Count);
        Assert.Empty(cut.FindAll("[data-slot=row-actions]"));
    }

    [Fact]
    public void The_buttons_are_named_by_the_row_label_or_by_the_position()
    {
        var labelled = RenderList([Act("a")], p => p.Add(c => c.MaxVisible, 0));
        Assert.Equal("Actions for Person 01", Row(labelled, 0).QuerySelector("[data-slot=row-actions-trigger]")!.GetAttribute("aria-label"));
        Assert.Equal("Actions for Person 01", Row(labelled, 0).QuerySelector("[data-slot=row-actions-phone]")!.GetAttribute("aria-label"));

        var plain = Render<DataListRowActionsHarness>(p => p.Add(c => c.Actions, [Act("a")]).Add(c => c.MaxVisible, 0));
        Assert.Equal("Actions for row 3", Row(plain, 2).QuerySelector("[data-slot=row-actions-trigger]")!.GetAttribute("aria-label"));
    }

    [Fact]
    public void Up_to_MaxVisible_actions_are_buttons_and_the_rest_are_in_a_menu()
    {
        var cut = RenderList([Act("a"), Act("b"), Act("c"), Act("d")], p => p.Add(c => c.MaxVisible, 2));

        var row = Row(cut, 0);
        Assert.Equal(["a", "b"], row.QuerySelectorAll("[data-slot=row-action]").Select(b => b.GetAttribute("data-action")));
        // The menu holds c and d only; the sheet holds all four.
        Assert.Equal(["c", "d"], row.QuerySelectorAll("[data-slot=content] [role=menuitem]").Select(i => i.TextContent.Trim()));
        Assert.Equal(4, row.QuerySelectorAll("[data-slot=row-actions-sheet-list] [data-action]").Length);

        var none = RenderList([Act("a"), Act("b")], p => p.Add(c => c.MaxVisible, 0));
        Assert.Empty(Row(none, 0).QuerySelectorAll("[data-slot=row-action]"));
        Assert.Equal(2, Row(none, 0).QuerySelectorAll("[data-slot=content] [role=menuitem]").Length);

        var all = RenderList([Act("a"), Act("b")]);
        Assert.Empty(Row(all, 0).QuerySelectorAll("[data-slot=row-actions-menu]"));
    }

    [Fact]
    public void Groups_are_in_the_order_of_their_first_visible_action_with_a_divider_between()
    {
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "hidden", Label = "Hidden", Group = "First", Visible = _ => false },
            new DataListRowAction<Person> { Key = "a", Label = "A", Group = "Second" },
            new DataListRowAction<Person> { Key = "b", Label = "B", Group = "Third" },
            new DataListRowAction<Person> { Key = "c", Label = "C", Group = "Second" },
            new DataListRowAction<Person> { Key = "d", Label = "D" },
        };
        var cut = RenderList(actions, p => p.Add(c => c.MaxVisible, 0));

        var menu = Row(cut, 0).QuerySelector("[data-slot=content]")!;
        Assert.Equal(["menuitem:A", "menuitem:C", "separator:", "menuitem:B", "separator:", "menuitem:D"],
            menu.Children.Select(c => $"{c.GetAttribute("role")}:{(c.GetAttribute("role") == "menuitem" ? c.TextContent.Trim() : "")}"));
        // The sheet has the same groups with a divider between.
        Assert.Equal(2, Row(cut, 0).QuerySelectorAll("[data-slot=row-actions-sheet-list] > li[data-divider]").Length);
    }

    [Fact]
    public void Children_make_a_submenu_and_an_action_whose_children_are_all_hidden_is_hidden()
    {
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "send", Label = "Send to", Children = [Act("email"), Act("sms")] },
            new DataListRowAction<Person> { Key = "empty", Label = "Nothing", Children = [new DataListRowAction<Person> { Key = "x", Label = "X", Visible = _ => false }] },
        };
        var cut = RenderList(actions, p => p.Add(c => c.MaxVisible, 3));

        var row = Row(cut, 0);
        // An action with children is never a button of the row: it opens a submenu of the menu.
        Assert.Empty(row.QuerySelectorAll("[data-slot=row-action]"));
        Assert.Single(row.QuerySelectorAll("button[aria-haspopup=menu]"));
        Assert.Equal(["email", "sms"], row.QuerySelectorAll("[data-slot=submenu] [role=menuitem]").Select(i => i.TextContent.Trim()));
        Assert.DoesNotContain("Nothing", row.TextContent);
    }

    [Fact]
    public void A_condition_hides_an_action_for_one_row_and_a_row_with_nothing_to_show_has_no_controls()
    {
        var actions = new[] { new DataListRowAction<Person> { Key = "a", Label = "A", Visible = c => c.Item.Id % 2 == 0 } };
        var cut = RenderList(actions);

        // Sorted by name: Person 01 is id 1 (hidden), Person 02 is id 2.
        Assert.Empty(Row(cut, 0).QuerySelectorAll("[data-slot=row-actions] *"));
        Assert.Single(Row(cut, 1).QuerySelectorAll("[data-slot=row-action]"));
    }

    [Fact]
    public void A_disabled_action_says_why_in_the_sheet_and_is_not_run()
    {
        var ran = 0;
        var actions = new[]
        {
            new DataListRowAction<Person>
            {
                Key = "a", Label = "Archive", Disabled = _ => true, DisabledReason = _ => "Nothing to archive.",
                OnExecute = _ => { ran++; return Task.CompletedTask; },
            },
        };
        var cut = RenderList(actions);

        var button = Row(cut, 0).QuerySelector("[data-slot=row-action]")!;
        Assert.True(button.HasAttribute("disabled"));
        var entry = Row(cut, 0).QuerySelector("[data-slot=row-actions-sheet-list] button[data-action=a]")!;
        Assert.Equal("true", entry.GetAttribute("aria-disabled"));
        var reason = Row(cut, 0).QuerySelector("[data-slot=row-action-reason]")!;
        Assert.Equal("Nothing to archive.", reason.TextContent.Trim());
        Assert.Equal(reason.Id, entry.GetAttribute("aria-describedby"));
        Assert.Throws<MissingEventHandlerException>(() => entry.Click());
        Assert.Equal(0, ran);
    }

    [Fact]
    public void The_sheet_is_rendered_once_per_row_with_unique_ids()
    {
        var cut = RenderList([Act("a"), Act("b")]);

        Assert.Equal(10, cut.FindAll("[data-slot=row-actions-sheet]").Count);
        var ids = cut.FindAll("[id]").Select(e => e.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void Clicking_runs_the_action_with_the_row_and_ignores_a_second_click_while_it_runs()
    {
        var gate = new TaskCompletionSource();
        var calls = new List<(int Id, string Key, int Position)>();
        var actions = new[]
        {
            new DataListRowAction<Person>
            {
                Key = "slow", Label = "Slow",
                OnExecute = async c => { calls.Add((c.Item.Id, c.Key, c.Position)); await gate.Task; },
            },
        };
        var cut = RenderList(actions);

        Row(cut, 1).QuerySelector<IElement>("[data-action=slow]")!.Click();
        cut.WaitForAssertion(() => Assert.True(Row(cut, 1).QuerySelector("[data-action=slow]")!.HasAttribute("disabled")));
        Assert.Single(calls);
        Assert.False(Row(cut, 0).QuerySelector("[data-action=slow]")!.HasAttribute("disabled"));
        gate.SetResult();
        cut.WaitForAssertion(() => Assert.False(Row(cut, 1).QuerySelector("[data-action=slow]")!.HasAttribute("disabled")));

        Assert.Equal((2, "2", 2), calls[0]);
    }

    [Fact]
    public void A_failure_reported_without_throwing_raises_OnError_and_does_not_refresh()
    {
        DataListRowActionFailure<Person>? failure = null;
        var loads = 0;
        var provider = Labelled.InMemoryProvider(DataListControllerTests.People);
        var actions = new[]
        {
            new DataListRowAction<Person>
            {
                Key = "a", Label = "A", RefreshOnSuccess = true,
                OnExecute = c => { c.Fail("refused"); return Task.CompletedTask; },
            },
        };
        DataListProvider<Person> counting = (request, ct) => { loads++; return provider(request, ct); };
        var cut = RenderList(actions, p => p.Add(c => c.Provider, counting).Add(c => c.OnError, (DataListRowActionFailure<Person> f) => failure = f));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-slot=row-action]")));
        var before = loads;

        Row(cut, 0).QuerySelector<IElement>("[data-action=a]")!.Click();

        cut.WaitForAssertion(() => Assert.NotNull(failure));
        Assert.Equal("refused", failure!.Message);
        Assert.Null(failure.Exception);
        Assert.Equal(1, failure.Item.Id);
        Assert.Equal(before, loads);
    }

    [Fact]
    public void An_exception_is_caught_and_reported_and_a_success_refreshes_when_asked()
    {
        DataListRowActionFailure<Person>? failure = null;
        var loads = 0;
        var provider = Labelled.InMemoryProvider(DataListControllerTests.People);
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "boom", Label = "Boom", OnExecute = _ => throw new InvalidOperationException("no") },
            new DataListRowAction<Person> { Key = "ok", Label = "Ok", RefreshOnSuccess = true, OnExecute = _ => Task.CompletedTask },
        };
        DataListProvider<Person> counting = (request, ct) => { loads++; return provider(request, ct); };
        var cut = RenderList(actions, p => p.Add(c => c.Provider, counting).Add(c => c.OnError, (DataListRowActionFailure<Person> f) => failure = f));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-slot=row-action]")));
        var before = loads;

        Row(cut, 0).QuerySelector<IElement>("[data-action=boom]")!.Click();
        cut.WaitForAssertion(() => Assert.IsType<InvalidOperationException>(failure?.Exception));
        Assert.Equal(before, loads);

        Row(cut, 0).QuerySelector<IElement>("[data-action=ok]")!.Click();
        cut.WaitForAssertion(() => Assert.True(loads > before));
    }

    [Fact]
    public void A_confirmation_asks_first_and_runs_on_the_answer_yes()
    {
        var ran = new List<int>();
        var actions = new[]
        {
            new DataListRowAction<Person>
            {
                Key = "delete", Label = "Delete", Color = BxColor.Error,
                Confirm = new DataListRowActionConfirm<Person> { Title = "Delete person", Description = c => $"{c.Item.Name} will go.", ConfirmLabel = "Remove" },
                OnExecute = c => { ran.Add(c.Item.Id); return Task.CompletedTask; },
            },
        };
        var cut = RenderList(actions);

        Row(cut, 0).QuerySelector<IElement>("[data-action=delete]")!.Click();

        Assert.Empty(ran);
        var dialog = cut.Find("[data-slot=action-dialog]");
        Assert.Contains("Delete person", dialog.TextContent);
        Assert.Contains("Person 01 will go.", dialog.TextContent);
        cut.Find("[data-slot=action-confirm]").Click();
        cut.WaitForAssertion(() => Assert.Equal([1], ran));
    }

    [Fact]
    public void An_item_of_the_menu_runs_the_action_and_a_submenu_item_too()
    {
        var ran = new List<string>();
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "a", Label = "A", OnExecute = _ => { ran.Add("a"); return Task.CompletedTask; } },
            new DataListRowAction<Person>
            {
                Key = "send", Label = "Send",
                Children = [new DataListRowAction<Person> { Key = "email", Label = "Email", OnExecute = c => { ran.Add("email" + c.Item.Id); return Task.CompletedTask; } }],
            },
        };
        var cut = RenderList(actions, p => p.Add(c => c.MaxVisible, 0));

        Row(cut, 0).QuerySelectorAll("[data-slot=content] [role=menuitem]")[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("a", ran));
        Row(cut, 0).QuerySelectorAll("[data-slot=submenu] [role=menuitem]")[0].Click();
        cut.WaitForAssertion(() => Assert.Contains("email1", ran));
    }

    [Fact]
    public void A_link_action_is_a_link_and_is_not_run()
    {
        var ran = 0;
        var actions = new[] { new DataListRowAction<Person> { Key = "open", Label = "Open", Href = c => $"/people/{c.Item.Id}", OnExecute = _ => { ran++; return Task.CompletedTask; } } };
        var cut = RenderList(actions);

        var link = Row(cut, 0).QuerySelector("a[data-slot=row-action]")!;
        Assert.Equal("/people/1", link.GetAttribute("href"));
        Assert.Equal(0, ran);
    }

    [Fact]
    public void Statically_rendered_only_the_links_are_drawn()
    {
        using var context = new StaticRowActions();
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "open", Label = "Open", Href = c => $"/people/{c.Item.Id}" },
            new DataListRowAction<Person> { Key = "copy", Label = "Copy", OnExecute = _ => Task.CompletedTask },
            new DataListRowAction<Person> { Key = "more", Label = "More", Children = [new DataListRowAction<Person> { Key = "profile", Label = "Profile", Href = c => $"/profile/{c.Item.Id}" }, Act("x")] },
        };
        var cut = context.RenderList(p => p.Add(c => c.Schema, Labelled).Add(c => c.Actions, actions));

        var row = cut.FindAll("tbody tr[data-slot=row]")[0];
        Assert.Equal(["/people/1", "/profile/1"], row.QuerySelectorAll("a[data-slot=row-action]").Select(a => a.GetAttribute("href")));
        Assert.Empty(row.QuerySelectorAll("button"));
        Assert.Empty(row.QuerySelectorAll("[data-slot=row-actions-sheet]"));
    }

    private sealed class StaticRowActions : BluxToolsTestContext
    {
        public StaticRowActions() : base(interactive: false) { }

        public IRenderedComponent<DataListRowActionsHarness> RenderList(Action<ComponentParameterCollectionBuilder<DataListRowActionsHarness>> parameters) => Render<DataListRowActionsHarness>(parameters);
    }

    [Fact]
    public void The_runner_groups_by_first_appearance()
    {
        var a = new DataListRowAction<Person> { Key = "a", Label = "A", Group = "g2" };
        var b = new DataListRowAction<Person> { Key = "b", Label = "B", Group = "g1" };
        var c = new DataListRowAction<Person> { Key = "c", Label = "C", Group = "g2" };
        var d = new DataListRowAction<Person> { Key = "d", Label = "D" };

        var groups = DataListRowActionRunner<Person>.Group([a, b, c, d]);

        Assert.Equal(["a,c", "b", "d"], groups.Select(g => string.Join(',', g.Select(x => x.Key))));
    }
}
