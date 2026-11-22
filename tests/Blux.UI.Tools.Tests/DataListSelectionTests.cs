using AngleSharp.Dom;
using Blux.UI.Tools.Query;
using Microsoft.AspNetCore.Components.Web;

namespace Blux.UI.Tools.Tests;

/// <summary>Row selection and the actions of a data list: the checkbox column, the select-all box, ranges, scope rules, pruning, bound keys, the bar and the menus.</summary>
public class DataListSelectionTests : BluxToolsTestContext
{
    private static readonly Person[] People = DataListControllerTests.People;

    // Sorted by name: Person 01 is id 1, and so on. A search field on the schema lets a test narrow the result.
    private static readonly DataListSchema<Person> SearchSchema = DataListSchema.For<Person>(s => s
        .RowKey(p => p.Id)
        .RowLabel(p => p.Name)
        .Search(p => p.Name)
        .Field(p => p.Name, f => f.Sortable())
        .Field(p => p.Age, f => f.Sortable())
        .Field(p => p.City)
        .DefaultSort(p => p.Name)
        .DefaultPageSize(10));

    private static DataListAction<Person> Action(string key, Func<DataListActionContext<Person>, Task>? run = null, Action<DataListAction<Person>>? _ = null) =>
        new() { Key = key, Label = key, OnExecute = run ?? (_ => Task.CompletedTask) };

    private static readonly DataListAction<Person>[] Delete = [new() { Key = "delete", Label = "Delete", OnExecute = _ => Task.CompletedTask }];

    private IRenderedComponent<DataListSelectionHarness> RenderList(Action<ComponentParameterCollectionBuilder<DataListSelectionHarness>>? configure = null, bool remote = false) =>
        Render<DataListSelectionHarness>(p =>
        {
            if (remote) p.Add(c => c.Provider, DataListControllerTests.Schema.InMemoryProvider(People));
            configure?.Invoke(p);
        });

    private static IElement Box(IRenderedComponent<DataListSelectionHarness> cut, int id) => cut.Find($"input[data-select=row][data-key='{id}']");

    private static string Status(IRenderedComponent<DataListSelectionHarness> cut) => cut.Find("[data-slot=selection-status]").TextContent;

    private static void Tick(IRenderedComponent<DataListSelectionHarness> cut, int id, bool on = true, bool shift = false)
    {
        var box = Box(cut, id);
        box.Click(new MouseEventArgs { ShiftKey = shift });
        box.Change(on);
    }

    private static void Next(IRenderedComponent<DataListSelectionHarness> cut) => cut.Find("button[aria-label='Page 2']").Click();

    [Fact]
    public void A_list_without_a_bar_or_a_binding_has_no_checkbox_column()
    {
        var cut = Render<DataListSelectionHarness>(p => p.Add(c => c.ShowBar, false));

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.Equal(2, cut.FindAll("thead th").Count);
    }

    [Fact]
    public void A_bar_turns_the_column_on_and_the_boxes_are_named_by_the_row_label_or_the_position()
    {
        var plain = RenderList();
        Assert.Equal("Select all rows on this page", plain.Find("input[data-select=all]").GetAttribute("aria-label"));
        Assert.Equal(10, plain.FindAll("input[data-select=row]").Count);
        Assert.Equal("Select row 1", Box(plain, 1).GetAttribute("aria-label"));
        Assert.Equal("Select row 10", Box(plain, 10).GetAttribute("aria-label"));

        var labelled = RenderList(p => p.Add(c => c.Schema, SearchSchema));
        Assert.Equal("Select Person 01", Box(labelled, 1).GetAttribute("aria-label"));
        // The header cell and the row cells are the first column.
        Assert.Equal(3, labelled.FindAll("thead th").Count);
        Assert.Equal("select", labelled.FindAll("thead th")[0].GetAttribute("data-key"));
    }

    [Fact]
    public void Selectable_false_turns_it_off_even_with_a_bar()
    {
        var cut = RenderList(p => p.Add(c => c.Selectable, false));

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.Empty(cut.FindAll("[data-slot=selection-actions]"));
    }

    [Fact]
    public void A_statically_rendered_list_has_no_selection_and_no_actions()
    {
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Static", false));
        var cut = Render<DataListSelectionHarness>(p =>
        {
            p.Add(c => c.ToolbarActions, Delete);
        });

        Assert.Empty(cut.FindAll("input[type=checkbox]"));
        Assert.Empty(cut.FindAll("[data-slot=selection-actions]"));
        Assert.Empty(cut.FindAll("[data-slot=actions-dropdown]"));
        Assert.Equal(2, cut.FindAll("thead th").Count);
    }

    [Fact]
    public void The_prerender_of_an_interactive_page_keeps_the_column_with_disabled_boxes_so_the_page_does_not_shift()
    {
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Static", false));
        var cut = Render<BxDataList<Person>>(p => p
            .Add(c => c.Schema, DataListControllerTests.Schema)
            .Add(c => c.Items, People)
            .Add(c => c.UrlSync, false)
            .Add(c => c.Selectable, true)
            .SetAssignedRenderMode(new Microsoft.AspNetCore.Components.Web.InteractiveServerRenderMode(prerender: true))
            .Add(c => c.ChildContent, (Microsoft.AspNetCore.Components.RenderFragment)(b =>
            {
                b.OpenComponent<BxDataListTable<Person>>(0);
                b.AddAttribute(1, "ChildContent", (Microsoft.AspNetCore.Components.RenderFragment)(c =>
                {
                    c.OpenComponent<BxDataListColumn<Person>>(0);
                    c.AddAttribute(1, "Field", "name");
                    c.CloseComponent();
                }));
                b.CloseComponent();
            })));

        Assert.Equal(11, cut.FindAll("input[type=checkbox]").Count);
        Assert.All(cut.FindAll("input[type=checkbox]"), box => Assert.True(box.HasAttribute("disabled")));
    }

    [Fact]
    public void Ticking_a_row_shows_the_count_in_a_live_region_and_the_bar()
    {
        var cut = RenderList();

        Assert.Empty(cut.FindAll("[data-slot=selection-bar]"));
        Assert.Equal("", Status(cut));
        Assert.Equal("status", cut.Find("[data-slot=selection-status]").GetAttribute("role"));

        Tick(cut, 3);

        Assert.Equal("1 selected", Status(cut));
        Assert.NotNull(cut.Find("[data-slot=selection-bar]"));
        Assert.True(Box(cut, 3).HasAttribute("checked"));
        Assert.Equal("1 selected", cut.Find("[data-slot=selection-count]").TextContent);

        Tick(cut, 3, on: false);

        Assert.Equal("", Status(cut));
        Assert.Empty(cut.FindAll("[data-slot=selection-bar]"));
    }

    [Fact]
    public void The_header_box_ticks_the_page_and_is_mixed_while_only_some_rows_are_ticked()
    {
        var cut = RenderList();
        var header = () => cut.Find("input[data-select=all]");

        Tick(cut, 2);
        Assert.True(header().HasAttribute("data-indeterminate"));
        Assert.False(header().HasAttribute("checked"));

        header().Change(true);
        Assert.Equal("10 selected", Status(cut));
        Assert.True(header().HasAttribute("checked"));
        Assert.False(header().HasAttribute("data-indeterminate"));

        header().Change(false);
        Assert.Equal("", Status(cut));
        Assert.False(header().HasAttribute("data-indeterminate"));
    }

    [Fact]
    public void Shift_click_ticks_the_range_from_the_last_row_touched_over_the_rows_on_screen()
    {
        var cut = RenderList();

        Tick(cut, 2);
        Tick(cut, 6, shift: true);

        Assert.Equal("5 selected", Status(cut));
        Assert.All(new[] { 2, 3, 4, 5, 6 }, id => Assert.True(Box(cut, id).HasAttribute("checked"), $"row {id}"));
        Assert.False(Box(cut, 7).HasAttribute("checked"));

        // A shift click that unticks clears the range from the row touched last, here 6, to the row clicked.
        Tick(cut, 3, on: false, shift: true);
        Assert.Equal("1 selected", Status(cut));
        Assert.True(Box(cut, 2).HasAttribute("checked"));
        Assert.False(Box(cut, 3).HasAttribute("checked"));
        // The range also goes up the page.
        Tick(cut, 9);
        Tick(cut, 7, shift: true);
        Assert.Equal("4 selected", Status(cut));
    }

    [Fact]
    public void A_shift_click_without_an_anchor_on_screen_ticks_only_the_row()
    {
        var cut = RenderList();

        Tick(cut, 2);
        Next(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[data-select=row][data-key='12']")));
        Tick(cut, 14, shift: true);

        Assert.Equal("2 selected", Status(cut));
    }

    [Fact]
    public void A_list_in_memory_keeps_the_selection_across_pages()
    {
        var cut = RenderList();

        Tick(cut, 1);
        Tick(cut, 2);
        Next(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[data-select=row][data-key='12']")));

        Assert.Equal("2 selected", Status(cut));
        Tick(cut, 12);
        Assert.Equal("3 selected", Status(cut));
    }

    [Fact]
    public void A_list_over_a_provider_keeps_only_the_rows_of_the_page()
    {
        var cut = RenderList(remote: true);

        Tick(cut, 1);
        Tick(cut, 2);
        Assert.Equal("2 selected", Status(cut));
        Next(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[data-select=row][data-key='12']")));

        Assert.Equal("", Status(cut));
        Assert.Empty(cut.FindAll("[data-slot=selection-bar]"));
    }

    [Fact]
    public void SelectionScope_page_makes_a_list_in_memory_drop_the_rows_that_leave_the_page()
    {
        var cut = RenderList(p => p.Add(c => c.SelectionScope, DataListSelectionScope.Page));

        Tick(cut, 1);
        Next(cut);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[data-select=row][data-key='12']")));

        Assert.Equal("", Status(cut));
    }

    [Fact]
    public async Task A_list_in_memory_keeps_only_the_selected_rows_the_new_search_still_matches()
    {
        var cut = RenderList(p => p.Add(c => c.Schema, SearchSchema));
        var state = SearchSchema.DefaultState;

        Tick(cut, 1);
        Tick(cut, 2);
        Tick(cut, 3);
        Assert.Equal("3 selected", Status(cut));

        // "Person 02" matches one row: the other two leave the selection.
        cut.Render(p => p.Add(c => c.State, state with { Search = "Person 02" }));

        await cut.WaitForAssertionAsync(() => Assert.Equal("1 selected", Status(cut)));
        Assert.True(Box(cut, 2).HasAttribute("checked"));
    }

    [Fact]
    public async Task A_search_that_matches_none_of_the_selected_rows_clears_the_selection_and_the_bar()
    {
        var cut = RenderList(p => p.Add(c => c.Schema, SearchSchema));

        Tick(cut, 1);
        cut.Render(p => p.Add(c => c.State, SearchSchema.DefaultState with { Search = "Person 4" }));

        await cut.WaitForAssertionAsync(() => Assert.Empty(cut.FindAll("[data-slot=selection-bar]")));
        Assert.Equal("", Status(cut));
    }

    [Fact]
    public void A_sort_does_not_change_a_selection_that_still_matches()
    {
        var cut = RenderList();

        Tick(cut, 1);
        cut.Find("thead th button[aria-label^='Sort by Age']").Click();

        cut.WaitForAssertion(() => Assert.Equal("1 selected", Status(cut)));
    }

    [Fact]
    public void Clear_selection_unticks_everything()
    {
        var cut = RenderList();

        Tick(cut, 1);
        Tick(cut, 2);
        cut.Find("[data-slot=clear-selection]").Click();

        Assert.Equal("", Status(cut));
        Assert.False(Box(cut, 1).HasAttribute("checked"));
    }

    [Fact]
    public void A_bound_set_of_keys_is_reported_when_the_user_ticks_and_set_from_outside()
    {
        IReadOnlySet<string>? reported = null;
        var cut = RenderList(p =>
        {
            p.Add(c => c.SelectedKeys, new HashSet<string> { "4" });
            p.Add(c => c.SelectedKeysChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<IReadOnlySet<string>>(this, set => reported = set));
        });

        Assert.Equal("1 selected", Status(cut));
        Assert.True(Box(cut, 4).HasAttribute("checked"));

        Tick(cut, 5);
        Assert.Equal(new[] { "4", "5" }, reported!.Order());

        cut.Render(p => p.Add(c => c.SelectedKeys, new HashSet<string> { "7", "8", "9" }));
        Assert.Equal("3 selected", Status(cut));
        Assert.True(Box(cut, 8).HasAttribute("checked"));
        Assert.False(Box(cut, 4).HasAttribute("checked"));
    }

    [Fact]
    public void Bound_keys_the_list_cannot_find_are_dropped_and_reported()
    {
        IReadOnlySet<string>? reported = null;
        RenderList(p =>
        {
            p.Add(c => c.SelectedKeys, new HashSet<string> { "4", "999" });
            p.Add(c => c.SelectedKeysChanged, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<IReadOnlySet<string>>(this, set => reported = set));
        });

        Assert.Equal(new[] { "4" }, reported!.Order());
    }

    [Fact]
    public void Binding_keys_turns_selection_on_without_a_bar()
    {
        var cut = RenderList(p =>
        {
            p.Add(c => c.ShowBar, false);
            p.Add(c => c.SelectedKeys, new HashSet<string>());
        });

        Assert.Equal(10, cut.FindAll("input[data-select=row]").Count);
    }

    [Fact]
    public async Task An_action_gets_the_ticked_rows_and_the_request_and_the_selection_survives_by_default()
    {
        DataListActionContext<Person>? seen = null;
        var cut = RenderList(p => p.Add(c => c.Actions, [Action("export", c => { seen = c; return Task.CompletedTask; })]));

        Tick(cut, 3);
        Tick(cut, 5);
        cut.Find("[data-action=export]").Click();
        await cut.WaitForAssertionAsync(() => Assert.NotNull(seen));

        Assert.Equal(DataListActionScope.Selection, seen!.Scope);
        Assert.Equal(new[] { "3", "5" }, seen.SelectedKeys.Order());
        Assert.Equal(new[] { 3, 5 }, seen.SelectedItems.Select(p => p.Id).Order());
        Assert.Equal(45, seen.MatchingCount);
        Assert.IsType<DataListPaging.None>(seen.Request!.Paging);
        Assert.Empty(seen.Request.Facets);
        Assert.Equal("2 selected", Status(cut));
    }

    [Fact]
    public async Task Clearing_on_success_unticks_and_refreshing_loads_the_list_again()
    {
        var calls = 0;
        var provider = DataListControllerTests.Schema.InMemoryProvider(People);
        DataListProvider<Person> counting = (request, token) =>
        {
            calls++;
            return provider(request, token);
        };
        var cut = RenderList(p =>
        {
            p.Add(c => c.Provider, counting);
            p.Add(c => c.Actions, [new DataListAction<Person> { Key = "archive", Label = "Archive", ClearSelection = DataListSelectionClear.Success, RefreshOnSuccess = true, OnExecute = _ => Task.CompletedTask }]);
        });
        var before = calls;

        Tick(cut, 1);
        cut.Find("[data-action=archive]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal("", Status(cut)));
        await cut.WaitForAssertionAsync(() => Assert.True(calls > before));
    }

    [Fact]
    public async Task Clearing_on_trigger_unticks_before_the_action_runs_and_the_action_still_gets_the_rows()
    {
        IReadOnlyList<string>? keys = null;
        var cut = RenderList(p => p.Add(c => c.Actions,
            [new DataListAction<Person> { Key = "move", Label = "Move", ClearSelection = DataListSelectionClear.Trigger, OnExecute = c => { keys = c.SelectedKeys; return Task.CompletedTask; } }]));

        Tick(cut, 2);
        cut.Find("[data-action=move]").Click();

        await cut.WaitForAssertionAsync(() => Assert.NotNull(keys));
        Assert.Equal(new[] { "2" }, keys);
        Assert.Equal("", Status(cut));
    }

    [Fact]
    public async Task A_failing_action_keeps_the_selection_does_not_refresh_and_reports_the_error()
    {
        DataListActionFailure<Person>? failure = null;
        var boom = new InvalidOperationException("boom");
        var cut = RenderList(p =>
        {
            p.Add(c => c.Actions, [new DataListAction<Person> { Key = "bad", Label = "Bad", ClearSelection = DataListSelectionClear.Success, OnExecute = _ => throw boom }]);
            p.Add(c => c.OnError, Microsoft.AspNetCore.Components.EventCallback.Factory.Create<DataListActionFailure<Person>>(this, f => failure = f));
        });

        Tick(cut, 1);
        cut.Find("[data-action=bad]").Click();

        await cut.WaitForAssertionAsync(() => Assert.NotNull(failure));
        Assert.Same(boom, failure!.Exception);
        Assert.Equal("bad", failure.Action.Key);
        Assert.Equal("1 selected", Status(cut));
        Assert.False(cut.Find("[data-action=bad]").HasAttribute("disabled"));
    }

    [Fact]
    public async Task A_running_action_shows_a_spinner_and_cannot_start_twice()
    {
        var gate = new TaskCompletionSource();
        var runs = 0;
        var cut = RenderList(p => p.Add(c => c.Actions, [Action("slow", async _ => { runs++; await gate.Task; })]));

        Tick(cut, 1);
        cut.Find("[data-action=slow]").Click();

        await cut.WaitForAssertionAsync(() =>
        {
            var button = cut.Find("[data-action=slow]");
            Assert.Equal("true", button.GetAttribute("aria-busy"));
            Assert.True(button.HasAttribute("disabled"));
        });
        gate.SetResult();
        await cut.WaitForAssertionAsync(() => Assert.Null(cut.Find("[data-action=slow]").GetAttribute("aria-busy")));
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task A_confirmation_runs_the_action_only_after_the_confirm_button()
    {
        var runs = 0;
        var action = new DataListAction<Person>
        {
            Key = "delete",
            Label = "Delete",
            Color = BxColor.Error,
            Confirm = new DataListActionConfirm<Person> { Title = "Delete people", Description = c => $"{c.TargetCount} people will be removed.", ConfirmLabel = "Remove" },
            OnExecute = _ => { runs++; return Task.CompletedTask; },
        };
        var cut = RenderList(p => p.Add(c => c.Actions, [action]));

        Tick(cut, 1);
        Tick(cut, 2);
        cut.Find("[data-action=delete]").Click();

        var dialog = cut.Find("[data-slot=action-dialog]");
        Assert.Equal(0, runs);
        Assert.Contains("Delete people", dialog.TextContent);
        Assert.Contains("2 people will be removed.", dialog.TextContent);
        Assert.Equal("Remove", cut.Find("[data-slot=action-confirm]").TextContent.Trim());

        cut.Find("[data-slot=action-confirm]").Click();

        await cut.WaitForAssertionAsync(() => Assert.Equal(1, runs));
    }

    [Fact]
    public void Cancelling_a_confirmation_runs_nothing()
    {
        var runs = 0;
        var action = new DataListAction<Person> { Key = "delete", Label = "Delete", Confirm = new DataListActionConfirm<Person> { Title = "Sure?" }, OnExecute = _ => { runs++; return Task.CompletedTask; } };
        var cut = RenderList(p => p.Add(c => c.Actions, [action]));

        Tick(cut, 1);
        cut.Find("[data-action=delete]").Click();

        // The dialog is on screen and the action waits: cancelling closes it in the browser, with no round trip.
        Assert.NotNull(cut.Find("[data-slot=action-cancel][data-bx-close]"));
        Assert.Equal(0, runs);
        Assert.Equal("1 selected", Status(cut));
    }

    [Fact]
    public void Visible_and_Disabled_read_the_context_of_the_selection()
    {
        var cut = RenderList(p => p.Add(c => c.Actions,
        [
            new DataListAction<Person> { Key = "many", Label = "Many", Visible = c => c.SelectedItems.Count >= 2, OnExecute = _ => Task.CompletedTask },
            new DataListAction<Person> { Key = "one", Label = "One", Disabled = c => c.SelectedItems.Count != 1, OnExecute = _ => Task.CompletedTask },
        ]));

        Tick(cut, 1);
        Assert.Empty(cut.FindAll("[data-action=many]"));
        Assert.False(cut.Find("[data-action=one]").HasAttribute("disabled"));

        Tick(cut, 2);
        Assert.NotNull(cut.Find("[data-action=many]"));
        Assert.True(cut.Find("[data-action=one]").HasAttribute("disabled"));
    }

    [Fact]
    public void Actions_past_MaxVisible_go_into_a_more_menu()
    {
        var cut = RenderList(p =>
        {
            p.Add(c => c.MaxVisible, 1);
            p.Add(c => c.Actions, [Action("a"), Action("b"), Action("c")]);
        });

        Tick(cut, 1);

        Assert.Single(cut.FindAll("[data-slot=selection-bar] [data-slot=action]"));
        Assert.NotNull(cut.Find("[data-slot=actions-more]"));
        Assert.Equal(2, cut.FindAll("[data-slot=selection-bar] [role=menuitem]").Count);
    }

    [Fact]
    public void The_bar_offers_every_result_when_the_total_is_known_and_switches_back()
    {
        var cut = RenderList();

        Tick(cut, 1);
        Assert.Equal("Select all 45 results", cut.Find("[data-slot=select-all-results]").TextContent.Trim());

        cut.Find("[data-slot=select-all-results]").Click();

        Assert.Equal("All 45 results selected", Status(cut));
        Assert.Equal("Only the 1 selected", cut.Find("[data-slot=select-only-selected]").TextContent.Trim());

        cut.Find("[data-slot=select-only-selected]").Click();

        Assert.Equal("1 selected", Status(cut));
    }

    [Fact]
    public void The_bar_does_not_offer_every_result_when_the_total_is_unknown()
    {
        var inner = DataListControllerTests.Schema.InMemoryProvider(People);
        DataListProvider<Person> noTotal = async (request, token) => (await inner(request, token)) with { TotalCount = null };
        var cut = RenderList(p =>
        {
            p.Add(c => c.Provider, noTotal);
        });

        Tick(cut, 1);

        Assert.Empty(cut.FindAll("[data-slot=select-all-results]"));
    }

    [Fact]
    public async Task An_action_on_every_result_of_a_list_in_memory_gets_every_matching_row()
    {
        DataListActionContext<Person>? seen = null;
        var cut = RenderList(p =>
        {
            p.Add(c => c.Schema, SearchSchema);
            p.Add(c => c.State, SearchSchema.DefaultState with { Search = "1" });
            p.Add(c => c.Actions, [Action("export", c => { seen = c; return Task.CompletedTask; })]);
        });
        await cut.WaitForAssertionAsync(() => Assert.NotEmpty(cut.FindAll("input[data-select=row]")));
        // "1" matches Person 01, 10 to 19, 21, 31 and 41: 14 rows, 10 on the first page.
        Tick(cut, 1);

        cut.Find("[data-slot=select-all-results]").Click();
        cut.Find("[data-action=export]").Click();

        await cut.WaitForAssertionAsync(() => Assert.NotNull(seen));
        Assert.Equal(DataListActionScope.All, seen!.Scope);
        Assert.Equal(14, seen.SelectedItems.Count);
        Assert.Equal(14, seen.SelectedKeys.Count);
        Assert.Equal(14, seen.MatchingCount);
        Assert.Equal("1", seen.Request!.Search!.Term);
    }

    [Fact]
    public async Task An_action_on_every_result_over_a_provider_gets_the_request_and_no_rows()
    {
        DataListActionContext<Person>? seen = null;
        var cut = RenderList(p => p.Add(c => c.Actions, [Action("purge", c => { seen = c; return Task.CompletedTask; })]), remote: true);

        Tick(cut, 1);
        cut.Find("[data-slot=select-all-results]").Click();
        cut.Find("[data-action=purge]").Click();

        await cut.WaitForAssertionAsync(() => Assert.NotNull(seen));
        Assert.Equal(DataListActionScope.All, seen!.Scope);
        Assert.Empty(seen.SelectedItems);
        Assert.Equal(45, seen.MatchingCount);
        Assert.IsType<DataListPaging.None>(seen.Request!.Paging);
    }

    [Fact]
    public async Task The_every_result_scope_does_not_survive_a_change_of_the_query()
    {
        var cut = RenderList(p => p.Add(c => c.Schema, SearchSchema));

        Tick(cut, 1);
        cut.Find("[data-slot=select-all-results]").Click();
        Assert.Equal("All 45 results selected", Status(cut));

        // The row is still selected under the new search, but "every result" now means other rows: the scope goes back to the selection.
        cut.Render(p => p.Add(c => c.State, SearchSchema.DefaultState with { Search = "Person 0" }));

        await cut.WaitForAssertionAsync(() => Assert.Equal("1 selected", Status(cut)));
    }

    [Fact]
    public void Ticking_a_row_after_choosing_every_result_goes_back_to_the_selection()
    {
        var cut = RenderList();

        Tick(cut, 1);
        cut.Find("[data-slot=select-all-results]").Click();
        Tick(cut, 2);

        Assert.Equal("2 selected", Status(cut));
    }

    [Fact]
    public async Task The_toolbar_menu_lists_its_actions_and_runs_one_without_a_selection()
    {
        DataListActionContext<Person>? seen = null;
        var cut = RenderList(p => p.Add(c => c.ToolbarActions, [Action("export", c => { seen = c; return Task.CompletedTask; }), Action("import")]));

        Assert.Equal("Actions", cut.Find("[data-slot=actions-trigger]").TextContent.Trim());
        var items = cut.FindAll("[data-slot=actions-dropdown] [role=menuitem]");
        Assert.Equal(["export", "import"], items.Select(i => i.TextContent.Trim()));

        items[0].Click();

        await cut.WaitForAssertionAsync(() => Assert.NotNull(seen));
        Assert.Empty(seen!.SelectedItems);
        Assert.Equal(DataListActionScope.Selection, seen.Scope);
    }

    [Fact]
    public void The_toolbar_menu_is_not_drawn_without_visible_actions()
    {
        var cut = RenderList(p => p.Add(c => c.ToolbarActions, [new DataListAction<Person> { Key = "x", Label = "X", Visible = _ => false }]));

        Assert.Empty(cut.FindAll("[data-slot=actions-dropdown]"));
    }

    [Fact]
    public void A_selection_without_a_row_key_says_what_is_missing()
    {
        var noKey = DataListSchema.For<Person>(s => s.Field(p => p.Name, f => f.Sortable()).DefaultSort(p => p.Name));

        var error = Assert.ThrowsAny<Exception>(() => RenderList(p => p.Add(c => c.Schema, noKey)));

        Assert.Contains("RowKey", error.ToString());
    }

    [Fact]
    public void The_messages_follow_the_locale()
    {
        var culture = System.Globalization.CultureInfo.CurrentUICulture;
        System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
        try
        {
            var cut = RenderList();

            Assert.Equal("Sélectionner la ligne 1", Box(cut, 1).GetAttribute("aria-label"));
            Assert.Equal("Sélectionner toutes les lignes de cette page", cut.Find("input[data-select=all]").GetAttribute("aria-label"));
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentUICulture = culture;
        }
    }

    [Fact]
    public void TargetCount_is_the_ticked_rows_and_under_every_result_the_matching_count_in_the_confirmation_and_the_predicates()
    {
        var seen = new List<(DataListActionScope Scope, long Target, int Items)>();
        var action = new DataListAction<Person>
        {
            Key = "delete",
            Label = "Delete",
            Confirm = new DataListActionConfirm<Person> { Title = "Delete", Description = c => $"{c.TargetCount} people will be removed." },
            Visible = c => { seen.Add((c.Scope, c.TargetCount, c.SelectedItems.Count)); return true; },
            Disabled = c => c.TargetCount > 40,
            OnExecute = _ => Task.CompletedTask,
        };
        var cut = RenderList(p => p.Add(c => c.Actions, [action]));

        Tick(cut, 1);
        Tick(cut, 2);
        cut.Find("[data-action=delete]").Click();
        Assert.Contains("2 people will be removed.", cut.Find("[data-slot=action-dialog]").TextContent);
        Assert.False(cut.Find("[data-action=delete]").HasAttribute("disabled"));

        cut.Find("[data-slot=select-all-results]").Click();

        // The predicates see the scope and 45, the confirmation says 45, whatever the two ticked rows are.
        Assert.Contains((DataListActionScope.All, 45L, 2), seen);
        Assert.True(cut.Find("[data-action=delete]").HasAttribute("disabled"));
        Assert.Contains("45 people will be removed.", cut.Find("[data-slot=action-dialog]").TextContent);
    }

    [Fact]
    public void TargetCount_under_every_result_over_a_provider_is_the_matching_count()
    {
        long target = -1;
        var action = new DataListAction<Person>
        {
            Key = "purge",
            Label = "Purge",
            Confirm = new DataListActionConfirm<Person> { Title = "Purge", Description = c => $"{c.TargetCount} will go." },
            Visible = c => { target = c.TargetCount; return true; },
            OnExecute = _ => Task.CompletedTask,
        };
        var cut = RenderList(p => p.Add(c => c.Actions, [action]), remote: true);

        Tick(cut, 1);
        cut.Find("[data-slot=select-all-results]").Click();
        cut.Find("[data-action=purge]").Click();

        Assert.Equal(45, target);
        Assert.Contains("45 will go.", cut.Find("[data-slot=action-dialog]").TextContent);
    }

    [Fact]
    public async Task Clearing_on_success_unticks_only_the_rows_the_action_targeted()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderList(p => p.Add(c => c.Actions,
            [new DataListAction<Person> { Key = "move", Label = "Move", ClearSelection = DataListSelectionClear.Success, OnExecute = _ => gate.Task }]));

        Tick(cut, 1);
        Tick(cut, 2);
        cut.Find("[data-action=move]").Click();
        // Ticked while the action runs: not part of it.
        Tick(cut, 5);
        gate.SetResult();

        await cut.WaitForAssertionAsync(() => Assert.Equal("1 selected", Status(cut)));
        Assert.True(Box(cut, 5).HasAttribute("checked"));
        Assert.False(Box(cut, 1).HasAttribute("checked"));
        Assert.False(Box(cut, 2).HasAttribute("checked"));
    }
}
