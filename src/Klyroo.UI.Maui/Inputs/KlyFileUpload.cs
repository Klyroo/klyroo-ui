using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A file input with a drop zone that opens the file picker of the platform, a list of the chosen files with remove buttons, and limits on size, count and type.
/// A new pick replaces the files chosen before. Files that break a limit are left out and named in an error text under the list.
/// </summary>
public class KlyFileUpload : VerticalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Multiple"/>.</summary>
    public static readonly BindableProperty MultipleProperty = BindableProperty.Create(nameof(Multiple), typeof(bool), typeof(KlyFileUpload), false);
    /// <summary>Identifies <see cref="Accept"/>.</summary>
    public static readonly BindableProperty AcceptProperty = BindableProperty.Create(nameof(Accept), typeof(string), typeof(KlyFileUpload), null);
    /// <summary>Identifies <see cref="MaxSize"/>.</summary>
    public static readonly BindableProperty MaxSizeProperty = BindableProperty.Create(nameof(MaxSize), typeof(long?), typeof(KlyFileUpload), null, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());
    /// <summary>Identifies <see cref="MaxFiles"/>.</summary>
    public static readonly BindableProperty MaxFilesProperty = BindableProperty.Create(nameof(MaxFiles), typeof(int?), typeof(KlyFileUpload), null);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyFileUpload), null, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());
    /// <summary>Identifies <see cref="Hint"/>.</summary>
    public static readonly BindableProperty HintProperty = BindableProperty.Create(nameof(Hint), typeof(string), typeof(KlyFileUpload), null, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyFileUpload), KlySize.Md, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyFileUpload), false, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyFileUpload), null, propertyChanged: (b, _, _) => ((KlyFileUpload)b).Rebuild());

    private readonly InputsFieldTaken _taken = new();
    private readonly Label _error = new() { IsVisible = false };
    private readonly List<InputsIconButton> _removeButtons = [];
    private IReadOnlyList<FileResult> _files = [];
    private string? _errorText;

    /// <summary>Creates a file upload.</summary>
    public KlyFileUpload()
    {
        Spacing = 8;
        HorizontalOptions = LayoutOptions.Fill;
        Rebuild();
    }

    /// <summary>Raised when the chosen files change, by the user or by <see cref="ClearFiles"/>.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<FileResult>>>? FilesChanged;

    /// <summary>The chosen files.</summary>
    public IReadOnlyList<FileResult> Files => _files;

    /// <summary>The error text under the list: what the last pick left out, or <c>null</c>.</summary>
    public string? Error => _errorText;

    /// <summary>Allows choosing several files.</summary>
    public bool Multiple { get => (bool)GetValue(MultipleProperty); set => SetValue(MultipleProperty, value); }

    /// <summary>The accepted types: comma separated extensions (<c>.png</c>) and MIME types (<c>image/*</c>). The picker shows them where the platform can filter, and every pick is checked.</summary>
    public string? Accept { get => (string?)GetValue(AcceptProperty); set => SetValue(AcceptProperty, value); }

    /// <summary>Largest file, in bytes. Larger files are left out with a message.</summary>
    public long? MaxSize { get => (long?)GetValue(MaxSizeProperty); set => SetValue(MaxSizeProperty, value); }

    /// <summary>Most files kept at once. Defaults to 1 unless <see cref="Multiple"/>.</summary>
    public int? MaxFiles { get => (int?)GetValue(MaxFilesProperty); set => SetValue(MaxFilesProperty, value); }

    /// <summary>Main text of the drop zone. Defaults to <c>Drop files here or click to browse</c>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Secondary text of the drop zone, such as the accepted formats.</summary>
    public string? Hint { get => (string?)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    /// <summary>Size of the texts, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the border of the drop zone in the error color.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the drop zone. Defaults to its text.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal Border DropZone { get; private set; } = new();

    internal IReadOnlyList<InputsIconButton> RemoveButtons => _removeButtons;

    internal Label ErrorLabel => _error;

    /// <summary>Opens the file picker. Tests replace it.</summary>
    internal Func<PickOptions, bool, Task<IEnumerable<FileResult>?>> Picker { get; set; } = PickWithPlatformAsync;

    /// <summary>Opens the file picker of the platform and takes the files chosen: they replace the files chosen before, after the limits are checked.</summary>
    public async Task PickAsync()
    {
        if (!IsEnabled) return;
        var options = new PickOptions { PickerTitle = Label ?? Texts.DropFiles, FileTypes = InputsFileTypes.ToPickerType(Accept) };
        var picked = await Picker(options, Multiple);
        if (picked is null) return;
        var list = picked.Where(f => f is not null).ToList();
        if (list.Count == 0) return;
        Take(list);
    }

    /// <summary>Removes every chosen file.</summary>
    public void ClearFiles() => SetFiles([], null);

    /// <summary>The size of a number of bytes as a short text: <c>512 B</c>, <c>1.5 KB</c>, <c>2 MB</c>.</summary>
    internal static string FormatSize(long bytes, CultureInfo culture)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return $"{Math.Round(size, 1).ToString(culture)} {units[unit]}";
    }

    /// <summary>Checks files against the limits, as when they are picked: the files that fit become the chosen files and one message names each refusal.</summary>
    internal void Take(IReadOnlyList<FileResult> picked)
    {
        var culture = CultureInfo.CurrentCulture;
        var limit = MaxFiles is > 0 ? MaxFiles.Value : Multiple ? int.MaxValue : 1;
        var accepted = new List<FileResult>();
        var errors = new List<string>();
        foreach (var file in picked)
        {
            if (!InputsFileTypes.Matches(file.FileName, InputsFileTypes.ContentTypeOf(file), Accept))
            {
                errors.Add(string.Format(culture, Texts.FileNotAccepted, file.FileName));
            }
            else if (MaxSize is { } max && SizeOf(file) > max)
            {
                errors.Add(string.Format(culture, Texts.FileTooLarge, file.FileName, FormatSize(max, culture)));
            }
            else if (accepted.Count >= limit)
            {
                var message = string.Format(culture, Texts.TooManyFiles, limit);
                if (!errors.Contains(message)) errors.Add(message);
            }
            else
            {
                accepted.Add(file);
            }
        }
        SetFiles(accepted, errors.Count == 0 ? null : string.Join(' ', errors));
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web drop zone: has-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            Opacity = IsEnabled ? 1 : 0.6;
            foreach (var button in _removeButtons) button.IsEnabled = IsEnabled;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(DropZone, state.Hint);
    }

    private static async Task<IEnumerable<FileResult>?> PickWithPlatformAsync(PickOptions options, bool multiple)
    {
        if (multiple) return await FilePicker.Default.PickMultipleAsync(options);
        var file = await FilePicker.Default.PickAsync(options);
        return file is null ? null : [file];
    }

    private static long SizeOf(FileResult file)
    {
        try
        {
            if (File.Exists(file.FullPath)) return new FileInfo(file.FullPath).Length;
            using var stream = file.OpenReadAsync().GetAwaiter().GetResult();
            return stream.CanSeek ? stream.Length : 0;
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }
    }

    private static double FontSizeOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm => 12,
        KlySize.Md or KlySize.Lg => 14,
        KlySize.Xl => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private void SetFiles(IReadOnlyList<FileResult> files, string? error)
    {
        var old = _files;
        _files = files;
        _errorText = error;
        Rebuild();
        if (!old.SequenceEqual(files)) FilesChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<FileResult>>(old, files));
    }

    private void Remove(FileResult file) => SetFiles([.. _files.Where(f => !ReferenceEquals(f, file))], null);

    private async void OnDropZoneTapped()
    {
        try
        {
            await PickAsync();
        }
        catch (Exception exception)
        {
            // A picker that cannot open (a permission refused, no picker on the device) is shown under the list instead of ending the app.
            SetFiles(_files, exception.Message);
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _removeButtons.Clear();
        var fontSize = FontSizeOf(Size);
        var culture = CultureInfo.CurrentCulture;

        var zone = new VerticalStackLayout { Spacing = 4, HorizontalOptions = LayoutOptions.Center };
        zone.Children.Add(new KlyIcon { Name = KlyIconNames.Upload, Size = KlySize.Xl, IconColor = KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center });
        zone.Children.Add(MakeLabel(Label ?? Texts.DropFiles, fontSize, KlyTheme.Text, FontAttributes.Bold));
        if (!string.IsNullOrEmpty(Hint)) zone.Children.Add(MakeLabel(Hint, fontSize, KlyTheme.TextMuted));
        if (MaxSize is { } max) zone.Children.Add(MakeLabel(string.Format(culture, Texts.MaximumSize, FormatSize(max, culture)), fontSize, KlyTheme.TextMuted));

        DropZone = new Border
        {
            Padding = new Thickness(24),
            StrokeThickness = 2,
            StrokeDashArray = [4, 3],
            Content = zone,
        };
        DropZone.SetRadius(KlyTheme.RadiusOf(1));
        DropZone.SetThemeColor(BackgroundColorProperty, KlyTheme.Background);
        DropZone.SetStroke(Invalid ? KlyTheme.Resolve(KlyColor.Error).Fill : KlyTheme.BorderAccented);
        SemanticProperties.SetDescription(DropZone, string.IsNullOrEmpty(AccessibleName) ? Label ?? Texts.DropFiles : AccessibleName);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => OnDropZoneTapped();
        DropZone.GestureRecognizers.Add(tap);
        Children.Add(DropZone);

        if (_files.Count > 0)
        {
            var list = new VerticalStackLayout { Spacing = 4 };
            foreach (var file in _files)
            {
                var row = new Grid { ColumnSpacing = 8 };
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                var name = MakeLabel(file.FileName, fontSize, KlyTheme.Text);
                name.LineBreakMode = LineBreakMode.MiddleTruncation;
                var size = MakeLabel(FormatSize(SizeOf(file), culture), fontSize, KlyTheme.TextMuted);
                var remove = new InputsIconButton(KlyIconNames.Close, string.Format(culture, Texts.RemoveFile, file.FileName), KlySize.Sm) { IsEnabled = IsEnabled };
                var removed = file;
                remove.Clicked += (_, _) => Remove(removed);
                _removeButtons.Add(remove);
                row.Add(name, 0);
                row.Add(size, 1);
                row.Add(remove, 2);

                var item = new Border { Padding = new Thickness(8, 2), StrokeThickness = 1, Content = row };
                item.SetRadius(KlyTheme.RadiusOf(1));
                item.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
                item.SetStroke(KlyTheme.Border);
                list.Children.Add(item);
            }
            Children.Add(list);
        }

        _error.Text = _errorText;
        _error.IsVisible = !string.IsNullOrEmpty(_errorText);
        _error.FontSize = fontSize;
        _error.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, KlyTheme.Resolve(KlyColor.Error).Text);
        Children.Add(_error);
    }

    private static Label MakeLabel(string text, double fontSize, KlyThemeColor color, FontAttributes attributes = FontAttributes.None)
    {
        var label = new Label { Text = text, FontSize = fontSize, FontAttributes = attributes, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.Center };
        label.SetThemeColor(Microsoft.Maui.Controls.Label.TextColorProperty, color);
        return label;
    }

    /// <summary>The texts of the control, in English.</summary>
    private static class Texts
    {
        public const string DropFiles = "Drop files here or click to browse";
        public const string RemoveFile = "Remove {0}";
        public const string MaximumSize = "Maximum size {0}";
        public const string FileTooLarge = "{0} is larger than {1}.";
        public const string TooManyFiles = "At most {0} files are allowed.";
        public const string FileNotAccepted = "{0} is not an accepted file type.";
    }
}
