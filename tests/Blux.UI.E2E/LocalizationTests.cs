namespace Blux.UI.E2E;

/// <summary>Locales in the browser: per subtree, switched at runtime, and followed from the culture cookie, in every render mode.</summary>
public class LocalizationTests(PlaygroundServer server) : PlaygroundTest(server)
{
    private async Task SetCultureCookie(string culture) =>
        await Page.Context.AddCookiesAsync([new Cookie
        {
            Name = ".AspNetCore.Culture",
            Value = Uri.EscapeDataString($"c={culture}|uic={culture}"),
            Url = BaseUrl,
        }]);

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Each_BxApp_applies_its_own_texts_direction_and_formats(string mode)
    {
        await Open(mode, "locale-select");

        var french = Page.GetByTestId("locale-panel-fr");
        await Assertions.Expect(french).ToHaveAttributeAsync("lang", "fr");
        await Assertions.Expect(french).ToHaveAttributeAsync("dir", "ltr");
        await Assertions.Expect(french).ToContainTextAsync("Déposez des fichiers ici ou cliquez pour parcourir");
        await Assertions.Expect(french).ToContainTextAsync("Taille maximale 1,5 Ko");
        Assert.Contains("mars", (await french.InnerTextAsync()).ToLowerInvariant());

        var arabic = Page.GetByTestId("locale-panel-ar");
        await Assertions.Expect(arabic).ToHaveAttributeAsync("lang", "ar");
        await Assertions.Expect(arabic).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(arabic).ToContainTextAsync("أفلت الملفات هنا أو انقر للتصفح");
        Assert.Equal("rtl", await arabic.EvaluateAsync<string>("e => getComputedStyle(e).direction"));

        var japanese = Page.GetByTestId("locale-panel-ja");
        await Assertions.Expect(japanese).ToContainTextAsync("ここにファイルをドロップするか、クリックして選択");
        // The page itself keeps its own direction.
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
    }

    [Theory]
    [MemberData(nameof(InteractiveModes))]
    public async Task Changing_the_Locale_of_a_BxApp_switches_texts_and_direction(string mode)
    {
        await Open(mode, "locale-select");
        var panel = Page.GetByTestId("locale-switch-panel");
        await Assertions.Expect(panel).ToContainTextAsync("Drop files here or click to browse");
        await Assertions.Expect(panel).ToHaveAttributeAsync("dir", "ltr");

        await Page.GetByTestId("locale-switch-fr").ClickAsync();
        await Assertions.Expect(panel).ToContainTextAsync("Déposez des fichiers ici ou cliquez pour parcourir");
        await Assertions.Expect(panel).ToHaveAttributeAsync("lang", "fr");
        await Assertions.Expect(panel.GetByRole(AriaRole.Navigation, new() { Name = "Pagination" })).ToBeVisibleAsync();

        await Page.GetByTestId("locale-switch-ar").ClickAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(panel).ToContainTextAsync("أفلت الملفات هنا أو انقر للتصفح");
        await Assertions.Expect(Page.GetByTestId("locale-switch-dir")).ToHaveTextAsync("rtl");
        // The pagination is in Arabic too.
        await Assertions.Expect(panel.GetByLabel("الصفحة التالية").First).ToBeVisibleAsync();

        await Page.GetByTestId("locale-switch-en").ClickAsync();
        await Assertions.Expect(panel).ToHaveAttributeAsync("dir", "ltr");
        await Assertions.Expect(panel).ToContainTextAsync("Drop files here or click to browse");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task An_extended_locale_overrides_one_message_and_keeps_the_rest(string mode)
    {
        await Open(mode, "locale-select");

        var panel = Page.GetByTestId("locale-extend");
        await Assertions.Expect(panel).ToContainTextAsync("Glissez-déposez vos fichiers ici ou cliquez pour parcourir");
        await Assertions.Expect(panel).ToContainTextAsync("Taille maximale 1,5 Ko");
        await Assertions.Expect(Page.GetByTestId("locale-extend-code")).ToHaveTextAsync("fr-CA");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_page_follows_the_culture_cookie_on_the_server_and_in_the_client(string mode)
    {
        await SetCultureCookie("ar");
        await Open(mode, "locale-select");

        // The interactive render (WebAssembly included) agrees with the prerender: same culture, same locale.
        await Assertions.Expect(Page.GetByTestId("locale-current")).ToHaveTextAsync("ar");
        await Assertions.Expect(Page.GetByTestId("locale-resolved")).ToHaveTextAsync("ar");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "ar");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "rtl");
        await Assertions.Expect(Page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox, new() { Name = "اللغة" })).ToBeVisibleAsync();
        await Assertions.Expect(Page.GetByTestId("locale-select").Locator("bx-select")).ToHaveAttributeAsync("value", "ar");
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task Slideover_docks_to_the_end_edge_which_is_the_left_one_in_right_to_left(string mode)
    {
        await SetCultureCookie("ar");
        await Open(mode, "slideover");

        await Page.GetByTestId("slideover-trigger").ClickAsync();

        var dialog = Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit profile" });
        await Assertions.Expect(dialog).ToBeVisibleAsync();
        await Assertions.Expect(dialog).ToHaveCSSAsync("translate", "0px");
        Assert.True(await dialog.EvaluateAsync<bool>("d => Math.abs(d.getBoundingClientRect().left) < 1 && d.getBoundingClientRect().height >= innerHeight - 1"));
        await Assertions.Expect(dialog.GetByRole(AriaRole.Button, new() { Name = "إغلاق" })).ToBeVisibleAsync();
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task The_switch_thumb_moves_towards_the_left_when_checked_in_right_to_left(string mode)
    {
        await SetCultureCookie("ar");
        await Open(mode, "switch");

        var toggle = Page.GetByRole(AriaRole.Switch, new() { Name = "Email notifications" });
        var thumb = Page.Locator("[data-slot=thumb]").First;
        const string centre = "e => { const r = e.getBoundingClientRect(); return r.left + r.width / 2; }";
        var track = await toggle.EvaluateAsync<double>(centre);

        // Unchecked, the thumb sits at the start edge: the right one in right to left.
        Assert.True(await thumb.EvaluateAsync<double>(centre) > track, "the thumb starts on the right");

        await toggle.ClickAsync(new() { Force = true });
        await Page.WaitForFunctionAsync("([t, c]) => { const e = document.querySelector('[data-slot=thumb]'); const r = e.getBoundingClientRect(); return r.left + r.width / 2 < c; }", new object[] { 0, track });
    }

    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_regional_culture_picks_its_own_locale_then_its_language(string mode)
    {
        await SetCultureCookie("pt-BR");
        await Open(mode, "locale-select");

        await Assertions.Expect(Page.GetByTestId("locale-resolved")).ToHaveTextAsync("pt-BR");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("lang", "pt-BR");
        await Assertions.Expect(Page.Locator("html")).ToHaveAttributeAsync("dir", "ltr");
        await Assertions.Expect(Page.GetByTestId("locale-select").GetByRole(AriaRole.Combobox, new() { Name = "Idioma" })).ToBeVisibleAsync();
    }
}
