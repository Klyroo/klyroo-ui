using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui;

/// <summary>
/// Joins fields into one control: <see cref="KlyInput"/>, <see cref="KlyTextarea"/> and <see cref="KlySelect"/> sit side by side (or stacked) and keep the corner radius on the outer corners only.
/// A <see cref="KlyButton"/> or a native <see cref="Button"/> sits in the row too, but keeps all four of its round corners: the native button has one radius for every corner.
/// </summary>
[ContentProperty(nameof(Items))]
public class KlyFieldGroup : ContentView
{
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(KlyOrientation), typeof(KlyFieldGroup), KlyOrientation.Horizontal, propertyChanged: (b, _, _) => ((KlyFieldGroup)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyFieldGroup), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyFieldGroup)b, (string?)v));

    private readonly ObservableCollection<View> _items = [];
    private readonly Grid _grid = new() { ColumnSpacing = -1, RowSpacing = -1 };

    /// <summary>Creates an empty group: add the fields to <see cref="Items"/>.</summary>
    public KlyFieldGroup()
    {
        HorizontalOptions = LayoutOptions.Fill;
        _items.CollectionChanged += OnItemsChanged;
        Content = _grid;
    }

    /// <summary>The fields to attach to each other.</summary>
    public IList<View> Items => _items;

    /// <summary>Whether the fields sit side by side (<c>Horizontal</c>) or stacked (<c>Vertical</c>).</summary>
    public KlyOrientation Orientation { get => (KlyOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>What a screen reader says for the group, when it needs a name.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void Rebuild()
    {
        _grid.Children.Clear();
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        var horizontal = Orientation == KlyOrientation.Horizontal;
        var radius = KlyTheme.RadiusOf(1);
        for (var i = 0; i < _items.Count; i++)
        {
            var view = _items[i];
            var first = i == 0;
            var last = i == _items.Count - 1;
            if (horizontal)
            {
                // A field that fills takes the room that is left, a button keeps its own width.
                _grid.ColumnDefinitions.Add(new ColumnDefinition(view.HorizontalOptions.Alignment == LayoutAlignment.Fill ? GridLength.Star : GridLength.Auto));
                _grid.Add(view, i, 0);
            }
            else
            {
                _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                _grid.Add(view, 0, i);
            }

            if (view is Border border)
            {
                // The same corners as the web group: the inner ones are square.
                var near = first ? radius : 0;
                var far = last ? radius : 0;
                border.StrokeShape = horizontal
                    ? new RoundRectangle { CornerRadius = new CornerRadius(near, far, near, far) }
                    : new RoundRectangle { CornerRadius = new CornerRadius(near, near, far, far) };
            }
        }
    }
}
