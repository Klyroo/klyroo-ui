using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// URL state (QueryState) on the "URL state" guide of the playground, in the four render modes: typing replaces the history entry, a filter
/// pushes one, Back restores the previous state and the page, a typed URL loads its state, and static links carry the state.
/// </summary>
public class QueryStateTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task OpenState(string mode, string query = "")
    {
        await Page.GotoAsync($"/{mode}/docs/url-state{query}");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover') && document.title.length > 0");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
    }

    private ILocator Search => Page.GetByTestId("url-q");

    private ILocator Items => Page.GetByTestId("url-list").Locator("li");

    private ILocator Status(string name) => Page.GetByTestId($"url-status-{name}");

    private Task<int> HistoryLength() => Page.EvaluateAsync<int>("() => history.length");

    private string Query => new Uri(Page.Url).Query;

    private Task ExpectQuery(string pattern) => Assertions.Expect(Page.GetByTestId("url-query")).ToHaveTextAsync(new Regex(pattern));

    private async Task WaitForQuery(string expected)
    {
        // The address bar, not the text of the page: this is what a user shares.
        await Page.WaitForURLAsync(url => new Uri(url).Query == expected, new() { Timeout = 15_000 });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_typed_url_loads_its_state(string mode)
    {
        await OpenState(mode, "?q=review&status=open&desc=true&utm_source=mail");

        await Assertions.Expect(Search).ToHaveValueAsync("review");
        await Assertions.Expect(Items).ToHaveCountAsync(1);
        await Assertions.Expect(Items.First).ToContainTextAsync("Review the pull request");
        await Assertions.Expect(Status("open")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(Page.GetByTestId("url-sort")).ToHaveTextAsync("Z to A");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_page_beyond_the_end_and_a_bad_value_fall_back_without_an_error(string mode)
    {
        await OpenState(mode, "?page=99&status=nonsense&desc=maybe");

        // The status and the sort are the defaults; the page is clamped when it is shown.
        await Assertions.Expect(Status("all")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(Page.GetByTestId("url-sort")).ToHaveTextAsync("A to Z");
        await Assertions.Expect(Items.First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Static_links_carry_the_state_and_keep_the_other_parameters()
    {
        await OpenState("ssr", "?utm_source=mail");
        var open = await Status("open").GetAttributeAsync("href");

        Assert.Equal("/ssr/docs/url-state?utm_source=mail&status=open", open);
        await Status("open").ClickAsync();

        await WaitForQuery("?utm_source=mail&status=open");
        await Assertions.Expect(Status("open")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(Items).ToHaveCountAsync(4);
        // The next link starts from the new state: page 2 of the open tasks.
        await Page.GetByTestId("url-next").ClickAsync();
        await WaitForQuery("?utm_source=mail&status=open&page=2");
        await Assertions.Expect(Items).ToHaveCountAsync(2);
        await Assertions.Expect(Page.GetByTestId("url-page")).ToHaveTextAsync("2");
    }

    [Fact]
    public async Task Static_back_restores_the_previous_link_state()
    {
        await OpenState("ssr");
        await Status("done").ClickAsync();
        await WaitForQuery("?status=done");

        await Page.GoBackAsync();

        await WaitForQuery("");
        await Assertions.Expect(Status("all")).ToHaveAttributeAsync("aria-current", "true");
    }

    [Fact]
    public async Task The_static_search_form_submits_the_search_and_the_other_keys()
    {
        await OpenState("ssr", "?status=open");

        await Search.FillAsync("plan");
        await Page.GetByTestId("url-form").GetByRole(AriaRole.Button, new() { Name = "Search", Exact = true }).ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).Query.Contains("q=plan"));
        Assert.Contains("status=open", Query);
        await Assertions.Expect(Items).ToHaveCountAsync(1);
        await Assertions.Expect(Items.First).ToContainTextAsync("Plan the next sprint");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Typing_replaces_the_history_entry_instead_of_adding_one(string mode)
    {
        await OpenState(mode, "?utm_source=mail");
        var before = await HistoryLength();

        await Search.PressSequentiallyAsync("revi", new() { Delay = 40 });
        await WaitForQuery("?utm_source=mail&q=revi");
        await Search.PressSequentiallyAsync("ew", new() { Delay = 40 });
        await WaitForQuery("?utm_source=mail&q=review");

        Assert.Equal(before, await HistoryLength());
        await Assertions.Expect(Items).ToHaveCountAsync(1);
        await ExpectQuery(@"q=review");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Clearing_the_search_removes_its_key(string mode)
    {
        await OpenState(mode, "?q=review&status=open");

        await Search.FillAsync("");

        await WaitForQuery("?status=open");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Clicking_a_filter_pushes_an_entry_and_back_restores_the_value_and_the_page(string mode)
    {
        await OpenState(mode);
        var before = await HistoryLength();

        await Status("done").ClickAsync();

        await WaitForQuery("?status=done");
        Assert.Equal(before + 1, await HistoryLength());
        await Assertions.Expect(Status("done")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(Items.First).ToContainTextAsync("Archive the board");

        await Page.GoBackAsync();

        await WaitForQuery("");
        await Assertions.Expect(Status("all")).ToHaveAttributeAsync("aria-current", "true");
        await Assertions.Expect(Status("done")).Not.ToHaveAttributeAsync("aria-current", new Regex(".*"));
        await Assertions.Expect(Page.GetByTestId("url-page")).ToHaveTextAsync("1");
        // Forward goes back to the filter.
        await Page.GoForwardAsync();
        await WaitForQuery("?status=done");
        await Assertions.Expect(Status("done")).ToHaveAttributeAsync("aria-current", "true");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_button_that_writes_the_state_pushes_one_entry_and_back_undoes_it(string mode)
    {
        await OpenState(mode, "?q=the");
        var before = await HistoryLength();
        var sort = Page.GetByTestId("url-sort");
        var first = (await Items.First.TextContentAsync())!;

        await sort.ClickAsync();

        await WaitForQuery("?q=the&desc=true");
        Assert.Equal(before + 1, await HistoryLength());
        await Assertions.Expect(sort).ToHaveTextAsync("Z to A");
        Assert.NotEqual(first, await Items.First.TextContentAsync());

        await Page.GoBackAsync();

        await WaitForQuery("?q=the");
        await Assertions.Expect(sort).ToHaveTextAsync("A to Z");
        await Assertions.Expect(Items.First).ToHaveTextAsync(first);
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Back_after_typing_a_search_leaves_the_page_it_came_from_not_the_letters(string mode)
    {
        await OpenState(mode);
        await Status("open").ClickAsync();
        await WaitForQuery("?status=open");

        await Search.PressSequentiallyAsync("plan", new() { Delay = 40 });
        await WaitForQuery("?status=open&q=plan");
        await Page.GoBackAsync();

        // The typed letters were one replaced entry, so Back reaches the state before the filter click, in one step.
        await WaitForQuery("");
        await Assertions.Expect(Search).ToHaveValueAsync("");
        await Assertions.Expect(Status("all")).ToHaveAttributeAsync("aria-current", "true");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Back_and_forward_across_typed_text_restore_the_search_box(string mode)
    {
        await OpenState(mode, "?q=review");
        await Status("open").ClickAsync();
        await WaitForQuery("?q=review&status=open");

        await Page.GoBackAsync();

        await WaitForQuery("?q=review");
        await Assertions.Expect(Search).ToHaveValueAsync("review");
        await Page.GoForwardAsync();
        await WaitForQuery("?q=review&status=open");
        await Assertions.Expect(Search).ToHaveValueAsync("review");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_change_of_filter_goes_back_to_the_first_page_in_one_entry(string mode)
    {
        await OpenState(mode);
        await Page.GetByTestId("url-next").ClickAsync();
        await WaitForQuery("?page=2");
        var before = await HistoryLength();

        await Status("open").ClickAsync();

        await WaitForQuery("?status=open");
        Assert.Equal(before + 1, await HistoryLength());
        await Assertions.Expect(Page.GetByTestId("url-page")).ToHaveTextAsync("1");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task SupplyParameterFromQuery_reads_the_same_url_the_state_writes(string mode)
    {
        await OpenState(mode, "?q=abc");
        var supplied = Page.GetByTestId("url-supplied");
        await Assertions.Expect(supplied).ToHaveTextAsync("abc");
        await Status("open").ClickAsync();
        await WaitForQuery("?q=abc&status=open");

        await Search.FillAsync("abcd");

        await WaitForQuery("?q=abcd&status=open");
        await Assertions.Expect(supplied).ToHaveTextAsync("abcd");
        // Back leaves the replaced entry for the one before it.
        await Page.GoBackAsync();
        await Assertions.Expect(supplied).ToHaveTextAsync("abc");
        await Assertions.Expect(Search).ToHaveValueAsync("abc");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_prerender_that_normalizes_the_state_does_not_redirect_the_request(string mode)
    {
        // The page asks for a page past the end and the component normalizes it in OnInitialized, during the prerender and the static render.
        // The playground runs with BlazorDisableThrowNavigationException, where a NavigateTo of the prerender is a real redirect.
        var response = await Page.GotoAsync($"/{mode}/docs/url-state?page=99&utm_source=mail");

        Assert.NotNull(response);
        Assert.Equal(200, response.Status);
        Assert.Null(response.Request.RedirectedFrom);
        if (mode == "ssr")
        {
            // Static rendering cannot write: the URL stays as it was typed and the page shows the last page.
            await Page.WaitForTimeoutAsync(500);
            Assert.Equal("?page=99&utm_source=mail", Query);
            return;
        }
        await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        var length = await HistoryLength();
        // Interactive, the same component replaces the entry.
        await WaitForQuery("?page=3&utm_source=mail");
        Assert.Equal(length, await HistoryLength());
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task A_write_followed_by_a_navigation_to_another_page_in_one_handler_does_not_send_the_user_back(string mode)
    {
        await OpenState(mode);

        await Page.GetByTestId("url-leave").ClickAsync();

        await Page.WaitForURLAsync(url => new Uri(url).AbsolutePath.EndsWith("/docs"), new() { Timeout = 15_000 });
        // The write was meant for the page being left: after a while the address is still the other page's, with no state of ours.
        await Page.WaitForTimeoutAsync(1500);
        Assert.EndsWith("/docs", new Uri(Page.Url).AbsolutePath);
        Assert.DoesNotContain("q=left", Query);
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_guide_has_no_accessibility_violations_in_light_and_dark(string mode)
    {
        await OpenState(mode, "?status=open");
        await Assertions.Expect(Items.First).ToBeVisibleAsync();

        await AssertNoAccessibilityViolations();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.WaitForFunctionAsync("() => document.getAnimations().length === 0");

        await AssertNoAccessibilityViolations();
    }
}
