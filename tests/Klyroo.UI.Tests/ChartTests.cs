using System.Globalization;
using Klyroo.UI.Internal;

namespace Klyroo.UI.Tests;

/// <summary>The "Chart" family: KlyChart, drawn as SVG in C#.</summary>
public class ChartTests : KlyrooTestContext
{
    private sealed record Sale(string Month, double? Desktop, double? Mobile);

    private static readonly Sale[] Sales =
    [
        new("January", 186, 80),
        new("February", 305, 200),
        new("March", 237, 120),
        new("April", 73, 190),
        new("May", 209, 130),
        new("June", 214, 140),
    ];

    private static readonly KlyChartCategory<Sale>[] Both =
    [
        new("desktop", s => s.Desktop) { Label = "Desktop" },
        new("mobile", s => s.Mobile) { Label = "Mobile" },
    ];

    private IRenderedComponent<KlyChart<Sale>> Chart(Action<ComponentParameterCollectionBuilder<KlyChart<Sale>>>? configure = null, IReadOnlyList<Sale>? data = null, IReadOnlyList<KlyChartCategory<Sale>>? categories = null) =>
        Render<KlyChart<Sale>>(p =>
        {
            p.Add(c => c.Data, data ?? Sales)
             .Add(c => c.Index, s => s.Month)
             .Add(c => c.Categories, categories ?? Both);
            configure?.Invoke(p);
        });

    /// <summary>The name of the chart: on the svg image, or on the focusable group when the chart is interactive.</summary>
    private static string AccessibleName(IRenderedComponent<KlyChart<Sale>> cut) =>
        cut.Find("svg").GetAttribute("aria-label") ?? cut.Find("[data-slot=plot]").GetAttribute("aria-label")!;

    private static string[] Texts(IRenderedComponent<KlyChart<Sale>> cut, string selector) =>
        cut.FindAll(selector).Select(e => e.TextContent.Trim()).ToArray();

    // ---- Scale

    [Fact]
    public void Nice_scale_rounds_a_range_out_to_ticks_on_1_2_5()
    {
        var (min, max, step, ticks) = ChartLayout.NiceScale(0, 305);
        Assert.Equal((0, 350, 50), (min, max, step));
        Assert.Equal([0, 50, 100, 150, 200, 250, 300, 350], ticks);

        var small = ChartLayout.NiceScale(0, 1);
        Assert.Equal([0, 0.2, 0.4, 0.6, 0.8, 1], small.Ticks);

        var negative = ChartLayout.NiceScale(-30, 80);
        Assert.Equal(-40, negative.Min);
        Assert.Equal(80, negative.Max);
        Assert.Contains(0, negative.Ticks);
    }

    [Fact]
    public void Nice_scale_of_a_flat_range_still_has_ticks()
    {
        Assert.Equal([0, 0.2, 0.4, 0.6, 0.8, 1], ChartLayout.NiceScale(0, 0).Ticks);
        var five = ChartLayout.NiceScale(5, 5);
        Assert.True(five.Min < 5 && five.Max > 5);
    }

    // ---- Structure and accessibility

    [Fact]
    public void Chart_is_an_image_with_a_generated_summary()
    {
        var cut = Chart(p => p.Add(c => c.Tooltip, false));

        var svg = cut.Find("svg[role=img]");
        var label = svg.GetAttribute("aria-label")!;
        Assert.StartsWith("Line chart with 6 data points from January to June.", label);
        Assert.Contains("Desktop (73 to 305)", label);
        Assert.Contains("Mobile (80 to 200)", label);
        Assert.Equal("0 0 640 300", svg.GetAttribute("viewBox"));
    }

    [Fact]
    public void An_aria_label_replaces_the_summary_and_a_description_is_linked()
    {
        var cut = Chart(p => p.Add(c => c.AriaLabel, "Sales by month").Add(c => c.AriaDescription, "Desktop leads every month but April.").Add(c => c.Tooltip, false));

        var svg = cut.Find("svg[role=img]");
        Assert.Equal("Sales by month", svg.GetAttribute("aria-label"));
        var description = cut.Find("#" + svg.GetAttribute("aria-describedby"));
        Assert.Equal("Desktop leads every month but April.", description.TextContent);
        Assert.Contains("sr-only", description.ClassList);
    }

    [Fact]
    public void Axes_have_nice_ticks_and_labels_and_the_grid_follows_the_ticks()
    {
        var cut = Chart();

        // A line chart's axis fits its values (73 to 305); bars and areas always include zero.
        Assert.Equal(["50", "100", "150", "200", "250", "300", "350"], Texts(cut, "svg > text").Where(t => t.Length <= 3 && char.IsDigit(t[0])).ToArray());
        Assert.Equal(["January", "February", "March", "April", "May", "June"], Texts(cut, "svg > text").Where(t => char.IsLetter(t[0])).ToArray());
        // One grid line per tick, and the line of the index axis.
        Assert.Equal(8, cut.FindAll("svg > line").Count);
        Assert.Contains("0", Texts(Chart(p => p.Add(c => c.Type, KlyChartType.Bar)), "svg > text"));
    }

    [Fact]
    public void Grid_and_axes_can_be_turned_off()
    {
        var cut = Chart(p => p.Add(c => c.Grid, false).Add(c => c.XAxis, false).Add(c => c.YAxis, false));

        Assert.Empty(cut.FindAll("svg > line"));
        Assert.Empty(cut.FindAll("svg > text"));
    }

    [Fact]
    public void Axis_titles_are_drawn()
    {
        var cut = Chart(p => p.Add(c => c.XLabel, "Month").Add(c => c.YLabel, "Visitors"));

        var texts = Texts(cut, "svg > text");
        Assert.Contains("Month", texts);
        Assert.Contains("Visitors", texts);
        Assert.Contains("rotate(-90", cut.FindAll("svg > text").First(t => t.TextContent == "Visitors").GetAttribute("transform"));
    }

    [Fact]
    public void Value_labels_use_the_current_culture_and_a_custom_format_wins()
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            Sale[] big = [new("a", 1500, null), new("b", 2500, null)];
            var cut = Chart(null, big, [new("d", s => s.Desktop)]);

            Assert.Contains(2000.0.ToString("#,0.##", new CultureInfo("fr")), Texts(cut, "svg > text"));

            var formatted = Chart(p => p.Add(c => c.Format, v => $"${v}"), Sales);
            Assert.Contains("$100", Texts(formatted, "svg > text"));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    [Fact]
    public void Dates_in_the_index_are_formatted_with_the_current_culture()
    {
        var cut = Render<KlyChart<DateTime>>(p => p
            .Add(c => c.Data, [new DateTime(2026, 3, 5), new DateTime(2026, 3, 6)])
            .Add(c => c.Index, d => d)
            .Add(c => c.Categories, new KlyChartCategory<DateTime>[] { new("v", d => d.Day) })
            .Add(c => c.IndexFormat, "MMM d"));

        Assert.Equal(["Mar 5", "Mar 6"], cut.FindAll("svg > text").Select(t => t.TextContent).Where(t => t.StartsWith("Mar")).ToArray());
    }

    // ---- Data table

    [Fact]
    public void Data_table_holds_the_same_data_and_is_visually_hidden_by_default()
    {
        var cut = Chart();

        var table = cut.Find("table");
        Assert.Equal("Line chart", table.QuerySelector("caption")!.TextContent);
        Assert.Equal(["Category", "Desktop", "Mobile"], table.QuerySelectorAll("thead th").Select(t => t.TextContent.Trim()));
        var rows = table.QuerySelectorAll("tbody tr").ToList();
        Assert.Equal(6, rows.Count);
        Assert.Equal(["February", "305", "200"], rows[1].Children.Select(c => c.TextContent.Trim()));
        Assert.Equal("row", rows[0].QuerySelector("th")!.GetAttribute("scope"));
        Assert.Contains("sr-only", table.ParentElement!.ClassList);
        Assert.Null(cut.FindAll("details").FirstOrDefault());
    }

    [Fact]
    public void Data_table_can_be_shown_or_put_behind_a_disclosure()
    {
        var visible = Chart(p => p.Add(c => c.Table, KlyChartTable.Visible));
        Assert.DoesNotContain("sr-only", visible.Find("table").ParentElement!.ClassList);

        var toggle = Chart(p => p.Add(c => c.Table, KlyChartTable.Toggle).Add(c => c.XLabel, "Month"));
        Assert.Equal("Data table", toggle.Find("details > summary").TextContent);
        Assert.NotNull(toggle.Find("details table"));
        Assert.Equal("Month", toggle.Find("thead th").TextContent.Trim());
    }

    [Fact]
    public void Gaps_are_empty_cells_and_break_the_line()
    {
        Sale[] gap = [new("a", 1, 1), new("b", null, 2), new("c", 3, 3), new("d", 4, 4)];
        var cut = Chart(data: gap);

        Assert.Equal("", cut.FindAll("tbody tr")[1].Children[1].TextContent.Trim());
        var desktop = cut.Find("g[data-series='0']");
        // A line before the gap and one after it: the missing value is not drawn as zero.
        Assert.Equal(2, desktop.QuerySelectorAll("path").Length);
        var mobile = cut.Find("g[data-series='1']");
        Assert.Single(mobile.QuerySelectorAll("path"));
    }

    [Fact]
    public void Empty_data_renders_a_chart_that_says_so()
    {
        var cut = Chart(data: []);

        Assert.Contains("No data", cut.Find("svg").GetAttribute("aria-label"));
        Assert.Empty(cut.FindAll("[data-hit]"));
        Assert.Null(cut.Find("[data-slot=plot]").GetAttribute("tabindex"));
    }

    // ---- Colors and distinguishing without color

    [Fact]
    public void Series_get_their_color_from_the_tokens_of_data_color_and_never_from_a_palette()
    {
        var cut = Chart(null, null, [new("desktop", s => s.Desktop), new("mobile", s => s.Mobile) { Color = KlyColor.Success }]);

        Assert.Equal(["chart-1", "success"], cut.FindAll("g[data-series]").Select(g => g.GetAttribute("data-color")));
        Assert.Equal(["chart-1", "success"], cut.FindAll("li[data-color]").Select(g => g.GetAttribute("data-color")));
        var html = cut.Markup;
        Assert.DoesNotContain("fill=\"#", html);
        Assert.DoesNotContain("stroke=\"#", html);
        Assert.DoesNotContain("zinc-", html);
        Assert.DoesNotContain("rgb(", html);
        Assert.Contains("stroke-(--kly-color-text)", cut.Find("g[data-series='0'] path").GetAttribute("class"));
    }

    [Fact]
    public void Colors_replace_the_palette_in_order()
    {
        var cut = Chart(p => p.Add(c => c.Colors, new[] { KlyColor.Error, KlyColor.Info }));

        Assert.Equal(["error", "info"], cut.FindAll("g[data-series]").Select(g => g.GetAttribute("data-color")));
    }

    [Fact]
    public void Lines_differ_by_dash_pattern_and_markers_by_shape()
    {
        var cut = Chart(p => p.Add(c => c.Points, true));

        var first = cut.Find("g[data-series='0'] path");
        var second = cut.Find("g[data-series='1'] path");
        Assert.Null(first.GetAttribute("stroke-dasharray"));
        Assert.NotNull(second.GetAttribute("stroke-dasharray"));
        var markers = cut.FindAll("g[data-hit][data-index='0'] path").Select(m => m.GetAttribute("d")!).ToList();
        Assert.Equal(2, markers.Count);
        Assert.NotEqual(markers[0], markers[1]);
        // Points always shown: the marker is opaque instead of waiting for the active row.
        Assert.Contains("opacity-100", cut.Find("g[data-hit] path").GetAttribute("class"));
    }

    [Fact]
    public void Markers_wait_for_the_active_row_unless_points_are_on()
    {
        var cut = Chart();

        var marker = cut.Find("g[data-hit] path").GetAttribute("class")!;
        Assert.Contains("opacity-0", marker);
        Assert.Contains("group-data-active/col:opacity-100", marker);
    }

    [Fact]
    public void Bars_and_areas_differ_by_fill_pattern_and_the_legend_shows_the_same_pattern()
    {
        var bars = Chart(p => p.Add(c => c.Type, KlyChartType.Bar));

        var patterns = bars.FindAll("defs pattern");
        Assert.Equal(2, patterns.Count);
        Assert.Equal(["chart-1", "chart-2"], patterns.Select(x => x.GetAttribute("data-color")));
        Assert.NotEmpty(patterns[1].QuerySelectorAll("path"));
        Assert.Empty(patterns[0].QuerySelectorAll("path"));
        var id = patterns[1].GetAttribute("id");
        Assert.Contains(bars.FindAll("rect[fill]"), r => r.GetAttribute("fill") == $"url(#{id})");
        Assert.Equal($"url(#{id})", bars.FindAll("li svg rect")[1].GetAttribute("fill"));

        var area = Chart(p => p.Add(c => c.Type, KlyChartType.Area));
        Assert.Equal(2, area.FindAll("defs pattern").Count);
        Assert.Equal(2, area.FindAll("g[data-series] > path[fill^='url(#']").Count);
    }

    [Fact]
    public void Line_charts_need_no_pattern_definitions()
    {
        Assert.Empty(Chart().FindAll("defs"));
    }

    // ---- Types

    [Fact]
    public void Line_chart_has_a_path_per_series_and_a_hit_row_per_index()
    {
        var cut = Chart();

        Assert.Equal(2, cut.FindAll("g[data-series]").Count);
        Assert.Equal(6, cut.FindAll("g[data-hit]").Count);
        Assert.Equal(["0", "1", "2", "3", "4", "5"], cut.FindAll("g[data-hit]").Select(g => g.GetAttribute("data-index")));
        var d = cut.Find("g[data-series='0'] path").GetAttribute("d")!;
        Assert.StartsWith("M", d);
        Assert.Equal(5, d.Count(c => c == 'L'));
    }

    [Theory]
    [InlineData(KlyChartCurve.Linear, "L", "C")]
    [InlineData(KlyChartCurve.Monotone, "C", "L")]
    public void Curve_shapes_the_line(KlyChartCurve curve, string present, string absent)
    {
        var cut = Chart(p => p.Add(c => c.Curve, curve));

        var d = cut.Find("g[data-series='0'] path").GetAttribute("d")!;
        Assert.Contains(present, d);
        Assert.DoesNotContain(absent, d);
    }

    [Fact]
    public void Step_curve_makes_right_angles()
    {
        var path = ChartLayout.CurvePath([(0, 10), (10, 20), (20, 5)], KlyChartCurve.Step);

        Assert.Equal("M0 10 L5 10 L5 20 L10 20 L15 20 L15 5 L20 5", path);
    }

    [Fact]
    public void Monotone_curve_does_not_overshoot_the_values()
    {
        var path = ChartLayout.CurvePath([(0, 0), (10, 0), (20, 10), (30, 10)], KlyChartCurve.Monotone);

        // Control points of the flat segments stay on the flat level.
        var numbers = path.Replace("M", "").Replace("C", "").Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(double.Parse).ToArray();
        Assert.All(Enumerable.Range(0, numbers.Length / 2).Select(i => numbers[i * 2 + 1]), y => Assert.InRange(y, 0, 10));
    }

    [Fact]
    public void Area_chart_closes_each_shape_down_to_the_baseline()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Area));

        var areas = cut.FindAll("g[data-series='0'] > path[fill^='url(#']");
        Assert.Single(areas);
        Assert.EndsWith("Z", areas[0].GetAttribute("d"));
        // The outline is a separate path, so it can carry the dash pattern.
        Assert.Equal(2, cut.Find("g[data-series='0']").QuerySelectorAll("path").Length);
    }

    [Fact]
    public void Stacked_areas_sit_on_each_other()
    {
        var layout = ChartLayout.Build(KlyChartType.Area, false, true, KlyChartCurve.Linear, ["a", "b"],
            [(IReadOnlyList<double?>)new double?[] { 10, 20 }, new double?[] { 5, 5 }], 640, 300, true, true, false, false, 24, v => v.ToString());

        // The second series starts where the first ends: its upper edge is higher than the first's.
        Assert.True(layout.Series[1].Points[0].Y < layout.Series[0].Points[0].Y);
        Assert.Equal(25, layout.DomainMax);
    }

    [Fact]
    public void Bar_chart_draws_a_bar_per_value_side_by_side()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Bar));

        var first = cut.FindAll("g[data-series='0'] rect");
        var second = cut.FindAll("g[data-series='1'] rect");
        Assert.Equal(6, first.Count);
        Assert.Equal(6, second.Count);
        var x0 = double.Parse(first[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
        var x1 = double.Parse(second[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
        Assert.True(x1 > x0);
        // Bars grow with the value.
        double H(AngleSharp.Dom.IElement r) => double.Parse(r.GetAttribute("height")!, CultureInfo.InvariantCulture);
        Assert.True(H(first[1]) > H(first[0]));
        Assert.True(H(first[3]) < H(first[0]));
    }

    [Fact]
    public void Stacked_bars_share_a_position_and_reach_the_total()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Bar).Add(c => c.Stacked, true));

        var a = cut.FindAll("g[data-series='0'] rect")[0];
        var b = cut.FindAll("g[data-series='1'] rect")[0];
        Assert.Equal(a.GetAttribute("x"), b.GetAttribute("x"));
        var top = double.Parse(b.GetAttribute("y")!, CultureInfo.InvariantCulture);
        var bottom = double.Parse(a.GetAttribute("y")!, CultureInfo.InvariantCulture) + double.Parse(a.GetAttribute("height")!, CultureInfo.InvariantCulture);
        Assert.True(top < bottom);
        // 305 + 200 is the tallest stack: the axis reaches 600.
        Assert.Contains("600", Texts(cut, "svg > text"));
    }

    [Fact]
    public void Horizontal_bars_put_the_index_on_the_y_axis_and_grow_to_the_right()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Bar).Add(c => c.Horizontal, true));

        var bars = cut.FindAll("g[data-series='0'] rect");
        double W(AngleSharp.Dom.IElement r) => double.Parse(r.GetAttribute("width")!, CultureInfo.InvariantCulture);
        Assert.True(W(bars[1]) > W(bars[0]));
        var january = cut.FindAll("svg > text").First(t => t.TextContent == "January");
        Assert.Equal("end", january.GetAttribute("text-anchor"));
        Assert.Contains("Horizontal bar chart", AccessibleName(cut));
        // The grid lines are vertical.
        var line = cut.FindAll("svg > line")[0];
        Assert.Equal(line.GetAttribute("x1"), line.GetAttribute("x2"));
    }

    [Fact]
    public void Negative_values_reach_below_a_zero_line()
    {
        Sale[] swing = [new("a", -20, null), new("b", 40, null)];
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Bar), swing, [new("d", s => s.Desktop)]);

        Assert.Contains("-20", Texts(cut, "svg > text"));
        Assert.Contains(cut.FindAll("svg > line"), l => l.GetAttribute("class")!.Contains("border-accented") && l.GetAttribute("y1") == l.GetAttribute("y2"));
        var bars = cut.FindAll("g[data-series='0'] rect");
        Assert.True(double.Parse(bars[0].GetAttribute("y")!, CultureInfo.InvariantCulture) > double.Parse(bars[1].GetAttribute("y")!, CultureInfo.InvariantCulture));
    }

    // ---- Donut

    [Fact]
    public void Donut_has_a_slice_per_row_sized_by_the_first_category_and_a_legend_with_values()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Donut));

        var slices = cut.FindAll("path[data-hit]");
        Assert.Equal(6, slices.Count);
        Assert.Equal(["chart-1", "chart-2", "chart-3", "chart-4", "chart-5", "chart-6"], slices.Select(s => s.GetAttribute("data-color")));
        Assert.Equal(6, cut.FindAll("ul li").Count);
        Assert.Equal("186", cut.FindAll("ul li")[0].QuerySelector("span:last-child")!.TextContent);
        var label = AccessibleName(cut);
        Assert.StartsWith("Donut chart. Slices: January 186 (", label);
        // The table has one column of values: the first category.
        Assert.Equal(["Category", "Desktop"], cut.FindAll("thead th").Select(t => t.TextContent.Trim()));
    }

    [Fact]
    public void Donut_slices_add_up_to_a_full_turn_and_a_single_slice_is_a_whole_ring()
    {
        var layout = ChartLayout.Build(KlyChartType.Donut, false, false, KlyChartCurve.Linear, ["a", "b"],
            [(IReadOnlyList<double?>)new double?[] { 1, 3 }], 200, 200, true, true, false, false, 24, v => v.ToString());
        Assert.Equal([0.25, 0.75], layout.Slices.Select(s => s.Fraction));
        Assert.Equal(70, layout.InnerRadius);

        var whole = ChartLayout.Build(KlyChartType.Donut, false, false, KlyChartCurve.Linear, ["a"],
            [(IReadOnlyList<double?>)new double?[] { 5 }], 200, 200, true, true, false, false, 24, v => v.ToString());
        Assert.Equal(2, whole.Slices[0].Path.Split('M', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Donut_center_content_is_rendered_over_the_hole()
    {
        var cut = Chart(p => p
            .Add(c => c.Type, KlyChartType.Donut)
            .Add(c => c.Center, b => b.AddMarkupContent(0, "<strong data-testid=total>1,224</strong>")));

        Assert.Equal("1,224", cut.Find("[data-testid=total]").TextContent);
        Assert.Contains("absolute", cut.Find("[data-testid=total]").ParentElement!.ClassList);
    }

    // ---- Legend

    [Fact]
    public void Legend_shows_with_several_series_and_not_with_one_unless_asked()
    {
        Assert.NotNull(Chart().Find("ul[aria-label=Legend]"));
        KlyChartCategory<Sale>[] one = [new("d", s => s.Desktop) { Label = "Desktop" }];
        Assert.Empty(Chart(null, null, one).FindAll("ul"));
        Assert.Single(Chart(p => p.Add(c => c.Legend, true), null, one).FindAll("ul"));
        Assert.Empty(Chart(p => p.Add(c => c.Legend, false)).FindAll("ul"));
    }

    [Fact]
    public void Legend_swatch_of_a_line_shows_its_dash_and_marker()
    {
        var cut = Chart(p => p.Add(c => c.Points, true));

        var swatches = cut.FindAll("li svg");
        Assert.Null(swatches[0].QuerySelector("path")!.GetAttribute("stroke-dasharray"));
        Assert.NotNull(swatches[1].QuerySelector("path")!.GetAttribute("stroke-dasharray"));
        Assert.Equal(2, swatches[0].QuerySelectorAll("path").Length);
        Assert.All(swatches, s => Assert.Equal("true", s.GetAttribute("aria-hidden")));
    }

    // ---- Tooltip and keyboard

    [Fact]
    public void Interactive_chart_can_be_focused_and_has_a_hidden_tip_per_row()
    {
        var cut = Chart();

        var plot = cut.Find("[data-slot=plot]");
        Assert.Equal("0", plot.GetAttribute("tabindex"));
        Assert.Equal("group", plot.GetAttribute("role"));
        Assert.StartsWith("Line chart with 6 data points", plot.GetAttribute("aria-label"));
        var tips = cut.FindAll("[data-slot=tips] > [data-tip]");
        Assert.Equal(6, tips.Count);
        Assert.NotNull(cut.Find("[data-slot=tips]").GetAttribute("hidden"));
        Assert.Equal("February: Desktop 305, Mobile 200", tips[1].GetAttribute("data-text"));
        Assert.Equal("February", tips[1].QuerySelector("p")!.TextContent);
        var rows = tips[1].QuerySelectorAll("p").Skip(1).ToList();
        Assert.Equal(["Desktop", "305"], rows[0].QuerySelectorAll("span").Where(s => s.TextContent.Length > 0).Select(s => s.TextContent));
        Assert.Equal("chart-1", rows[0].GetAttribute("data-color"));
        Assert.Equal("true", cut.Find("[data-slot=tooltip]").GetAttribute("aria-hidden"));
        Assert.Equal("status", cut.Find("[data-slot=status]").GetAttribute("role"));
        Assert.Empty(cut.Find("[data-slot=tooltip]").Children);
    }

    [Fact]
    public void Tooltip_off_leaves_a_plain_image()
    {
        var cut = Chart(p => p.Add(c => c.Tooltip, false));

        Assert.Null(cut.Find("[data-slot=plot]").GetAttribute("tabindex"));
        Assert.Null(cut.Find("[data-slot=plot]").GetAttribute("role"));
        Assert.Empty(cut.FindAll("[data-slot=tips]"));
        Assert.Empty(cut.FindAll("[data-slot=tooltip]"));
    }

    [Fact]
    public void Donut_tip_gives_the_value_and_the_share()
    {
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Donut));

        var tip = cut.Find("[data-tip='0']");
        Assert.StartsWith("January: 186 (", tip.GetAttribute("data-text"));
        Assert.Contains("Desktop", tip.TextContent);
        Assert.Contains("%", tip.TextContent);
    }

    [Fact]
    public void Animation_respects_reduced_motion()
    {
        var themes = new KlyrooOptions().Chart;

        var animated = themes.AllClassTokens().Where(t => t.Contains("transition")).ToList();
        Assert.NotEmpty(animated);
        Assert.All(animated, t => Assert.StartsWith("motion-safe:", t));
    }

    [Fact]
    public void Static_rendering_gives_the_same_markup()
    {
        using var ctx = new StaticContext();
        var cut = ctx.Render<KlyChart<Sale>>(p => p.Add(c => c.Data, Sales).Add(c => c.Index, s => s.Month).Add(c => c.Categories, Both));

        Assert.Equal(6, cut.FindAll("g[data-hit]").Count);
        Assert.NotNull(cut.Find("kly-chart"));
    }

    private sealed class StaticContext() : KlyrooTestContext(interactive: false);

    // ---- Theming

    [Fact]
    public void Ui_and_class_override_slots()
    {
        var cut = Chart(p => p.Add(c => c.Class, "my-chart").Add(c => c.Ui, new ChartUi { Legend = "justify-start" }));

        Assert.Contains("my-chart", cut.Find("kly-chart").ClassList);
        Assert.Contains("justify-start", cut.Find("ul").ClassList);
        Assert.DoesNotContain("justify-center", cut.Find("ul").ClassList);
    }

    [Fact]
    public void Height_and_width_set_the_drawing_and_its_largest_size()
    {
        var cut = Chart(p => p.Add(c => c.Height, 200).Add(c => c.Width, 500));

        Assert.Equal("0 0 500 200", cut.Find("svg").GetAttribute("viewBox"));
        Assert.Contains("max-width:500px", cut.Find("[data-slot=plot]").GetAttribute("style"));
    }


    // ---- Review findings (waves C and D)

    [Theory]
    [InlineData(1e16, 1e16 + 2)]
    [InlineData(-1e300, 1e300)]
    [InlineData(-double.MaxValue, double.MaxValue)]
    [InlineData(0, 1e-300)]
    public async Task Nice_scale_of_extreme_values_finishes_with_a_bounded_number_of_ticks(double min, double max)
    {
        var task = Task.Run(() => ChartLayout.NiceScale(min, max), Xunit.TestContext.Current.CancellationToken);

        var finished = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(3), Xunit.TestContext.Current.CancellationToken));
        Assert.Same(task, finished);
        var (lo, hi, _, ticks) = await task;
        Assert.InRange(ticks.Count, 1, 1000);
        Assert.All(ticks, t => Assert.True(double.IsFinite(t)));
        Assert.True(double.IsFinite(lo) && double.IsFinite(hi));
    }

    [Fact]
    public void Chart_formats_values_and_index_with_the_Culture_parameter()
    {
        var before = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("en-US");
        try
        {
            var cut = Chart(
                p => p.Add(c => c.Culture, new CultureInfo("de-DE")).Add(c => c.Table, KlyChartTable.Visible),
                data: [new("a", 1234.5, 0.5)]);

            Assert.Contains("1.234,5", cut.Find("tbody td").TextContent);
            Assert.Contains("0,5", cut.FindAll("tbody td")[1].TextContent);
            Assert.Contains("1.234,5", cut.Find("[data-tip]").GetAttribute("data-text"));
            Assert.DoesNotContain("1,234.5", cut.Markup);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public async Task Chart_rebuilds_its_layout_on_a_parent_render_and_not_on_its_own_renders()
    {
        var calls = 0;
        Func<Sale, object?> index = s => { calls++; return s.Month; };
        Action<ComponentParameterCollectionBuilder<KlyChart<Sale>>> parameters = p =>
            p.Add(c => c.Data, Sales).Add(c => c.Index, index).Add(c => c.Categories, Both);
        var cut = Render(parameters);
        var first = calls;
        Assert.True(first > 0);

        // The chart's own re-renders (state changes inside the chart) reuse the layout.
        var stateHasChanged = typeof(Microsoft.AspNetCore.Components.ComponentBase)
            .GetMethod("StateHasChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        await cut.InvokeAsync(() => stateHasChanged.Invoke(cut.Instance, null));
        await cut.InvokeAsync(() => stateHasChanged.Invoke(cut.Instance, null));
        Assert.Equal(first, calls);

        // A parent render may come with a list changed in place: the layout is built again.
        cut.Render(parameters);
        Assert.True(calls > first);
    }

    [Fact]
    public void Chart_shows_new_values_when_the_data_list_changed_in_place_with_the_same_count()
    {
        var data = new List<Sale> { new("a", 1, null), new("b", 2, null), new("c", 3, null) };
        var categories = new[] { new KlyChartCategory<Sale>("desktop", s => s.Desktop) };
        Func<Sale, object?> index = s => s.Month;
        Action<ComponentParameterCollectionBuilder<KlyChart<Sale>>> parameters = p =>
            p.Add(c => c.Data, data).Add(c => c.Index, index).Add(c => c.Categories, categories);
        var cut = Render(parameters);
        Assert.Contains("a: desktop 1", cut.Find("[data-tip='0']").GetAttribute("data-text"));

        data[0] = new("a", 100, null);
        cut.Render(parameters);
        Assert.Contains("a: desktop 100", cut.Find("[data-tip='0']").GetAttribute("data-text"));

        data.RemoveAt(0);
        data.Add(new("d", 42, null));
        cut.Render(parameters);
        Assert.Contains("d: desktop 42", cut.Find("[data-tip='2']").GetAttribute("data-text"));
        Assert.Contains("b: desktop 2", cut.Find("[data-tip='0']").GetAttribute("data-text"));
    }

    [Fact]
    public void Chart_rebuilds_when_the_data_changes()
    {
        var calls = 0;
        Func<Sale, object?> index = s => { calls++; return s.Month; };
        var cut = Render<KlyChart<Sale>>(p => p.Add(c => c.Data, Sales).Add(c => c.Index, index).Add(c => c.Categories, Both));
        var first = calls;

        cut.Render(p => p.Add(c => c.Data, Sales.Take(3).ToArray()).Add(c => c.Index, index).Add(c => c.Categories, Both));

        Assert.True(calls > first);
        Assert.Equal(3, cut.FindAll("[data-slot=tips] > [data-tip]").Count);
    }

    [Fact]
    public void Chart_focus_target_is_named_by_the_chart_and_the_hint_is_its_description()
    {
        var cut = Chart(p => p.Add(c => c.AriaLabel, "Sales by month"));

        var plot = cut.Find("[data-slot=plot]");
        Assert.Equal("Sales by month", plot.GetAttribute("aria-label"));
        var hint = cut.Find("#" + plot.GetAttribute("aria-describedby"));
        Assert.Equal("Chart values. Use the arrow keys to move between points.", hint.TextContent.Trim());
    }

    [Fact]
    public void Interactive_chart_names_the_group_once_and_hides_the_drawing_and_the_hint_from_its_content()
    {
        var cut = Chart(p => p.Add(c => c.AriaLabel, "Sales by month"));

        var plot = cut.Find("[data-slot=plot]");
        var svg = plot.QuerySelector("svg")!;
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Null(svg.GetAttribute("role"));
        Assert.Null(svg.GetAttribute("aria-label"));
        // The hint describes the group but is not part of its content.
        var hintId = plot.GetAttribute("aria-describedby");
        Assert.NotNull(hintId);
        Assert.Null(plot.QuerySelector("#" + hintId));
        Assert.True(cut.Find("#" + hintId).HasAttribute("hidden"));
    }

    [Fact]
    public void Static_chart_keeps_a_named_image()
    {
        var cut = Chart(p => p.Add(c => c.AriaLabel, "Sales by month").Add(c => c.Tooltip, false));

        var svg = cut.Find("[data-slot=plot] svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Sales by month", svg.GetAttribute("aria-label"));
        Assert.Null(svg.GetAttribute("aria-hidden"));
        Assert.Null(cut.Find("[data-slot=plot]").GetAttribute("role"));
    }

    [Fact]
    public void Interactive_chart_keeps_the_description_on_the_group()
    {
        var cut = Chart(p => p.Add(c => c.AriaDescription, "Peak in February"));

        var plot = cut.Find("[data-slot=plot]");
        var ids = plot.GetAttribute("aria-describedby")!.Split(' ');
        Assert.Equal(2, ids.Length);
        Assert.Contains("Peak in February", cut.Find("#" + ids[1]).TextContent);
        Assert.Null(plot.QuerySelector("#" + ids[1]));
    }

    [Theory]
    [InlineData(KlyChartType.Line)]
    [InlineData(KlyChartType.Bar)]
    [InlineData(KlyChartType.Area)]
    public void Subnormal_domains_render_finite_coordinates(KlyChartType type)
    {
        foreach (var (a, b) in new[] { (0.0, double.Epsilon), (-double.Epsilon, double.Epsilon) })
        {
            var data = new List<Sale> { new("a", a, null), new("b", b, null) };
            var cut = Chart(p => p.Add(c => c.Type, type), data, [new("desktop", s => s.Desktop)]);

            Assert.DoesNotContain("NaN", cut.Markup);
            Assert.DoesNotContain("Infinity", cut.Markup);
        }
    }

    // ---- Selection

    [Fact]
    public void A_chart_without_OnSelect_is_not_selectable_and_has_no_pick_handler()
    {
        var cut = Chart();

        Assert.False(cut.Find("kly-chart").HasAttribute("data-selectable"));
        Assert.DoesNotContain("cursor-pointer", cut.Find("[data-hit] rect").GetAttribute("class"));
        Assert.DoesNotContain("select", cut.Find("[hidden][id$=-hint]").TextContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void A_chart_with_OnSelect_is_selectable_with_a_pointer_cursor_and_says_so_in_its_hint()
    {
        var cut = Chart(p => p.Add(c => c.OnSelect, _ => { }));

        Assert.True(cut.Find("kly-chart").HasAttribute("data-selectable"));
        Assert.Contains("cursor-pointer", cut.Find("[data-hit] rect").GetAttribute("class"));
        Assert.Contains("Press Enter", cut.Find("[hidden][id$=-hint]").TextContent);
    }

    [Fact]
    public async Task A_pick_reports_the_row_the_series_the_value_and_the_item()
    {
        KlyChartSelectEventArgs<Sale>? received = null;
        var cut = Chart(p => p.Add(c => c.OnSelect, e => received = e));

        await cut.Find("kly-chart").TriggerEventAsync("onklypick", new KlyPickEventArgs { Row = 1, Series = 1 });

        Assert.NotNull(received);
        Assert.Equal(1, received.Row);
        Assert.Equal("February", received.Index);
        Assert.Equal("mobile", received.SeriesKey);
        Assert.Equal(1, received.SeriesIndex);
        Assert.Equal(200, received.Value);
        Assert.Same(Sales[1], received.Item);
    }

    [Fact]
    public async Task A_pick_that_names_no_series_on_a_chart_with_several_leaves_the_choice_to_the_handler()
    {
        KlyChartSelectEventArgs<Sale>? received = null;
        var data = new List<Sale> { new("a", null, 7), new("b", 3, 4) };
        var cut = Chart(p => p.Add(c => c.OnSelect, e => received = e), data);

        await cut.Find("kly-chart").TriggerEventAsync("onklypick", new KlyPickEventArgs { Row = 0, Series = -1 });

        Assert.Equal(0, received!.Row);
        Assert.Equal(-1, received.SeriesIndex);
        Assert.Null(received.SeriesKey);
        Assert.Null(received.Value);
        Assert.Same(data[0], received.Item);
    }

    [Fact]
    public async Task A_pick_on_a_chart_with_one_series_always_names_it()
    {
        KlyChartSelectEventArgs<Sale>? received = null;
        var cut = Chart(p => p.Add(c => c.OnSelect, e => received = e), categories: [new("desktop", s => s.Desktop)]);

        await cut.Find("kly-chart").TriggerEventAsync("onklypick", new KlyPickEventArgs { Row = 1, Series = -1 });

        Assert.Equal("desktop", received!.SeriesKey);
        Assert.Equal(305, received.Value);
    }

    [Fact]
    public async Task A_donut_slice_pick_reports_the_slice_and_its_size()
    {
        KlyChartSelectEventArgs<Sale>? received = null;
        var cut = Chart(p => p.Add(c => c.Type, KlyChartType.Donut).Add(c => c.OnSelect, e => received = e));

        await cut.Find("kly-chart").TriggerEventAsync("onklypick", new KlyPickEventArgs { Row = 2, Series = 0 });

        Assert.Equal("March", received!.Index);
        Assert.Equal("desktop", received.SeriesKey);
        Assert.Equal(237, received.Value);
    }

    [Fact]
    public async Task A_pick_of_a_row_that_does_not_exist_is_ignored()
    {
        var calls = 0;
        var cut = Chart(p => p.Add(c => c.OnSelect, _ => calls++));

        await cut.Find("kly-chart").TriggerEventAsync("onklypick", new KlyPickEventArgs { Row = 99, Series = 0 });

        Assert.Equal(0, calls);
    }

    [Fact]
    public void Markers_carry_their_series_so_the_browser_can_tell_which_point_was_picked()
    {
        var cut = Chart(p => p.Add(c => c.OnSelect, _ => { }));

        Assert.Equal(["0", "1"], cut.FindAll("[data-hit]")[0].QuerySelectorAll("path[data-series]").Select(m => m.GetAttribute("data-series")!).ToArray());
    }
}
