using System.Net;
using System.Text.Json;
using Blux.Templates.Content;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.Templates.Tests;

public class MarkdownSearchIndexTests
{
    private static async Task<(WebApplication App, HttpClient Client)> Start(ContentFolder folder)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddMarkdownContent(new ContentOptions { Root = folder.Root });
        var app = builder.Build();
        app.MapSearchIndex();
        await app.StartAsync(TestContext.Current.CancellationToken);
        return (app, app.GetTestClient());
    }

    private static ContentFolder Content() => new ContentFolder()
        .Page("start/install.md", "Install", body: "## From a package\n\nRun the installer. It uses systemd.", extra: "description: Put it on a machine.\n")
        .Add("start/_dir.yml", "title: Getting started\n")
        .Page("about.md", "About");

    [Fact]
    public async Task The_index_is_a_json_array_of_the_pages_with_their_headings_and_text()
    {
        using var folder = Content();
        var (app, client) = await Start(folder);
        await using var host = app;

        var response = await client.GetAsync("/search-index.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var pages = json.RootElement.EnumerateArray().ToList();
        var install = pages.Single(p => p.GetProperty("title").GetString() == "Install");
        Assert.Equal("start/install", install.GetProperty("href").GetString());
        Assert.Equal("Getting started", install.GetProperty("group").GetString());
        Assert.Equal("Put it on a machine.", install.GetProperty("description").GetString());
        Assert.Contains("systemd", install.GetProperty("keywords").GetString());
        Assert.Equal("from-a-package", install.GetProperty("headings")[0].GetProperty("id").GetString());
        Assert.Equal("From a package", install.GetProperty("headings")[0].GetProperty("text").GetString());
        // A page without a group or a description has no such field.
        var about = pages.Single(p => p.GetProperty("title").GetString() == "About");
        Assert.False(about.TryGetProperty("group", out _));
        Assert.False(about.TryGetProperty("description", out _));
    }

    [Fact]
    public async Task The_index_has_an_etag_and_a_request_that_has_it_gets_a_304_with_no_body()
    {
        using var folder = Content();
        var (app, client) = await Start(folder);
        await using var host = app;

        var first = await client.GetAsync("/search-index.json", TestContext.Current.CancellationToken);
        var etag = first.Headers.ETag!.ToString();
        var request = new HttpRequestMessage(HttpMethod.Get, "/search-index.json");
        request.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var second = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("no-cache", first.Headers.CacheControl!.ToString());
        Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        Assert.Empty(await second.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task A_reload_makes_a_new_index_with_another_etag()
    {
        using var folder = Content();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddMarkdownContent(new ContentOptions { Root = folder.Root });
        await using var app = builder.Build();
        app.MapSearchIndex();
        await app.StartAsync(TestContext.Current.CancellationToken);
        var client = app.GetTestClient();

        var before = (await client.GetAsync("/search-index.json", TestContext.Current.CancellationToken)).Headers.ETag!.ToString();
        folder.Page("new.md", "A new page");
        Assert.True(app.Services.GetRequiredService<MarkdownStore>().Reload());
        var response = await client.GetAsync("/search-index.json", TestContext.Current.CancellationToken);

        Assert.NotEqual(before, response.Headers.ETag!.ToString());
        Assert.Contains("A new page", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void The_index_is_much_smaller_than_the_same_pages_written_into_each_page()
    {
        using var folder = new ContentFolder();
        for (var i = 0; i < 20; i++)
        {
            folder.Page($"g/p{i}.md", $"Page {i}", body: "## One\n\n## Two\n\n## Three\n\n" + string.Join(' ', Enumerable.Repeat("word", 400)));
        }

        var bytes = MarkdownSearchIndex.Serialize(folder.Load());

        // Twenty pages, three headings each, 2,000 characters of text each: the text is once per page, not once per heading.
        Assert.InRange(bytes.Length, 1, 70_000);
    }
}
