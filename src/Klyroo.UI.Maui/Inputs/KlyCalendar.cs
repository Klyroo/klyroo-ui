using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A month calendar to pick a single date, several dates or a range, with localized month and day names. It is laid out as a grid of days under a header with the month and the buttons
/// to go to the previous and the next month. <see cref="Mode"/> says what is selected: <see cref="Value"/>, <see cref="Values"/> or <see cref="Range"/>.
/// </summary>
public class KlyCalendar : VerticalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Mode"/>.</summary>
    public static readonly BindableProperty ModeProperty = BindableProperty.Create(nameof(Mode), typeof(KlyCalendarMode), typeof(KlyCalendar), KlyCalendarMode.Single, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(DateOnly?), typeof(KlyCalendar), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyCalendar)b).ShowValue((DateOnly?)o, (DateOnly?)v));
    /// <summary>Identifies <see cref="Values"/>.</summary>
    public static readonly BindableProperty ValuesProperty = BindableProperty.Create(nameof(Values), typeof(IReadOnlyList<DateOnly>), typeof(KlyCalendar), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyCalendar)b).ShowValues((IReadOnlyList<DateOnly>?)o, (IReadOnlyList<DateOnly>?)v));
    /// <summary>Identifies <see cref="Range"/>.</summary>
    public static readonly BindableProperty RangeProperty = BindableProperty.Create(nameof(Range), typeof(KlyDateRange), typeof(KlyCalendar), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyCalendar)b).ShowRange((KlyDateRange?)o, (KlyDateRange?)v));
    /// <summary>Identifies <see cref="Min"/>.</summary>
    public static readonly BindableProperty MinProperty = BindableProperty.Create(nameof(Min), typeof(DateOnly?), typeof(KlyCalendar), null, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(DateOnly?), typeof(KlyCalendar), null, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="DisabledDates"/>.</summary>
    public static readonly BindableProperty DisabledDatesProperty = BindableProperty.Create(nameof(DisabledDates), typeof(IEnumerable<DateOnly>), typeof(KlyCalendar), null, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="WeekStart"/>.</summary>
    public static readonly BindableProperty WeekStartProperty = BindableProperty.Create(nameof(WeekStart), typeof(DayOfWeek?), typeof(KlyCalendar), null, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyCalendar), null, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="DisplayedMonth"/>.</summary>
    public static readonly BindableProperty DisplayedMonthProperty = BindableProperty.Create(nameof(DisplayedMonth), typeof(DateOnly), typeof(KlyCalendar), FirstOfMonth(DateOnly.FromDateTime(DateTime.Today)), BindingMode.TwoWay, coerceValue: (_, v) => FirstOfMonth((DateOnly)v), propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyCalendar), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyCalendar), KlySize.Md, propertyChanged: (b, _, _) => ((KlyCalendar)b).Rebuild());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyCalendar), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyCalendar)b, (string?)v));

    private readonly InputsFieldTaken _taken = new();
    private readonly List<InputsCalendarDay> _days = [];
    private InputsIconButton? _previous;
    private InputsIconButton? _next;
    private Label? _heading;

    /// <summary>Creates a calendar that shows the current month.</summary>
    public KlyCalendar()
    {
        HorizontalOptions = LayoutOptions.Start;
        Spacing = 8;
        Rebuild();
    }

    /// <summary>Raised when <see cref="Value"/> changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<DateOnly?>>? ValueChanged;

    /// <summary>Raised when <see cref="Values"/> changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<IReadOnlyList<DateOnly>>>? ValuesChanged;

    /// <summary>Raised when <see cref="Range"/> changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<KlyDateRange>>? RangeChanged;

    /// <summary>What the calendar selects. <c>Single</c> binds <see cref="Value"/>, <c>Multiple</c> binds <see cref="Values"/>, <c>Range</c> binds <see cref="Range"/>.</summary>
    public KlyCalendarMode Mode { get => (KlyCalendarMode)GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    /// <summary>The selected date in <c>Single</c> mode. Two-way bindable.</summary>
    public DateOnly? Value { get => (DateOnly?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The selected dates in <c>Multiple</c> mode, in order. Two-way bindable.</summary>
    public IReadOnlyList<DateOnly>? Values { get => (IReadOnlyList<DateOnly>?)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }

    /// <summary>The selected range in <c>Range</c> mode. Two-way bindable.</summary>
    public KlyDateRange? Range { get => (KlyDateRange?)GetValue(RangeProperty); set => SetValue(RangeProperty, value); }

    /// <summary>Earliest selectable date.</summary>
    public DateOnly? Min { get => (DateOnly?)GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Latest selectable date.</summary>
    public DateOnly? Max { get => (DateOnly?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Dates that cannot be selected, besides those outside <see cref="Min"/> and <see cref="Max"/>.</summary>
    public IEnumerable<DateOnly>? DisabledDates { get => (IEnumerable<DateOnly>?)GetValue(DisabledDatesProperty); set => SetValue(DisabledDatesProperty, value); }

    /// <summary>First column of the grid. Defaults to the first day of the week of <see cref="Culture"/>.</summary>
    public DayOfWeek? WeekStart { get => (DayOfWeek?)GetValue(WeekStartProperty); set => SetValue(WeekStartProperty, value); }

    /// <summary>Source of the month and day names and of the first day of the week. Defaults to the current culture.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>The month the grid shows, as its first day. It follows the buttons, and moves to the month of the first selected date when the selection changes by code. Two-way bindable.</summary>
    public DateOnly DisplayedMonth { get => (DateOnly)GetValue(DisplayedMonthProperty); set => SetValue(DisplayedMonthProperty, value); }

    /// <summary>Semantic color of the selected days and of today.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the days, from <c>Xs</c> (28) to <c>Xl</c> (44).</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the calendar, such as its label.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<InputsCalendarDay> Days => _days;

    internal InputsIconButton? PreviousButton => _previous;

    internal InputsIconButton? NextButton => _next;

    internal Label? Heading => _heading;

    internal static DateOnly FirstOfMonth(DateOnly date) => new(date.Year, date.Month, 1);

    /// <summary>Goes to the previous month.</summary>
    public void ShowPreviousMonth() => DisplayedMonth = DisplayedMonth.AddMonths(-1);

    /// <summary>Goes to the next month.</summary>
    public void ShowNextMonth() => DisplayedMonth = DisplayedMonth.AddMonths(1);

    /// <summary>The dates of a mode: the ones that show as selected.</summary>
    internal IEnumerable<DateOnly> Selection() => Mode switch
    {
        KlyCalendarMode.Multiple => Values ?? [],
        KlyCalendarMode.Range => Range is { } range ? new[] { range.Start, range.End }.OfType<DateOnly>() : [],
        _ => Value is { } value ? [value] : [],
    };

    /// <summary>A date chosen in the grid: sets the value of the mode.</summary>
    internal void Pick(DateOnly date)
    {
        switch (Mode)
        {
            case KlyCalendarMode.Multiple:
                var current = Values ?? [];
                Values = [.. (current.Contains(date) ? current.Where(d => d != date) : [.. current, date]).Order()];
                break;
            case KlyCalendarMode.Range:
                // The first date starts a range, the second ends it (the earlier of the two first), the third starts another.
                var range = Range;
                if (range?.Start is not { } start || range.End is not null) Range = new KlyDateRange(date, null);
                else Range = date < start ? new KlyDateRange(date, start) : new KlyDateRange(start, date);
                break;
            default:
                if (Value != date) Value = date;
                break;
        }
    }

    internal static double DimensionOf(KlySize size) => size switch
    {
        KlySize.Xs => 28,
        KlySize.Sm => 32,
        KlySize.Md => 36,
        KlySize.Lg => 40,
        KlySize.Xl => 44,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    internal static double DayFontOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm => 12,
        KlySize.Md or KlySize.Lg => 14,
        KlySize.Xl => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    internal static double WeekdayFontOf(KlySize size) => size switch
    {
        KlySize.Xs or KlySize.Sm or KlySize.Md => 12,
        KlySize.Lg or KlySize.Xl => 14,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web calendar buttons: aria-disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            Opacity = IsEnabled ? 1 : 0.6;
            Rebuild();
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private CultureInfo CultureOrCurrent => Culture ?? CultureInfo.CurrentCulture;

    private bool IsDisabled(DateOnly date) => (Min is { } min && date < min) || (Max is { } max && date > max) || DisabledDates?.Contains(date) == true;

    private void ShowValue(DateOnly? old, DateOnly? value)
    {
        Reveal(value);
        if (Mode == KlyCalendarMode.Single) Rebuild();
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<DateOnly?>(old, value));
    }

    private void ShowValues(IReadOnlyList<DateOnly>? old, IReadOnlyList<DateOnly>? values)
    {
        Reveal(values?.Count > 0 ? values[0] : null);
        if (Mode == KlyCalendarMode.Multiple) Rebuild();
        ValuesChanged?.Invoke(this, new KlyInputChangedEventArgs<IReadOnlyList<DateOnly>>(old, values));
    }

    private void ShowRange(KlyDateRange? old, KlyDateRange? range)
    {
        Reveal(range?.Start);
        if (Mode == KlyCalendarMode.Range) Rebuild();
        RangeChanged?.Invoke(this, new KlyInputChangedEventArgs<KlyDateRange>(old, range));
    }

    /// <summary>Shows the month of a date that was just selected, when the grid does not show it.</summary>
    private void Reveal(DateOnly? date)
    {
        if (date is { } day && FirstOfMonth(day) != DisplayedMonth) DisplayedMonth = day;
    }

    private void Rebuild()
    {
        Children.Clear();
        _days.Clear();
        var culture = CultureOrCurrent;
        var set = KlyTheme.Resolve(Color);
        var month = DisplayedMonth;
        var dimension = DimensionOf(Size);

        var header = new Grid { ColumnSpacing = 8 };
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _previous = new InputsIconButton(KlyIconNames.ChevronLeft, "Previous month", KlySize.Md) { Dimmed = 0.6, IsEnabled = IsEnabled && !(Min is { } min && month.AddDays(-1) < min) };
        _next = new InputsIconButton(KlyIconNames.ChevronRight, "Next month", KlySize.Md) { Dimmed = 0.6, IsEnabled = IsEnabled && !(Max is { } max && month.AddMonths(1) > max) };
        _previous.Clicked += (_, _) => ShowPreviousMonth();
        _next.Clicked += (_, _) => ShowNextMonth();
        _heading = new Label
        {
            Text = month.ToString("Y", culture),
            FontSize = DayFontOf(Size),
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        _heading.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        SemanticProperties.SetHeadingLevel(_heading, SemanticHeadingLevel.Level2);
        header.Add(_previous, 0);
        header.Add(_heading, 1);
        header.Add(_next, 2);
        Children.Add(header);

        var grid = new Grid { ColumnSpacing = 2, RowSpacing = 2, HorizontalOptions = LayoutOptions.Center };
        for (var column = 0; column < 7; column++) grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(dimension)));
        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

        var start = (int)(WeekStart ?? culture.DateTimeFormat.FirstDayOfWeek);
        for (var column = 0; column < 7; column++)
        {
            var index = (start + column) % 7;
            var weekday = new Label
            {
                Text = culture.DateTimeFormat.ShortestDayNames[index],
                FontSize = WeekdayFontOf(Size),
                HorizontalTextAlignment = TextAlignment.Center,
            };
            weekday.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
            SemanticProperties.SetDescription(weekday, culture.DateTimeFormat.DayNames[index]);
            grid.Add(weekday, column, 0);
        }

        var selection = Selection().ToList();
        var range = Mode == KlyCalendarMode.Range ? Range : null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var offset = ((int)month.DayOfWeek - start + 7) % 7;
        var days = DateTime.DaysInMonth(month.Year, month.Month);
        var rows = (offset + days + 6) / 7;
        for (var row = 0; row < rows; row++) grid.RowDefinitions.Add(new RowDefinition(new GridLength(dimension)));

        for (var day = 1; day <= days; day++)
        {
            var date = new DateOnly(month.Year, month.Month, day);
            var position = offset + day - 1;
            var selected = selection.Contains(date);
            var inRange = range is { Start: { } from, End: { } to } && date > from && date < to;
            var description = date.ToString("D", culture) + (selected ? ", selected" : string.Empty);
            var cell = new InputsCalendarDay(date, day.ToString(culture), description, selected, inRange, date == today, IsDisabled(date), set, dimension, DayFontOf(Size)) { IsEnabled = IsEnabled };
            cell.Picked += (_, picked) => Pick(picked);
            _days.Add(cell);
            grid.Add(cell, position % 7, (position / 7) + 1);
        }
        Children.Add(grid);
    }
}
