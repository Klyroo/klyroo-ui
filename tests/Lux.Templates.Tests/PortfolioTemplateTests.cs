using System.ComponentModel.DataAnnotations;
using Lux.Templates.Content;
using Portfolio.Web.Site;

namespace Lux.Templates.Tests;

/// <summary>
/// The data code of the Portfolio template (the library that reads the projects and the articles out of the content folder, the resume, the talks and
/// the contact message) and the sample content it ships, which is read here the way the site reads it.
/// </summary>
public class PortfolioTemplateTests
{
    private static readonly string TemplateRoot = Path.Combine(RepositoryRoot(), "templates", "portfolio");
    private static readonly string WebProject = Path.Combine(TemplateRoot, "src", "Portfolio.Web");

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Lux.UI.slnx")))
            {
                return dir.FullName;
            }
        }
        throw new DirectoryNotFoundException("Lux.UI.slnx not found above the test output directory.");
    }

    private static MarkdownSite Sample() =>
        MarkdownSite.Load(new ContentOptions { Root = Path.Combine(WebProject, "content"), WebRoot = Path.Combine(WebProject, "wwwroot") });

    // ---- The sample content

    [Fact]
    public void The_sample_content_has_no_broken_link_no_missing_image_and_passes_the_validation_with_no_warning()
    {
        var site = Sample();

        Assert.Empty(site.BrokenLinks);
        Assert.Empty(site.MissingImages);
        Assert.Empty(Library.Validate(site, Path.Combine(WebProject, "wwwroot")));
    }

    [Fact]
    public void The_sample_projects_are_listed_newest_year_first_with_a_cover_that_exists_and_links_that_are_safe()
    {
        var projects = Sample().Projects();

        Assert.Equal(["harbor-atlas", "quire", "fieldnote", "kiln", "plume"], projects.Select(p => p.Name));
        Assert.Equal([.. projects.OrderByDescending(p => p.Year)], projects);
        Assert.All(projects, project =>
        {
            Assert.NotNull(project.Description);
            Assert.NotNull(project.Role);
            Assert.NotNull(project.Client);
            Assert.NotEmpty(project.Stack);
            Assert.NotNull(project.Image);
            Assert.NotEmpty(project.ImageAlt!);
            Assert.True(File.Exists(Path.Combine(WebProject, "wwwroot", project.Image!.Replace('/', Path.DirectorySeparatorChar))), $"{project.Image} is not in wwwroot");
            // Relative, so that it follows a path base.
            Assert.False(project.Image.StartsWith('/'));
            Assert.Equal($"work/{project.Name}", project.Href);
            Assert.All(project.Links.Where(l => ContentUrl.IsExternal(l.Url)), link => Assert.Equal("example.com", new Uri(link.Url).Host));
        });
    }

    [Fact]
    public void A_link_of_a_sample_project_to_an_article_is_the_address_of_that_article()
    {
        var site = Sample();
        var link = site.Projects().Single(p => p.Name == "quire").Links.Single(l => l.Label == "How I audit");

        Assert.Equal("writing/the-audit-i-run-before-writing-a-token", link.Url);
        Assert.NotNull(site.Find(link.Url));
    }

    [Fact]
    public void Three_projects_are_featured_and_they_are_the_most_recent_ones_that_are()
    {
        var site = Sample();

        Assert.Equal(["harbor-atlas", "quire", "fieldnote"], site.FeaturedProjects().Select(p => p.Name));
        Assert.All(site.FeaturedProjects(), p => Assert.True(p.Featured));
    }

    [Fact]
    public void The_sample_articles_are_newest_first_with_tags_a_reading_time_and_a_cover_that_exists()
    {
        var articles = Sample().Articles();

        Assert.Equal(5, articles.Count);
        Assert.Equal([.. articles.OrderByDescending(a => a.Date)], articles);
        Assert.Equal("a-focus-ring-you-can-see-on-every-background", articles[0].Name);
        Assert.All(articles, article =>
        {
            Assert.NotNull(article.Description);
            Assert.NotEmpty(article.Tags);
            Assert.InRange(article.ReadingMinutes, 1, 10);
            Assert.True(File.Exists(Path.Combine(WebProject, "wwwroot", article.Image!.Replace('/', Path.DirectorySeparatorChar))), $"{article.Image} is not in wwwroot");
            Assert.StartsWith("writing/", article.Href);
        });
    }

    [Fact]
    public void The_tags_count_the_articles_and_the_most_used_come_first()
    {
        var tags = Sample().Tags();

        Assert.Contains(("css", 2), tags);
        Assert.Contains(("design-systems", 2), tags);
        Assert.Contains(("practice", 1), tags);
        Assert.Equal(2, tags[0].Count);
    }

    [Fact]
    public void The_about_page_and_the_uses_page_are_what_the_site_expects()
    {
        var site = Sample();

        Assert.NotNull(site.About());
        Assert.Equal("about", site.About()!.Slug);
        // The about page has its own route: it is not a plain page, so it is not served twice.
        Assert.Equal(["uses"], site.PlainPages().Select(p => p.Slug));
        Assert.Contains(site.Search, page => page.Href == "about");
        Assert.Contains(site.Search, page => page.Href == "work/quire" && page.Group == "Work");
        Assert.Contains(site.Search, page => page.Href == "uses");
        Assert.All(SiteInfo.FooterColumns.SelectMany(c => c.Links).Select(l => l.Href!).Where(h => !h.StartsWith("http")), href =>
        {
            var page = href is "work" or "writing" or "speaking" or "contact" || site.Find(href) is not null;
            Assert.True(page, $"{href} leads nowhere");
        });
    }

    [Fact]
    public void The_words_of_the_site_never_use_the_template_name_because_a_rename_would_rewrite_them()
    {
        // rename.cs replaces the name of the template (in its three spellings) in every file: the project name in a command is meant to change, a word in a
        // sentence is not. The names of the namespace and of the project are the only occurrences there may be.
        var files = Directory.EnumerateFiles(Path.Combine(WebProject, "content"), "*.md", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(WebProject, "Site"), "*.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(WebProject, "Components"), "*.razor", SearchOption.AllDirectories));
        foreach (var file in files)
        {
            var text = File.ReadAllText(file).Replace("Portfolio.Web", "");
            Assert.False(text.Contains("portfolio", StringComparison.OrdinalIgnoreCase), $"{Path.GetRelativePath(TemplateRoot, file)} uses the word of the template name");
        }
    }

    // ---- The library, on small folders

    [Fact]
    public void A_project_without_a_year_stops_the_read_with_the_name_of_its_file()
    {
        using var folder = new ContentFolder().Page("work/no-year.md", "No year");

        var error = Assert.Throws<ContentException>(() => folder.Load().Projects());

        Assert.Contains("work/no-year.md", error.Message);
        Assert.Contains("year", error.Message);
    }

    [Theory]
    [InlineData("25")]
    [InlineData("twenty")]
    [InlineData("2025-01-01")]
    [InlineData("1900")]
    public void A_year_that_is_not_four_digits_is_refused_with_the_name_of_the_file_and_the_value(string year)
    {
        using var folder = new ContentFolder().Page("work/a.md", "A", extra: $"year: \"{year}\"\n");

        var error = Assert.Throws<ContentException>(() => folder.Load().Projects());

        Assert.Contains("work/a.md", error.Message);
        Assert.Contains(year, error.Message);
    }

    [Fact]
    public void An_article_without_a_valid_date_is_refused_with_the_name_of_the_file()
    {
        using var missing = new ContentFolder().Page("writing/a.md", "A");
        using var invalid = new ContentFolder().Page("writing/b.md", "B", extra: "date: 2026-3-4\n");

        Assert.Contains("writing/a.md", Assert.Throws<ContentException>(() => missing.Load().Articles()).Message);
        var error = Assert.Throws<ContentException>(() => invalid.Load().Articles());
        Assert.Contains("writing/b.md", error.Message);
        Assert.Contains("2026-3-4", error.Message);
    }

    [Fact]
    public void Projects_of_the_same_year_are_ordered_by_order_then_title_and_the_stack_can_be_a_list_or_one_word()
    {
        using var folder = new ContentFolder()
            .Page("work/c.md", "Charlie", extra: "year: 2024\nstack: [One, Two, \"\"]\n")
            .Page("work/a.md", "Alpha", order: 2, extra: "year: 2024\nstack: Single\n")
            .Page("work/b.md", "Bravo", order: 1, extra: "year: 2024\n")
            .Page("work/old.md", "Old", extra: "year: 2020\n");

        var projects = folder.Load().Projects();

        Assert.Equal(["Bravo", "Alpha", "Charlie", "Old"], projects.Select(p => p.Title));
        Assert.Equal(["Single"], projects[1].Stack);
        Assert.Equal(["One", "Two"], projects[2].Stack);
        Assert.Empty(projects[0].Stack);
        Assert.All(projects, p => Assert.False(p.Featured));
    }

    [Fact]
    public void The_links_of_a_project_need_a_label_and_a_url_and_only_known_schemes_are_links()
    {
        using var good = new ContentFolder().Page("work/a.md", "A", extra: "year: 2024\nlinks:\n  - label: Live\n    url: https://example.com/a\n  - label: Mail\n    url: mailto:a@example.com\n");
        var links = good.Load().Projects()[0].Links;
        Assert.Equal([new ProjectLink("Live", "https://example.com/a"), new ProjectLink("Mail", "mailto:a@example.com")], links);

        foreach (var yaml in new[]
        {
            "links:\n  - label: Only a label\n",
            "links:\n  - url: https://example.com\n",
            "links:\n  - label: Bad\n    url: \"javascript:alert(1)\"\n",
            "links:\n  - just text\n",
            "links: nope\n",
        })
        {
            using var bad = new ContentFolder().Page("work/a.md", "A", extra: "year: 2024\n" + yaml);
            var error = Assert.Throws<ContentException>(() => bad.Load().Projects());
            Assert.Contains("work/a.md", error.Message);
        }
    }

    [Fact]
    public void An_address_of_the_site_in_the_links_of_a_project_is_a_page_address_and_an_external_one_is_kept()
    {
        using var folder = new ContentFolder()
            .Page("work/a.md", "A", extra: "year: 2024\nlinks:\n  - label: Absolute\n    url: /uses\n  - label: File\n    url: kiln.md\n  - label: Parent\n    url: ../writing/post.md#top\n  - label: Route\n    url: /contact\n  - label: Web\n    url: https://example.com/a?b=1#c\n  - label: Mail\n    url: mailto:a@example.com\n")
            .Page("work/kiln.md", "Kiln", extra: "year: 2023\n");

        var links = folder.Load().Projects().Single(p => p.Name == "a").Links;

        // Relative to the <base href>, so that they follow a path base; the same rewrite as the links of a body.
        Assert.Equal(["uses", "work/kiln", "writing/post#top", "contact", "https://example.com/a?b=1#c", "mailto:a@example.com"], links.Select(l => l.Url));
    }

    [Fact]
    public void The_link_of_a_project_to_a_page_is_right_below_a_path_base_in_the_page_it_renders()
    {
        // Resolved the way the browser does with <base href="/site/">: nothing starts with a slash, so nothing leaves the prefix.
        using var folder = new ContentFolder().Page("work/a.md", "A", extra: "year: 2024\nlinks:\n  - label: Uses\n    url: /uses\n  - label: Kiln\n    url: kiln.md\n").Page("work/kiln.md", "Kiln", extra: "year: 2023\n");
        var baseUri = new Uri("https://example.com/site/");

        Assert.All(folder.Load().Projects().Single(p => p.Name == "a").Links, link =>
            Assert.StartsWith("https://example.com/site/", new Uri(baseUri, link.Url).ToString()));
    }

    [Theory]
    [InlineData("contact.md")]
    [InlineData("work.md")]
    [InlineData("writing.md")]
    [InlineData("speaking.md")]
    [InlineData("error.md")]
    [InlineData("not-found.md")]
    [InlineData("Contact.md")]
    public void A_plain_page_named_after_the_route_of_a_page_of_the_site_is_refused_with_the_file_and_the_route(string file)
    {
        using var folder = new ContentFolder().Page("uses.md", "Uses").Page(file, "Shadowed");

        var error = Assert.Throws<ContentException>(() => Library.Validate(folder.Load(), null));

        Assert.Contains(file, error.Message);
        Assert.Contains("route of a page of the site", error.Message);
        Assert.Contains(Path.GetFileNameWithoutExtension(file).ToLowerInvariant(), error.Message.ToLowerInvariant());
    }

    [Fact]
    public void A_page_that_only_looks_like_a_route_is_fine_and_the_store_keeps_the_previous_content_when_a_reload_adds_one_that_is_not()
    {
        using var folder = new ContentFolder().Page("work/contact.md", "A project called contact", extra: "year: 2024\n").Page("contact/thanks.md", "Thanks").Page("uses.md", "Uses");
        Assert.Empty(Library.Validate(folder.Load(), null));
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root, Validate = site => Library.Validate(site, null) });
        var before = store.Site;

        folder.Page("contact.md", "Shadowed");

        Assert.False(store.Reload());
        Assert.Same(before, store.Site);
        // It is not in the search of the site that is served.
        Assert.DoesNotContain(store.Site.Search, page => page.Href == "contact");
    }

    [Fact]
    public void A_cover_must_be_https_when_it_leaves_the_site_and_one_that_has_no_file_is_a_warning()
    {
        using var http = new ContentFolder().Page("work/a.md", "A", extra: "year: 2024\nimage: http://example.com/a.png\n");
        Assert.Contains("https://", Assert.Throws<ContentException>(() => http.Load().Projects()).Message);

        using var folder = new ContentFolder()
            .Page("work/a.md", "A", extra: "year: 2024\nimage: /img/work/nope.svg\n")
            .Page("writing/b.md", "B", extra: "date: 2026-01-01\nimage: img/writing/nope.svg\n");
        var webRoot = Directory.CreateTempSubdirectory("lux-web-").FullName;
        try
        {
            var warnings = Library.Validate(folder.Load(), webRoot).ToList();

            Assert.Equal(2, warnings.Count);
            Assert.Contains(warnings, w => w.Contains("project 'work/a'"));
            Assert.Contains(warnings, w => w.Contains("article 'writing/b'"));
        }
        finally
        {
            Directory.Delete(webRoot, recursive: true);
        }
    }

    [Fact]
    public void A_page_outside_work_and_writing_is_plain_and_so_is_a_file_in_a_folder_below_them()
    {
        using var folder = new ContentFolder()
            .Page("work/p.md", "Project", extra: "year: 2024\n")
            .Page("work/archive/old.md", "Old project")
            .Page("writing/a.md", "Article", extra: "date: 2026-01-02\n")
            .Page("writing/2026/nested.md", "Nested")
            .Page("about.md", "About")
            .Page("uses.md", "Uses")
            .Page("legal/terms.md", "Terms");

        var site = folder.Load();

        Assert.Equal(["legal/terms", "uses", "work/archive/old", "writing/2026/nested"], site.PlainPages().Select(p => p.Slug).Order(StringComparer.Ordinal));
        Assert.Equal(["Project"], site.Projects().Select(p => p.Title));
        Assert.Equal(["Article"], site.Articles().Select(a => a.Title));
        Assert.Equal("About", site.About()!.Title);
    }

    [Fact]
    public void The_store_keeps_the_previous_content_when_a_reload_fails_the_validation()
    {
        using var folder = new ContentFolder().Page("work/a.md", "A", extra: "year: 2024\n");
        using var store = new MarkdownStore(new ContentOptions { Root = folder.Root, Validate = site => Library.Validate(site, null) });
        var before = store.Site;

        folder.Page("work/new.md", "New");

        Assert.False(store.Reload());
        Assert.Same(before, store.Site);
        Assert.Single(store.Site.Projects());

        folder.Page("work/new.md", "New", extra: "year: 2025\n");

        Assert.True(store.Reload());
        Assert.Equal(["New", "A"], store.Site.Projects().Select(p => p.Title));
    }

    // ---- Resume, talks, links

    [Fact]
    public void The_timeline_has_one_item_per_job_in_the_order_they_are_written()
    {
        var items = Resume.Timeline();

        Assert.Equal(Resume.Experiences.Count, items.Count);
        Assert.Equal("2024 to now", items[0].Date);
        Assert.Contains("Calloway Studio", items[0].Title);
        Assert.All(items, item => Assert.NotNull(item.Description));
        Assert.All(Resume.Skills, group => Assert.NotEmpty(group.Items));
    }

    [Fact]
    public void The_talks_have_a_title_an_event_and_materials_on_a_reserved_example_domain()
    {
        Assert.NotEmpty(Talks.All);
        Assert.Equal([.. Talks.All.OrderByDescending(t => t.Date)], Talks.All);
        Assert.Contains(Talks.All, t => t.Kind == TalkKind.Workshop);
        Assert.All(Talks.All, talk =>
        {
            Assert.NotEmpty(talk.Title);
            Assert.NotEmpty(talk.Event);
            Assert.All(new[] { talk.Slides, talk.Recording }.OfType<string>(), url => Assert.Equal("example.com", new Uri(url).Host));
        });
    }

    [Fact]
    public void Every_link_of_the_site_data_is_relative_or_on_a_reserved_example_domain()
    {
        var links = SiteInfo.Navigation.Select(i => i.Href!)
            .Concat(SiteInfo.FooterColumns.SelectMany(c => c.Links.Select(l => l.Href!)))
            .Concat(SiteInfo.HeroLinks.Select(l => l.Href!))
            .Concat(SiteInfo.CtaLinks.Select(l => l.Href!))
            .Concat(SiteInfo.Social.Select(l => l.Href));

        Assert.All(links, link =>
        {
            var external = ContentUrl.IsExternal(link);
            Assert.True(!external || link.StartsWith("mailto:hello@example.com", StringComparison.Ordinal) || new Uri(link).Host == "example.com", link);
            Assert.False(link.StartsWith('/'), $"{link} is absolute: it would leave a path base");
        });
        Assert.All(SiteInfo.Social, link => Assert.StartsWith("ph:", link.Icon));
    }

    // ---- The contact message

    private static List<ValidationResult> Check(ContactMessage message)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(message, new ValidationContext(message), results, validateAllProperties: true);
        return results;
    }

    private static ContactMessage Valid() => new() { Name = "Ana Torres", Email = "ana@company.test", Topic = "project", Message = "I would like to talk about a design system." };

    [Fact]
    public void A_complete_message_is_valid()
    {
        Assert.Empty(Check(Valid()));
    }

    [Fact]
    public void An_empty_message_says_what_is_missing_for_each_field()
    {
        var results = Check(new ContactMessage());

        Assert.Equal(["Email", "Message", "Name", "Topic"], results.SelectMany(r => r.MemberNames).Order());
        Assert.Contains(results, r => r.ErrorMessage == "Enter your name.");
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@")]
    public void An_email_that_is_not_an_address_is_refused(string email)
    {
        var message = Valid();
        message.Email = email;

        Assert.Equal(["Email"], Check(message).SelectMany(r => r.MemberNames));
    }

    [Fact]
    public void A_topic_that_is_not_one_of_the_choices_is_refused_and_the_choices_are_accepted()
    {
        Assert.All(SiteInfo.Topics, topic =>
        {
            var message = Valid();
            message.Topic = topic.Value;
            Assert.Empty(Check(message));
        });
        var forged = Valid();
        forged.Topic = "free-money";
        Assert.Equal(["Topic"], Check(forged).SelectMany(r => r.MemberNames));
    }

    [Fact]
    public void A_message_is_between_ten_and_two_thousand_characters_and_a_name_has_a_limit()
    {
        var tooShort = Valid();
        tooShort.Message = "Hi";
        var tooLong = Valid();
        tooLong.Message = new string('x', ContactMessage.MaxMessageLength + 1);
        var longName = Valid();
        longName.Name = new string('n', 81);

        Assert.Equal(["Message"], Check(tooShort).SelectMany(r => r.MemberNames));
        Assert.Equal(["Message"], Check(tooLong).SelectMany(r => r.MemberNames));
        Assert.Equal(["Name"], Check(longName).SelectMany(r => r.MemberNames));
        var edge = Valid();
        edge.Message = new string('x', ContactMessage.MaxMessageLength);
        Assert.Empty(Check(edge));
    }
}
