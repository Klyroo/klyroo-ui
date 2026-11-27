using Microsoft.Maui.Controls.Xaml;

namespace Klyroo.UI.Maui;

/// <summary>
/// Styles the text inside it, such as a rendered article: every <see cref="Label"/> below it gets the body color, size and line height, and four style classes
/// give the web prose look: <c>h2</c>, <c>h3</c>, <c>a</c> (a link) and <c>blockquote</c>, plus <c>code</c>. Set them with <c>StyleClass</c>:
/// <c>new Label { StyleClass = ["h2"] }</c>. The colors follow the app theme.
/// </summary>
public class KlyPageBody : ContentView
{
    private const string Text = "KlyBodyText";
    private const string Heading = "KlyBodyHeading";
    private const string Link = "KlyBodyLink";
    private const string Muted = "KlyBodyMuted";
    private const string Code = "KlyBodyCode";

    /// <summary>Creates a page body.</summary>
    public KlyPageBody()
    {
        Margin = new Thickness(0, 48, 0, 0);
        Padding = new Thickness(0, 0, 0, 96);
        Resources.Add(new Style(typeof(Label))
        {
            Setters = { Resource(Label.TextColorProperty, Text), new Setter { Property = Label.FontSizeProperty, Value = 16d }, new Setter { Property = Label.LineHeightProperty, Value = 1.75 } },
        });
        Resources.Add(new Style(typeof(Label))
        {
            Class = "h2",
            Setters =
            {
                Resource(Label.TextColorProperty, Heading),
                new Setter { Property = Label.FontSizeProperty, Value = 24d },
                new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold },
                new Setter { Property = View.MarginProperty, Value = new Thickness(0, 48, 0, 16) },
            },
        });
        Resources.Add(new Style(typeof(Label))
        {
            Class = "h3",
            Setters =
            {
                Resource(Label.TextColorProperty, Heading),
                new Setter { Property = Label.FontSizeProperty, Value = 20d },
                new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold },
                new Setter { Property = View.MarginProperty, Value = new Thickness(0, 32, 0, 12) },
            },
        });
        Resources.Add(new Style(typeof(Label))
        {
            Class = "a",
            Setters =
            {
                Resource(Label.TextColorProperty, Link),
                new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold },
                new Setter { Property = Label.TextDecorationsProperty, Value = TextDecorations.Underline },
            },
        });
        Resources.Add(new Style(typeof(Label))
        {
            Class = "blockquote",
            Setters = { Resource(Label.TextColorProperty, Muted), new Setter { Property = View.MarginProperty, Value = new Thickness(16, 24, 0, 24) } },
        });
        Resources.Add(new Style(typeof(Label))
        {
            Class = "code",
            Setters =
            {
                Resource(VisualElement.BackgroundColorProperty, Code),
                new Setter { Property = Label.FontSizeProperty, Value = 14d },
                new Setter { Property = Label.PaddingProperty, Value = new Thickness(6, 2) },
            },
        });
        ApplyColors();
        HandlerChanged += (_, _) =>
        {
            if (Application.Current is not { } app) return;
            app.RequestedThemeChanged -= OnThemeChanged;
            if (Handler is not null) app.RequestedThemeChanged += OnThemeChanged;
        };
    }

    private static Setter Resource(BindableProperty property, string key) => new() { Property = property, Value = ((IMarkupExtension)new DynamicResourceExtension { Key = key }).ProvideValue(null!) };

    // A style setter has no theme binding: the colors are dynamic resources, set again when the app theme changes.
    private void ApplyColors()
    {
        Resources[Text] = KlyTheme.Text.Resolve();
        Resources[Heading] = KlyTheme.TextHighlighted.Resolve();
        Resources[Link] = KlyTheme.Resolve(KlyColor.Primary).Text.Resolve();
        Resources[Muted] = KlyTheme.TextMuted.Resolve();
        Resources[Code] = KlyTheme.BackgroundElevated.Resolve();
    }

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => ApplyColors();
}
