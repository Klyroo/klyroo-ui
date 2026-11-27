namespace Klyroo.UI.Maui;

/// <summary>
/// The small form under the toolbar that asks for the address of a link or of an image: a field, Apply, and for a link Remove.
/// A refused address stays in the field and the reason shows under it.
/// </summary>
internal sealed class EditorAddressForm : VerticalStackLayout
{
    private readonly Border _frame = new() { StrokeThickness = 1, MinimumHeightRequest = 40 };
    private readonly Label _message = new() { FontSize = 12, IsVisible = false };

    internal EditorAddressForm(string inputLabel, bool withRemove)
    {
        Spacing = 4;
        Padding = new Thickness(0, 4, 0, 0);
        IsVisible = false;
        SemanticProperties.SetDescription(this, inputLabel);

        Input = new Entry { Placeholder = "https://", Keyboard = Keyboard.Url, ReturnType = ReturnType.Done, FontSize = 14, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Center };
        SemanticProperties.SetDescription(Input, inputLabel);
        Input.Completed += (_, _) => Submitted?.Invoke(this, Input.Text?.Trim() ?? "");
        // Typing a new address takes back the explanation of the last one.
        Input.TextChanged += (_, _) => ShowProblem(null);

        ApplyButton = TextButton("Apply");
        ApplyButton.Clicked += (_, _) => Submitted?.Invoke(this, Input.Text?.Trim() ?? "");
        RemoveButton = TextButton("Remove link");
        RemoveButton.IsVisible = false;
        RemoveButton.Clicked += (_, _) => Removed?.Invoke(this, EventArgs.Empty);
        WithRemove = withRemove;

        _frame.SetRadius(KlyTheme.RadiusOf(1) - 2);
        _frame.Padding = new Thickness(8, 0);
        _frame.Content = Input;

        var row = new Grid { ColumnSpacing = 4, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto) } };
        row.Add(_frame, 0);
        row.Add(ApplyButton, 1);
        if (withRemove) row.Add(RemoveButton, 2);
        Children.Add(row);
        Children.Add(_message);
        ApplyStyle(KlyColor.Primary);
    }

    internal Entry Input { get; }

    internal Button ApplyButton { get; }

    internal Button RemoveButton { get; }

    internal bool WithRemove { get; }

    internal string? Problem => _message.IsVisible ? _message.Text : null;

    /// <summary>Raised with the trimmed address when the user applies it.</summary>
    internal event EventHandler<string>? Submitted;

    /// <summary>Raised when the user asks to remove the link.</summary>
    internal event EventHandler? Removed;

    internal void Open(string address, bool removable)
    {
        ShowProblem(null);
        Input.Text = address;
        RemoveButton.IsVisible = WithRemove && removable;
        IsVisible = true;
        Input.Focus();
    }

    internal void Close()
    {
        ShowProblem(null);
        IsVisible = false;
    }

    internal void ShowProblem(string? text)
    {
        _message.Text = text;
        _message.IsVisible = !string.IsNullOrEmpty(text);
        SemanticProperties.SetDescription(_message, text);
        if (!string.IsNullOrEmpty(text))
        {
            _frame.SetStroke(KlyTheme.Resolve(KlyColor.Error).Fill);
            _frame.StrokeThickness = 2;
        }
        else
        {
            _frame.SetStroke(KlyTheme.BorderAccented);
            _frame.StrokeThickness = 1;
        }
    }

    internal void ApplyStyle(KlyColor color)
    {
        var set = KlyTheme.Resolve(color);
        _frame.SetThemeColor(VisualElement.BackgroundColorProperty, KlyTheme.Background);
        if (!_message.IsVisible) _frame.SetStroke(KlyTheme.BorderAccented);
        Input.SetThemeColor(InputView.TextColorProperty, KlyTheme.Text);
        Input.SetThemeColor(InputView.PlaceholderColorProperty, KlyTheme.TextDimmed);
        _message.SetThemeColor(Label.TextColorProperty, KlyTheme.Resolve(KlyColor.Error).Text);
        ApplyButton.SetThemeColor(Button.TextColorProperty, set.Text);
        RemoveButton.SetThemeColor(Button.TextColorProperty, set.Text);
    }

    private static Button TextButton(string text) => new()
    {
        Text = text,
        FontSize = 12,
        TextTransform = TextTransform.None,
        BackgroundColor = Colors.Transparent,
        BorderWidth = 0,
        Padding = new Thickness(8, 4),
        MinimumHeightRequest = EditorToolbarButton.TouchTarget,
        CornerRadius = (int)(KlyTheme.RadiusOf(1) - 2),
    };
}
