namespace Klyroo.UI.Maui;

/// <summary>
/// The main area of a page: it takes the space the header and the footer leave and, by default, scrolls its content.
/// Put it in the row of a <see cref="Grid"/> that has a <c>*</c> height, between a <see cref="KlyHeader"/> and a <see cref="KlyFooter"/>.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyMain : ContentView
{
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyMain), null, propertyChanged: (b, _, _) => ((KlyMain)b).Arrange());
    /// <summary>Identifies <see cref="Scrollable"/>.</summary>
    public static readonly BindableProperty ScrollableProperty = BindableProperty.Create(nameof(Scrollable), typeof(bool), typeof(KlyMain), true, propertyChanged: (b, _, _) => ((KlyMain)b).Arrange());

    private readonly ScrollView _scroll = new() { Orientation = ScrollOrientation.Vertical };

    /// <summary>Creates a main area.</summary>
    public KlyMain()
    {
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;
        Arrange();
    }

    /// <summary>The content of the page.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>
    /// Wraps the content in a vertical <see cref="ScrollView"/> (default), so a long page scrolls the way it does on the web.
    /// Turn it off when the content scrolls by itself (a <see cref="CollectionView"/>) or the page already has a scroll view around it.
    /// </summary>
    public bool Scrollable { get => (bool)GetValue(ScrollableProperty); set => SetValue(ScrollableProperty, value); }

    private void Arrange()
    {
        var child = ChildContent;
        // A view can only have one parent: detach it before it moves between the scroll view and this control.
        _scroll.Content = null;
        Content = null;
        if (Scrollable)
        {
            _scroll.Content = child;
            Content = _scroll;
        }
        else
        {
            Content = child;
        }
    }
}
