namespace Klyroo.UI.Maui;

/// <summary>
/// A tappable area made of a real native <see cref="Button"/> (ripple, focus, keyboard and screen reader behavior of the platform) laid over a content that does not take touches.
/// It is how the chat and data controls make a row with an icon and text into a button.
/// </summary>
internal sealed class ChatDataTapArea : Grid
{
    private readonly KlyButton _button = new() { Block = true, Variant = ButtonVariant.Ghost, Color = KlyColor.Neutral, Padding = new Thickness(0), MinimumHeightRequest = 40 };
    private readonly View _content;

    public ChatDataTapArea(View content)
    {
        _content = content;
        content.InputTransparent = true;
        Children.Add(content);
        Children.Add(_button);
        _button.Clicked += (_, _) => Tapped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Raised when the area is pressed.</summary>
    public event EventHandler? Tapped;

    /// <summary>What a screen reader says for the area.</summary>
    public string? Description
    {
        get => SemanticProperties.GetDescription(_button);
        set => SemanticProperties.SetDescription(_button, value);
    }

    /// <summary>What a screen reader adds after the description, such as the action of the button.</summary>
    public string? Hint
    {
        get => SemanticProperties.GetHint(_button);
        set => SemanticProperties.SetHint(_button, value);
    }

    /// <summary>The smallest height of the touch target. 40 unless the area has room for less.</summary>
    public double MinimumTarget
    {
        get => _button.MinimumHeightRequest;
        set => _button.MinimumHeightRequest = value;
    }

    /// <summary>Whether the area takes taps. Without it only the content shows, as a plain line.</summary>
    public bool Interactive
    {
        get => _button.IsVisible;
        set => _button.IsVisible = value;
    }

    /// <summary>Presses the area, as a tap does.</summary>
    public void SendTap() => _button.SendClicked();

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web trigger: disabled:opacity-60. The parent's state does not reach the opacity of the children.
        if (propertyName == nameof(IsEnabled))
        {
            _button.IsEnabled = IsEnabled;
            _content.Opacity = IsEnabled ? 1 : 0.6;
        }
    }
}
