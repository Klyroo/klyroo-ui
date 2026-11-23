namespace Blux.UI.E2E;

/// <summary>
/// Not a test: the generator of the screenshots the /templates page shows. It opens the live demo of every available template, inside the docs
/// host the fixture started, at 1280x800 in light and in dark, and writes <c>template-{name}-{light|dark}.png</c> to the folder in
/// <c>BLUX_TEMPLATE_SCREENSHOTS</c>; without the variable it is skipped. Run it on demand and commit the images, they are not compared in CI:
/// <code>
/// BLUX_TEMPLATE_SCREENSHOTS=playground/Blux.Playground/Blux.Playground/wwwroot/img dotnet test --project tests/Blux.UI.E2E --filter-class "Blux.UI.E2E.TemplateScreenshots"
/// </code>
/// </summary>
public class TemplateScreenshots(PlaygroundServer server)
{
    /// <summary>The templates that run as a demo: the registry of the docs site, compiled into this project.</summary>
    private static readonly string[] Templates = [.. Blux.Playground.Client.Templates.TemplateRegistry.All.Where(t => t.HasDemo).Select(t => t.Name)];

    [Fact]
    public async Task Generate()
    {
        var output = Environment.GetEnvironmentVariable("BLUX_TEMPLATE_SCREENSHOTS");
        Assert.SkipWhen(string.IsNullOrEmpty(output), "BLUX_TEMPLATE_SCREENSHOTS is not set.");
        Assert.SkipUnless(server.TemplatesReady, "The template demos are not running.");
        // A relative folder is relative to the repository root (the working directory of a test run is the build output).
        output = Path.GetFullPath(output!, RepoRoot());
        Directory.CreateDirectory(output);

        foreach (var name in Templates)
        {
            foreach (var (mode, scheme) in new[] { ("light", ColorScheme.Light), ("dark", ColorScheme.Dark) })
            {
                await using var context = await server.Browser.NewContextAsync(new()
                {
                    BaseURL = server.BaseUrl + "/",
                    ViewportSize = new() { Width = 1280, Height = 800 },
                    ColorScheme = scheme,
                    DeviceScaleFactor = 1,
                });
                var page = await context.NewPageAsync();
                await page.GotoAsync($"templates/{name}/demo/");
                // Blux's script has run, and a template that renders interactively (data-testid="renderer") has left the prerendered state.
                await page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
                await page.WaitForFunctionAsync("() => { const r = document.querySelector('[data-testid=renderer]'); return !r || /Server|WebAssembly/.test(r.textContent ?? ''); }", null, new() { Timeout = 30_000 });
                await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(mode == "dark" ? "dark" : "^((?!dark).)*$"));
                // Fonts and the icon bundle settled, and nothing animating when the shot is taken.
                // The Editor template starts on an empty home page: open its guide, a document that shows what the editor does.
                if (name == "editor")
                {
                    await page.GetByTestId("home-guide").ClickAsync();
                    await EditorChecks.WaitForDocumentUrl(page);
                    await EditorChecks.WaitEditor(page);
                    await Assertions.Expect(page.GetByTestId("save-state")).ToHaveTextAsync("Saved");
                }
                await page.EvaluateAsync("() => document.fonts.ready");
                // FocusOnNavigate puts the focus on the h1 of the page: its ring is not part of the design.
                await page.EvaluateAsync("() => document.activeElement instanceof HTMLElement && document.activeElement.blur()");
                await Task.Delay(1000, TestContext.Current.CancellationToken);
                await page.ScreenshotAsync(new() { Path = Path.Combine(output, $"template-{name}-{mode}.png"), Animations = ScreenshotAnimations.Disabled });
            }
        }
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx"))) return dir.FullName;
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }
}
