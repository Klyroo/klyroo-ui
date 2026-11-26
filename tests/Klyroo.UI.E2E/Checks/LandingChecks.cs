using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Klyroo.UI.E2E;

/// <summary>What the Landing template does in a browser, shared by the run of the published app and the run of its demo behind the docs host.</summary>
internal static class LandingChecks
{
    public static readonly string[] SectionIds = ["features", "how-it-works", "customers", "pricing", "faq"];

    /// <summary>
    /// The page is static HTML: every section is there, and the pieces that move work without .NET in the browser. The monthly and yearly
    /// switch is two radio buttons and CSS, the accordion and the color mode button are Klyroo elements, so no circuit and no WebAssembly start.
    /// </summary>
    public static async Task AssertBehaviour(IPage page)
    {
        await Assertions.Expect(page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToContainTextAsync("Send an invoice");
        foreach (var id in SectionIds) await Assertions.Expect(page.Locator($"section#{id}")).ToHaveCountAsync(1);
        await Assertions.Expect(page.GetByTestId("testimonial")).ToHaveCountAsync(3);
        // Klyroo's browser side is loaded by blazor.web.js, static pages included.
        await page.WaitForFunctionAsync("() => !!customElements.get('kly-accordion')");

        // Pricing: monthly first, and the yearly copy is not in the layout.
        var pricing = page.GetByTestId("pricing");
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$15", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        // Yearly, with the mouse on the label.
        await pricing.GetByText("Yearly, 2 months free").ClickAsync();
        await Assertions.Expect(pricing.GetByText("$15", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(0);
        await Assertions.Expect(pricing.GetByText("$180 billed once a year.").Locator("visible=true")).ToHaveCountAsync(1);
        // Back to monthly, with the keyboard: the radio group is a real one.
        await page.GetByRole(AriaRole.Radio, new() { Name = "Yearly, 2 months free" }).FocusAsync();
        await page.Keyboard.PressAsync("ArrowLeft");
        await Assertions.Expect(page.GetByRole(AriaRole.Radio, new() { Name = "Monthly" })).ToBeCheckedAsync();
        await Assertions.Expect(pricing.GetByText("$18", new() { Exact = true }).Locator("visible=true")).ToHaveCountAsync(1);

        // FAQ: a closed answer opens when its question is pressed.
        var question = page.GetByRole(AriaRole.Button, new() { Name = "How do reminders work?" });
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "false");
        await question.ClickAsync();
        await Assertions.Expect(question).ToHaveAttributeAsync("aria-expanded", "true");
        await Assertions.Expect(page.GetByText("Reminders stop when the invoice is paid or cancelled.")).ToBeVisibleAsync();

        // Color mode.
        var dark = (await page.Locator("html").GetAttributeAsync("class") ?? "").Contains("dark");
        await page.Locator("[data-kly-color-mode=toggle]").ClickAsync();
        await Assertions.Expect(page.Locator("html")).ToHaveClassAsync(dark ? new Regex("^((?!dark).)*$") : new Regex("dark"));
    }

    /// <summary>
    /// In Windows High Contrast the page drops its background fills: the checked billing option must still look different from the other one
    /// (the template paints it with the system Highlight colors).
    /// </summary>
    public static async Task AssertForcedColors(IPage page)
    {
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        try
        {
            var monthly = page.Locator("label[for=billing-monthly]");
            var yearly = page.Locator("label[for=billing-yearly]");
            await page.GetByRole(AriaRole.Radio, new() { Name = "Monthly" }).CheckAsync(new() { Force = true });
            var checkedFill = await monthly.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
            var otherFill = await yearly.EvaluateAsync<string>("e => getComputedStyle(e).backgroundColor");
            Assert.NotEqual(otherFill, checkedFill);
            // And the text of the checked option is not the color of its fill.
            var checkedText = await monthly.EvaluateAsync<string>("e => getComputedStyle(e).color");
            Assert.NotEqual(checkedFill, checkedText);
        }
        finally
        {
            await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.None });
        }
    }

    /// <summary>
    /// The locale select of a static page: choosing a language writes the culture cookie and reloads the page in that language, with no
    /// .NET in the browser to do it. Changes the state of the page, so it goes last.
    /// </summary>
    public static async Task AssertLocaleSelect(IPage page)
    {
        await page.EvaluateAsync("() => { window.__beforeReload = true; }");
        await page.GetByRole(AriaRole.Combobox, new() { Name = "Language" }).ClickAsync();
        await page.GetByRole(AriaRole.Option, new() { Name = "Français" }).ClickAsync();
        await page.WaitForFunctionAsync("() => window.__beforeReload === undefined");
        await Assertions.Expect(page.Locator("html")).ToHaveAttributeAsync("lang", "fr");
        var cookie = (await page.Context.CookiesAsync()).Single(c => c.Name == ".AspNetCore.Culture");
        Assert.Contains("fr", cookie.Value);
    }

    /// <summary>The page has no horizontal scroll at the width of a small phone.</summary>
    public static async Task AssertNoHorizontalScroll(IPage page)
    {
        await page.SetViewportSizeAsync(375, 800);
        var sizes = await page.EvaluateAsync<int[]>("() => [document.documentElement.scrollWidth, document.documentElement.clientWidth]");
        Assert.True(sizes[0] <= sizes[1], $"horizontal scroll at 375px: scrollWidth {sizes[0]} > clientWidth {sizes[1]}");
    }
}
