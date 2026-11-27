namespace Klyroo.UI.Maui.Tests;

public class IconTests
{
    [Fact]
    public void Every_built_in_icon_has_shapes_that_parse_to_a_path_with_bounds()
    {
        Assert.Equal(90, BuiltinIcons.Lucide.Count);
        foreach (var (name, shapes) in BuiltinIcons.Lucide)
        {
            Assert.NotEmpty(shapes);
            foreach (var shape in shapes)
            {
                var path = PathBuilder.Build(shape);
                Assert.True(path.Count > 0, $"{name}: no segment in '{shape}'");
                Assert.InRange(path.Bounds.Right, 0, 24.5f);
                Assert.InRange(path.Bounds.Bottom, 0, 24.5f);
            }
        }
    }

    [Fact]
    public void Every_name_of_KlyIconNames_is_a_built_in_icon()
    {
        foreach (var field in typeof(KlyIconNames).GetFields().Where(f => f.IsLiteral))
        {
            var name = (string)field.GetRawConstantValue()!;
            Assert.True(KlyIcons.TryGet(name, out _), $"{field.Name} = {name}");
        }
    }

    [Theory]
    [InlineData("search")]
    [InlineData("i-lucide-search")]
    [InlineData("lucide:search")]
    public void The_name_forms_of_klyroo_ui_resolve_to_the_same_icon(string name)
    {
        Assert.True(KlyIcons.TryGet(name, out var paths));
        Assert.Equal(BuiltinIcons.Lucide["search"], paths);
    }

    [Fact]
    public void A_registered_icon_resolves_and_an_unknown_name_does_not()
    {
        KlyIcons.Register("mdi:test-icon", "M0 0L24 24");

        Assert.True(KlyIcons.TryGet("mdi:test-icon", out var paths));
        Assert.Equal(["M0 0L24 24"], paths);
        Assert.False(KlyIcons.TryGet("nope-not-an-icon", out _));
        Assert.False(KlyIcons.TryGet(" ", out _));
    }

    [Theory]
    [InlineData(KlySize.Xs, 16)]
    [InlineData(KlySize.Sm, 16)]
    [InlineData(KlySize.Md, 20)]
    [InlineData(KlySize.Lg, 20)]
    [InlineData(KlySize.Xl, 24)]
    public void Icon_size_is_the_web_size_and_an_icon_with_a_label_is_in_the_accessibility_tree(KlySize size, double expected)
    {
        var icon = new KlyIcon { Name = KlyIconNames.Search, Size = size };
        Assert.Equal(expected, icon.WidthRequest);
        Assert.Equal(expected, icon.HeightRequest);
        Assert.False(AutomationProperties.GetIsInAccessibleTree(icon));

        icon.Label = "Search";
        Assert.True(AutomationProperties.GetIsInAccessibleTree(icon));
        Assert.Equal("Search", SemanticProperties.GetDescription(icon));
    }
}
