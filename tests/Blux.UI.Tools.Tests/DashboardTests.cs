using System.Globalization;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blux.UI.Tools.Tests;

public sealed class DashboardTests : BunitContext
{
    public DashboardTests()
    {
        Services.AddBluxUiTools();
    }

    [Fact]
    public void DashboardFormat_formats_presets_comparison_ranges_and_csv_export()
    {
        Assert.Equal("—", DashboardFormat.Format(null));
        Assert.Equal("—", DashboardFormat.Format(double.NaN));
        Assert.Equal("12.6K", DashboardFormat.Format(12600, DashboardFormatPreset.Compact, CultureInfo.InvariantCulture));
        Assert.Equal("+14.2%", DashboardFormat.Format(14.2, DashboardFormatPreset.Delta, CultureInfo.InvariantCulture));
        Assert.Equal("+2.1 pts", DashboardFormat.Format(2.1, DashboardFormatPreset.Points, CultureInfo.InvariantCulture));
        Assert.Equal("+1 pt", DashboardFormat.Format(1.0, DashboardFormatPreset.Points, CultureInfo.InvariantCulture));
        Assert.Equal("57%", DashboardFormat.Format(0.57, DashboardFormatPreset.Ratio, CultureInfo.InvariantCulture));

        // Leap year Feb 29 -> Feb 28 on Year comparison
        var leapStart = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var leapEnd = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero);
        var yearRange = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Year);
        Assert.NotNull(yearRange);
        Assert.Equal(new DateTimeOffset(2023, 2, 28, 0, 0, 0, TimeSpan.Zero), yearRange.Value.End);

        var prevRange = DashboardFormat.ResolveComparisonRange((leapStart, leapEnd), DashboardComparisonMode.Previous);
        Assert.NotNull(prevRange);
        Assert.Equal(29, (int)(prevRange.Value.End - prevRange.Value.Start).TotalDays + 1);

        Assert.Equal("consommation-mensuelle.csv", DashboardFormat.ResolveFileName("Consommation mensuelle"));

        var table = new DashboardDataTable(
            [new DashboardDataColumn("month", "Month"), new DashboardDataColumn("total", "Total", Numeric: true)],
            [new Dictionary<string, string?> { ["month"] = "Jan", ["total"] = "120" }]);
        var csv = DashboardFormat.ExportCsv(table, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.StartsWith("\uFEFF\"Month\";\"Total\"", csv);
        Assert.Contains("\"Jan\";120", csv);
    }

    [Fact]
    public async Task DashboardController_coordinates_staged_queries_filters_and_views()
    {
        var order = new List<string>();
        var controller = new DashboardController("analytics");
        controller.AddView("overview", "Overview", badge: "3");
        controller.AddView("details", "Details");

        var yearFilter = controller.AddFilter(new DashboardFilterControl(
            "year",
            "Year",
            items: [new DashboardOption("2025", "2025"), new DashboardOption("2026", "2026")],
            defaultValues: ["2026"]));

        var essential = controller.RegisterResource(new DashboardResource<int>(
            "summary",
            async _ => { await Task.Yield(); order.Add("essential"); return 42; },
            DashboardQueryStage.Essential));

        var background = controller.RegisterResource(new DashboardResource<string>(
            "accounts",
            async _ => { await Task.Yield(); order.Add("background"); return "ok"; },
            DashboardQueryStage.Background));

        var deferred = controller.RegisterResource(new DashboardResource<string>(
            "lazy",
            async _ => { await Task.Yield(); order.Add("deferred"); return "lazy-ok"; },
            DashboardQueryStage.Deferred));

        await controller.RefreshAsync();
        Assert.Equal(["essential", "background"], order);
        Assert.Equal(42, essential.Data);
        Assert.Equal(DashboardResourceState.Idle, deferred.State);

        var cut = Render<BxDashboardPage>(p => p
            .Add(x => x.Dashboard, controller)
            .Add(x => x.Title, "Revenue Analytics")
            .AddChildContent<BxDashboardGrid>(g => g
                .Add(x => x.Menu, true)
                .Add(x => x.Freshness, true)
                .AddChildContent<BxDashboardStat>(s => s
                    .Add(x => x.Source, essential)
                    .Add(x => x.Label, "Units")
                    .Add(x => x.Value, 120)
                    .Add(x => x.Compare, 100)
                    .Add(x => x.Goal, 150)
                    .Add(x => x.Trend, [80, 95, 110, 120]))));

        Assert.Equal("Revenue Analytics", cut.Find("[data-slot=dashboard-title]").TextContent);
        Assert.Equal(2, cut.FindAll("[data-slot=dashboard-view-tab]").Count);
        Assert.Equal("3", cut.Find("[data-slot=dashboard-view-badge]").TextContent);
        Assert.Contains("+20%", cut.Find("[data-slot=dashboard-stat-delta]").TextContent);
        Assert.NotNull(cut.Find("[data-slot=dashboard-stat-goal-bar]"));

        // Change filter value and verify Reset button appears and restores default
        yearFilter.Toggle("2025");
        cut.Render();
        Assert.True(controller.Filtered);
        cut.Find("[data-slot=dashboard-filters-reset]").Click();
        Assert.False(controller.Filtered);
        Assert.Equal("2026", yearFilter.Value);
    }

    [Fact]
    public void BxDashboardBlocks_render_Table_sorting_Bars_Funnel_Alerts_Feed_Gauge_and_Details()
    {
        var cut = Render<BxDashboardTable>(p => p
            .Add(x => x.Title, "Top Regions")
            .Add(x => x.Menu, true)
            .Add(x => x.Columns, [
                new DashboardTableColumn("region", "Region", Type: "text"),
                new DashboardTableColumn("revenue", "Revenue", Type: "bar"),
                new DashboardTableColumn("delta", "Growth", Type: "delta")
            ])
            .Add(x => x.Rows, [
                new Dictionary<string, object?> { ["region"] = "EMEA", ["revenue"] = 200.0, ["delta"] = 12.5 },
                new Dictionary<string, object?> { ["region"] = "AMER", ["revenue"] = 450.0, ["delta"] = -4.0 }
            ]));

        // Click numeric column "revenue": natural sort is descending (AMER 450 first)
        cut.FindAll("[data-slot=dashboard-table-sort]")[1].Click();
        Assert.Equal("descending", cut.FindAll("[data-slot=dashboard-table-header]")[1].GetAttribute("aria-sort"));
        Assert.Contains("AMER", cut.FindAll("[data-slot=dashboard-table-row]")[0].TextContent);

        // Second click flips to ascending (EMEA 200 first)
        cut.FindAll("[data-slot=dashboard-table-sort]")[1].Click();
        Assert.Equal("ascending", cut.FindAll("[data-slot=dashboard-table-header]")[1].GetAttribute("aria-sort"));
        Assert.Contains("EMEA", cut.FindAll("[data-slot=dashboard-table-row]")[0].TextContent);

        // Third click resets to unsorted
        cut.FindAll("[data-slot=dashboard-table-sort]")[1].Click();
        Assert.Equal("none", cut.FindAll("[data-slot=dashboard-table-header]")[1].GetAttribute("aria-sort"));

        // Funnel bars with step conversion rate and table toggle
        var funnel = Render<BxDashboardBars>(p => p
            .Add(x => x.Title, "Signup Funnel")
            .Add(x => x.Variant, "funnel")
            .Add(x => x.Menu, true)
            .Add(x => x.Items, [
                new DashboardBarItem("Visited", 1000),
                new DashboardBarItem("Signed up", 250)
            ]));
        var notes = funnel.FindAll("[data-slot=dashboard-funnel-note]");
        Assert.Equal("base 100%", notes[0].TextContent);
        Assert.Equal("25% of the previous step", notes[1].TextContent);
        funnel.Find("[data-slot=dashboard-card-table-toggle]").Click();
        Assert.NotNull(funnel.Find("[data-slot=dashboard-data-table]"));

        // Alerts ordered by severity (Error before Info)
        var alerts = Render<BxDashboardAlerts>(p => p
            .Add(x => x.Title, "System Alerts")
            .Add(x => x.Items, [
                new DashboardAlertItem(DashboardAlertSeverity.Info, "Backup completed"),
                new DashboardAlertItem(DashboardAlertSeverity.Error, "High latency")
            ]));
        Assert.Equal("error", alerts.FindAll("[data-slot=dashboard-alert-row]")[0].GetAttribute("data-severity"));

        // Gauge & Feed
        var gauge = Render<BxDashboardGauge>(p => p
            .Add(x => x.Title, "Uptime")
            .Add(x => x.Value, 99.5)
            .Add(x => x.Target, 99.9));
        Assert.Equal("meter", gauge.Find("[data-slot=dashboard-gauge]").GetAttribute("role"));
        Assert.Contains("99.5", gauge.Find("[data-slot=gauge-value]").TextContent);

        var now = new DateTimeOffset(2026, 11, 24, 12, 0, 0, TimeSpan.Zero);
        var feed = Render<BxDashboardFeed>(p => p
            .Add(x => x.Title, "Activity")
            .Add(x => x.Now, now)
            .Add(x => x.Items, [
                new DashboardFeedItem("Deployed v2.1", now.AddMinutes(-10), AvatarText: "GP"),
                new DashboardFeedItem("Merged PR #42", now.AddDays(-1), AvatarText: "AL")
            ]));
        var days = feed.FindAll("[data-slot=dashboard-feed-day]");
        Assert.Equal(2, days.Count);
        Assert.Equal("Today", days[0].TextContent.Trim());
        Assert.Equal("Yesterday", days[1].TextContent.Trim());
    }
}
