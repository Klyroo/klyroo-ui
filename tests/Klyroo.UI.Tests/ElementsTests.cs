using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

public class ElementsTests : KlyrooTestContext
{
    [Fact]
    public void Kbd_renders_a_kbd_element_with_variant_and_default_neutral_color()
    {
        var cut = Render<KlyKbd>(p => p.Add(k => k.Value, "Ctrl").Add(k => k.Variant, KlyVariant.Solid));

        var kbd = cut.Find("kbd");
        Assert.Equal("Ctrl", kbd.TextContent);
        Assert.Equal("neutral", kbd.GetAttribute("data-color"));
        Assert.Contains("bg-(--kly-color)", kbd.ClassList);
    }

    [Fact]
    public void Separator_without_content_is_a_horizontal_separator()
    {
        var cut = Render<KlySeparator>();

        var separator = cut.Find("[role=separator]");
        Assert.False(separator.HasAttribute("aria-orientation"));
        Assert.False(separator.HasAttribute("aria-label"));
        Assert.Single(cut.FindAll("span"));
    }

    [Fact]
    public void Separator_vertical_declares_its_orientation()
    {
        var cut = Render<KlySeparator>(p => p.Add(s => s.Orientation, KlyOrientation.Vertical));

        Assert.Equal("vertical", cut.Find("[role=separator]").GetAttribute("aria-orientation"));
    }

    [Fact]
    public void Separator_label_names_the_separator_and_its_copy_is_hidden()
    {
        var cut = Render<KlySeparator>(p => p.Add(s => s.Label, "or"));

        Assert.Equal("or", cut.Find("[role=separator]").GetAttribute("aria-label"));
        var container = cut.Find("[aria-hidden=true]");
        Assert.Equal("or", container.TextContent.Trim());
    }

    [Fact]
    public void Skeleton_is_hidden_from_assistive_technology_and_pulses()
    {
        var cut = Render<KlySkeleton>(p => p.Add(s => s.Class, "h-4 w-40"));

        var skeleton = cut.Find("div");
        Assert.Equal("true", skeleton.GetAttribute("aria-hidden"));
        Assert.Contains("animate-pulse", skeleton.ClassList);
        Assert.Contains("w-40", skeleton.ClassList);
    }

    [Fact]
    public void Chip_dot_is_hidden_without_a_label()
    {
        var cut = Render<KlyChip>(p => p.AddChildContent("<b>child</b>"));

        var indicator = cut.Find("[data-slot=indicator]");
        Assert.Equal("true", indicator.GetAttribute("aria-hidden"));
        Assert.Contains("top-0", indicator.ClassList);
        Assert.Contains("end-0", indicator.ClassList);
        Assert.Contains("size-2.5", indicator.ClassList);
    }

    [Fact]
    public void Chip_with_a_label_is_an_image_named_by_it_and_shows_the_text()
    {
        var cut = Render<KlyChip>(p => p.Add(c => c.Text, "5").Add(c => c.Label, "5 notifications").Add(c => c.Position, KlyChipPosition.BottomLeft));

        var indicator = cut.Find("[data-slot=indicator]");
        Assert.Equal("img", indicator.GetAttribute("role"));
        Assert.Equal("5 notifications", indicator.GetAttribute("aria-label"));
        Assert.Equal("5", indicator.TextContent);
        Assert.Contains("bottom-0", indicator.ClassList);
        Assert.Contains("start-0", indicator.ClassList);
    }

    [Fact]
    public void Chip_show_false_removes_the_indicator_but_keeps_the_child()
    {
        var cut = Render<KlyChip>(p => p.Add(c => c.Show, false).AddChildContent("<b>child</b>"));

        Assert.Empty(cut.FindAll("[data-slot=indicator]"));
        Assert.Equal("child", cut.Find("b").TextContent);
    }

    [Fact]
    public void Chip_standalone_is_not_positioned()
    {
        var cut = Render<KlyChip>(p => p.Add(c => c.Standalone, true).Add(c => c.Text, "3"));

        var indicator = cut.Find("[data-slot=indicator]");
        Assert.DoesNotContain("absolute", indicator.ClassList);
        Assert.DoesNotContain("top-0", indicator.ClassList);
    }

    [Fact]
    public void Link_is_active_on_its_exact_path_with_aria_current()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/docs/theming");

        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/docs/theming").Add(l => l.Label, "Theming"));

        var link = cut.Find("a");
        Assert.Equal("page", link.GetAttribute("aria-current"));
        Assert.Contains("text-(--klyroo-primary-text)", link.ClassList);
    }

    [Fact]
    public void Link_prefix_matches_by_segment_unless_exact()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/docs/theming?tab=1#top");

        var prefix = Render<KlyLink>(p => p.Add(l => l.Href, "/docs"));
        var exact = Render<KlyLink>(p => p.Add(l => l.Href, "/docs").Add(l => l.Exact, true));
        var sibling = Render<KlyLink>(p => p.Add(l => l.Href, "/doc"));

        Assert.Equal("page", prefix.Find("a").GetAttribute("aria-current"));
        Assert.False(exact.Find("a").HasAttribute("aria-current"));
        Assert.False(sibling.Find("a").HasAttribute("aria-current"));
    }

    [Fact]
    public void Link_to_the_root_is_active_only_on_the_root()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/docs");

        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/"));
        Assert.False(cut.Find("a").HasAttribute("aria-current"));

        nav.NavigateTo("/");
        Assert.Equal("page", cut.Find("a").GetAttribute("aria-current"));
    }

    [Fact]
    public void Link_ignores_other_hosts_and_follows_navigation()
    {
        var nav = Services.GetRequiredService<NavigationManager>();
        var external = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com/"));
        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/pricing"));

        Assert.False(external.Find("a").HasAttribute("aria-current"));
        Assert.False(cut.Find("a").HasAttribute("aria-current"));

        nav.NavigateTo("/pricing");

        cut.WaitForAssertion(() => Assert.Equal("page", cut.Find("a").GetAttribute("aria-current")));
    }

    [Fact]
    public void Link_applies_active_and_inactive_classes()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/a");

        var active = Render<KlyLink>(p => p.Add(l => l.Href, "/a").Add(l => l.ActiveClass, "is-active").Add(l => l.InactiveClass, "is-idle"));
        var inactive = Render<KlyLink>(p => p.Add(l => l.Href, "/b").Add(l => l.ActiveClass, "is-active").Add(l => l.InactiveClass, "is-idle"));

        Assert.Contains("is-active", active.Find("a").ClassList);
        Assert.DoesNotContain("is-idle", active.Find("a").ClassList);
        Assert.Contains("is-idle", inactive.Find("a").ClassList);
    }

    [Fact]
    public void Link_active_parameter_overrides_the_comparison()
    {
        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/elsewhere").Add(l => l.Active, true));

        Assert.Equal("page", cut.Find("a").GetAttribute("aria-current"));
    }

    [Fact]
    public void Link_disabled_has_no_href_and_is_marked_disabled()
    {
        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/x").Add(l => l.Disabled, true));

        var link = cut.Find("a");
        Assert.False(link.HasAttribute("href"));
        Assert.Equal("link", link.GetAttribute("role"));
        Assert.Equal("true", link.GetAttribute("aria-disabled"));
    }

    [Fact]
    public void Link_blank_target_gets_a_safe_rel()
    {
        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "https://example.com").Add(l => l.Target, KlyLinkTarget.Blank));

        Assert.Equal("noopener noreferrer", cut.Find("a").GetAttribute("rel"));
    }

    [Fact]
    public void Icon_renders_a_builtin_icon_hidden_by_default()
    {
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "search"));

        var svg = cut.Find("svg");
        Assert.Equal("true", svg.GetAttribute("aria-hidden"));
        Assert.Equal("search", svg.GetAttribute("data-icon"));
        Assert.NotEmpty(cut.Find("path").GetAttribute("d")!);
    }

    [Fact]
    public void Icon_with_a_label_is_an_image()
    {
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "check").Add(i => i.Label, "Done"));

        var svg = cut.Find("svg");
        Assert.Equal("img", svg.GetAttribute("role"));
        Assert.Equal("Done", svg.GetAttribute("aria-label"));
        Assert.False(svg.HasAttribute("aria-hidden"));
    }

    [Fact]
    public void Icon_unknown_name_renders_no_shape()
    {
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "no-such-icon"));

        Assert.Empty(cut.FindAll("path"));
    }

    [Fact]
    public void Icon_helper_draws_a_builtin_icon_with_its_class()
    {
        var named = Render(KlyIcons.Named("bell", "size-4"));

        Assert.NotEmpty(named.FindAll("svg path"));
        Assert.Equal("currentColor", named.Find("svg path").GetAttribute("stroke"));
        Assert.Contains("size-4", named.Find("svg").ClassList);
        Assert.Equal("0 0 24 24", named.Find("svg").GetAttribute("viewBox"));
        Assert.Equal("true", named.Find("svg").GetAttribute("aria-hidden"));
    }

    [Fact]
    public void Container_renders_the_requested_element_with_its_max_width()
    {
        var cut = Render<KlyContainer>(p => p.Add(c => c.As, KlyElement.Main).Add(c => c.Size, KlySize.Sm).AddChildContent("Hi"));

        var main = cut.Find("main");
        Assert.Contains("max-w-4xl", main.ClassList);
        Assert.Contains("mx-auto", main.ClassList);
        Assert.Equal("Hi", main.TextContent);
    }

    [Fact]
    public void Progress_determinate_exposes_value_range_and_name()
    {
        var cut = Render<KlyProgress>(p => p.Add(b => b.Value, 40).Add(b => b.AriaLabel, "Upload").Add(b => b.Status, true));

        var bar = cut.Find("[role=progressbar]");
        Assert.Equal("40", bar.GetAttribute("aria-valuenow"));
        Assert.Equal("0", bar.GetAttribute("aria-valuemin"));
        Assert.Equal("100", bar.GetAttribute("aria-valuemax"));
        Assert.Equal("Upload", bar.GetAttribute("aria-label"));
        Assert.Equal("40%", bar.GetAttribute("aria-valuetext"));
        Assert.Equal("width: 40%", cut.Find("[role=progressbar] > div").GetAttribute("style"));
        Assert.Equal("40%", cut.Find("[aria-hidden=true]").TextContent.Trim());
    }

    [Fact]
    public void Progress_clamps_the_value_to_the_range()
    {
        var cut = Render<KlyProgress>(p => p.Add(b => b.Value, 150));

        Assert.Equal("100", cut.Find("[role=progressbar]").GetAttribute("aria-valuenow"));
    }

    [Fact]
    public void Progress_without_a_value_is_indeterminate_and_respects_reduced_motion()
    {
        var cut = Render<KlyProgress>();

        var bar = cut.Find("[role=progressbar]");
        Assert.False(bar.HasAttribute("aria-valuenow"));
        Assert.Equal("Progress", bar.GetAttribute("aria-label"));
        var indicator = cut.Find("[role=progressbar] > div");
        Assert.False(indicator.HasAttribute("style"));
        Assert.Contains("motion-reduce:animate-none", indicator.ClassList);
    }

    [Fact]
    public void Progress_steps_label_the_bar_and_mark_the_current_step()
    {
        var cut = Render<KlyProgress>(p => p.Add(b => b.Value, 1).Add(b => b.Steps, new[] { "Cart", "Shipping", "Done" }));

        var bar = cut.Find("[role=progressbar]");
        Assert.Equal("2", bar.GetAttribute("aria-valuemax"));
        Assert.Equal("Shipping", bar.GetAttribute("aria-valuetext"));
        var states = cut.FindAll("[data-state]").Select(s => s.GetAttribute("data-state")).ToArray();
        Assert.Equal(["completed", "active", "inactive"], states);
    }

    [Fact]
    public void Banner_renders_the_element_that_persists_its_dismissal()
    {
        var cut = Render<KlyBanner>(p => p.Add(b => b.Id, "launch").Add(b => b.Title, "Hello").Add(b => b.Close, true));

        Assert.Equal("launch", cut.Find("kly-banner").GetAttribute("data-kly-banner-id"));
        Assert.Equal("Close", cut.Find("button[data-slot=close]").GetAttribute("aria-label"));
        Assert.Equal("Hello", cut.Find("[data-slot=title]").TextContent);
    }

    [Fact]
    public void Banner_with_a_link_makes_the_title_the_link()
    {
        var cut = Render<KlyBanner>(p => p.Add(b => b.Title, "Read").Add(b => b.Href, "/news"));

        var link = cut.Find("a[data-slot=title]");
        Assert.Equal("/news", link.GetAttribute("href"));
        Assert.Contains("after:absolute", link.ClassList);
        Assert.Empty(cut.FindAll("button"));
    }

    [Fact]
    public void Empty_renders_icon_title_description_and_actions()
    {
        var cut = Render<KlyEmpty>(p => p
            .Add(e => e.Title, "Nothing here")
            .Add(e => e.Description, "Add something")
            .Add(e => e.Icon, KlyIcons.Named("inbox"))
            .Add(e => e.Actions, (RenderFragment)(b => b.AddMarkupContent(0, "<button>Add</button>"))));

        Assert.Equal("Nothing here", cut.Find("[data-slot=title]").TextContent);
        Assert.Equal("Add something", cut.Find("[data-slot=description]").TextContent);
        Assert.Equal("true", cut.Find("svg").ParentElement!.GetAttribute("aria-hidden"));
        Assert.Equal("Add", cut.Find("button").TextContent);
    }

    [Fact]
    public void Empty_title_can_be_a_heading()
    {
        var cut = Render<KlyEmpty>(p => p.Add(e => e.Title, "Nothing").Add(e => e.HeadingLevel, 3));

        Assert.Equal("Nothing", cut.Find("h3").TextContent);
    }

    [Fact]
    public void User_shows_a_decorative_avatar_with_the_name_and_description()
    {
        var cut = Render<KlyUser>(p => p.Add(u => u.Name, "Ada Lovelace").Add(u => u.Description, "Mathematician"));

        Assert.Equal("Ada Lovelace", cut.Find("[data-slot=name]").TextContent);
        Assert.Equal("Mathematician", cut.Find("[data-slot=description]").TextContent);
        var avatar = cut.Find("[role=img]");
        Assert.Equal("true", avatar.ParentElement!.GetAttribute("aria-hidden"));
        Assert.Equal("AL", avatar.TextContent.Trim());
        Assert.Empty(cut.FindAll("a"));
    }

    [Fact]
    public void User_with_an_href_is_a_link()
    {
        var cut = Render<KlyUser>(p => p.Add(u => u.Name, "Ada").Add(u => u.Href, "/ada"));

        Assert.Equal("/ada", cut.Find("a").GetAttribute("href"));
    }

    [Fact]
    public void AvatarGroup_collapses_the_overflow_into_a_named_plus_avatar()
    {
        KlyAvatarItem[] items = [new(Alt: "Ada"), new(Alt: "Grace"), new(Alt: "Alan"), new(Alt: "Edsger")];

        var cut = Render<KlyAvatarGroup>(p => p.Add(g => g.Items, items).Add(g => g.Max, 2).Add(g => g.AriaLabel, "Team"));

        Assert.Equal("Team", cut.Find("[role=group]").GetAttribute("aria-label"));
        var avatars = cut.FindAll("[role=img]");
        Assert.Equal(3, avatars.Count);
        Assert.Equal("2 more", avatars[2].GetAttribute("aria-label"));
        Assert.Equal("+2", avatars[2].TextContent.Trim());
    }

    [Fact]
    public void AvatarGroup_without_max_shows_everyone()
    {
        KlyAvatarItem[] items = [new(Alt: "Ada"), new(Alt: "Grace")];

        var cut = Render<KlyAvatarGroup>(p => p.Add(g => g.Items, items));

        Assert.Equal(2, cut.FindAll("[role=img]").Count);
    }

    [Fact]
    public void FieldGroup_is_a_group_whose_children_lose_their_inner_radii()
    {
        var cut = Render<KlyFieldGroup>(p => p.Add(g => g.AriaLabel, "Actions").AddChildContent("<button>a</button><button>b</button>"));

        var group = cut.Find("[role=group]");
        Assert.Equal("Actions", group.GetAttribute("aria-label"));
        Assert.Contains("[&>:not(:first-child)]:rounded-s-none", group.ClassList);
        Assert.Contains("[&>:not(:last-child)]:rounded-e-none", group.ClassList);
    }

    [Fact]
    public void FieldGroup_vertical_removes_the_vertical_radii()
    {
        var cut = Render<KlyFieldGroup>(p => p.Add(g => g.Orientation, KlyOrientation.Vertical));

        var group = cut.Find("[role=group]");
        Assert.Contains("flex-col", group.ClassList);
        Assert.Contains("[&>:not(:first-child)]:rounded-t-none", group.ClassList);
    }
}

public class ElementsCustomIconTests() : KlyrooTestContext(o =>
{
    o.Icons.Register("brand", "<path d=\"M1 1h22\"/>");
    o.Icons.Register("search", "<path d=\"M2 2h20\"/>");
})
{
    [Fact]
    public void Registered_icons_replace_and_extend_the_builtin_set()
    {
        var custom = Render<KlyIcon>(p => p.Add(i => i.Name, "brand"));
        var replaced = Render<KlyIcon>(p => p.Add(i => i.Name, "search"));

        Assert.Equal("M1 1h22", custom.Find("path").GetAttribute("d"));
        Assert.Equal("M2 2h20", replaced.Find("path").GetAttribute("d"));
    }
}

public class ElementsStaticTests() : KlyrooTestContext(interactive: false)
{
    [Fact]
    public void Link_computes_the_active_state_in_static_rendering()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/docs/a");

        var cut = Render<KlyLink>(p => p.Add(l => l.Href, "/docs"));

        Assert.Equal("page", cut.Find("a").GetAttribute("aria-current"));
    }
}
