using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Tests;

/// <summary>
/// The read-only blocks that came from KlyDisplay: KlyHeading, KlyText, KlyStat, KlyStatGroup, KlyFacts, KlyBreakdown, KlyDiff, KlyRecordList,
/// KlyList, KlyDateTile, KlyMeter, KlyImageGallery and the features the existing components gained (alert details, table footer, user contacts...).
/// </summary>
public class BlocksTests : KlyrooTestContext
{
    private static RenderFragment Icon(string name) => builder =>
    {
        builder.OpenComponent<KlyIcon>(0);
        builder.AddComponentParameter(1, nameof(KlyIcon.Name), name);
        builder.CloseComponent();
    };

    private static void WithCulture(string name, Action action)
    {
        var (previous, previousUi) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        try
        {
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // ---- KlyHeading

    [Theory]
    [InlineData(2, "H2")]
    [InlineData(3, "H3")]
    [InlineData(0, "H1")]
    [InlineData(9, "H6")]
    public void Heading_level_selects_the_heading_element(int level, string tag)
    {
        var cut = Render<KlyHeading>(p => p.Add(x => x.Title, "Billing").Add(x => x.HeadingLevel, level));

        var title = cut.Find("[data-slot=title]");
        Assert.Equal(tag, title.TagName);
        Assert.Equal("Billing", title.TextContent);
    }

    [Fact]
    public void Heading_defaults_to_level_three_and_shows_count_description_icon_and_action()
    {
        var cut = Render<KlyHeading>(p => p
            .Add(x => x.Title, "Active Subscriptions")
            .Add(x => x.Count, 42)
            .Add(x => x.Description, "Updated 5m ago")
            .Add(x => x.Icon, Icon("zap"))
            .Add(x => x.Action, new KlyActionItem("Export") { Href = "/export" }));

        Assert.Equal("H3", cut.Find("[data-slot=title]").TagName);
        Assert.Contains("Active Subscriptions", cut.Find("[data-slot=title]").TextContent);
        Assert.Equal("42", cut.Find("[data-slot=heading-count]").TextContent);
        Assert.Equal("Updated 5m ago", cut.Find("[data-slot=heading-description]").TextContent);
        Assert.NotNull(cut.Find("[data-slot=heading] svg"));
        Assert.Equal("/export", cut.Find("a[data-slot=action]").GetAttribute("href"));
    }

    [Fact]
    public void Heading_has_no_eyebrow_variant_and_always_renders_a_heading_element()
    {
        Assert.DoesNotContain("Eyebrow", Enum.GetNames<KlySurface>());
        Assert.NotNull(Render<KlyHeading>(p => p.Add(x => x.Title, "A")).Find("h3"));
    }

    [Fact]
    public void Action_target_reaches_the_link()
    {
        var cut = Render<KlyHeading>(p => p
            .Add(x => x.Title, "Docs")
            .Add(x => x.Action, new KlyActionItem("Open") { Href = "https://example.com", Target = KlyLinkTarget.Blank }));

        Assert.Equal("_blank", cut.Find("a[data-slot=action]").GetAttribute("target"));
    }

    [Fact]
    public async Task Action_click_runs_the_callback()
    {
        var clicks = 0;
        var cut = Render<KlyHeading>(p => p
            .Add(x => x.Title, "Docs")
            .Add(x => x.Action, new KlyActionItem("Go") { OnClick = EventCallback.Factory.Create(this, () => clicks++) }));

        await cut.Find("button[data-slot=action]").ClickAsync();

        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Every_block_shows_a_skeleton_instead_of_its_content_while_loading()
    {
        Assert.NotNull(Render<KlyHeading>(p => p.Add(x => x.Title, "A").Add(x => x.Loading, true)).Find("[data-slot=skeleton][aria-busy=true]"));
        Assert.NotNull(Render<KlyStat<int>>(p => p.Add(x => x.Value, 1).Add(x => x.Loading, true)).Find("[data-slot=skeleton]"));
        Assert.NotNull(Render<KlyFacts>(p => p.Add(x => x.Loading, true)).Find("[data-slot=skeleton]"));
        Assert.NotNull(Render<KlyMeter>(p => p.Add(x => x.Loading, true)).Find("[data-slot=skeleton]"));
        Assert.NotNull(Render<KlyText>(p => p.Add(x => x.Value, "t").Add(x => x.Loading, true)).Find("[data-slot=skeleton]"));
        Assert.Empty(Render<KlyStat<int>>(p => p.Add(x => x.Value, 1).Add(x => x.Loading, true)).FindAll("[data-slot=stat-value]"));
    }

    [Fact]
    public void Surface_picks_the_look_of_the_block_and_each_block_has_its_own_default()
    {
        Assert.Contains("bg-(--klyroo-bg-muted)", Render<KlyStat<int>>(p => p.Add(x => x.Value, 42)).Find("[data-slot=stat]").ClassList);
        Assert.Contains("border", Render<KlyFacts>().Find("[data-slot=facts]").ClassList);
        Assert.DoesNotContain("border", Render<KlyText>(p => p.Add(x => x.Value, "t")).Find("[data-slot=text]").ClassList);
        Assert.Contains("shadow-sm", Render<KlyText>(p => p.Add(x => x.Value, "t").Add(x => x.Surface, KlySurface.Elevated)).Find("[data-slot=text]").ClassList);
        Assert.DoesNotContain("bg-(--klyroo-bg-muted)", Render<KlyStat<int>>(p => p.Add(x => x.Value, 42).Add(x => x.Surface, KlySurface.Plain)).Find("[data-slot=stat]").ClassList);
    }

    // ---- KlyText

    [Fact]
    public void Text_quote_is_a_blockquote_with_its_author_and_meta_in_the_footer()
    {
        var cut = Render<KlyText>(p => p
            .Add(x => x.Variant, KlyTextVariant.Quote)
            .Add(x => x.Value, "Simplicity is prerequisite for reliability.")
            .Add(x => x.Author, "Dijkstra")
            .Add(x => x.Meta, "1975"));

        Assert.Contains("Simplicity is prerequisite for reliability.", cut.Find("blockquote").TextContent);
        Assert.Equal("Dijkstra · 1975", cut.Find("footer").TextContent);
        Assert.Contains("border-s-2", cut.Find("blockquote").ClassList);
    }

    [Fact]
    public void Text_paragraphs_replace_the_value_and_a_title_comes_first()
    {
        var cut = Render<KlyText>(p => p
            .Add(x => x.Title, "Summary")
            .Add(x => x.Value, "ignored")
            .Add(x => x.Paragraphs, ["First", "Second"]));

        Assert.Equal("Summary", cut.Find("[data-slot=text-title]").TextContent);
        Assert.Equal(["Summary", "First", "Second"], cut.FindAll("p").Select(p => p.TextContent));
    }

    [Fact]
    public void MaxLines_sets_the_number_of_clamped_lines_and_the_toggle_expands_the_text()
    {
        var cut = Render<KlyText>(p => p.Add(x => x.Value, "A long text").Add(x => x.MaxLines, 4));

        Assert.Equal("-webkit-line-clamp:4", cut.Find("[data-slot=text-body]").GetAttribute("style"));
        Assert.Equal("Read more", cut.Find("[data-slot=text-toggle]").TextContent.Trim());
        Assert.Equal("false", cut.Find("[data-slot=text-toggle]").GetAttribute("aria-expanded"));

        cut.Find("[data-slot=text-toggle]").Click();

        Assert.Null(cut.Find("[data-slot=text-body]").GetAttribute("style"));
        Assert.Equal("Show less", cut.Find("[data-slot=text-toggle]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-slot=text-toggle]").GetAttribute("aria-expanded"));
    }

    // ---- KlyStat and KlyStatGroup

    [Theory]
    [InlineData(KlyDeltaDirection.Up, "success")]
    [InlineData(KlyDeltaDirection.Down, "error")]
    [InlineData(KlyDeltaDirection.Flat, "neutral")]
    public void The_delta_direction_picks_the_color_of_the_badge(KlyDeltaDirection direction, string color)
    {
        var cut = Render<KlyStat<int>>(p => p.Add(x => x.Value, 42).Add(x => x.Delta, 1).Add(x => x.DeltaDirection, direction));

        Assert.Equal(color, cut.Find("[data-slot=stat-delta]").GetAttribute("data-color"));
    }

    [Theory]
    [InlineData(12.4, "success", "+12.4")]
    [InlineData(-3.0, "error", "-3")]
    [InlineData(0.0, "neutral", "0")]
    public void Without_a_direction_the_sign_of_the_delta_picks_the_color_and_the_format_shows_it(double delta, string color, string text)
    {
        var cut = Render<KlyStat<double>>(p => p.Add(x => x.Value, 10).Add(x => x.Delta, delta).Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));

        Assert.Equal(color, cut.Find("[data-slot=stat-delta]").GetAttribute("data-color"));
        Assert.Equal(text, cut.Find("[data-slot=stat-delta]").TextContent.Trim());
    }

    [Fact]
    public void An_explicit_direction_wins_over_the_sign_for_a_change_where_a_drop_is_good_news()
    {
        var cut = Render<KlyStat<decimal>>(p => p.Add(x => x.Value, 3.2m).Add(x => x.Delta, -0.5m).Add(x => x.DeltaDirection, KlyDeltaDirection.Up));

        Assert.Equal("success", cut.Find("[data-slot=stat-delta]").GetAttribute("data-color"));
        Assert.Contains("-0.5", cut.Find("[data-slot=stat-delta]").TextContent);
    }

    [Fact]
    public void The_value_and_the_delta_follow_the_format_and_the_culture()
    {
        var fr = CultureInfo.GetCultureInfo("fr-FR");
        var cut = Render<KlyStat<decimal>>(p => p.Add(x => x.Value, 12400.5m).Add(x => x.Culture, fr).Add(x => x.Format, "N1").Add(x => x.Delta, 0.125m).Add(x => x.DeltaFormat, "+0.0%;-0.0%").Add(x => x.DeltaUnit, " vs N-1"));

        Assert.Equal("12 400,5", cut.Find("[data-slot=stat-value]").TextContent.Replace('\u202f', ' ').Replace('\u00a0', ' '));
        Assert.Equal("+12,5% vs N-1", cut.Find("[data-slot=stat-delta]").TextContent.Replace('\u202f', ' ').Replace('\u00a0', ' ').Trim());
    }

    [Fact]
    public void A_value_that_is_not_finite_shows_a_dash_and_a_missing_or_not_finite_delta_shows_no_badge()
    {
        var cut = Render<KlyStat<double>>(p => p.Add(x => x.Value, double.NaN).Add(x => x.Delta, double.PositiveInfinity));

        Assert.Equal("—", cut.Find("[data-slot=stat-value]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=stat-delta]"));
        Assert.Empty(Render<KlyStat<int?>>(p => p.Add(x => x.Value, 1)).FindAll("[data-slot=stat-delta]"));
    }

    [Fact]
    public void Type_arguments_are_inferred_from_the_values_in_markup()
    {
        var cut = Render<StatInference>();

        Assert.Equal(["12,400", "7", "98.6"], cut.FindAll("[data-slot=stat-value]").Select(v => v.TextContent));
        Assert.Equal(["12", "340"], cut.FindAll("[data-slot=stat-item-value]").Select(v => v.TextContent));
        Assert.Equal("+14%", cut.Find("[data-slot=stat-delta]").TextContent.Trim());
    }

    [Fact]
    public void Stat_shows_value_unit_delta_sparkline_and_caption()
    {
        var cut = Render<KlyStat<decimal>>(p => p
            .Add(x => x.Title, "MRR")
            .Add(x => x.Value, 12400m)
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US"))
            .Add(x => x.Format, "N0")
            .Add(x => x.Unit, "EUR")
            .Add(x => x.Delta, 14m)
            .Add(x => x.DeltaUnit, "%")
            .Add(x => x.Trend, [10, 14, 13, 19])
            .Add(x => x.Caption, "vs last month"));

        Assert.Equal("MRR", cut.Find("[data-slot=stat-label]").TextContent);
        Assert.Equal("12,400", cut.Find("[data-slot=stat-value]").TextContent);
        Assert.Equal("EUR", cut.Find("[data-slot=stat-unit]").TextContent);
        Assert.Equal("+14%", cut.Find("[data-slot=stat-delta]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-slot=stat-sparkline]").GetAttribute("aria-hidden"));
        Assert.Equal(4, cut.Find("[data-slot=stat-sparkline] polyline").GetAttribute("points")!.Split(' ').Length);
        Assert.Equal("vs last month", cut.Find("[data-slot=stat-caption]").TextContent);
    }

    [Fact]
    public void Stat_without_a_value_shows_a_dash_and_a_single_point_draws_no_sparkline()
    {
        var cut = Render<KlyStat<int?>>(p => p.Add(x => x.Trend, [1.0]));

        Assert.Equal("—", cut.Find("[data-slot=stat-value]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=stat-sparkline]"));
    }

    [Fact]
    public void Stat_group_shows_its_title_and_item_colors()
    {
        var cut = Render<KlyStatGroup<int>>(p => p
            .Add(x => x.Title, "This week")
            .Add(x => x.Items, [new KlyStatItem<int>("Errors", 12) { Color = KlyColor.Error, Caption = "up" }, new KlyStatItem<int>("Users", 340)]));

        Assert.Equal("This week", cut.Find("[data-slot=stat-group-title]").TextContent);
        var values = cut.FindAll("[data-slot=stat-item-value]");
        Assert.Equal("error", values[0].GetAttribute("data-color"));
        Assert.Null(values[1].GetAttribute("data-color"));
        Assert.Contains("data-[color]:text-(--kly-color-text)", values[0].ClassList);
        Assert.Equal("up", cut.Find("[data-slot=stat-item] span:last-child").TextContent);
        Assert.Equal(["12", "340"], values.Select(v => v.TextContent));
    }

    [Fact]
    public void Stat_group_values_follow_the_format_and_the_culture_and_a_missing_one_is_a_dash()
    {
        var cut = Render<KlyStatGroup<decimal?>>(p => p
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("de-DE"))
            .Add(x => x.Format, "N2")
            .Add(x => x.Items, [new KlyStatItem<decimal?>("Revenue", 1234.5m), new KlyStatItem<decimal?>("Refunds", null)]));

        Assert.Equal(["1.234,50", "—"], cut.FindAll("[data-slot=stat-item-value]").Select(v => v.TextContent));
    }

    // ---- KlyFacts

    [Fact]
    public void Facts_render_rows_copy_buttons_and_dashes()
    {
        var cut = Render<KlyFacts>(p => p.Add(x => x.Items, [
            new KlyFact("Environment") { Value = "Production", Copy = true },
            new KlyFact("Region") { Value = null },
        ]));

        Assert.Equal(2, cut.FindAll("[data-slot=fact-item]").Count);
        Assert.Equal("Copy Environment", cut.Find("[data-slot=fact-copy]").GetAttribute("aria-label"));
        Assert.NotNull(cut.Find("[data-slot=fact-empty]"));
    }

    [Fact]
    public void Facts_badge_icon_hint_and_color_are_rendered()
    {
        var cut = Render<KlyFacts>(p => p.Add(x => x.Items, [
            new KlyFact("Status") { Badge = new KlyBadgeItem("Paid") { Color = KlyColor.Success, Icon = "check" } },
            new KlyFact("Balance") { Value = "-€40", Hint = "Due on the 5th", Color = KlyColor.Error, Icon = "clock", Mono = true, Emphasis = true },
        ]));

        Assert.NotNull(cut.Find("[data-slot=badge] svg"));
        Assert.Equal("Due on the 5th", cut.Find("[data-slot=fact-hint]").TextContent);
        var value = cut.FindAll("[data-slot=fact-value]")[1];
        Assert.Equal("error", value.GetAttribute("data-color"));
        Assert.Contains("data-[color]:text-(--kly-color-text)", value.ClassList);
        Assert.Contains("font-mono", value.ClassList);
        Assert.Contains("font-semibold", value.ClassList);
        Assert.NotNull(cut.FindAll("[data-slot=fact-label]")[1].QuerySelector("svg"));
    }

    [Fact]
    public void A_fact_can_be_a_link_copy_a_different_text_or_load()
    {
        var cut = Render<KlyFacts>(p => p.Add(x => x.Items, [
            new KlyFact("Site") { Value = "klyroo.dev", Href = "https://klyroo.dev" },
            new KlyFact("Key") { Value = "••••", CopyText = "secret-value" },
            new KlyFact("Plan") { Loading = true, Value = "hidden" },
        ]));

        Assert.Equal("https://klyroo.dev", cut.Find("[data-slot=fact-value] a").GetAttribute("href"));
        Assert.Single(cut.FindAll("[data-slot=fact-copy]"));
        Assert.Single(cut.FindAll("[data-slot=fact-value] div[aria-hidden=true]"));
        Assert.DoesNotContain("hidden", cut.FindAll("[data-slot=fact-value]")[2].TextContent);
    }

    [Fact]
    public void Facts_groups_replace_the_flat_rows_and_show_their_title_and_aside()
    {
        var cut = Render<KlyFacts>(p => p
            .Add(x => x.Items, [new KlyFact("Ignored") { Value = "x" }])
            .Add(x => x.Groups, [
                new KlyFactGroup("Account", [new KlyFact("Owner") { Value = "Ada" }]) { Aside = "2 items" },
                new KlyFactGroup(null, [new KlyFact("Plan") { Value = "Pro" }]),
            ]));

        Assert.Equal(2, cut.FindAll("[data-slot=fact-group]").Count);
        Assert.Contains("Account", cut.Find("[data-slot=fact-group]").TextContent);
        Assert.Contains("2 items", cut.Find("[data-slot=fact-group]").TextContent);
        Assert.DoesNotContain("Ignored", cut.Markup);
    }

    [Fact]
    public void Facts_without_rows_show_a_dash_and_Max_and_Step_reveal_the_others()
    {
        Assert.NotNull(Render<KlyFacts>().Find("[data-slot=facts-empty]"));

        var cut = Render<KlyFacts>(p => p
            .Add(x => x.Max, 1)
            .Add(x => x.Step, 1)
            .Add(x => x.Items, [new KlyFact("A") { Value = "1" }, new KlyFact("B") { Value = "2" }, new KlyFact("C") { Value = "3" }]));

        Assert.Single(cut.FindAll("[data-slot=fact-item]"));
        Assert.Equal("Show more · 1", cut.Find("[data-slot=more]").TextContent.Trim());
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(2, cut.FindAll("[data-slot=fact-item]").Count);
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(3, cut.FindAll("[data-slot=fact-item]").Count);
        Assert.Empty(cut.FindAll("[data-slot=more]"));
    }

    [Fact]
    public void Copy_buttons_are_disabled_in_a_static_render()
    {
        using var context = new BlocksStaticContext();
        var cut = context.Render<KlyFacts>(p => p.Add(x => x.Items, [new KlyFact("A") { Value = "1", Copy = true }]));

        Assert.True(cut.Find("[data-slot=fact-copy]").HasAttribute("disabled"));
    }

    private sealed class BlocksStaticContext : KlyrooTestContext
    {
        public BlocksStaticContext() : base(interactive: false)
        {
        }
    }

    // ---- KlyBreakdown

    [Fact]
    public void Breakdown_compare_columns_line_up_both_amounts_with_detail_color_and_badge()
    {
        var cut = Render<KlyBreakdown>(p => p
            .Add(x => x.Format, "N0")
            .Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US"))
            .Add(x => x.CompareColumns, new KlyBreakdownColumns("2026", "2025"))
            .Add(x => x.Lines, [
                new KlyBreakdownLine("Licenses") { Amount = 900, CompareAmount = 800, Detail = "12 seats", Badge = new KlyBadgeItem("New") },
                new KlyBreakdownLine("Discount") { Amount = -100, Color = KlyColor.Success },
            ])
            .Add(x => x.Total, new KlyBreakdownLine("Total") { Amount = 800, CompareAmount = 800 }));

        var header = cut.Find("[data-slot=breakdown-columns]");
        Assert.Contains("2026", header.TextContent);
        Assert.Contains("2025", header.TextContent);
        var lines = cut.FindAll("[data-slot=breakdown-line]");
        Assert.Equal("800", lines[0].QuerySelector("[data-slot=breakdown-compare]")!.TextContent);
        Assert.Equal("900", lines[0].QuerySelector("[data-slot=breakdown-value]")!.TextContent);
        Assert.Equal("—", lines[1].QuerySelector("[data-slot=breakdown-compare]")!.TextContent);
        Assert.Equal("-100", lines[1].QuerySelector("[data-slot=breakdown-value]")!.TextContent);
        Assert.Equal("12 seats", lines[0].QuerySelector("[data-slot=breakdown-detail]")!.TextContent);
        Assert.Contains("New", lines[0].QuerySelector("[data-slot=badge]")!.TextContent);
        Assert.Equal("success", lines[1].QuerySelector("[data-slot=breakdown-value]")!.GetAttribute("data-color"));
        Assert.NotNull(cut.Find("[data-slot=breakdown-total] [data-slot=breakdown-compare]"));

        // A second amount alone also switches to two columns, without headers.
        var plain = Render<KlyBreakdown>(p => p.Add(x => x.Lines, [new KlyBreakdownLine("Fees") { Amount = 10, CompareAmount = 12 }]));
        Assert.Equal("true", plain.Find("[data-slot=breakdown]").GetAttribute("data-compare"));
        Assert.Empty(plain.FindAll("[data-slot=breakdown-columns]"));
    }

    [Fact]
    public void Breakdown_amounts_follow_the_format_and_the_culture()
    {
        KlyBreakdownLine[] lines = [new KlyBreakdownLine("Licenses") { Amount = 1234.5m }, new KlyBreakdownLine("Free"), new KlyBreakdownLine("Later") { IsMuted = true }];

        var english = Render<KlyBreakdown>(p => p.Add(x => x.Lines, lines).Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));
        var french = Render<KlyBreakdown>(p => p.Add(x => x.Lines, lines).Add(x => x.Culture, CultureInfo.GetCultureInfo("fr-FR")).Add(x => x.Format, "C2"));

        Assert.Equal("1,234.50", english.Find("[data-slot=breakdown-value]").TextContent);
        Assert.Equal("—", english.FindAll("[data-slot=breakdown-value]")[1].TextContent);
        Assert.Equal("1 234,50 €", french.Find("[data-slot=breakdown-value]").TextContent.Replace(' ', ' ').Replace(' ', ' '));
        Assert.Contains("text-(--klyroo-text-dimmed)", english.FindAll("[data-slot=breakdown-line]")[2].ClassList);
    }

    [Fact]
    public void Breakdown_subtotals_and_the_total_are_emphasized_and_the_footnote_closes_the_block()
    {
        var cut = Render<KlyBreakdown>(p => p
            .Add(x => x.Title, "Invoice")
            .Add(x => x.Footnote, "VAT included")
            .Add(x => x.Lines, [new KlyBreakdownLine("Items") { Amount = 10, IsSubtotal = true }])
            .Add(x => x.Total, new KlyBreakdownLine("Total") { Amount = 12 }));

        Assert.Equal("Invoice", cut.Find("[data-slot=breakdown-title]").TextContent);
        Assert.Contains("border-t", cut.Find("[data-slot=breakdown-line]").ClassList);
        Assert.Contains("font-semibold", cut.Find("[data-slot=breakdown-total]").ClassList);
        Assert.Equal("VAT included", cut.Find("[data-slot=breakdown-footnote]").TextContent);
    }

    [Fact]
    public void A_collapsible_breakdown_hides_its_lines_behind_a_toggle_and_keeps_the_total()
    {
        var cut = Render<KlyBreakdown>(p => p
            .Add(x => x.Collapsible, true)
            .Add(x => x.Lines, [new KlyBreakdownLine("Items") { Amount = 10 }])
            .Add(x => x.Total, new KlyBreakdownLine("Total") { Amount = 10 }));

        Assert.Empty(cut.FindAll("[data-slot=breakdown-line]"));
        Assert.NotNull(cut.Find("[data-slot=breakdown-total]"));
        Assert.Equal("Details", cut.Find("[data-slot=breakdown-toggle]").TextContent.Trim());
        Assert.Equal("false", cut.Find("[data-slot=breakdown-toggle]").GetAttribute("aria-expanded"));

        cut.Find("[data-slot=breakdown-toggle]").Click();

        Assert.Single(cut.FindAll("[data-slot=breakdown-line]"));
        Assert.Equal("Hide details", cut.Find("[data-slot=breakdown-toggle]").TextContent.Trim());

        var open = Render<KlyBreakdown>(p => p.Add(x => x.Collapsible, true).Add(x => x.DefaultOpen, true).Add(x => x.Lines, [new KlyBreakdownLine("Items") { Amount = 10 }]));
        Assert.Single(open.FindAll("[data-slot=breakdown-line]"));
    }

    // ---- KlyDiff

    [Fact]
    public void Diff_shows_changed_fields_with_the_old_value_deleted_and_the_new_one_inserted()
    {
        var cut = Render<KlyDiff>(p => p.Add(x => x.Items, [
            new KlyDiffItem("Plan", "Starter", "Enterprise"),
            new KlyDiffItem("Currency", "EUR", "EUR") { Changed = false },
        ]));

        Assert.Single(cut.FindAll("[data-slot=diff-item]"));
        Assert.Equal("Starter", cut.Find("del[data-slot=diff-before]").TextContent);
        Assert.Equal("Enterprise", cut.Find("ins[data-slot=diff-after]").TextContent);
    }

    [Fact]
    public void Diff_ShowUnchanged_lists_the_unchanged_fields_once_and_a_note_sits_beside_the_label()
    {
        var cut = Render<KlyDiff>(p => p
            .Add(x => x.ShowUnchanged, true)
            .Add(x => x.Items, [
                new KlyDiffItem("Plan", "Pro", "Team") { Note = new KlyBadgeItem("Needs approval") },
                new KlyDiffItem("Currency", "EUR", "EUR") { Changed = false },
            ]));

        Assert.Equal(2, cut.FindAll("[data-slot=diff-item]").Count);
        Assert.Contains("Needs approval", cut.Find("[data-slot=diff-item] [data-slot=badge]").TextContent);
        Assert.Equal("EUR", cut.Find("[data-slot=diff-unchanged]").TextContent);
        Assert.Equal("false", cut.FindAll("[data-slot=diff-item]")[1].GetAttribute("data-changed"));
    }

    [Fact]
    public void Diff_Compare_builds_an_item_from_two_typed_values()
    {
        var same = KlyDiffItem.Compare("Seats", 12, 12);
        var changed = KlyDiffItem.Compare("Due", new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 1), "yyyy-MM-dd");
        var added = KlyDiffItem.Compare<string>("Note", null, "New");

        Assert.False(same.Changed);
        Assert.True(changed.Changed);
        Assert.Equal("2026-01-01", changed.Before);
        Assert.Equal("2026-02-01", changed.After);
        Assert.Null(added.Before);
        Assert.Equal("New", added.After);
    }

    [Fact]
    public void Diff_formats_typed_values_when_it_renders_with_the_format_and_the_culture_of_the_block()
    {
        KlyDiffItem[] items =
        [
            KlyDiffItem.Compare("Price", 1200.5m, 1500m, "N2"),
            KlyDiffItem.Compare("Due", new DateOnly(2026, 3, 5), new DateOnly(2026, 4, 1), "d"),
            KlyDiffItem.Compare("Owner", "Ada", "Grace", (name, culture) => name.ToUpper(culture)),
        ];

        var english = Render<KlyDiff>(p => p.Add(x => x.Items, items).Add(x => x.Culture, CultureInfo.GetCultureInfo("en-US")));
        var german = Render<KlyDiff>(p => p.Add(x => x.Items, items).Add(x => x.Culture, CultureInfo.GetCultureInfo("de-DE")));

        Assert.Equal(["1,200.50", "3/5/2026", "ADA"], english.FindAll("[data-slot=diff-before]").Select(v => v.TextContent));
        Assert.Equal(["1.500,00", "01.04.2026", "GRACE"], german.FindAll("[data-slot=diff-after]").Select(v => v.TextContent));
    }

    // ---- KlyRecordList

    [Fact]
    public async Task Record_rows_render_image_meta_icon_badges_caption_color_and_click()
    {
        var clicks = 0;
        var cut = Render<KlyRecordList>(p => p.Add(x => x.Items, [
            new KlyRecordItem("Order 1042")
            {
                Description = "Shipped",
                DescriptionIcon = "clock",
                ImageSrc = "/img/box.png",
                Icon = "inbox",
                Color = KlyColor.Success,
                Badges = [new KlyBadgeItem("Gift"), new KlyBadgeItem("Express")],
                Trailing = "€120",
                TrailingCaption = "Nov 20",
                OnClick = EventCallback.Factory.Create(this, () => clicks++),
            },
        ]));

        Assert.Equal("/img/box.png", cut.Find("img[data-slot=record-image]").GetAttribute("src"));
        Assert.Empty(cut.FindAll("[data-slot=record-avatar]"));
        var meta = cut.Find("[data-slot=record-meta]");
        Assert.Equal("Shipped", meta.TextContent.Trim());
        Assert.NotNull(meta.QuerySelector("svg"));
        Assert.Equal(2, cut.FindAll("[data-slot=badge]").Count);
        Assert.Equal("Nov 20", cut.Find("[data-slot=record-trailing-caption]").TextContent);
        Assert.Equal("success", cut.Find("[data-slot=record-item]").GetAttribute("data-color"));
        Assert.Equal("success", cut.Find("[data-slot=record-trailing]").GetAttribute("data-color"));

        var title = cut.Find("[data-slot=record-title]");
        Assert.Equal("BUTTON", title.TagName);
        await title.ClickAsync();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Record_rows_render_the_reference_title_link_avatar_and_a_single_badge()
    {
        var cut = Render<KlyRecordList>(p => p
            .Add(x => x.Title, "Invoices")
            .Add(x => x.Count, 1200)
            .Add(x => x.Items, [
                new KlyRecordItem("Acme Annual Plan") { Reference = "INV-42", Href = "/invoices/42", AvatarText = "AC", Trailing = "€12,000", Badge = new KlyBadgeItem("Paid") },
                new KlyRecordItem("Plain row"),
            ]));

        Assert.Equal("Invoices", cut.Find("[data-slot=record-list-title]").TextContent);
        Assert.Equal("1,200", cut.Find("[data-slot=record-list-count]").TextContent);
        Assert.Equal("INV-42", cut.Find("[data-slot=record-reference]").TextContent);
        Assert.Equal("/invoices/42", cut.Find("a[data-slot=record-title]").GetAttribute("href"));
        Assert.Equal("AC", cut.Find("[data-slot=record-avatar]").TextContent.Trim());
        Assert.Equal("SPAN", cut.FindAll("[data-slot=record-title]")[1].TagName);
        Assert.Single(cut.FindAll("[data-slot=badge]"));
    }

    [Fact]
    public void Record_Max_and_Step_reveal_more_rows()
    {
        var cut = Render<KlyRecordList>(p => p
            .Add(x => x.Max, 1)
            .Add(x => x.Items, [new KlyRecordItem("A"), new KlyRecordItem("B")]));

        Assert.Single(cut.FindAll("[data-slot=record-item]"));
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(2, cut.FindAll("[data-slot=record-item]").Count);
    }

    // ---- KlyList

    [Fact]
    public void List_items_show_their_icon_and_color()
    {
        var cut = Render<KlyList>(p => p.Add(x => x.Items, [
            new KlyListItem("Backups") { Icon = "check", Color = KlyColor.Success },
            new KlyListItem("Alerts") { Color = KlyColor.Warning },
        ]));

        var icon = cut.Find("[data-slot=list-icon]");
        Assert.Equal("success", icon.GetAttribute("data-color"));
        Assert.NotNull(icon.QuerySelector("svg"));
        Assert.Equal("warning", cut.Find("[data-slot=list-bullet]").GetAttribute("data-color"));
    }

    [Fact]
    public void List_Max_and_Step_reveal_more_items()
    {
        var cut = Render<KlyList>(p => p
            .Add(x => x.Max, 2)
            .Add(x => x.Step, 2)
            .Add(x => x.Items, [new KlyListItem("Step 1"), new KlyListItem("Step 2"), new KlyListItem("Step 3")]));

        Assert.Equal(2, cut.FindAll("[data-slot=list-item]").Count);
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(3, cut.FindAll("[data-slot=list-item]").Count);
    }

    [Fact]
    public void Steps_are_a_numbered_list_that_marks_done_and_current_items_for_assistive_technology()
    {
        var cut = Render<KlyList>(p => p
            .Add(x => x.Variant, KlyListVariant.Steps)
            .Add(x => x.Items, [
                new KlyListItem("Account") { Done = true },
                new KlyListItem("Billing") { Current = true, Description = "Add a card", Meta = "2 min" },
                new KlyListItem("Launch") { Href = "/launch" },
            ]));

        Assert.NotNull(cut.Find("ol"));
        var items = cut.FindAll("[data-slot=list-item]");
        Assert.Equal(["done", "current", "todo"], items.Select(i => i.GetAttribute("data-state")));
        Assert.Equal([null, "step", null], items.Select(i => i.GetAttribute("aria-current")));
        Assert.NotNull(items[0].QuerySelector("svg"));
        Assert.Equal(KlyLocales.En.Messages.StepCompleted, items[0].QuerySelector(".sr-only")!.TextContent);
        Assert.Equal("2", items[1].QuerySelector("[data-slot=list-marker]")!.TextContent.Trim());
        Assert.Contains("Add a card", items[1].TextContent);
        Assert.Contains("2 min", items[1].TextContent);
        Assert.Equal("/launch", items[2].QuerySelector("a")!.GetAttribute("href"));
    }

    [Fact]
    public void Checks_number_the_items_in_a_bulleted_list_and_the_plain_list_has_no_state()
    {
        var checks = Render<KlyList>(p => p.Add(x => x.Variant, KlyListVariant.Checks).Add(x => x.Items, [new KlyListItem("A") { Done = true }]));
        var plain = Render<KlyList>(p => p.Add(x => x.Items, [new KlyListItem("A") { Done = true }]));

        Assert.NotNull(checks.Find("ul"));
        Assert.Equal("done", checks.Find("[data-slot=list-item]").GetAttribute("data-state"));
        Assert.Null(plain.Find("[data-slot=list-item]").GetAttribute("data-state"));
        Assert.Empty(plain.FindAll("[data-slot=list-marker]"));
    }

    // ---- KlyMeter

    [Fact]
    public void Meter_follows_the_warning_and_error_thresholds()
    {
        Assert.Equal("error", Render<KlyMeter>(p => p.Add(x => x.Value, 92).Add(x => x.ErrorThreshold, 90)).Find("[data-slot=meter]").GetAttribute("data-tone"));
        Assert.Equal("warning", Render<KlyMeter>(p => p.Add(x => x.Value, 80).Add(x => x.WarningThreshold, 75).Add(x => x.ErrorThreshold, 90)).Find("[data-slot=meter]").GetAttribute("data-tone"));
        Assert.Equal("primary", Render<KlyMeter>(p => p.Add(x => x.Value, 10).Add(x => x.WarningThreshold, 75)).Find("[data-slot=meter]").GetAttribute("data-tone"));
    }

    [Fact]
    public void Meter_bar_is_a_named_meter_with_its_value_text_and_a_clamped_width()
    {
        var cut = Render<KlyMeter>(p => p
            .Add(x => x.Title, "Storage")
            .Add(x => x.Value, 250)
            .Add(x => x.Max, 200)
            .Add(x => x.ValueText, "200 GB")
            .Add(x => x.Description, "Almost full"));

        var track = cut.Find("[role=meter]");
        Assert.Equal("Storage", track.GetAttribute("aria-label"));
        Assert.Equal("200", track.GetAttribute("aria-valuenow"));
        Assert.Equal("0", track.GetAttribute("aria-valuemin"));
        Assert.Equal("200", track.GetAttribute("aria-valuemax"));
        Assert.Equal("200 GB", track.GetAttribute("aria-valuetext"));
        Assert.Equal("width:100%", cut.Find("[data-slot=meter-bar]").GetAttribute("style"));
        Assert.Equal("200 GB", cut.Find("[data-slot=meter-value]").TextContent);
        Assert.Equal("Almost full", cut.Find("[data-slot=meter-caption]").TextContent);
        Assert.Equal("Meter", Render<KlyMeter>(p => p.Add(x => x.Value, 5)).Find("[role=meter]").GetAttribute("aria-label"));
    }

    [Fact]
    public void Meter_parts_have_colors_a_legend_and_a_cap()
    {
        var cut = Render<KlyMeter>(p => p
            .Add(x => x.Max, 10)
            .Add(x => x.Parts, [
                new KlyMeterPart("Photos", 3) { ValueLabel = "3 GB", Color = KlyColor.Info, Icon = "image" },
                new KlyMeterPart("Video", 9) { Max = 5 },
            ]));

        var parts = cut.FindAll("[data-slot=meter-part]");
        Assert.Equal("info", parts[0].GetAttribute("data-color"));
        Assert.Equal("width:50%", parts[1].GetAttribute("style"));
        var legend = cut.FindAll("[data-slot=meter-legend-item]");
        Assert.Contains("3 GB", legend[0].TextContent);
        Assert.NotNull(legend[0].QuerySelector("svg"));
        Assert.Contains("9 / 5", legend[1].TextContent);
    }

    [Fact]
    public void Meter_parts_without_a_label_icon_or_cap_have_no_legend()
    {
        var cut = Render<KlyMeter>(p => p.Add(x => x.Parts, [new KlyMeterPart("A", 30), new KlyMeterPart("B", 20)]));

        Assert.Empty(cut.FindAll("[data-slot=meter-legend]"));
        Assert.Equal(["width:30%", "width:20%"], cut.FindAll("[data-slot=meter-part]").Select(p => p.GetAttribute("style")));
    }

    [Fact]
    public void Ring_meter_draws_the_value_and_follows_the_thresholds()
    {
        var cut = Render<KlyMeter>(p => p
            .Add(x => x.Layout, KlyMeterLayout.Ring)
            .Add(x => x.Title, "Quota")
            .Add(x => x.Value, 92)
            .Add(x => x.ErrorThreshold, 90));

        Assert.NotNull(cut.Find("[data-slot=meter-ring] [role=meter]"));
        var arc = cut.Find("[data-slot=meter-ring-value]");
        Assert.Equal("92 8", arc.GetAttribute("stroke-dasharray"));
        Assert.Equal("error", arc.GetAttribute("data-color"));
        Assert.Equal("92%", cut.Find("[data-slot=meter-value]").TextContent);
        Assert.Empty(cut.FindAll("[data-slot=meter-track]"));
    }

    [Fact]
    public void Ring_meter_parts_are_stacked_arcs_with_a_tooltip()
    {
        var cut = Render<KlyMeter>(p => p
            .Add(x => x.Layout, KlyMeterLayout.Ring)
            .Add(x => x.Parts, [new KlyMeterPart("A", 30) { ValueLabel = "30 GB" }, new KlyMeterPart("B", 50) { Color = KlyColor.Success }]));

        var arcs = cut.FindAll("[data-slot=meter-ring-part]");
        Assert.Equal(["30 70", "50 50"], arcs.Select(a => a.GetAttribute("stroke-dasharray")));
        Assert.Null(arcs[0].GetAttribute("stroke-dashoffset"));
        Assert.Equal("-30", arcs[1].GetAttribute("stroke-dashoffset"));
        Assert.Equal("A: 30 GB", arcs[0].QuerySelector("title")!.TextContent);
    }

    [Fact]
    public void Scale_meter_highlights_the_levels_up_to_the_current_one()
    {
        var cut = Render<KlyMeter>(p => p
            .Add(x => x.Layout, KlyMeterLayout.Scale)
            .Add(x => x.Levels, ["Low", "Medium", "High"])
            .Add(x => x.CurrentLevel, 1));

        var levels = cut.FindAll("[data-slot=meter-level]");
        Assert.Equal(["true", "true", "false"], levels.Select(l => l.GetAttribute("data-reached")));
        Assert.Equal([null, "true", null], levels.Select(l => l.GetAttribute("data-active")));
        Assert.Equal("grid-template-columns:repeat(3,minmax(0,1fr))", cut.Find("[data-slot=meter-scale]").GetAttribute("style"));
        Assert.Empty(cut.FindAll("[role=meter]"));
        // Without levels, a scale is a bar.
        Assert.NotNull(Render<KlyMeter>(p => p.Add(x => x.Layout, KlyMeterLayout.Scale)).Find("[data-slot=meter-track]"));
    }

    // ---- KlyDateTile

    [Theory]
    [InlineData(KlyDateTileMode.Countdown)]
    [InlineData(KlyDateTileMode.Range)]
    public void Countdown_and_range_dates_show_the_title_as_a_caption(KlyDateTileMode mode)
    {
        var cut = Render<KlyDateTile>(p => p
            .Add(x => x.Mode, mode)
            .Add(x => x.Title, "Renewal")
            .Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(x => x.EndValue, new DateTimeOffset(2026, 12, 7, 0, 0, 0, TimeSpan.Zero))
            .Add(x => x.Now, new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero)));

        Assert.Equal("Renewal", cut.Find("[data-slot=date-caption]").TextContent);
        Assert.DoesNotContain("Renewal", cut.Find("[data-slot=date-title]").TextContent);
    }

    [Theory]
    [InlineData(26, "Today")]
    [InlineData(25, "Tomorrow")]
    [InlineData(21, "In 5 days")]
    [InlineData(27, "1 day overdue")]
    [InlineData(29, "3 days overdue")]
    public void Countdown_names_the_days_until_or_since_the_date(int nowDay, string expected)
    {
        var cut = Render<KlyDateTile>(p => p
            .Add(x => x.Mode, KlyDateTileMode.Countdown)
            .Add(x => x.Value, new DateTimeOffset(2026, 11, 26, 0, 0, 0, TimeSpan.Zero))
            .Add(x => x.Now, new DateTimeOffset(2026, 11, nowDay, 12, 0, 0, TimeSpan.Zero)));

        // The date is the 26th: "Tomorrow" is seen from the 25th, "overdue" from the 27th on.
        Assert.Equal(expected, cut.Find("[data-slot=date-title]").TextContent.Trim());
    }

    [Fact]
    public void A_date_tile_shows_the_calendar_the_long_date_or_the_title_and_the_range_length()
    {
        WithCulture("en-US", () =>
        {
            var plain = Render<KlyDateTile>(p => p.Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.Equal("Dec", plain.Find("[data-slot=date-tile-calendar] span:first-child").TextContent);
            Assert.Equal("1", plain.Find("[data-slot=date-tile-calendar] span:last-child").TextContent);
            Assert.Equal("Tuesday, December 1, 2026", plain.Find("[data-slot=date-title]").TextContent.Trim());

            var titled = Render<KlyDateTile>(p => p.Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)).Add(x => x.Title, "Launch").Add(x => x.Description, "Paris"));
            Assert.Equal("Launch", titled.Find("[data-slot=date-title]").TextContent.Trim());
            Assert.Equal("Paris", titled.Find("[data-slot=date-description]").TextContent);

            var range = Render<KlyDateTile>(p => p.Add(x => x.Mode, KlyDateTileMode.Range).Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)).Add(x => x.EndValue, new DateTimeOffset(2026, 12, 7, 0, 0, 0, TimeSpan.Zero)));
            Assert.Contains("→", range.Find("[data-slot=date-title]").TextContent);
            Assert.Contains("(7 days)", range.Find("[data-slot=date-title]").TextContent);

            // A range without an end is a plain date.
            var open = Render<KlyDateTile>(p => p.Add(x => x.Mode, KlyDateTileMode.Range).Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)));
            Assert.DoesNotContain("→", open.Find("[data-slot=date-title]").TextContent);
        });
    }

    // ---- Texts come from the locale

    private IRenderedComponent<CascadingValue<KlyLocale>> InLocale<TComponent>(KlyLocale locale, Action<ComponentParameterCollectionBuilder<TComponent>> parameters) where TComponent : IComponent =>
        Render<CascadingValue<KlyLocale>>(p => p.Add(x => x.Value, locale).AddChildContent<TComponent>(parameters));

    [Fact]
    public void Block_texts_come_from_the_messages_of_the_locale()
    {
        var custom = KlyLocales.Extend(KlyLocales.En, messages: m => m with { ShowMore = n => $"Afficher {n} de plus", DayCount = n => $"{n} jours" });

        var list = InLocale<KlyList>(custom, d => d.Add(x => x.Max, 1).Add(x => x.Items, [new KlyListItem("a"), new KlyListItem("b")]));
        Assert.Equal("Afficher 1 de plus", list.Find("[data-slot=more]").TextContent.Trim());

        var range = InLocale<KlyDateTile>(custom, d => d
            .Add(x => x.Mode, KlyDateTileMode.Range)
            .Add(x => x.Value, new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero))
            .Add(x => x.EndValue, new DateTimeOffset(2026, 12, 7, 0, 0, 0, TimeSpan.Zero)));
        Assert.Contains("(7 jours)", range.Find("[data-slot=date-title]").TextContent);
    }

    [Fact]
    public void A_component_shows_the_texts_of_its_locale()
    {
        KlyListItem[] items = [new("a"), new("b")];

        Assert.Equal("Afficher plus · 1", InLocale<KlyList>(KlyLocales.Fr, d => d.Add(x => x.Max, 1).Add(x => x.Items, items)).Find("[data-slot=more]").TextContent.Trim());
        Assert.Equal("Mehr anzeigen · 1", InLocale<KlyList>(KlyLocales.De, d => d.Add(x => x.Max, 1).Add(x => x.Items, items)).Find("[data-slot=more]").TextContent.Trim());
        Assert.Equal("Show more · 1", InLocale<KlyList>(KlyLocales.En, d => d.Add(x => x.Max, 1).Add(x => x.Items, items)).Find("[data-slot=more]").TextContent.Trim());
    }

    [Fact]
    public void A_locale_derived_from_french_keeps_its_block_texts()
    {
        var canadian = KlyLocales.Extend(KlyLocales.Fr, code: "fr-CA", name: "Français (Canada)");

        Assert.Equal("Afficher plus · 1", InLocale<KlyList>(canadian, d => d.Add(x => x.Max, 1).Add(x => x.Items, [new KlyListItem("a"), new KlyListItem("b")])).Find("[data-slot=more]").TextContent.Trim());
    }

    public static TheoryData<string> LocaleCodes()
    {
        var data = new TheoryData<string>();
        foreach (var locale in KlyLocales.All) data.Add(locale.Code);
        return data;
    }

    [Theory]
    [MemberData(nameof(LocaleCodes))]
    public void Every_locale_has_every_block_text_and_the_functions_answer_for_every_count(string code)
    {
        var messages = KlyLocales.All.Single(l => l.Code == code).Messages;
        var counts = new[] { 0, 1, 2, 3, 5, 11, 21, 22, 100 };

        foreach (var n in counts)
        {
            Assert.False(string.IsNullOrWhiteSpace(messages.ShowMore(n)), $"{code} ShowMore({n})");
            Assert.False(string.IsNullOrWhiteSpace(messages.InDays(n)), $"{code} InDays({n})");
            Assert.False(string.IsNullOrWhiteSpace(messages.DaysOverdue(n)), $"{code} DaysOverdue({n})");
            Assert.False(string.IsNullOrWhiteSpace(messages.DayCount(n)), $"{code} DayCount({n})");
            Assert.Contains(n.ToString(CultureInfo.InvariantCulture), messages.ShowMore(n));
        }
        Assert.Equal(messages.Duration(0), messages.Duration(-5));
        Assert.Contains("{0}", messages.CopyValue);
        Assert.Contains("{0}", messages.ImageAlt);
    }

    [Fact]
    public void Russian_and_Arabic_keep_their_plural_forms()
    {
        Assert.Equal("Через 1 день", KlyLocales.Ru.Messages.InDays(1));
        Assert.Equal("Через 2 дня", KlyLocales.Ru.Messages.InDays(2));
        Assert.Equal("Через 5 дней", KlyLocales.Ru.Messages.InDays(5));
        Assert.Equal("Через 11 дней", KlyLocales.Ru.Messages.InDays(11));
        Assert.Equal("Через 21 день", KlyLocales.Ru.Messages.InDays(21));
        Assert.NotEqual(KlyLocales.Ar.Messages.DayCount(3), KlyLocales.Ar.Messages.DayCount(11));
    }

    // ---- KlyImageGallery

    [Fact]
    public void Gallery_shows_the_first_images_and_a_plus_N_tile_for_the_others()
    {
        var cut = Render<KlyImageGallery>(p => p
            .Add(x => x.Caption, "Trip")
            .Add(x => x.Images, [new KlyGalleryImage("/1.jpg"), new KlyGalleryImage("/2.jpg") { Alt = "Beach" }, new KlyGalleryImage("/3.jpg"), new KlyGalleryImage("/4.jpg"), new KlyGalleryImage("/5.jpg")]));

        var tiles = cut.FindAll("[data-slot=gallery-tile]");
        Assert.Equal(3, tiles.Count);
        Assert.Equal(["Image 1", "Beach", "Image 3"], tiles.Select(t => t.QuerySelector("img")!.GetAttribute("alt")));
        Assert.Equal("+2", cut.Find("[data-slot=gallery-overflow]").TextContent);
        Assert.NotNull(tiles[2].QuerySelector("[data-slot=gallery-overflow]"));
        Assert.Equal("Trip", cut.Find("[data-slot=gallery-caption]").TextContent);
        Assert.Contains("grid-cols-3", cut.Find("[data-slot=gallery-grid]").ClassList);
    }

    [Fact]
    public void Gallery_Max_and_Columns_are_clamped_and_every_image_shows_without_overflow()
    {
        var images = new[] { new KlyGalleryImage("/1.jpg"), new KlyGalleryImage("/2.jpg") };

        var all = Render<KlyImageGallery>(p => p.Add(x => x.Images, images).Add(x => x.Max, 5).Add(x => x.Columns, 9));
        Assert.Equal(2, all.FindAll("[data-slot=gallery-tile]").Count);
        Assert.Empty(all.FindAll("[data-slot=gallery-overflow]"));
        Assert.Contains("grid-cols-6", all.Find("[data-slot=gallery-grid]").ClassList);

        var one = Render<KlyImageGallery>(p => p.Add(x => x.Images, images).Add(x => x.Max, 0));
        Assert.Single(one.FindAll("[data-slot=gallery-tile]"));
        Assert.Equal("+1", one.Find("[data-slot=gallery-overflow]").TextContent);
    }

    // ---- Existing components that gained features

    [Fact]
    public void Alert_details_are_collapsed_and_open_with_a_toggle()
    {
        var cut = Render<KlyAlert>(p => p.Add(a => a.Color, KlyColor.Warning).Add(a => a.Title, "Quota warning").Add(a => a.Details, "85% of monthly volume used"));

        Assert.Empty(cut.FindAll("[data-slot=details]"));
        Assert.Equal("Show details", cut.Find("[data-slot=details-toggle]").TextContent.Trim());
        Assert.Equal("false", cut.Find("[data-slot=details-toggle]").GetAttribute("aria-expanded"));

        cut.Find("[data-slot=details-toggle]").Click();

        Assert.Contains("85% of monthly volume used", cut.Find("[data-slot=details]").TextContent);
        Assert.Equal("Hide details", cut.Find("[data-slot=details-toggle]").TextContent.Trim());
        Assert.Equal("true", cut.Find("[data-slot=details-toggle]").GetAttribute("aria-expanded"));
    }

    [Fact]
    public void Alert_DefaultOpen_starts_with_the_details_open_and_the_toggle_waits_for_interactivity()
    {
        var open = Render<KlyAlert>(p => p.Add(a => a.Details, "More").Add(a => a.DefaultOpen, true));
        Assert.Equal("More", open.Find("[data-slot=details]").TextContent);

        using var context = new BlocksStaticContext();
        var inert = context.Render<KlyAlert>(p => p.Add(a => a.Details, "More"));
        Assert.True(inert.Find("[data-slot=details-toggle]").HasAttribute("disabled"));
    }

    [Theory]
    [InlineData("success")]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("info")]
    public void Alert_has_a_status_icon_on_request_and_no_role_by_default(string color)
    {
        var cut = Render<KlyAlert>(p => p.Add(a => a.Color, new KlyColor(color)).Add(a => a.StatusIcon, true).Add(a => a.Title, "T"));

        Assert.Null(cut.Find("[data-color]").GetAttribute("role"));
        Assert.NotNull(cut.Find("svg"));
        Assert.Empty(Render<KlyAlert>(p => p.Add(a => a.Title, "T")).FindAll("svg"));
    }

    [Theory]
    [InlineData(KlyAlertLive.None, null)]
    [InlineData(KlyAlertLive.Polite, "status")]
    [InlineData(KlyAlertLive.Assertive, "alert")]
    public void Alert_is_a_live_region_only_when_asked(KlyAlertLive live, string? role)
    {
        var cut = Render<KlyAlert>(p => p.Add(a => a.Color, KlyColor.Error).Add(a => a.Title, "T").Add(a => a.Live, live));

        Assert.Equal(role, cut.Find("[data-color]").GetAttribute("role"));
    }

    [Fact]
    public void Image_without_a_caption_is_a_bare_img_and_with_one_a_figure()
    {
        var bare = Render<KlyImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "A").Add(i => i.Class, "rounded"));
        Assert.Empty(bare.FindAll("figure"));
        Assert.Contains("rounded", bare.Find("img").ClassList);

        var cut = Render<KlyImage>(p => p.Add(i => i.Src, "/a.png").Add(i => i.Alt, "A box").Add(i => i.Caption, "A box on a shelf").Add(i => i.Class, "rounded").Add(i => i.Ui, new ImageUi { Caption = "italic" }));

        var figure = cut.Find("figure");
        Assert.Equal("A box", figure.QuerySelector("img")!.GetAttribute("alt"));
        Assert.Contains("rounded", figure.QuerySelector("img")!.ClassList);
        Assert.DoesNotContain("rounded", figure.ClassList);
        Assert.Equal("A box on a shelf", figure.QuerySelector("figcaption")!.TextContent);
        Assert.Contains("italic", figure.QuerySelector("figcaption")!.ClassList);
        Assert.Equal(["IMG", "FIGCAPTION"], figure.Children.Select(c => c.TagName));
    }

    [Fact]
    public async Task Alert_and_Empty_render_typed_actions()
    {
        var clicks = 0;
        var alert = Render<KlyAlert>(p => p.Add(a => a.Title, "T").Add(a => a.ActionItems, [new KlyActionItem("Retry") { OnClick = EventCallback.Factory.Create(this, () => clicks++) }, new KlyActionItem("Docs") { Href = "/docs" }]));
        var empty = Render<KlyEmpty>(p => p.Add(e => e.Title, "No artifacts").Add(e => e.Description, "Upload a file to start.").Add(e => e.ActionItems, [new KlyActionItem("Upload") { Icon = "inbox", Color = KlyColor.Primary, Variant = ButtonVariant.Solid }]));

        await alert.Find("button[data-slot=action]").ClickAsync();

        Assert.Equal(1, clicks);
        Assert.Equal("/docs", alert.Find("a[data-slot=action]").GetAttribute("href"));
        Assert.Contains("No artifacts", empty.Markup);
        Assert.Equal("primary", empty.Find("[data-slot=action]").GetAttribute("data-color"));
        Assert.NotNull(empty.Find("[data-slot=action] svg"));
    }

    private sealed record Metric(string Name, double Value);

    [Fact]
    public void Table_footer_cells_follow_the_columns_and_show_only_when_one_is_given()
    {
        Metric[] rows = [new("Latency", 14), new("Errors", 2)];

        var plain = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).AddChildContent<KlyColumn<Metric, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, m => m.Name)));
        Assert.Empty(plain.FindAll("tfoot"));

        var cut = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).AddChildContent(b =>
        {
            b.OpenComponent<KlyColumn<Metric, string>>(0);
            b.AddComponentParameter(1, "Title", "Name");
            b.AddComponentParameter(2, "Field", (Func<Metric, string>)(m => m.Name));
            b.AddComponentParameter(3, "Footer", "Total");
            b.CloseComponent();
            b.OpenComponent<KlyColumn<Metric, double>>(4);
            b.AddComponentParameter(5, "Title", "Value");
            b.AddComponentParameter(6, "Field", (Func<Metric, double>)(m => m.Value));
            b.AddComponentParameter(7, "Numeric", true);
            b.AddComponentParameter(8, "FooterTemplate", (RenderFragment)(f => f.AddMarkupContent(0, "<b>16</b>")));
            b.CloseComponent();
        }));

        var cells = cut.FindAll("tfoot td");
        Assert.Equal(2, cells.Count);
        Assert.Equal("Total", cells[0].TextContent);
        Assert.Equal("16", cells[1].TextContent);
        Assert.Contains("text-end", cells[1].ClassList);
    }

    [Fact]
    public void A_column_aligned_to_the_end_and_a_numeric_one_use_text_end_and_a_mono_one_the_mono_font()
    {
        Metric[] rows = [new("latency_p95", 14)];
        var cut = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).AddChildContent(b =>
        {
            b.OpenComponent<KlyColumn<Metric, string>>(0);
            b.AddComponentParameter(1, "Title", "A");
            b.AddComponentParameter(2, "Field", (Func<Metric, string>)(m => m.Name));
            b.AddComponentParameter(3, "Mono", true);
            b.CloseComponent();
            b.OpenComponent<KlyColumn<Metric, string>>(4);
            b.AddComponentParameter(5, "Title", "B");
            b.AddComponentParameter(6, "Field", (Func<Metric, string>)(m => m.Name));
            b.AddComponentParameter(7, "Align", KlyColumnAlign.End);
            b.CloseComponent();
            b.OpenComponent<KlyColumn<Metric, double>>(8);
            b.AddComponentParameter(9, "Title", "C");
            b.AddComponentParameter(10, "Field", (Func<Metric, double>)(m => m.Value));
            b.AddComponentParameter(11, "Numeric", true);
            b.CloseComponent();
        }));

        Assert.Equal([false, true, true], cut.FindAll("thead th").Select(h => h.ClassList.Contains("text-end")));
        Assert.Equal([false, true, true], cut.FindAll("tbody td").Select(h => h.ClassList.Contains("text-end")));
        Assert.Contains("tabular-nums", cut.FindAll("tbody td")[2].ClassList);
        Assert.Contains("font-mono", cut.FindAll("tbody td")[0].ClassList);
        Assert.Contains("latency_p95", cut.Markup);
    }

    [Fact]
    public void Table_sticky_header_keeps_its_head_in_a_scrolling_box_and_Max_reveals_more_rows()
    {
        Metric[] rows = [new("a", 1), new("b", 2), new("c", 3)];
        var sticky = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).Add(t => t.Sticky, true).Add(t => t.Class, "max-h-80").AddChildContent<KlyColumn<Metric, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, m => m.Name)));
        Assert.Contains("sticky", sticky.Find("thead").ClassList);
        Assert.Contains("max-h-80", sticky.Find("div").ClassList);

        var plain = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).AddChildContent<KlyColumn<Metric, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, m => m.Name)));
        Assert.DoesNotContain("sticky", plain.Find("thead").ClassList);

        var cut = Render<KlyTable<Metric>>(p => p.Add(t => t.Items, rows).Add(t => t.Max, 1).Add(t => t.Step, 1).AddChildContent<KlyColumn<Metric, string>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, m => m.Name)));
        Assert.Single(cut.FindAll("tbody tr"));
        Assert.Equal("Show more · 1", cut.Find("[data-slot=more]").TextContent.Trim());
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        cut.Find("[data-slot=more]").Click();
        Assert.Equal(3, cut.FindAll("tbody tr").Count);
        Assert.Empty(cut.FindAll("[data-slot=more]"));
    }

    [Fact]
    public void User_shows_its_avatar_image_color_badge_and_contact_links()
    {
        var cut = Render<KlyUser>(p => p
            .Add(x => x.Name, "Ada Lovelace")
            .Add(x => x.Description, "Engineer")
            .Add(x => x.AvatarSrc, "/img/ada.png")
            .Add(x => x.Badge, new KlyBadgeItem("Owner"))
            .Add(x => x.Contacts, [new KlyContactItem("ada@example.com") { Icon = "mail", Href = "mailto:ada@example.com" }, new KlyContactItem("+33 1 23") { Copy = false }]));

        Assert.Equal("/img/ada.png", cut.Find("img[data-kly-avatar-image]").GetAttribute("src"));
        Assert.Contains("Owner", cut.Find("[data-slot=user-badge]").TextContent);
        var contacts = cut.FindAll("[data-slot=user-contact]");
        Assert.Equal("mailto:ada@example.com", contacts[0].QuerySelector("a")!.GetAttribute("href"));
        Assert.NotNull(contacts[0].QuerySelector("svg"));
        Assert.Equal("Copy ada@example.com", contacts[0].QuerySelector("button")!.GetAttribute("aria-label"));
        Assert.Null(contacts[1].QuerySelector("button"));
        // The contacts sit next to the user, not inside a link.
        Assert.Empty(cut.FindAll("a a"));

        var toned = Render<KlyUser>(p => p.Add(x => x.Name, "Grace Hopper").Add(x => x.AvatarColor, KlyColor.Info));
        Assert.Equal("info", toned.Find("[role=img]").GetAttribute("data-color"));
        Assert.Equal("GH", toned.Find("[role=img] [aria-hidden=true]").TextContent.Trim());
    }

    [Fact]
    public void User_without_a_name_shows_a_person_icon_and_no_name()
    {
        var cut = Render<KlyUser>(p => p.Add(x => x.Description, "Unassigned"));

        Assert.Empty(cut.FindAll("[data-slot=name]"));
        Assert.NotNull(cut.Find("[role=img] svg"));
        Assert.Equal("Unassigned", cut.Find("[data-slot=description]").TextContent);
    }

    [Fact]
    public void Avatar_group_puts_description_and_badge_in_the_tooltip_and_the_accessible_name()
    {
        var cut = Render<KlyAvatarGroup>(p => p.Add(x => x.Max, 1).Add(x => x.Items, [
            new KlyAvatarItem(Alt: "Ada Lovelace") { Description = "Engineer", Badge = new KlyBadgeItem("Owner"), Color = KlyColor.Info },
            new KlyAvatarItem(Alt: "Grace Hopper"),
            new KlyAvatarItem(Alt: "Alan Turing"),
        ]));

        var first = cut.FindAll("[role=img]")[0];
        Assert.Equal("Ada Lovelace · Engineer · Owner", first.GetAttribute("title"));
        Assert.Equal("Ada Lovelace · Engineer · Owner", first.GetAttribute("aria-label"));
        Assert.Equal("info", first.GetAttribute("data-color"));
        Assert.Equal("AL", first.QuerySelector("[aria-hidden=true]")!.TextContent.Trim());
        Assert.Equal("+2", cut.FindAll("[role=img]")[1].QuerySelector("[aria-hidden=true]")!.TextContent.Trim());
    }

    [Fact]
    public void Accordion_items_can_start_open_and_show_text()
    {
        var cut = Render<KlyAccordion>(p => p.Add(a => a.Items, [new KlyAccordionItem("Section A") { Text = "Content A", DefaultOpen = true }, new KlyAccordionItem("Section B") { Text = "Content B", DefaultOpen = true }]));

        var items = cut.FindAll("[data-slot=item]");
        Assert.Equal(["open", "closed"], items.Select(i => i.GetAttribute("data-state")));
        Assert.Equal("true", items[0].QuerySelector("[data-slot=trigger]")!.GetAttribute("aria-expanded"));
        Assert.Contains("Content A", items[0].QuerySelector("[data-slot=content]")!.TextContent);
        Assert.Equal("until-found", items[1].QuerySelector("[data-slot=content]")!.GetAttribute("hidden"));
        Assert.Equal("0", cut.Find("kly-accordion").GetAttribute("value"));

        var multiple = Render<KlyAccordion>(p => p.Add(a => a.Multiple, true).Add(a => a.Items, [new KlyAccordionItem("A") { DefaultOpen = true }, new KlyAccordionItem("B") { DefaultOpen = true }]));
        Assert.Equal(["open", "open"], multiple.FindAll("[data-slot=item]").Select(i => i.GetAttribute("data-state")));

        // A value that is set wins over the default.
        var controlled = Render<KlyAccordion>(p => p.Add(a => a.Value, ["1"]).Add(a => a.Items, [new KlyAccordionItem("A") { DefaultOpen = true }, new KlyAccordionItem("B")]));
        Assert.Equal(["closed", "open"], controlled.FindAll("[data-slot=item]").Select(i => i.GetAttribute("data-state")));
    }

    [Fact]
    public void Accordion_content_wins_over_text()
    {
        var cut = Render<KlyAccordion>(p => p.Add(a => a.Items, [new KlyAccordionItem("A") { Text = "text", Content = b => b.AddContent(0, "markup") }]));

        Assert.Equal("markup", cut.Find("[data-slot=content]").TextContent);
    }

    // ---- Timeline kinds of the old Display tests

    [Fact]
    public void Timeline_uses_date_icon_and_color_and_the_log_layout_shows_timestamps()
    {
        WithCulture("en-US", () =>
        {
            var date = new DateTimeOffset(2026, 11, 20, 14, 30, 0, TimeSpan.Zero);
            var culture = CultureInfo.GetCultureInfo("en-US");
            KlyTimelineItem[] items =
            [
                new("Deployed") { At = date, Icon = Icon("zap"), Color = KlyColor.Success },
                new("Rolled back") { Date = "15:02", At = date },
            ];

            var cut = Render<KlyTimeline>(p => p.Add(x => x.Items, items));
            var time = cut.Find("time[data-slot=timeline-time]");
            Assert.Equal(date.ToString("O", CultureInfo.InvariantCulture), time.GetAttribute("datetime"));
            Assert.Equal(date.ToString("g", culture), time.TextContent);
            var marker = cut.Find("[data-slot=timeline-marker]");
            Assert.Equal("success", marker.GetAttribute("data-color"));
            Assert.NotNull(marker.QuerySelector("svg"));
            Assert.Equal("15:02", cut.FindAll("[data-slot=timeline-time]")[1].TextContent);

            var log = Render<KlyTimeline>(p => p.Add(x => x.Items, items).Add(x => x.Layout, KlyTimelineLayout.Log));
            Assert.NotNull(log.Find("[data-slot=timeline-log]"));
            var stamps = log.FindAll("[data-slot=timeline-timestamp]");
            Assert.Equal($"{date.ToString("d", culture)} {date.ToString("t", culture)}", stamps[0].TextContent);
            Assert.Equal("11/20/2026 15:02", stamps[1].TextContent);
        });
    }

    [Fact]
    public void Timeline_states_mark_the_current_event_and_the_pipeline_numbers_its_markers()
    {
        KlyTimelineItem[] items =
        [
            new("Created") { State = KlyTimelineState.Done },
            new("In Review") { State = KlyTimelineState.Current, Actor = "Alice", Description = "Waiting" },
            new("Published") { State = KlyTimelineState.Upcoming },
        ];

        var cut = Render<KlyTimeline>(p => p.Add(x => x.Items, items).Add(x => x.Layout, KlyTimelineLayout.Pipeline).Add(x => x.ShowNumbers, true));

        Assert.Equal(["done", "current", "upcoming"], cut.FindAll("[data-slot=timeline-item]").Select(i => i.GetAttribute("data-state")));
        Assert.Equal([null, "step", null], cut.FindAll("[data-slot=timeline-item]").Select(i => i.GetAttribute("aria-current")));
        Assert.Equal(["1", "2", "3"], cut.FindAll("[data-slot=timeline-marker]").Select(m => m.TextContent.Trim()));
        Assert.Contains("In Review", cut.Markup);
        Assert.Contains("Alice", cut.Markup);
        Assert.Empty(cut.FindAll("[data-slot=timeline-item] [data-state][class*=h-0]"));
    }

    [Fact]
    public void Timeline_default_layout_keeps_Active_and_lets_a_state_override_it()
    {
        var active = Render<KlyTimeline>(p => p.Add(x => x.Items, [new KlyTimelineItem("A"), new KlyTimelineItem("B"), new KlyTimelineItem("C") { State = KlyTimelineState.Upcoming }]).Add(x => x.Active, 1));

        Assert.Equal(["active", "active", "upcoming"], active.FindAll("li").Select(i => i.GetAttribute("data-state")));
        Assert.Equal([null, "step", null], active.FindAll("li").Select(i => i.GetAttribute("aria-current")));
    }

    [Fact]
    public void Timeline_Max_and_Step_reveal_more_events_in_every_layout()
    {
        KlyTimelineItem[] items = [new("A"), new("B"), new("C")];
        foreach (var layout in new[] { KlyTimelineLayout.Default, KlyTimelineLayout.Pipeline, KlyTimelineLayout.Log })
        {
            var cut = Render<KlyTimeline>(p => p.Add(x => x.Items, items).Add(x => x.Layout, layout).Add(x => x.Max, 1).Add(x => x.Step, 1));

            Assert.Single(cut.FindAll("li"));
            cut.Find("[data-slot=more]").Click();
            Assert.Equal(2, cut.FindAll("li").Count);
        }
    }

    [Fact]
    public void Timeline_item_keeps_its_positional_parameter_and_copies_with_an_end_date()
    {
        var item = new KlyTimelineItem("T") { Description = "D", Date = "9", At = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), Actor = "a", State = KlyTimelineState.Done, Color = KlyColor.Error };
        Assert.Null(item.EndAt);

        var withEnd = item with { EndAt = new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero) };

        Assert.Equal(new DateTimeOffset(2026, 3, 2, 0, 0, 0, TimeSpan.Zero), withEnd.EndAt);
        Assert.Equal(KlyColor.Error, withEnd.Color);
    }

    // ---- Chart: the funnel and the other charts of the old Display tests

    private sealed record Step(string Label, double Value);

    private IRenderedComponent<KlyChart<Step, string>> Chart(KlyChartType type, IReadOnlyList<Step> data, Func<double, string>? format = null, string? name = "Revenue", Action<ComponentParameterCollectionBuilder<KlyChart<Step, string>>>? configure = null, KlyChartCategory<Step>? category = null) =>
        Render<KlyChart<Step, string>>(p =>
        {
            p.Add(c => c.Data, data)
             .Add(c => c.Index, s => s.Label)
             .Add(c => c.Categories, [category ?? new KlyChartCategory<Step>("value", s => s.Value) { Label = name ?? "Value" }])
             .Add(c => c.Type, type)
             .Add(c => c.Format, format)
             .Add(c => c.Table, KlyChartTable.Toggle)
             .Add(c => c.Legend, false);
            configure?.Invoke(p);
        });

    [Fact]
    public void Bar_chart_has_axes_tooltip_and_a_data_table_toggle()
    {
        var cut = Chart(KlyChartType.Bar, [new("Mon", 10), new("Tue", 20)]);

        Assert.NotEmpty(cut.FindAll("kly-chart text"));
        Assert.NotEmpty(cut.FindAll("kly-chart [data-slot=tips] [data-tip]"));
        Assert.NotNull(cut.Find("kly-chart details table"));
        var label = cut.Find("kly-chart [data-slot=plot]").GetAttribute("aria-label")!;
        Assert.Contains("Revenue", label);
        Assert.Contains("20", label);
        var rows = cut.FindAll("kly-chart table tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Mon", rows[0].TextContent);
        Assert.Contains("10", rows[0].TextContent);
    }

    [Fact]
    public void Bar_chart_keeps_negative_and_zero_values_honest()
    {
        var cut = Chart(KlyChartType.Bar, [new("A", -5), new("B", 0), new("C", 10)]);

        var heights = cut.FindAll("kly-chart svg rect[rx]").Select(r => double.Parse(r.GetAttribute("height")!, CultureInfo.InvariantCulture)).ToList();
        Assert.DoesNotContain(heights, h => h < 0);
        Assert.True(heights[0] > 20, string.Join(",", heights));
        Assert.True(heights[1] <= 1, string.Join(",", heights));
        Assert.Equal(heights[2] / 2, heights[0], 3);
        Assert.Contains("-5", cut.FindAll("kly-chart [data-tip]")[0].GetAttribute("data-text"));
    }

    [Fact]
    public void A_chart_of_one_series_has_one_value_column_in_its_table()
    {
        var cut = Chart(KlyChartType.Bar, [new("A", 1), new("B", 2), new("C", 3)]);

        Assert.Equal(["chart-1"], cut.FindAll("kly-chart svg g[data-series]").Select(g => g.GetAttribute("data-color")));
        Assert.Equal(2, cut.FindAll("kly-chart table thead th").Count);

        var colored = Chart(KlyChartType.Bar, [new("A", 1)], category: new KlyChartCategory<Step>("v", s => s.Value) { Color = KlyColor.Error });
        Assert.Equal("error", colored.Find("kly-chart svg g[data-series]").GetAttribute("data-color"));
    }

    [Fact]
    public void A_funnel_ignores_non_finite_steps_and_scales_to_the_largest_positive_one()
    {
        var cut = Chart(KlyChartType.Funnel, [new("A", 0.8), new("B", 0.4), new("C", double.NaN), new("D", double.PositiveInfinity)]);

        var widths = cut.FindAll("[data-slot=funnel-step] [aria-hidden=true]").Select(b => b.GetAttribute("style")).ToList();
        Assert.Equal(["width:100%", "width:50%", "width:0%", "width:0%"], widths);
    }

    [Fact]
    public void Line_chart_is_exposed_to_assistive_technology_with_values()
    {
        var cut = Chart(KlyChartType.Line, [new("Mon", 0), new("Tue", 50), new("Wed", 100)], name: null);

        var label = cut.Find("kly-chart [data-slot=plot]").GetAttribute("aria-label")!;
        Assert.Contains("Mon", label);
        Assert.Contains("Wed", label);
        Assert.Contains("100", label);
        Assert.Equal(3, cut.FindAll("kly-chart table tbody tr").Count);
        Assert.Contains("Value", cut.Find("kly-chart table").TextContent);
    }

    [Fact]
    public void Chart_Format_formats_axis_tooltip_table_and_funnel_values()
    {
        Func<double, string> euro = v => $"{v:0}EUR";

        var bars = Chart(KlyChartType.Bar, [new("A", 12)], euro);
        Assert.Contains("12EUR", bars.Find("kly-chart [data-tip]").GetAttribute("data-text"));
        Assert.Contains("12EUR", bars.Find("kly-chart table").TextContent);
        Assert.Contains(bars.FindAll("kly-chart svg text"), t => t.TextContent.EndsWith("EUR"));

        var funnel = Chart(KlyChartType.Funnel, [new("Visits", 100), new("Signups", 25)], euro);
        Assert.Contains("100EUR", funnel.FindAll("[data-slot=funnel-step]")[0].TextContent);
        Assert.Contains("25EUR (25%)", funnel.FindAll("[data-slot=funnel-step]")[1].TextContent);
    }

    [Fact]
    public void Default_chart_format_follows_the_format_culture()
    {
        var cut = Chart(KlyChartType.Funnel, [new("Visits", 12345.5)]);

        Assert.Contains("12,345.5", cut.Find("[data-slot=funnel-step]").TextContent);
    }

    [Fact]
    public void Funnel_zero_and_negative_steps_have_no_bar_and_positive_ones_stay_visible()
    {
        var cut = Chart(KlyChartType.Funnel, [new("Visits", 1000), new("Tiny", 1), new("None", 0), new("Refunds", -20)]);

        var widths = cut.FindAll("[data-slot=funnel-step] [aria-hidden=true]").Select(d => d.GetAttribute("style")!).ToList();
        Assert.Equal(["width:100%", "width:2%", "width:0%", "width:0%"], widths);
    }

    [Fact]
    public void A_funnel_is_a_list_of_steps_each_with_its_share_of_the_previous_one_and_a_color()
    {
        var cut = Chart(KlyChartType.Funnel, [new("Visits", 200), new("Signups", 50), new("Paid", 10)], configure: p => p.Add(c => c.AriaLabel, "Conversion").Add(c => c.Colors, [KlyColor.Success]));

        Assert.Equal("Conversion", cut.Find("ol").GetAttribute("aria-label"));
        var steps = cut.FindAll("[data-slot=funnel-step]");
        Assert.DoesNotContain("%", steps[0].TextContent);
        Assert.Contains("(25%)", steps[1].TextContent);
        Assert.Contains("(20%)", steps[2].TextContent);
        Assert.Equal("success", steps[0].QuerySelector("[aria-hidden=true]")!.GetAttribute("data-color"));
        Assert.Empty(cut.FindAll("kly-chart"));
    }

    [Fact]
    public void Chart_without_data_says_so()
    {
        var funnel = Chart(KlyChartType.Funnel, []);
        Assert.Equal("No data", funnel.Find("[data-slot=chart-empty]").TextContent);

        var bars = Chart(KlyChartType.Bar, []);
        Assert.Contains("No data", AccessibleName(bars));
    }

    private static string AccessibleName(IRenderedComponent<KlyChart<Step, string>> cut) =>
        cut.Find("svg").GetAttribute("aria-label") ?? cut.Find("[data-slot=plot]").GetAttribute("aria-label")!;
}
