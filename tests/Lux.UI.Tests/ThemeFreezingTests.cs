using Lux.UI.Theming;
using Microsoft.Extensions.DependencyInjection;

namespace Lux.UI.Tests;

public class ThemeFreezingTests
{
    private static readonly IReadOnlyDictionary<string, string?> NoSelection = new Dictionary<string, string?>();

    private static ThemeResolver NewResolver() => new(new TailwindClassMerger());

    private static ComponentTheme Theme() =>
        new ComponentTheme("test", "base", "icon")
            .Slot("base", "px-2")
            .Variant("size", "lg", "base", "px-4")
            .Compound(new() { ["size"] = "lg" }, "base", "py-3")
            .Default("size", "lg");

    private static ComponentTheme Frozen()
    {
        var theme = Theme();
        NewResolver().Resolve(theme, NoSelection);
        return theme;
    }

    public static TheoryData<string, Action<ComponentTheme>> Mutations => new()
    {
        { "Slot", t => t.Slot("base", "p-1") },
        { "Variant", t => t.Variant("size", "lg", "base", "p-1") },
        { "Variant (new value)", t => t.Variant("size", "sm", "base", "p-1") },
        { "Compound", t => t.Compound(new() { ["size"] = "sm" }, "base", "p-1") },
        { "Default", t => t.Default("size", "sm") },
    };

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Every_fluent_method_throws_once_the_theme_has_been_resolved(string path, Action<ComponentTheme> mutate)
    {
        var theme = Frozen();

        var ex = Assert.Throws<InvalidOperationException>(() => mutate(theme));

        Assert.Contains("AddLuxUi", ex.Message);
        Assert.Contains("before the first render", ex.Message);
        Assert.Contains("'test'", ex.Message);
        Assert.NotEmpty(path);
    }

    [Theory]
    [MemberData(nameof(Mutations))]
    public void Every_fluent_method_still_works_before_the_first_resolve(string path, Action<ComponentTheme> mutate)
    {
        var theme = Theme();

        mutate(theme);

        Assert.NotEmpty(path);
    }

    [Fact]
    public void A_rejected_mutation_leaves_the_theme_unchanged()
    {
        var theme = Frozen();

        Assert.Throws<InvalidOperationException>(() => theme.Slot("base", "p-1"));

        Assert.Equal("px-2", theme.Slots["base"]);
        Assert.Single(theme.CompoundVariants);
    }

    [Fact]
    public void Resolving_again_after_freezing_keeps_working_for_other_selections()
    {
        var resolver = NewResolver();
        var theme = Theme();
        var first = resolver.Resolve(theme, NoSelection);

        var other = resolver.Resolve(theme, new Dictionary<string, string?> { ["size"] = "sm" });

        Assert.Equal("px-4 py-3", first["base"]);
        Assert.Equal("px-2", other["base"]);
    }

    [Fact]
    public void Public_collections_expose_no_mutating_surface()
    {
        var theme = Frozen();

        Assert.False((object)theme.Slots is IDictionary<string, string>);
        Assert.False((object)theme.DefaultVariants is IDictionary<string, string>);
        Assert.False((object)theme.Variants is System.Collections.IDictionary);
        Assert.False((object)theme.Variants["size"] is System.Collections.IDictionary);
        Assert.False((object)theme.Variants["size"]["lg"] is IDictionary<string, string>);
        Assert.False((object)theme.CompoundVariants is System.Collections.IList);
        Assert.False((object)theme.CompoundVariants[0].When is IDictionary<string, string>);
        Assert.False((object)theme.CompoundVariants[0].Classes is IDictionary<string, string>);
    }

    [Fact]
    public void The_dictionary_passed_to_Compound_is_copied()
    {
        var when = new Dictionary<string, string> { ["size"] = "lg" };
        var theme = Theme().Compound(when, "icon", "size-6");

        when["size"] = "sm";
        when["extra"] = "x";

        Assert.Equal("lg", theme.CompoundVariants[^1].When["size"]);
        Assert.Single(theme.CompoundVariants[^1].When);
    }

    [Fact]
    public void Concurrent_first_resolves_and_mutations_never_apply_a_change_after_freezing()
    {
        for (var round = 0; round < 50; round++)
        {
            var resolver = NewResolver();
            var theme = new ComponentTheme("race", "base").Slot("base", "a");
            var applied = 0;
            var results = new System.Collections.Concurrent.ConcurrentBag<string>();

            Parallel.For(0, 16, i =>
            {
                if (i % 2 == 0)
                {
                    results.Add(resolver.Resolve(theme, NoSelection)["base"]);
                }
                else
                {
                    try
                    {
                        theme.Slot("base", "b" + i);
                        Interlocked.Increment(ref applied);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            });

            // A resolve that ran after another resolve's freeze sees the final state, so the theme is stable:
            // the cached result of the first resolve equals what a later resolve returns.
            var final = resolver.Resolve(theme, NoSelection)["base"];
            Assert.Equal(theme.Slots["base"], final);
            Assert.Equal(applied, theme.Slots["base"].Split(' ').Length - 1);
        }
    }
}

public class ThemeFreezingServiceTests
{
    [Fact]
    public void Calling_AddLuxUi_twice_configures_the_same_options_before_any_render()
    {
        var services = new ServiceCollection();
        services.AddLuxUi(o => o.Button.Slot("base", "underline"));
        services.AddLuxUi(o => o.Button.Slot("base", "italic"));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LuxOptions>();
        var slots = provider.GetRequiredService<ThemeResolver>().Resolve(options.Button, new Dictionary<string, string?>());

        Assert.Contains("underline", slots["base"]);
        Assert.Contains("italic", slots["base"]);
    }

    [Fact]
    public void Options_registered_by_AddLuxUi_are_frozen_by_the_first_resolve()
    {
        var services = new ServiceCollection();
        services.AddLuxUi();
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<LuxOptions>();

        provider.GetRequiredService<ThemeResolver>().Resolve(options.Card, new Dictionary<string, string?>());

        Assert.Throws<InvalidOperationException>(() => options.Card.Slot("root", "shadow-sm"));
        options.Alert.Slot("root", "shadow-sm"); // another theme, not resolved yet
    }
}

public class ThemeFreezingRenderingTests : BluxTestContext
{
    [Fact]
    public void Rendering_freezes_the_theme_and_keeps_rendering_every_selection()
    {
        var options = Services.GetRequiredService<LuxOptions>();

        var first = Render<BxButton>(p => p.Add(b => b.Label, "One"));

        Assert.Throws<InvalidOperationException>(() => options.Button.Slot("base", "font-bold"));

        var second = Render<BxButton>(p => p.Add(b => b.Label, "Two").Add(b => b.Size, BxSize.Lg));
        Assert.Equal("Two", second.Find("button").TextContent.Trim());
        Assert.Equal("One", first.Find("button").TextContent.Trim());
    }

    [Fact]
    public void A_scope_keeps_working_with_frozen_themes()
    {
        var cut = Render<BxTheme>(p => p
            .Add(t => t.For, typeof(BxButton))
            .Add(t => t.Ui, new ButtonUi { Base = "rounded-none" })
            .AddChildContent<BxButton>());

        Assert.Contains("rounded-none", cut.Find("button").ClassList);
        Assert.Throws<InvalidOperationException>(() => Services.GetRequiredService<LuxOptions>().Button.Slot("base", "p-9"));
    }
}
