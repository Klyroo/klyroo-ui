using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Blux.UI.Mcp.Tests;

/// <summary>The protocol layer: a real MCP client from the SDK against the server, in memory and as a stdio process.</summary>
public class ServerTests
{
    private sealed class Session(IHost host, McpClient client) : IAsyncDisposable
    {
        public McpClient Client { get; } = client;

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            await host.StopAsync();
            host.Dispose();
        }
    }

    private static async Task<Session> Connect(Action<BluxDocsOptions>? configure = null)
    {
        var toServer = new Pipe();
        var toClient = new Pipe();
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddBluxDocsServer(configure).WithStreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream());
        var host = builder.Build();
        await host.StartAsync();
        var client = await McpClient.CreateAsync(new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()));
        return new Session(host, client);
    }

    private static string Text(CallToolResult result) => string.Concat(result.Content.OfType<TextContentBlock>().Select(c => c.Text));

    [Fact]
    public async Task A_client_lists_the_tools_with_descriptions_and_schemas()
    {
        await using var session = await Connect();

        var tools = await session.Client.ListToolsAsync();

        Assert.Equal(DocsTools.Names.Order(), tools.Select(t => t.Name).Order());
        Assert.All(tools, t =>
        {
            Assert.False(string.IsNullOrWhiteSpace(t.Description), t.Name);
            Assert.Equal("object", t.ProtocolTool.InputSchema.GetProperty("type").GetString());
        });
        var get = tools.Single(t => t.Name == "get-component");
        var properties = get.ProtocolTool.InputSchema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.True(properties.TryGetProperty("sections", out _));
        Assert.Contains("name", get.ProtocolTool.InputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal("blux-ui", session.Client.ServerInfo!.Name);
        Assert.Contains("search-components", session.Client.ServerInstructions!);
    }

    [Fact]
    public async Task A_client_calls_tools_and_gets_markdown()
    {
        await using var session = await Connect();

        var result = await session.Client.CallToolAsync("search-components", new Dictionary<string, object?> { ["query"] = "dialog" });

        Assert.NotEqual(true, result.IsError);
        Assert.Contains("**Modal**", Text(result));

        var component = await session.Client.CallToolAsync("get-component", new Dictionary<string, object?> { ["name"] = "tabs", ["sections"] = "api" });
        Assert.Contains("## API", Text(component));
    }

    [Fact]
    public async Task A_failing_tool_reports_an_error_the_agent_can_act_on()
    {
        await using var session = await Connect();

        var result = await session.Client.CallToolAsync("get-component", new Dictionary<string, object?> { ["name"] = "Buton" });

        Assert.True(result.IsError);
        Assert.Contains("Did you mean", Text(result));
        Assert.Contains("Button", Text(result));
    }

    [Fact]
    public async Task A_client_lists_and_gets_the_prompts()
    {
        await using var session = await Connect();

        var prompts = await session.Client.ListPromptsAsync();
        Assert.Equal(["find-component-for-usecase", "implement-component-with-props"], prompts.Select(p => p.Name).Order());

        var find = await session.Client.GetPromptAsync("find-component-for-usecase", new Dictionary<string, object?> { ["useCase"] = "confirm before deleting" });
        var findText = string.Concat(find.Messages.Select(m => (m.Content as TextContentBlock)?.Text));
        Assert.Contains("confirm before deleting", findText);
        Assert.Contains("Modal", findText);

        var implement = await session.Client.GetPromptAsync("implement-component-with-props",
            new Dictionary<string, object?> { ["component"] = "button", ["requirements"] = "a ghost submit button" });
        var implementText = string.Concat(implement.Messages.Select(m => (m.Content as TextContentBlock)?.Text));
        Assert.Contains("a ghost submit button", implementText);
        Assert.Contains("Variant: ButtonVariant", implementText);
    }

    [Fact]
    public async Task A_client_lists_and_reads_the_resources()
    {
        await using var session = await Connect();

        var resources = await session.Client.ListResourcesAsync();
        Assert.Equal(
            ["resource://blux-ui/components", "resource://blux-ui/documentation-pages", "resource://blux-ui/examples"],
            resources.Select(r => r.Uri).Order());
        Assert.Contains("resource://blux-ui/components/{slug}", (await session.Client.ListResourceTemplatesAsync()).Select(t => t.UriTemplate));

        var components = await session.Client.ReadResourceAsync("resource://blux-ui/components");
        var json = ((TextResourceContents)components.Contents[0]).Text;
        Assert.Contains("\"slug\": \"button\"", json);
        Assert.Equal(TestSupport.Bundle.Components.Count, System.Text.Json.JsonDocument.Parse(json).RootElement.GetArrayLength());

        var page = await session.Client.ReadResourceAsync("resource://blux-ui/components/modal");
        Assert.Contains("# Modal", ((TextResourceContents)page.Contents[0]).Text);

        var pages = ((TextResourceContents)(await session.Client.ReadResourceAsync("resource://blux-ui/documentation-pages")).Contents[0]).Text;
        Assert.Contains("/docs/installation", pages);
        Assert.Contains("/components/button", pages);

        var examples = ((TextResourceContents)(await session.Client.ReadResourceAsync("resource://blux-ui/examples")).Contents[0]).Text;
        Assert.Contains("ButtonVariantsExample", examples);
    }

    [Fact]
    public async Task The_tools_option_restricts_what_the_server_exposes()
    {
        await using var session = await Connect(o => o.Tools = ToolFilter.Parse("search-components, get-example"));

        var tools = await session.Client.ListToolsAsync();

        Assert.Equal(["get-example", "search-components"], tools.Select(t => t.Name).Order());
    }

    [Fact]
    public async Task The_stdio_tool_serves_the_same_tools_as_a_process()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "blux-ui",
            Command = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            Arguments = [TestSupport.ToolAssembly()],
        });
        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();
        Assert.Equal(DocsTools.Names.Order(), tools.Select(t => t.Name).Order());

        var result = await client.CallToolAsync("get-component-metadata", new Dictionary<string, object?> { ["name"] = "switch" });
        Assert.Contains("BxSwitch", Text(result));
    }

    [Fact]
    public async Task The_stdio_tool_honors_the_tools_option()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "blux-ui",
            Command = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            Arguments = [TestSupport.ToolAssembly(), "--tools", "search-components,get-component"],
        });
        await using var client = await McpClient.CreateAsync(transport);

        Assert.Equal(["get-component", "search-components"], (await client.ListToolsAsync()).Select(t => t.Name).Order());
    }

    // ---- the filter itself ----

    [Fact]
    public void The_tool_filter_parses_header_values()
    {
        Assert.Null(ToolFilter.Parse((string?)null));
        Assert.Null(ToolFilter.Parse(""));
        Assert.Null(ToolFilter.Parse(" , "));
        Assert.Equal(["a", "b"], ToolFilter.Parse(" a, B ")!.Select(n => n.ToLowerInvariant()).Order());
        // A header sent twice arrives as several values.
        Assert.Equal(3, ToolFilter.Parse(["a,b", "c"])!.Count);
        Assert.Contains("search-components", ToolFilter.Parse("Search-Components")!);
    }

    [Fact]
    public void The_tool_filter_reports_unknown_names()
    {
        Assert.Equal(["get-widget"], ToolFilter.Unknown(["search-components", "get-widget"]));
        Assert.Empty(ToolFilter.Unknown(DocsTools.Names));
    }

    [Fact]
    public void Applying_the_filter_removes_the_other_tools_from_the_options()
    {
        var options = new McpServerOptions
        {
            ToolCollection = [.. DocsTools.Names.Select(name => McpServerTool.Create(() => name, new McpServerToolCreateOptions { Name = name }))],
        };

        ToolFilter.Apply(options, ToolFilter.Parse("get-example,search-icons"));

        Assert.Equal(["get-example", "search-icons"], options.ToolCollection!.Select(t => t.ProtocolTool.Name).Order());

        ToolFilter.Apply(options, null);
        Assert.Equal(2, options.ToolCollection!.Count);
    }
}
