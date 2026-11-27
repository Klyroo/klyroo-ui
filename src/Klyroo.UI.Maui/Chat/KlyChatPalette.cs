namespace Klyroo.UI.Maui;

/// <summary>
/// A full chat panel: the conversation takes the room that is left and the prompt stays pinned under it, over a line.
/// It fills the space its parent gives it, so put it in a layout with a height (a page, a sheet, a bordered box). Messages inside are meant to be compact: see <see cref="KlyChatMessage.Compact"/>.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyChatPalette : Grid
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyChatPalette), null, propertyChanged: (b, _, v) => ((KlyChatPalette)b)._content.Content = (View?)v);
    /// <summary>Identifies <see cref="Prompt"/>.</summary>
    public static readonly BindableProperty PromptProperty = BindableProperty.Create(nameof(Prompt), typeof(View), typeof(KlyChatPalette), null, propertyChanged: (b, _, v) => ((KlyChatPalette)b).ShowPrompt((View?)v));

    private readonly ContentView _content = new();
    private readonly BoxView _line = new() { HeightRequest = 1, IsVisible = false };
    private readonly ContentView _prompt = new() { Padding = new Thickness(8), IsVisible = false };

    /// <summary>Creates an empty panel.</summary>
    public KlyChatPalette()
    {
        RowSpacing = 0;
        RowDefinitions.Add(new RowDefinition(GridLength.Star));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
        Children.Add(_content);
        Children.Add(_line);
        Children.Add(_prompt);
        ChatDataGrid.Row(_content, 0);
        ChatDataGrid.Row(_line, 1);
        ChatDataGrid.Row(_prompt, 2);
    }

    /// <summary>The conversation: a <see cref="KlyChatMessages"/>.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>The message form, pinned to the bottom: a <see cref="KlyChatPrompt"/>.</summary>
    public View? Prompt { get => (View?)GetValue(PromptProperty); set => SetValue(PromptProperty, value); }

    internal ContentView ContentHost => _content;
    internal ContentView PromptHost => _prompt;
    internal BoxView Line => _line;

    private void ShowPrompt(View? prompt)
    {
        _prompt.Content = prompt;
        _prompt.IsVisible = prompt is not null;
        _line.IsVisible = prompt is not null;
    }
}
