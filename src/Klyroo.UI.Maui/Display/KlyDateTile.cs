using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>A calendar tile with a title: the date, the days until or since it, or a range between two dates.</summary>
public class KlyDateTile : KlyBlockBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(DateTimeOffset?), typeof(KlyDateTile), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="EndValue"/>.</summary>
    public static readonly BindableProperty EndValueProperty = BindableProperty.Create(nameof(EndValue), typeof(DateTimeOffset?), typeof(KlyDateTile), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Mode"/>.</summary>
    public static readonly BindableProperty ModeProperty = BindableProperty.Create(nameof(Mode), typeof(KlyDateTileMode), typeof(KlyDateTile), KlyDateTileMode.Date, propertyChanged: Update);
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyDateTile), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyDateTile), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="Now"/>.</summary>
    public static readonly BindableProperty NowProperty = BindableProperty.Create(nameof(Now), typeof(DateTimeOffset?), typeof(KlyDateTile), null, propertyChanged: Update);
    /// <summary>Identifies <see cref="TodayText"/>.</summary>
    public static readonly BindableProperty TodayTextProperty = BindableProperty.Create(nameof(TodayText), typeof(string), typeof(KlyDateTile), "Today", propertyChanged: Update);
    /// <summary>Identifies <see cref="TomorrowText"/>.</summary>
    public static readonly BindableProperty TomorrowTextProperty = BindableProperty.Create(nameof(TomorrowText), typeof(string), typeof(KlyDateTile), "Tomorrow", propertyChanged: Update);

    private readonly Label _month = DisplayParts.NewLabel(10, DisplayParts.PrimaryText, FontAttributes.Bold, LineBreakMode.NoWrap);
    private readonly Label _day = DisplayParts.NewLabel(16, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
    private readonly Label _title = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
    private readonly Label _description = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
    private Func<int, string> _inDays = n => $"In {n} days";
    private Func<int, string> _daysOverdue = n => n == 1 ? "1 day overdue" : $"{n} days overdue";
    private Func<int, string> _dayCount = n => n == 1 ? "1 day" : $"{n} days";

    /// <summary>Creates a date tile showing today.</summary>
    public KlyDateTile() : base(KlySurface.Plain)
    {
        var tile = new Border { WidthRequest = 48, HeightRequest = 48, StrokeThickness = 1, Padding = 0, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Start };
        tile.SetRadius(KlyTheme.RadiusOf(1));
        tile.SetStroke(KlyTheme.Border);
        tile.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);
        _month.HorizontalTextAlignment = TextAlignment.Center;
        _month.TextTransform = TextTransform.Uppercase;
        _day.HorizontalTextAlignment = TextAlignment.Center;
        tile.Content = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { _month, _day } };
        // The calendar is a picture of the text beside it.
        AutomationProperties.SetIsInAccessibleTree(tile, false);

        var text = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center, Children = { _title, _caption, _description } };
        var row = new HorizontalStackLayout { Spacing = 12, Children = { tile, text } };
        Body.Add(row);
        Update();
    }

    /// <summary>The date, or the start of a range. The current date when not set.</summary>
    public DateTimeOffset? Value { get => (DateTimeOffset?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The end of a <see cref="KlyDateTileMode.Range"/>, inclusive in the day count.</summary>
    public DateTimeOffset? EndValue { get => (DateTimeOffset?)GetValue(EndValueProperty); set => SetValue(EndValueProperty, value); }

    /// <summary>What the tile says beside the calendar: the date, the days until it, or a range.</summary>
    public KlyDateTileMode Mode { get => (KlyDateTileMode)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    /// <summary>The title. In the default mode it replaces the formatted date; in the countdown and range modes it is a caption under the text.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>A line of smaller text under the title.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The reference time of a <see cref="KlyDateTileMode.Countdown"/>. <see cref="DateTimeOffset.UtcNow"/> when not set. Pass it for tests and for stable screenshots.</summary>
    public DateTimeOffset? Now { get => (DateTimeOffset?)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    /// <summary>The countdown text for the current day ("Today"), in the language of the app.</summary>
    public string TodayText { get => (string)GetValue(TodayTextProperty); set => SetValue(TodayTextProperty, value); }

    /// <summary>The countdown text for the next day ("Tomorrow"), in the language of the app.</summary>
    public string TomorrowText { get => (string)GetValue(TomorrowTextProperty); set => SetValue(TomorrowTextProperty, value); }

    /// <summary>The countdown text for a date two days away or more, given the number of days (<c>"In 5 days"</c>), in the language of the app.</summary>
    public Func<int, string> InDays
    {
        get => _inDays;
        set
        {
            _inDays = value;
            Update();
        }
    }

    /// <summary>The countdown text for a past date, given the number of days (<c>"3 days overdue"</c>), in the language of the app.</summary>
    public Func<int, string> DaysOverdue
    {
        get => _daysOverdue;
        set
        {
            _daysOverdue = value;
            Update();
        }
    }

    /// <summary>The length of a range, given the number of days (<c>"7 days"</c>), in the language of the app.</summary>
    public Func<int, string> DayCount
    {
        get => _dayCount;
        set
        {
            _dayCount = value;
            Update();
        }
    }

    internal string TitleText => _title.Text;

    internal string MonthText => _month.Text;

    internal string DayText => _day.Text;

    private static void Update(BindableObject bindable, object oldValue, object newValue) => ((KlyDateTile)bindable).Update();

    private void Update()
    {
        var culture = CultureInfo.CurrentCulture;
        var date = Value ?? DateTimeOffset.UtcNow;
        var now = Now ?? DateTimeOffset.UtcNow;
        var days = (int)Math.Round((date.Date - now.Date).TotalDays);
        var countdown = Mode == KlyDateTileMode.Countdown;
        var range = Mode == KlyDateTileMode.Range && EndValue is not null;

        _month.Text = date.ToString("MMM", culture);
        _day.Text = date.Day.ToString(culture);

        if (countdown)
        {
            _title.Text = days == 0 ? TodayText : days == 1 ? TomorrowText : days > 1 ? InDays(days) : DaysOverdue(Math.Abs(days));
        }
        else if (range)
        {
            var end = EndValue!.Value;
            var span = Math.Max(1, (int)Math.Round((end.Date - date.Date).TotalDays) + 1);
            _title.Text = $"{date.ToString("yyyy-MM-dd", culture)} → {end.ToString("yyyy-MM-dd", culture)} ({DayCount(span)})";
        }
        else
        {
            _title.Text = Title ?? date.ToString("D", culture);
        }

        DisplayParts.Show(_caption, (countdown || range) ? Title : null);
        DisplayParts.Show(_description, Description);
    }
}
