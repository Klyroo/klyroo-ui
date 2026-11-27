namespace Klyroo.UI.Maui;

/// <summary>A grid of images. When there are more than <see cref="Max"/>, the last tile shows how many are hidden.</summary>
public class KlyImageGallery : KlyBlockBase
{
    /// <summary>Identifies <see cref="Images"/>.</summary>
    public static readonly BindableProperty ImagesProperty = BindableProperty.Create(nameof(Images), typeof(IReadOnlyList<KlyGalleryImage>), typeof(KlyImageGallery), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int), typeof(KlyImageGallery), 3, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Columns"/>.</summary>
    public static readonly BindableProperty ColumnsProperty = BindableProperty.Create(nameof(Columns), typeof(int), typeof(KlyImageGallery), 3, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Caption"/>.</summary>
    public static readonly BindableProperty CaptionProperty = BindableProperty.Create(nameof(Caption), typeof(string), typeof(KlyImageGallery), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ImageAltFormat"/>.</summary>
    public static readonly BindableProperty ImageAltFormatProperty = BindableProperty.Create(nameof(ImageAltFormat), typeof(string), typeof(KlyImageGallery), "Image {0}", propertyChanged: Rebuild);

    private const double TileHeight = 96;

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };

    /// <summary>Creates a gallery.</summary>
    public KlyImageGallery() : base(KlySurface.Plain, 8)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The images, as <see cref="KlyGalleryImage"/>.</summary>
    public IReadOnlyList<KlyGalleryImage>? Images { get => (IReadOnlyList<KlyGalleryImage>?)GetValue(ImagesProperty); set => SetValue(ImagesProperty, value); }

    /// <summary>The number of tiles shown. The last one carries a +N overflow when more images exist (<c>3</c> by default, at least <c>1</c>).</summary>
    public int Max { get => (int)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of columns of the grid, from <c>1</c> to <c>6</c> (<c>3</c> by default; other values are clamped).</summary>
    public int Columns { get => (int)GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <summary>A line of smaller text under the gallery.</summary>
    public string? Caption { get => (string?)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>The text alternative of an image that has none, where <c>{0}</c> is its position, in the language of the app.</summary>
    public string ImageAltFormat { get => (string)GetValue(ImageAltFormatProperty); set => SetValue(ImageAltFormatProperty, value); }

    internal Grid? Tiles => _stack.Children.OfType<Grid>().FirstOrDefault();

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyImageGallery)bindable).Rebuild();

    private void Rebuild()
    {
        _stack.Children.Clear();
        if (Images is { Count: > 0 } images)
        {
            var columns = Math.Clamp(Columns, 1, 6);
            var shown = images.Take(Math.Max(1, Max)).ToList();
            var hidden = images.Count - shown.Count;
            var grid = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
            for (var c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            for (var i = 0; i < shown.Count; i++)
            {
                if (i % columns == 0) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                grid.Add(Tile(shown[i], i, i == shown.Count - 1 && hidden > 0 ? hidden : 0), i % columns, i / columns);
            }
            _stack.Children.Add(grid);
        }

        if (!string.IsNullOrEmpty(Caption))
        {
            var caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = Caption;
            _stack.Children.Add(caption);
        }
    }

    private Border Tile(KlyGalleryImage image, int index, int overflow)
    {
        var tile = new Border { StrokeThickness = 1, Padding = 0, HeightRequest = TileHeight };
        tile.SetRadius(KlyTheme.RadiusOf(1));
        tile.SetStroke(KlyTheme.Border);
        var picture = new Image { Source = image.Src, Aspect = Aspect.AspectFill, HeightRequest = TileHeight };
        var content = new Grid { Children = { picture } };
        if (overflow > 0)
        {
            // The same pair of tokens as the web overlay (60% black with white text), taken from the inverted tokens so that it reads in the dark theme too.
            var veil = new Border { StrokeThickness = 0 };
            veil.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundInverted.WithAlpha(0.6f));
            var count = DisplayParts.NewLabel(14, KlyTheme.TextInverted, FontAttributes.Bold, LineBreakMode.NoWrap);
            count.Text = $"+{overflow}";
            count.HorizontalOptions = LayoutOptions.Center;
            count.VerticalOptions = LayoutOptions.Center;
            veil.Content = count;
            content.Children.Add(veil);
        }
        tile.Content = content;
        SemanticProperties.SetDescription(tile, image.Alt ?? string.Format(ImageAltFormat, index + 1));
        return tile;
    }
}
