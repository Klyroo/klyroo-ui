namespace Klyroo.UI.Maui;

/// <summary>The close button of panels and toasts: a Lucide cross in a 40 point touch target, with a hover tint, named for a screen reader.</summary>
internal sealed class OverlaysCloseButton : Border
{
    public OverlaysCloseButton(string label)
    {
        this.SetRadius(KlyTheme.RadiusOf(1));
        StrokeThickness = 0;
        Padding = new Thickness(10);
        MinimumWidthRequest = 40;
        MinimumHeightRequest = 40;
        HorizontalOptions = LayoutOptions.End;
        VerticalOptions = LayoutOptions.Start;
        BackgroundColor = Colors.Transparent;
        Content = new KlyIcon { Name = KlyIconNames.Close, Size = KlySize.Md, IconColor = KlyTheme.TextMuted, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        Label = label;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Clicked?.Invoke(this, EventArgs.Empty);
        GestureRecognizers.Add(tap);

        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => this.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundElevated);
        pointer.PointerExited += (_, _) => BackgroundColor = Colors.Transparent;
        GestureRecognizers.Add(pointer);
    }

    public event EventHandler? Clicked;

    /// <summary>What a screen reader says for the button.</summary>
    public string Label
    {
        get => SemanticProperties.GetDescription(this);
        set => SemanticProperties.SetDescription(this, value);
    }
}
