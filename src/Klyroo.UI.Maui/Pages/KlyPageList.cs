namespace Klyroo.UI.Maui;

/// <summary>A vertical list of items. With <see cref="Divide"/> a line separates the items; otherwise they are only spaced.</summary>
[ContentProperty(nameof(Items))]
public class KlyPageList : ContentView
{
    /// <summary>Identifies <see cref="Divide"/>.</summary>
    public static readonly BindableProperty DivideProperty = BindableProperty.Create(nameof(Divide), typeof(bool), typeof(KlyPageList), true, propertyChanged: (b, _, _) => ((KlyPageList)b).Rebuild());

    private readonly System.Collections.ObjectModel.ObservableCollection<View> _items = [];
    private readonly VerticalStackLayout _stack = new();

    /// <summary>Creates a list.</summary>
    public KlyPageList()
    {
        Content = _stack;
        _items.CollectionChanged += (_, _) => Rebuild();
        Rebuild();
    }

    /// <summary>The items.</summary>
    public IList<View> Items => _items;

    /// <summary>Draws a line between the items; otherwise they are only spaced.</summary>
    public bool Divide { get => (bool)GetValue(DivideProperty); set => SetValue(DivideProperty, value); }

    internal VerticalStackLayout Stack => _stack;

    private void Rebuild()
    {
        _stack.Children.Clear();
        _stack.Spacing = Divide ? 0 : 32;
        for (var i = 0; i < _items.Count; i++)
        {
            if (Divide && i > 0)
            {
                var line = PagesUi.Line(KlyTheme.Border);
                line.Margin = new Thickness(0, 16);
                _stack.Children.Add(line);
            }
            _stack.Children.Add(_items[i]);
        }
    }
}
