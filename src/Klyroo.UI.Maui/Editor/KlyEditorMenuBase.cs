using MauiLabel = Microsoft.Maui.Controls.Label;

namespace Klyroo.UI.Maui;

/// <summary>
/// Base of the menus that open when a trigger character is typed in a <see cref="KlyEditor"/>. The menu is a list under the editor: it opens while the word before the
/// cursor starts with <see cref="Char"/>, filters as the user types (best matches first: label starts with the text, label contains it, keywords contain it) and
/// takes the typed word out for what the user taps. Put the menu in <see cref="KlyEditor.Children"/>. There is no keyboard navigation of the list: a native text
/// field keeps the arrow keys and Enter for itself, so an option is chosen by tapping it.
/// </summary>
public abstract class KlyEditorMenuBase : Border
{
    /// <summary>Identifies <see cref="Char"/>.</summary>
    public static readonly BindableProperty CharProperty = BindableProperty.Create(nameof(Char), typeof(string), typeof(KlyEditorMenuBase), null);
    /// <summary>Identifies <see cref="Limit"/>.</summary>
    public static readonly BindableProperty LimitProperty = BindableProperty.Create(nameof(Limit), typeof(int), typeof(KlyEditorMenuBase), 42, propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Refresh());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyEditorMenuBase), null, propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Describe());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyEditorMenuBase), KlySize.Md, propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Refresh());

    private const double MaxHeight = 288;

    private readonly VerticalStackLayout _list = new();
    private readonly MauiLabel _empty = new() { Text = "No results", FontSize = 14, Padding = new Thickness(12, 8), IsVisible = false };
    private List<EditorMenuOption> _shown = [];
    private string? _query;

    private protected KlyEditorMenuBase()
    {
        IsVisible = false;
        Padding = new Thickness(4);
        HorizontalOptions = LayoutOptions.Fill;
        Content = new ScrollView { MaximumHeightRequest = MaxHeight, Content = new VerticalStackLayout { Children = { _list, _empty } } };
        this.SetRadius(KlyTheme.RadiusOf(1));
        _empty.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
        ApplyStyle();
        Describe();
    }

    /// <summary>Raised when an option is chosen, before the editor changes the text.</summary>
    internal event EventHandler<EditorMenuOption>? Chosen;

    /// <summary>Character that opens the menu. Defaults to <c>/</c> for commands, <c>@</c> for mentions and <c>:</c> for emoji.</summary>
    public string? Char { get => (string?)GetValue(CharProperty); set => SetValue(CharProperty, value); }

    /// <summary>Most options shown at a time. Defaults to 42.</summary>
    public int Limit { get => (int)GetValue(LimitProperty); set => SetValue(LimitProperty, value); }

    /// <summary>What a screen reader says for the list. Defaults to <c>Commands</c>, <c>Mentions</c> or <c>Emoji</c>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Size of the options, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    internal abstract string DefaultChar { get; }

    internal abstract string DefaultLabel { get; }

    /// <summary>What choosing an option inserts: <c>suggestion</c>, <c>mention</c> or <c>emoji</c>.</summary>
    internal abstract string Kind { get; }

    /// <summary>Characters typed after the trigger before the menu opens.</summary>
    internal virtual int MinQuery => 0;

    internal string Trigger => string.IsNullOrEmpty(Char) ? DefaultChar : Char;

    internal bool IsOpen => IsVisible;

    internal IReadOnlyList<EditorMenuOption> Shown => _shown;

    internal bool ShowsEmpty => _empty.IsVisible;

    internal abstract IEnumerable<EditorMenuOption> BuildOptions();

    /// <summary>Called after the editor inserted what an option stands for.</summary>
    internal virtual void Accepted(EditorMenuOption option)
    {
    }

    /// <summary>Opens the menu for the text typed after the trigger, or closes it when that is shorter than <see cref="MinQuery"/>.</summary>
    internal void Show(string query)
    {
        if (query.Length < MinQuery)
        {
            Hide();
            return;
        }
        _query = query;
        var text = query.ToLowerInvariant();
        _shown = BuildOptions()
            .Select((option, index) => (option, index, score: Rank(option, text)))
            .Where(m => m.score >= 0)
            .OrderBy(m => m.score)
            .ThenBy(m => m.index)
            .Take(Math.Max(0, Limit))
            .Select(m => m.option)
            .ToList();
        _list.Children.Clear();
        foreach (var option in _shown) _list.Children.Add(Row(option));
        _empty.IsVisible = _shown.Count == 0;
        IsVisible = true;
    }

    internal void Hide()
    {
        _query = null;
        IsVisible = false;
    }

    /// <summary>Chooses an option, as a tap on its row does. A disabled option, or a disabled menu, does nothing.</summary>
    internal void Choose(EditorMenuOption option)
    {
        if (!IsEnabled || option.Disabled) return;
        Chosen?.Invoke(this, option);
    }

    /// <summary>Shows the options again for the same text, after the items or the size changed.</summary>
    internal void Refresh()
    {
        if (_query is not null) Show(_query);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : 0.6;
    }

    /// <summary>Best matches first: 0 when the label starts with the text, 1 when it contains it, 2 when the keywords do, -1 for no match.</summary>
    private static int Rank(EditorMenuOption option, string query)
    {
        if (query.Length == 0) return 0;
        var label = option.Label.ToLowerInvariant();
        if (label.StartsWith(query, StringComparison.Ordinal)) return 0;
        if (label.Contains(query, StringComparison.Ordinal)) return 1;
        return option.Keywords?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ? 2 : -1;
    }

    private View Row(EditorMenuOption option)
    {
        var (padding, fontSize) = Size switch
        {
            KlySize.Xs => (new Thickness(6, 4), 12d),
            KlySize.Sm => (new Thickness(8, 4), 12d),
            KlySize.Md => (new Thickness(8, 6), 14d),
            KlySize.Lg => (new Thickness(10, 8), 14d),
            KlySize.Xl => (new Thickness(12, 8), 16d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };

        var title = new Label { Text = option.Label, FontSize = fontSize, LineBreakMode = LineBreakMode.TailTruncation };
        title.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.Text);
        var body = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { title } };
        if (option.Description is not null)
        {
            var description = new Label { Text = option.Description, FontSize = 12, LineBreakMode = LineBreakMode.TailTruncation };
            description.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
            body.Children.Add(description);
        }

        var grid = new Grid { ColumnSpacing = 6, Padding = padding, ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        if (option.IconName is not null)
        {
            grid.Add(new KlyIcon { Name = option.IconName, Size = KlySize.Md, IconColor = KlyTheme.TextMuted, VerticalOptions = LayoutOptions.Center }, 0);
        }
        else if (option.Prefix is not null)
        {
            var prefix = new Label { Text = option.Prefix, FontSize = fontSize, WidthRequest = 20, HorizontalTextAlignment = TextAlignment.Center, VerticalOptions = LayoutOptions.Center };
            prefix.SetThemeColor(MauiLabel.TextColorProperty, KlyTheme.TextMuted);
            AutomationProperties.SetIsInAccessibleTree(prefix, false);
            grid.Add(prefix, 0);
        }
        grid.Add(body, 1);

        var row = new Border { StrokeThickness = 0, BackgroundColor = Colors.Transparent, MinimumHeightRequest = EditorToolbarButton.TouchTarget, Opacity = option.Disabled ? 0.6 : 1, Content = grid };
        row.SetRadius(KlyTheme.RadiusOf(1) - 2);
        SemanticProperties.SetDescription(row, option.Description is null ? option.Label : $"{option.Label}, {option.Description}");
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Choose(option);
        row.GestureRecognizers.Add(tap);
        return row;
    }

    private void Describe() => SemanticProperties.SetDescription(_list, Label ?? DefaultLabel);

    private void ApplyStyle() => this.Apply(new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text));
}
