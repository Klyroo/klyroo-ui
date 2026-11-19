using Blux.UI.Theming;

namespace Blux.UI.Tests;

public class ThemeResolverTests
{
    private static readonly ThemeResolver Resolver = new(new TailwindClassMerger());

    private static ComponentTheme Theme() =>
        new ComponentTheme("test", "base", "icon")
            .Slot("base", "px-2 text-sm")
            .Slot("icon", "size-4")
            .Variant("size", "lg", "base", "px-4 text-lg")
            .Variant("size", "lg", "icon", "size-6")
            .Variant("block", "true", "base", "w-full")
            .Compound(new() { ["size"] = "lg", ["block"] = "true" }, "base", "py-3")
            .Default("size", "sm")
            .Default("block", "false");

    [Fact]
    public void Defaults_apply_when_no_value_is_selected()
    {
        var slots = Resolver.Resolve(Theme(), new Dictionary<string, string?>());

        Assert.Equal("px-2 text-sm", slots["base"]);
        Assert.Equal("size-4", slots["icon"]);
    }

    [Fact]
    public void Variant_classes_override_conflicting_base_classes()
    {
        var slots = Resolver.Resolve(Theme(), new Dictionary<string, string?> { ["size"] = "lg" });

        Assert.Equal("px-4 text-lg", slots["base"]);
        Assert.Equal("size-6", slots["icon"]);
    }

    [Fact]
    public void Compound_variant_applies_only_when_every_condition_matches()
    {
        var both = Resolver.Resolve(Theme(), new Dictionary<string, string?> { ["size"] = "lg", ["block"] = "true" });
        var one = Resolver.Resolve(Theme(), new Dictionary<string, string?> { ["block"] = "true" });

        Assert.Equal("px-4 text-lg w-full py-3", both["base"]);
        Assert.Equal("px-2 text-sm w-full", one["base"]);
    }

    [Fact]
    public void Global_override_appends_and_wins_over_defaults()
    {
        var theme = Theme().Slot("base", "px-8");

        var slots = Resolver.Resolve(theme, new Dictionary<string, string?>());

        Assert.Equal("text-sm px-8", slots["base"]);
    }

    [Fact]
    public void Unknown_slot_is_rejected_with_the_list_of_slots()
    {
        var ex = Assert.Throws<ArgumentException>(() => Theme().Slot("nope", "p-1"));

        Assert.Contains("base, icon", ex.Message);
    }
}
