using System.Net;
using System.Text;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Blux.UI.E2E;

/// <summary>The AI tooling served by the docs site: llms.txt, llms-full.txt and the MCP server at /mcp. No browser is involved.</summary>
public class AiTests(PlaygroundServer server)
{
    private static readonly HttpClient Http = new();

    [Fact]
    public async Task Llms_txt_is_served_as_text_with_links_to_this_site()
    {
        var response = await Http.GetAsync($"{server.BaseUrl}/llms.txt", TestContext.Current.CancellationToken);
        var text = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.Content.Headers.ContentType!.ToString());
        Assert.StartsWith("# Blux UI\n\n> ", text);
        Assert.Contains($"]({server.BaseUrl}/components/button)", text);
        Assert.Contains($"]({server.BaseUrl}/docs/mcp)", text);
    }

    [Fact]
    public async Task Every_link_of_llms_txt_leads_to_a_page_that_exists()
    {
        var text = await Http.GetStringAsync($"{server.BaseUrl}/llms.txt", TestContext.Current.CancellationToken);
        var links = System.Text.RegularExpressions.Regex.Matches(text, @"\]\((" + System.Text.RegularExpressions.Regex.Escape(server.BaseUrl) + @"[^)]*)\)")
            .Select(m => m.Groups[1].Value).Distinct().ToList();

        Assert.True(links.Count > 100);
        foreach (var link in links)
        {
            var status = (await Http.GetAsync(link, HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken)).StatusCode;
            Assert.True(status == HttpStatusCode.OK, $"{link} answered {(int)status}");
        }
    }

    [Fact]
    public async Task Llms_full_txt_holds_the_components_with_their_api()
    {
        var text = await Http.GetStringAsync($"{server.BaseUrl}/llms-full.txt", TestContext.Current.CancellationToken);

        Assert.StartsWith("# Blux UI", text);
        Assert.Contains("## Button", text);
        Assert.Contains("`Variant`: `ButtonVariant`", text);
        Assert.Contains("<BxButton", text);
    }

    [Fact]
    public async Task The_mcp_endpoint_answers_initialize_and_tools_list_over_http()
    {
        // The raw protocol first: what any MCP client sends.
        using var initialize = new HttpRequestMessage(HttpMethod.Post, $"{server.BaseUrl}/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"e2e","version":"1"}}}""",
                Encoding.UTF8, "application/json"),
        };
        initialize.Headers.Accept.ParseAdd("application/json");
        initialize.Headers.Accept.ParseAdd("text/event-stream");
        var response = await Http.SendAsync(initialize, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"serverInfo\"", body);
        Assert.Contains("\"name\":\"blux-ui\"", body);

        // Then a real client from the SDK.
        await using var client = await Connect();
        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("search-components", tools.Select(t => t.Name));
        Assert.Equal(9, tools.Count);

        var result = await client.CallToolAsync("get-component", new Dictionary<string, object?> { ["name"] = "button", ["sections"] = "api" },
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains("ButtonVariant", string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
    }

    [Fact]
    public async Task The_x_mcp_tools_header_restricts_the_tools()
    {
        await using var client = await Connect(new Dictionary<string, string> { ["X-MCP-Tools"] = "search-components,get-example" });

        var tools = await client.ListToolsAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["get-example", "search-components"], tools.Select(t => t.Name).Order());
    }

    [Theory]
    [InlineData("/docs/mcp")]
    [InlineData("/docs/llms-txt")]
    [InlineData("/docs/skills")]
    public async Task The_ai_guides_are_pages_of_the_site(string path)
    {
        var response = await Http.GetAsync(server.BaseUrl + path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<McpClient> Connect(Dictionary<string, string>? headers = null) =>
        McpClient.CreateAsync(new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri($"{server.BaseUrl}/mcp"),
            AdditionalHeaders = headers,
        }), cancellationToken: TestContext.Current.CancellationToken);
}
