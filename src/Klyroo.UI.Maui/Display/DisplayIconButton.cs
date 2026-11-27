namespace Klyroo.UI.Maui;

/// <summary>A tap target with an icon in the middle: the copy and close buttons of the display controls. It is 40 units wide and high, as a touch target must be.</summary>
internal sealed class DisplayIconButton : Border
{
    private readonly KlyIcon _icon;

    public DisplayIconButton(string iconName, string description, KlyThemeColor? color = null)
    {
        StrokeThickness = 0;
        Padding = 0;
        WidthRequest = 40;
        HeightRequest = 40;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Center;
        _icon = new KlyIcon { Name = iconName, Size = KlySize.Sm, IconColor = color ?? KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Content = _icon;
        SemanticProperties.SetDescription(this, description);
        DisplayParts.Tap(this, () => Clicked?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Clicked;

    public KlyIcon Icon => _icon;

    public void SendClicked()
    {
        if (IsEnabled) Clicked?.Invoke(this, EventArgs.Empty);
    }

    public void SetDescription(string description) => SemanticProperties.SetDescription(this, description);
}
