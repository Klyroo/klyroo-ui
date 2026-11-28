namespace Klyroo.UI.Maui;

/// <summary>A day of a <see cref="KlyCalendar"/>: a square with the day number, in the look of its state (selected, in the range, today, disabled).</summary>
internal sealed class InputsCalendarDay : Border
{
    public InputsCalendarDay(DateOnly date, string text, string description, bool selected, bool inRange, bool today, bool disabled, KlyColorSet set, double dimension, double fontSize)
    {
        Date = date;
        Selected = selected;
        InRange = inRange;
        Today = today;
        Disabled = disabled;
        WidthRequest = dimension;
        HeightRequest = dimension;

        // A day in the range has square corners so that the range reads as one band.
        this.SetRadius(inRange ? 0 : KlyTheme.RadiusOf(1));
        KlyThemeColor background = KlyThemeColor.Transparent;
        KlyThemeColor stroke = KlyThemeColor.Transparent;
        var thickness = 0d;
        var textColor = KlyTheme.Text;
        if (selected)
        {
            background = set.Fill;
            stroke = set.Text;
            thickness = 1;
            textColor = set.Contrast;
        }
        else if (inRange)
        {
            background = set.Fill.WithAlpha(0.15f);
        }
        if (today && !selected)
        {
            stroke = set.Text;
            thickness = 1;
        }
        if (disabled) textColor = KlyTheme.TextDimmed;
        this.SetThemeColor(BackgroundColorProperty, background);
        this.SetStroke(stroke);
        StrokeThickness = thickness;

        var label = new Label
        {
            Text = text,
            FontSize = fontSize,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            TextDecorations = disabled ? TextDecorations.Strikethrough : TextDecorations.None,
        };
        label.SetThemeColor(Label.TextColorProperty, textColor);
        Content = label;
        Label = label;

        SemanticProperties.SetDescription(this, description);
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => Press();
        GestureRecognizers.Add(tap);
    }

    public DateOnly Date { get; }

    public bool Selected { get; }

    public bool InRange { get; }

    public bool Today { get; }

    public bool Disabled { get; }

    public Label Label { get; }

    public event EventHandler<DateOnly>? Picked;

    public void Press()
    {
        if (IsEnabled && !Disabled) Picked?.Invoke(this, Date);
    }
}
