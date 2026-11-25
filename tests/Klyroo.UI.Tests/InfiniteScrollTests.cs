using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

public class InfiniteScrollTests : KlyrooTestContext
{
    [Fact]
    public void Renders_the_sentinel_the_manual_button_and_a_polite_status()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { })));

        var host = cut.Find("kly-infinite-scroll");
        Assert.True(host.HasAttribute("data-has-more"));
        Assert.False(host.HasAttribute("data-loading"));
        Assert.Equal("0px 0px 200px 0px", host.GetAttribute("data-root-margin"));
        Assert.False(host.HasAttribute("data-root"));
        // The browser side does nothing without this marker, which only a listening, interactive component renders.
        Assert.True(host.HasAttribute("data-kly-seq"));

        var sentinel = cut.Find("[data-slot=sentinel]");
        Assert.Equal("true", sentinel.GetAttribute("aria-hidden"));
        var button = cut.Find("button[data-slot=trigger]");
        Assert.Equal("Load more", button.TextContent.Trim());
        Assert.False(button.HasAttribute("disabled"));
        var status = cut.Find("[data-slot=status]");
        Assert.Equal("status", status.GetAttribute("role"));
        Assert.Equal("polite", status.GetAttribute("aria-live"));
        Assert.Contains("sr-only", status.ClassName);
        Assert.Equal("", status.TextContent);
    }

    [Fact]
    public void Root_and_RootMargin_are_passed_to_the_browser()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.Root, "#feed").Add(s => s.RootMargin, "100px 0px 0px 0px").Add(s => s.Label, "More events"));

        var host = cut.Find("kly-infinite-scroll");
        Assert.Equal("#feed", host.GetAttribute("data-root"));
        Assert.Equal("100px 0px 0px 0px", host.GetAttribute("data-root-margin"));
        Assert.Equal("More events", cut.Find("[data-slot=trigger]").TextContent.Trim());
    }

    [Fact]
    public void Without_a_handler_the_button_is_a_link_to_Href_or_inert()
    {
        var link = Render<KlyInfiniteScroll>(p => p.Add(s => s.Href, "/items?page=2"));
        Assert.False(link.Find("kly-infinite-scroll").HasAttribute("data-kly-seq"));
        var anchor = link.Find("a[data-slot=trigger]");
        Assert.Equal("/items?page=2", anchor.GetAttribute("href"));
        Assert.Equal("Load more", anchor.TextContent.Trim());

        var inert = Render<KlyInfiniteScroll>();
        Assert.True(inert.Find("button[data-slot=trigger]").HasAttribute("disabled"));
    }

    [Fact]
    public void With_a_handler_and_a_runtime_the_control_is_a_button_even_when_Href_is_set()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { })).Add(s => s.Href, "?page=2"));

        Assert.Empty(cut.FindAll("a[data-slot=trigger]"));
        Assert.Equal("button", cut.Find("[data-slot=trigger]").LocalName);
    }

    [Fact]
    public void Static_rendering_has_no_handler_so_the_link_works_without_a_runtime()
    {
        SetRendererInfo(new RendererInfo("Static", false));
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { })).Add(s => s.Href, "?page=2"));

        Assert.False(cut.Find("kly-infinite-scroll").HasAttribute("data-kly-seq"));
        Assert.Equal("?page=2", cut.Find("a[data-slot=trigger]").GetAttribute("href"));
    }

    [Fact]
    public void Loading_is_announced_and_shown_without_disabling_the_button()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.Loading, true).Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { })));

        var host = cut.Find("kly-infinite-scroll");
        Assert.True(host.HasAttribute("data-loading"));
        // aria-busy on the element would hide its own live region: the status announces, the consumer marks the list busy.
        Assert.False(host.HasAttribute("aria-busy"));
        Assert.Equal("Loading more...", cut.Find("[data-slot=status]").TextContent);
        var button = cut.Find("[data-slot=trigger]");
        // Still focusable: a keyboard user pressing it does not lose the place.
        Assert.False(button.HasAttribute("disabled"));
        Assert.Equal("true", button.GetAttribute("aria-disabled"));
        // The spinner stops for people who asked for less motion.
        var spinner = button.QuerySelector("svg");
        Assert.NotNull(spinner);
        Assert.Contains("motion-safe:animate-spin", spinner.ClassName ?? spinner.GetAttribute("class"));
    }

    [Fact]
    public void The_end_removes_the_sentinel_and_the_button_and_is_announced()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.HasMore, false));

        Assert.Empty(cut.FindAll("[data-slot=sentinel]"));
        Assert.Empty(cut.FindAll("[data-slot=trigger]"));
        Assert.False(cut.Find("kly-infinite-scroll").HasAttribute("data-has-more"));
        Assert.Equal("No more items", cut.Find("[data-slot=status]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=end]"));
        // Where the focus goes when the button that had it disappears: a plain element, not the live region.
        var marker = cut.Find("[data-slot=marker]");
        Assert.Equal("-1", marker.GetAttribute("tabindex"));
        Assert.DoesNotContain(marker.Attributes, a => a.Name is "role" or "aria-live");
        Assert.Equal("", marker.TextContent);

        cut.Render(p => p.Add(s => s.ShowEnd, true));
        var end = cut.Find("[data-slot=end]");
        Assert.Equal("No more items", end.TextContent);
        // The status carries it for assistive technology; the visible copy is not read twice.
        Assert.Equal("true", end.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void The_browser_report_calls_OnLoadMore_once_while_it_runs_and_renders_the_end_of_the_load()
    {
        var calls = 0;
        var release = new TaskCompletionSource();
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, async () => { calls++; await release.Task; })));
        var host = cut.Find("kly-infinite-scroll");
        Assert.Equal("0", host.GetAttribute("data-loads"));

        _ = host.TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 1 });
        // The load is running and shown as such, even though the parent never set Loading.
        cut.WaitForAssertion(() => Assert.True(cut.Find("kly-infinite-scroll").HasAttribute("data-loading")));
        _ = cut.Find("kly-infinite-scroll").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 2 });

        Assert.Equal(1, calls);

        release.SetResult();
        cut.WaitForAssertion(() =>
        {
            host = cut.Find("kly-infinite-scroll");
            Assert.False(host.HasAttribute("data-loading"));
            // A finished callback changes this, which is how the browser knows to look at the sentinel again.
            Assert.Equal("1", host.GetAttribute("data-loads"));
        });
        Assert.Equal(1, calls);

        cut.Find("kly-infinite-scroll").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 3 });
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task A_handler_that_throws_ends_the_load_and_can_be_asked_again()
    {
        var calls = 0;
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () =>
        {
            calls++;
            throw new InvalidOperationException("boom");
        })));

        await Assert.ThrowsAnyAsync<Exception>(() => cut.Find("kly-infinite-scroll").TriggerEventAsync("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 1 }));

        // Not stuck loading; the count that tells the browser a load is over still moved, and the browser decides whether to ask again.
        var host = cut.Find("kly-infinite-scroll");
        Assert.False(host.HasAttribute("data-loading"));
        Assert.Equal("1", host.GetAttribute("data-loads"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void A_report_is_ignored_while_the_parent_says_it_is_loading_or_there_is_nothing_more()
    {
        var calls = 0;
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.Loading, true).Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { calls++; })));

        cut.Find("kly-infinite-scroll").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 1 });
        Assert.Equal(0, calls);

        cut.Render(p => p.Add(s => s.Loading, false).Add(s => s.HasMore, false));
        // The element is still there, so a late report can still arrive.
        cut.Find("kly-infinite-scroll").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "more", Sequence = 2 });
        Assert.Equal(0, calls);
    }

    [Fact]
    public void Slots_take_classes_from_Ui_and_Class()
    {
        var cut = Render<KlyInfiniteScroll>(p => p.Add(s => s.Class, "bg-red-500")
            .Add(s => s.Ui, new InfiniteScrollUi { Sentinel = "h-4", Trigger = "uppercase", Status = "text-xs" })
            .Add(s => s.OnLoadMore, EventCallback.Factory.Create(this, () => { })));

        Assert.Contains("bg-red-500", cut.Find("kly-infinite-scroll").ClassName);
        Assert.Contains("h-4", cut.Find("[data-slot=sentinel]").ClassName);
        Assert.Contains("uppercase", cut.Find("[data-slot=trigger]").ClassName);
        Assert.Contains("text-xs", cut.Find("[data-slot=status]").ClassName);
    }

    [Fact]
    public void Texts_are_translated_in_every_locale()
    {
        foreach (var locale in KlyLocales.All.Where(l => l.Code != "en"))
        {
            Assert.False(string.IsNullOrWhiteSpace(locale.Messages.LoadMore), locale.Code);
            Assert.False(string.IsNullOrWhiteSpace(locale.Messages.LoadingMore), locale.Code);
            Assert.False(string.IsNullOrWhiteSpace(locale.Messages.NoMoreItems), locale.Code);
            Assert.NotEqual(KlyLocales.En.Messages.LoadMore, locale.Messages.LoadMore);
            Assert.NotEqual(KlyLocales.En.Messages.LoadingMore, locale.Messages.LoadingMore);
            Assert.NotEqual(KlyLocales.En.Messages.NoMoreItems, locale.Messages.NoMoreItems);
        }
        Assert.Equal(12, KlyLocales.All.Count);
    }

    [Fact]
    public void Follows_the_locale_of_the_app()
    {
        var cut = Render<KlyApp>(p => p.Add(a => a.Locale, KlyLocales.Fr).Add(a => a.ChildContent, (RenderFragment)(b =>
        {
            b.OpenComponent<KlyInfiniteScroll>(0);
            b.AddAttribute(1, nameof(KlyInfiniteScroll.Href), "?p=2");
            b.CloseComponent();
        })));

        Assert.Equal(KlyLocales.Fr.Messages.LoadMore, cut.Find("[data-slot=trigger]").TextContent.Trim());
    }
}
