using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>Sizes, text and link helpers shared by the page, marketing and content controls.</summary>
internal static class PagesUi
{
    // The Tailwind breakpoints of the web library, in device-independent units.
    public const double Sm = 640;
    public const double Md = 768;
    public const double Lg = 1024;

    /// <summary>Picks the value of the widest breakpoint the width reaches: <c>lg</c>, then <c>sm</c>, else the phone value.</summary>
    public static T Pick<T>(double width, T phone, T sm, T lg) => width >= Lg ? lg : width >= Sm ? sm : phone;

    /// <summary>A label with a theme color. It starts hidden: <see cref="Show"/> shows it when it has text.</summary>
    public static Label Text(double fontSize, KlyThemeColor color, FontAttributes attributes = FontAttributes.None)
    {
        var label = new Label { FontSize = fontSize, FontAttributes = attributes, IsVisible = false };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    /// <summary>The small upper-case label above a title (the web <c>font-mono text-xs uppercase tracking-wider</c> eyebrow).</summary>
    public static Label Eyebrow()
    {
        var label = Text(12, KlyTheme.Resolve(KlyColor.Primary).Text);
        label.TextTransform = TextTransform.Uppercase;
        label.CharacterSpacing = 0.6;
        return label;
    }

    public static void Show(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    public static void Heading(Label label, int? level) =>
        SemanticProperties.SetHeadingLevel(label, level is { } l ? (SemanticHeadingLevel)Math.Clamp(l, 1, 6) : SemanticHeadingLevel.None);

    /// <summary>An empty container that hides itself until it has a view.</summary>
    public static ContentView Slot() => new() { IsVisible = false };

    public static void Put(ContentView slot, View? view)
    {
        slot.Content = view;
        slot.IsVisible = view is not null;
    }

    /// <summary>A wrapping row. The web <c>gap</c> becomes the margin of each child.</summary>
    public static FlexLayout Row() => new() { Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center, IsVisible = false };

    public static BoxView Line(KlyThemeColor color, bool vertical = false)
    {
        var line = new BoxView { HeightRequest = vertical ? -1 : 1, WidthRequest = vertical ? 1 : -1 };
        line.SetThemeColor(BoxView.ColorProperty, color);
        return line;
    }

    public static KlyIcon Icon(string name, KlySize size, KlyThemeColor? color = null) => new() { Name = name, Size = size, IconColor = color };

    /// <summary>Fills a row with one button per link: the buttons keep the color and variant of their link.</summary>
    public static void FillButtons(FlexLayout host, IReadOnlyList<PageLinkItem>? links, KlySize size, double gapX, double gapY, Action<PageLinkItem> clicked)
    {
        host.Children.Clear();
        host.IsVisible = links is { Count: > 0 };
        if (links is null) return;
        foreach (var link in links)
        {
            var button = new KlyButton { Text = link.Label, Variant = link.Variant, Size = size, IsEnabled = !link.Disabled, Margin = new Thickness(0, 0, gapX, gapY) };
            if (link.Color is { } color) button.Color = color;
            button.Clicked += (_, _) => clicked(link);
            host.Children.Add(button);
        }
    }

    /// <summary>Makes a view react to a tap.</summary>
    public static void Tappable(View view, Action tapped)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => tapped();
        view.GestureRecognizers.Add(tap);
    }
}
