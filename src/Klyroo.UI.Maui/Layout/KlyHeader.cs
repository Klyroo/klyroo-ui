namespace Klyroo.UI.Maui;

/// <summary>
/// The header of a site or an app: a title or a logo at the start, links in the middle, actions at the end, over a hairline.
/// Below 1024 wide the middle gives way to a menu button that opens <see cref="Body"/> in a panel under the bar. Put the header outside the scrolling area to keep it at the top of the screen.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyHeader : Grid
{
    /// <summary>Height of the bar, 4rem on the web.</summary>
    internal const double BarHeight = 64;

    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyHeader), null, propertyChanged: (b, _, v) => ((KlyHeader)b).ShowTitle((string?)v));
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyHeader), null, propertyChanged: (b, _, _) => ((KlyHeader)b).FillLeft());
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyHeader), null, propertyChanged: (b, _, v) => PlaceView(((KlyHeader)b)._center, (View?)v, b));
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyHeader), null, propertyChanged: (b, _, v) => PlaceView(((KlyHeader)b)._right, (View?)v, b));
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyHeader), null, propertyChanged: (b, _, v) => PlaceView(((KlyHeader)b)._menu, (View?)v, b));
    /// <summary>Identifies <see cref="Menu"/>.</summary>
    public static readonly BindableProperty MenuProperty = BindableProperty.Create(nameof(Menu), typeof(bool), typeof(KlyHeader), true, propertyChanged: (b, _, _) => ((KlyHeader)b).ApplyMode());
    /// <summary>Identifies <see cref="MenuOpen"/>.</summary>
    public static readonly BindableProperty MenuOpenProperty = BindableProperty.Create(nameof(MenuOpen), typeof(bool), typeof(KlyHeader), false, BindingMode.TwoWay, propertyChanged: (b, _, _) => ((KlyHeader)b).ApplyMode());
    /// <summary>Identifies <see cref="MenuLabel"/>.</summary>
    public static readonly BindableProperty MenuLabelProperty = BindableProperty.Create(nameof(MenuLabel), typeof(string), typeof(KlyHeader), "Open menu", propertyChanged: (b, _, v) => ((KlyHeader)b)._menuButton.Label = (string)v);

    private readonly KlyContainer _bar = new();
    private readonly Grid _row = new() { HeightRequest = BarHeight, ColumnSpacing = 16 };
    private readonly ContentView _left = LayoutParts.Slot();
    private readonly ContentView _center = LayoutParts.Slot();
    private readonly ContentView _right = LayoutParts.Slot();
    private readonly ContentView _menu = new() { IsVisible = false, Padding = new Thickness(16, 8) };
    private readonly LayoutIconButton _menuButton = new(KlyIconNames.Menu, "Open menu") { IsVisible = false, VerticalOptions = LayoutOptions.Center };
    private readonly Label _title = new() { FontSize = 18, FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, VerticalOptions = LayoutOptions.Center, VerticalTextAlignment = TextAlignment.Center, MinimumHeightRequest = 40 };
    private bool _compact = true;

    /// <summary>Creates a header.</summary>
    public KlyHeader()
    {
        RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
        this.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        _title.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        _title.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => TitleTapped?.Invoke(this, EventArgs.Empty)) });

        _left.HorizontalOptions = LayoutOptions.Start;
        _left.VerticalOptions = LayoutOptions.Center;
        _center.VerticalOptions = LayoutOptions.Center;
        _right.HorizontalOptions = LayoutOptions.End;
        _right.VerticalOptions = LayoutOptions.Center;
        _menuButton.Clicked += (_, _) => MenuOpen = !MenuOpen;

        _row.Add(_left);
        _row.Add(_center);
        _row.Add(_right);
        _row.Add(_menuButton);
        _bar.Add(_row);
        this.Add(_bar, 0, 0);
        this.Add(_menu, 0, 1);
        this.Add(LayoutParts.Line(), 0, 2);

        SizeChanged += (_, _) => UpdateWidth(Width);
        ApplyMode();
    }

    /// <summary>Raised when the user taps the <see cref="Title"/>, to go home.</summary>
    public event EventHandler? TitleTapped;

    /// <summary>The title at the start, used when <see cref="Left"/> is not set. Tapping it raises <see cref="TitleTapped"/>.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Replaces the title: a logo, or a logo with the title.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>The middle of the header, shown from 1024 wide up: links.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>The end of the header: actions, the color mode button. Always visible.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    /// <summary>The content of the menu panel that replaces <see cref="ChildContent"/> below 1024 wide.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Shows the menu button below 1024 wide, when there is a <see cref="Body"/>.</summary>
    public bool Menu { get => (bool)GetValue(MenuProperty); set => SetValue(MenuProperty, value); }

    /// <summary>Whether the menu panel is open. Two-way bindable; it closes by itself when the header grows past 1024.</summary>
    public bool MenuOpen { get => (bool)GetValue(MenuOpenProperty); set => SetValue(MenuOpenProperty, value); }

    /// <summary>Accessible name of the menu button, in the language of the app.</summary>
    public string MenuLabel { get => (string)GetValue(MenuLabelProperty); set => SetValue(MenuLabelProperty, value); }

    /// <summary>True below 1024 wide, where the middle is replaced by the menu. The header starts compact, as the mobile first styles of the web.</summary>
    public bool IsCompact => _compact;

    internal void UpdateWidth(double width)
    {
        var compact = !LayoutBreakpoints.IsWide(width);
        if (compact == _compact) return;
        _compact = compact;
        ApplyMode();
    }

    private static void PlaceView(ContentView slot, View? view, BindableObject owner)
    {
        LayoutParts.Fill(slot, view);
        ((KlyHeader)owner).ApplyMode();
    }

    private void ShowTitle(string? text)
    {
        _title.Text = text;
        FillLeft();
    }

    private void FillLeft() => LayoutParts.Fill(_left, Left ?? (string.IsNullOrEmpty(Title) ? null : _title));

    private void ApplyMode()
    {
        if (!_compact && MenuOpen)
        {
            MenuOpen = false;
            return;
        }

        var hasMenu = Menu && Body is not null;
        _center.IsVisible = !_compact && ChildContent is not null;
        _menuButton.IsVisible = _compact && hasMenu;
        _menu.IsVisible = _compact && hasMenu && MenuOpen;

        if (_compact)
        {
            _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto)];
            LayoutParts.Column(_left, 0);
            LayoutParts.Column(_center, 1);
            LayoutParts.Column(_right, 1);
            LayoutParts.Column(_menuButton, 2);
        }
        else
        {
            _row.ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)];
            LayoutParts.Column(_left, 0);
            LayoutParts.Column(_center, 1);
            LayoutParts.Column(_right, 2);
            LayoutParts.Column(_menuButton, 2);
        }
    }
}
