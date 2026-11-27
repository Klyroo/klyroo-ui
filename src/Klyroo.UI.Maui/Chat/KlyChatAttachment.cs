using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A file as a chip: an icon or a picture, the name and the size, with an optional remove button.
/// An image with a <see cref="Url"/> shows a preview; <see cref="Status"/> tells an upload in progress or a failed one.
/// </summary>
public class KlyChatAttachment : Border
{
    private const string SpinnerName = "KlyChatAttachmentSpinner";

    /// <summary>Identifies <see cref="Name"/>.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(KlyChatAttachment), "", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Type"/>.</summary>
    public static readonly BindableProperty TypeProperty = BindableProperty.Create(nameof(Type), typeof(string), typeof(KlyChatAttachment), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Url"/>.</summary>
    public static readonly BindableProperty UrlProperty = BindableProperty.Create(nameof(Url), typeof(string), typeof(KlyChatAttachment), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="FileSize"/>.</summary>
    public static readonly BindableProperty FileSizeProperty = BindableProperty.Create(nameof(FileSize), typeof(long?), typeof(KlyChatAttachment), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyChatAttachment), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Status"/>.</summary>
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(nameof(Status), typeof(ChatAttachmentStatus), typeof(KlyChatAttachment), ChatAttachmentStatus.Idle, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ChatAttachmentVariant), typeof(KlyChatAttachment), ChatAttachmentVariant.Card, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyChatAttachment), StackOrientation.Horizontal, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyChatAttachment), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyChatAttachment), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Removable"/>.</summary>
    public static readonly BindableProperty RemovableProperty = BindableProperty.Create(nameof(Removable), typeof(bool), typeof(KlyChatAttachment), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="UploadingLabel"/>.</summary>
    public static readonly BindableProperty UploadingLabelProperty = BindableProperty.Create(nameof(UploadingLabel), typeof(string), typeof(KlyChatAttachment), "uploading", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="FailedLabel"/>.</summary>
    public static readonly BindableProperty FailedLabelProperty = BindableProperty.Create(nameof(FailedLabel), typeof(string), typeof(KlyChatAttachment), "failed", propertyChanged: Restyle);
    /// <summary>Identifies <see cref="RemoveLabel"/>.</summary>
    public static readonly BindableProperty RemoveLabelProperty = BindableProperty.Create(nameof(RemoveLabel), typeof(string), typeof(KlyChatAttachment), "Remove {0}", propertyChanged: Restyle);

    private readonly Grid _grid = new() { ColumnSpacing = 8 };
    private readonly Border _leading = new() { StrokeThickness = 0, VerticalOptions = LayoutOptions.Center };
    private readonly Grid _leadingContent = new();
    private readonly Image _image = new() { Aspect = Aspect.AspectFill, IsVisible = false };
    private readonly KlyIcon _glyph = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly ContentView _custom = new() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, IsVisible = false };
    private readonly VerticalStackLayout _wrapper = new() { VerticalOptions = LayoutOptions.Center };
    private readonly Label _name = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation };
    private readonly KlyChatShimmer _nameShimmer = new() { FontAttributes = FontAttributes.Bold, LineBreakMode = LineBreakMode.TailTruncation, IsVisible = false };
    private readonly Label _description = new() { FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation, IsVisible = false };
    private readonly ChatDataIconButton _remove = new() { IconName = KlyIconNames.Close, IconSize = KlySize.Xs, IsVisible = false };
    private bool _preview;

    /// <summary>Creates an attachment.</summary>
    public KlyChatAttachment()
    {
        _wrapper.Children.Add(_name);
        _wrapper.Children.Add(_nameShimmer);
        _wrapper.Children.Add(_description);
        _leadingContent.Children.Add(_image);
        _leadingContent.Children.Add(_glyph);
        _leadingContent.Children.Add(_custom);
        _leading.Content = _leadingContent;
        _grid.Children.Add(_leading);
        _grid.Children.Add(_wrapper);
        _grid.Children.Add(_remove);
        Content = _grid;
        _remove.Clicked += (_, _) => Removed?.Invoke(this, EventArgs.Empty);
        ApplyStyle();
    }

    /// <summary>Raised when the user presses the remove button. Take the attachment out of your list.</summary>
    public event EventHandler? Removed;

    /// <summary>File name.</summary>
    public string Name { get => (string)GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>Media type (<c>image/png</c>): an image with a <see cref="Url"/> shows a preview.</summary>
    public string? Type { get => (string?)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Address of the file: a URL, or the name of a local image file. With an image media type, the attachment shows a preview of it.</summary>
    public string? Url { get => (string?)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }

    /// <summary>Size in bytes, shown in the description.</summary>
    public long? FileSize { get => (long?)GetValue(FileSizeProperty); set => SetValue(FileSizeProperty, value); }

    /// <summary>Replaces the default description: the extension, then the size. For an error, put the reason here.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Whether the file is idle, uploading or failed.</summary>
    public ChatAttachmentStatus Status { get => (ChatAttachmentStatus)GetValue(StatusProperty); set => SetValue(StatusProperty, value); }

    /// <summary>Look of the attachment: a <c>Card</c> or a <c>Thumbnail</c>.</summary>
    public ChatAttachmentVariant Variant { get => (ChatAttachmentVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Card only: a vertical card gives an image a large preview.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Size of the attachment, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Replaces the file icon.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Shows a remove button. Handle <see cref="Removed"/> and take the attachment out of your list.</summary>
    public bool Removable { get => (bool)GetValue(RemovableProperty); set => SetValue(RemovableProperty, value); }

    /// <summary>What a screen reader adds after the name while the file uploads, in the language of the app.</summary>
    public string UploadingLabel { get => (string)GetValue(UploadingLabelProperty); set => SetValue(UploadingLabelProperty, value); }

    /// <summary>What a screen reader adds after the name when the upload failed, in the language of the app.</summary>
    public string FailedLabel { get => (string)GetValue(FailedLabelProperty); set => SetValue(FailedLabelProperty, value); }

    /// <summary>Accessible name of the remove button: <c>{0}</c> is the file name.</summary>
    public string RemoveLabel { get => (string)GetValue(RemoveLabelProperty); set => SetValue(RemoveLabelProperty, value); }

    internal Grid Body => _grid;
    internal Border Leading => _leading;
    internal Label NameLabel => _name;
    internal KlyChatShimmer NameShimmer => _nameShimmer;
    internal Label DescriptionLabel => _description;
    internal ChatDataIconButton RemoveButton => _remove;
    internal KlyIcon Glyph => _glyph;
    internal Image Preview => _image;
    internal bool ShowsPreview => _preview;

    /// <summary>A size in bytes as a short text: <c>512 B</c>, <c>1.5 KB</c>, <c>2 MB</c>, with the digits of the current culture.</summary>
    internal static string FormatSize(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return string.Format(culture, "{0} {1}", Math.Round(size, 1).ToString(culture), units[unit]);
    }

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        SyncMotion();
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyChatAttachment)bindable).ApplyStyle();

    private static (double Leading, double Thumbnail, double Font, KlySize IconSize) Metrics(KlySize size) => size switch
    {
        KlySize.Xs => (24, 40, 12, KlySize.Xs),
        KlySize.Sm => (28, 48, 12, KlySize.Xs),
        KlySize.Md => (36, 64, 14, KlySize.Md),
        KlySize.Lg => (40, 80, 14, KlySize.Md),
        KlySize.Xl => (48, 96, 16, KlySize.Xl),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private void ApplyStyle()
    {
        var thumbnail = Variant == ChatAttachmentVariant.Thumbnail;
        var vertical = !thumbnail && Orientation == StackOrientation.Vertical;
        var error = Status == ChatAttachmentStatus.Error;
        var uploading = Status == ChatAttachmentStatus.Uploading;
        var (box, thumbBox, font, iconSize) = Metrics(Size);
        _preview = !string.IsNullOrEmpty(Url) && Type?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true;
        var errorSet = KlyTheme.Resolve(KlyColor.Error);

        // The root: a bordered chip, or the picture alone.
        this.SetRadius(KlyTheme.RadiusOf(1));
        this.SetThemeColor(BackgroundColorProperty, error ? errorSet.Fill.WithAlpha(0.1f) : thumbnail ? KlyThemeColor.Transparent : KlyTheme.BackgroundElevated);
        this.SetStroke(error ? errorSet.Fill.WithAlpha(0.5f) : KlyTheme.BorderAccented);
        StrokeThickness = 1;
        Padding = thumbnail ? new Thickness(0) : new Thickness(6);
        HorizontalOptions = vertical ? LayoutOptions.Fill : LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Start;
        (WidthRequest, HeightRequest) = thumbnail ? (thumbBox, thumbBox) : (-1d, -1d);

        // The leading box: an icon, or a preview that fills it.
        var previewHeight = vertical && _preview ? 128d : box;
        _leading.SetRadius(thumbnail ? 0 : KlyTheme.RadiusOf(1) - 2);
        _leading.SetThemeColor(BackgroundColorProperty, error ? errorSet.Fill.WithAlpha(0.15f) : KlyTheme.BackgroundAccented);
        (_leading.WidthRequest, _leading.HeightRequest) = thumbnail ? (-1d, -1d) : (vertical && _preview ? -1d : box, previewHeight);
        _leading.HorizontalOptions = thumbnail || (vertical && _preview) ? LayoutOptions.Fill : LayoutOptions.Start;
        _leading.VerticalOptions = thumbnail ? LayoutOptions.Fill : LayoutOptions.Center;

        _image.IsVisible = _preview;
        _image.Opacity = uploading ? 0.6 : 1;
        if (_preview) _image.Source = ToSource(Url!);
        SemanticProperties.SetDescription(_image, thumbnail ? Name : null);
        AutomationProperties.SetIsInAccessibleTree(_image, thumbnail);

        var custom = Icon is not null && !_preview && !uploading && !error;
        _custom.Content = custom ? Icon : null;
        _custom.IsVisible = custom;
        _glyph.IsVisible = !_preview && !custom;
        _glyph.Name = uploading ? KlyIconNames.Loading : error ? KlyIconNames.Caution : KlyIconNames.File;
        _glyph.Size = iconSize;
        _glyph.IconColor = error ? errorSet.Text : KlyTheme.TextMuted;

        // The text.
        var extension = Path.GetExtension(Name ?? string.Empty).TrimStart('.').ToUpperInvariant();
        var computed = string.Join(" · ", new[] { extension, FileSize is { } bytes ? FormatSize(bytes) : string.Empty }.Where(p => p.Length > 0));
        var description = Description ?? (computed.Length > 0 ? computed : null);
        _name.Text = Name;
        _name.FontSize = font;
        _name.SetThemeColor(Label.TextColorProperty, KlyTheme.Text);
        _nameShimmer.Text = Name ?? string.Empty;
        _nameShimmer.FontSize = font;
        _name.IsVisible = !uploading;
        _nameShimmer.IsVisible = uploading;
        _description.Text = description;
        _description.IsVisible = description is not null;
        _description.SetThemeColor(Label.TextColorProperty, error ? errorSet.Text : KlyTheme.TextMuted);
        var statusText = uploading ? UploadingLabel : error ? FailedLabel : null;
        var spoken = statusText is null ? null : $"{Name} ({statusText})";
        SemanticProperties.SetDescription(_name, spoken);
        SemanticProperties.SetDescription(_nameShimmer, spoken ?? Name);
        _wrapper.IsVisible = !thumbnail;

        // The remove button: over the picture of a thumbnail or of a vertical card, at the end of a horizontal one.
        var overlay = thumbnail || vertical;
        _remove.IsVisible = Removable;
        _remove.Description = string.Format(CultureInfo.CurrentCulture, RemoveLabel, Name);
        _remove.Variant = overlay && _preview ? ButtonVariant.Solid : ButtonVariant.Ghost;
        _remove.Margin = overlay ? new Thickness(4) : new Thickness(0, -8);
        _remove.HorizontalOptions = overlay ? LayoutOptions.End : LayoutOptions.Center;
        _remove.VerticalOptions = overlay ? LayoutOptions.Start : LayoutOptions.Center;

        PlaceParts(thumbnail, vertical);
        SyncMotion();
    }

    private void PlaceParts(bool thumbnail, bool vertical)
    {
        _grid.ColumnDefinitions.Clear();
        _grid.RowDefinitions.Clear();
        _wrapper.Margin = vertical ? new Thickness(2, 0) : new Thickness(0);
        if (thumbnail)
        {
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            SetCell(_leading, 0, 0);
            SetCell(_wrapper, 0, 0);
            SetCell(_remove, 0, 0);
        }
        else if (vertical)
        {
            _grid.RowSpacing = 8;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            _grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            SetCell(_leading, 0, 0);
            SetCell(_wrapper, 1, 0);
            SetCell(_remove, 0, 0);
        }
        else
        {
            _grid.RowSpacing = 0;
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            _grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            SetCell(_leading, 0, 0);
            SetCell(_wrapper, 0, 1);
            SetCell(_remove, 0, 2);
        }
    }

    private void SetCell(View view, int row, int column)
    {
        ChatDataGrid.Row(view, row);
        ChatDataGrid.Column(view, column);
    }

    private static ImageSource ToSource(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https" ? ImageSource.FromUri(uri) : ImageSource.FromFile(url);

    // The spinner of an upload turns while the chip is on screen.
    private void SyncMotion()
    {
        ChatDataMotion.Stop(_glyph, SpinnerName);
        _glyph.Rotation = 0;
        if (Handler is not null && Status == ChatAttachmentStatus.Uploading && !_preview)
        {
            ChatDataMotion.Loop(_glyph, SpinnerName, value => _glyph.Rotation = value * 360, 1000);
        }
    }
}
