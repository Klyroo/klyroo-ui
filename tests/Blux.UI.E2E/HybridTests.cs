using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>
/// The browser side of Blux in a Blazor Hybrid web view, without a device. A web view is recognized by the message bridge
/// blazor.webview.js uses (window.external.sendMessage / receiveMessage): the page is given one before it loads, so the
/// storage, culture and touch behavior of the web view run in the playground. The real thing is the emulator run of the sample.
/// </summary>
public class HybridTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private const string Bridge = "Object.defineProperty(window, 'external', { value: { sendMessage() {}, receiveMessage() {} }, configurable: true });";

    private async Task<IPage> NewWebViewPage(bool touch = false)
    {
        var context = await NewContext(new() { HasTouch = touch });
        await context.AddInitScriptAsync(Bridge);
        return await context.NewPageAsync();
    }

    [Fact]
    public async Task The_page_is_marked_as_running_in_a_web_view_only_when_the_bridge_exists()
    {
        var hybrid = await NewWebViewPage();
        await Open(hybrid, "server", "button");
        await Assertions.Expect(hybrid.Locator("html")).ToHaveAttributeAsync("data-bx-host", "webview");

        await Open("server", "button");
        await Assertions.Expect(Page.Locator("html")).Not.ToHaveAttributeAsync("data-bx-host", "webview");
    }

    [Fact]
    public async Task Sidebar_state_is_kept_in_localStorage_not_in_a_cookie_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "sidebar");
        var sidebar = page.GetByTestId("sidebar");

        await sidebar.GetByRole(AriaRole.Button, new() { Name = "Collapse sidebar" }).ClickAsync();

        await Assertions.Expect(sidebar).ToHaveAttributeAsync("data-state", "collapsed");
        await page.WaitForFunctionAsync("() => localStorage.getItem('blux-sidebar-playground-sidebar') === '1'");
        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name.StartsWith("blux-sidebar-", StringComparison.Ordinal));

        await page.ReloadAsync();
        await page.WaitForFunctionAsync("() => !!customElements.get('bx-sidebar')");
        await Assertions.Expect(page.GetByTestId("sidebar")).ToHaveAttributeAsync("data-state", "collapsed");
    }

    [Fact]
    public async Task Locale_select_neither_writes_the_ASP_NET_cookie_nor_reloads_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "locale-select");
        await page.EvaluateAsync("() => { window.__marker = 'same page'; }");

        await page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(await page.Context.CookiesAsync(), c => c.Name == ".AspNetCore.Culture");
        Assert.Equal("same page", await page.EvaluateAsync<string>("() => window.__marker"));
    }

    [Fact]
    public async Task Server_culture_is_ignored_in_a_web_view()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "button");

        var source = await page.EvaluateAsync<string?[]>("() => Blux.serverCulture()");

        Assert.Equal([null, null, null], source);
    }

    [Fact]
    public async Task Stored_culture_round_trips_through_localStorage()
    {
        var page = await NewWebViewPage();
        await Open(page, "server", "button");

        await page.EvaluateAsync("() => Blux.setStoredCulture('blux-culture', 'pt-BR')");

        Assert.Equal("pt-BR", await page.EvaluateAsync<string>("() => Blux.getStoredCulture('blux-culture')"));
    }

    [Fact]
    public async Task Tooltip_ignores_a_tap_and_opens_on_a_long_press()
    {
        var page = await NewWebViewPage(touch: true);
        await Open(page, "server", "tooltip");
        var trigger = page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true });
        var tooltip = page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" });

        await trigger.TapAsync();
        await Task.Delay(800, TestContext.Current.CancellationToken);
        await Assertions.Expect(tooltip).ToBeHiddenAsync();

        if (BrowserName != "chromium") return;
        var box = await trigger.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        var x = box.X + box.Width / 2;
        var y = box.Y + box.Height / 2;
        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchStart",
            ["touchPoints"] = new[] { new Dictionary<string, object> { ["x"] = x, ["y"] = y } },
        });
        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await cdp.SendAsync("Input.dispatchTouchEvent", new Dictionary<string, object>
        {
            ["type"] = "touchEnd",
            ["touchPoints"] = Array.Empty<object>(),
        });
        // It lingers a moment after the finger lifts, then goes.
        await Assertions.Expect(tooltip).ToBeHiddenAsync(new() { Timeout = 6000 });
    }

    [Fact]
    public async Task Tooltip_still_opens_on_mouse_hover()
    {
        await Open("server", "tooltip");

        await Page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true }).HoverAsync();

        await Assertions.Expect(Page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Spatial_navigation_moves_focus_with_the_arrow_keys_in_tv_mode()
    {
        await Open("server", "tooltip");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("data-bx-tv", "");
        var buttons = Page.Locator("main button", new() { HasTextRegex = new Regex("^(top|right|bottom|left)$") });
        await buttons.First.FocusAsync();
        var first = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");

        await Page.Keyboard.PressAsync("ArrowRight");
        var second = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");
        await Page.Keyboard.PressAsync("ArrowLeft");
        var back = await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()");

        Assert.Equal("top", first);
        Assert.NotEqual(first, second);
        Assert.Equal(first, back);
    }

    [Fact]
    public async Task Forwarded_remote_keys_move_focus_and_Enter_activates()
    {
        await Open("server", "toast");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("toast-trigger").FocusAsync();

        // The Android TV activity sends the keys this way (untrusted events: no default action).
        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvKey('ArrowRight')"));
        Assert.Equal("Show error", await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()"));
        await Page.EvaluateAsync("() => Blux.tvKey('Enter')");

        await Assertions.Expect(Page.GetByText("Upload failed", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Back_closes_the_topmost_overlay_and_reports_when_nothing_is_left()
    {
        await Open("server", "modal");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        Assert.False(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));

        await Page.GetByTestId("modal-trigger").ClickAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog)).ToBeVisibleAsync();

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(Page.GetByRole(AriaRole.Dialog)).ToBeHiddenAsync();
    }

    // What the Android TV activity does: a key is sent into the page as an untrusted keydown.
    private static Task<bool> Key(IPage page, string key) => page.EvaluateAsync<bool>("(k) => Blux.tvKey(k)", key);

    private static Task<string?> Active(IPage page) =>
        page.EvaluateAsync<string?>("() => document.activeElement?.closest('[data-testid]')?.getAttribute('data-testid') ?? document.activeElement?.tagName");

    private static Task<int> OpenPopovers(IPage page) =>
        page.EvaluateAsync<int>("() => document.querySelectorAll('[popover]:popover-open').length");

    [Fact]
    public async Task A_form_can_be_walked_through_with_the_remote_in_both_directions()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("tv-name").FocusAsync();

        // Arrows leave a text field, a select and a textarea: none of them keeps the focus.
        foreach (var expected in new[] { "tv-country", "tv-bio", "tv-save" })
        {
            await Key(Page, "ArrowDown");
            Assert.Equal(expected, await Active(Page));
        }
        foreach (var expected in new[] { "tv-bio", "tv-country", "tv-name" })
        {
            await Key(Page, "ArrowUp");
            Assert.Equal(expected, await Active(Page));
        }
        Assert.Equal(0, await OpenPopovers(Page));
    }

    [Fact]
    public async Task Select_opens_a_select_in_TV_mode_and_the_arrows_pick_inside_the_list()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("tv-country").FocusAsync();

        await Key(Page, "Enter");
        Assert.True(await OpenPopovers(Page) > 0);
        await Key(Page, "ArrowDown");
        await Key(Page, "Enter");

        await Assertions.Expect(Page.GetByTestId("tv-country-state")).ToHaveTextAsync("Selected: de");
        Assert.Equal(0, await OpenPopovers(Page));
        Assert.Equal("tv-country", await Active(Page));
        await Key(Page, "ArrowDown");
        Assert.Equal("tv-bio", await Active(Page));
    }

    [Fact]
    public async Task Text_fields_take_the_arrows_only_in_editing_mode_which_Select_enters_and_Back_leaves()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("tv-bio").FocusAsync();
        var bio = Page.GetByTestId("tv-bio");

        await Key(Page, "Enter");
        await Assertions.Expect(bio).ToHaveAttributeAsync("data-bx-editing", "");
        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvTextEditing()"));
        await Page.Keyboard.TypeAsync("one");
        await Key(Page, "Enter");
        await Page.Keyboard.TypeAsync("two");
        await Key(Page, "ArrowUp");
        await Key(Page, "ArrowLeft");
        // Editing: the arrows stayed in the field and moved the caret, Enter was a line break.
        Assert.Equal("tv-bio", await Active(Page));
        Assert.Equal("one\ntwo", await bio.InputValueAsync());
        Assert.Equal(2, await bio.EvaluateAsync<int>("(e) => e.selectionStart"));

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(bio).Not.ToHaveAttributeAsync("data-bx-editing", "");
        Assert.False(await Page.EvaluateAsync<bool>("() => Blux.tvTextEditing()"));
        await Key(Page, "ArrowUp");
        Assert.Equal("tv-country", await Active(Page));
    }

    [Fact]
    public async Task Enter_leaves_editing_a_single_line_field()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        var name = Page.GetByTestId("tv-name");
        await name.FocusAsync();

        await Key(Page, "Enter");
        await Assertions.Expect(name).ToHaveAttributeAsync("data-bx-editing", "");
        await Key(Page, "ArrowRight");
        Assert.Equal("tv-name", await Active(Page));
        await Key(Page, "Enter");

        await Assertions.Expect(name).Not.ToHaveAttributeAsync("data-bx-editing", "");
        await Key(Page, "ArrowDown");
        Assert.Equal("tv-country", await Active(Page));
    }

    [Fact]
    public async Task Forwarded_arrows_change_a_number_a_range_and_a_native_select_in_editing_mode()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("""
            () => {
              const box = document.createElement('div');
              box.innerHTML = '<input id="n" type="number" value="5" step="1" /><input id="r" type="range" min="0" max="10" value="2" /><select id="s"><option>a</option><option>b</option><option>c</option></select>';
              document.querySelector('[data-testid=tv-form]').after(box);
              Blux.setSpatialNavigation(true);
            }
            """);
        foreach (var (id, expected, key) in new[] { ("n", "6", "ArrowUp"), ("r", "1", "ArrowLeft"), ("s", "b", "ArrowDown") })
        {
            await Page.Locator("#" + id).FocusAsync();
            // Not editing: the arrow moves the focus away and the value stays.
            await Key(Page, key);
            Assert.NotEqual(id, await Page.EvaluateAsync<string>("() => document.activeElement.id"));
            await Page.Locator("#" + id).FocusAsync();
            await Key(Page, "Enter");
            await Key(Page, key);
            Assert.Equal(expected, await Page.Locator("#" + id).EvaluateAsync<string>("(e) => e.tagName === 'SELECT' ? e.value : String(e.value)"));
            await Key(Page, "Escape");
            await Assertions.Expect(Page.Locator("#" + id)).Not.ToHaveAttributeAsync("data-bx-editing", "");
        }
    }

    [Fact]
    public async Task Back_does_not_close_a_modal_that_is_not_dismissible_and_does_not_leave_the_app()
    {
        await Open("server", "modal");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete project" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Delete project?" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();

        // True: the page took the key (the app must not go to the background either), and the modal stays.
        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        Assert.True(await Key(Page, "Escape"));
        await Assertions.Expect(dialog).ToBeVisibleAsync();

        await dialog.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        Assert.False(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
    }

    [Fact]
    public async Task Back_closes_the_open_list_of_a_select_in_a_modal_before_the_modal()
    {
        await Open("server", "modal");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("modal-select-trigger").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Shipping" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Page.GetByTestId("modal-country").ClickAsync();
        Assert.Equal(1, await OpenPopovers(Page));

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        Assert.Equal(0, await OpenPopovers(Page));
        await Assertions.Expect(dialog).ToBeVisibleAsync();

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    [Fact]
    public async Task Back_closes_the_overlay_opened_last_even_when_it_comes_first_in_the_document()
    {
        await Open("server", "button");
        await Page.EvaluateAsync("""
            () => {
              Blux.setSpatialNavigation(true);
              const box = document.createElement('div');
              box.innerHTML = '<div id="pop" popover>menu</div><dialog id="dlg"><button>ok</button></dialog>';
              document.body.append(box);
              document.getElementById('dlg').showModal();
              document.getElementById('pop').showPopover();
            }
            """);

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        Assert.False(await Page.EvaluateAsync<bool>("() => document.getElementById('pop').matches(':popover-open')"));
        Assert.True(await Page.EvaluateAsync<bool>("() => document.getElementById('dlg').open"));

        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        Assert.False(await Page.EvaluateAsync<bool>("() => document.getElementById('dlg').open"));
    }

    [Fact]
    public async Task Back_closes_the_modal_not_a_tooltip_inside_it_and_leaves_the_tooltip_state_closed()
    {
        await Open("server", "modal");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByTestId("modal-select-trigger").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Shipping" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Page.EvaluateAsync("""
            () => {
              const tip = document.createElement('bx-tooltip');
              tip.id = 'tip';
              tip.setAttribute('delay', '0');
              tip.innerHTML = '<span data-slot="trigger"><button id="tip-trigger" type="button">info</button></span><div popover="manual" role="tooltip">Helpful</div>';
              document.querySelector('dialog[open]').append(tip);
            }
            """);
        await Page.Locator("#tip-trigger").HoverAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Tooltip, new() { Name = "Helpful" })).ToBeVisibleAsync();

        // One Back closes the modal: the tooltip is not a layer of its own, and it does not keep a stale "open" state.
        Assert.True(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(dialog).ToBeHiddenAsync();
        Assert.Equal("closed", await Page.EvaluateAsync<string>("() => document.getElementById('tip').dataset.state"));
    }

    [Fact]
    public async Task Back_hides_a_visible_tooltip_through_its_own_hide_path_even_when_nothing_else_is_open()
    {
        await Open("server", "tooltip");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        await Page.GetByRole(AriaRole.Button, new() { Name = "top", Exact = true }).HoverAsync();
        var tooltip = Page.GetByRole(AriaRole.Tooltip, new() { Name = "Tooltip on the top" });
        await Assertions.Expect(tooltip).ToBeVisibleAsync();

        // No real layer: Back is not used by the page (the app may leave), but the tooltip is hidden and its state is closed.
        Assert.False(await Page.EvaluateAsync<bool>("() => Blux.tvBack()"));
        await Assertions.Expect(tooltip).ToBeHiddenAsync();
        Assert.Equal("closed", await Page.EvaluateAsync<string>("() => document.querySelector('bx-tooltip[data-state]').dataset.state"));
    }

    [Fact]
    public async Task Trusted_Enter_enters_editing_mode_and_the_arrows_then_belong_to_the_field_in_TV_mode()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        var name = Page.GetByTestId("tv-name");
        await name.FocusAsync();
        await name.FillAsync("abc");

        // A physical keyboard (trusted events): the first Enter enters editing without submitting anything.
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(name).ToHaveAttributeAsync("data-bx-editing", "");
        await Page.Keyboard.PressAsync("ArrowLeft");
        Assert.Equal("tv-name", await Active(Page));
        Assert.Equal(2, await name.EvaluateAsync<int>("(e) => e.selectionStart"));

        // Escape leaves editing and the arrows move the focus again.
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(name).Not.ToHaveAttributeAsync("data-bx-editing", "");
        await Page.Keyboard.PressAsync("ArrowDown");
        Assert.Equal("tv-country", await Active(Page));

        // Enter while editing a single line field leaves editing again.
        await name.FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(name).ToHaveAttributeAsync("data-bx-editing", "");
        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(name).Not.ToHaveAttributeAsync("data-bx-editing", "");
    }

    [Fact]
    public async Task Trusted_keys_edit_a_textarea_in_TV_mode_with_the_native_caret_and_line_breaks()
    {
        await Open("server", "select");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        var bio = Page.GetByTestId("tv-bio");
        await bio.FocusAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(bio).ToHaveAttributeAsync("data-bx-editing", "");
        await Page.Keyboard.TypeAsync("one");
        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.TypeAsync("two");
        await Page.Keyboard.PressAsync("ArrowUp");
        await Page.Keyboard.PressAsync("ArrowLeft");

        Assert.Equal("tv-bio", await Active(Page));
        Assert.Equal("one\ntwo", await bio.InputValueAsync());
        Assert.Equal(2, await bio.EvaluateAsync<int>("(e) => e.selectionStart"));
        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(bio).Not.ToHaveAttributeAsync("data-bx-editing", "");
        await Page.Keyboard.PressAsync("ArrowUp");
        Assert.Equal("tv-country", await Active(Page));
    }

    [Fact]
    public async Task Without_TV_mode_the_keyboard_keeps_its_native_behavior_in_fields()
    {
        await Open("server", "select");
        var name = Page.GetByTestId("tv-name");
        await name.FocusAsync();
        await name.FillAsync("abc");

        await Page.Keyboard.PressAsync("Enter");
        await Page.Keyboard.PressAsync("ArrowLeft");

        await Assertions.Expect(name).Not.ToHaveAttributeAsync("data-bx-editing", "");
        Assert.Equal("tv-name", await Active(Page));
        Assert.Equal(2, await name.EvaluateAsync<int>("(e) => e.selectionStart"));
    }

    [Fact]
    public async Task A_dialog_that_is_not_modal_does_not_confine_the_arrows()
    {
        await Open("server", "tooltip");
        await Page.EvaluateAsync("""
            () => {
              Blux.setSpatialNavigation(true);
              const dialog = document.createElement('dialog');
              dialog.innerHTML = '<button id="inside">inside</button>';
              document.body.append(dialog);
              dialog.show();
            }
            """);
        await Page.Locator("main button", new() { HasTextRegex = new Regex("^top$") }).FocusAsync();

        await Key(Page, "ArrowRight");

        Assert.NotEqual("inside", await Page.EvaluateAsync<string>("() => document.activeElement.id"));
        Assert.NotEqual("top", await Page.EvaluateAsync<string>("() => document.activeElement.textContent.trim()"));
    }

    [Fact]
    public async Task Header_mobile_menu_opens_with_a_tap_in_a_web_view()
    {
        var context = await NewContext(new() { HasTouch = true, IsMobile = true, ViewportSize = new() { Width = 390, Height = 800 } });
        await context.AddInitScriptAsync(Bridge);
        var page = await context.NewPageAsync();
        await Open(page, "server", "header");
        var dialog = page.GetByRole(AriaRole.Dialog, new() { Name = "Site menu" });

        await page.GetByTestId("header").GetByRole(AriaRole.Button, new() { Name = "Open menu" }).TapAsync();

        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await page.EvaluateAsync("() => Blux.tvBack()");
        await Assertions.Expect(dialog).ToBeHiddenAsync();
    }

    private async Task<IPage> NewTouchPage()
    {
        var context = await NewContext(new() { HasTouch = true, IsMobile = true, ViewportSize = new() { Width = 390, Height = 800 } });
        return await context.NewPageAsync();
    }

    private static Task<double[]> Size(ILocator locator) =>
        locator.EvaluateAsync<double[]>("(e) => { const r = e.getBoundingClientRect(); return [r.width, r.height]; }");

    // The element at a point, as the id of the element or of its closest control: what a finger there would press.
    private static Task<string?> HitAt(IPage page, double x, double y) =>
        page.EvaluateAsync<string?>("([x, y]) => { const e = document.elementFromPoint(x, y); const c = e?.closest('button, label, input, [data-slot]'); return c?.getAttribute('data-hit') ?? c?.tagName.toLowerCase() ?? null; }", new[] { x, y });

    private static Task<string> HitDate(IPage page, double x, double y) =>
        page.EvaluateAsync<string>(FormattableString.Invariant($"() => document.elementFromPoint({x}, {y})?.closest('button')?.dataset.date ?? ''"));

    [Fact]
    public async Task Coarse_pointers_get_a_hit_area_of_44_pixels_around_an_icon_button_without_changing_its_size()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "button");
        var icon = page.Locator("main button:has(svg:only-child)").First;
        var before = await Size(icon);
        Assert.True(before[0] < 44 || before[1] < 44, $"the test needs a button under 44px, got {before[0]}x{before[1]}");
        var reaches = await icon.EvaluateAsync<bool[]>("""
            (b) => {
              b.scrollIntoView({ block: 'center' });
              const r = b.getBoundingClientRect();
              const x = r.x + r.width / 2;
              const y = r.y + r.height / 2;
              return [[20, 0], [-20, 0], [0, 20], [0, -20]].map(([dx, dy]) => { const e = document.elementFromPoint(x + dx, y + dy); return !!e && b.contains(e); });
            }
            """);

        Assert.All(reaches, reached => Assert.True(reached, "a point 20px from the icon center does not hit the button"));
        Assert.Equal(before, await Size(icon));
    }

    [Fact]
    public async Task Coarse_pointers_keep_the_size_of_the_buttons_in_the_layout()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "button");

        var small = await page.EvaluateAsync<double[]>("() => [...document.querySelectorAll('main button')].filter((b) => b.getClientRects().length).map((b) => b.getBoundingClientRect().height).filter((h) => h < 40)");

        // A mouse and a finger see the same layout: the 36px (and smaller) buttons are still there, with no min-height forced on them.
        Assert.NotEmpty(small);
    }

    [Fact]
    public async Task Coarse_pointers_keep_the_switch_track_at_its_size_and_toggle_it_from_the_hit_area()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "switch");
        var toggle = page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });

        Assert.Equal(new[] { 36d, 20d }, await Size(toggle));

        var box = await toggle.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        // The hit area must not move the track: it stays level with the first line of its label, as with a mouse.
        var label = page.Locator("label", new() { HasText = "Email notifications" }).Last;
        var labelBox = await label.BoundingBoxAsync() ?? throw new InvalidOperationException("No label box.");
        Assert.True(box.Y < labelBox.Y + labelBox.Height / 2 && box.Y + box.Height > labelBox.Y,
            $"track at y={box.Y}..{box.Y + box.Height}, label at y={labelBox.Y}..{labelBox.Y + labelBox.Height}");
        var wasChecked = await toggle.IsCheckedAsync();
        // 10px above the track is outside it but inside the 44px hit area: the tap toggles.
        await page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y - 10);
        Assert.NotEqual(wasChecked, await toggle.IsCheckedAsync());
    }

    // Every visible control of the page, tapped 2px inside each of its four edges (at the middle of the edge): the element
    // that gets the tap must be that control, never a neighbour whose extended hit area reaches over it.
    private const string SweepScript = """
        () => {
          const selector = 'button, a[href], summary, [role=button], [role=tab], [role=switch], [role=checkbox], [role=radio], label:has(> input[role=switch])';
          const controls = [...document.querySelectorAll('main ' + selector.split(', ').join(', main '))]
            .filter((c) => !c.closest('[hidden], [inert]') && c.getClientRects().length > 0);
          const bad = [];
          for (const c of controls) {
            c.scrollIntoView({ block: 'center', inline: 'center' });
            const r = c.getBoundingClientRect();
            if (r.width < 8 || r.height < 8 || r.left < 0 || r.right > innerWidth || r.top < 0 || r.bottom > innerHeight) continue;
            const points = { left: [r.left + 2, r.top + r.height / 2], right: [r.right - 2, r.top + r.height / 2], top: [r.left + r.width / 2, r.top + 2], bottom: [r.left + r.width / 2, r.bottom - 2] };
            for (const [edge, [x, y]] of Object.entries(points)) {
              const hit = document.elementFromPoint(x, y)?.closest(selector);
              if (hit && hit !== c && !hit.contains(c) && !c.contains(hit)) bad.push((c.getAttribute('aria-label') ?? c.textContent.trim().slice(0, 20)) + ' [' + edge + '] -> ' + (hit.getAttribute('aria-label') ?? hit.textContent.trim().slice(0, 20)));
            }
          }
          return { count: controls.length, bad };
        }
        """;

    [Theory]
    [InlineData("pagination")]
    [InlineData("toggle-group")]
    [InlineData("editor-toolbar")]
    [InlineData("tabs")]
    [InlineData("button")]
    [InlineData("input-tags")]
    [InlineData("input-rating")]
    [InlineData("input-number")]
    [InlineData("stepper")]
    [InlineData("carousel")]
    [InlineData("dashboard-toolbar")]
    [InlineData("header")]
    [InlineData("navigation-menu")]
    [InlineData("breadcrumb")]
    [InlineData("table")]
    [InlineData("card")]
    [InlineData("modal")]
    [InlineData("toast")]
    [InlineData("checkbox-group")]
    [InlineData("radio-group")]
    [InlineData("toggle")]
    [InlineData("switch")]
    [InlineData("color-mode-switch")]
    [InlineData("chat-prompt")]
    [InlineData("accordion")]
    [InlineData("pin-input")]
    [InlineData("calendar")]
    public async Task Coarse_hit_areas_never_cover_the_box_of_a_neighbouring_control(string component)
    {
        var page = await NewTouchPage();
        await Open(page, "server", component);

        var result = await page.EvaluateAsync<System.Text.Json.JsonElement>(SweepScript);

        Assert.True(result.GetProperty("count").GetInt32() > 3, "the page has controls to check");
        var bad = result.GetProperty("bad").EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.True(bad.Length == 0, string.Join("; ", bad));
    }

    [Fact]
    public async Task Pagination_xs_buttons_4px_apart_each_get_their_own_edges_and_the_toolbar_keeps_its_free_space_reach()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "pagination");
        var nav = page.GetByTestId("pagination-xs");
        var pages = nav.Locator("button[aria-label]:not([disabled])");
        Assert.Equal(24d, (await Size(pages.First))[1]);

        // 2px inside the right and the left edge of every page button: the button itself, not its right-hand neighbour.
        for (var i = 0; i < await pages.CountAsync() - 1; i++)
        {
            await pages.Nth(i).ScrollIntoViewIfNeededAsync();
            var box = await pages.Nth(i).BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
            var right = await page.EvaluateAsync<string?>("([x, y]) => document.elementFromPoint(x, y)?.closest('button')?.getAttribute('aria-label')", new double[] { box.X + box.Width - 2, box.Y + box.Height / 2 });
            var left = await page.EvaluateAsync<string?>("([x, y]) => document.elementFromPoint(x, y)?.closest('button')?.getAttribute('aria-label')", new double[] { box.X + 2, box.Y + box.Height / 2 });
            var own = await pages.Nth(i).GetAttributeAsync("aria-label");
            Assert.True(own == right, $"{own} right edge -> {right}");
            Assert.Equal(own, left);
        }

        // A toolbar-like row (toggle group): items touch each other, but the free space above and below still extends them.
        await Open(page, "server", "toggle-group");
        var group = page.GetByRole(AriaRole.Group, new() { Name = "Text alignment" });
        var items = group.GetByRole(AriaRole.Button);
        await items.Nth(0).ScrollIntoViewIfNeededAsync();
        var first = await items.Nth(0).BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        Assert.True(first.Height < 44, $"item of {first.Height}px");
        var above = await page.EvaluateAsync<string?>("([x, y]) => document.elementFromPoint(x, y)?.closest('button')?.textContent.trim()", new double[] { first.X + first.Width / 2, first.Y - 4 });
        Assert.Equal("Left", above);
    }

    // The hit area of a switch is its wrapping label: a tap there must reach ancestors as one click, not as the label's click
    // followed by the synthetic click the browser sends to the input.
    private static async Task<int> ClicksAt(IPage page, ILocator toggle, Func<Task> act)
    {
        await page.EvaluateAsync("() => { if (!window.__hooked) { window.__hooked = true; document.body.addEventListener('click', () => window.__clicks++); } window.__clicks = 0; }");
        var before = await toggle.IsCheckedAsync();
        await act();
        await Task.Delay(150, TestContext.Current.CancellationToken);
        Assert.NotEqual(before, await toggle.IsCheckedAsync());
        return await page.EvaluateAsync<int>("() => window.__clicks");
    }

    [Fact]
    public async Task One_tap_on_a_switch_is_one_click_for_ancestors_whether_on_the_track_or_in_the_hit_area()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "switch");
        var toggle = page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });
        var box = await toggle.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");

        Assert.Equal(1, await ClicksAt(page, toggle, () => page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y + box.Height / 2)));
        Assert.Equal(1, await ClicksAt(page, toggle, () => page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y - 10)));
    }

    [Fact]
    public async Task A_click_on_the_label_text_of_a_switch_still_counts_two_for_ancestors_as_the_HTML_spec_says()
    {
        // A label that is not the switch container (its text) dispatches a click on the control after its own: two events for
        // ancestors, one for the label and one for the input. Only the container's hit area is made to count once.
        var page = await NewTouchPage();
        await Open(page, "server", "switch");
        var toggle = page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });
        var text = page.Locator("label[for]", new() { HasText = "Email notifications" });

        Assert.Equal(2, await ClicksAt(page, toggle, () => text.ClickAsync()));
    }

    [Fact]
    public async Task Coarse_pointers_give_the_color_mode_switch_the_same_hit_area_as_a_switch()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "color-mode-switch");
        var toggle = page.GetByRole(AriaRole.Switch, new() { Name = "Dark mode", Exact = true });
        var size = await Size(toggle);
        Assert.Equal(new[] { 40d, 22d }, size);

        var box = await toggle.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        var label = page.Locator("label[for]", new() { HasText = "Dark mode" });
        var labelBox = await label.BoundingBoxAsync() ?? throw new InvalidOperationException("No label box.");
        Assert.True(box.Y < labelBox.Y + labelBox.Height / 2 && box.Y + box.Height > labelBox.Y,
            $"track at y={box.Y}..{box.Y + box.Height}, label at y={labelBox.Y}..{labelBox.Y + labelBox.Height}");

        var wasChecked = await toggle.IsCheckedAsync();
        // 10px above the track is outside it but inside the 44px hit area: the tap toggles, once.
        Assert.Equal(1, await ClicksAt(page, toggle, () => page.Touchscreen.TapAsync(box.X + box.Width / 2, box.Y - 10)));
        Assert.NotEqual(wasChecked, await toggle.IsCheckedAsync());
        Assert.Equal(size, await Size(toggle));
    }

    [Fact]
    public async Task Coarse_pointers_do_not_make_the_vertical_steppers_overlap()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "input-number");
        await page.EvaluateAsync("() => document.querySelectorAll('bx-input-number').forEach((n) => { n.querySelector('[data-slot=increment]')?.setAttribute('data-hit', 'increment'); n.querySelector('[data-slot=decrement]')?.setAttribute('data-hit', 'decrement'); })");
        var vertical = page.Locator("bx-input-number:has([data-slot='increment'][class*='h-1/2'])").First;
        await Assertions.Expect(vertical).ToBeVisibleAsync();

        var increment = vertical.Locator("[data-slot='increment']");
        var decrement = vertical.Locator("[data-slot='decrement']");
        var up = await increment.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        var down = await decrement.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");

        Assert.True(up.Height < 44 && down.Height < 44, "the steppers keep the height of the field halves");
        Assert.True(up.Y + up.Height <= down.Y + 0.5, "the halves do not overlap");
        Assert.Equal("increment", await HitAt(page, up.X + up.Width / 2, up.Y + up.Height / 2));
        Assert.Equal("increment", await HitAt(page, up.X + up.Width / 2, up.Y + 1));
        Assert.Equal("increment", await HitAt(page, up.X + up.Width / 2, up.Y + up.Height - 1));
        Assert.Equal("decrement", await HitAt(page, down.X + down.Width / 2, down.Y + 1));
        Assert.Equal("decrement", await HitAt(page, down.X + down.Width / 2, down.Y + down.Height / 2));
    }

    [Fact]
    public async Task Coarse_pointers_keep_the_size_of_calendar_days_and_tag_chip_buttons()
    {
        var page = await NewTouchPage();
        await Open(page, "server", "calendar");
        var day = page.Locator("button[data-date]").First;
        var daySize = await Size(day);
        Assert.True(daySize[0] < 44 && daySize[1] < 44, $"day of {daySize[0]}x{daySize[1]}");
        var dayBox = await day.BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        var next = await page.Locator("button[data-date]").Nth(1).BoundingBoxAsync() ?? throw new InvalidOperationException("No box.");
        // The hit areas of neighbouring days do not eat into each other: the middle of each cell hits its own day.
        Assert.True(double.IsFinite(dayBox.X + dayBox.Y + dayBox.Width + dayBox.Height), $"box {dayBox.X} {dayBox.Y} {dayBox.Width} {dayBox.Height}");
        var first = await HitDate(page, dayBox.X + dayBox.Width / 2, dayBox.Y + dayBox.Height - 1);
        Assert.Equal(await day.GetAttributeAsync("data-date"), first);
        Assert.True(next.X > dayBox.X);

        await Open(page, "server", "input-tags");
        var remove = page.Locator("button[data-remove]").First;
        var removeSize = await Size(remove);
        var chip = await page.Locator("button[data-remove]").First.EvaluateAsync<double>("(e) => e.parentElement.getBoundingClientRect().height");
        Assert.True(removeSize[0] <= 24 && removeSize[1] <= 24, $"remove of {removeSize[0]}x{removeSize[1]}");
        Assert.True(chip < 44);
    }

    [Fact]
    public async Task The_bottom_safe_area_inset_is_applied_once()
    {
        await Open("server", "main");
        await Page.EvaluateAsync("() => document.documentElement.style.setProperty('--bx-safe-bottom', '20px')");
        var main = Page.GetByTestId("main");

        // A footer follows the main block: it owns the inset.
        Assert.Equal("0px", await main.EvaluateAsync<string>("(e) => getComputedStyle(e).paddingBottom"));
        Assert.Equal("20px", await main.Locator("xpath=following-sibling::footer").EvaluateAsync<string>("(e) => getComputedStyle(e).paddingBottom"));

        // Without one, the main block is the bottom edge (a bare main with the theme's padding classes; the example's own padding class replaces them).
        var bare = await Page.EvaluateAsync<string[]>("""
            () => {
              const box = document.createElement('div');
              box.innerHTML = '<main class="pb-(--bx-safe-bottom) has-[~footer]:pb-0"></main><footer></footer>';
              document.body.append(box);
              const main = box.querySelector('main');
              const withFooter = getComputedStyle(main).paddingBottom;
              box.querySelector('footer').remove();
              return [withFooter, getComputedStyle(main).paddingBottom];
            }
            """);
        Assert.Equal(new[] { "0px", "20px" }, bare);
    }

    [Fact]
    public async Task A_dashboard_inside_the_main_block_leaves_the_bottom_inset_to_it()
    {
        await Open("server", "dashboard-group");
        await Page.EvaluateAsync("() => document.documentElement.style.setProperty('--bx-safe-bottom', '20px')");
        var dashboard = Page.Locator("bx-dashboard-group").First;

        Assert.Equal("20px", await dashboard.EvaluateAsync<string>("(e) => getComputedStyle(e).paddingBottom"));
        await dashboard.EvaluateAsync("(e) => e.parentElement.setAttribute('data-bx-main', '')");
        Assert.Equal("0px", await dashboard.EvaluateAsync<string>("(e) => getComputedStyle(e).paddingBottom"));
    }

    [Fact]
    public async Task The_viewport_high_minimum_of_main_fits_inside_the_TV_overscan_margins()
    {
        await Open("server", "main");
        await Page.EvaluateAsync("() => Blux.setSpatialNavigation(true)");
        // BxMain's minimum height is the viewport minus --bx-tv-inset, which TV mode sets to both vertical margins of the html element.
        var overflow = await Page.EvaluateAsync<double>("""
            () => {
              const probe = document.createElement('div');
              probe.style.minHeight = 'calc(100dvh - var(--bx-tv-inset, 0px))';
              document.body.replaceChildren(probe);
              return document.documentElement.scrollHeight - document.documentElement.clientHeight;
            }
            """);
        Assert.True(overflow <= 1, $"the page scrolls by {overflow}px in TV mode");
    }
}
