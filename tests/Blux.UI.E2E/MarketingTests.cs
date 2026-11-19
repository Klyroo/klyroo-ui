namespace Blux.UI.E2E;

/// <summary>The "Marketing" family: AuthForm, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts, ChangelogVersion, ChangelogVersions.</summary>
public class MarketingTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private static readonly string[] Slugs =
    [
        "auth-form", "pricing-plan", "pricing-plans", "pricing-table", "blog-post", "blog-posts", "changelog-version", "changelog-versions",
    ];

    public static TheoryData<string, string> EverySlugInEveryMode()
    {
        var data = new TheoryData<string, string>();
        foreach (var slug in Slugs)
        {
            foreach (var mode in new[] { "ssr", "server", "wasm", "auto" }) data.Add(slug, mode);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(EverySlugInEveryMode))]
    public async Task Page_has_no_accessibility_violations(string slug, string mode)
    {
        await Open(mode, slug);

        await AssertNoAccessibilityViolations();
    }

    private ILocator SignIn => Page.GetByRole(AriaRole.Form, new() { Name = "Welcome back" });

    private async Task FillCredentials(string email, string password)
    {
        await SignIn.Locator("input[type=email]").FillAsync(email);
        await SignIn.Locator("input[name='Posted[password]']").FillAsync(password);
        // Leaving the field commits the value of a bound control before the submit click.
        await Page.Keyboard.PressAsync("Tab");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_validates_then_submits(string mode)
    {
        await Open(mode, "auth-form");

        await SignIn.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Assertions.Expect(SignIn.GetByText("Email is required.")).ToBeVisibleAsync();
        await Assertions.Expect(SignIn.GetByText("Password is required.")).ToBeVisibleAsync();
        await Assertions.Expect(SignIn.Locator("input[type=email]")).ToHaveAttributeAsync("aria-invalid", "true");

        // A static page came back from the post with empty fields: fill them again.
        await FillCredentials("not an email", "short");
        await SignIn.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Assertions.Expect(SignIn.GetByText("Enter a valid email address.")).ToBeVisibleAsync();

        await FillCredentials("ada@example.com", "short");
        await SignIn.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();
        await Assertions.Expect(SignIn.GetByText("Use at least 8 characters.")).ToBeVisibleAsync();

        await FillCredentials("ada@example.com", "correct-horse");
        await SignIn.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("auth-signed-in")).ToHaveTextAsync("Signed in as ada@example.com.");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_shows_an_error_about_the_whole_form(string mode)
    {
        await Open(mode, "auth-form");

        await FillCredentials("locked@example.com", "correct-horse");
        await SignIn.GetByRole(AriaRole.Button, new() { Name = "Sign in" }).ClickAsync();

        await Assertions.Expect(SignIn.GetByRole(AriaRole.Alert)).ToContainTextAsync("This account is locked");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_password_can_be_shown_and_hidden(string mode)
    {
        await Open(mode, "auth-form");
        var password = SignIn.Locator("input[name='Posted[password]']");
        var toggle = SignIn.GetByRole(AriaRole.Button, new() { Name = "Show password" });

        await password.FillAsync("correct-horse");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
        await toggle.ClickAsync();
        await Assertions.Expect(password).ToHaveAttributeAsync("type", "text");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(password).ToHaveValueAsync("correct-horse");

        await toggle.ClickAsync();
        await Assertions.Expect(password).ToHaveAttributeAsync("type", "password");
        await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_provider_buttons_are_links(string mode)
    {
        await Open(mode, "auth-form");

        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Continue with GitHub" })).ToHaveAttributeAsync("href", "/docs");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_posts_select_and_checkbox_values(string mode)
    {
        await Open(mode, "auth-form");
        var form = Page.GetByRole(AriaRole.Form, new() { Name = "Create your account" });
        async Task Fill()
        {
            await form.GetByLabel("Full name").FillAsync("Ada Lovelace");
            await form.GetByLabel("Work email").FillAsync("ada@company.com");
            await form.Locator("input[name='Posted[password]']").FillAsync("correct-horse");
            await Page.Keyboard.PressAsync("Tab");
        }

        await Fill();
        await form.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();
        await Assertions.Expect(form.GetByText("I agree to the terms of service is required.")).ToBeVisibleAsync();

        await Fill();
        await form.GetByRole(AriaRole.Combobox, new() { Name = "Your role" }).ClickAsync();
        await Page.GetByRole(AriaRole.Option, new() { Name = "Designer" }).ClickAsync();
        await form.GetByRole(AriaRole.Checkbox, new() { Name = "I agree to the terms of service" }).CheckAsync();
        await form.GetByRole(AriaRole.Button, new() { Name = "Create account" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("auth-created")).ToHaveTextAsync("Account created for ada@company.com.");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task AuthForm_one_time_code_is_checked_then_accepted(string mode)
    {
        await Open(mode, "auth-form");
        var form = Page.GetByRole(AriaRole.Form, new() { Name = "Check your email" });
        var cells = form.Locator("[data-slot=cell]");

        await cells.First.FocusAsync();
        await Page.Keyboard.TypeAsync("123");
        await form.GetByRole(AriaRole.Button, new() { Name = "Verify" }).ClickAsync();
        await Assertions.Expect(form.GetByText("Enter all 6 characters.")).ToBeVisibleAsync();

        await cells.First.FocusAsync();
        await Page.Keyboard.TypeAsync("123456");
        await form.GetByRole(AriaRole.Button, new() { Name = "Verify" }).ClickAsync();

        await Assertions.Expect(Page.GetByTestId("auth-verified")).ToHaveTextAsync("Email verified.");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task PricingPlans_billing_switch_changes_the_prices(string mode)
    {
        await Open(mode, "pricing-plans");
        var yearly = Page.GetByRole(AriaRole.Button, new() { Name = "Yearly" });

        await Assertions.Expect(Page.GetByText("Billed monthly, cancel anytime.")).ToBeVisibleAsync();
        await yearly.ClickAsync();

        await Assertions.Expect(yearly).ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(Page.GetByText("Billed yearly, 2 months free.")).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByText("$24", new() { Exact = true })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Button, new() { Name = "Monthly" }).ClickAsync();
        await Assertions.Expect(Page.GetByText("$29", new() { Exact = true })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PricingPlans_billing_switch_is_disabled_without_interactivity()
    {
        await Open("ssr", "pricing-plans");

        await Assertions.Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Yearly" })).ToBeDisabledAsync();
        await Assertions.Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Get Pro" })).ToHaveAttributeAsync("href", "/docs");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PricingPlan_discount_names_both_prices(string mode)
    {
        await Open(mode, "pricing-plan");
        var plan = Page.Locator("del").First.Locator("xpath=ancestor::p");

        await Assertions.Expect(plan).ToContainTextAsync("Original price $120");
        await Assertions.Expect(plan).ToContainTextAsync("Now $96");
        await Assertions.Expect(Page.Locator("del").First).ToHaveCSSAsync("text-decoration-line", "line-through");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PricingTable_says_included_or_not_included_and_keeps_its_header_sticky(string mode)
    {
        await Open(mode, "pricing-table");
        var table = Page.GetByRole(AriaRole.Table, new() { Name = "Compare the Starter, Pro and Team plans" });

        await Assertions.Expect(table.GetByRole(AriaRole.Row, new() { Name = "Audit log" }))
            .ToContainTextAsync("Not included Included Included");
        await Assertions.Expect(table.GetByRole(AriaRole.Columnheader, new() { Name = "Pro" }).First).ToHaveCSSAsync("position", "sticky");
        await Assertions.Expect(table.GetByRole(AriaRole.Cell, new() { Name = "Unlimited" }).First).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task PricingTable_stacks_the_tiers_on_a_phone(string mode)
    {
        await Page.SetViewportSizeAsync(390, 800);
        await Open(mode, "pricing-table");
        var cell = Page.Locator("td[data-label=Pro]").First;

        await Assertions.Expect(cell).ToHaveCSSAsync("display", "flex");
        await Assertions.Expect(Page.Locator("thead th").Nth(1)).ToHaveCSSAsync("display", "block");
        await Assertions.Expect(Page.Locator("thead th").First).ToBeHiddenAsync();
        Assert.True(await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth <= window.innerWidth"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task BlogPost_has_one_link_and_the_whole_card_follows_it(string mode)
    {
        await Open(mode, "blog-posts");
        var first = Page.Locator("article[data-color]").First;

        Assert.Equal(1, await first.Locator("a").CountAsync());
        await Assertions.Expect(first.Locator("time")).ToHaveAttributeAsync("datetime", "2026-11-12");
        // The format follows the culture of the server (the invariant one here), not a fixed order.
        await Assertions.Expect(first.Locator("time")).ToHaveTextAsync(new System.Text.RegularExpressions.Regex(@"^(November 12, 2026|12 November 2026)$"));

        // The title's link covers the card, so the click on the description lands on it (hence Force: Playwright sees an overlay).
        await first.GetByText("How Blux keeps one markup").ClickAsync(new() { Force = true });
        await Assertions.Expect(Page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex("/docs/render-modes"));
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task BlogPost_link_is_reached_with_the_keyboard_and_shows_a_focus_outline(string mode)
    {
        await Open(mode, "blog-posts");
        var link = Page.Locator("article[data-color]").First.GetByRole(AriaRole.Link);

        await link.FocusAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Page.Keyboard.PressAsync("Tab");

        await Assertions.Expect(link).ToBeFocusedAsync();
        await Assertions.Expect(Page.Locator("article[data-color]").First).ToHaveCSSAsync("outline-style", "solid");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task BlogPost_with_several_authors_names_the_group(string mode)
    {
        await Open(mode, "blog-posts");

        await Assertions.Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Grace Hopper, Alan Turing" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task ChangelogVersions_list_versions_with_dates_and_an_indicator(string mode)
    {
        await Open(mode, "changelog-versions");
        var versions = Page.Locator("article[data-color]");

        await Assertions.Expect(versions).ToHaveCountAsync(3);
        await Assertions.Expect(versions.First.GetByRole(AriaRole.Heading, new() { Name = "v1.2.0" })).ToBeVisibleAsync();
        await Assertions.Expect(versions.First.Locator("time")).ToHaveAttributeAsync("datetime", "2026-11-18");
        await Assertions.Expect(versions.First.Locator("[data-slot=indicator]")).ToHaveAttributeAsync("aria-hidden", "true");
        await Assertions.Expect(versions.First.GetByRole(AriaRole.Link, new() { Name = "v1.2.0" })).ToHaveAttributeAsync("href", "/docs");
    }
}
