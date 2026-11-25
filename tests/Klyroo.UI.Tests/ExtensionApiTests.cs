using System.Globalization;
using System.Reflection;
using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

/// <summary>Options of a pretend sister package.</summary>
public sealed class GaugeOptions
{
    public ComponentTheme Gauge { get; } = new ComponentTheme("gauge", "root", "needle")
        .Slot("root", "relative inline-flex")
        .Slot("needle", "absolute h-4")
        .Variant("tone", "calm", "needle", "bg-blue-500")
        .Variant("tone", "hot", "needle", "bg-red-500")
        .Default("tone", "calm");

    public ComponentTheme Legend { get; } = new ComponentTheme("legend", "root").Slot("root", "text-xs");

    public string NotATheme { get; set; } = "x";
}

public sealed class OtherExtensionOptions
{
    public ComponentTheme Other { get; } = new ComponentTheme("other", "root").Slot("root", "gap-9");
}

/// <summary>A component that would live in another assembly: it only uses what <see cref="KlyComponentBase"/> offers to subclasses.</summary>
public sealed class TestGauge : KlyComponentBase
{
    [Parameter] public string Tone { get; set; } = "calm";
    [Parameter] public KlySize? Size { get; set; }

    public string Id { get; private set; } = "";
    public KlyColor Color { get; private set; } = KlyColor.Primary;
    public KlySize EffectiveSize { get; private set; } = KlySize.Md;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var slots = ResolveSlots(Options.Extension<GaugeOptions>().Gauge, new Dictionary<string, string?> { ["tone"] = Tone }, null, "root");
        Id = NewId("gauge");
        Color = DefaultColor(KlyColor.Primary);
        EffectiveSize = Size ?? DefaultSize(KlySize.Md);
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", Id);
        builder.AddAttribute(2, "class", slots["root"]);
        builder.OpenElement(3, "span");
        builder.AddAttribute(4, "class", slots["needle"]);
        builder.CloseElement();
        builder.AddContent(5, RenderIcon("close", "kly-test-icon"));
        builder.CloseElement();
    }
}

public class ExtensionApiTests : KlyrooTestContext
{
    public ExtensionApiTests() : base(o => o.Extension<GaugeOptions>().Gauge.Slot("root", "p-9")) { }

    [Fact]
    public void A_component_of_another_assembly_resolves_its_own_theme_with_the_AddKlyrooUi_overrides()
    {
        var cut = Render<TestGauge>(p => p.Add(c => c.Class, "mine").Add(c => c.Tone, "hot"));

        var root = cut.Find("div");
        Assert.Contains("relative", root.ClassList);
        Assert.Contains("p-9", root.ClassList);
        Assert.Contains("mine", root.ClassList);
        Assert.Contains("bg-red-500", cut.Find("span").ClassList);
        Assert.StartsWith("kly-gauge-", root.Id);
        Assert.NotNull(cut.Find(".kly-test-icon"));
    }

    [Fact]
    public void Defaults_of_the_app_reach_the_protected_helpers()
    {
        var options = Services.GetRequiredService<KlyrooOptions>();
        options.Defaults.Size = KlySize.Lg;

        var cut = Render<TestGauge>();

        Assert.Equal(KlySize.Lg, cut.Instance.EffectiveSize);
        Assert.Equal(KlyColor.Primary, cut.Instance.Color);
    }

    [Fact]
    public void The_theme_is_read_only_after_the_first_render_like_a_core_theme()
    {
        var gauge = Services.GetRequiredService<KlyrooOptions>().Extension<GaugeOptions>().Gauge;
        Render<TestGauge>();

        var ex = Assert.Throws<InvalidOperationException>(() => gauge.Slot("root", "p-1"));
        Assert.Contains("'gauge'", ex.Message);
        Assert.Contains("AddKlyrooUi", ex.Message);
        Assert.Equal("p-9", gauge.Slots["root"][^3..]);
    }

    [Fact]
    public void A_theme_that_was_not_rendered_stays_configurable()
    {
        var options = Services.GetRequiredService<KlyrooOptions>();
        Render<TestGauge>();

        options.Extension<GaugeOptions>().Legend.Slot("root", "text-sm");
    }

    [Fact]
    public void The_theme_members_are_protected_for_subclasses_of_other_assemblies()
    {
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
        foreach (var name in new[] { "ResolveSlots", "RenderIcon", "NewId", "DefaultColor", "DefaultSize" })
        {
            var method = Assert.Single(typeof(KlyComponentBase).GetMethods(all), m => m.Name == name);
            Assert.True(method.IsFamily, $"{name} must be protected, not private protected");
        }
        Assert.True(typeof(SlotSet).IsPublic);
    }

    [Fact]
    public void The_parameter_and_return_types_of_the_protected_members_are_public()
    {
        // The test assembly sees internals, so a type that is internal would compile here and not in a sister package.
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
        foreach (var name in new[] { "ResolveSlots", "RenderIcon", "NewId", "DefaultColor", "DefaultSize" })
        {
            var method = Assert.Single(typeof(KlyComponentBase).GetMethods(all), m => m.Name == name);
            foreach (var type in method.GetParameters().Select(p => p.ParameterType).Append(method.ReturnType))
            {
                AssertPublic(type, name);
            }
        }
    }

    private static void AssertPublic(Type type, string member)
    {
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments()) AssertPublic(argument, member);
        }
        Assert.True(type.IsVisible, $"{type} used by {member} is not public");
    }
}

public class ExtensionOptionsTests
{
    [Fact]
    public void Extension_returns_one_instance_per_type_and_per_options()
    {
        var options = new KlyrooOptions();

        Assert.Same(options.Extension<GaugeOptions>(), options.Extension<GaugeOptions>());
        Assert.Same(options.Extension<GaugeOptions>(), options.Extension(typeof(GaugeOptions)));
        Assert.NotSame(options.Extension<GaugeOptions>(), new KlyrooOptions().Extension<GaugeOptions>());
        Assert.NotSame((object)options.Extension<GaugeOptions>(), options.Extension<OtherExtensionOptions>());
    }

    [Fact]
    public void Extension_is_created_once_under_contention()
    {
        var options = new KlyrooOptions();

        var instances = Enumerable.Range(0, 64).AsParallel().WithDegreeOfParallelism(16)
            .Select(_ => options.Extension<GaugeOptions>()).ToList();

        Assert.Single(instances.Distinct());
        Assert.Single(options.Extensions);
    }

    [Fact]
    public void AllThemes_lists_core_themes_then_every_public_theme_of_each_extension()
    {
        var options = new KlyrooOptions();
        var coreCount = options.AllThemes.Count();
        Assert.DoesNotContain(options.AllThemes, t => t.Name == "gauge");

        var gauge = options.Extension<GaugeOptions>();
        var other = options.Extension<OtherExtensionOptions>();

        var all = options.AllThemes.ToList();
        Assert.Equal(coreCount + 3, all.Count);
        Assert.Contains(gauge.Gauge, all);
        Assert.Contains(gauge.Legend, all);
        Assert.Contains(other.Other, all);
    }

    [Fact]
    public void The_class_catalog_includes_the_classes_of_extension_themes()
    {
        var options = new KlyrooOptions();
        Assert.DoesNotContain("bg-red-500", ThemeCatalog.AllClassTokens(options));

        options.Extension<GaugeOptions>();

        var tokens = ThemeCatalog.AllClassTokens(options);
        Assert.Contains("bg-red-500", tokens);
        Assert.Contains("relative", tokens);
        Assert.Contains("gap-9", ThemeCatalog.AllClassTokens(Registered(options, o => o.Extension<OtherExtensionOptions>())));
        Assert.Equal(tokens.Order(StringComparer.Ordinal), tokens);
    }

    [Fact]
    public void An_extension_must_be_a_constructible_class()
    {
        var options = new KlyrooOptions();

        Assert.Throws<ArgumentException>(() => options.Extension(typeof(IDisposable)));
        Assert.Throws<ArgumentException>(() => options.Extension(typeof(Stream)));
        Assert.Throws<ArgumentException>(() => options.Extension(typeof(string)));
    }

    [Fact]
    public void KlyrooOptions_and_open_generic_types_are_not_extensions()
    {
        var options = new KlyrooOptions();

        Assert.Throws<ArgumentException>(() => options.Extension(typeof(KlyrooOptions)));
        Assert.Throws<ArgumentException>(() => options.Extension<KlyrooOptions>());
        Assert.Throws<ArgumentException>(() => options.Extension(typeof(KlyRadioGroup<>)));
    }

    [Fact]
    public void Core_themes_are_unchanged_by_extensions()
    {
        var before = new KlyrooOptions().AllThemes.Select(t => t.Name).ToList();
        var options = new KlyrooOptions();
        options.Extension<GaugeOptions>();

        Assert.Equal(before, options.AllThemes.Take(before.Count).Select(t => t.Name));
    }

    private static KlyrooOptions Registered(KlyrooOptions options, Action<KlyrooOptions> register)
    {
        register(options);
        return options;
    }
}

public class LocaleCodeResolutionTests
{
    private static readonly string[] AllCodes = [.. KlyLocales.All.Select(l => l.Code)];

    [Fact]
    public void Every_builtin_code_resolves_to_itself()
    {
        Assert.Equal(12, AllCodes.Length);
        foreach (var code in AllCodes)
        {
            Assert.Equal(code, KlyLocales.ResolveCode(code, AllCodes));
        }
    }

    [Theory]
    [InlineData("fr-FR", "fr")]
    [InlineData("fr-CA", "fr")]
    [InlineData("pt-PT", "pt")]
    [InlineData("pt-BR", "pt-BR")]
    [InlineData("pt-br", "pt-BR")]
    [InlineData("zh-CN", "zh-Hans")]
    [InlineData("zh-SG", "zh-Hans")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("zh-Hans-CN", "zh-Hans")]
    [InlineData("zh-TW", "en")]
    [InlineData("zh-HK", "en")]
    [InlineData("zh-Hant", "en")]
    [InlineData("ja-JP", "ja")]
    [InlineData("ar-EG", "ar")]
    [InlineData("ru-RU", "ru")]
    [InlineData("nl-BE", "nl")]
    [InlineData("de-AT", "de")]
    [InlineData("en-GB", "en")]
    [InlineData("sv-SE", "en")]
    [InlineData("", "en")]
    [InlineData("xx-YY-zz", "en")]
    public void A_code_picks_the_exact_code_then_its_parents_then_English_like_core(string code, string expected)
    {
        Assert.Equal(expected, KlyLocales.ResolveCode(code, AllCodes));
        Assert.Equal(KlyLocales.Resolve(CultureInfo.GetCultureInfo(code.StartsWith("xx", StringComparison.Ordinal) ? "" : code)).Code, expected);
    }

    [Fact]
    public void A_parent_never_reaches_a_region_and_the_first_code_is_the_last_fallback()
    {
        Assert.Equal("pt", KlyLocales.ResolveCode("pt-BR", ["en", "pt"]));
        Assert.Equal("en", KlyLocales.ResolveCode("pt", ["en", "pt-BR"]));
        Assert.Equal("de", KlyLocales.ResolveCode("sv", ["de", "fr"]));
        Assert.Equal("fr", KlyLocales.ResolveCode("fr-CA", ["de", "fr"]));
    }

    [Fact]
    public void The_result_keeps_the_spelling_of_the_available_code_and_an_empty_list_is_rejected()
    {
        Assert.Equal("PT-br", KlyLocales.ResolveCode("pt-BR", ["en", "PT-br"]));
        Assert.Throws<ArgumentException>(() => KlyLocales.ResolveCode("fr", []));
        Assert.Throws<ArgumentNullException>(() => KlyLocales.ResolveCode(null!, ["en"]));
    }

    [Fact]
    public void It_accepts_any_enumerable_such_as_dictionary_keys()
    {
        var tables = new Dictionary<string, int> { ["en"] = 1, ["fr"] = 2, ["zh-Hans"] = 3 };

        Assert.Equal(3, tables[KlyLocales.ResolveCode("zh-CN", tables.Keys)]);
    }
}
