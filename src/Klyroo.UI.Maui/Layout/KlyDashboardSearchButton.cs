using TextLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// The button that opens the <see cref="KlyDashboardSearch"/> of its dashboard and puts the cursor in it. In a collapsed sidebar only the icon is left; the label stays for a screen reader.
/// </summary>
public class KlyDashboardSearchButton : Border, ILayoutSidebarPart
{
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyDashboardSearchButton), "Search", propertyChanged: (b, _, v) => ((KlyDashboardSearchButton)b).ShowLabel((string)v));

    private readonly KlyIcon _icon = new() { Name = KlyIconNames.Search, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
    private readonly TextLabel _text = new() { Text = "Search", FontSize = 14, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, InputTransparent = true };
    private readonly Button _button = new()
    {
        BackgroundColor = Colors.Transparent,
        BorderWidth = 0,
        Padding = 0,
        MinimumWidthRequest = 0,
        MinimumHeightRequest = 0,
        HorizontalOptions = LayoutOptions.Fill,
        VerticalOptions = LayoutOptions.Fill,
    };

    private readonly Grid _row = new() { ColumnSpacing = 6, ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)] };
    private bool _iconOnly;

    /// <summary>Creates a search button.</summary>
    public KlyDashboardSearchButton()
    {
        this.Apply(new Surface(KlyTheme.Background, KlyTheme.BorderAccented, 1, KlyTheme.TextMuted));
        this.SetRadius(KlyTheme.RadiusOf(1));
        MinimumHeightRequest = 40;
        _text.SetThemeColor(TextLabel.TextColorProperty, KlyTheme.TextMuted);
        _button.SetThemeColor(Button.TextColorProperty, KlyTheme.TextMuted);
        _button.Clicked += (_, _) => Activate();
        _row.Add(_icon, 0);
        _row.Add(_text, 1);
        Content = new Grid { Children = { _row, _button } };
        ShowLabel(Label);
        ApplyMode();
    }

    /// <summary>Raised after the button opened the search.</summary>
    public event EventHandler? Clicked;

    /// <summary>The text of the button, in the language of the app. Defaults to "Search".</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>True while the button shows only its icon, in a collapsed sidebar.</summary>
    public bool IsIconOnly => _iconOnly;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    /// <summary>Opens the search of the dashboard, as a tap does.</summary>
    internal void Activate()
    {
        var group = KlyDashboardGroup.Of(this);
        if (group is not null && LayoutTree.Descendants<KlyDashboardSearch>(group).FirstOrDefault() is { } search) search.FocusInput();
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    void ILayoutSidebarPart.SidebarChanged(KlySidebar sidebar)
    {
        var iconOnly = sidebar.Collapsed && !sidebar.IsPanel;
        if (iconOnly == _iconOnly) return;
        _iconOnly = iconOnly;
        ApplyMode();
    }

    private void ShowLabel(string text)
    {
        _text.Text = text;
        SemanticProperties.SetDescription(_button, text);
    }

    private void ApplyMode()
    {
        _text.IsVisible = !_iconOnly;
        Padding = _iconOnly ? new Thickness(0, 6) : new Thickness(10, 6);
        _icon.HorizontalOptions = _iconOnly ? LayoutOptions.Center : LayoutOptions.Start;
        LayoutParts.Column(_icon, 0);
        LayoutParts.ColumnSpan(_icon, _iconOnly ? 2 : 1);
    }
}
