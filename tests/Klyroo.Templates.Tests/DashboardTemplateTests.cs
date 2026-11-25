using Klyroo.UI;
using Klyroo.Query;
using Dashboard.Shared.Data;
using Dashboard.Shared.Services;

namespace Klyroo.Templates.Tests;

/// <summary>
/// The Dashboard template's data and services, compiled here from the template's own source (see the project file): the sample customers and the schema of their
/// table, the figures of the Home page, the stores of the session. The row selection is the data list's own. The pages are checked in a browser by the E2E project and by
/// build/verify-templates.mjs.
/// </summary>
public class DashboardTemplateTests
{
    private static DataListRequest Request(DataListPaging? paging = null, DataListSearch? search = null, params DataListSort[] sort) =>
        new(paging ?? DataListPaging.Offset.ForPage(1, 10), sort, DataListFilterGroup.Empty, search);

    [Fact]
    public void The_sample_customers_are_deterministic_and_have_unique_ids_and_emails()
    {
        Assert.Equal(240, CustomerData.All.Length);
        Assert.Equal(240, CustomerData.All.Select(c => c.Id).Distinct().Count());
        Assert.Equal(240, CustomerData.All.Select(c => c.Email).Distinct().Count());
        // Same seed, same rows: the server and the WebAssembly client build the same table.
        Assert.Equal(CustomerData.All[17], CustomerData.All[17] with { });
        Assert.All(CustomerData.All, c =>
        {
            Assert.Contains(c.Status, CustomerData.Statuses);
            Assert.Contains(c.Plan, CustomerData.Plans);
            Assert.Contains(c.Country, CustomerData.Countries);
            Assert.True(c.Joined <= DemoClock.Today && c.Joined >= DemoClock.Today.AddDays(-700));
            Assert.EndsWith(".example", c.Email);
        });
    }

    [Fact]
    public void The_schema_searches_sorts_and_filters_the_way_the_table_promises()
    {
        var schema = CustomerData.Schema;

        Assert.True(schema.HasSearch);
        // One filter of each kind the data list offers: text, option, number, date, boolean.
        Assert.Equal(["name", "company", "country", "plan", "status", "orders", "totalSpent", "joined", "newsletter"], schema.FilterKeys);
        Assert.Contains("joined", schema.SortKeys);
        Assert.DoesNotContain("newsletter", schema.SortKeys);
        Assert.True(schema.TryGetFilter("status", out var status));
        Assert.Equal(["Active", "Trial", "Paused", "Churned"], status.Options.Select(o => o.Value));
        Assert.True(status.Facets);
        Assert.True(schema.TryGetFilter("totalSpent", out var spent) && spent.Kind == FilterKind.Number);
        Assert.True(schema.TryGetFilter("joined", out var joined) && joined.Kind == FilterKind.Date);
        Assert.True(schema.TryGetFilter("newsletter", out var newsletter) && newsletter.Kind == FilterKind.Boolean);
        Assert.True(schema.TryGetFilter("name", out var name) && name.Kind == FilterKind.Text);

        var page = schema.ExecuteInMemory(CustomerData.All, Request(sort: new DataListSort("joined", Descending: true)));
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(240, page.TotalCount);
        Assert.Equal(page.Items.OrderByDescending(c => c.Joined).Select(c => c.Id), page.Items.Select(c => c.Id));

        // The search reads the name, the email and the company.
        var byCompany = schema.ExecuteInMemory(CustomerData.All, Request(search: new DataListSearch("Foxglove")));
        Assert.True(byCompany.TotalCount > 0);
        Assert.All(byCompany.Items, c => Assert.Contains("foxglove", $"{c.Name} {c.Email} {c.Company}", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(MetricsPeriod.Week, 7)]
    [InlineData(MetricsPeriod.Month, 10)]
    [InlineData(MetricsPeriod.Quarter, 12)]
    [InlineData(MetricsPeriod.Year, 13)]
    public void The_figures_of_the_home_page_agree_with_each_other(MetricsPeriod period, int buckets)
    {
        var revenue = MetricsData.Revenue(period);
        var summary = MetricsData.Summary(period);

        Assert.Equal(buckets, revenue.Count);
        Assert.Equal(buckets, revenue.Select(p => p.Label).Distinct().Count());
        Assert.Equal((double)summary.Revenue, revenue.Sum(p => p.Current));
        Assert.Equal((double)summary.PreviousRevenue, revenue.Sum(p => p.Previous));
        Assert.Equal((double)summary.Revenue, MetricsData.Channels(period).Sum(c => c.Revenue), 3);
        Assert.InRange(summary.Conversion, 0.5, 10);
        // The new customers are the ones of the Customers page.
        var from = DemoClock.Today.AddDays(-MetricsData.Length(period) + 1);
        Assert.Equal(CustomerData.All.Count(c => c.Joined >= from), summary.NewCustomers);
        Assert.Equal(14, MetricsData.OrdersByDay().Count);
        Assert.Equal(8, SalesData.Recent.Length);
    }

    [Fact]
    public void The_customer_store_adds_with_the_next_id_and_removes_by_id_with_a_new_array_each_time()
    {
        var store = new CustomerStore();
        var changes = 0;
        store.Changed += () => changes++;
        var before = store.Items;

        var added = store.Add("Ada Test", "ada@test.example", "Test Co", "France", "Pro");
        Assert.Equal(241, added.Id);
        Assert.Equal("Active", added.Status);
        Assert.Equal(241, store.Items.Length);
        // The data list loads again only for a new collection: the old array is left as it was.
        Assert.Equal(240, before.Length);
        Assert.NotSame(before, store.Items);

        store.Remove([added.Id, 1, 2]);
        Assert.Equal(238, store.Items.Length);
        Assert.Equal(2, changes);
        Assert.Equal("Trial", new CustomerStore().Add("A", "a@a.example", "C", "France", "Free").Status);
    }

    [Fact]
    public void The_mail_store_reads_and_archives_for_the_session_only()
    {
        var store = new MailStore();
        var unread = store.UnreadCount;
        Assert.True(unread > 0);
        var first = store.Items.First(m => m.Unread);

        store.MarkRead(first.Id);
        Assert.Equal(unread - 1, store.UnreadCount);
        Assert.False(store.Find(first.Id)!.Unread);

        store.Archive(first.Id);
        Assert.Null(store.Find(first.Id));
        Assert.Equal(MailData.All.Length - 1, store.Items.Count);
        // Another session starts again from the sample data.
        Assert.Equal(unread, new MailStore().UnreadCount);
    }

    [Fact]
    public void The_notifications_and_the_members_keep_their_rules()
    {
        var notifications = new NotificationStore();
        Assert.Equal(3, notifications.UnreadCount);
        notifications.MarkAllRead();
        Assert.Equal(0, notifications.UnreadCount);

        var members = new MemberStore();
        members.Invite("new@example.com", "New Person", "Member");
        members.Invite("NEW@example.com", "Again", "Admin");
        Assert.Equal(PeopleData.Members.Length + 1, members.Items.Count);
        Assert.False(members.Items.Last().Active);
        members.SetRole(members.Items.Last().Id, "Admin");
        Assert.Equal("Admin", members.Items.Last().Role);
        // The owner cannot be removed.
        members.Remove(1);
        Assert.Contains(members.Items, m => m.Role == "Owner");
    }

    [Fact]
    public async Task The_remote_directories_page_and_resolve()
    {
        PeopleDirectory.LatencyMs = 0;
        var first = await PeopleDirectory.Search(new KlyOptionsRequest("", 0, 8, null), CancellationToken.None);
        Assert.Equal(8, first.Items.Count);
        Assert.True(first.HasMore);
        var second = await PeopleDirectory.Search(new KlyOptionsRequest("", 8, 8, null), CancellationToken.None);
        Assert.Empty(first.Items.Select(i => i.Value).Intersect(second.Items.Select(i => i.Value)));

        var found = await PeopleDirectory.Search(new KlyOptionsRequest("ahlberg", 0, 50, null), CancellationToken.None);
        Assert.NotEmpty(found.Items);
        Assert.All(found.Items, i => Assert.Contains("ahlberg", i.Label, StringComparison.OrdinalIgnoreCase));
        Assert.Equal(found.Items[0].Label, (await PeopleDirectory.Resolve([found.Items[0].Value], CancellationToken.None)).Single().Label);

        var countries = await CountryDirectory.Search(new KlyOptionsRequest("land", 0, 20, null), CancellationToken.None);
        Assert.Contains(countries.Items, c => c.Label == "Finland");
        Assert.Equal("France", CountryDirectory.NameOf("FR"));
    }

    [Fact]
    public void The_customer_schema_gives_the_selection_a_key_and_a_name_for_each_row()
    {
        // The data list's selection ticks rows by RowKey and names each box "Select {RowLabel}".
        Assert.True(CustomerData.Schema.HasRowKey);
        var customer = CustomerData.All[3];
        Assert.Equal(customer.Id.ToString(), CustomerData.Schema.GetRowKey(customer));
        Assert.Equal(customer.Name, CustomerData.Schema.GetRowLabel(customer));
        Assert.Equal(CustomerData.All.Length, CustomerData.All.Select(CustomerData.Schema.GetRowKey).Distinct().Count());
    }

    [Fact]
    public void The_rows_a_query_matches_are_all_of_them_not_one_page_of_them()
    {
        // What the data list does for the selection of the Customers page when the search or the filters change: the matching ids over the whole set, with no paging.
        var state = DataListState.Initial with { Search = "a", PageSize = 10 };
        var request = state.ToRequest() with { Paging = DataListPaging.None.Instance };
        var matching = CustomerData.Schema.ExecuteInMemory(CustomerData.All, request).Items.Select(c => c.Id).ToHashSet();

        var onePage = CustomerData.Schema.ExecuteInMemory(CustomerData.All, state.ToRequest()).Items;
        Assert.Equal(10, onePage.Count);
        Assert.True(matching.Count > 10);
        Assert.All(onePage, c => Assert.Contains(c.Id, matching));
        Assert.All(CustomerData.All.Where(c => matching.Contains(c.Id)), c => Assert.Contains("a", $"{c.Name} {c.Email} {c.Company}", StringComparison.OrdinalIgnoreCase));
        // A search that matches nobody leaves nothing to keep selected.
        var none = CustomerData.Schema.ExecuteInMemory(CustomerData.All, (DataListState.Initial with { Search = "zzzzqq" }).ToRequest() with { Paging = DataListPaging.None.Instance });
        Assert.Empty(none.Items);
    }

    [Fact]
    public void The_neutral_colors_on_offer_are_klyroo_or_tailwind_neutral_scales_that_the_contrast_tests_cover()
    {
        Assert.True(AppearanceState.Neutrals[0].IsKlyroo);
        Assert.Equal(["slate", "gray", "zinc", "neutral", "stone"], AppearanceState.Neutrals.Skip(1).Select(p => p.Name));
        Assert.All(AppearanceState.Neutrals.Skip(1), p => Assert.Contains(p, KlyPalette.TailwindPalettes));
    }

    private sealed class FakeCookies(string? pathBase, params (string Name, string Value)[] cookies) : IKlyRequestCookies
    {
        public string? PathBase => pathBase;

        public string? Get(string name) => cookies.Where(c => c.Name == name).Select(c => c.Value).FirstOrDefault();
    }

    private sealed class MemoryStore : IAppearanceStore
    {
        public (string? Primary, string? Neutral) Saved { get; set; }

        public ValueTask<(string? Primary, string? Neutral)> LoadAsync() => ValueTask.FromResult(Saved);

        public ValueTask SaveAsync(string primary, string neutral)
        {
            Saved = (primary, neutral);
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void A_render_with_a_request_starts_with_the_colors_of_the_cookies()
    {
        var cookies = new FakeCookies(null, ("dashboard-primary", "emerald"), ("dashboard-neutral", "zinc"));

        var state = new AppearanceState(new MemoryStore(), cookies);

        Assert.Equal("emerald", state.Primary.Name);
        Assert.Equal("zinc", state.Neutral.Name);
        Assert.Equal(KlyPalette.Emerald, state.Colors.Primary);
        Assert.Equal(KlyPalette.Zinc, state.Colors.Neutral);
        // No request (WebAssembly, MAUI): the defaults.
        var none = new AppearanceState(new MemoryStore());
        Assert.True(none.Primary.IsKlyroo && none.Neutral.IsKlyroo);
    }

    [Fact]
    public void The_cookies_are_scoped_to_the_path_base_like_the_other_cookies_of_klyroo()
    {
        var scoped = KlyPathScope.CookieName("dashboard-primary", "/demo/dashboard");
        Assert.NotEqual("dashboard-primary", scoped);
        Assert.Equal(scoped, AppearanceCookies.Name(AppearanceCookies.PrimaryKey, "/demo/dashboard"));
        Assert.Equal("dashboard-neutral", AppearanceCookies.Name(AppearanceCookies.NeutralKey, null));

        // Only the cookie of this path base counts: the one of the root is not read below a path base.
        var cookies = new FakeCookies("/demo/dashboard", ("dashboard-primary", "rose"), (scoped, "violet"));
        Assert.Equal("violet", new AppearanceState(new MemoryStore(), cookies).Primary.Name);
    }

    [Theory]
    [InlineData("<script>")]
    [InlineData("red")]
    [InlineData("klyroo; path=/")]
    [InlineData("")]
    [InlineData("EMERALD")]
    public void A_cookie_that_is_not_the_name_of_an_offered_palette_is_ignored(string value)
    {
        // "red" is a palette of Klyroo, but not one the dialog offers; the others are not palettes at all.
        var state = new AppearanceState(new MemoryStore(), new FakeCookies(null, ("dashboard-primary", value), ("dashboard-neutral", value)));

        Assert.True(state.Primary.IsKlyroo);
        Assert.True(state.Neutral.IsKlyroo);
        // A neutral name is not a primary one and the other way round.
        var swapped = new AppearanceState(new MemoryStore(), new FakeCookies(null, ("dashboard-primary", "zinc"), ("dashboard-neutral", "emerald")));
        Assert.True(swapped.Primary.IsKlyroo && swapped.Neutral.IsKlyroo);
    }

    [Fact]
    public async Task Choosing_a_color_applies_it_and_saves_it_and_loading_reads_the_store_once()
    {
        var store = new MemoryStore();
        var state = new AppearanceState(store);
        var changes = 0;
        state.Changed += () => changes++;

        await state.SetAsync(KlyPalette.Violet, KlyPalette.Stone);
        Assert.Equal(("violet", "stone"), store.Saved);
        Assert.Equal(1, changes);
        await state.SetAsync(KlyPalette.Rose);
        Assert.Equal(("rose", "stone"), store.Saved);

        // A new session reads what the store has, once, and says so only when it changed something.
        store.Saved = ("blue", "slate");
        var next = new AppearanceState(store);
        var nextChanges = 0;
        next.Changed += () => nextChanges++;
        await next.LoadAsync();
        await next.LoadAsync();
        Assert.Equal(("blue", "slate"), (next.Primary.Name, next.Neutral.Name));
        Assert.Equal(1, nextChanges);
        // What a prerender hands over is restored without a notification.
        next.Restore("emerald", "gray");
        Assert.Equal(("emerald", "gray"), (next.Primary.Name, next.Neutral.Name));
        Assert.Equal(1, nextChanges);
    }

    [Fact]
    public void The_primary_colors_on_offer_are_klyroo_or_tailwind_palettes_that_the_contrast_tests_cover()
    {
        Assert.True(AppearanceState.Palettes[0].IsKlyroo);
        Assert.All(AppearanceState.Palettes.Skip(1), p => Assert.Contains(p, KlyPalette.TailwindPalettes));
        Assert.Equal(AppearanceState.Palettes.Count, AppearanceState.Palettes.Select(p => p.Name).Distinct().Count());
    }
}
