using Lux.Templates.Content;
using Lux.UI;

namespace Lux.Templates.Tests;

public class FrontMatterTests
{
    [Fact]
    public void All_keys_are_read_and_the_body_is_what_follows()
    {
        var (data, body) = FrontMatterParser.Parse("---\ntitle: Install\ndescription: Put it on a machine.\norder: 3\nnavigationTitle: Setup\nicon: ph:download\ndraft: true\n---\n## Body\n", "a.md");

        Assert.Equal("Install", data.Title);
        Assert.Equal("Put it on a machine.", data.Description);
        Assert.Equal(3, data.Order);
        Assert.Equal("Setup", data.NavigationTitle);
        Assert.Equal("ph:download", data.Icon);
        Assert.True(data.Draft);
        Assert.Equal("## Body\n", body);
    }

    [Theory]
    [InlineData("Just text.\n")]
    [InlineData("")]
    [InlineData("\n---\ntitle: x\n---\n")]
    public void A_file_that_does_not_start_with_front_matter_has_none(string text)
    {
        var (data, body) = FrontMatterParser.Parse(text, "a.md");

        Assert.Null(data.Title);
        Assert.False(data.Draft);
        Assert.Equal(text, body);
    }

    [Fact]
    public void Windows_line_endings_a_byte_order_mark_and_an_empty_block_are_fine()
    {
        var (data, body) = FrontMatterParser.Parse("\uFEFF---\r\ntitle: A title\r\n---\r\nBody\r\n", "a.md");
        Assert.Equal("A title", data.Title);
        Assert.Equal("Body\n", body);

        var (empty, rest) = FrontMatterParser.Parse("---\n---\nText", "a.md");
        Assert.Null(empty.Title);
        Assert.Equal("Text", rest);
    }

    [Fact]
    public void Unknown_keys_are_ignored()
    {
        var (data, _) = FrontMatterParser.Parse("---\ntitle: A\nauthor: me\n---\n", "a.md");

        Assert.Equal("A", data.Title);
    }

    [Fact]
    public void Every_key_is_kept_as_written_in_Meta_for_the_data_a_site_adds_of_its_own()
    {
        var (data, _) = FrontMatterParser.Parse("---\ntitle: A\ndate: 2026-03-04\nauthor: Maya\ntags: [news, product]\nfeatured: true\n---\n", "a.md");

        Assert.Equal("A", data.Meta["title"]);
        Assert.Equal("2026-03-04", data.Meta["date"]);
        Assert.Equal("Maya", data.Meta["author"]);
        Assert.Equal(["news", "product"], ((IEnumerable<object>)data.Meta["tags"]!).Select(t => t.ToString()));
        Assert.Equal("true", data.Meta["featured"]);
    }

    [Fact]
    public void A_file_without_front_matter_has_an_empty_Meta()
    {
        var (data, _) = FrontMatterParser.Parse("Text", "a.md");

        Assert.Empty(data.Meta);
    }

    [Fact]
    public void A_block_that_is_never_closed_names_the_file()
    {
        var error = Assert.Throws<ContentException>(() => FrontMatterParser.Parse("---\ntitle: A\n", "guides/a.md"));

        Assert.Contains("guides/a.md", error.Message);
        Assert.Contains("not closed", error.Message);
    }

    [Theory]
    [InlineData("---\ntitle: [unclosed\n---\n")]
    [InlineData("---\norder: not a number\n---\n")]
    public void Invalid_yaml_names_the_file(string text)
    {
        var error = Assert.Throws<ContentException>(() => FrontMatterParser.Parse(text, "guides/a.md"));

        Assert.StartsWith("guides/a.md: invalid YAML", error.Message);
    }

    [Fact]
    public void A_directory_file_has_a_title_an_order_and_an_icon()
    {
        var meta = FrontMatterParser.ParseDirectory("title: Getting started\norder: 1\nicon: ph:rocket\n", "_dir.yml");

        Assert.Equal(("Getting started", 1, "ph:rocket"), (meta.Title, meta.Order, meta.Icon));
    }
}

public class MarkdownSiteTests
{
    private static string[] Titles(IEnumerable<LxContentNavigationItem> items) => [.. items.Select(i => i.Title)];

    [Fact]
    public void A_page_carries_the_front_matter_keys_the_site_added_and_a_page_built_by_hand_has_none()
    {
        using var folder = new ContentFolder().Page("blog/post.md", "Post", extra: "date: 2026-03-04\ntags: [news]\n");
        var page = folder.Load().Find("blog/post")!;

        Assert.Equal("2026-03-04", page.Data["date"]);
        Assert.Equal("Post", page.Data["title"]);
        Assert.Empty(new MarkdownPage("p", "", false, "P", null, null, null, null, false, "", [], "", "p.md").Data);
    }

    [Fact]
    public void The_navigation_is_folders_as_groups_and_pages_as_links()
    {
        using var folder = new ContentFolder()
            .Page("start/install.md", "Install", 1)
            .Page("start/first-run.md", "First run", 2)
            .Add("start/_dir.yml", "title: Getting started\norder: 1\nicon: ph:rocket\n")
            .Page("guides/config.md", "Configuration");
        var site = folder.Load();

        var start = site.Navigation[0];
        Assert.Equal("Getting started", start.Title);
        Assert.Equal("ph:rocket", start.Icon);
        Assert.Null(start.Href);
        Assert.Equal(["Install", "First run"], Titles(start.Children!));
        Assert.Equal(["start/install", "start/first-run"], start.Children!.Select(c => c.Href));
        Assert.All(start.Children!, c => Assert.True(c.Exact));
        // A folder without _dir.yml is named after its folder.
        Assert.Equal("Guides", site.Navigation[1].Title);
    }

    [Fact]
    public void Pages_and_folders_are_ordered_by_order_then_title_and_the_ones_without_an_order_come_last()
    {
        using var folder = new ContentFolder()
            .Page("zeta.md", "Zeta", 1)
            .Page("alpha.md", "Alpha")
            .Page("beta.md", "beta")
            .Page("late.md", "Late", 99)
            .Page("mid/one.md", "One")
            .Add("mid/_dir.yml", "title: Middle\norder: 2\n")
            .Page("other/two.md", "Two")
            .Add("other/_dir.yml", "title: Aardvark\n");

        var site = folder.Load();

        // Order 1, 2 (the folder), 99, then no order: Aardvark (the folder), Alpha and beta by title, ignoring case.
        Assert.Equal(["Zeta", "Middle", "Late", "Aardvark", "Alpha", "beta"], Titles(site.Navigation));
    }

    [Fact]
    public void The_navigation_title_replaces_the_title_in_the_link_only()
    {
        using var folder = new ContentFolder().Page("a.md", "A very long title for a page", extra: "navigationTitle: Short\n");

        var site = folder.Load();

        Assert.Equal("Short", site.Navigation[0].Title);
        Assert.Equal("A very long title for a page", site.Pages[0].Title);
    }

    [Fact]
    public void A_title_without_front_matter_comes_from_the_file_name()
    {
        using var folder = new ContentFolder().Add("getting-started/first_run.md", "Text.\n");

        var site = folder.Load();

        Assert.Equal("First run", site.Pages[0].Title);
        Assert.Equal("Getting started", site.Navigation[0].Title);
    }

    [Fact]
    public void An_index_file_is_the_page_of_its_folder_and_comes_first()
    {
        using var folder = new ContentFolder()
            .Page("guides/index.md", "Guides overview")
            .Page("guides/a.md", "A", 1)
            .Add("index.md", "---\ntitle: Home\n---\nThe site writes its own home page.\n");

        var site = folder.Load();

        Assert.Equal(["guides", "guides/a"], site.Pages.Select(p => p.Slug));
        Assert.Equal(["Guides overview", "A"], Titles(site.Navigation[0].Children!));
        Assert.NotNull(site.Find("guides"));
    }

    [Fact]
    public void A_folder_whose_pages_are_all_drafts_is_not_listed_and_drafts_can_be_included()
    {
        using var folder = new ContentFolder()
            .Page("a.md", "A")
            .Page("secret/b.md", "B", extra: "draft: true\n");

        var site = folder.Load();
        var withDrafts = folder.Load(drafts: true);

        Assert.Equal(["A"], Titles(site.Navigation));
        Assert.Null(site.Find("secret/b"));
        Assert.Equal(["A", "Secret"], Titles(withDrafts.Navigation));
        Assert.True(withDrafts.Find("secret/b")!.Draft);
    }

    [Fact]
    public void Previous_and_next_follow_the_navigation_across_folders()
    {
        using var folder = new ContentFolder()
            .Page("one/a.md", "A", 1, extra: "description: First.\n")
            .Page("one/b.md", "B", 2)
            .Page("two/c.md", "C", 1)
            .Add("one/_dir.yml", "order: 1\n")
            .Add("two/_dir.yml", "order: 2\n");
        var site = folder.Load();

        var (previousA, nextA) = site.Surround(site.Find("one/a")!);
        var (previousB, nextB) = site.Surround(site.Find("one/b")!);
        var (previousC, nextC) = site.Surround(site.Find("two/c")!);

        Assert.Null(previousA);
        Assert.Equal(("B", "one/b"), (nextA!.Title, nextA.Href));
        Assert.Equal("A", previousB!.Title);
        Assert.Equal("First.", previousB.Description);
        Assert.Equal("C", nextB!.Title);
        Assert.Equal("B", previousC!.Title);
        Assert.Null(nextC);
    }

    [Fact]
    public void Find_ignores_case_and_surrounding_slashes()
    {
        using var folder = new ContentFolder().Page("Guides/Setup.md", "Setup");
        var site = folder.Load();

        Assert.Equal("guides/setup", site.Find("/Guides/SETUP/")!.Slug);
        Assert.Null(site.Find("guides"));
        Assert.Null(site.Find(null));
    }

    [Fact]
    public void The_toc_of_a_page_lists_its_h2_and_h3_with_the_ids_of_the_html()
    {
        using var folder = new ContentFolder().Page("a.md", "A", body: "## Install\n\n### From a package\n\n#### Too deep\n\n## Use");
        var page = folder.Load().Pages[0];

        Assert.Equal(["install", "from-a-package", "use"], page.Toc.Select(t => t.Id));
        Assert.All(page.Toc, t => Assert.Contains($"id=\"{t.Id}\"", page.Html));
    }

    [Fact]
    public void The_search_index_has_titles_headings_groups_and_the_start_of_the_text()
    {
        using var folder = new ContentFolder()
            .Page("start/install.md", "Install", body: "## From a package\n\nRun the installer and wait. " + string.Join(' ', Enumerable.Repeat("filler", 600)) + " tail", extra: "description: Put it on a machine.\n")
            .Add("start/_dir.yml", "title: Getting started\n")
            .Page("about.md", "About");
        var site = folder.Load();

        var install = site.Search.Single(p => p.Title == "Install");
        Assert.Equal("start/install", install.Href);
        Assert.Equal("Getting started", install.Group);
        Assert.Equal("Put it on a machine.", install.Description);
        Assert.Equal(("From a package", "from-a-package"), (install.Headings![0].Text, install.Headings[0].Id));
        Assert.StartsWith("From a package Run the installer and wait.", install.Keywords);
        Assert.InRange(install.Keywords!.Length, 1500, 2000);
        Assert.DoesNotContain("tail", install.Keywords);
        Assert.False(install.Keywords!.EndsWith("fill", StringComparison.Ordinal), "the snippet is cut at a word");
        Assert.Null(site.Search.Single(p => p.Title == "About").Group);
    }

    [Fact]
    public void A_search_text_limit_of_zero_leaves_the_text_out()
    {
        using var folder = new ContentFolder().Page("a.md", "A", body: "Some text.");

        var site = MarkdownSite.Load(new ContentOptions { Root = folder.Root, SearchTextLimit = 0 });

        Assert.Null(site.Search[0].Keywords);
    }

    [Fact]
    public void Two_files_with_the_same_address_are_refused_with_both_names()
    {
        using var folder = new ContentFolder().Page("Setup.md", "A").Page("setup.md", "B");

        var error = Assert.Throws<ContentException>(() => folder.Load());

        Assert.Contains("Setup.md", error.Message);
        Assert.Contains("setup.md", error.Message);
        Assert.Contains("'setup'", error.Message);
    }

    [Fact]
    public void A_bad_front_matter_stops_the_load_and_names_the_file()
    {
        using var folder = new ContentFolder().Add("guides/bad.md", "---\ntitle: [x\n---\n");

        var error = Assert.Throws<ContentException>(() => folder.Load());

        Assert.StartsWith("guides/bad.md", error.Message);
    }

    [Fact]
    public void A_missing_folder_is_refused()
    {
        var error = Assert.Throws<ContentException>(() => MarkdownSite.Load(new ContentOptions { Root = Path.Combine(Path.GetTempPath(), "lux-no-such-folder") }));

        Assert.Contains("does not exist", error.Message);
    }

    [Fact]
    public void Links_to_pages_that_do_not_exist_are_reported()
    {
        using var folder = new ContentFolder()
            .Page("a.md", "A", body: "[ok](b) [bad](/nowhere) [home](/) [x](https://example.com)")
            .Page("b.md", "B");

        var site = folder.Load();

        Assert.Equal([new BrokenLink("a", "nowhere")], site.BrokenLinks);
    }

    [Fact]
    public void The_rendered_html_of_a_page_resolves_links_from_its_folder()
    {
        using var folder = new ContentFolder().Page("start/a.md", "A", body: "[b](b.md)").Page("start/b.md", "B");

        Assert.Contains("href=\"start/b\"", folder.Load().Find("start/a")!.Html);
    }
}

public class MissingImageTests
{
    [Fact]
    public void An_image_with_no_file_in_the_web_root_is_reported_and_one_with_a_file_is_not()
    {
        using var folder = new ContentFolder()
            .Page("guides/a.md", "A", body: "![ok](/img/ok.png) ![next to the page](chart.png) ![gone](/img/gone.png) ![outside](/../secret.png) ![web](https://example.com/x.png)");
        using var web = new ContentFolder().Add("img/ok.png", "x").Add("guides/chart.png.txt", "x");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(web.Root)!, "secret.png"), "x");
        try
        {
            var site = MarkdownSite.Load(new ContentOptions { Root = folder.Root, WebRoot = web.Root });

            // An image next to its page is not served, so it is reported; a path that climbs out of the web root never counts.
            Assert.Equal(["guides/chart.png", "img/gone.png", "secret.png"], site.MissingImages.Select(i => i.Path).Order());
            Assert.All(site.MissingImages, i => Assert.Equal("guides/a", i.FromSlug));
        }
        finally
        {
            File.Delete(Path.Combine(Path.GetDirectoryName(web.Root)!, "secret.png"));
        }
    }

    [Fact]
    public void Without_a_web_root_images_are_not_checked()
    {
        using var folder = new ContentFolder().Page("a.md", "A", body: "![x](/img/gone.png)");

        Assert.Empty(folder.Load().MissingImages);
    }
}

public class MarkdownStoreTests
{
    [Fact]
    public void A_handler_of_Reloaded_that_throws_does_not_undo_the_reload()
    {
        using var folder = new ContentFolder().Page("a.md", "First");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root });
        store.Reloaded += () => throw new InvalidOperationException("boom");
        folder.Page("a.md", "Second");

        Assert.True(store.Reload());
        Assert.Equal("Second", store.Site.Pages[0].Title);
    }

    [Fact]
    public void Any_failure_while_reading_keeps_the_last_good_content()
    {
        using var folder = new ContentFolder().Page("a.md", "First");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root });
        // The folder goes away: not a ContentException but a missing folder, and the timer thread must not die of it.
        Directory.Delete(folder.Root, recursive: true);

        Assert.False(store.Reload());
        Assert.Equal("First", store.Site.Pages[0].Title);
    }

    [Fact]
    public async Task A_changed_file_is_read_again_and_a_broken_one_keeps_the_previous_content()
    {
        using var folder = new ContentFolder().Page("a.md", "First");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root, Watch = true });
        var reloaded = new SemaphoreSlim(0);
        store.Reloaded += () => reloaded.Release();
        Assert.Equal("First", store.Site.Pages[0].Title);

        folder.Page("a.md", "Second");
        Assert.True(await reloaded.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken), "the store did not reload");
        Assert.Equal("Second", store.Site.Pages[0].Title);

        folder.Add("a.md", "---\ntitle: [broken\n---\n");
        await Task.Delay(1500, TestContext.Current.CancellationToken);
        Assert.Equal("Second", store.Site.Pages[0].Title);
        Assert.False(store.Reload());
    }

    [Fact]
    public void Without_watch_nothing_changes_until_reload_is_called()
    {
        using var folder = new ContentFolder().Page("a.md", "First");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root });

        folder.Page("a.md", "Second");

        Assert.Equal("First", store.Site.Pages[0].Title);
        Assert.True(store.Reload());
        Assert.Equal("Second", store.Site.Pages[0].Title);
    }

    [Fact]
    public void A_bad_folder_stops_the_start()
    {
        Assert.Throws<ContentException>(() => new MarkdownStore(new ContentOptions { Root = Path.Combine(Path.GetTempPath(), "lux-no-such-folder") }));
    }
}
