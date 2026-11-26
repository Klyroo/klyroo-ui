using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

public sealed class OverlayPicker : ComponentBase
{
    [CascadingParameter] public KlyOverlayContext<string> Overlay { get; set; } = default!;
    [Parameter] public string Name { get; set; } = "";

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddAttribute(1, "class", "picked");
        builder.AddContent(2, "Hello " + Name);
        builder.CloseElement();
        builder.OpenElement(3, "button");
        builder.AddAttribute(4, "id", "done-" + Name);
        builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => Overlay.Close(Name.ToUpperInvariant())));
        builder.AddContent(6, "Done");
        builder.CloseElement();
        builder.OpenElement(7, "button");
        builder.AddAttribute(8, "id", "cancel-" + Name);
        builder.AddAttribute(9, "onclick", EventCallback.Factory.Create(this, () => Overlay.Cancel()));
        builder.AddContent(10, "Nope");
        builder.CloseElement();
    }
}

public sealed class OverlayHosts : ComponentBase
{
    [Parameter] public bool Second { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "section");
        builder.AddAttribute(1, "id", "first");
        builder.OpenComponent<KlyOverlayHost>(2);
        builder.CloseComponent();
        builder.CloseElement();
        if (Second)
        {
            builder.OpenElement(3, "section");
            builder.AddAttribute(4, "id", "second");
            builder.OpenComponent<KlyOverlayHost>(5);
            builder.CloseComponent();
            builder.CloseElement();
        }
    }
}

public class OverlayServiceTests : KlyrooTestContext
{
    private IKlyOverlayService Overlays => Services.GetRequiredService<IKlyOverlayService>();

    private static IReadOnlyDictionary<string, object?> Name(string name) => new Dictionary<string, object?> { ["Name"] = name };

    [Fact]
    public async Task Show_without_a_host_explains_what_is_missing()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Overlays.ShowAsync<OverlayPicker, string>(cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Contains("KlyOverlayHost", ex.Message);
    }

    [Fact]
    public async Task Show_opens_a_titled_modal_with_the_component_and_its_parameters()
    {
        var cut = Render<KlyOverlayHost>();

        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), new KlyOverlayOptions { Title = "Pick", Description = "Choose one", Size = KlySize.Lg }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Assert.True(cut.Find("kly-modal").HasAttribute("open"));
        Assert.Equal("Pick", cut.Find("h2").TextContent);
        Assert.Equal("Hello Ada", cut.Find("dialog .picked").TextContent);
        Assert.False(pending.IsCompleted);
    }

    [Fact]
    public async Task The_component_completes_the_dialog_with_a_result()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        cut.Find("#done-Ada").Click();
        var result = await pending;

        Assert.False(result.Canceled);
        Assert.Equal("ADA", result.Value);
        Assert.True(result.TryGetValue(out var text));
        Assert.Equal("ADA", text);
        // The dialog is told to close, then its markup goes away once the browser has closed it.
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task The_component_can_cancel()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        cut.Find("#cancel-Ada").Click();
        var result = await pending;

        Assert.True(result.Canceled);
        Assert.Null(result.Value);
        Assert.False(result.TryGetValue(out _));
    }

    private sealed class NumberPicker : ComponentBase
    {
        [CascadingParameter] public KlyOverlayContext<int> Overlay { get; set; } = default!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "id", "seven");
            builder.AddAttribute(2, "onclick", EventCallback.Factory.Create(this, () => Overlay.Close(7)));
            builder.AddContent(3, "Seven");
            builder.CloseElement();
        }
    }

    [Fact]
    public async Task The_result_has_the_type_the_caller_asked_for_and_content_can_end_the_dialog_too()
    {
        var cut = Render<KlyOverlayHost>();
        var typed = Overlays.ShowAsync<NumberPicker, int>(cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));
        cut.Find("#seven").Click();
        var number = await typed;
        Assert.False(number.Canceled);
        Assert.Equal(7, number.Value);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));

        var content = Overlays.ShowAsync<int>(b =>
        {
            b.OpenComponent<NumberPicker>(0);
            b.CloseComponent();
        }, new KlyOverlayOptions { Title = "Fragment" }, Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Equal("Fragment", cut.Find("kly-modal h2").TextContent.Trim()));
        cut.Find("#seven").Click();
        Assert.Equal(7, (await content).Value);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));

        using var source = new CancellationTokenSource();
        var dismissed = Overlays.ShowAsync<int>(b =>
        {
            b.OpenComponent<NumberPicker>(0);
            b.CloseComponent();
        }, cancellationToken: source.Token);
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("#seven")));
        source.Cancel();
        var result = await dismissed;
        Assert.True(result.Canceled);
        Assert.Equal(0, result.Value);
        Assert.False(result.TryGetValue(out _));
    }

    [Fact]
    public async Task Escape_backdrop_and_close_button_are_a_cancel_result()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), new KlyOverlayOptions { Title = "Pick" }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        // The browser closes the native dialog on Escape, a click outside or the close button and reports it as a toggle.
        cut.Find("kly-modal").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 2 });
        var result = await pending;

        Assert.True(result.Canceled);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")));
    }

    [Fact]
    public void Dismissible_false_and_close_false_are_passed_to_the_modal()
    {
        var cut = Render<KlyOverlayHost>();
        _ = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), new KlyOverlayOptions { Title = "Pick", Dismissible = false, Close = false }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Assert.False(cut.Find("kly-modal").HasAttribute("dismissible"));
        Assert.Empty(cut.FindAll("button[aria-label=Close]"));
    }

    [Fact]
    public void A_slideover_is_available_through_the_options()
    {
        var cut = Render<KlyOverlayHost>();
        _ = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), new KlyOverlayOptions { Title = "Panel", Kind = KlyOverlayKind.Slideover, Side = SlideoverSide.Left }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Assert.Equal("left", cut.Find("dialog").GetAttribute("data-side"));
    }

    [Fact]
    public async Task Two_dialogs_stack_and_end_independently()
    {
        var cut = Render<KlyOverlayHost>();
        var first = Overlays.ShowAsync<OverlayPicker, string>(Name("One"), new KlyOverlayOptions { Title = "First" }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var second = Overlays.ShowAsync<OverlayPicker, string>(Name("Two"), new KlyOverlayOptions { Title = "Second" }, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("kly-modal").Count));

        // The second one is rendered after the first, so it is on top in the browser.
        Assert.Equal(["First", "Second"], cut.FindAll("h2").Select(h => h.TextContent));

        cut.Find("#cancel-Two").Click();
        Assert.True((await second).Canceled);
        Assert.False(first.IsCompleted);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));
        Assert.Equal("First", cut.Find("h2").TextContent);

        cut.Find("#done-One").Click();
        Assert.Equal("ONE", (await first).Value);
    }

    [Fact]
    public async Task A_cancellation_token_cancels_the_dialog()
    {
        var cut = Render<KlyOverlayHost>();
        using var source = new CancellationTokenSource();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: source.Token);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        await source.CancelAsync();

        Assert.True((await pending).Canceled);
    }

    [Fact]
    public async Task Removing_the_host_cancels_what_is_open()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        await DisposeComponentsAsync();

        Assert.True((await pending).Canceled);
    }

    [Fact]
    public async Task Confirm_returns_true_on_the_confirm_button()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ConfirmAsync("Delete file", "This cannot be undone.", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Assert.Equal("Delete file", cut.Find("h2").TextContent);
        Assert.Equal("This cannot be undone.", cut.Find("dialog p").TextContent);
        var buttons = cut.FindAll("[data-slot=footer] button");
        Assert.Equal(["Cancel", "Confirm"], buttons.Select(b => b.TextContent.Trim()));
        buttons[1].Click();

        Assert.True(await pending);
    }

    [Fact]
    public async Task Confirm_returns_false_on_cancel_and_on_dismiss()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ConfirmAsync("Delete file", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));
        cut.FindAll("[data-slot=footer] button")[0].Click();
        Assert.False(await pending);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));

        pending = Overlays.ConfirmAsync("Delete file", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));
        cut.Find("kly-modal").TriggerEvent("onklytoggle", new KlyToggleEventArgs { Open = false, Sequence = 2 });
        Assert.False(await pending);
    }

    [Fact]
    public async Task Confirm_takes_labels_and_a_color()
    {
        var cut = Render<KlyOverlayHost>();
        _ = Overlays.ConfirmAsync("Delete", "Sure?", "Delete it", "Keep it", KlyColor.Error, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        var buttons = cut.FindAll("[data-slot=footer] button");
        Assert.Equal(["Keep it", "Delete it"], buttons.Select(b => b.TextContent.Trim()));
        Assert.Equal("error", buttons[1].GetAttribute("data-color"));
    }

    [Fact]
    public async Task Confirm_buttons_use_the_locale()
    {
        var cut = Render<KlyOverlayHost>(p => p.AddCascadingValue(KlyLocales.Fr));
        _ = Overlays.ConfirmAsync("Supprimer", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Assert.Equal(["Annuler", "Confirmer"], cut.FindAll("[data-slot=footer] button").Select(b => b.TextContent.Trim()));
    }

    [Fact]
    public async Task KlyApp_hosts_the_dialogs_unless_overlays_is_off()
    {
        var app = Render<KlyApp>();
        _ = Overlays.ConfirmAsync("Hosted", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        app.WaitForAssertion(() => Assert.Single(app.FindAll("kly-modal")));
        await DisposeComponentsAsync();

        var off = Render<KlyApp>(p => p.Add(a => a.Overlays, false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Overlays.ConfirmAsync("Nowhere", cancellationToken: Xunit.TestContext.Current.CancellationToken));
        Assert.Empty(off.FindAll("kly-modal"));
    }

    [Fact]
    public async Task A_navigation_to_another_page_cancels_the_open_dialogs_but_a_query_or_fragment_change_does_not()
    {
        var cut = Render<KlyOverlayHost>();
        var navigation = Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        navigation.NavigateTo(navigation.Uri + "#section");
        Assert.False(pending.IsCompleted);
        // A page that keeps its filters in the query rewrites it while the dialog is open.
        navigation.NavigateTo(navigation.GetUriWithQueryParameter("region", "eu"), replace: true);
        Assert.False(pending.IsCompleted);

        navigation.NavigateTo("/elsewhere");
        var result = await pending;

        Assert.True(result.Canceled);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task A_confirm_is_false_after_a_navigation()
    {
        var cut = Render<KlyOverlayHost>();
        var pending = Overlays.ConfirmAsync("Delete", cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("kly-modal")));

        Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>().NavigateTo("/elsewhere");

        Assert.False(await pending);
    }

    [Fact]
    public async Task A_non_interactive_render_attaches_no_host_and_the_call_throws_with_advice()
    {
        using var staticContext = new StaticContext();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => staticContext.Show());

        Assert.Contains("OnAfterRenderAsync", ex.Message);
        Assert.Contains("OnInitialized", ex.Message);
        Assert.Contains("prerender", ex.Message);
    }

    [Fact]
    public async Task Calling_before_the_host_attached_throws_and_says_to_wait_for_a_render()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Overlays.ConfirmAsync("Too early", cancellationToken: Xunit.TestContext.Current.CancellationToken));

        Assert.Contains("OnAfterRenderAsync", ex.Message);
        Assert.Contains("event handler", ex.Message);
    }

    [Fact]
    public async Task Show_from_a_thread_pool_thread_and_the_delayed_removal_leave_the_list_consistent()
    {
        var cut = Render<KlyOverlayHost>();
        var tasks = await Task.Run(() => Enumerable.Range(0, 8)
            .Select(i => Overlays.ShowAsync<OverlayPicker, string>(Name("N" + i), cancellationToken: Xunit.TestContext.Current.CancellationToken)).ToArray(), Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Equal(8, cut.FindAll("kly-modal").Count), TimeSpan.FromSeconds(3));

        for (var i = 0; i < 8; i++) cut.Find($"#cancel-N{i}").Click();
        await Task.WhenAll(tasks);

        // The removals run on a timer thread and come back through the dispatcher of the host.
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task With_several_hosts_the_newest_draws_the_dialogs_and_the_last_one_to_go_cancels()
    {
        var cut = Render<OverlayHosts>();
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("#first kly-modal")));

        // A second host appears: it becomes the one that draws.
        cut.Render(p => p.Add(c => c.Second, true));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("#second kly-modal")));
        Assert.Empty(cut.FindAll("#first kly-modal"));
        Assert.False(pending.IsCompleted);

        // It goes away: the dialog is not canceled, the first host draws it again.
        cut.Render(p => p.Add(c => c.Second, false));
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("#first kly-modal")));
        Assert.False(pending.IsCompleted);

        cut.Find("#done-Ada").Click();
        Assert.Equal("ADA", (await pending).Value);
        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll("kly-modal")), TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Removing_the_only_host_cancels_even_with_a_second_one_gone_before()
    {
        var cut = Render<OverlayHosts>(p => p.Add(c => c.Second, true));
        var pending = Overlays.ShowAsync<OverlayPicker, string>(Name("Ada"), cancellationToken: Xunit.TestContext.Current.CancellationToken);
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("#second kly-modal")));

        await DisposeComponentsAsync();

        Assert.True((await pending).Canceled);
    }

    private sealed class StaticContext : KlyrooTestContext
    {
        public StaticContext() : base(interactive: false) => Render<KlyOverlayHost>();

        public Task<KlyOverlayResult<string>> Show() =>
            Services.GetRequiredService<IKlyOverlayService>().ShowAsync<OverlayPicker, string>(cancellationToken: Xunit.TestContext.Current.CancellationToken);
    }
}
