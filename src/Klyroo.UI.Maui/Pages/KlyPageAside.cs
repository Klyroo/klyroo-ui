namespace Klyroo.UI.Maui;

/// <summary>
/// A side column of a page, shown from 1024 units wide up, as in the web layout: put it in the <see cref="KlyPage.Left"/> or <see cref="KlyPage.Right"/> of a <see cref="KlyPage"/>
/// and it hides itself while that page is narrower. It does not stay in place while the page scrolls: a native page scrolls as a whole.
/// </summary>
[ContentProperty(nameof(Body))]
public class KlyPageAside : ContentView
{
    /// <summary>Identifies <see cref="Top"/>.</summary>
    public static readonly BindableProperty TopProperty = BindableProperty.Create(nameof(Top), typeof(View), typeof(KlyPageAside), null, propertyChanged: (b, _, v) => ((KlyPageAside)b).ShowSlot(((KlyPageAside)b)._top, (View?)v));
    /// <summary>Identifies <see cref="Body"/>.</summary>
    public static readonly BindableProperty BodyProperty = BindableProperty.Create(nameof(Body), typeof(View), typeof(KlyPageAside), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyPageAside)b)._body, (View?)v));
    /// <summary>Identifies <see cref="Bottom"/>.</summary>
    public static readonly BindableProperty BottomProperty = BindableProperty.Create(nameof(Bottom), typeof(View), typeof(KlyPageAside), null, propertyChanged: (b, _, v) => ((KlyPageAside)b).ShowSlot(((KlyPageAside)b)._bottom, (View?)v));
    /// <summary>Identifies <see cref="AlwaysVisible"/>.</summary>
    public static readonly BindableProperty AlwaysVisibleProperty = BindableProperty.Create(nameof(AlwaysVisible), typeof(bool), typeof(KlyPageAside), false, propertyChanged: (b, _, _) => ((KlyPageAside)b).Relayout(((KlyPageAside)b)._parentWidth));

    private readonly ContentView _top = PagesUi.Slot();
    private readonly ContentView _body = PagesUi.Slot();
    private readonly ContentView _bottom = PagesUi.Slot();
    private readonly BoxView _topLine = PagesUi.Line(KlyTheme.Border);
    private readonly BoxView _bottomLine = PagesUi.Line(KlyTheme.Border);
    private VisualElement? _parent;
    private double _parentWidth = double.PositiveInfinity;

    /// <summary>Creates a page aside.</summary>
    public KlyPageAside()
    {
        Padding = new Thickness(0, 32);
        _topLine.IsVisible = false;
        _bottomLine.IsVisible = false;
        _topLine.Margin = new Thickness(0, 16, 0, 16);
        _bottomLine.Margin = new Thickness(0, 16, 0, 16);
        Content = new VerticalStackLayout { Children = { _top, _topLine, _body, _bottomLine, _bottom } };
        ParentChanged += (_, _) => WatchParent();
    }

    /// <summary>Content at the top of the column, set apart by a line.</summary>
    public View? Top { get => (View?)GetValue(TopProperty); set => SetValue(TopProperty, value); }

    /// <summary>Content of the page aside.</summary>
    public View? Body { get => (View?)GetValue(BodyProperty); set => SetValue(BodyProperty, value); }

    /// <summary>Content at the bottom of the column, set apart by a line.</summary>
    public View? Bottom { get => (View?)GetValue(BottomProperty); set => SetValue(BottomProperty, value); }

    /// <summary>Shows the aside at any width, instead of from 1024 units up.</summary>
    public bool AlwaysVisible { get => (bool)GetValue(AlwaysVisibleProperty); set => SetValue(AlwaysVisibleProperty, value); }

    /// <summary>Shows or hides the aside for the width of the page it sits in.</summary>
    internal void Relayout(double pageWidth)
    {
        _parentWidth = pageWidth;
        IsVisible = AlwaysVisible || pageWidth >= PagesUi.Lg;
    }

    private void ShowSlot(ContentView slot, View? view)
    {
        PagesUi.Put(slot, view);
        (slot == _top ? _topLine : _bottomLine).IsVisible = view is not null;
    }

    private void WatchParent()
    {
        if (_parent is not null) _parent.SizeChanged -= OnParentSizeChanged;
        _parent = Parent as VisualElement;
        if (_parent is null) return;
        _parent.SizeChanged += OnParentSizeChanged;
        if (_parent.Width > 0) Relayout(_parent.Width);
    }

    private void OnParentSizeChanged(object? sender, EventArgs e)
    {
        if (_parent is { Width: > 0 }) Relayout(_parent.Width);
    }
}
