namespace Lux.UI.Tests;

/// <summary>
/// LxSelectMenu and LxInputMenu throw only when the Items parameter is not passed at all and there is no OptionsProvider: <c>Items="@_list"</c> with a null list (still loading) renders an empty list,
/// and the list shows when it arrives.
/// </summary>
public class MenuItemsParameterTests : LuxTestContext
{
    private static readonly IReadOnlyList<LxSelectItem> Cities = [new("p", "Paris"), new("l", "Lyon")];

    private static string[] SelectLabels(IRenderedComponent<LxSelectMenu> cut) => [.. cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")!)];

    private static string[] InputLabels(IRenderedComponent<LxInputMenu> cut) => [.. cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")!)];

    // ---- LxSelectMenu

    [Fact]
    public void SelectMenu_without_Items_and_without_a_provider_throws_and_names_both()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<LxSelectMenu>());

        Assert.Contains("Items", error.Message);
        Assert.Contains("OptionsProvider", error.Message);
    }

    [Fact]
    public void SelectMenu_with_Items_null_renders_an_empty_list_and_shows_the_list_when_it_arrives()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, null!));

        Assert.Empty(SelectLabels(cut));

        cut.Render(p => p.Add(s => s.Items, Cities));
        Assert.Equal(["Paris", "Lyon"], SelectLabels(cut));

        cut.Render(p => p.Add(s => s.Items, null!));
        Assert.Empty(SelectLabels(cut));
    }

    [Fact]
    public void SelectMenu_with_an_empty_Items_renders_an_empty_list()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, []));

        Assert.Empty(SelectLabels(cut));
    }

    [Fact]
    public void SelectMenu_with_a_null_Items_and_a_value_shows_the_raw_value()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.Items, null!).Add(s => s.Value, ["p"]));

        Assert.Equal("p", cut.Find("[data-slot=label]").GetAttribute("data-label"));
    }

    [Fact]
    public void SelectMenu_with_a_provider_does_not_need_Items()
    {
        var cut = Render<LxSelectMenu>(p => p.Add(s => s.OptionsProvider, (_, _) => new ValueTask<LxOptionsResult>(new LxOptionsResult([]))));

        Assert.Empty(SelectLabels(cut));
    }

    // ---- LxInputMenu

    [Fact]
    public void InputMenu_without_Items_and_without_a_provider_throws_and_names_both()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<LxInputMenu>());

        Assert.Contains("Items", error.Message);
        Assert.Contains("OptionsProvider", error.Message);
    }

    [Fact]
    public void InputMenu_with_Items_null_renders_an_empty_list_and_shows_the_list_when_it_arrives()
    {
        var cut = Render<LxInputMenu>(p => p.Add(s => s.Items, null!));

        Assert.Empty(InputLabels(cut));

        cut.Render(p => p.Add(s => s.Items, Cities));
        Assert.Equal(["Paris", "Lyon"], InputLabels(cut));

        cut.Render(p => p.Add(s => s.Items, null!));
        Assert.Empty(InputLabels(cut));
    }

    [Fact]
    public void InputMenu_with_an_empty_Items_renders_an_empty_list()
    {
        var cut = Render<LxInputMenu>(p => p.Add(s => s.Items, []));

        Assert.Empty(InputLabels(cut));
    }

    [Fact]
    public void InputMenu_with_a_provider_does_not_need_Items()
    {
        var cut = Render<LxInputMenu>(p => p.Add(s => s.OptionsProvider, (_, _) => new ValueTask<LxOptionsResult>(new LxOptionsResult([]))));

        Assert.Empty(InputLabels(cut));
    }
}
