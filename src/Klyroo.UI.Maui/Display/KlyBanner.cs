using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// A dismissible announcement bar at the top of a page. With an <see cref="Id"/>, a dismissal is remembered on the device (in the <see cref="Preferences"/> of the app)
/// and the banner stays hidden on later runs; without one it only lasts until the banner is created again.
/// </summary>
public class KlyBanner : Border
{
    private static readonly HashSet<string> Dismissed = [];

    /// <summary>Identifies <see cref="Id"/>.</summary>
    public static readonly BindableProperty IdProperty = BindableProperty.Create(nameof(Id), typeof(string), typeof(KlyBanner), null, propertyChanged: (b, _, _) => ((KlyBanner)b).RestoreDismissal());
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyBanner), null, propertyChanged: (b, _, v) => DisplayParts.Show(((KlyBanner)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyBanner), null, propertyChanged: (b, _, v) => DisplayParts.Show(((KlyBanner)b)._description, (string?)v));
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyBanner), null, propertyChanged: (b, _, _) => ((KlyBanner)b).Refresh());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyBanner), null, propertyChanged: (b, _, _) => ((KlyBanner)b).Refresh());
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyBanner), null, propertyChanged: (b, _, _) => ((KlyBanner)b).Refresh());
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyBanner), null, propertyChanged: (b, _, v) => ((KlyBanner)b)._title.TextDecorations = v is null ? TextDecorations.None : TextDecorations.Underline);
    /// <summary>Identifies <see cref="Target"/>.</summary>
    public static readonly BindableProperty TargetProperty = BindableProperty.Create(nameof(Target), typeof(KlyLinkTarget?), typeof(KlyBanner), null);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyBanner), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyBanner)b).ApplyStyle());
    /// <summary>Identifies <see cref="Closable"/>.</summary>
    public static readonly BindableProperty ClosableProperty = BindableProperty.Create(nameof(Closable), typeof(bool), typeof(KlyBanner), false, propertyChanged: (b, _, v) => ((KlyBanner)b)._close.IsVisible = (bool)v);
    /// <summary>Identifies <see cref="CloseLabel"/>.</summary>
    public static readonly BindableProperty CloseLabelProperty = BindableProperty.Create(nameof(CloseLabel), typeof(string), typeof(KlyBanner), "Close", propertyChanged: (b, _, v) => ((KlyBanner)b)._close.SetDescription((string)v));

    private readonly Label _title = DisplayParts.NewLabel(14, KlyTheme.Text, FontAttributes.Bold);
    private readonly Label _description = DisplayParts.NewLabel(14, KlyTheme.Text);
    private readonly FlexLayout _content = new() { Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Center, AlignItems = FlexAlignItems.Center };
    private readonly Grid _row = new() { ColumnSpacing = 12 };
    private readonly DisplayIconButton _close = new(KlyIconNames.Close, "Close") { IsVisible = false };
    private readonly Border _iconHost = new() { StrokeThickness = 0, Padding = 0, VerticalOptions = LayoutOptions.Center, IsVisible = false };

    /// <summary>Creates a banner.</summary>
    public KlyBanner()
    {
        HorizontalOptions = LayoutOptions.Fill;
        Padding = new Thickness(16, 6);
        StrokeShape = new Microsoft.Maui.Controls.Shapes.Rectangle();
        _title.HorizontalTextAlignment = TextAlignment.Center;
        _description.HorizontalTextAlignment = TextAlignment.Center;
        _title.Margin = new Thickness(0, 0, 12, 0);
        _description.Margin = new Thickness(0, 0, 12, 0);
        _title.IsVisible = false;
        _description.IsVisible = false;
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        _row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _row.Add(_iconHost, 0);
        _row.Add(_content, 1);
        _row.Add(_close, 2);
        Content = _row;
        _close.Clicked += (_, _) => Close();
        DisplayParts.Tap(_title, OpenHref);
        DisplayParts.Tap(_description, OpenHref);
        ApplyStyle();
    }

    /// <summary>Raised when the user closes the banner with the close button. The banner hides itself first.</summary>
    public event EventHandler? Closed;

    /// <summary>Identifies the banner on the device: a dismissed banner stays hidden on later runs. Without an Id a dismissal only lasts until the banner is created again.</summary>
    public new string? Id { get => (string?)GetValue(IdProperty); set => SetValue(IdProperty, value); }

    /// <summary>Title of the banner.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Text of the banner.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Rich description, any view, instead of <see cref="Description"/>.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>The icon of the banner, such as a <see cref="KlyIcon"/>. Give it <see cref="KlyIcon.IconColor"/> of the contrast color of the banner so that it reads on the fill.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Buttons shown at the end of the text.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Makes the banner open this address when its text is tapped.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Where <see cref="Href"/> opens: <see cref="KlyLinkTarget.Blank"/> in the external browser, anything else in the in-app browser when the platform has one.</summary>
    public KlyLinkTarget? Target { get => (KlyLinkTarget?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>Semantic color of the banner.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Shows a close button. Pressing it hides the banner, remembers that when there is an <see cref="Id"/>, and raises <see cref="Closed"/>.</summary>
    public bool Closable { get => (bool)GetValue(ClosableProperty); set => SetValue(ClosableProperty, value); }

    /// <summary>Accessible name of the close button, in the language of the app.</summary>
    public string CloseLabel { get => (string)GetValue(CloseLabelProperty); set => SetValue(CloseLabelProperty, value); }

    /// <summary>Remembers dismissals: reads and writes the <see cref="Preferences"/> of the app, or memory when there are none (unit tests).</summary>
    internal static bool IsDismissed(string id)
    {
        try
        {
            return Preferences.Default.Get(Key(id), false);
        }
        catch (Exception)
        {
            return Dismissed.Contains(id);
        }
    }

    internal static void Remember(string id)
    {
        Dismissed.Add(id);
        try
        {
            Preferences.Default.Set(Key(id), true);
        }
        catch (Exception)
        {
            // No preference store on this platform: the memory above keeps it until the process ends.
        }
    }

    private static string Key(string id) => "klyroo.banner." + id;

    internal void Close()
    {
        IsVisible = false;
        if (Id is { } id) Remember(id);
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreDismissal()
    {
        if (Id is { } id && IsDismissed(id)) IsVisible = false;
    }

    private void OpenHref() => DisplayLaunch.Open(Href, Target);

    private void Refresh()
    {
        DisplayParts.Detach(Icon);
        DisplayParts.Detach(Body);
        DisplayParts.Detach(Actions);
        _iconHost.Content = Icon;
        _iconHost.IsVisible = Icon is not null;

        _content.Children.Clear();
        _content.Children.Add(_title);
        // A rich body wins over the plain description, as the child content does on the web.
        _content.Children.Add(Body ?? _description);
        if (Actions is not null) _content.Children.Add(Actions);
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        this.Apply(new Surface(set.Fill, set.Text, 1, set.Contrast));
        _title.SetThemeColor(Label.TextColorProperty, set.Contrast);
        _description.SetThemeColor(Label.TextColorProperty, set.Contrast);
        _close.Icon.IconColor = set.Contrast;
        Refresh();
    }
}
