using System.Globalization;
using Blux.Templates.Content;
using SaaS.Web.Site;

namespace Blux.Templates.Tests;

/// <summary>
/// The data code of the SaaS template (the library that reads the posts, the versions and the documentation out of the content folder, and the
/// pricing data) and the sample content it ships, which is read here the way the site reads it.
/// </summary>
public class SaasTemplateTests
{
    private static readonly string WebProject = Path.Combine(RepositoryRoot(), "templates", "saas", "src", "SaaS.Web");

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx")))
            {
                return dir.FullName;
            }
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }

    private static MarkdownSite Sample() =>
        MarkdownSite.Load(new ContentOptions { Root = Path.Combine(WebProject, "content"), WebRoot = Path.Combine(WebProject, "wwwroot") });

    // ---- The sample content

    [Fact]
    public void The_sample_content_has_no_broken_link_and_no_missing_image()
    {
        var site = Sample();

        Assert.Empty(site.BrokenLinks);
        Assert.Empty(site.MissingImages);
    }

    [Fact]
    public void The_sample_posts_are_listed_newest_first_with_an_author_tags_and_a_cover_that_exists()
    {
        var posts = Sample().Posts();

        Assert.Equal(5, posts.Count);
        Assert.Equal([.. posts.OrderByDescending(p => p.Date)], posts);
        Assert.Equal("tideline-2-status-pages-and-a-new-cli", posts[0].Name);
        Assert.All(posts, post =>
        {
            Assert.NotNull(post.Author);
            Assert.NotNull(post.Description);
            Assert.NotEmpty(post.Tags);
            Assert.NotNull(post.Image);
            Assert.True(File.Exists(Path.Combine(WebProject, "wwwroot", post.Image!.Replace('/', Path.DirectorySeparatorChar))), $"{post.Image} is not in wwwroot");
            // Relative, so that it follows a path base.
            Assert.False(post.Image.StartsWith('/'));
            Assert.NotEmpty(post.Authors);
        });
    }

    [Fact]
    public void The_sample_changelog_is_newest_first_and_each_version_has_a_page_of_its_own()
    {
        var site = Sample();
        var releases = site.Releases();

        Assert.Equal(["2.0", "1.9", "1.8", "1.7", "1.6"], releases.Select(r => r.Version));
        Assert.Equal("changelog/2-0", releases[0].Href);
        Assert.Equal("Major", releases[0].Badge);
        Assert.NotNull(site.Find("changelog/2-0"));
        // No heading inside a version: the list shows all of them on one page, and a repeated heading is a repeated id.
        Assert.All(releases, r => Assert.Empty(r.Page.Headings));
    }

    [Fact]
    public void The_documentation_is_the_pages_below_docs_in_the_order_of_its_navigation()
    {
        var site = Sample();

        Assert.Equal(10, site.DocsPages().Count);
        Assert.All(site.DocsPages(), page => Assert.StartsWith("docs/", page.Slug));
        Assert.Equal(["Getting started", "Guides", "Reference"], site.DocsNavigation().Select(item => item.Title));
        Assert.Equal("docs/getting-started/introduction", site.DocsPages()[0].Slug);
    }

    [Fact]
    public void The_previous_and_next_links_of_the_documentation_stop_at_its_ends_and_never_reach_a_post()
    {
        var site = Sample();
        var docs = site.DocsPages();

        Assert.Null(site.DocsSurround(docs[0]).Previous);
        Assert.Equal(docs[1].Href, site.DocsSurround(docs[0]).Next!.Href);
        var last = docs[^1];
        Assert.Null(site.DocsSurround(last).Next);
        Assert.Equal(docs[^2].Href, site.DocsSurround(last).Previous!.Href);
        // The whole site continues with the blog and the changelog after the docs: the docs' own neighbours do not.
        Assert.NotNull(site.Surround(last).Next);
        Assert.Equal((null, null), site.DocsSurround(site.Posts()[0].Page));
    }

    [Fact]
    public void The_tags_count_the_posts_and_the_most_used_come_first()
    {
        var tags = Sample().Tags();

        Assert.Contains(("engineering", 2), tags);
        Assert.Contains(("product", 3), tags);
        Assert.Equal("product", tags[0].Tag);
    }

    [Fact]
    public void A_post_has_a_reading_time_of_at_least_a_minute()
    {
        Assert.All(Sample().Posts(), post => Assert.InRange(post.ReadingMinutes, 1, 10));
    }

    // ---- The library, on small folders

    [Fact]
    public void A_post_without_a_date_stops_the_read_with_the_name_of_its_file()
    {
        using var folder = new ContentFolder().Page("blog/no-date.md", "No date");

        var error = Assert.Throws<ContentException>(() => folder.Load().Posts());

        Assert.Contains("blog/no-date.md", error.Message);
        Assert.Contains("yyyy-MM-dd", error.Message);
    }

    [Theory]
    [InlineData("2026-13-40")]
    [InlineData("12 March 2026")]
    [InlineData("2026-3-4")]
    public void A_date_that_is_not_yyyy_MM_dd_is_refused_with_the_name_of_the_file_and_the_value(string date)
    {
        using var folder = new ContentFolder().Page("changelog/1-0.md", "1.0", extra: $"date: \"{date}\"\n");

        var error = Assert.Throws<ContentException>(() => folder.Load().Releases());

        Assert.Contains("changelog/1-0.md", error.Message);
        Assert.Contains(date, error.Message);
    }

    [Fact]
    public void Tags_can_be_a_list_or_one_word_and_empty_values_are_dropped()
    {
        using var folder = new ContentFolder()
            .Page("blog/a.md", "A", extra: "date: 2026-01-02\ntags: [one, two, \"\"]\n")
            .Page("blog/b.md", "B", extra: "date: 2026-01-03\ntags: single\n")
            .Page("blog/c.md", "C", extra: "date: 2026-01-01\n");

        var posts = folder.Load().Posts();

        Assert.Equal(["B", "A", "C"], posts.Select(p => p.Title));
        Assert.Equal(["single"], posts[0].Tags);
        Assert.Equal(["one", "two"], posts[1].Tags);
        Assert.Empty(posts[2].Tags);
    }

    [Fact]
    public void Posts_on_the_same_day_are_ordered_by_title_and_posts_outside_blog_are_not_posts()
    {
        using var folder = new ContentFolder()
            .Page("blog/b.md", "Beta", extra: "date: 2026-01-02\n")
            .Page("blog/a.md", "Alpha", extra: "date: 2026-01-02\n")
            .Page("blog/nested/c.md", "Nested", extra: "date: 2026-01-02\n")
            .Page("docs/d.md", "Docs", extra: "date: not needed\n");

        var site = folder.Load();

        Assert.Equal(["Alpha", "Beta"], site.Posts().Select(p => p.Title));
        Assert.Equal(["Docs"], site.DocsPages().Select(p => p.Title));
    }

    [Fact]
    public void The_author_becomes_a_post_author_with_the_role_as_its_description()
    {
        using var folder = new ContentFolder().Page("blog/a.md", "A", extra: "date: 2026-01-02\nauthor: Noor Haddad\nauthorRole: Co-founder\n");

        var author = Assert.Single(folder.Load().Posts()[0].Authors);

        Assert.Equal("Noor Haddad", author.Name);
        Assert.Equal("Co-founder", author.Description);
    }

    [Fact]
    public void The_parts_are_worked_out_again_for_a_new_version_of_the_content()
    {
        using var folder = new ContentFolder().Page("blog/a.md", "A", extra: "date: 2026-01-02\n");
        var first = folder.Load();
        Assert.Same(first.Posts(), first.Posts());

        folder.Page("blog/b.md", "B", extra: "date: 2026-01-03\n");

        Assert.Equal(2, folder.Load().Posts().Count);
        Assert.Single(first.Posts());
    }

    // ---- Pricing

    [Fact]
    public void The_yearly_label_follows_the_prices()
    {
        Assert.Equal("Yearly, 2 months free", PricingInfo.YearlyLabel);
        Assert.Equal(2, PricingInfo.Plans.Single(p => p.Id == "team").MonthsFree);
        Assert.Equal(2, PricingInfo.Plans.Single(p => p.Id == "scale").MonthsFree);
        Assert.Equal(0, PricingInfo.Plans.Single(p => p.Id == "hobby").MonthsFree);
    }

    [Fact]
    public void A_paid_plan_costs_less_a_month_when_billed_yearly_and_says_what_it_charges()
    {
        var team = PricingInfo.Plans.Single(p => p.Id == "team");

        Assert.Equal("$29", team.Price(yearly: false));
        Assert.Equal("$24", team.Price(yearly: true));
        Assert.Equal("$288 billed once a year.", team.Terms(yearly: true));
        Assert.Equal("Billed monthly, cancel at any time.", team.Terms(yearly: false));
        Assert.All(PricingInfo.Plans.Where(p => p.MonthlyPrice > 0), p => Assert.True(p.YearlyMonthlyPrice < p.MonthlyPrice));
        var free = PricingInfo.Plans[0];
        Assert.Equal("$0", free.Price(yearly: true));
        Assert.Equal(free.Terms(yearly: false), free.Terms(yearly: true));
    }

    [Fact]
    public void One_plan_is_highlighted_and_the_comparison_table_has_one_value_per_plan_in_every_row()
    {
        Assert.Single(PricingInfo.Plans, p => p.Highlight);
        Assert.Equal(PricingInfo.Plans.Select(p => p.Id), PricingInfo.Tiers.Select(t => t.Id));
        foreach (var section in PricingInfo.Sections)
        {
            Assert.All(section.Features, feature => Assert.Equal(PricingInfo.Plans.Count, feature.Values.Length));
        }
    }

    [Fact]
    public void The_limits_of_the_comparison_table_are_the_ones_of_the_plans_and_of_the_docs()
    {
        var monitors = PricingInfo.Sections.SelectMany(s => s.Features).Single(f => f.Title == "Monitors");
        var users = PricingInfo.Sections.SelectMany(s => s.Features).Single(f => f.Title == "Users");
        var limits = Sample().Find("docs/reference/limits")!.Text;

        Assert.Equal(["5", "50", "Unlimited"], monitors.Values.Select(v => v?.ToString()));
        Assert.Equal(["1", "10", "50"], users.Values.Select(v => v?.ToString()));
        Assert.Contains("Unlimited", limits);
        Assert.Contains("Monitors 5 50 Unlimited", limits);
        Assert.Contains("Users 1 10 50", limits);
        Assert.Contains(PricingInfo.Plans.Single(p => p.Id == "team").Features, f => f == "50 monitors");
    }

    [Fact]
    public void Every_link_of_the_site_data_is_relative_or_on_a_reserved_example_domain()
    {
        var links = SiteInfo.Navigation.Select(i => i.Href!)
            .Concat(SiteInfo.FooterColumns.SelectMany(c => c.Links.Select(l => l.Href!)))
            .Concat(SiteInfo.HeroLinks.Select(l => l.Href!))
            .Concat(SiteInfo.CtaLinks.Select(l => l.Href!))
            .Concat(PricingInfo.Plans.Select(p => p.ButtonHref))
            .Concat([SiteInfo.ContactUrl, SiteInfo.StatusUrl, SiteInfo.GitHubSignInUrl, SiteInfo.GoogleSignInUrl]);

        Assert.All(links, link =>
        {
            var external = ContentUrl.IsExternal(link);
            Assert.True(!external || link.StartsWith("mailto:hello@example.com", StringComparison.Ordinal) || new Uri(link).Host == "example.com", link);
            Assert.False(link.StartsWith('/'), $"{link} is absolute: it would leave a path base");
        });
    }

    // ---- Plain pages

    [Fact]
    public void The_sample_legal_pages_are_plain_pages_and_are_in_the_search_index()
    {
        var site = Sample();

        Assert.Equal(["legal/terms", "legal/privacy"], site.PlainPages().Select(p => p.Slug));
        Assert.Contains(site.Search, page => page.Href == "legal/terms" && page.Group == "Legal");
        Assert.All(SiteInfo.FooterColumns.SelectMany(c => c.Links).Select(l => l.Href!).Where(h => h.StartsWith("legal/")), href => Assert.NotNull(site.Find(href)));
    }

    [Fact]
    public void A_page_outside_docs_blog_and_changelog_is_plain_and_so_is_a_file_in_a_folder_below_blog_or_changelog()
    {
        using var folder = new ContentFolder()
            .Page("docs/a.md", "Docs page")
            .Page("blog/post.md", "Post", extra: "date: 2026-01-02\n")
            .Page("blog/2026/nested.md", "Nested post")
            .Page("changelog/1-0.md", "1.0", extra: "date: 2026-01-02\n")
            .Page("changelog/old/0-9.md", "0.9")
            .Page("legal/terms.md", "Terms")
            .Page("about.md", "About");

        var site = folder.Load();

        Assert.Equal(["about", "blog/2026/nested", "changelog/old/0-9", "legal/terms"], site.PlainPages().Select(p => p.Slug).Order(StringComparer.Ordinal));
        Assert.Equal(["Post"], site.Posts().Select(p => p.Title));
        Assert.Equal(["1.0"], site.Releases().Select(r => r.Version));
        Assert.Equal(["docs/a"], site.DocsPages().Select(p => p.Slug));
    }

    // ---- Validation and covers

    [Fact]
    public void The_sample_content_passes_the_validation_with_no_warning()
    {
        Assert.Empty(Library.Validate(Sample(), Path.Combine(WebProject, "wwwroot")));
    }

    [Fact]
    public void Validate_stops_on_a_post_without_a_date_and_the_store_keeps_the_previous_content_when_a_reload_fails_it()
    {
        using var folder = new ContentFolder().Page("blog/a.md", "A", extra: "date: 2026-01-02\n");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root, Validate = site => Library.Validate(site, null) });
        var before = store.Site;

        folder.Page("blog/new.md", "New");

        Assert.False(store.Reload());
        Assert.Same(before, store.Site);
        Assert.Single(store.Site.Posts());

        folder.Page("blog/new.md", "New", extra: "date: 2026-01-03\n");

        Assert.True(store.Reload());
        Assert.Equal(["New", "A"], store.Site.Posts().Select(p => p.Title));
    }

    [Fact]
    public void The_first_read_of_a_post_without_a_date_stops_the_start_with_the_name_of_its_file()
    {
        using var folder = new ContentFolder().Page("blog/no-date.md", "No date");

        var error = Assert.Throws<ContentException>(() => new MarkdownStore(new ContentOptions { Root = folder.Root, Validate = site => Library.Validate(site, null) }));

        Assert.Contains("blog/no-date.md", error.Message);
    }

    [Theory]
    [InlineData("/img/blog/a.svg", "img/blog/a.svg")]
    [InlineData("img/blog/a.svg", "img/blog/a.svg")]
    [InlineData("./img/a.svg", "img/a.svg")]
    [InlineData("https://cdn.example.com/a.png", "https://cdn.example.com/a.png")]
    public void A_cover_is_rewritten_like_the_images_of_a_body_so_a_path_from_the_root_works_below_a_path_base(string written, string expected)
    {
        using var folder = new ContentFolder().Page("blog/a.md", "A", extra: $"date: 2026-01-02\nimage: {written}\n");

        Assert.Equal(expected, folder.Load().Posts()[0].Image);
    }

    [Theory]
    [InlineData("http://example.com/a.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("//example.com/a.png")]
    public void A_cover_outside_the_site_has_to_be_https(string written)
    {
        using var folder = new ContentFolder().Page("blog/a.md", "A", extra: $"date: 2026-01-02\nimage: \"{written}\"\n");

        var error = Assert.Throws<ContentException>(() => folder.Load().Posts());

        Assert.Contains("blog/a.md", error.Message);
        Assert.Contains("https://", error.Message);
    }

    [Fact]
    public void A_cover_with_no_file_in_the_web_root_is_a_warning_naming_the_post_and_an_external_cover_is_not_checked()
    {
        using var webRoot = new ContentFolder().Add("img/there.svg", "<svg/>");
        using var folder = new ContentFolder()
            .Page("blog/missing.md", "Missing", extra: "date: 2026-01-02\nimage: /img/gone.svg\n")
            .Page("blog/there.md", "There", extra: "date: 2026-01-02\nimage: img/there.svg\n")
            .Page("blog/remote.md", "Remote", extra: "date: 2026-01-02\nimage: https://cdn.example.com/a.png\n")
            .Page("blog/escape.md", "Escape", extra: "date: 2026-01-02\nimage: ../../etc/passwd\n")
            .Page("blog/none.md", "None", extra: "date: 2026-01-02\n");

        var warnings = Library.Validate(folder.Load(), webRoot.Root).ToList();

        Assert.Equal(2, warnings.Count);
        Assert.Contains(warnings, w => w.Contains("'blog/missing'") && w.Contains("img/gone.svg"));
        Assert.Contains(warnings, w => w.Contains("'blog/escape'"));
    }

    // ---- Dates

    [Theory]
    [InlineData("en-US", "March 4, 2026")]
    [InlineData("fr-FR", "4 mars 2026")]
    [InlineData("de-DE", "4. März 2026")]
    public void The_visible_date_is_the_long_date_of_the_culture_without_the_day_name(string culture, string expected)
    {
        Assert.Equal(expected, SiteDates.Format(new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero), CultureInfo.GetCultureInfo(culture)));
    }

    [Fact]
    public void The_date_in_russian_is_not_the_english_pattern_with_russian_month_names()
    {
        var text = SiteDates.Format(new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero), CultureInfo.GetCultureInfo("ru-RU"));

        // "4 марта 2026 г.": the day first, as the culture writes it, not "марта 4, 2026".
        Assert.StartsWith("4 марта 2026", text);
    }

    [Fact]
    public void The_datetime_attribute_is_the_same_in_every_culture()
    {
        var original = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            Assert.Equal("2026-03-04", SiteDates.Iso(new DateTimeOffset(2026, 3, 4, 0, 0, 0, TimeSpan.Zero)));
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = original;
        }
    }

    [Theory]
    [InlineData("en-GB", "en", "en-GB")]
    [InlineData("en-US", "fr", "fr")]
    [InlineData("fr-CA", "fr", "fr-CA")]
    public void The_culture_of_the_date_is_the_regional_one_when_it_has_the_language_of_the_locale_otherwise_the_one_of_the_locale(string current, string ui, string expected)
    {
        var original = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(current);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(ui);
            Assert.Equal(expected, SiteDates.Culture().Name);
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = original;
        }
    }
}
