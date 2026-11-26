using System.Reflection;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Tests.Icons;

public class IconNameRenderingTests : KlyrooTestContext
{
    [Theory]
    [InlineData("search")]
    [InlineData("lucide:search")]
    [InlineData("i-lucide-search")]
    public void Every_form_of_a_builtin_name_draws_the_same_icon(string name)
    {
        var expected = Render<KlyIcon>(p => p.Add(i => i.Name, "search")).Find("svg").InnerHtml;
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, name));

        Assert.Equal(expected, cut.Find("svg").InnerHtml);
        Assert.False(cut.Find("svg").HasAttribute("data-kly-missing-icon"));
    }

    [Fact]
    public void An_icon_of_the_app_bundle_is_found_in_both_name_forms_and_keeps_its_view_box()
    {
        KlyrooIconBundle.Add("testset", "dot", "<circle cx=\"4\" cy=\"4\" r=\"3\"/>", "0 0 8 8");

        foreach (var name in new[] { "testset:dot", "i-testset-dot" })
        {
            var cut = Render<KlyIcon>(p => p.Add(i => i.Name, name));
            Assert.Equal("0 0 8 8", cut.Find("svg").GetAttribute("viewBox"));
            Assert.Equal("3", cut.Find("svg circle").GetAttribute("r"));
        }
    }

    [Fact]
    public void An_unknown_name_renders_an_empty_icon_that_can_be_found_in_the_page()
    {
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "ph:no-such-icon"));

        var svg = cut.Find("svg");
        Assert.Equal("ph:no-such-icon", svg.GetAttribute("data-kly-missing-icon"));
        Assert.Empty(svg.Children);
        Assert.Equal("0 0 24 24", svg.GetAttribute("viewBox"));
    }

    [Fact]
    public void The_render_fragment_helper_marks_a_missing_icon_too()
    {
        var cut = Render(KlyIcons.Named("i-ph-no-such-icon", "size-4"));

        Assert.Equal("i-ph-no-such-icon", cut.Find("svg").GetAttribute("data-kly-missing-icon"));
        Assert.Contains("size-4", cut.Find("svg").ClassList);
    }

    [Fact]
    public void Previously_shipped_names_still_work()
    {
        string[] names =
        [
            "x", "check", "minus", "plus", "chevron-down", "chevron-up", "chevron-left", "chevron-right", "chevrons-up-down", "arrow-up", "arrow-down",
            "arrow-left", "arrow-right", "arrow-up-right", "info", "circle-check", "circle-x", "circle-alert", "circle-help", "triangle-alert", "search",
            "panel-left", "menu", "list", "ellipsis", "home", "user", "mail", "bell", "calendar", "clock", "trash", "pencil", "copy", "download", "upload",
            "external-link", "link", "eye", "lock", "star", "heart", "bookmark", "inbox", "file", "folder", "image", "message-square", "sun", "moon", "globe",
            "map-pin", "zap", "code", "terminal", "log-out", "refresh-cw", "loader",
        ];

        Assert.Equal(58, names.Length);
        Assert.All(names, n => Assert.True(IconResolver.TryResolve(n, null, out _), n));
    }

    [Fact]
    public void The_builtin_list_of_the_generator_is_what_the_library_embeds()
    {
        var root = DefaultTokenTests.FindLibraryRoot();
        var json = File.ReadAllText(Path.Combine(root, "..", "..", "build", "builtin-icons.json"));
        var names = Regex.Matches(json[json.IndexOf("\"icons\"", StringComparison.Ordinal)..], "\"([a-z0-9-]+)\"").Select(m => m.Groups[1].Value).Where(n => n != "icons");

        Assert.Equal(names.Distinct().Order(StringComparer.Ordinal), BuiltinIcons.Lucide.Keys.Order(StringComparer.Ordinal));
    }
}

public class AppIconOptionsTests
{
    private static readonly string[] NuxtKeys =
    [
        "ArrowDown", "ArrowLeft", "ArrowRight", "ArrowUp", "Caution", "Check", "ChevronDoubleLeft", "ChevronDoubleRight", "ChevronDown", "ChevronLeft",
        "ChevronRight", "ChevronUp", "Close", "Copy", "CopyCheck", "Dark", "Drag", "Edit", "Ellipsis", "Error", "External", "Eye", "EyeOff", "File",
        "Folder", "FolderOpen", "Hash", "Info", "Light", "Loading", "Menu", "Minus", "PanelClose", "PanelOpen", "Plus", "Reload", "Search", "Stop", "Star",
        "Success", "System", "Tip", "Trash", "Upload", "Warning",
    ];

    private static readonly Dictionary<string, string> NuxtDefaults = new()
    {
        ["ArrowDown"] = "arrow-down", ["ArrowLeft"] = "arrow-left", ["ArrowRight"] = "arrow-right", ["ArrowUp"] = "arrow-up", ["Caution"] = "circle-alert",
        ["Check"] = "check", ["ChevronDoubleLeft"] = "chevrons-left", ["ChevronDoubleRight"] = "chevrons-right", ["ChevronDown"] = "chevron-down",
        ["ChevronLeft"] = "chevron-left", ["ChevronRight"] = "chevron-right", ["ChevronUp"] = "chevron-up", ["Close"] = "x", ["Copy"] = "copy",
        ["CopyCheck"] = "copy-check", ["Dark"] = "moon", ["Drag"] = "grip-vertical", ["Edit"] = "pencil", ["Ellipsis"] = "ellipsis", ["Error"] = "circle-x",
        ["External"] = "arrow-up-right", ["Eye"] = "eye", ["EyeOff"] = "eye-off", ["File"] = "file", ["Folder"] = "folder", ["FolderOpen"] = "folder-open",
        ["Hash"] = "hash", ["Info"] = "info", ["Light"] = "sun", ["Loading"] = "loader-circle", ["Menu"] = "menu", ["Minus"] = "minus",
        ["PanelClose"] = "panel-left-close", ["PanelOpen"] = "panel-left-open", ["Plus"] = "plus", ["Reload"] = "rotate-ccw", ["Search"] = "search",
        ["Stop"] = "square", ["Star"] = "star", ["Success"] = "circle-check", ["System"] = "monitor", ["Tip"] = "lightbulb", ["Trash"] = "trash",
        ["Upload"] = "upload", ["Warning"] = "triangle-alert",
    };

    [Fact]
    public void Has_the_45_keys_of_nuxt_ui_with_their_lucide_defaults()
    {
        var options = new KlyrooIconOptions();
        Assert.Equal(45, NuxtKeys.Length);
        foreach (var key in NuxtKeys)
        {
            var value = (string)typeof(KlyrooIconOptions).GetProperty(key)!.GetValue(options)!;
            Assert.Equal("i-lucide-" + NuxtDefaults[key], value);
        }
    }

    [Fact]
    public void Every_default_icon_is_built_into_the_library()
    {
        var options = new KlyrooIconOptions();
        var properties = typeof(KlyrooIconOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.PropertyType == typeof(string)).ToList();

        Assert.True(properties.Count >= 45);
        Assert.All(properties, p => Assert.True(IconResolver.TryResolve((string)p.GetValue(options)!, null, out _), p.Name));
    }

    [Fact]
    public void A_key_can_be_replaced_until_an_icon_is_drawn()
    {
        var options = new KlyrooOptions();
        options.Icons.Close = "ph:x";
        Assert.Equal("ph:x", options.Icons.Close);

        IconResolver.TryResolve("search", options.Icons, out _);

        var e = Assert.Throws<InvalidOperationException>(() => options.Icons.Close = "mdi:close");
        Assert.Contains("AddKlyrooUi", e.Message);
        Assert.Throws<InvalidOperationException>(() => options.Icons.Register("a", "<g/>"));
    }

    [Fact]
    public void An_empty_name_is_refused()
    {
        Assert.Throws<ArgumentException>(() => new KlyrooIconOptions { Close = " " });
    }

    [Fact]
    public void Register_validates_the_name_and_the_body()
    {
        var options = new KlyrooIconOptions();
        Assert.Throws<ArgumentException>(() => options.Register("Not Valid", "<g/>"));
        Assert.Throws<ArgumentException>(() => options.Register("custom:x", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Register("custom:x", "<g/>", 0, 24));
    }

    [Fact]
    public void RegisterSvg_reads_the_view_box_and_the_content_of_a_file()
    {
        var options = new KlyrooIconOptions();
        options.RegisterSvg("custom:wide", "<svg viewBox=\"0 0 40 10\" fill=\"none\" stroke=\"currentColor\"><path d=\"M0 5h40\"/></svg>");

        Assert.True(IconResolver.TryResolve("i-custom-wide", options, out var body));
        Assert.Equal("0 0 40 10", body.ViewBox);
        Assert.Equal("<g fill=\"none\" stroke=\"currentColor\"><path d=\"M0 5h40\"/></g>", body.Body);
        Assert.Throws<ArgumentException>(() => options.RegisterSvg("custom:bad", "<div/>"));
    }

    [Fact]
    public void A_registered_icon_is_private_to_its_options()
    {
        var one = new KlyrooIconOptions();
        var two = new KlyrooIconOptions();
        one.Register("only-one:thing", "<g/>");

        Assert.True(IconResolver.TryResolve("only-one:thing", one, out _));
        Assert.False(IconResolver.TryResolve("only-one:thing", two, out _));
    }
}

public class AppIconsInComponentsTests() : KlyrooTestContext(o =>
{
    o.Icons.Register("app:one", "<path d=\"M1 1\"/>", 8, 8);
    o.Icons.Register("app:two", "<path d=\"M2 2\"/>", 9, 9);
    o.Icons.Register("app:three", "<path d=\"M3 3\"/>", 10, 10);
    o.Icons.Close = "app:one";
    o.Icons.ChevronUpDown = "app:two";
    o.Icons.Loading = "app:three";
    o.Icons.Check = "app:one";
    o.Icons.Star = "app:two";
})
{
    [Fact]
    public void Alert_close_uses_the_close_icon_of_the_app()
    {
        var cut = Render<KlyAlert>(p => p.Add(a => a.Title, "x").Add(a => a.Close, true));

        var svg = cut.Find("button svg");
        Assert.Equal("0 0 8 8", svg.GetAttribute("viewBox"));
        Assert.Equal("M1 1", svg.QuerySelector("path")!.GetAttribute("d"));
    }

    [Fact]
    public void Select_trigger_uses_the_up_down_icon_of_the_app()
    {
        var cut = Render<KlySelect<string>>(p => p.Add(s => s.Items, [new KlySelectItem("a", "A")]));

        Assert.Equal("0 0 9 9", cut.Find("button svg").GetAttribute("viewBox"));
    }

    [Fact]
    public void Loading_button_uses_the_loading_icon_of_the_app()
    {
        var cut = Render<KlyButton>(p => p.Add(b => b.Label, "Save").Add(b => b.Loading, true));

        var svg = cut.Find("[data-slot=leadingIcon] svg");
        Assert.Equal("0 0 10 10", svg.GetAttribute("viewBox"));
    }

    [Fact]
    public void Checkbox_and_rating_use_their_icons()
    {
        var checkbox = Render<KlyCheckbox>(p => p.Add(c => c.Label, "x"));
        Assert.Contains(checkbox.FindAll("svg"), s => s.GetAttribute("viewBox") == "0 0 8 8");

        var rating = Render<KlyInputRating>(p => p.Add(r => r.Max, 3));
        Assert.Equal(3, rating.FindAll("svg").Count(s => s.GetAttribute("viewBox") == "0 0 9 9"));
    }

    [Fact]
    public void The_icons_that_script_draws_are_handed_over_as_attributes()
    {
        var toaster = Render<KlyToaster>();
        var icon = toaster.Find("kly-toaster").GetAttribute("data-icon-close")!;
        Assert.StartsWith("<svg viewBox=\"0 0 8 8\"", icon);
        Assert.Contains("<path d=\"M1 1\"/>", icon);

        var tags = Render<KlyInputTags>();
        Assert.StartsWith("<svg viewBox=\"0 0 8 8\"", tags.Find("kly-input-tags").GetAttribute("data-icon-close"));
    }
}

public class DefaultAppIconsInComponentsTests : KlyrooTestContext
{
    [Fact]
    public void Components_draw_lucide_icons_by_default()
    {
        var alert = Render<KlyAlert>(p => p.Add(a => a.Title, "x").Add(a => a.Close, true));

        Assert.Equal("0 0 24 24", alert.Find("button svg").GetAttribute("viewBox"));
        Assert.Equal("M18 6L6 18M6 6l12 12", alert.Find("button svg path").GetAttribute("d"));
    }

    [Fact]
    public void A_button_that_loads_draws_the_spinner_in_its_leading_slot()
    {
        var cut = Render<KlyButton>(p => p.Add(b => b.Label, "Save").Add(b => b.Loading, true));

        Assert.Single(cut.FindAll("[data-slot=leadingIcon] svg"));
    }

    [Fact]
    public void The_calendar_navigation_icons_come_from_dotnet()
    {
        var cut = Render<KlyCalendar>();

        Assert.StartsWith("<svg", cut.Find("kly-calendar").GetAttribute("data-icon-prev"));
        Assert.StartsWith("<svg", cut.Find("kly-calendar").GetAttribute("data-icon-next"));
    }
}

/// <summary>The icons live in one place: no component or script keeps a copy of an icon's shapes.</summary>
public class NoDuplicateIconTests
{
    private static readonly string Source = DefaultTokenTests.FindLibraryRoot();

    private static IEnumerable<string> Files(string pattern) =>
        Directory.EnumerateFiles(Source, pattern, SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    [Fact]
    public void No_razor_file_or_code_file_draws_path_data_outside_the_icon_files_and_the_chart()
    {
        var offenders = Files("*.razor").Concat(Files("*.cs"))
            .Where(f => !f.EndsWith(".g.cs") && !f.Contains(Path.DirectorySeparatorChar + "Chart" + Path.DirectorySeparatorChar))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "<path\\b|<circle\\b|<polygon\\b|\"M-?[0-9.]+[ ,][0-9.-]+[a-zA-Z]"))
            .Select(f => Path.GetRelativePath(Source, f))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void Only_the_icon_component_and_the_helpers_open_an_svg_element()
    {
        var offenders = Files("*.razor").Concat(Files("*.cs"))
            .Where(f => !f.EndsWith(".g.cs") && !f.Contains(Path.DirectorySeparatorChar + "Chart" + Path.DirectorySeparatorChar))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "<svg\\b|OpenElement\\(\\d+, \"svg\"\\)"))
            .Select(f => Path.GetFileName(f))
            .Order()
            .ToList();

        Assert.Equal(["IconRuntime.cs", "KlyIcon.razor"], offenders);
    }

    [Fact]
    public void No_script_builds_an_icon()
    {
        var offenders = Files("*.js")
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "wwwroot" + Path.DirectorySeparatorChar))
            .Where(f => Regex.IsMatch(File.ReadAllText(f), "createElementNS|<svg viewBox|<path d="))
            .Select(f => Path.GetRelativePath(Source, f))
            .ToList();

        Assert.Empty(offenders);
    }

    [Fact]
    public void The_duplicate_icon_classes_are_gone()
    {
        var classes = Files("*.cs").Concat(Files("*.razor"))
            .SelectMany(f => Regex.Matches(File.ReadAllText(f), @"static class (\w*Icons)\b").Select(m => m.Groups[1].Value))
            .Distinct()
            .Order()
            .ToList();

        Assert.Equal(["BuiltinIcons", "KlyIcons"], classes);
    }
}
