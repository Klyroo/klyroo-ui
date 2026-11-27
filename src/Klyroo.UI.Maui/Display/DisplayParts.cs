using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>Small building blocks shared by the controls of the display batch.</summary>
internal static class DisplayParts
{
    /// <summary>The text color of the primary color (<c>--klyroo-primary-text</c> on the web).</summary>
    public static KlyThemeColor PrimaryText => KlyTheme.Resolve(KlyColor.Primary).Text;

    /// <summary>A monospace font family of the platform. The default one when the platform cannot be told.</summary>
    public static string MonoFont()
    {
        try
        {
            var platform = DeviceInfo.Platform;
            if (platform == DevicePlatform.iOS || platform == DevicePlatform.MacCatalyst) return "Menlo";
            if (platform == DevicePlatform.WinUI) return "Consolas";
        }
        catch (Exception)
        {
            // Not running on a platform (unit tests): fall through to the generic family name.
        }
        return "monospace";
    }

    /// <summary>A label with a size and a color that follows the app theme.</summary>
    public static Label NewLabel(double size, KlyThemeColor color, FontAttributes attributes = FontAttributes.None, LineBreakMode lineBreak = LineBreakMode.WordWrap)
    {
        var label = new Label { FontSize = size, FontAttributes = attributes, LineBreakMode = lineBreak };
        label.SetThemeColor(Label.TextColorProperty, color);
        return label;
    }

    /// <summary>Sets a text and hides the label when there is none.</summary>
    public static void Show(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    /// <summary>A line of one device-independent unit, such as the dividers of a list.</summary>
    public static Border Divider(KlyThemeColor color)
    {
        var line = new Border { StrokeThickness = 0, HeightRequest = 1, HorizontalOptions = LayoutOptions.Fill };
        line.SetThemeColor(VisualElement.BackgroundColorProperty, color);
        AutomationProperties.SetIsInAccessibleTree(line, false);
        return line;
    }

    /// <summary>A <see cref="KlyBadgeItem"/> as a subtle small badge.</summary>
    public static KlyBadge Badge(KlyBadgeItem item) => new()
    {
        Text = item.Label,
        Color = item.Color ?? KlyColor.Neutral,
        Variant = KlyVariant.Subtle,
        Size = KlySize.Sm,
        VerticalOptions = LayoutOptions.Center,
    };

    /// <summary>A <see cref="KlyActionItem"/> as a small button. It opens the address, then runs the callback.</summary>
    public static KlyButton ActionButton(KlyActionItem item)
    {
        var button = new KlyButton { Text = item.Label, Color = item.Color, Variant = item.Variant, Size = KlySize.Sm, VerticalOptions = LayoutOptions.Center };
        button.Clicked += (_, _) =>
        {
            DisplayLaunch.Open(item.Href, item.Target);
            item.OnClick?.Invoke();
        };
        return button;
    }

    /// <summary>A borderless native button that looks like a text action ("Show more", "Read more").</summary>
    public static Button TextButton(string? text, KlyThemeColor color, double fontSize, Action onClick)
    {
        var button = new Button
        {
            Text = text,
            FontSize = fontSize,
            Padding = new Thickness(0),
            MinimumHeightRequest = 40,
            BackgroundColor = Colors.Transparent,
            BorderWidth = 0,
            TextTransform = TextTransform.None,
            HorizontalOptions = LayoutOptions.Start,
        };
        button.SetThemeColor(Button.TextColorProperty, color);
        button.Clicked += (_, _) => onClick();
        return button;
    }

    /// <summary>A label that opens an address and/or runs a callback when it is tapped.</summary>
    public static Label LinkLabel(string? text, KlyThemeColor color, double fontSize, string? href, Action? onClick, FontAttributes attributes = FontAttributes.None)
    {
        var label = NewLabel(fontSize, color, attributes, LineBreakMode.TailTruncation);
        label.Text = text;
        label.TextDecorations = TextDecorations.Underline;
        label.VerticalTextAlignment = TextAlignment.Center;
        label.MinimumHeightRequest = 40;
        Tap(label, () =>
        {
            DisplayLaunch.Open(href, null);
            onClick?.Invoke();
        });
        return label;
    }

    /// <summary>Calls <paramref name="action"/> when the view is tapped while it is enabled.</summary>
    public static void Tap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (view.IsEnabled) action();
        };
        view.GestureRecognizers.Add(tap);
    }

    /// <summary>The heading level of a screen reader for a level from 1 to 6; other values are clamped.</summary>
    public static SemanticHeadingLevel HeadingLevel(int level) => Math.Clamp(level, 1, 6) switch
    {
        1 => SemanticHeadingLevel.Level1,
        2 => SemanticHeadingLevel.Level2,
        3 => SemanticHeadingLevel.Level3,
        4 => SemanticHeadingLevel.Level4,
        5 => SemanticHeadingLevel.Level5,
        _ => SemanticHeadingLevel.Level6,
    };

    /// <summary>A number with at most two decimals, in the given culture, with no <c>-0</c>.</summary>
    public static string Number(double value, CultureInfo culture) => (value == 0 ? 0 : value).ToString("#,0.##", culture);

    /// <summary>
    /// Takes a view out of the view that holds it, so that it can be put in another one. The controls rebuild their content when a property changes,
    /// and a view the app gave them (an icon, a body) is still inside the previous content then.
    /// </summary>
    public static void Detach(View? view)
    {
        switch (view?.Parent)
        {
            case Layout layout:
                layout.Remove(view);
                break;
            case Border border:
                border.Content = null;
                break;
            case ContentView contentView:
                contentView.Content = null;
                break;
        }
    }

    /// <summary>A tile of a size and a radius with a background that follows the app theme: the leading visual of the rows.</summary>
    public static Border Tile(double size, double radius, KlyThemeColor background)
    {
        var tile = new Border { WidthRequest = size, HeightRequest = size, StrokeThickness = 0, Padding = 0, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start };
        tile.SetRadius(radius);
        tile.SetThemeColor(VisualElement.BackgroundColorProperty, background);
        return tile;
    }

    /// <summary>The icon of the library at 75% of the size of <see cref="KlySize.Sm"/>, for the small icons of rows (12 units).</summary>
    public static KlyIcon SmallIcon(string name, KlyThemeColor color) => new()
    {
        Name = name,
        Size = KlySize.Sm,
        IconColor = color,
        Scale = 0.75,
        HorizontalOptions = LayoutOptions.Center,
        VerticalOptions = LayoutOptions.Center,
    };
}
