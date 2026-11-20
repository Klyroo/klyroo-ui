using Blux.UI.BundleGenerator;

namespace Blux.UI.Mcp.Tests;

public class GuideMarkdownTests
{
    private const string Page = """"
        @page "/docs/sample"
        @layout Layout.DocsLayout

        <PageTitle>Sample · blux-ui</PageTitle>
        <div class="pg-prose">
            <p class="pg-eyebrow">Getting started</p>
            <h1>Sample</h1>
            <p class="pg-lead">A lead &amp; more.</p>

            @* a Razor comment *@
            <h2>Steps</h2>
            <p>
                Call <code>AddBluxUi</code> in <code>Program.cs</code>, then read
                the <a href="/docs/theming">theming guide</a> about <strong>tokens</strong>. Write <code>@@bind-Value</code>.
            </p>
            <ul>
                <li><strong>@Total.Count components</strong> in ten families.</li>
                <li>Second item</li>
            </ul>
            <CodeBlock Title="Program.cs" Code="@Setup" />
            <CodeBlock Code="dotnet add package Blux.UI" />
            <CodeBlock Code="@("builder.Services.AddBluxUi(\"x\");")" />
            <div class="not-prose my-4"><BxButton Label="Live demo" /></div>
        </div>

        @code {
            private const string Setup = """
                builder.Services.AddBluxUi();
                  // indented
                """;
        }
        """";

    [Fact]
    public void A_guide_page_becomes_markdown()
    {
        var markdown = GuideMarkdown.Convert(Page, new Dictionary<string, string> { ["Total.Count"] = "133" });

        Assert.StartsWith("# Sample\n\nA lead & more.\n\n## Steps\n\n", markdown);
        Assert.Contains("Call `AddBluxUi` in `Program.cs`, then read the [theming guide](/docs/theming) about **tokens**. Write `@bind-Value`.", markdown);
        Assert.Contains("- **133 components** in ten families.\n\n- Second item", markdown);
    }

    [Fact]
    public void Code_blocks_keep_their_code_title_and_language()
    {
        var markdown = GuideMarkdown.Convert(Page, new Dictionary<string, string> { ["Total.Count"] = "133" });

        Assert.Contains("**Program.cs**\n\n```csharp\nbuilder.Services.AddBluxUi();\n  // indented\n```", markdown);
        Assert.Contains("```bash\ndotnet add package Blux.UI\n```", markdown);
        Assert.Contains("```csharp\nbuilder.Services.AddBluxUi(\"x\");\n```", markdown);
    }

    [Fact]
    public void Eyebrows_live_demos_and_razor_directives_are_dropped()
    {
        var markdown = GuideMarkdown.Convert(Page);

        Assert.DoesNotContain("Getting started", markdown);
        Assert.DoesNotContain("Live demo", markdown);
        Assert.DoesNotContain("@page", markdown);
        Assert.DoesNotContain("PageTitle", markdown);
        Assert.DoesNotContain("Razor comment", markdown);
    }

    [Fact]
    public void A_page_without_prose_is_an_error()
    {
        Assert.Throws<InvalidOperationException>(() => GuideMarkdown.Convert("@page \"/x\"\n<div>nothing</div>"));
    }
}
