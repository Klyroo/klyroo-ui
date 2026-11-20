using System.Text.RegularExpressions;

namespace Blux.UI.E2E;

/// <summary>The "Chart" family: BxChart, drawn as SVG on the server with a tooltip and keyboard exploration in the browser.</summary>
public class ChartTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs = ["chart"];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Chart_page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    [Fact]
    public async Task Chart_page_has_no_accessibility_violations_in_dark_mode_with_a_tooltip_open()
    {
        await Open("ssr", "chart");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Toggle dark mode" }).ClickAsync();
        await Assertions.Expect(Page.Locator("html")).ToHaveClassAsync(new Regex("dark"));
        await Page.GetByTestId("chart-line").Locator("g[data-hit][data-index='1'] rect").HoverAsync();
        await Assertions.Expect(Page.GetByTestId("chart-line").Locator("[data-slot=tooltip]")).ToHaveAttributeAsync("data-state", "open");

        await AssertNoAccessibilityViolations();
    }

    private ILocator Chart(string testId) => Page.GetByTestId(testId);

    // ---- Structure

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Chart_is_an_image_with_a_summary_and_the_same_data_in_a_table(string mode)
    {
        await Open(mode, "chart");
        var chart = Chart("chart-line");

        // The chart is interactive: the focusable group carries the name and the drawing is hidden from assistive technology.
        var image = chart.Locator("[data-slot=plot]");
        await Assertions.Expect(chart.GetByRole(AriaRole.Img)).ToHaveCountAsync(0);
        await Assertions.Expect(chart.Locator("[data-slot=plot] > svg")).ToHaveAttributeAsync("aria-hidden", "true");
        await Assertions.Expect(image).ToHaveAttributeAsync("aria-label", new Regex(@"^Line chart with 6 data points from Jan to Jun\. Series: Desktop \(73 to 305\), Mobile \(80 to 200\)\.$"));
        // The table is visually hidden, not removed: assistive technology still reads it.
        var table = chart.GetByRole(AriaRole.Table);
        await Assertions.Expect(table).ToHaveCountAsync(1);
        await Assertions.Expect(table.GetByRole(AriaRole.Row)).ToHaveCountAsync(7);
        await Assertions.Expect(table.GetByRole(AriaRole.Cell).Nth(0)).ToHaveTextAsync("186");
        // Its wrapper is clipped to a pixel (sr-only): the table is read by assistive technology but not seen.
        var box = (await table.Locator("xpath=..").BoundingBoxAsync())!;
        Assert.True(box.Width <= 1 && box.Height <= 1, $"the table is visually hidden ({box.Width}x{box.Height})");
    }

    [Fact]
    public async Task Chart_colors_come_from_data_color_and_the_legend_matches_the_series()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-bar");

        await Assertions.Expect(chart.Locator("g[data-series]")).ToHaveCountAsync(2);
        Assert.Equal("primary", await chart.Locator("g[data-series='0']").GetAttributeAsync("data-color"));
        Assert.Equal("info", await chart.Locator("g[data-series='1']").GetAttributeAsync("data-color"));
        // The color is applied through the variable of data-color: the stroke of the first bar is the primary text color.
        var stroke = await chart.Locator("g[data-series='0'] rect").First.EvaluateAsync<string>("e => getComputedStyle(e).stroke");
        var expected = await chart.Locator("g[data-series='0']").EvaluateAsync<string>("e => { const s = document.createElement('i'); s.style.color = 'var(--bx-color-text)'; e.appendChild(s); const c = getComputedStyle(s).color; s.remove(); return c; }");
        Assert.Equal(expected, stroke);
        await Assertions.Expect(chart.GetByRole(AriaRole.List, new() { Name = "Legend" }).GetByRole(AriaRole.Listitem)).ToHaveCountAsync(2);
        // Series differ without color: the second series is filled with a pattern, the first is plain.
        Assert.True(await chart.Locator("defs pattern").Nth(1).Locator("path").CountAsync() > 0);
    }

    [Fact]
    public async Task Chart_data_table_can_be_revealed_with_a_disclosure()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-bar");
        var summary = chart.GetByText("Data table", new() { Exact = true });
        var table = chart.GetByRole(AriaRole.Table);

        await Assertions.Expect(table).ToBeHiddenAsync();
        await summary.ClickAsync();

        await Assertions.Expect(table).ToBeVisibleAsync();
        await Assertions.Expect(table.GetByRole(AriaRole.Row)).ToHaveCountAsync(5);
        await Assertions.Expect(table.GetByRole(AriaRole.Cell).Nth(0)).ToHaveTextAsync("$4,200");
        await AssertNoAccessibilityViolations();
    }

    // ---- Tooltip on hover

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Hovering_a_column_shows_its_values_in_a_tooltip_and_leaving_hides_it(string mode)
    {
        await Open(mode, "chart");
        var chart = Chart("chart-line");
        var tooltip = chart.Locator("[data-slot=tooltip]");
        await Assertions.Expect(tooltip).Not.ToBeVisibleAsync();

        var hit = chart.Locator("g[data-hit][data-index='1']");
        await hit.Locator("rect").HoverAsync();

        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await Assertions.Expect(tooltip).ToContainTextAsync("Feb");
        await Assertions.Expect(tooltip).ToContainTextAsync("Desktop");
        await Assertions.Expect(tooltip).ToContainTextAsync("305");
        await Assertions.Expect(tooltip).ToContainTextAsync("Mobile");
        await Assertions.Expect(tooltip).ToContainTextAsync("200");
        await Assertions.Expect(hit).ToHaveAttributeAsync("data-active", "");
        // The tooltip stays inside the chart.
        var plot = (await chart.Locator("[data-slot=plot]").BoundingBoxAsync())!;
        var box = (await tooltip.BoundingBoxAsync())!;
        Assert.True(box.X >= plot.X - 1 && box.X + box.Width <= plot.X + plot.Width + 1);

        await Page.Mouse.MoveAsync(5, 5);

        await Assertions.Expect(tooltip).Not.ToBeVisibleAsync();
        await Assertions.Expect(hit).Not.ToHaveAttributeAsync("data-active", "");
    }

    [Fact]
    public async Task Tooltip_follows_the_pointer_from_row_to_row()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-bar");
        var tooltip = chart.Locator("[data-slot=tooltip]");

        await chart.Locator("g[data-hit][data-index='0'] rect").HoverAsync();
        await Assertions.Expect(tooltip).ToContainTextAsync("Q1");
        await chart.Locator("g[data-hit][data-index='3'] rect").HoverAsync();

        await Assertions.Expect(tooltip).ToContainTextAsync("Q4");
        await Assertions.Expect(tooltip).ToContainTextAsync("$6,900");
        await Assertions.Expect(tooltip).Not.ToContainTextAsync("Q1");
        // Only one row is marked at a time.
        await Assertions.Expect(chart.Locator("g[data-hit][data-active]")).ToHaveCountAsync(1);
    }

    [Fact]
    public async Task Hovering_a_donut_slice_shows_its_value_and_share()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-donut");
        var tooltip = chart.Locator("[data-slot=tooltip]");

        await chart.ScrollIntoViewIfNeededAsync();
        // A point in the middle of the ring, on the first slice.
        var point = await chart.Locator("path[data-hit][data-index='0']").EvaluateAsync<double[]>(@"e => {
            const svg = e.ownerSVGElement, box = svg.getBoundingClientRect(), m = svg.getScreenCTM();
            const p = e.getPointAtLength(e.getTotalLength() * 0.1);
            const x = p.x * m.a + m.e, y = p.y * m.d + m.f, cx = box.left + box.width / 2, cy = box.top + box.height / 2;
            const d = Math.hypot(x - cx, y - cy), r = d - 18 * m.a;
            return [cx + (x - cx) * r / d, cy + (y - cy) * r / d];
        }");
        await Page.Mouse.MoveAsync((float)point[0], (float)point[1]);

        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await Assertions.Expect(tooltip).ToContainTextAsync("Chrome");
        await Assertions.Expect(tooltip).ToContainTextAsync("54");
        await Assertions.Expect(tooltip).ToContainTextAsync("%");
        await Assertions.Expect(chart.Locator("path[data-hit][data-index='0']")).ToHaveAttributeAsync("data-active", "");
    }

    // ---- Keyboard

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Arrow_keys_move_between_points_and_announce_them(string mode)
    {
        await Open(mode, "chart");
        var chart = Chart("chart-line");
        var plot = chart.Locator("[data-slot=plot]");
        var tooltip = chart.Locator("[data-slot=tooltip]");
        var status = chart.GetByRole(AriaRole.Status);

        await plot.FocusAsync();
        await Assertions.Expect(plot).ToBeFocusedAsync();
        // Focusing alone shows nothing: the chart is named and waits for a key.
        await Assertions.Expect(tooltip).Not.ToBeVisibleAsync();
        // Named by the chart, described by the hint on how to explore it.
        Assert.StartsWith("Line chart with 6 data points", await plot.GetAttributeAsync("aria-label"));
        await Assertions.Expect(chart.Locator("#" + await plot.GetAttributeAsync("aria-describedby"))).ToHaveTextAsync("Chart values. Use the arrow keys to move between points.");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(tooltip).ToBeVisibleAsync();
        await Assertions.Expect(tooltip).ToContainTextAsync("Jan");
        await Assertions.Expect(status).ToHaveTextAsync("Jan: Desktop 186, Mobile 80");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(status).ToHaveTextAsync("Feb: Desktop 305, Mobile 200");
        await Assertions.Expect(tooltip).ToContainTextAsync("305");
        await Assertions.Expect(chart.Locator("g[data-hit][data-index='1']")).ToHaveAttributeAsync("data-active", "");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(status).ToHaveTextAsync("Jan: Desktop 186, Mobile 80");
        await Page.Keyboard.PressAsync("End");
        await Assertions.Expect(status).ToHaveTextAsync("Jun: Desktop 214, Mobile 140");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(status).ToHaveTextAsync("Jun: Desktop 214, Mobile 140");
        await Page.Keyboard.PressAsync("Home");
        await Assertions.Expect(status).ToHaveTextAsync("Jan: Desktop 186, Mobile 80");
        await AssertNoAccessibilityViolations();

        await Page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(tooltip).Not.ToBeVisibleAsync();
        await Assertions.Expect(plot).ToBeFocusedAsync();
    }

    [Fact]
    public async Task Keyboard_exploration_works_on_a_donut_and_leaving_the_chart_hides_the_tooltip()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-donut");
        var plot = chart.Locator("[data-slot=plot]");

        await plot.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(chart.GetByRole(AriaRole.Status)).ToHaveTextAsync(new Regex(@"^Chrome: 54 \(5\d%\)$"));
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(chart.GetByRole(AriaRole.Status)).ToHaveTextAsync(new Regex(@"^Safari: 24 "));

        await Page.Keyboard.PressAsync("Tab");

        await Assertions.Expect(chart.Locator("[data-slot=tooltip]")).Not.ToBeVisibleAsync();
        await Assertions.Expect(chart.Locator("path[data-hit][data-active]")).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Static_markup_stays_after_an_enhanced_navigation_and_the_tooltip_still_works()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-line");
        await chart.Locator("g[data-hit][data-index='2'] rect").HoverAsync();
        await Assertions.Expect(chart.Locator("[data-slot=tooltip]")).ToBeVisibleAsync();

        await Page.EvaluateAsync("() => { window.__enhanced = new Promise((r) => Blazor.addEventListener('enhancedload', () => setTimeout(r, 50), { once: true })); }");
        await Page.EvaluateAsync("() => Blazor.navigateTo(location.pathname + '?enhanced=' + Date.now())");
        await Page.EvaluateAsync("() => window.__enhanced");

        await Assertions.Expect(chart.Locator("[data-slot=tooltip]")).Not.ToBeVisibleAsync();
        await chart.Locator("g[data-hit][data-index='4'] rect").HoverAsync();
        await Assertions.Expect(chart.Locator("[data-slot=tooltip]")).ToContainTextAsync("May");
    }

    // ---- Motion

    [Fact]
    public async Task Animation_follows_the_reduced_motion_preference()
    {
        await Open("ssr", "chart");
        var tooltip = Chart("chart-line").Locator("[data-slot=tooltip]");

        Assert.NotEqual("0s", await tooltip.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration"));

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.Equal("0s", await tooltip.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration"));
        Assert.Equal("0s", await Chart("chart-line").Locator("g[data-hit] path").First.EvaluateAsync<string>("e => getComputedStyle(e).transitionDuration"));
    }


    // ---- Review findings (waves C and D)

    [Fact]
    public async Task Tooltip_shows_the_new_values_when_the_tip_of_the_active_row_changes()
    {
        await Open("ssr", "chart");
        var chart = Chart("chart-line");
        var tooltip = chart.Locator("[data-slot=tooltip]");
        var hit = chart.Locator("g[data-hit][data-index='1'] rect");
        await hit.HoverAsync();
        await Assertions.Expect(tooltip).ToHaveAttributeAsync("data-state", "open");

        // What a Blazor render does to the hidden tips while the pointer stays on the row.
        await chart.Locator("[data-slot=tips] [data-tip='1'] p:last-child span:last-child").EvaluateAsync("e => { e.textContent = 'NEW-999'; }");
        var box = (await hit.BoundingBoxAsync())!;
        await Page.Mouse.MoveAsync(box.X + box.Width / 2 + 2, box.Y + box.Height / 2 + 2);

        await Assertions.Expect(tooltip).ToContainTextAsync("NEW-999");
    }

    [Fact]
    public async Task Escape_closes_the_tooltip_first_and_the_modal_around_the_chart_on_the_next_press()
    {
        await Open("ssr", "chart");
        await Page.EvaluateAsync("""
            () => {
                const dialog = document.createElement('dialog');
                dialog.id = 'around-chart';
                dialog.append(document.querySelector('[data-testid=chart-line]'));
                document.body.append(dialog);
                dialog.showModal();
            }
            """);
        var chart = Chart("chart-line");
        var plot = chart.Locator("[data-slot=plot]");
        var tooltip = chart.Locator("[data-slot=tooltip]");
        await plot.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Assertions.Expect(tooltip).ToHaveAttributeAsync("data-state", "open");

        await Page.Keyboard.PressAsync("Escape");

        await Assertions.Expect(tooltip).ToHaveAttributeAsync("data-state", "closed");
        Assert.True(await Page.EvaluateAsync<bool>("() => document.getElementById('around-chart').open"), "the first Escape only dismisses the tooltip");

        await Page.Keyboard.PressAsync("Escape");

        await Page.WaitForFunctionAsync("() => !document.getElementById('around-chart').open");
    }
}
