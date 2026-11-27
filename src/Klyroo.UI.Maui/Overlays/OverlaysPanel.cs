using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui;

/// <summary>Where a panel sits in the host: centered as a dialog, or docked to an edge.</summary>
internal enum OverlaysPanelMode
{
    Modal,
    Right,
    Left,
    Top,
    Bottom,
}

/// <summary>
/// The panel of <see cref="KlyModal"/>, <see cref="KlySlideover"/> and <see cref="KlyDrawer"/>, and of the dialogs of <see cref="KlyOverlayHost.ShowAsync{TResult}"/>:
/// an optional handle, a header (title, description, close button), a scrolling body and a footer on a raised surface.
/// </summary>
internal sealed class OverlaysPanel : Border
{
    private const double EdgeWidth = 448;
    private const double EdgeHeightRatio = 0.85;

    private readonly Grid _grid = new() { RowSpacing = 0 };
    private readonly Label _title = new() { FontSize = 16, FontAttributes = FontAttributes.Bold, IsVisible = false };
    private readonly Label _description = new() { FontSize = 14, IsVisible = false };
    private readonly OverlaysCloseButton _close = new("Close") { IsVisible = false };
    private readonly VerticalStackLayout _header = new() { Spacing = 0 };
    private readonly VerticalStackLayout _footer = new() { Spacing = 0 };
    private readonly ScrollView _body = new() { Padding = new Thickness(16) };
    private readonly ContentView _footerContent = new() { Padding = new Thickness(16) };
    private readonly BoxView _handle = new() { WidthRequest = 48, HeightRequest = 6, CornerRadius = 3, HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 12), IsVisible = false };
    private bool _loaded;
    private KlySize _size = KlySize.Md;

    public OverlaysPanel()
    {
        Padding = 0;
        _title.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        _description.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        _handle.SetThemeColor(BoxView.ColorProperty, KlyTheme.BorderAccented);
        SemanticProperties.SetHeadingLevel(_title, SemanticHeadingLevel.Level2);
        _close.Clicked += (_, _) => RequestClose();

        var texts = new VerticalStackLayout { Spacing = 4, Children = { _title, _description } };
        var headerRow = new Grid { Padding = new Thickness(16, 8, 8, 8), ColumnSpacing = 6, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
        headerRow.Add(new ContentView { Content = texts, VerticalOptions = LayoutOptions.Center, Padding = new Thickness(0, 8) }, 0);
        headerRow.Add(_close, 1);
        _header.Add(headerRow);
        _header.Add(Line());
        _footer.Add(Line());
        _footer.Add(_footerContent);

        _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _grid.Add(_handle, 0, 0);
        _grid.Add(_header, 0, 1);
        _grid.Add(_body, 0, 2);
        _grid.Add(_footer, 0, 3);
        Content = _grid;

        this.ApplySurface(KlyTheme.RadiusOf(2));
        Configure(OverlaysPanelMode.Modal, KlySize.Md, false);
        Refresh();
    }

    /// <summary>The control that owns the panel, to close it from inside (a command palette that closes on select). Null for a dialog opened from code.</summary>
    public KlyOverlayBase? Owner { get; set; }

    public OverlaysPanelMode Mode { get; private set; }

    public string? PanelTitle { get; set; }

    public string? PanelDescription { get; set; }

    public string? AccessibleName { get; set; }

    public bool ShowClose { get; set; }

    public bool ShowHandle { get; set; }

    public string CloseLabel
    {
        get => _close.Label;
        set => _close.Label = value;
    }

    public View? BodyView { get; set; }

    public View? FooterView { get; set; }

    internal Label TitleLabel => _title;

    internal Label DescriptionLabel => _description;

    internal OverlaysCloseButton CloseButton => _close;

    internal View HeaderView => _header;

    internal View FooterRegion => _footer;

    internal BoxView Handle => _handle;

    internal ScrollView BodyScroll => _body;

    /// <summary>Raised when the close button is pressed, or by <see cref="RequestClose"/>.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>The panel a view is in, if any.</summary>
    public static OverlaysPanel? Find(Element? from)
    {
        for (var element = from; element is not null; element = element.Parent)
        {
            if (element is OverlaysPanel panel) return panel;
        }
        return null;
    }

    public void RequestClose() => CloseRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Largest width of a dialog of <paramref name="size"/>: <c>max-w-xs</c> to <c>max-w-4xl</c> of the web theme.</summary>
    public static double MaxWidthOf(KlySize size) => size switch
    {
        KlySize.Xs => 320,
        KlySize.Sm => 384,
        KlySize.Md => 512,
        KlySize.Lg => 672,
        KlySize.Xl => 896,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>Chooses where the panel sits, its alignment and its corners.</summary>
    public void Configure(OverlaysPanelMode mode, KlySize size, bool inset)
    {
        Mode = mode;
        _size = size;
        var radius = KlyTheme.RadiusOf(2);
        var corners = new CornerRadius(radius);
        var margin = inset ? 8 : 0;
        switch (mode)
        {
            case OverlaysPanelMode.Modal:
                (HorizontalOptions, VerticalOptions, Margin) = (LayoutOptions.Center, LayoutOptions.Center, new Thickness(16));
                WidthRequest = MaxWidthOf(size);
                break;
            case OverlaysPanelMode.Right:
            case OverlaysPanelMode.Left:
                (HorizontalOptions, VerticalOptions, Margin) = (mode == OverlaysPanelMode.Right ? LayoutOptions.End : LayoutOptions.Start, LayoutOptions.Fill, new Thickness(margin));
                WidthRequest = EdgeWidth;
                corners = inset ? corners : new CornerRadius(0);
                break;
            case OverlaysPanelMode.Top:
                (HorizontalOptions, VerticalOptions, Margin) = (LayoutOptions.Fill, LayoutOptions.Start, new Thickness(margin));
                corners = inset ? corners : new CornerRadius(0, 0, radius, radius);
                break;
            case OverlaysPanelMode.Bottom:
                (HorizontalOptions, VerticalOptions, Margin) = (LayoutOptions.Fill, LayoutOptions.End, new Thickness(margin));
                corners = inset ? corners : new CornerRadius(radius, radius, 0, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
        StrokeShape = new RoundRectangle { CornerRadius = corners };
        Refresh();
    }

    /// <summary>Fits the panel to the host: no wider than the screen, and sheets docked to the top or bottom no taller than 85% of it.</summary>
    public void Fit(Size host)
    {
        switch (Mode)
        {
            case OverlaysPanelMode.Modal:
                WidthRequest = Math.Min(MaxWidthOf(_size), Math.Max(0, host.Width - Margin.HorizontalThickness));
                break;
            case OverlaysPanelMode.Right:
            case OverlaysPanelMode.Left:
                WidthRequest = Math.Min(EdgeWidth, Math.Max(0, host.Width - Margin.HorizontalThickness));
                break;
            default:
                MaximumHeightRequest = Math.Max(0, host.Height * EdgeHeightRatio - Margin.VerticalThickness);
                break;
        }
    }

    /// <summary>Applies the texts, the body and the footer to the views of the panel. Call it after changing them.</summary>
    public void Refresh()
    {
        ShowText(_title, PanelTitle);
        ShowText(_description, PanelDescription);
        _close.IsVisible = ShowClose;
        _header.IsVisible = PanelTitle is not null || PanelDescription is not null || ShowClose;

        if (BodyView is not null && !ReferenceEquals(_body.Content, BodyView))
        {
            OverlaysSupport.Detach(BodyView);
            _body.Content = BodyView;
        }
        else if (BodyView is null)
        {
            _body.Content = null;
        }
        _body.IsVisible = BodyView is not null;

        if (FooterView is not null && !ReferenceEquals(_footerContent.Content, FooterView))
        {
            OverlaysSupport.Detach(FooterView);
            _footerContent.Content = FooterView;
        }
        else if (FooterView is null)
        {
            _footerContent.Content = null;
        }
        _footer.IsVisible = FooterView is not null;

        // The handle of a drawer sits at the free end of the sheet: the top for a bottom sheet, the bottom for a top sheet.
        var handleVisible = ShowHandle && Mode is OverlaysPanelMode.Top or OverlaysPanelMode.Bottom;
        _handle.IsVisible = handleVisible;
        Grid.SetRow(_handle, Mode == OverlaysPanelMode.Top ? 4 : 0);

        SemanticProperties.SetDescription(this, PanelTitle ?? AccessibleName);
        SemanticProperties.SetHint(this, PanelDescription);
    }

    /// <summary>Shows the panel in the host as a modal layer docked or centered according to its mode, and keeps it fitted to the host.</summary>
    public KlyOverlayLayer Present(KlyOverlayHost host, bool dismissible)
    {
        var transition = Mode switch
        {
            OverlaysPanelMode.Right => KlyOverlayTransition.SlideFromRight,
            OverlaysPanelMode.Left => KlyOverlayTransition.SlideFromLeft,
            OverlaysPanelMode.Top => KlyOverlayTransition.SlideFromTop,
            OverlaysPanelMode.Bottom => KlyOverlayTransition.SlideFromBottom,
            _ => KlyOverlayTransition.Fade,
        };
        var layer = host.Show(this, new KlyOverlayLayerOptions { Modal = true, Dim = true, Dismissible = dismissible, Transition = transition });
        layer.HostSizeChanged += (_, size) => Fit(size);
        if (host.Width > 0) Fit(new Size(host.Width, host.Height));
        if (!_loaded)
        {
            _loaded = true;
            // MAUI has no call to move the screen reader focus: the dialog is announced when it opens instead, and the page below is hidden from the reader.
            Loaded += (_, _) => OverlaysSupport.Announce(string.Join(". ", new[] { PanelTitle ?? AccessibleName, PanelDescription }.Where(s => !string.IsNullOrEmpty(s))));
        }
        return layer;
    }

    private static void ShowText(Label label, string? text)
    {
        label.Text = text;
        label.IsVisible = !string.IsNullOrEmpty(text);
    }

    private static BoxView Line()
    {
        var line = new BoxView { HeightRequest = 1 };
        line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
        return line;
    }
}
