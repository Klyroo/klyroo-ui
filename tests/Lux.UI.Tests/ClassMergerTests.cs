using Lux.UI.Theming;

namespace Lux.UI.Tests;

/// <summary>
/// Themes use arbitrary CSS variables (bg-(--lx-color)); user overrides use regular utilities.
/// These pin that tailwind-merge treats them as the same group, otherwise overrides would stack instead of replace.
/// </summary>
public class ClassMergerTests
{
    private readonly TailwindClassMerger _merger = new();

    [Theory]
    [InlineData("bg-(--lx-color) bg-red-500", "bg-red-500")]
    [InlineData("bg-(--lx-color)/10 bg-red-500/20", "bg-red-500/20")]
    [InlineData("text-(--lx-color-contrast) text-white", "text-white")]
    [InlineData("text-(--lx-color-text) text-sm", "text-(--lx-color-text) text-sm")]
    [InlineData("rounded-(--lux-radius) rounded-full", "rounded-full")]
    [InlineData("ring-(--lx-color)/50 ring-red-500", "ring-red-500")]
    [InlineData("hover:bg-(--lx-color)/90 hover:bg-red-600", "hover:bg-red-600")]
    [InlineData("px-2.5 py-1.5 p-1", "p-1")]
    public void Overrides_replace_theme_classes_of_the_same_group(string input, string expected)
    {
        Assert.Equal(expected, _merger.Merge(input));
    }
}
