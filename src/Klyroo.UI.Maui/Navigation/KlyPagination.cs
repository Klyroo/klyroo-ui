using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>Page buttons for a list of items: the current page, its neighbors, the first and the last, with previous and next buttons. The buttons wrap onto several lines when they do not fit.</summary>
public class KlyPagination : FlexLayout
{
    /// <summary>Identifies <see cref="Total"/>.</summary>
    public static readonly BindableProperty TotalProperty = BindableProperty.Create(nameof(Total), typeof(int), typeof(KlyPagination), 0, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="PageSize"/>.</summary>
    public static readonly BindableProperty PageSizeProperty = BindableProperty.Create(nameof(PageSize), typeof(int), typeof(KlyPagination), 10, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Page"/>.</summary>
    public static readonly BindableProperty PageProperty = BindableProperty.Create(nameof(Page), typeof(int), typeof(KlyPagination), 1, BindingMode.TwoWay, propertyChanged: (b, o, n) => ((KlyPagination)b).OnPageChanged((int)n));
    /// <summary>Identifies <see cref="SiblingCount"/>.</summary>
    public static readonly BindableProperty SiblingCountProperty = BindableProperty.Create(nameof(SiblingCount), typeof(int), typeof(KlyPagination), 1, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowControls"/>.</summary>
    public static readonly BindableProperty ShowControlsProperty = BindableProperty.Create(nameof(ShowControls), typeof(bool), typeof(KlyPagination), true, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowFirstLast"/>.</summary>
    public static readonly BindableProperty ShowFirstLastProperty = BindableProperty.Create(nameof(ShowFirstLast), typeof(bool), typeof(KlyPagination), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPagination), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyPagination), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyPagination), "Pagination", propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyPagination)b, (string?)v));
    /// <summary>Identifies <see cref="PageLabel"/>.</summary>
    public static readonly BindableProperty PageLabelProperty = BindableProperty.Create(nameof(PageLabel), typeof(string), typeof(KlyPagination), "Page", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="FirstPageLabel"/>.</summary>
    public static readonly BindableProperty FirstPageLabelProperty = BindableProperty.Create(nameof(FirstPageLabel), typeof(string), typeof(KlyPagination), "First page", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="PreviousPageLabel"/>.</summary>
    public static readonly BindableProperty PreviousPageLabelProperty = BindableProperty.Create(nameof(PreviousPageLabel), typeof(string), typeof(KlyPagination), "Previous page", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="NextPageLabel"/>.</summary>
    public static readonly BindableProperty NextPageLabelProperty = BindableProperty.Create(nameof(NextPageLabel), typeof(string), typeof(KlyPagination), "Next page", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="LastPageLabel"/>.</summary>
    public static readonly BindableProperty LastPageLabelProperty = BindableProperty.Create(nameof(LastPageLabel), typeof(string), typeof(KlyPagination), "Last page", propertyChanged: Rebuild);

    private readonly List<NavigationButton> _buttons = [];

    /// <summary>Creates a pagination.</summary>
    public KlyPagination()
    {
        Direction = FlexDirection.Row;
        Wrap = FlexWrap.Wrap;
        AlignItems = FlexAlignItems.Center;
        SemanticProperties.SetDescription(this, AccessibleName);
        Rebuild();
    }

    /// <summary>Raised with the page number when the current page changes, by the user or by code.</summary>
    public event EventHandler<int>? PageChanged;

    /// <summary>Total number of items. The page count is <c>ceil(Total / PageSize)</c>, at least 1.</summary>
    public int Total { get => (int)GetValue(TotalProperty); set => SetValue(TotalProperty, value); }

    /// <summary>Number of items on a page. Defaults to 10.</summary>
    public int PageSize { get => (int)GetValue(PageSizeProperty); set => SetValue(PageSizeProperty, value); }

    /// <summary>Current page, starting at 1. Two-way bindable.</summary>
    public int Page { get => (int)GetValue(PageProperty); set => SetValue(PageProperty, value); }

    /// <summary>Pages shown on each side of the current one.</summary>
    public int SiblingCount { get => (int)GetValue(SiblingCountProperty); set => SetValue(SiblingCountProperty, value); }

    /// <summary>Previous and next buttons.</summary>
    public bool ShowControls { get => (bool)GetValue(ShowControlsProperty); set => SetValue(ShowControlsProperty, value); }

    /// <summary>Buttons that jump to the first and the last page.</summary>
    public bool ShowFirstLast { get => (bool)GetValue(ShowFirstLastProperty); set => SetValue(ShowFirstLastProperty, value); }

    /// <summary>Semantic color of the current page.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the buttons, from <c>Xs</c> to <c>Xl</c>. Each button has a tap target of at least 40 whatever its size.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the pagination. Defaults to "Pagination".</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Prefix of what a screen reader says for a page button ("Page 3"). Defaults to "Page".</summary>
    public string PageLabel { get => (string)GetValue(PageLabelProperty); set => SetValue(PageLabelProperty, value); }

    /// <summary>What a screen reader says for the first page button.</summary>
    public string FirstPageLabel { get => (string)GetValue(FirstPageLabelProperty); set => SetValue(FirstPageLabelProperty, value); }

    /// <summary>What a screen reader says for the previous page button.</summary>
    public string PreviousPageLabel { get => (string)GetValue(PreviousPageLabelProperty); set => SetValue(PreviousPageLabelProperty, value); }

    /// <summary>What a screen reader says for the next page button.</summary>
    public string NextPageLabel { get => (string)GetValue(NextPageLabelProperty); set => SetValue(NextPageLabelProperty, value); }

    /// <summary>What a screen reader says for the last page button.</summary>
    public string LastPageLabel { get => (string)GetValue(LastPageLabelProperty); set => SetValue(LastPageLabelProperty, value); }

    /// <summary>The number of pages.</summary>
    internal int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)Math.Max(1, PageSize)));

    /// <summary>The page shown as current: <see cref="Page"/> kept between 1 and the page count.</summary>
    internal int EffectivePage => Math.Clamp(Page, 1, PageCount);

    internal IReadOnlyList<NavigationButton> Buttons => _buttons;

    /// <summary>Page numbers to show, 0 standing for an ellipsis. The width stays constant while paging.</summary>
    internal static List<int> Entries(int page, int count, int sibling)
    {
        var slots = 2 * sibling + 5;
        var result = new List<int>();
        if (count <= slots)
        {
            for (var p = 1; p <= count; p++) result.Add(p);
            return result;
        }
        var left = Math.Max(page - sibling, 1);
        var right = Math.Min(page + sibling, count);
        var showLeft = left > 3;
        var showRight = right < count - 2;
        if (!showLeft && showRight)
        {
            for (var p = 1; p <= 3 + 2 * sibling; p++) result.Add(p);
            result.Add(0);
            result.Add(count);
        }
        else if (showLeft && !showRight)
        {
            result.Add(1);
            result.Add(0);
            for (var p = count - (2 + 2 * sibling); p <= count; p++) result.Add(p);
        }
        else
        {
            result.Add(1);
            result.Add(0);
            for (var p = left; p <= right; p++) result.Add(p);
            result.Add(0);
            result.Add(count);
        }
        return result;
    }

    /// <summary>The height, the smallest width, the horizontal padding and the font size of a button, as the web theme gives them.</summary>
    internal static (double Height, double MinWidth, double PadX, double Font) MetricsOf(KlySize size) => size switch
    {
        KlySize.Xs => (24, 24, 4, 12),
        KlySize.Sm => (28, 28, 6, 12),
        KlySize.Md => (32, 32, 8, 14),
        KlySize.Lg => (36, 36, 8, 14),
        KlySize.Xl => (40, 40, 10, 16),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyPagination)bindable).Rebuild();

    private void OnPageChanged(int page)
    {
        Rebuild();
        PageChanged?.Invoke(this, page);
    }

    private void Rebuild()
    {
        Children.Clear();
        _buttons.Clear();
        var page = EffectivePage;
        var count = PageCount;
        var set = KlyTheme.Resolve(Color);
        var metrics = MetricsOf(Size);

        if (ShowFirstLast) AddControl(KlyIconNames.ChevronDoubleLeft, FirstPageLabel, 1, page <= 1, metrics);
        if (ShowControls) AddControl(KlyIconNames.ChevronLeft, PreviousPageLabel, page - 1, page <= 1, metrics);
        foreach (var entry in Entries(page, count, Math.Max(0, SiblingCount)))
        {
            if (entry == 0) AddEllipsis(metrics);
            else AddPage(entry, entry == page, set, metrics);
        }
        if (ShowControls) AddControl(KlyIconNames.ChevronRight, NextPageLabel, page + 1, page >= count, metrics);
        if (ShowFirstLast) AddControl(KlyIconNames.ChevronDoubleRight, LastPageLabel, count, page >= count, metrics);
    }

    private NavigationButton NewButton(int target, string description, (double Height, double MinWidth, double PadX, double Font) metrics)
    {
        var button = new NavigationButton { Margin = new Thickness(2) };
        button.Face.HeightRequest = metrics.Height;
        button.Face.MinimumWidthRequest = metrics.MinWidth;
        button.Face.Padding = new Thickness(metrics.PadX, 0);
        button.Face.SetRadius(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(button, description);
        button.Clicked += (_, _) => Select(target);
        _buttons.Add(button);
        Children.Add(button);
        return button;
    }

    private void AddPage(int number, bool current, KlyColorSet set, (double Height, double MinWidth, double PadX, double Font) metrics)
    {
        var button = NewButton(number, $"{PageLabel} {number}", metrics);
        var text = current ? set.Contrast : KlyTheme.Text;
        var label = NavigationLayout.TextLabel(metrics.Font, text, lineBreak: LineBreakMode.NoWrap);
        label.Text = number.ToString(System.Globalization.CultureInfo.CurrentCulture);
        label.HorizontalTextAlignment = TextAlignment.Center;
        button.Face.Content = label;
        // The current page is filled, with an inset ring in the text color of the semantic color.
        button.Face.Apply(current
            ? new Surface(set.Fill, set.Text, 1, text)
            : new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, text));
    }

    private void AddControl(string icon, string description, int target, bool disabled, (double Height, double MinWidth, double PadX, double Font) metrics)
    {
        var button = NewButton(target, description, metrics);
        button.IsEnabled = !disabled;
        button.Face.Content = NavigationLayout.Icon(icon, KlySize.Sm, KlyTheme.TextMuted);
        button.Face.Apply(new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.TextMuted));
    }

    private void AddEllipsis((double Height, double MinWidth, double PadX, double Font) metrics)
    {
        var label = NavigationLayout.TextLabel(metrics.Font, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
        label.Text = "…";
        label.HorizontalTextAlignment = TextAlignment.Center;
        label.HeightRequest = metrics.Height;
        label.MinimumWidthRequest = metrics.MinWidth;
        label.Margin = new Thickness(2);
        AutomationProperties.SetIsInAccessibleTree(label, false);
        Children.Add(label);
    }

    private void Select(int target)
    {
        target = Math.Clamp(target, 1, PageCount);
        if (target == EffectivePage) return;
        Page = target;
    }
}
