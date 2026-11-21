using System.Xml.Linq;
using Changelog.Web.Releases;

namespace Blux.Templates.Tests;

public class ReleaseFeedTests
{
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly ReleaseBody Body = new("https://github.com/octo/tern");
    private static readonly FeedInfo Info = new("https://example.com/changes/", "Tern releases", "What changed");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-06-01T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    private static XDocument Parse(string xml) => XDocument.Parse(xml);

    private static ReleaseEntry Release(string tag, string title, string body, ReleaseAuthor? author = null, bool prerelease = false, DateTimeOffset? published = null) =>
        new(1, tag, title, ReleaseSlug.From(tag), body, published ?? Now, prerelease, null, author);

    [Fact]
    public void The_feed_is_valid_atom_with_one_entry_per_release_and_absolute_links()
    {
        var xml = ReleaseFeed.Build(
            [Release("v2", "Tern 2", "## New\n\nSee #5", new ReleaseAuthor("mira-okafor", null)), Release("v1", "Tern 1", "First", prerelease: true, published: Now.AddDays(-9))],
            Info, Body, Now);

        var doc = Parse(xml);
        var feed = doc.Root!;
        Assert.Equal(Atom + "feed", feed.Name);
        Assert.Equal("Tern releases", (string)feed.Element(Atom + "title")!);
        Assert.Equal("https://example.com/changes/", (string)feed.Element(Atom + "id")!);
        Assert.Equal("2026-06-01T12:00:00Z", (string)feed.Element(Atom + "updated")!);
        Assert.Equal("https://example.com/changes/feed.xml", feed.Elements(Atom + "link").Single(l => (string)l.Attribute("rel")! == "self").Attribute("href")!.Value);
        var entries = feed.Elements(Atom + "entry").ToList();
        Assert.Equal(2, entries.Count);
        var first = entries[0];
        Assert.Equal("https://example.com/changes/releases/v2", (string)first.Element(Atom + "id")!);
        Assert.Equal("https://example.com/changes/releases/v2", first.Element(Atom + "link")!.Attribute("href")!.Value);
        Assert.Equal("mira-okafor", (string)first.Element(Atom + "author")!.Element(Atom + "name")!);
        Assert.Equal("https://github.com/mira-okafor", (string)first.Element(Atom + "author")!.Element(Atom + "uri")!);
        var content = first.Element(Atom + "content")!;
        Assert.Equal("html", (string)content.Attribute("type")!);
        Assert.Contains("<h3 id=\"rel-v2-new\">New</h3>", content.Value);
        Assert.Contains("href=\"https://github.com/octo/tern/issues/5\"", content.Value);
        Assert.Equal("pre-release", (string)entries[1].Element(Atom + "category")!.Attribute("term")!);
        Assert.Equal("2026-05-23T12:00:00Z", (string)entries[1].Element(Atom + "published")!);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>", xml);
    }

    [Fact]
    public void Hostile_text_is_escaped_and_never_becomes_an_element_or_an_attribute()
    {
        var hostile = "</title><script>alert(1)</script><entry xmlns=\"x\"/>]]> & \" ' <![CDATA[";
        var xml = ReleaseFeed.Build(
            [Release("v1", hostile, "<script>alert(1)</script>\n\n[x](https://example.com/\"><injected/>)\n\n" + hostile, new ReleaseAuthor("octo-fan", null))],
            new FeedInfo("https://example.com/", hostile, hostile), Body, Now);

        var doc = Parse(xml);
        Assert.Single(doc.Descendants(Atom + "entry"));
        // Only Atom elements: nothing the data said became markup.
        Assert.All(doc.Descendants(), element => Assert.Equal(Atom, element.Name.Namespace));
        Assert.DoesNotContain(doc.Descendants(), element => element.Name.LocalName is "script" or "injected");
        Assert.Equal(hostile, (string)doc.Root!.Element(Atom + "title")!);
        Assert.Equal(hostile, (string)doc.Root.Element(Atom + "entry")!.Element(Atom + "title")!);
        var content = doc.Root.Element(Atom + "entry")!.Element(Atom + "content")!.Value;
        Assert.DoesNotContain("<script", content);
        Assert.DoesNotContain("<injected", content);
        Assert.DoesNotContain("<script", xml);
        Assert.Contains("&lt;script&gt;", content);
    }

    [Fact]
    public void Characters_that_xml_cannot_hold_are_removed_instead_of_breaking_the_document()
    {
        var bad = "a\u0000b\u0001c\u0008d\u000Be\u001Ff￾g￿h\uD800i\uDC00j";
        var xml = ReleaseFeed.Build([Release("v1", bad, bad + "\n\n" + bad, new ReleaseAuthor("octo-fan", null))], new FeedInfo("https://example.com/", bad, bad), Body, Now);

        var doc = Parse(xml);
        Assert.Equal("abcdefghij", (string)doc.Root!.Element(Atom + "title")!);
        Assert.Equal("abcdefghij", (string)doc.Root.Element(Atom + "entry")!.Element(Atom + "title")!);
        Assert.DoesNotContain(xml, c => c < 0x20 && c is not ('\t' or '\n' or '\r'));
    }

    [Fact]
    public void Valid_surrogate_pairs_tabs_and_new_lines_are_kept()
    {
        Assert.Equal("a\tb\nc\rd😀e", ReleaseFeed.Clean("a\tb\nc\rd😀e"));
        Assert.Equal("", ReleaseFeed.Clean(null));
    }

    [Fact]
    public void The_feed_has_the_latest_twenty_and_no_more()
    {
        var releases = Enumerable.Range(0, 35).Select(i => Release("v" + i, "Tern " + i, "", published: Now.AddDays(-i))).ToList();

        var doc = Parse(ReleaseFeed.Build(releases, Info, Body, Now));

        Assert.Equal(20, doc.Descendants(Atom + "entry").Count());
        Assert.Equal("https://example.com/changes/releases/v0", (string)doc.Descendants(Atom + "entry").First().Element(Atom + "id")!);
    }

    [Fact]
    public void The_links_of_an_entry_are_relative_to_the_base_of_the_feed_so_the_address_of_the_site_is_not_in_what_is_cached()
    {
        var one = ReleaseFeed.Build([Release("v1", "v1", "[go](#top)\n\n## Top")], new FeedInfo("https://a.example/", "t", "s"), Body, Now);
        var two = ReleaseFeed.Build([Release("v1", "v1", "[go](#top)\n\n## Top")], new FeedInfo("https://b.example/", "t", "s"), Body, Now);

        var content = Parse(one).Descendants(Atom + "content").Single().Value;
        Assert.Contains("href=\"releases/v1#rel-v1-top\"", content);
        Assert.Equal("https://a.example/", (string)Parse(one).Root!.Attribute(XNamespace.Xml + "base")!);
        Assert.Equal("https://b.example/", (string)Parse(two).Root!.Attribute(XNamespace.Xml + "base")!);
        Assert.DoesNotContain("a.example", Parse(two).Descendants(Atom + "content").Single().Value);
    }

    [Fact]
    public void An_empty_list_is_a_valid_feed_that_says_when_it_was_made()
    {
        var doc = Parse(ReleaseFeed.Build([], Info, Body, Now));

        Assert.Empty(doc.Descendants(Atom + "entry"));
        Assert.Equal("2026-06-01T12:00:00Z", (string)doc.Root!.Element(Atom + "updated")!);
    }

    [Fact]
    public void A_release_with_no_date_uses_the_time_of_the_feed_and_a_release_with_no_body_has_no_content()
    {
        var release = new ReleaseEntry(1, "v1", "v1", "v1", "", null, false, null, null);

        var entry = Parse(ReleaseFeed.Build([release], Info, Body, Now)).Descendants(Atom + "entry").Single();

        Assert.Equal("2026-06-01T12:00:00Z", (string)entry.Element(Atom + "updated")!);
        Assert.Null(entry.Element(Atom + "published"));
        Assert.Null(entry.Element(Atom + "content"));
        Assert.Null(entry.Element(Atom + "author"));
    }

    [Fact]
    public void The_copy_button_of_a_code_block_is_not_in_the_feed()
    {
        var entry = Parse(ReleaseFeed.Build([Release("v1", "v1", "```bash\ntern sync\n```")], Info, Body, Now)).Descendants(Atom + "entry").Single();

        var html = entry.Element(Atom + "content")!.Value;
        Assert.Contains("<pre", html);
        Assert.DoesNotContain("data-code-copy", html);
        Assert.DoesNotContain("<button", html);
    }

    [Fact]
    public void A_bot_author_links_to_its_app_page()
    {
        Assert.Equal("https://github.com/apps/github-actions", new ReleaseAuthor("github-actions[bot]", null).ProfileUrl);
        Assert.Equal("https://github.com/octo-fan", new ReleaseAuthor("octo-fan", null).ProfileUrl);
    }
}

public class ReleaseSlugTests
{
    [Theory]
    [InlineData("v1.4.0", "v1.4.0")]
    [InlineData("V1.4.0-RC.1", "v1.4.0-rc.1")]
    [InlineData("release/1.2", "release-1.2")]
    [InlineData("tools/v0.8+build.5", "tools-v0.8-build.5")]
    [InlineData("v1.0 beta", "v1.0-beta")]
    [InlineData("été", "ete")]
    [InlineData("1.0_final", "1.0_final")]
    public void A_tag_becomes_a_segment_of_safe_characters(string tag, string slug) => Assert.Equal(slug, ReleaseSlug.From(tag));

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("..\\..\\windows\\system32")]
    [InlineData("%2e%2e%2f%2e%2e%2fsecret")]
    [InlineData("..%2f..%2f")]
    [InlineData("/absolute")]
    [InlineData("v1/../../x")]
    [InlineData("a?b=c#d")]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("v1\0v2")]
    [InlineData("v1\r\nSet-Cookie: x=y")]
    [InlineData("https://evil.example/")]
    [InlineData("javascript:alert(1)")]
    public void A_hostile_tag_is_a_single_harmless_segment(string tag)
    {
        var slug = ReleaseSlug.From(tag);

        Assert.Matches("^[a-z0-9][a-z0-9._-]*[a-z0-9]$|^[a-z0-9]$", slug);
        Assert.DoesNotContain("/", slug);
        Assert.DoesNotContain("\\", slug);
        Assert.NotEqual(".", slug);
        Assert.NotEqual("..", slug);
        Assert.True(Uri.TryCreate("https://example.com/releases/" + slug, UriKind.Absolute, out var uri) && uri.AbsolutePath == "/releases/" + slug, $"'{slug}' changes when it is put in an address");
    }

    [Theory]
    [InlineData("..")]
    [InlineData(".")]
    [InlineData("...")]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("日本語")]
    [InlineData("🚀")]
    public void A_tag_with_nothing_to_keep_is_called_release(string tag) => Assert.Equal("release", ReleaseSlug.From(tag));

    [Theory]
    [InlineData("v1\uFFFE", "v1")]
    [InlineData("\uFFFF", "release")]
    [InlineData("a\uD800b", "a-b")]
    [InlineData("a\uDC00", "a")]
    [InlineData("\uFFFEv\u0000\u0001", "v")]
    public void A_tag_with_characters_that_are_not_text_gives_a_slug_instead_of_an_exception(string tag, string slug) => Assert.Equal(slug, ReleaseSlug.From(tag));

    [Fact]
    public void A_null_tag_is_called_release() => Assert.Equal("release", ReleaseSlug.From(null));

    [Fact]
    public void A_dot_segment_is_never_a_slug_even_with_other_characters_around_it()
    {
        Assert.DoesNotContain(new[] { "..", ".", "-.", ".-" }, bad => ReleaseSlug.From(bad) == bad);
        Assert.Equal("a", ReleaseSlug.From("../a/.."));
    }

    [Fact]
    public void A_long_tag_is_cut_without_leaving_a_separator_at_the_end()
    {
        var slug = ReleaseSlug.From(new string('a', 79) + "-" + new string('b', 50));

        Assert.True(slug.Length <= ReleaseSlug.MaxLength);
        Assert.DoesNotMatch("[.\\-_]$", slug);
    }

    [Fact]
    public void Tags_with_the_same_slug_get_a_number_so_every_release_has_a_page_of_its_own()
    {
        var slugs = ReleaseSlug.Assign(["v1", "V1", "v1", "release/1", "release-1", "..", "***", null]);

        Assert.Equal(["v1", "v1-2", "v1-3", "release-1", "release-1-2", "release", "release-2", "release-3"], slugs);
        Assert.Equal(slugs.Count, slugs.Distinct().Count());
    }

    [Fact]
    public void A_number_added_to_a_clash_cannot_clash_with_a_tag_that_looks_like_it()
    {
        var slugs = ReleaseSlug.Assign(["v1", "v1", "v1-2"]);

        Assert.Equal(slugs.Count, slugs.Distinct().Count());
    }
}
