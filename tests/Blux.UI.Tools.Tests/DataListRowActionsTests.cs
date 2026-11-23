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

    // The browser opens the sheet from its trigger and tells .NET: only then does the sheet hold its actions.
    private static void SetSheet(IRenderedComponent<DataListRowActionsHarness> cut, int index, bool open) =>
        Row(cut, index).QuerySelector("bx-modal")!.TriggerEvent("onbxtoggle", new BxToggleEventArgs { Open = open, Sequence = open ? 1 : 2 });

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
        // The menu holds c and d only; the sheet holds all four once it is open.
        Assert.Equal(["c", "d"], row.QuerySelectorAll("[data-slot=content] [role=menuitem]").Select(i => i.TextContent.Trim()));
        SetSheet(cut, 0, open: true);
        Assert.Equal(4, Row(cut, 0).QuerySelectorAll("[data-slot=row-actions-sheet-list] [data-action]").Length);

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
        SetSheet(cut, 0, open: true);
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
        SetSheet(cut, 0, open: true);
        var entry = Row(cut, 0).QuerySelector("[data-slot=row-actions-sheet-list] button[data-action=a]")!;
        Assert.Equal("true", entry.GetAttribute("aria-disabled"));
        var reason = Row(cut, 0).QuerySelector("[data-slot=row-action-reason]")!;
        Assert.Equal("Nothing to archive.", reason.TextContent.Trim());
        Assert.Equal(reason.Id, entry.GetAttribute("aria-describedby"));
        Assert.Throws<MissingEventHandlerException>(() => entry.Click());
        Assert.Equal(0, ran);
    }

    [Fact]
    public void A_closed_row_holds_one_copy_of_its_actions_and_the_sheet_only_while_it_is_open()
    {
        var cut = RenderList([Act("a"), Act("b")]);

        Assert.Equal(10, cut.FindAll("[data-slot=row-actions-sheet]").Count);
        // Closed: the actions are the buttons of the row, nothing else carries them, and the dialog has no body.
        Assert.Equal(["a", "b"], Row(cut, 0).QuerySelectorAll("[data-action]").Select(e => e.GetAttribute("data-action")));
        Assert.Empty(cut.FindAll("[data-slot=row-actions-sheet-list]"));
        Assert.Empty(cut.FindAll("[data-slot=row-actions-sheet] [data-slot=body]"));

        SetSheet(cut, 1, open: true);
        Assert.Equal(["a", "b", "a", "b"], Row(cut, 1).QuerySelectorAll("[data-action]").Select(e => e.GetAttribute("data-action")));
        Assert.Single(cut.FindAll("[data-slot=row-actions-sheet-list]"));
        // The other rows still hold one copy.
        Assert.Equal(["a", "b"], Row(cut, 0).QuerySelectorAll("[data-action]").Select(e => e.GetAttribute("data-action")));

        SetSheet(cut, 1, open: false);
        Assert.Empty(cut.FindAll("[data-slot=row-actions-sheet-list]"));
        Assert.Equal(["a", "b"], Row(cut, 1).QuerySelectorAll("[data-action]").Select(e => e.GetAttribute("data-action")));
    }

    [Fact]
    public void Ids_are_unique_in_the_list_with_every_sheet_open()
    {
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "a", Label = "A", Disabled = _ => true, DisabledReason = _ => "No." },
            new DataListRowAction<Person> { Key = "p", Label = "P", Disabled = _ => true, DisabledReason = _ => "Parent no.", Children = [Act("c1"), Act("c2")] },
        };
        var cut = RenderList(actions);
        for (var i = 0; i < 10; i++) SetSheet(cut, i, open: true);

        Assert.Equal(10, cut.FindAll("[data-slot=row-actions-sheet-list]").Count);
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

    private static DataListRowAction<Person> Parent(string key, Func<DataListRowActionContext<Person>, bool>? visible, Func<DataListRowActionContext<Person>, bool>? disabled, string? reason, Action<string> ran) =>
        new()
        {
            Key = key, Label = key, Visible = visible, Disabled = disabled, DisabledReason = reason is null ? null : _ => reason,
            Children =
            [
                new DataListRowAction<Person> { Key = key + "-child", Label = key + " child", OnExecute = _ => { ran(key + "-child"); return Task.CompletedTask; } },
                new DataListRowAction<Person> { Key = key + "-link", Label = key + " link", Href = c => $"/people/{c.Item.Id}" },
            ],
        };

    [Fact]
    public void A_child_of_a_disabled_action_does_not_run_in_the_sheet_and_the_sheet_says_why_with_the_reason_of_the_parent()
    {
        var ran = new List<string>();
        var cut = RenderList([Parent("send", null, _ => true, "Sending is off.", ran.Add)], p => p.Add(c => c.MaxVisible, 0));
        SetSheet(cut, 0, open: true);

        var sheet = Row(cut, 0).QuerySelector("[data-slot=row-actions-sheet-list]")!;
        var child = sheet.QuerySelector("button[data-action=send-child]")!;
        Assert.Equal("true", child.GetAttribute("aria-disabled"));
        Assert.Throws<MissingEventHandlerException>(() => child.Click());
        // A link child of a disabled parent is not a link in the sheet: a disabled entry.
        Assert.Null(sheet.QuerySelector("a[data-action=send-link]"));
        var link = sheet.QuerySelector("button[data-action=send-link]")!;
        Assert.Equal("true", link.GetAttribute("aria-disabled"));
        // The reason is said once, under the heading, and each child points to it.
        var reason = sheet.QuerySelectorAll("[data-slot=row-action-reason]");
        Assert.Single(reason);
        Assert.Equal("Sending is off.", reason[0].TextContent.Trim());
        Assert.Equal(reason[0].Id, child.GetAttribute("aria-describedby"));
        Assert.Equal(reason[0].Id, link.GetAttribute("aria-describedby"));
        Assert.Empty(ran);
    }

    [Fact]
    public void A_child_of_a_disabled_action_is_not_offered_in_the_menu_and_never_runs_from_it()
    {
        var ran = new List<string>();
        var cut = RenderList([Parent("send", null, _ => true, "Sending is off.", ran.Add)], p => p.Add(c => c.MaxVisible, 0));

        var row = Row(cut, 0);
        var parent = row.QuerySelector("[data-slot=content] [role=menuitem]")!;
        Assert.Equal("true", parent.GetAttribute("aria-disabled"));
        Assert.Equal("Sending is off.", parent.GetAttribute("title"));
        Assert.Empty(row.QuerySelectorAll("[data-slot=submenu]"));
        Assert.Empty(ran);
    }

    [Fact]
    public async Task The_runner_never_runs_a_child_of_a_disabled_or_hidden_action()
    {
        var ran = new List<string>();
        var disabled = Parent("d", null, _ => true, "Off.", ran.Add);
        var hidden = Parent("h", _ => false, null, null, ran.Add);
        var runner = new DataListRowActionRunner<Person>();
        // The runner reads only the row from the context.
        var context = new DataListRowActionContext<Person>(null!, DataListControllerTests.People[0], "1", 1, CancellationToken.None);

        Assert.False(runner.StateOf("1", disabled.Children![0], context).Disabled);
        var state = runner.StateOf("1", disabled.Children![0], context, [disabled]);
        Assert.True(state.Visible);
        Assert.True(state.Disabled);
        Assert.Equal("Off.", state.Reason);
        Assert.False(runner.StateOf("1", hidden.Children![0], context, [hidden]).Visible);
        Assert.False(runner.StateOf("1", hidden, context).Visible);

        Assert.Equal(RowActionOutcome.Ignored, await runner.RunAsync("1", disabled.Children![0], context, () => { }, (_, _) => Task.CompletedTask, CancellationToken.None, [disabled]));
        Assert.Equal(RowActionOutcome.Ignored, await runner.RunAsync("1", hidden.Children![0], context, () => { }, (_, _) => Task.CompletedTask, CancellationToken.None, [hidden]));
        Assert.Equal(RowActionOutcome.Succeeded, await runner.RunAsync("1", disabled.Children![0], context, () => { }, (_, _) => Task.CompletedTask, CancellationToken.None));
        Assert.Equal(["d-child"], ran);
    }

    [Fact]
    public void A_hidden_parent_hides_its_children_everywhere()
    {
        var ran = new List<string>();
        var cut = RenderList([Parent("send", _ => false, null, null, ran.Add), Act("x")], p => p.Add(c => c.MaxVisible, 0));
        SetSheet(cut, 0, open: true);

        Assert.DoesNotContain("send", Row(cut, 0).TextContent);
        Assert.Empty(Row(cut, 0).QuerySelectorAll("[data-action^=send]"));
    }

    [Fact]
    public void Statically_rendered_a_link_under_a_disabled_or_hidden_action_is_not_drawn()
    {
        using var context = new StaticRowActions();
        var ran = new List<string>();
        var actions = new[]
        {
            Parent("off", null, _ => true, "Off.", ran.Add),
            Parent("gone", _ => false, null, null, ran.Add),
            Parent("on", null, null, null, ran.Add),
        };
        var cut = context.RenderList(p => p.Add(c => c.Schema, Labelled).Add(c => c.Actions, actions));

        var row = cut.FindAll("tbody tr[data-slot=row]")[0];
        Assert.Equal(["on-link"], row.QuerySelectorAll("a[data-slot=row-action]").Select(a => a.GetAttribute("data-action")));
    }

    [Fact]
    public void The_reason_a_button_is_disabled_is_its_title_and_description_and_nothing_else_has_one()
    {
        var actions = new[]
        {
            new DataListRowAction<Person> { Key = "a", Label = "Archive", Disabled = _ => true, DisabledReason = _ => "Nothing to archive." },
            new DataListRowAction<Person> { Key = "b", Label = "Beta", DisabledReason = _ => "Never shown while enabled." },
            new DataListRowAction<Person> { Key = "c", Label = "Gamma", Disabled = _ => true },
        };
        var cut = RenderList(actions, p => p.Add(c => c.MaxVisible, 2));

        var row = Row(cut, 0);
        var archive = row.QuerySelector("[data-slot=row-action][data-action=a]")!;
        Assert.Equal("Nothing to archive.", archive.GetAttribute("title"));
        Assert.Equal("Nothing to archive.", archive.GetAttribute("aria-description"));
        var beta = row.QuerySelector("[data-slot=row-action][data-action=b]")!;
        Assert.Null(beta.GetAttribute("title"));
        Assert.Null(beta.GetAttribute("aria-description"));
        // In the menu (the third action does not fit the two buttons): no reason, so no title.
        Assert.Null(row.QuerySelector("[data-slot=content] [role=menuitem]")!.GetAttribute("title"));
    }

    [Fact]
    public void The_reason_of_a_disabled_menu_entry_is_its_title_and_description()
    {
        var actions = new[]
        {
            Act("a"),
            new DataListRowAction<Person> { Key = "b", Label = "Beta", Disabled = _ => true, DisabledReason = _ => "Beta is off." },
        };
        var cut = RenderList(actions, p => p.Add(c => c.MaxVisible, 0));

        var items = Row(cut, 0).QuerySelectorAll("[data-slot=content] [role=menuitem]");
        Assert.Null(items[0].GetAttribute("title"));
        Assert.Equal("Beta is off.", items[1].GetAttribute("title"));
        Assert.Equal("Beta is off.", items[1].GetAttribute("aria-description"));
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
