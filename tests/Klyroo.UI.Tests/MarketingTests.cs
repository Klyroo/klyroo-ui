using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

public class MarketingTests : KlyrooTestContext
{
    private static readonly KlyAuthField[] SignInFields =
    [
        new("email", "email") { Label = "Email", Required = true },
        new("password", "password") { Label = "Password", Required = true },
        new("remember", "checkbox") { Label = "Remember me" },
    ];

    private IRenderedComponent<KlyAuthForm> RenderAuth(Action<ComponentParameterCollectionBuilder<KlyAuthForm>>? configure = null, IReadOnlyList<KlyAuthField>? fields = null) =>
        Render<KlyAuthForm>(p =>
        {
            p.Add(f => f.Fields, fields ?? SignInFields).Add(f => f.FormName, "sign-in").Add(f => f.Title, "Welcome back");
            configure?.Invoke(p);
        });

    // AuthForm

    [Fact]
    public void AuthForm_renders_a_named_form_with_a_heading_labelled_controls_and_posted_names()
    {
        var cut = RenderAuth();

        Assert.Equal("Welcome back", cut.Find("form").GetAttribute("aria-label"));
        Assert.Equal("H2", cut.Find("h2").TagName);
        Assert.Equal("sign-in", cut.Find("input[name='Posted[__form]']").GetAttribute("value"));
        var email = cut.Find("input[type=email]");
        Assert.Equal("Posted[email]", email.GetAttribute("name"));
        Assert.True(email.HasAttribute("required"));
        Assert.Equal(email.Id, cut.FindAll("label").First(l => l.TextContent.Contains("Email")).GetAttribute("for"));
        Assert.Equal("Posted[remember]", cut.Find("input[type=checkbox]").GetAttribute("name"));
        Assert.Equal("submit", cut.Find("button[type=submit]").GetAttribute("type"));
        Assert.Equal("Continue", cut.Find("button[type=submit]").TextContent.Trim());
    }

    [Fact]
    public void AuthForm_password_has_a_toggle_button_with_a_stable_name_and_a_pressed_state()
    {
        var cut = RenderAuth();

        var button = cut.Find("kly-password button[data-slot=toggle]");
        Assert.Equal("Show password", button.GetAttribute("aria-label"));
        Assert.Equal("false", button.GetAttribute("aria-pressed"));
        Assert.Equal("button", button.GetAttribute("type"));
        Assert.Equal("password", cut.Find("kly-password input").GetAttribute("type"));
        Assert.Equal("current-password", cut.Find("kly-password input").GetAttribute("autocomplete"));
        Assert.Equal("true", button.QuerySelector("svg")!.GetAttribute("aria-hidden"));
    }

    [Fact]
    public void AuthForm_submit_with_empty_required_fields_shows_errors_and_marks_them_invalid()
    {
        var invalid = false;
        var cut = RenderAuth(p => p.Add(f => f.OnInvalidSubmit, (IReadOnlyDictionary<string, string?> _) => invalid = true));

        cut.Find("form").Submit();

        Assert.True(invalid);
        Assert.Equal("Email is required.", cut.Find("p[data-slot=error]").TextContent.Trim());
        Assert.Equal(2, cut.FindAll("p[data-slot=error]").Count);
        Assert.Equal("true", cut.Find("input[type=email]").GetAttribute("aria-invalid"));
        Assert.Contains("-error", cut.Find("input[type=email]").GetAttribute("aria-describedby"));
        Assert.Equal("alert", cut.Find("div[data-slot=error]").GetAttribute("role"));
    }

    [Fact]
    public void AuthForm_checks_the_email_format_and_then_the_custom_validator()
    {
        var cut = RenderAuth(p => p.Add(f => f.Validate, values =>
            values["password"] is { Length: < 8 } ? new Dictionary<string, string> { ["password"] = "Too short." } : null));

        cut.Find("input[type=email]").Change("nope");
        cut.Find("kly-password input").Change("longenough1");
        cut.Find("form").Submit();
        Assert.Equal("Enter a valid email address.", cut.Find("p[data-slot=error]").TextContent.Trim());

        cut.Find("input[type=email]").Change("ada@example.com");
        cut.Find("kly-password input").Change("short");
        cut.Find("form").Submit();
        Assert.Equal("Too short.", cut.Find("p[data-slot=error]").TextContent.Trim());
    }

    [Fact]
    public void AuthForm_rejects_a_pathological_email_quickly_instead_of_timing_out()
    {
        var cut = RenderAuth();
        cut.Find("input[type=email]").Change("a@" + new string('.', 50_000) + "@");
        cut.Find("kly-password input").Change("longenough1");

        var watch = System.Diagnostics.Stopwatch.StartNew();
        cut.Find("form").Submit();
        watch.Stop();

        Assert.Equal("Enter a valid email address.", cut.Find("p[data-slot=error]").TextContent.Trim());
        // The guard is the 254-character cap before the regex (and the regex timeout behind it): without them this input
        // backtracks for seconds. The bound covers the render of the form too, so it stays loose enough for a loaded machine.
        Assert.True(watch.ElapsedMilliseconds < 1000, $"validation took {watch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task AuthForm_only_keeps_posted_values_of_declared_fields()
    {
        IReadOnlyDictionary<string, string?>? submitted = null;
        var form = new KlyAuthForm();
        typeof(KlyAuthForm).GetProperty("Posted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(form, new Dictionary<string, string?>
            {
                ["__form"] = "sign-in",
                ["email"] = "ada@example.com",
                ["password"] = "longenough1",
                ["isAdmin"] = "true",
            });
        ComponentFactories.Add(t => t == typeof(KlyAuthForm), _ => form);

        var cut = Render<KlyAuthForm>(p => p.Add(f => f.Fields, SignInFields).Add(f => f.FormName, "sign-in")
            .Add(f => f.OnValidSubmit, (IReadOnlyDictionary<string, string?> values) => submitted = values));
        await cut.InvokeAsync(() => (Task)form.GetType().GetMethod("HandleSubmit", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(form, [null])!);

        Assert.Equal("ada@example.com", cut.Find("input[type=email]").GetAttribute("value"));
        Assert.NotNull(submitted);
        Assert.Equal("ada@example.com", submitted!["email"]);
        Assert.DoesNotContain("isAdmin", submitted.Keys);
    }

    [Fact]
    public void AuthForm_separator_comes_from_the_strings()
    {
        using var localized = new LocalizedContext();
        var cut = localized.Render<KlyAuthForm>(p => p.Add(f => f.Fields, SignInFields)
            .Add(f => f.Providers, [new KlyAuthProvider("GitHub", "https://example.com/o")]));

        var markup = cut.Markup.Replace(" ", "").Replace("\n", "");
        Assert.Contains(">ou</span>", markup);
        Assert.DoesNotContain(">or</span>", markup);
    }

    [Fact]
    public void AuthForm_valid_submit_hands_over_the_values_and_clears_the_errors()
    {
        IReadOnlyDictionary<string, string?>? submitted = null;
        var cut = RenderAuth(p => p.Add(f => f.OnValidSubmit, (IReadOnlyDictionary<string, string?> values) => submitted = values));
        cut.Find("form").Submit();
        Assert.Equal(2, cut.FindAll("p[data-slot=error]").Count);

        cut.Find("input[type=email]").Change("ada@example.com");
        cut.Find("kly-password input").Change("longenough1");
        cut.Find("input[type=checkbox]").Change(true);
        cut.Find("form").Submit();

        Assert.NotNull(submitted);
        Assert.Equal("ada@example.com", submitted!["email"]);
        Assert.Equal("longenough1", submitted["password"]);
        Assert.Equal("true", submitted["remember"]);
        Assert.Empty(cut.FindAll("p[data-slot=error]"));
    }

    [Fact]
    public async Task AuthForm_is_busy_while_the_submit_handler_runs()
    {
        var gate = new TaskCompletionSource();
        var cut = RenderAuth(p => p.Add(f => f.OnValidSubmit, (IReadOnlyDictionary<string, string?> _) => gate.Task));
        cut.Find("input[type=email]").Change("ada@example.com");
        cut.Find("kly-password input").Change("longenough1");

        var submit = cut.InvokeAsync(() => cut.Find("form").Submit());
        cut.WaitForAssertion(() =>
        {
            Assert.Equal("true", cut.Find("form").GetAttribute("aria-busy"));
            Assert.True(cut.Find("fieldset").HasAttribute("disabled"));
            Assert.Equal("true", cut.Find("button[type=submit]").GetAttribute("aria-busy"));
        });

        gate.SetResult();
        await submit;
        cut.WaitForAssertion(() => Assert.Null(cut.Find("form").GetAttribute("aria-busy")));
    }

    [Fact]
    public void AuthForm_providers_are_links_and_a_separator_names_the_split()
    {
        var cut = RenderAuth(p => p
            .Add(f => f.Providers, [new KlyAuthProvider("Continue with GitHub", "/auth/github") { Icon = "code" }])
            .Add(f => f.Error, "Wrong password."));

        var link = cut.Find("a[href='/auth/github']");
        Assert.Contains("Continue with GitHub", link.TextContent);
        Assert.Equal("or", cut.Find("[role=separator]").GetAttribute("aria-label"));
        Assert.Contains("Wrong password.", cut.Find("div[data-slot=error]").TextContent);
    }

    [Fact]
    public void AuthForm_select_otp_and_checkbox_fields_post_under_their_names()
    {
        var cut = RenderAuth(fields:
        [
            new KlyAuthField("role", "select") { Label = "Role", Items = [new("dev", "Developer")], Value = "dev" },
            new KlyAuthField("code", "otp") { Label = "Code", Length = 4 },
            new KlyAuthField("terms", "checkbox") { Label = "I agree", Required = true },
        ]);

        Assert.Equal("dev", cut.Find("input[type=hidden][name='Posted[role]']").GetAttribute("value"));
        Assert.Equal("Posted[code]", cut.Find("kly-pin-input input[type=hidden]").GetAttribute("name"));
        Assert.Equal(4, cut.FindAll("kly-pin-input input[data-slot=cell]").Count);
        Assert.Equal("Posted[terms]", cut.Find("input[type=checkbox]").GetAttribute("name"));

        cut.Find("form").Submit();

        Assert.Contains("I agree is required.", cut.Find("p[data-slot=error]").TextContent);
        Assert.Single(cut.FindAll("p[data-slot=error]"));
    }

    [Fact]
    public void AuthForm_incomplete_code_is_reported_and_server_field_errors_are_shown()
    {
        var cut = RenderAuth(p => p.Add(f => f.FieldErrors, new Dictionary<string, string> { ["code"] = "That code is not right." }),
            fields: [new KlyAuthField("code", "otp") { Label = "Code", Length = 6 }]);
        Assert.Equal("That code is not right.", cut.Find("p[data-slot=error]").TextContent.Trim());

        cut.Find("kly-pin-input").TriggerEvent("onklychange", new KlyChangeEventArgs { Value = "12", Sequence = 1 });
        cut.Find("form").Submit();

        Assert.Equal("Enter all 6 characters.", cut.Find("p[data-slot=error]").TextContent.Trim());
    }

    [Fact]
    public void AuthForm_does_not_echo_a_password_in_static_rendering()
    {
        using var staticContext = new StaticContext();
        var cut = staticContext.Render<KlyAuthForm>(p => p.Add(f => f.Fields,
            [new KlyAuthField("password", "password") { Label = "Password", Value = "hunter2" }]));

        Assert.False(cut.Find("input[type=password]").HasAttribute("value"));
    }

    [Fact]
    public void AuthForm_texts_come_from_the_strings()
    {
        using var localized = new LocalizedContext();
        var cut = localized.Render<KlyAuthForm>(p => p.Add(f => f.Fields, SignInFields));

        Assert.Equal("Afficher le mot de passe", cut.Find("kly-password button").GetAttribute("aria-label"));
        Assert.Equal("Continuer", cut.Find("button[type=submit]").TextContent.Trim());
    }

    // PricingPlan and PricingPlans

    [Fact]
    public void PricingPlan_renders_title_price_features_and_a_link_button()
    {
        var cut = Render<KlyPricingPlan>(p => p
            .Add(c => c.Title, "Pro").Add(c => c.Price, "$29").Add(c => c.BillingCycle, "/month").Add(c => c.Description, "For teams.")
            .Add(c => c.Features, ["Unlimited projects", new KlyPricingFeature("Fast") { Icon = "zap" }])
            .Add(c => c.ButtonLabel, "Get Pro").Add(c => c.ButtonHref, "/buy"));

        Assert.Equal("H3", cut.Find("h3").TagName);
        Assert.Equal("Pro", cut.Find("h3").TextContent.Trim());
        Assert.Equal("$29", cut.Find("span.tabular-nums").TextContent.Trim());
        Assert.Equal("/month", cut.Find("p span:last-child").TextContent.Trim());
        var items = cut.FindAll("ul > li");
        Assert.Equal(["Unlimited projects", "Fast"], items.Select(i => i.TextContent.Trim()));
        Assert.All(items, i => Assert.Equal("true", i.QuerySelector("svg")!.GetAttribute("aria-hidden")));
        Assert.Equal("/buy", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void PricingPlan_discount_strikes_the_original_and_spells_both_prices_out()
    {
        var cut = Render<KlyPricingPlan>(p => p.Add(c => c.Price, "$120").Add(c => c.Discount, "$96"));

        var original = cut.Find("del");
        Assert.Contains("Original price", original.TextContent);
        Assert.Contains("$120", original.TextContent);
        Assert.Equal("sr-only", original.QuerySelector("span")!.GetAttribute("class"));
        var current = cut.Find("p > span");
        Assert.Contains("Now", current.TextContent);
        Assert.Contains("$96", current.TextContent);
    }

    [Fact]
    public void PricingPlan_heading_level_highlight_scale_and_solid_variant_follow_parameters()
    {
        var cut = Render<KlyPricingPlan>(p => p.Add(c => c.Title, "Team").Add(c => c.HeadingLevel, 2).Add(c => c.Highlight, true)
            .Add(c => c.Scale, true).Add(c => c.Variant, KlyVariant.Solid).Add(c => c.ButtonLabel, "Contact").Add(c => c.ButtonHref, "/c"));

        Assert.Equal("H2", cut.Find("h2").TagName);
        var root = cut.Find("[data-color]");
        Assert.Contains("ring-2", root.ClassList);
        Assert.Contains("lg:scale-105", root.ClassList);
        Assert.Contains("bg-(--kly-color)", root.ClassList);
        // The primary button would vanish on a primary background.
        Assert.Equal("neutral", cut.Find("a").GetAttribute("data-color"));
    }

    [Fact]
    public void PricingPlan_button_click_waits_for_interactivity()
    {
        var clicked = 0;
        var cut = Render<KlyPricingPlan>(p => p.Add(c => c.ButtonLabel, "Choose").Add(c => c.OnButtonClick, () => clicked++));

        cut.Find("button").Click();

        Assert.Equal(1, clicked);
        using var staticContext = new StaticContext();
        var inert = staticContext.Render<KlyPricingPlan>(p => p.Add(c => c.ButtonLabel, "Choose").Add(c => c.OnButtonClick, () => { }));
        Assert.True(inert.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void PricingPlans_lay_plans_out_by_orientation()
    {
        var grid = Render<KlyPricingPlans>(p => p.AddChildContent("<p>x</p>"));
        Assert.Contains("grid", grid.Find("div").ClassList);

        var stack = Render<KlyPricingPlans>(p => p.Add(c => c.Orientation, KlyOrientation.Vertical).AddChildContent("<p>x</p>"));
        Assert.Contains("flex-col", stack.Find("div").ClassList);
    }

    // PricingTable

    private static readonly KlyPricingTier[] Tiers =
    [
        new("starter", "Starter") { Price = "$0", ButtonLabel = "Start", ButtonHref = "/s" },
        new("pro", "Pro") { Price = "$29", Highlight = true, Badge = "Popular" },
    ];

    private static readonly KlyPricingSection[] Sections =
    [
        new("Usage",
        [
            new("Projects", 3, "Unlimited"),
            new("SSO", false, true),
            new("Audit log", true, null) { ByTier = new Dictionary<string, KlyPricingTableValue> { ["pro"] = true } },
        ]),
        new("Support", [new("Forum", true, true)]),
    ];

    [Fact]
    public void PricingTable_is_a_real_table_with_a_caption_and_headers_scoped_to_their_cells()
    {
        var cut = Render<KlyPricingTable>(p => p.Add(c => c.Tiers, Tiers).Add(c => c.Sections, Sections).Add(c => c.Caption, "Compare plans"));

        Assert.Equal("Compare plans", cut.Find("caption").TextContent);
        Assert.Contains("sr-only", cut.Find("caption").ClassList);
        var columns = cut.FindAll("thead th[scope=col]");
        Assert.Equal(3, columns.Count);
        Assert.Equal("Features", columns[0].QuerySelector(".sr-only")!.TextContent);
        Assert.Contains("Starter", columns[1].TextContent);
        Assert.True(columns[2].HasAttribute("data-highlight"));
        Assert.False(columns[1].HasAttribute("data-highlight"));
        Assert.Equal(2, cut.FindAll("tbody").Count);
        Assert.Equal("rowgroup", cut.Find("tbody th").GetAttribute("scope"));
        Assert.Equal("3", cut.Find("tbody th").GetAttribute("colspan"));
        Assert.Equal(["Projects", "SSO", "Audit log", "Forum"], cut.FindAll("tbody th[scope=row]").Select(h => h.TextContent.Trim()));
        Assert.Equal("/s", cut.Find("thead a").GetAttribute("href"));
    }

    [Fact]
    public void PricingTable_values_convert_from_double_and_float_and_format_with_the_culture()
    {
        KlyPricingTableValue half = 2.5;
        KlyPricingTableValue tenth = 0.1f;
        Assert.Equal("2.5", half.ToString());
        Assert.Equal("0.1", tenth.ToString());

        var original = System.Globalization.CultureInfo.CurrentCulture;
        var originalUi = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("fr-FR");
            KlyPricingSection[] sections = [new("Usage", [new("Rate", 2.5, 0.1f), new("Mixed", 3, 4.5m)])];
            var cut = Render<KlyPricingTable>(p => p.Add(c => c.Tiers, Tiers).Add(c => c.Sections, sections));

            var cells = cut.FindAll("tbody td").Select(c => c.TextContent.Trim()).ToList();
            Assert.Equal(["2,5", "0,1", "3", "4,5"], cells);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = original;
            System.Globalization.CultureInfo.CurrentUICulture = originalUi;
        }
    }

    [Fact]
    public void PricingTable_cells_say_included_or_not_included_in_text_and_show_values()
    {
        var cut = Render<KlyPricingTable>(p => p.Add(c => c.Tiers, Tiers).Add(c => c.Sections, Sections));

        var rows = cut.FindAll("tbody tr[class*=row], tbody tr").Where(r => r.QuerySelector("th[scope=row]") is not null).ToList();
        var projects = rows[0].QuerySelectorAll("td");
        Assert.Equal(["3", "Unlimited"], projects.Select(c => c.TextContent.Trim()));
        Assert.Equal(["Starter", "Pro"], projects.Select(c => c.GetAttribute("data-label")));

        var sso = rows[1].QuerySelectorAll("td");
        Assert.Equal("Not included", sso[0].QuerySelector(".sr-only")!.TextContent);
        Assert.Equal("true", sso[0].QuerySelector("svg")!.GetAttribute("aria-hidden"));
        Assert.Equal("Included", sso[1].QuerySelector(".sr-only")!.TextContent);

        // ByTier wins for the tier it names; the position still serves the others.
        var audit = rows[2].QuerySelectorAll("td");
        Assert.Equal("Included", audit[0].QuerySelector(".sr-only")!.TextContent);
        Assert.Equal("Included", audit[1].QuerySelector(".sr-only")!.TextContent);
    }

    [Fact]
    public void PricingTable_sticky_header_is_optional()
    {
        var sticky = Render<KlyPricingTable>(p => p.Add(c => c.Tiers, Tiers).Add(c => c.Sections, Sections));
        Assert.Contains("md:sticky", sticky.Find("thead th[scope=col]:nth-child(2)").ClassList);

        var plain = Render<KlyPricingTable>(p => p.Add(c => c.Tiers, Tiers).Add(c => c.Sections, Sections).Add(c => c.Sticky, false));
        Assert.DoesNotContain("md:sticky", plain.Find("thead th[scope=col]:nth-child(2)").ClassList);
    }

    // BlogPost and BlogPosts

    [Fact]
    public void BlogPost_has_exactly_one_link_through_its_title()
    {
        var cut = Render<KlyBlogPost>(p => p.Add(c => c.Title, "Four modes").Add(c => c.Description, "How.").Add(c => c.Href, "/blog/four-modes")
            .Add(c => c.Badge, "Engineering").Add(c => c.Image, "/img/a.png").Add(c => c.Authors, [new KlyPostAuthor("Ada") { Href = "/u/ada" }]));

        var links = cut.FindAll("a");
        Assert.Single(links);
        Assert.Equal("/blog/four-modes", links[0].GetAttribute("href"));
        Assert.Equal("Four modes", links[0].TextContent);
        Assert.Equal("H3", links[0].ParentElement!.TagName);
        Assert.Empty(cut.FindAll("button"));
        Assert.Equal("", cut.Find("img").GetAttribute("alt"));
        Assert.Contains("after:absolute", links[0].ClassList);
        Assert.Contains("relative", cut.Find("article").ClassList);
    }

    [Fact]
    public void BlogPost_without_href_has_no_link_and_keeps_author_links()
    {
        var cut = Render<KlyBlogPost>(p => p.Add(c => c.Title, "Notes").Add(c => c.Authors, [new KlyPostAuthor("Ada") { Href = "/u/ada" }]));

        Assert.Equal("/u/ada", cut.Find("a").GetAttribute("href"));
        Assert.Equal("Notes", cut.Find("h3").TextContent.Trim());
    }

    [Fact]
    public void BlogPost_date_is_a_time_element_formatted_for_the_culture()
    {
        var date = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            var english = Render<KlyBlogPost>(p => p.Add(c => c.Date, date));
            Assert.Equal("2026-11-12", english.Find("time").GetAttribute("datetime"));
            Assert.Equal("November 12, 2026", english.Find("time").TextContent);

            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo("fr-FR");
            var french = Render<KlyBlogPost>(p => p.Add(c => c.Date, date));
            Assert.Equal("12 novembre 2026", french.Find("time").TextContent);

            var custom = Render<KlyBlogPost>(p => p.Add(c => c.Date, date).Add(c => c.DateFormat, "yyyy/MM/dd"));
            Assert.Equal("2026/11/12", custom.Find("time").TextContent);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void BlogPost_several_authors_become_a_named_avatar_group()
    {
        var cut = Render<KlyBlogPost>(p => p.Add(c => c.Title, "T").Add(c => c.Authors, [new KlyPostAuthor("Ada"), new KlyPostAuthor("Grace")]));

        var group = cut.Find("[role=group]");
        Assert.Equal("Ada, Grace", group.GetAttribute("aria-label"));
        Assert.Equal(["Ada", "Grace"], group.QuerySelectorAll("[role=img]").Select(a => a.GetAttribute("aria-label")));
    }

    [Fact]
    public void BlogPost_heading_level_orientation_and_variant_follow_parameters()
    {
        var cut = Render<KlyBlogPost>(p => p.Add(c => c.Title, "T").Add(c => c.HeadingLevel, 2).Add(c => c.Orientation, KlyOrientation.Horizontal)
            .Add(c => c.Variant, BlogPostVariant.Naked).Add(c => c.Image, "/a.png").Add(c => c.ImageAlt, "A chart"));

        Assert.Equal("H2", cut.Find("h2").TagName);
        Assert.Contains("md:flex-row", cut.Find("article").ClassList);
        Assert.Contains("bg-transparent", cut.Find("article").ClassList);
        Assert.Equal("A chart", cut.Find("img").GetAttribute("alt"));
    }

    [Fact]
    public void BlogPosts_is_a_grid_or_a_stack()
    {
        Assert.Contains("grid", Render<KlyBlogPosts>().Find("div").ClassList);
        Assert.Contains("flex-col", Render<KlyBlogPosts>(p => p.Add(c => c.Orientation, KlyOrientation.Vertical)).Find("div").ClassList);
    }

    // ChangelogVersion and ChangelogVersions

    [Fact]
    public void ChangelogVersion_renders_version_date_badge_rich_content_and_authors()
    {
        var cut = Render<KlyChangelogVersion>(p => p.Add(c => c.Title, "v1.2.0").Add(c => c.Badge, "Latest").Add(c => c.Description, "Summary.")
            .Add(c => c.Date, new DateTimeOffset(2026, 11, 18, 0, 0, 0, TimeSpan.Zero))
            .Add(c => c.Authors, [new KlyPostAuthor("Ada")]).AddChildContent("<ul><li>Fixed</li></ul>"));

        Assert.Equal("v1.2.0", cut.Find("h3").TextContent.Trim());
        Assert.Equal("2026-11-18", cut.Find("time").GetAttribute("datetime"));
        Assert.Equal("Latest", cut.Find("span[data-color=primary]").TextContent.Trim());
        Assert.Equal("Fixed", cut.Find("[data-slot=body] li").TextContent);
        Assert.Contains("Ada", cut.Find("[data-slot=authors]").TextContent);
        Assert.Equal("true", cut.Find("[data-slot=indicator]").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void ChangelogVersion_indicator_can_be_hidden_and_the_title_can_link()
    {
        var cut = Render<KlyChangelogVersion>(p => p.Add(c => c.Title, "v2").Add(c => c.Indicator, false).Add(c => c.Href, "/v2"));

        Assert.Empty(cut.FindAll("[data-slot=indicator]"));
        Assert.Equal("/v2", cut.Find("h3 a").GetAttribute("href"));
    }

    [Fact]
    public void BlogPost_and_ChangelogVersion_format_the_date_in_the_given_culture_whatever_the_current_one_is()
    {
        var date = new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero);
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var post = Render<KlyBlogPost>(p => p.Add(c => c.Date, date).Add(c => c.Culture, new CultureInfo("fr-FR")));
            var version = Render<KlyChangelogVersion>(p => p.Add(c => c.Date, date).Add(c => c.Culture, new CultureInfo("fr-FR")));

            Assert.Equal("12 novembre 2026", post.Find("time").TextContent);
            Assert.Equal("12 novembre 2026", version.Find("time").TextContent);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void BlogPost_and_ChangelogVersion_links_that_open_a_new_tab_say_so_to_screen_readers()
    {
        var post = Render<KlyBlogPost>(p => p.Add(c => c.Title, "Post").Add(c => c.Href, "https://example.com/").Add(c => c.Target, "_blank"));
        var version = Render<KlyChangelogVersion>(p => p.Add(c => c.Title, "v2").Add(c => c.Href, "https://example.com/").Add(c => c.Target, "_blank"));

        Assert.Equal("(opens in a new tab)", post.Find("h3 a span.sr-only").TextContent.Trim());
        Assert.Equal("(opens in a new tab)", version.Find("h3 a span.sr-only").TextContent.Trim());
    }

    [Fact]
    public void ChangelogVersions_wraps_the_versions()
    {
        var cut = Render<KlyChangelogVersions>(p => p.AddChildContent("<article>one</article>"));

        Assert.Single(cut.FindAll("div > article"));
    }

    // Overrides

    [Fact]
    public void Ui_and_Class_reach_the_marketing_slots()
    {
        var cut = Render<KlyPricingPlan>(p => p.Add(c => c.Title, "Pro").Add(c => c.Class, "w-80")
            .Add(c => c.Ui, new PricingPlanUi { Title = "text-2xl" }));

        Assert.Contains("w-80", cut.Find("[data-color]").ClassList);
        Assert.Contains("text-2xl", cut.Find("h3").ClassList);
    }

    private sealed class StaticContext : BunitContext
    {
        public StaticContext()
        {
            Services.AddKlyrooUi();
            SetRendererInfo(new RendererInfo("Static", false));
        }
    }

    private sealed class LocalizedContext : BunitContext
    {
        public LocalizedContext()
        {
            Services.AddKlyrooUi(o =>
            {
                o.Locale = KlyLocales.Extend(KlyLocales.En, messages: m => m with { AuthFormShowPassword = "Afficher le mot de passe", AuthFormSubmit = "Continuer", AuthFormSeparator = "ou" });
            });
            SetRendererInfo(new RendererInfo("Server", true));
        }
    }
}
