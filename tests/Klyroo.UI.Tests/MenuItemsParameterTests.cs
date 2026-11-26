namespace Klyroo.UI.Tests;

/// <summary>
/// KlySelectMenu and KlyInputMenu throw only when the Items parameter is not passed at all and there is no OptionsProvider: <c>Items="@_list"</c> with a null list (still loading) renders an empty list,
/// and the list shows when it arrives.
/// </summary>
public class MenuItemsParameterTests : KlyrooTestContext
{
    private static readonly IReadOnlyList<KlySelectItem> Cities = [new("p", "Paris"), new("l", "Lyon")];

    private static string[] SelectLabels(IRenderedComponent<KlySelectMenu<string>> cut) => [.. cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")!)];

    private static string[] InputLabels(IRenderedComponent<KlyInputMenu<string>> cut) => [.. cut.FindAll("[role=option]").Select(o => o.GetAttribute("data-label")!)];

    // ---- KlySelectMenu

    [Fact]
    public void SelectMenu_without_Items_and_without_a_provider_throws_and_names_both()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<KlySelectMenu<string>>());

        Assert.Contains("Items", error.Message);
        Assert.Contains("OptionsProvider", error.Message);
    }

    [Fact]
    public void SelectMenu_with_Items_null_renders_an_empty_list_and_shows_the_list_when_it_arrives()
    {
        var cut = Render<KlySelectMenu<string>>(p => p.Add(s => s.Items, null!));

        Assert.Empty(SelectLabels(cut));

        cut.Render(p => p.Add(s => s.Items, Cities));
        Assert.Equal(["Paris", "Lyon"], SelectLabels(cut));

        cut.Render(p => p.Add(s => s.Items, null!));
        Assert.Empty(SelectLabels(cut));
    }

    [Fact]
    public void SelectMenu_with_an_empty_Items_renders_an_empty_list()
    {
        var cut = Render<KlySelectMenu<string>>(p => p.Add(s => s.Items, []));

        Assert.Empty(SelectLabels(cut));
    }

    [Fact]
    public void SelectMenu_with_a_null_Items_and_a_value_shows_the_raw_value()
    {
        var cut = Render<KlySelectMenu<string>>(p => p.Add(s => s.Items, null!).Add(s => s.Value, ["p"]));

        Assert.Equal("p", cut.Find("[data-slot=label]").GetAttribute("data-label"));
    }

    [Fact]
    public void SelectMenu_with_a_provider_does_not_need_Items()
    {
        var cut = Render<KlySelectMenu<string>>(p => p.Add(s => s.OptionsProvider, (_, _) => new ValueTask<KlyOptionsResult>(new KlyOptionsResult([]))));

        Assert.Empty(SelectLabels(cut));
    }

    // ---- KlyInputMenu

    [Fact]
    public void InputMenu_without_Items_and_without_a_provider_throws_and_names_both()
    {
        var error = Assert.ThrowsAny<InvalidOperationException>(() => Render<KlyInputMenu<string>>());

        Assert.Contains("Items", error.Message);
        Assert.Contains("OptionsProvider", error.Message);
    }

    [Fact]
    public void InputMenu_with_Items_null_renders_an_empty_list_and_shows_the_list_when_it_arrives()
    {
        var cut = Render<KlyInputMenu<string>>(p => p.Add(s => s.Items, null!));

        Assert.Empty(InputLabels(cut));

        cut.Render(p => p.Add(s => s.Items, Cities));
        Assert.Equal(["Paris", "Lyon"], InputLabels(cut));

        cut.Render(p => p.Add(s => s.Items, null!));
        Assert.Empty(InputLabels(cut));
    }

    [Fact]
    public void InputMenu_with_an_empty_Items_renders_an_empty_list()
    {
        var cut = Render<KlyInputMenu<string>>(p => p.Add(s => s.Items, []));

        Assert.Empty(InputLabels(cut));
    }

    [Fact]
    public void InputMenu_with_a_provider_does_not_need_Items()
    {
        var cut = Render<KlyInputMenu<string>>(p => p.Add(s => s.OptionsProvider, (_, _) => new ValueTask<KlyOptionsResult>(new KlyOptionsResult([]))));

        Assert.Empty(InputLabels(cut));
    }
}
