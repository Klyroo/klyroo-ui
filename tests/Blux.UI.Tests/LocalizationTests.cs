using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

/// <summary>The locale model: built-in locales, resolution order, direction, culture formatting.</summary>
public partial class LocalizationTests : BluxTestContext
{
    private static readonly PropertyInfo[] MessageProperties = typeof(BxMessages).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.PropertyType == typeof(string)).ToArray();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex Placeholder();

    private static string[] Placeholders(string text) => Placeholder().Matches(text).Select(m => m.Value).Order().ToArray();

    public static TheoryData<string> LocaleCodes()
    {
        var data = new TheoryData<string>();
        foreach (var locale in BxLocales.All) data.Add(locale.Code);
        return data;
    }

    private static BxLocale ByCode(string code) => BxLocales.All.Single(l => l.Code == code);

    // ---- The catalogue

    [Fact]
    public void All_holds_the_twelve_built_in_locales_once_each()
    {
        Assert.Equal(["en", "fr", "de", "es", "it", "pt", "pt-BR", "nl", "ja", "zh-CN", "ar", "ru"], BxLocales.All.Select(l => l.Code));
        Assert.Equal(12, BxLocales.All.Select(l => l.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(BxLocales.All, l => Assert.False(string.IsNullOrWhiteSpace(l.Name)));
    }

    [Fact]
    public void Only_Arabic_is_right_to_left()
    {
        Assert.Equal(["ar"], BxLocales.All.Where(l => l.Dir == BxDirection.Rtl).Select(l => l.Code));
        Assert.Equal("rtl", BxLocales.Ar.DirAttribute);
        Assert.Equal("ltr", BxLocales.Fr.DirAttribute);
    }

    [Theory]
    [MemberData(nameof(LocaleCodes))]
    public void Every_message_of_every_locale_is_filled_and_keeps_the_placeholders_of_English(string code)
    {
        var locale = ByCode(code);
        Assert.NotEmpty(MessageProperties);
        foreach (var property in MessageProperties)
        {
            var english = (string)property.GetValue(BxLocales.En.Messages)!;
            var text = (string?)property.GetValue(locale.Messages);
            Assert.False(string.IsNullOrWhiteSpace(text), $"{code}: {property.Name} is empty");
            Assert.True(Placeholders(english).SequenceEqual(Placeholders(text!)), $"{code}: {property.Name} has the placeholders {string.Join(",", Placeholders(text!))}, English has {string.Join(",", Placeholders(english))}");
        }
    }

    [Theory]
    [MemberData(nameof(LocaleCodes))]
    public void A_translated_locale_translates_nearly_every_message(string code)
    {
        if (code == "en") return;
        var same = MessageProperties
            .Where(p => (string)p.GetValue(BxLocales.En.Messages)! == (string)p.GetValue(ByCode(code).Messages)!)
            .Select(p => p.Name).ToList();

        // Words that are the same in several languages (Menu, Code, Minimum, Ctrl, AM/PM...) stay; a locale that is mostly English does not.
        Assert.True(same.Count <= 20, $"{code} keeps {same.Count} English messages: {string.Join(", ", same)}");
    }

    [Fact]
    public void Messages_are_an_immutable_record_so_with_copies_them()
    {
        var changed = BxLocales.En.Messages with { Close = "Dismiss" };

        Assert.Equal("Dismiss", changed.Close);
        Assert.Equal("Close", BxLocales.En.Messages.Close);
        Assert.Equal(BxLocales.En.Messages.NoData, changed.NoData);
    }

    // ---- Extend

    [Fact]
    public void Extend_overrides_what_is_asked_and_keeps_the_rest()
    {
        var australian = BxLocales.Extend(BxLocales.En, code: "en-AU", name: "English (Australia)",
            messages: m => m with { SearchCommands = "Search..." });

        Assert.Equal("en-AU", australian.Code);
        Assert.Equal("English (Australia)", australian.Name);
        Assert.Equal(BxDirection.Ltr, australian.Dir);
        Assert.Equal("Search...", australian.Messages.SearchCommands);
        Assert.Equal(BxLocales.En.Messages.Close, australian.Messages.Close);
        Assert.Equal("en", BxLocales.En.Code);
        Assert.NotEqual("Search...", BxLocales.En.Messages.SearchCommands);
    }

    [Fact]
    public void Extend_without_overrides_is_a_copy_and_can_change_the_direction()
    {
        Assert.Equal(BxLocales.Fr, BxLocales.Extend(BxLocales.Fr));
        Assert.Equal(BxDirection.Rtl, BxLocales.Extend(BxLocales.Fr, code: "fr-x", dir: BxDirection.Rtl).Dir);
    }

    [Fact]
    public void A_locale_can_be_created_from_scratch()
    {
        var klingon = new BxLocale("tlh", "tlhIngan Hol", BxDirection.Ltr, new BxMessages { Close = "Qaw" });

        Assert.Equal("Qaw", klingon.Messages.Close);
        Assert.Equal(CultureInfo.InvariantCulture, new BxLocale("not a culture!", "?", BxDirection.Ltr, klingon.Messages).Culture);
        Assert.Equal("fr", BxLocales.Fr.Culture.Name);
        Assert.Equal("pt-BR", BxLocales.PtBr.Culture.Name);
    }

    // ---- Resolution of a culture

    [Theory]
    [InlineData("en", "en")]
    [InlineData("en-AU", "en")]
    [InlineData("fr-FR", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("de-AT", "de")]
    [InlineData("es-MX", "es")]
    [InlineData("pt", "pt")]
    [InlineData("pt-PT", "pt")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-Hans-CN", "zh-CN")]
    [InlineData("zh-TW", "zh-CN")]
    [InlineData("ja-JP", "ja")]
    [InlineData("ar-EG", "ar")]
    [InlineData("ru-RU", "ru")]
    [InlineData("nl-BE", "nl")]
    [InlineData("sv-SE", "en")]
    [InlineData("", "en")]
    public void A_culture_picks_the_exact_code_then_its_parents_then_its_language_then_English(string culture, string expected)
    {
        Assert.Equal(expected, BxLocales.Resolve(CultureInfo.GetCultureInfo(culture)).Code);
    }

    [Fact]
    public void Resolve_chooses_among_the_given_locales_and_falls_back_to_the_first_without_English()
    {
        BxLocale[] two = [BxLocales.Fr, BxLocales.De];

        Assert.Equal("de", BxLocales.Resolve(CultureInfo.GetCultureInfo("de-DE"), two).Code);
        Assert.Equal("fr", BxLocales.Resolve(CultureInfo.GetCultureInfo("ja-JP"), two).Code);
        Assert.Equal("en", BxLocales.Resolve(CultureInfo.GetCultureInfo("ja-JP"), [BxLocales.Fr, BxLocales.En]).Code);
    }

    [Fact]
    public void Resolve_reads_the_current_UI_culture_by_default()
    {
        using var _ = new CultureScope("es-ES");

        Assert.Equal("es", BxLocales.Resolve().Code);
    }

    // ---- Resolution order of a component

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _ui = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _ui;
        }
    }

    private sealed class Ctx(Action<BluxOptions>? configure = null) : BluxTestContext(configure);

    private static string CloseLabel(IRenderedComponent<BxAlert> cut) => cut.Find("button[aria-label]").GetAttribute("aria-label")!;

    private static void WithClose(ComponentParameterCollectionBuilder<BxAlert> p) => p.Add(a => a.Title, "T").Add(a => a.Close, true);

    [Fact]
    public void Without_anything_set_the_locale_is_the_one_of_the_UI_culture()
    {
        using var _ = new CultureScope("fr-FR");

        Assert.Equal("Fermer", CloseLabel(Render<BxAlert>(WithClose)));
    }

    [Fact]
    public void An_unknown_culture_falls_back_to_English()
    {
        using var _ = new CultureScope("sv-SE");

        Assert.Equal("Close", CloseLabel(Render<BxAlert>(WithClose)));
    }

    [Fact]
    public void The_option_locale_beats_the_culture()
    {
        using var _ = new CultureScope("fr-FR");
        using var ctx = new Ctx(o => o.Locale = BxLocales.De);

        Assert.Equal("Schließen", CloseLabel(ctx.Render<BxAlert>(WithClose)));
    }

    [Fact]
    public void A_cascading_locale_beats_the_option_and_the_culture()
    {
        using var _ = new CultureScope("fr-FR");
        using var ctx = new Ctx(o => o.Locale = BxLocales.De);

        var cut = ctx.Render<BxAlert>(p => WithClose(p.AddCascadingValue(BxLocales.Es)));

        Assert.Equal("Cerrar", CloseLabel(cut));
    }

    [Fact]
    public void The_available_locales_of_the_options_limit_the_automatic_choice()
    {
        using var _ = new CultureScope("de-DE");
        using var ctx = new Ctx(o => o.Locales = [BxLocales.En, BxLocales.Fr]);

        Assert.Equal("Close", CloseLabel(ctx.Render<BxAlert>(WithClose)));
    }

    [Fact]
    public void An_app_can_add_its_own_locale_to_the_available_ones()
    {
        var australian = BxLocales.Extend(BxLocales.En, code: "en-AU", messages: m => m with { Close = "Dismiss" });
        using var _ = new CultureScope("en-AU");
        using var ctx = new Ctx(o => o.Locales = [.. BxLocales.All, australian]);

        Assert.Equal("Dismiss", CloseLabel(ctx.Render<BxAlert>(WithClose)));
    }

    [Fact]
    public void The_automatic_lookup_is_cached_per_culture()
    {
        var options = Services.GetRequiredService<BluxOptions>();

        var first = options.ResolveLocale(CultureInfo.GetCultureInfo("fr-FR"));

        Assert.Same(first, options.ResolveLocale(CultureInfo.GetCultureInfo("fr-FR")));
        Assert.Same(BxLocales.Fr, first);
    }

    [Fact]
    public void Changing_the_locales_of_the_options_clears_the_cache()
    {
        var options = Services.GetRequiredService<BluxOptions>();
        Assert.Same(BxLocales.Fr, options.ResolveLocale(CultureInfo.GetCultureInfo("fr")));

        options.Locales = [BxLocales.En];

        Assert.Same(BxLocales.En, options.ResolveLocale(CultureInfo.GetCultureInfo("fr")));
    }

    // ---- BxApp

    [Fact]
    public void App_writes_lang_and_dir_of_the_resolved_locale()
    {
        using var _ = new CultureScope("ar-EG");

        var cut = Render<BxApp>(p => p.Add(a => a.Toaster, false));

        Assert.Equal("ar", cut.Find("div").GetAttribute("lang"));
        Assert.Equal("rtl", cut.Find("div").GetAttribute("dir"));
    }

    [Fact]
    public void App_Locale_parameter_sets_lang_dir_and_the_texts_of_its_components()
    {
        var cut = Render<BxApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Locale, BxLocales.Ar)
            .Add(a => a.ChildContent, b =>
            {
                b.OpenComponent<BxAlert>(0);
                b.AddAttribute(1, nameof(BxAlert.Title), "T");
                b.AddAttribute(2, nameof(BxAlert.Close), true);
                b.CloseComponent();
            }));

        Assert.Equal("ar", cut.Find("div.isolate").GetAttribute("lang"));
        Assert.Equal("rtl", cut.Find("div.isolate").GetAttribute("dir"));
        Assert.Equal("إغلاق", cut.Find("button[aria-label]").GetAttribute("aria-label"));
    }

    [Fact]
    public void App_explicit_Lang_and_Dir_win_over_the_locale()
    {
        var cut = Render<BxApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Locale, BxLocales.Ar).Add(a => a.Lang, "ar-EG").Add(a => a.Dir, "auto"));

        Assert.Equal("ar-EG", cut.Find("div").GetAttribute("lang"));
        Assert.Equal("auto", cut.Find("div").GetAttribute("dir"));
    }

    [Fact]
    public void Changing_the_Locale_of_the_app_re_renders_the_components_below()
    {
        var cut = Render<BxApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Locale, BxLocales.Fr)
            .Add(a => a.ChildContent, b =>
            {
                b.OpenComponent<BxAlert>(0);
                b.AddAttribute(1, nameof(BxAlert.Title), "T");
                b.AddAttribute(2, nameof(BxAlert.Close), true);
                b.CloseComponent();
            }));
        Assert.Equal("Fermer", cut.Find("button[aria-label]").GetAttribute("aria-label"));

        cut.Render(p => p.Add(a => a.Toaster, false).Add(a => a.Locale, BxLocales.Ja)
            .Add(a => a.ChildContent, b =>
            {
                b.OpenComponent<BxAlert>(0);
                b.AddAttribute(1, nameof(BxAlert.Title), "T");
                b.AddAttribute(2, nameof(BxAlert.Close), true);
                b.CloseComponent();
            }));

        Assert.Equal("閉じる", cut.Find("button[aria-label]").GetAttribute("aria-label"));
        Assert.Equal("ja", cut.Find("div.isolate").GetAttribute("lang"));
    }

    [Fact]
    public void A_nested_app_without_Locale_inherits_the_one_above()
    {
        var cut = Render<BxApp>(p => p.Add(a => a.Toaster, false).Add(a => a.Locale, BxLocales.Ar)
            .Add(a => a.ChildContent, b =>
            {
                b.OpenComponent<BxApp>(0);
                b.AddAttribute(1, nameof(BxApp.Toaster), false);
                b.CloseComponent();
            }));

        Assert.All(cut.FindAll("div.isolate"), d => Assert.Equal("rtl", d.GetAttribute("dir")));
    }

    // ---- Texts under a locale

    [Fact]
    public void Components_render_French_texts_under_a_French_culture_and_under_an_explicit_locale()
    {
        using (new CultureScope("fr-FR"))
        {
            var error = Render<BxError>(p => p.Add(e => e.StatusCode, 404));
            Assert.Equal("Page introuvable", error.Find("h1").TextContent);
            Assert.Equal("Retour à l'accueil", error.Find("a").TextContent.Trim());
        }

        var explicitLocale = Render<BxError>(p => p.AddCascadingValue(BxLocales.De).Add(e => e.StatusCode, 404));
        Assert.Equal("Seite nicht gefunden", explicitLocale.Find("h1").TextContent);
    }

    [Fact]
    public void LocaleSelect_lists_the_locales_of_the_options_by_default_and_selects_the_current_one()
    {
        using var _ = new CultureScope("pt-BR");

        var cut = Render<BxLocaleSelect>();

        Assert.Equal(12, cut.FindAll("[role=option]").Count);
        Assert.Contains("Português (Brasil)", cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")));
        Assert.Equal("pt-BR", cut.Find("bx-select").GetAttribute("value"));
        Assert.Equal("Idioma", cut.Find("button[role=combobox]").GetAttribute("aria-label"));
    }

    [Fact]
    public void The_modifier_key_label_follows_the_locale()
    {
        var german = Render<BxContentSearchButton>(p => p.AddCascadingValue(BxLocales.De).Add(b => b.Collapsed, false));
        var english = Render<BxContentSearchButton>(p => p.Add(b => b.Collapsed, false));

        Assert.Equal("Strg", german.Find("kbd[data-bx-mod]").GetAttribute("data-label"));
        Assert.Equal("Ctrl", english.Find("kbd[data-bx-mod]").GetAttribute("data-label"));
    }

    [Theory]
    [InlineData("fr", 512L, "512 o")]
    [InlineData("fr", 1536L, "1,5 Ko")]
    [InlineData("en", 1536L, "1.5 KB")]
    [InlineData("de", 5L * 1024 * 1024, "5 MB")]
    [InlineData("ru", 2048L, "2 КБ")]
    public void File_sizes_use_the_units_and_the_number_format_of_the_locale(string code, long bytes, string expected)
    {
        Assert.Equal(expected, Blux.UI.Internal.FileSizeText.Format(bytes, ByCode(code)));
    }

    private sealed record Person(string Name);

    [Fact]
    public void The_sort_button_of_a_column_says_what_a_click_does_in_the_language_of_the_locale()
    {
        var cut = Render<BxTable<Person>>(p => p.AddCascadingValue(BxLocales.Fr).Add(t => t.Items, [new Person("Zoe")])
            .AddChildContent<BxColumn<Person>>(c => c.Add(x => x.Title, "Name").Add(x => x.Field, x => x.Name).Add(x => x.Sortable, true)));

        Assert.Equal("Tri croissant", cut.Find("th button").GetAttribute("title"));
        cut.Find("th button").Click();
        Assert.Equal("Tri décroissant", cut.Find("th button").GetAttribute("title"));
    }

    [Fact]
    public void The_form_summary_is_named_by_its_message()
    {
        var english = Render<BxForm>(p => p.Add(f => f.Model, new object()).Add(f => f.ShowSummary, true));
        var french = Render<BxForm>(p => p.AddCascadingValue(BxLocales.Fr).Add(f => f.Model, new object()).Add(f => f.ShowSummary, true));

        Assert.Equal("Please fix the following errors", english.Find("[data-slot=summary]").GetAttribute("aria-label"));
        Assert.Equal("Veuillez corriger les erreurs suivantes", french.Find("[data-slot=summary]").GetAttribute("aria-label"));
    }

    // ---- Culture formatting follows the locale

    [Fact]
    public void Calendar_names_months_in_the_culture_of_the_locale_and_an_explicit_Culture_wins()
    {
        var french = Render<BxCalendar>(p => p.AddCascadingValue(BxLocales.Fr).Add(c => c.Value, new DateOnly(2026, 3, 5)));
        Assert.Contains("mars", french.Markup);
        Assert.DoesNotContain("March", french.Markup);

        var explicitCulture = Render<BxCalendar>(p => p.AddCascadingValue(BxLocales.Fr).Add(c => c.Culture, new CultureInfo("de-DE")).Add(c => c.Value, new DateOnly(2026, 3, 5)));
        Assert.Contains("März", explicitCulture.Markup);
    }

    [Fact]
    public void Calendar_follows_the_UI_culture_when_no_locale_is_cascaded()
    {
        using var _ = new CultureScope("es-ES");

        var cut = Render<BxCalendar>(p => p.Add(c => c.Value, new DateOnly(2026, 3, 5)));

        Assert.Contains("marzo", cut.Markup);
    }

    [Fact]
    public void InputDate_passes_the_locale_to_its_calendar()
    {
        var cut = Render<BxInputDate>(p => p.AddCascadingValue(BxLocales.Fr).Add(d => d.Value, new DateOnly(2026, 3, 5)));

        Assert.Contains("mars", cut.Markup);
    }

    [Fact]
    public void BlogPost_and_InputNumber_default_to_the_culture_of_the_locale()
    {
        var post = Render<BxBlogPost>(p => p.AddCascadingValue(BxLocales.Fr).Add(c => c.Date, new DateTimeOffset(2026, 11, 12, 0, 0, 0, TimeSpan.Zero)));
        Assert.Equal("12 novembre 2026", post.Find("time").TextContent);

        var number = Render<BxInputNumber>(p => p.AddCascadingValue(BxLocales.De).Add(n => n.Value, 1234.5m));
        Assert.Equal("de", number.Find("bx-input-number").GetAttribute("locale"));
    }

    [Fact]
    public void InputTime_uses_the_AM_PM_of_the_locale_when_the_culture_has_none()
    {
        var cut = Render<BxInputTime>(p => p.AddCascadingValue(BxLocales.Ja).Add(t => t.Hour12, true).Add(t => t.Value, new TimeOnly(15, 30)));

        Assert.Equal("時", cut.Find("[data-segment=hour]").GetAttribute("aria-label"));
        Assert.Equal("分", cut.Find("[data-segment=minute]").GetAttribute("aria-label"));
    }

    // ---- No English left behind

    private static HashSet<string> Texts(string markup)
    {
        var document = new HtmlParser().ParseDocument("<body>" + markup + "</body>");
        var texts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in document.Body!.DescendantsAndSelf<INode>())
        {
            if (node is IText text && text.Data.Trim() is { Length: > 0 } data)
            {
                texts.Add(data);
                // "(completed)": the parentheses belong to the markup, the word to the message.
                if (data.StartsWith('(') && data.EndsWith(')')) texts.Add(data[1..^1]);
            }
            if (node is IElement element)
            {
                // Only attributes that carry text for people: state values such as data-status="completed" are not messages.
                foreach (var attribute in element.Attributes.Where(a => a.Name is "aria-label" or "title" or "placeholder" or "alt" || a.Name.Contains("label") || a.Name.StartsWith("data-text")))
                {
                    texts.Add(attribute.Value.Trim());
                }
            }
        }
        return texts;
    }

    /// <summary>Whether a text of the page is the message; a message with placeholders ({0}) matches any text of its shape.</summary>
    private static bool Shows(HashSet<string> texts, string message)
    {
        if (!message.Contains('{')) return texts.Contains(message);
        var pattern = "^" + string.Join(".+", Placeholder().Split(message).Select(Regex.Escape)) + "$";
        return texts.Any(t => Regex.IsMatch(t, pattern));
    }

    private static readonly BxCommandGroup[] Groups = [new("Group", [new BxCommandItem { Label = "Item" }])];
    private static readonly BxCarouselItem[] Slides = [new(b => b.AddContent(0, "a")), new(b => b.AddContent(0, "b"))];

    public static TheoryData<string> Samples() => new(
        "error", "pagination", "breadcrumb", "calendar", "input-date", "input-time", "file-upload", "toaster", "command-palette",
        "color-mode-select", "sidebar", "editor-toolbar", "editor-suggestions", "carousel", "chat-prompt", "content-toc", "locale-select",
        "input-tags", "pin-input", "form", "stepper", "header", "progress", "chat-attachment");

    private IRenderedComponent<IComponent> Sample(string name, BxLocale locale) => name switch
    {
        "error" => Render<BxError>(p => p.AddCascadingValue(locale).Add(e => e.StatusCode, 404)),
        "pagination" => Render<BxPagination>(p => p.AddCascadingValue(locale).Add(c => c.Total, 100).Add(c => c.Page, 3)),
        "breadcrumb" => Render<BxBreadcrumb>(p => p.AddCascadingValue(locale).Add(c => c.Items, [new BxBreadcrumbItem("Home") { Href = "/" }, new BxBreadcrumbItem("Docs")])),
        "calendar" => Render<BxCalendar>(p => p.AddCascadingValue(locale).Add(c => c.Value, new DateOnly(2026, 3, 5))),
        "input-date" => Render<BxInputDate>(p => p.AddCascadingValue(locale)),
        "input-time" => Render<BxInputTime>(p => p.AddCascadingValue(locale).Add(c => c.Hour12, true)),
        "file-upload" => Render<BxFileUpload>(p => p.AddCascadingValue(locale).Add(c => c.MaxSize, 2048L)),
        "toaster" => Render<BxToaster>(p => p.AddCascadingValue(locale)),
        "command-palette" => Render<BxCommandPalette>(p => p.AddCascadingValue(locale).Add(c => c.Groups, Groups)),
        "color-mode-select" => Render<BxColorModeSelect>(p => p.AddCascadingValue(locale)),
        "sidebar" => Render<BxSidebar>(p => p.AddCascadingValue(locale)),
        "editor-toolbar" => Render<BxEditorToolbar>(p => p.AddCascadingValue(locale)),
        "editor-suggestions" => Render<BxEditorSuggestionMenu>(p => p.AddCascadingValue(locale)),
        "carousel" => Render<BxCarousel>(p => p.AddCascadingValue(locale).Add(c => c.Items, Slides).Add(c => c.AutoPlay, 3000)),
        "chat-prompt" => Render<BxChatPrompt>(p => p.AddCascadingValue(locale)),
        "content-toc" => Render<BxContentToc>(p => p.AddCascadingValue(locale).Add(t => t.Links, [new BxContentTocLink("a", "A")])),
        "locale-select" => Render<BxLocaleSelect>(p => p.AddCascadingValue(locale)),
        "input-tags" => Render<BxInputTags>(p => p.AddCascadingValue(locale).Add(t => t.Value, ["x"])),
        "pin-input" => Render<BxPinInput>(p => p.AddCascadingValue(locale).Add(i => i.Length, 4)),
        "form" => Render<BxForm>(p => p.AddCascadingValue(locale).Add(f => f.Model, new object()).Add(f => f.ShowSummary, true)),
        "stepper" => Render<BxStepper>(p => p.AddCascadingValue(locale).Add(s => s.Items, [new BxStepperItem("One"), new BxStepperItem("Two")]).Add(s => s.Current, 1)),
        "header" => Render<BxHeader>(p => p.AddCascadingValue(locale).Add(h => h.Items, [new BxNavigationItem("Home") { Href = "/" }])),
        "progress" => Render<BxProgress>(p => p.AddCascadingValue(locale).Add(b => b.Value, 20)),
        "chat-attachment" => Render<BxChatAttachment>(p => p.AddCascadingValue(locale).Add(c => c.Name, "a.pdf").Add(c => c.FileSize, 1536L).Add(c => c.Status, ChatAttachmentStatus.Uploading)),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Theory]
    [MemberData(nameof(Samples))]
    public void A_component_renders_no_English_message_under_the_French_locale(string name)
    {
        var english = Texts(Sample(name, BxLocales.En).Markup);
        var french = Texts(Sample(name, BxLocales.Fr).Markup);

        var used = MessageProperties
            .Select(p => (Name: p.Name, En: (string)p.GetValue(BxLocales.En.Messages)!, Fr: (string)p.GetValue(BxLocales.Fr.Messages)!))
            .Where(m => m.En != m.Fr && Shows(english, m.En))
            .ToList();

        Assert.NotEmpty(used);
        foreach (var message in used)
        {
            Assert.False(Shows(french, message.En), $"{name} still renders the English {message.Name} (\"{message.En}\") under fr");
            // A template can match text that is not its own (the "Remove {0}" shape fits "Remove link"): only plain messages must show up.
            if (!message.En.Contains('{')) Assert.True(Shows(french, message.Fr), $"{name} does not render the French {message.Name} (\"{message.Fr}\")");
        }
    }

    // ---- The old API is gone

    [Fact]
    public void No_component_reads_the_removed_Options_Strings()
    {
        var root = AppContext.BaseDirectory;
        while (root is not null && !Directory.Exists(Path.Combine(root, "src", "Blux.UI"))) root = Path.GetDirectoryName(root);
        Assert.NotNull(root);

        var offenders = Directory.EnumerateFiles(Path.Combine(root!, "src", "Blux.UI"), "*.*", SearchOption.AllDirectories)
            .Where(f => (f.EndsWith(".razor") || f.EndsWith(".cs")) && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f) is var text && (text.Contains("Options.Strings") || text.Contains("BluxStrings")))
            .ToList();

        Assert.Empty(offenders);
        Assert.Null(typeof(BluxOptions).GetProperty("Strings"));
    }
}
