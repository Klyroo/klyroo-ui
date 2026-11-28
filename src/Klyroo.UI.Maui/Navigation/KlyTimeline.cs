using System.Globalization;
using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// A list of events. The default layout draws them along a line, the pipeline, log and schedule layouts draw the same events differently.
/// A <c>Max</c> shows the first events and a "Show more" button for the rest.
/// </summary>
public class KlyTimeline : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyTimelineItem>), typeof(KlyTimeline), Array.Empty<KlyTimelineItem>(), propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Active"/>.</summary>
    public static readonly BindableProperty ActiveProperty = BindableProperty.Create(nameof(Active), typeof(int?), typeof(KlyTimeline), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(StackOrientation), typeof(KlyTimeline), StackOrientation.Vertical, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Layout"/>.</summary>
    public static readonly BindableProperty LayoutProperty = BindableProperty.Create(nameof(Layout), typeof(KlyTimelineLayout), typeof(KlyTimeline), KlyTimelineLayout.Default, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowNumbers"/>.</summary>
    public static readonly BindableProperty ShowNumbersProperty = BindableProperty.Create(nameof(ShowNumbers), typeof(bool), typeof(KlyTimeline), false, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="DayHeadingLevel"/>.</summary>
    public static readonly BindableProperty DayHeadingLevelProperty = BindableProperty.Create(nameof(DayHeadingLevel), typeof(int?), typeof(KlyTimeline), 4, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyTimeline), null, propertyChanged: (b, _, _) => { ((KlyTimeline)b)._reveal.Sync(((KlyTimeline)b).Max); ((KlyTimeline)b).Rebuild(); });
    /// <summary>Identifies <see cref="Step"/>.</summary>
    public static readonly BindableProperty StepProperty = BindableProperty.Create(nameof(Step), typeof(int?), typeof(KlyTimeline), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Now"/>.</summary>
    public static readonly BindableProperty NowProperty = BindableProperty.Create(nameof(Now), typeof(DateTimeOffset?), typeof(KlyTimeline), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Culture"/>.</summary>
    public static readonly BindableProperty CultureProperty = BindableProperty.Create(nameof(Culture), typeof(CultureInfo), typeof(KlyTimeline), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyTimeline), KlyColor.Primary, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyTimeline), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyTimeline), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyTimeline)b, (string?)v));
    /// <summary>Identifies <see cref="ShowMoreLabel"/>.</summary>
    public static readonly BindableProperty ShowMoreLabelProperty = BindableProperty.Create(nameof(ShowMoreLabel), typeof(string), typeof(KlyTimeline), "Show more · {0}", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="TodayLabel"/>.</summary>
    public static readonly BindableProperty TodayLabelProperty = BindableProperty.Create(nameof(TodayLabel), typeof(string), typeof(KlyTimeline), "Today", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="TomorrowLabel"/>.</summary>
    public static readonly BindableProperty TomorrowLabelProperty = BindableProperty.Create(nameof(TomorrowLabel), typeof(string), typeof(KlyTimeline), "Tomorrow", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AllDayLabel"/>.</summary>
    public static readonly BindableProperty AllDayLabelProperty = BindableProperty.Create(nameof(AllDayLabel), typeof(string), typeof(KlyTimeline), "All day", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="UnscheduledLabel"/>.</summary>
    public static readonly BindableProperty UnscheduledLabelProperty = BindableProperty.Create(nameof(UnscheduledLabel), typeof(string), typeof(KlyTimeline), "Unscheduled", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="DayCountLabel"/>.</summary>
    public static readonly BindableProperty DayCountLabelProperty = BindableProperty.Create(nameof(DayCountLabel), typeof(string), typeof(KlyTimeline), "{0} days", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="HourLabel"/>.</summary>
    public static readonly BindableProperty HourLabelProperty = BindableProperty.Create(nameof(HourLabel), typeof(string), typeof(KlyTimeline), "h", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="MinuteLabel"/>.</summary>
    public static readonly BindableProperty MinuteLabelProperty = BindableProperty.Create(nameof(MinuteLabel), typeof(string), typeof(KlyTimeline), "min", propertyChanged: Rebuild);

    private readonly NavigationReveal _reveal = new();

    /// <summary>Creates a timeline.</summary>
    public KlyTimeline()
    {
        Spacing = 0;
        Rebuild();
    }

    /// <summary>The events, as <see cref="KlyTimelineItem"/>.</summary>
    public IReadOnlyList<KlyTimelineItem> Items { get => (IReadOnlyList<KlyTimelineItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Index of the last highlighted item in the default layout. Null highlights none. An item with a <see cref="KlyTimelineItem.State"/> uses it instead.</summary>
    public int? Active { get => (int?)GetValue(ActiveProperty); set => SetValue(ActiveProperty, value); }

    /// <summary>Direction of the default layout: <c>Horizontal</c> or <c>Vertical</c>.</summary>
    public StackOrientation Orientation { get => (StackOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The layout: <see cref="KlyTimelineLayout.Default"/> (along a line), <see cref="KlyTimelineLayout.Pipeline"/>, <see cref="KlyTimelineLayout.Log"/> or <see cref="KlyTimelineLayout.Schedule"/>.</summary>
    public new KlyTimelineLayout Layout { get => (KlyTimelineLayout)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Numbers the markers of the default and pipeline layouts (1, 2, 3...) when an event has no icon.</summary>
    public bool ShowNumbers { get => (bool)GetValue(ShowNumbersProperty); set => SetValue(ShowNumbersProperty, value); }

    /// <summary>Heading level (1 to 6) of the day titles of the schedule layout, for the screen reader outline. Null makes them plain text. Defaults to 4.</summary>
    public int? DayHeadingLevel { get => (int?)GetValue(DayHeadingLevelProperty); set => SetValue(DayHeadingLevelProperty, value); }

    /// <summary>The number of events shown before a Show more button (all events when <see langword="null"/>). In the schedule layout the events are sorted first, so Show more reveals later events.</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The number of events each tap on Show more reveals (all the remaining ones when <see langword="null"/>).</summary>
    public int? Step { get => (int?)GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>The reference time of the schedule layout, which names the days "Today" and "Tomorrow". <see cref="DateTimeOffset.UtcNow"/> when not set.</summary>
    public DateTimeOffset? Now { get => (DateTimeOffset?)GetValue(NowProperty); set => SetValue(NowProperty, value); }

    /// <summary>The culture that formats dates and times. <see cref="CultureInfo.CurrentCulture"/> when not set.</summary>
    public CultureInfo? Culture { get => (CultureInfo?)GetValue(CultureProperty); set => SetValue(CultureProperty, value); }

    /// <summary>Semantic color of the markers.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the markers, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the list.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>Text of the Show more button; <c>{0}</c> is the number of events the tap reveals.</summary>
    public string ShowMoreLabel { get => (string)GetValue(ShowMoreLabelProperty); set => SetValue(ShowMoreLabelProperty, value); }

    /// <summary>Name of the current day in the schedule layout.</summary>
    public string TodayLabel { get => (string)GetValue(TodayLabelProperty); set => SetValue(TodayLabelProperty, value); }

    /// <summary>Name of the next day in the schedule layout.</summary>
    public string TomorrowLabel { get => (string)GetValue(TomorrowLabelProperty); set => SetValue(TomorrowLabelProperty, value); }

    /// <summary>The time of an event that lasts whole days in the schedule layout.</summary>
    public string AllDayLabel { get => (string)GetValue(AllDayLabelProperty); set => SetValue(AllDayLabelProperty, value); }

    /// <summary>The heading of the events without a date in the schedule layout.</summary>
    public string UnscheduledLabel { get => (string)GetValue(UnscheduledLabelProperty); set => SetValue(UnscheduledLabelProperty, value); }

    /// <summary>The length of an all-day event of several days in the schedule layout; <c>{0}</c> is the number of days.</summary>
    public string DayCountLabel { get => (string)GetValue(DayCountLabelProperty); set => SetValue(DayCountLabelProperty, value); }

    /// <summary>The abbreviation of hours in the length of an event.</summary>
    public string HourLabel { get => (string)GetValue(HourLabelProperty); set => SetValue(HourLabelProperty, value); }

    /// <summary>The abbreviation of minutes in the length of an event.</summary>
    public string MinuteLabel { get => (string)GetValue(MinuteLabelProperty); set => SetValue(MinuteLabelProperty, value); }

    private CultureInfo FormatCulture => Culture ?? CultureInfo.CurrentCulture;

    /// <summary>The diameter of the marker and its font size: <c>size-5</c> to <c>size-9</c> on the web.</summary>
    internal static (double Diameter, double Font) MetricsOf(KlySize size) => size switch
    {
        KlySize.Xs => (20d, 12d),
        KlySize.Sm => (24d, 12d),
        KlySize.Md => (28d, 14d),
        KlySize.Lg => (32d, 14d),
        KlySize.Xl => (36d, 16d),
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    /// <summary>The state of an event in the default layout: <c>done</c>, <c>current</c>, <c>upcoming</c> from its own state, else <c>active</c> or <c>inactive</c> from <see cref="Active"/>.</summary>
    internal string StateOf(KlyTimelineItem item, int index) =>
        item.State is { } state ? StateName(state) : Active is { } active && index <= active ? "active" : "inactive";

    internal static string StateName(KlyTimelineState state) => state switch
    {
        KlyTimelineState.Done => "done",
        KlyTimelineState.Current => "current",
        _ => "upcoming",
    };

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyTimeline)bindable).Rebuild();

    private void Rebuild()
    {
        _reveal.Sync(Max);
        Children.Clear();
        More = null;
        SemanticProperties.SetDescription(this, AccessibleName);
        switch (Layout)
        {
            case KlyTimelineLayout.Schedule:
                BuildSchedule();
                break;
            case KlyTimelineLayout.Log:
                BuildLog();
                break;
            case KlyTimelineLayout.Pipeline:
                BuildPipeline();
                break;
            default:
                BuildDefault();
                break;
        }
    }

    // ----- default and pipeline layouts -----

    private (KlyThemeColor Border, KlyThemeColor Fill, KlyThemeColor Text) MarkerColors(KlyTimelineItem item, string state)
    {
        var set = KlyTheme.Resolve(item.Color ?? Color);
        return state switch
        {
            "active" or "done" => (set.Text, set.Fill, set.Contrast),
            "current" => (set.Text, KlyTheme.Background, set.Text),
            _ => (KlyTheme.BorderAccented, KlyTheme.Background, KlyTheme.TextMuted),
        };
    }

    private View Marker(KlyTimelineItem item, string state, int index)
    {
        var (diameter, _) = MetricsOf(Size);
        var (border, fill, text) = MarkerColors(item, state);
        var marker = new Border { WidthRequest = diameter, HeightRequest = diameter, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
        marker.SetRadius(diameter / 2);
        marker.Apply(new Surface(fill, border, 2, text));
        if (item.Icon is { } icon)
        {
            marker.Content = NavigationLayout.Icon(icon, diameter >= 32 ? KlySize.Md : KlySize.Sm, text);
        }
        else if (ShowNumbers)
        {
            var number = NavigationLayout.TextLabel(11, text, FontAttributes.Bold, LineBreakMode.NoWrap);
            number.Text = (index + 1).ToString(CultureInfo.CurrentCulture);
            number.HorizontalTextAlignment = TextAlignment.Center;
            AutomationProperties.SetIsInAccessibleTree(number, false);
            marker.Content = number;
        }
        AutomationProperties.SetIsInAccessibleTree(marker, false);
        return marker;
    }

    private string? TimeText(KlyTimelineItem item) => !string.IsNullOrEmpty(item.Date) ? item.Date : item.At?.ToString("g", FormatCulture);

    private View Text(KlyTimelineItem item)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        if (TimeText(item) is { Length: > 0 } time)
        {
            var date = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            date.Text = time;
            stack.Children.Add(date);
        }
        var title = NavigationLayout.TextLabel(14, KlyTheme.Text, lineBreak: LineBreakMode.WordWrap);
        title.Text = item.Title;
        stack.Children.Add(title);
        if (item.Description is not null)
        {
            var description = NavigationLayout.TextLabel(14, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            description.Text = item.Description;
            stack.Children.Add(description);
        }
        if (!string.IsNullOrEmpty(item.Actor))
        {
            var actor = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            actor.Text = item.Actor;
            stack.Children.Add(actor);
        }
        return stack;
    }

    private void BuildDefault()
    {
        var (visible, remaining) = _reveal.Apply(Items, Max, Step);
        var entries = new List<NavigationTrackEntry>();
        for (var i = 0; i < visible.Count; i++)
        {
            var item = visible[i];
            var state = StateOf(item, i);
            var set = KlyTheme.Resolve(item.Color ?? Color);
            var line = state is "active" or "done" ? set.Text : KlyTheme.BorderAccented;
            entries.Add(new NavigationTrackEntry(Marker(item, state, i), line, Text(item)));
        }
        if (entries.Count > 0) Children.Add(NavigationTrack.Build(entries, Layout == KlyTimelineLayout.Default && Orientation == StackOrientation.Vertical));
        AddMore(remaining);
    }

    private void BuildPipeline()
    {
        var (visible, remaining) = _reveal.Apply(Items, Max, Step);
        var flex = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Start };
        for (var i = 0; i < visible.Count; i++)
        {
            var item = visible[i];
            var state = StateName(item.State ?? KlyTimelineState.Done);
            var entry = new HorizontalStackLayout { Spacing = 12, Margin = new Thickness(0, 0, 16, 8) };
            entry.Children.Add(Marker(item, state, i));
            entry.Children.Add(Text(item));
            flex.Children.Add(entry);
        }
        if (visible.Count > 0) Children.Add(flex);
        AddMore(remaining);
    }

    // ----- log layout -----

    // The log line shows the date and the time of each event: from At, with Date replacing the time part when given.
    private string LogTimestamp(KlyTimelineItem item)
    {
        if (item.At is { } at)
        {
            var time = !string.IsNullOrEmpty(item.Date) ? item.Date : at.ToString("t", FormatCulture);
            return $"{at.ToString("d", FormatCulture)} {time}";
        }
        return item.Date ?? "";
    }

    private void BuildLog()
    {
        var (visible, remaining) = _reveal.Apply(Items, Max, Step);
        var list = new VerticalStackLayout { Spacing = 0 };
        for (var i = 0; i < visible.Count; i++)
        {
            var item = visible[i];
            var set = item.Color is { } color ? KlyTheme.Resolve(color) : (KlyColorSet?)null;
            var markerColor = set?.Text ?? KlyTheme.TextMuted;

            var row = new Grid
            {
                ColumnSpacing = 12,
                Padding = new Thickness(0, 6),
                ColumnDefinitions = { new ColumnDefinition(new GridLength(128)), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) },
            };
            var timestamp = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
            timestamp.Text = LogTimestamp(item);
            row.Add(timestamp, 0);

            View marker;
            if (item.Icon is { } icon)
            {
                marker = NavigationLayout.Icon(icon, KlySize.Sm, markerColor);
            }
            else
            {
                var dot = new BoxView { WidthRequest = 6, HeightRequest = 6, CornerRadius = 3, VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.Center };
                dot.SetThemeColor(BoxView.ColorProperty, markerColor);
                marker = dot;
            }
            marker.VerticalOptions = LayoutOptions.Center;
            AutomationProperties.SetIsInAccessibleTree(marker, false);
            row.Add(marker, 1);

            var body = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center };
            var title = NavigationLayout.TextLabel(12, KlyTheme.TextHighlighted, lineBreak: LineBreakMode.WordWrap);
            title.Text = item.Title;
            title.Margin = new Thickness(0, 0, 8, 0);
            body.Children.Add(title);
            if (!string.IsNullOrEmpty(item.Description))
            {
                var description = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
                description.Text = item.Description;
                description.Margin = new Thickness(0, 0, 8, 0);
                body.Children.Add(description);
            }
            if (!string.IsNullOrEmpty(item.Actor))
            {
                var actor = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
                actor.Text = item.Actor;
                body.Children.Add(actor);
            }
            row.Add(body, 2);

            if (i > 0) list.Children.Add(NavigationLayout.Line(KlyTheme.Border, height: 1));
            list.Children.Add(row);
        }
        if (visible.Count > 0) Children.Add(list);
        AddMore(remaining);
    }

    // ----- schedule layout -----

    // "Today" and "Tomorrow" come first when the day is one of them, then the long date of the culture.
    private string DayTitle(TimelineScheduleDay day, DateTimeOffset reference)
    {
        var sample = day.Sample!.Value;
        var longDate = sample.ToString("D", FormatCulture);
        var referenceDay = DateOnly.FromDateTime(reference.ToOffset(sample.Offset).DateTime);
        if (day.Day == referenceDay) return $"{TodayLabel} · {longDate}";
        if (day.Day == referenceDay.AddDays(1)) return $"{TomorrowLabel} · {longDate}";
        return longDate;
    }

    // The time text wins when given. Otherwise: the all-day label, or the start with the end (and the end date when it is on another day).
    private string ScheduleTimeText(TimelineScheduleEvent e)
    {
        if (!string.IsNullOrEmpty(e.Item.Date)) return e.Item.Date;
        if (e.Start is not { } start) return "";
        if (e.AllDay) return AllDayLabel;
        var from = start.ToString("t", FormatCulture);
        if (e.End is not { } end) return from;
        var to = end.Date == start.Date ? end.ToString("t", FormatCulture) : $"{end.ToString("d", FormatCulture)} {end.ToString("t", FormatCulture)}";
        return $"{from} – {to}";
    }

    private string ScheduleDurationText(TimelineScheduleEvent e)
    {
        if (e.AllDay) return e.Days > 1 ? string.Format(FormatCulture, DayCountLabel, e.Days) : "";
        return e.Minutes is { } minutes ? NavigationTimelineSchedule.Duration(minutes, HourLabel, MinuteLabel) : "";
    }

    private void BuildSchedule()
    {
        var sorted = NavigationTimelineSchedule.Sort(Items);
        var (visible, remaining) = _reveal.Apply(sorted, Max, Step);
        var days = NavigationTimelineSchedule.Group(visible);
        var reference = Now ?? DateTimeOffset.UtcNow;
        var hasDatedDays = days.Any(d => d.Day is not null);

        var root = new VerticalStackLayout { Spacing = 16 };
        foreach (var day in days)
        {
            var section = new VerticalStackLayout { Spacing = 6 };
            if (day.Day is not null)
            {
                section.Children.Add(DayTitleLabel(DayTitle(day, reference), KlyTheme.TextHighlighted));
            }
            else if (hasDatedDays)
            {
                section.Children.Add(DayTitleLabel(UnscheduledLabel, KlyTheme.TextMuted));
            }
            var list = new VerticalStackLayout { Spacing = 4 };
            foreach (var e in day.Events) list.Children.Add(ScheduleEvent(e));
            section.Children.Add(list);
            root.Children.Add(section);
        }
        if (days.Count > 0) Children.Add(root);
        AddMore(remaining);
    }

    private Label DayTitleLabel(string text, KlyThemeColor color)
    {
        var label = NavigationLayout.TextLabel(12, color, FontAttributes.Bold, LineBreakMode.WordWrap);
        label.Text = text;
        SemanticProperties.SetHeadingLevel(label, NavigationLayout.HeadingLevelOf(DayHeadingLevel));
        return label;
    }

    private View ScheduleEvent(TimelineScheduleEvent e)
    {
        var item = e.Item;
        var state = item.State;
        var toned = item.Color is not null;
        var set = KlyTheme.Resolve(item.Color ?? Color);

        // The left edge says where the event stands: the primary color for the current one, the border colors for the others, the color of the event when it has one.
        var (edge, background, opacity) = (state, toned) switch
        {
            (KlyTimelineState.Current, false) => (KlyTheme.Resolve(KlyColor.Primary).Text, (KlyThemeColor?)KlyTheme.BackgroundMuted, 1d),
            (KlyTimelineState.Current, true) => (set.Text, KlyTheme.BackgroundMuted, 1d),
            (KlyTimelineState.Done, false) => (KlyTheme.Border, null, 1d),
            (KlyTimelineState.Done, true) => (set.Text, null, 0.7),
            (_, false) => (KlyTheme.BorderAccented, null, 1d),
            _ => (set.Text, null, 1d),
        };

        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(new GridLength(2)), new ColumnDefinition(GridLength.Star) }, Opacity = opacity };
        if (background is { } fill) row.SetThemeColor(BackgroundColorProperty, fill);
        row.Add(NavigationLayout.Line(edge), 0);

        var content = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(12, 6, 0, 6) };

        var times = new FlexLayout { Direction = FlexDirection.Row, Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center };
        var timeText = ScheduleTimeText(e);
        if (!string.IsNullOrEmpty(timeText))
        {
            var time = NavigationLayout.TextLabel(12, KlyTheme.Text, lineBreak: LineBreakMode.WordWrap);
            time.Text = timeText;
            time.Margin = new Thickness(0, 0, 8, 0);
            times.Children.Add(time);
        }
        var durationText = ScheduleDurationText(e);
        if (!string.IsNullOrEmpty(durationText))
        {
            var duration = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            duration.Text = durationText;
            times.Children.Add(duration);
        }
        if (times.Children.Count > 0) content.Children.Add(times);

        var header = new HorizontalStackLayout { Spacing = 6 };
        if (item.Icon is { } icon) header.Children.Add(NavigationLayout.Icon(icon, KlySize.Sm, toned ? set.Text : KlyTheme.TextMuted));
        var titleColor = state == KlyTimelineState.Done ? KlyTheme.TextMuted : KlyTheme.TextHighlighted;
        var title = NavigationLayout.TextLabel(14, titleColor, state == KlyTimelineState.Current ? FontAttributes.Bold : FontAttributes.None, LineBreakMode.WordWrap);
        title.Text = item.Title;
        header.Children.Add(title);
        content.Children.Add(header);

        if (!string.IsNullOrEmpty(item.Description))
        {
            var description = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            description.Text = item.Description;
            content.Children.Add(description);
        }
        if (!string.IsNullOrEmpty(item.Actor))
        {
            var actor = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.WordWrap);
            actor.Text = item.Actor;
            content.Children.Add(actor);
        }
        row.Add(content, 1);
        return row;
    }

    // ----- show more -----

    private void AddMore(int remaining)
    {
        if (remaining <= 0) return;
        var button = new NavigationButton { HorizontalOptions = LayoutOptions.Start };
        var row = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var label = NavigationLayout.TextLabel(12, KlyTheme.TextMuted, lineBreak: LineBreakMode.NoWrap);
        label.Text = string.Format(CultureInfo.CurrentCulture, ShowMoreLabel, remaining);
        row.Children.Add(label);
        row.Children.Add(NavigationLayout.Icon(KlyIconNames.ChevronDown, KlySize.Sm, KlyTheme.TextMuted));
        button.Face.Content = row;
        button.Face.Padding = new Thickness(8, 4);
        button.Face.SetRadius(KlyTheme.RadiusOf(1));
        SemanticProperties.SetDescription(button, label.Text);
        button.Clicked += (_, _) =>
        {
            _reveal.More(Items.Count, Max, Step);
            Rebuild();
        };
        More = button;
        Children.Add(button);
    }

    internal NavigationButton? More { get; private set; }
}
