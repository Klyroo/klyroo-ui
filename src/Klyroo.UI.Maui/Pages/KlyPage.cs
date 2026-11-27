namespace Klyroo.UI.Maui;

/// <summary>
/// A page layout of up to three columns. <see cref="Left"/> and <see cref="Right"/> are optional; <see cref="Center"/> takes the width they leave.
/// Below 1024 units the columns stack in reading order: left, center, right. From 1024 they sit on a ten-column grid: 2 for a side column, 6, 8 or 10 for the center.
/// </summary>
[ContentProperty(nameof(Center))]
public class KlyPage : Grid
{
    /// <summary>Identifies <see cref="Left"/>.</summary>
    public static readonly BindableProperty LeftProperty = BindableProperty.Create(nameof(Left), typeof(View), typeof(KlyPage), null, propertyChanged: (b, _, _) => ((KlyPage)b).Rebuild());
    /// <summary>Identifies <see cref="Center"/>.</summary>
    public static readonly BindableProperty CenterProperty = BindableProperty.Create(nameof(Center), typeof(View), typeof(KlyPage), null, propertyChanged: (b, _, _) => ((KlyPage)b).Rebuild());
    /// <summary>Identifies <see cref="Right"/>.</summary>
    public static readonly BindableProperty RightProperty = BindableProperty.Create(nameof(Right), typeof(View), typeof(KlyPage), null, propertyChanged: (b, _, _) => ((KlyPage)b).Rebuild());

    private double _width;

    /// <summary>Creates a page layout.</summary>
    public KlyPage()
    {
        SizeChanged += (_, _) => Relayout(Width);
        Relayout(0);
    }

    /// <summary>The left column, typically a <see cref="KlyPageAside"/> holding navigation.</summary>
    public View? Left { get => (View?)GetValue(LeftProperty); set => SetValue(LeftProperty, value); }

    /// <summary>The main column.</summary>
    public View? Center { get => (View?)GetValue(CenterProperty); set => SetValue(CenterProperty, value); }

    /// <summary>The right column, typically a <see cref="KlyPageAside"/> holding a table of contents.</summary>
    public View? Right { get => (View?)GetValue(RightProperty); set => SetValue(RightProperty, value); }

    internal bool IsWide => _width >= PagesUi.Lg;

    /// <summary>Number of columns of the ten-column grid the center spans from 1024 units: 6 with both sides, 8 with one, 10 with none.</summary>
    internal int CenterSpan => (Left is not null, Right is not null) switch
    {
        (true, true) => 6,
        (false, false) => 10,
        _ => 8,
    };

    internal void Relayout(double width)
    {
        _width = width;
        Rebuild();
    }

    private void Rebuild()
    {
        Children.Clear();
        ColumnDefinitions = new ColumnDefinitionCollection();
        RowDefinitions = new RowDefinitionCollection();
        var views = new[] { Left, Center, Right }.Where(v => v is not null).Select(v => v!).ToList();
        if (IsWide)
        {
            ColumnSpacing = 40;
            RowSpacing = 0;
            for (var i = 0; i < 10; i++) ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var column = 0;
            foreach (var view in views)
            {
                var span = view == Center ? CenterSpan : 2;
                this.Add(view, column, 0);
                SetColumnSpan((BindableObject)view, span);
                column += span;
            }
        }
        else
        {
            ColumnSpacing = 0;
            RowSpacing = 32;
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var i = 0; i < views.Count; i++)
            {
                RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                this.Add(views[i], 0, i);
                SetColumnSpan((BindableObject)views[i], 1);
            }
        }
    }
}
