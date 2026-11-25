using Klyroo.UI.Build;

namespace Klyroo.UI.Tests.Icons;

public class IconNamesTests
{
    [Theory]
    [InlineData("ph:house", "ph", "house", "Colon")]
    [InlineData("lucide:chevron-down", "lucide", "chevron-down", "Colon")]
    [InlineData("i-ph-house", "ph", "house", "IPrefix")]
    [InlineData("i-lucide-lightbulb", "lucide", "lightbulb", "IPrefix")]
    [InlineData("lightbulb", "lucide", "lightbulb", "Bare")]
    [InlineData("chevron-down", "lucide", "chevron-down", "Bare")]
    [InlineData("  ph:house  ", "ph", "house", "Colon")]
    public void Reads_the_three_forms(string text, string prefix, string name, string form)
    {
        Assert.True(IconNames.TryParse(text, null, out var icon, out var actual));
        Assert.Equal(prefix, icon.Prefix);
        Assert.Equal(name, icon.Name);
        Assert.Equal(form, actual.ToString());
        Assert.Equal(prefix + ":" + name, icon.Key);
    }

    [Theory]
    [InlineData("i-simple-icons-github", "simple-icons", "github")]
    [InlineData("simple-icons:github", "simple-icons", "github")]
    [InlineData("i-material-symbols-light-home", "material-symbols-light", "home")]
    [InlineData("i-mdi-light-home", "mdi-light", "home")]
    [InlineData("i-mdi-home-outline", "mdi", "home-outline")]
    public void A_prefix_with_dashes_is_found_by_the_longest_known_collection(string text, string prefix, string name)
    {
        Assert.True(IconNames.TryParse(text, null, out var icon, out _));
        Assert.Equal((prefix, name), (icon.Prefix, icon.Name));
    }

    [Fact]
    public void The_colon_form_is_unambiguous()
    {
        // mdi-light is a collection, but "mdi:light-home" says the prefix is mdi.
        Assert.True(IconNames.TryParse("mdi:light-home", null, out var icon, out _));
        Assert.Equal(("mdi", "light-home"), (icon.Prefix, icon.Name));
    }

    [Fact]
    public void A_name_that_starts_with_i_dash_and_matches_no_collection_is_a_lucide_icon()
    {
        Assert.True(IconNames.TryParse("i-cursor", null, out var icon, out var form));
        Assert.Equal(("lucide", "i-cursor"), (icon.Prefix, icon.Name));
        Assert.Equal(IconNameForm.Bare, form);
        Assert.True(IconNames.TryParse("i-was-here", null, out icon, out form));
        Assert.Equal(IconNameForm.Bare, form);
    }

    [Fact]
    public void Custom_prefixes_take_part_in_the_i_form_when_the_caller_knows_them()
    {
        Assert.True(IconNames.TryParse("i-my-brand-logo", p => p == "my-brand", out var icon, out var form));
        Assert.Equal(("my-brand", "logo"), (icon.Prefix, icon.Name));
        Assert.Equal(IconNameForm.IPrefix, form);
        Assert.True(IconNames.TryParse("i-custom-logo", p => p == "custom", out icon, out _));
        Assert.Equal(("custom", "logo"), (icon.Prefix, icon.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Ph:House")]
    [InlineData("ph:")]
    [InlineData(":house")]
    [InlineData("ph:a:b")]
    [InlineData("a b")]
    [InlineData("x--y")]
    [InlineData("-x")]
    [InlineData("x-")]
    [InlineData("ph:x_y")]
    [InlineData("@x")]
    public void Rejects_what_is_not_a_name(string? text)
    {
        Assert.False(IconNames.TryParse(text, null, out _, out _));
    }

    [Fact]
    public void The_collection_list_is_the_iconify_one()
    {
        Assert.Contains("lucide", IconCollections.Prefixes);
        Assert.Contains("ph", IconCollections.Prefixes);
        Assert.Contains("mdi", IconCollections.Prefixes);
        Assert.Contains("simple-icons", IconCollections.Prefixes);
        Assert.True(IconCollections.Prefixes.Length > 200);
        Assert.All(IconCollections.Prefixes, p => Assert.True(IconNames.IsValidPart(p), p));
        Assert.Equal(IconCollections.Prefixes.Length, IconCollections.Prefixes.Distinct().Count());
    }

    [Theory]
    [InlineData("i-simple-icons-github")]
    [InlineData("i-mdi-light-home")]
    [InlineData("i-cursor")]
    [InlineData("ph:house")]
    [InlineData("search")]
    public void The_library_reads_names_like_the_build_task(string text)
    {
        Assert.True(IconNames.TryParse(text, null, out var task, out var taskForm));
        Assert.True(Klyroo.UI.Internal.IconNames.TryParse(text, null, out var runtime, out var runtimeForm));
        Assert.Equal(task.Key, runtime.Key);
        Assert.Equal(taskForm.ToString(), runtimeForm.ToString());
    }

    [Fact]
    public void The_built_in_list_the_task_skips_is_the_one_the_library_embeds()
    {
        Assert.Equal(IconCollections.BuiltinLucide.Order(StringComparer.Ordinal), BuiltinIcons.Lucide.Keys.Order(StringComparer.Ordinal));
    }
}
