namespace Klyroo.UI.Maui;

/// <summary>A small icon that acts as a button: the step buttons of a number field, the navigation of a calendar, the remove button of a tag or a file.</summary>
internal sealed class InputsIconButton : ContentView
{
    private readonly KlyIcon _icon;

    public InputsIconButton(string iconName, string description, KlySize iconSize = KlySize.Md, double minimumWidth = 40, double minimumHeight = 40)
    {
        _icon = new KlyIcon
        {
            Name = iconName,
            Size = iconSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            InputTransparent = true,
            IconColor = KlyTheme.TextMuted,
        };
        Content = _icon;
        MinimumWidthRequest = minimumWidth;
        MinimumHeightRequest = minimumHeight;
        SemanticProperties.SetDescription(this, description);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Press();
        GestureRecognizers.Add(tap);
    }

    public event EventHandler? Clicked;

    public string IconName
    {
        get => _icon.Name;
        set => _icon.Name = value;
    }

    public KlyThemeColor IconColor
    {
        set => _icon.IconColor = value;
    }

    public double Dimmed { get; set; } = 0.4;

    public void Press()
    {
        if (IsEnabled && IsVisible) Clicked?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Opacity = IsEnabled ? 1 : Dimmed;
    }
}
