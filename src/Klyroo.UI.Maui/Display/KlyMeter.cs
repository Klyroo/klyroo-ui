using System.Globalization;
using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// A gauge in a known range: a bar, a ring or a scale of levels, with warning and error thresholds or stacked parts and a legend.
/// It is not a progress bar: the value is a measure (disk used, quota), not the completion of a task: see <see cref="KlyProgress"/> for that.
/// </summary>
public class KlyMeter : KlyBlockBase
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(double), typeof(KlyMeter), 0d, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(double), typeof(KlyMeter), 100d, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="WarningThreshold"/>.</summary>
    public static readonly BindableProperty WarningThresholdProperty = BindableProperty.Create(nameof(WarningThreshold), typeof(double?), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ErrorThreshold"/>.</summary>
    public static readonly BindableProperty ErrorThresholdProperty = BindableProperty.Create(nameof(ErrorThreshold), typeof(double?), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Parts"/>.</summary>
    public static readonly BindableProperty PartsProperty = BindableProperty.Create(nameof(Parts), typeof(IReadOnlyList<KlyMeterPart>), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Levels"/>.</summary>
    public static readonly BindableProperty LevelsProperty = BindableProperty.Create(nameof(Levels), typeof(IReadOnlyList<string>), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CurrentLevel"/>.</summary>
    public static readonly BindableProperty CurrentLevelProperty = BindableProperty.Create(nameof(CurrentLevel), typeof(int?), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Layout"/>.</summary>
    public static readonly BindableProperty LayoutProperty = BindableProperty.Create(nameof(Layout), typeof(KlyMeterLayout), typeof(KlyMeter), KlyMeterLayout.Bar, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ValueText"/>.</summary>
    public static readonly BindableProperty ValueTextProperty = BindableProperty.Create(nameof(ValueText), typeof(string), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyMeter), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="DefaultTitle"/>.</summary>
    public static readonly BindableProperty DefaultTitleProperty = BindableProperty.Create(nameof(DefaultTitle), typeof(string), typeof(KlyMeter), "Meter", propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CurrentLevelLabel"/>.</summary>
    public static readonly BindableProperty CurrentLevelLabelProperty = BindableProperty.Create(nameof(CurrentLevelLabel), typeof(string), typeof(KlyMeter), "Current", propertyChanged: Rebuild);

    private readonly VerticalStackLayout _stack = new() { Spacing = 8 };

    /// <summary>Creates a meter.</summary>
    public KlyMeter() : base(KlySurface.Plain, 8)
    {
        Body.Add(_stack);
        Rebuild();
    }

    /// <summary>The current value, clamped between 0 and <see cref="Max"/>.</summary>
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>The maximum (<c>100</c> by default). When it is zero or less, the sum of <see cref="Parts"/> is used.</summary>
    public double Max { get => (double)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>The percentage of <see cref="Max"/> from which the bar turns to the warning color.</summary>
    public double? WarningThreshold { get => (double?)GetValue(WarningThresholdProperty); set => SetValue(WarningThresholdProperty, value); }

    /// <summary>The percentage of <see cref="Max"/> from which the bar turns to the error color. It wins over <see cref="WarningThreshold"/>.</summary>
    public double? ErrorThreshold { get => (double?)GetValue(ErrorThresholdProperty); set => SetValue(ErrorThresholdProperty, value); }

    /// <summary>The segments, stacked in the bar or around the ring. When set, they replace the single value and the thresholds do not apply. A legend lists them when one of them has a <see cref="KlyMeterPart.ValueLabel"/>, an <see cref="KlyMeterPart.Icon"/> or a <see cref="KlyMeterPart.Max"/>.</summary>
    public IReadOnlyList<KlyMeterPart>? Parts { get => (IReadOnlyList<KlyMeterPart>?)GetValue(PartsProperty); set => SetValue(PartsProperty, value); }

    /// <summary>The level labels of a <see cref="KlyMeterLayout.Scale"/>.</summary>
    public IReadOnlyList<string>? Levels { get => (IReadOnlyList<string>?)GetValue(LevelsProperty); set => SetValue(LevelsProperty, value); }

    /// <summary>The zero-based index of the current level of a scale. The levels up to it are highlighted.</summary>
    public int? CurrentLevel { get => (int?)GetValue(CurrentLevelProperty); set => SetValue(CurrentLevelProperty, value); }

    /// <summary>The drawing: a bar, a ring with the value in its center, or a scale of discrete levels (a bar when there are no <see cref="Levels"/>).</summary>
    public new KlyMeterLayout Layout { get => (KlyMeterLayout)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>The label of the meter, and its accessible name (<see cref="DefaultTitle"/> when not set).</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The value as displayed, such as <c>"3.2 GB"</c>. When not set, the number out of the maximum (the percentage in the center of a ring).</summary>
    public string? ValueText { get => (string?)GetValue(ValueTextProperty); set => SetValue(ValueTextProperty, value); }

    /// <summary>A line of smaller text under the meter.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The accessible name of a meter without a <see cref="Title"/>, in the language of the app.</summary>
    public string DefaultTitle { get => (string)GetValue(DefaultTitleProperty); set => SetValue(DefaultTitleProperty, value); }

    /// <summary>What a screen reader adds for the current level of a scale, in the language of the app.</summary>
    public string CurrentLevelLabel { get => (string)GetValue(CurrentLevelLabelProperty); set => SetValue(CurrentLevelLabelProperty, value); }

    private bool HasParts => Parts is { Count: > 0 };

    private bool IsScale => Layout == KlyMeterLayout.Scale && Levels is { Count: > 0 };

    /// <summary>The maximum in effect: <see cref="Max"/>, or the sum of the parts when it is not positive.</summary>
    internal double EffectiveMax => Max > 0 ? Max : HasParts ? Math.Max(1, Parts!.Sum(PartAmount)) : 100;

    /// <summary>The value, clamped between 0 and the maximum.</summary>
    internal double EffectiveValue => Math.Clamp(Value, 0, EffectiveMax);

    /// <summary>The share of the maximum, from 0 to 100.</summary>
    internal double Percent => Math.Clamp(EffectiveValue / EffectiveMax * 100.0, 0, 100);

    /// <summary>The color of the bar or the ring: the error color from the error threshold, the warning color from the warning one, else primary.</summary>
    internal KlyColor Tone => ErrorThreshold is { } error && Percent >= error ? KlyColor.Error : WarningThreshold is { } warning && Percent >= warning ? KlyColor.Warning : KlyColor.Primary;

    /// <summary>The value as written next to the title.</summary>
    internal string ValueLabel => ValueText ?? $"{EffectiveValue.ToString("0.##", CultureInfo.CurrentCulture)} / {EffectiveMax.ToString("0.##", CultureInfo.CurrentCulture)}";

    /// <summary>Whether a level of a scale is reached: it is not above the current level.</summary>
    internal bool Reached(int level) => CurrentLevel is { } current && level <= current;

    internal VerticalStackLayout Stack => _stack;

    // A part's size on the meter: its value, capped by its own maximum when it has one.
    private static double PartAmount(KlyMeterPart part) => Math.Max(0, part.Max is { } partMax ? Math.Min(part.Value, partMax) : part.Value);

    private static string PartValueText(KlyMeterPart part) =>
        part.ValueLabel ?? (part.Max is { } partMax
            ? $"{part.Value.ToString("0.##", CultureInfo.CurrentCulture)} / {partMax.ToString("0.##", CultureInfo.CurrentCulture)}"
            : part.Value.ToString("0.##", CultureInfo.CurrentCulture));

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyMeter)bindable).Rebuild();

    // The arcs of the ring: one per part, stacked, or one for the value, rounded and colored by the thresholds.
    internal IReadOnlyList<DisplayRingArc> RingArcs()
    {
        if (HasParts)
        {
            var arcs = new List<DisplayRingArc>();
            var offset = 0.0;
            foreach (var part in Parts!)
            {
                var share = Math.Clamp(PartAmount(part) / EffectiveMax * 100.0, 0, 100 - offset);
                arcs.Add(new DisplayRingArc(share, offset, KlyTheme.Resolve(part.Color ?? KlyColor.Primary).Fill));
                offset += share;
            }
            return arcs;
        }
        return Percent > 0 ? [new DisplayRingArc(Percent, 0, KlyTheme.Resolve(Tone).Fill, RoundCap: true)] : [];
    }

    private void Rebuild()
    {
        _stack.Children.Clear();
        var name = Title ?? DefaultTitle;
        var hasHeader = !string.IsNullOrEmpty(Title) || !string.IsNullOrEmpty(ValueText);

        if (Layout == KlyMeterLayout.Ring)
        {
            _stack.Children.Add(BuildRing(name));
        }
        else
        {
            if (hasHeader) _stack.Children.Add(BuildHeader());
            _stack.Children.Add(IsScale ? BuildScale() : BuildTrack(name));
            if (!string.IsNullOrEmpty(Description))
            {
                var caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
                caption.Text = Description;
                _stack.Children.Add(caption);
            }
        }

        if (HasParts && !IsScale && Parts!.Any(p => p.ValueLabel is not null || p.Icon is not null || p.Max is not null))
        {
            _stack.Children.Add(BuildLegend());
        }
    }

    private View BuildHeader()
    {
        var header = new Grid { ColumnSpacing = 8 };
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
        label.Text = Title;
        var value = DisplayParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        value.Text = ValueLabel;
        value.HorizontalTextAlignment = TextAlignment.End;
        header.Add(label, 0);
        header.Add(value, 1);
        return header;
    }

    private View BuildTrack(string name)
    {
        var track = new Border { StrokeThickness = 0, Padding = 0, HeightRequest = 10 };
        track.SetRadius(9999);
        track.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundMuted);

        // Each part takes its share of the width of the track: star columns need no measure of the track, and the rest of the track is the last column.
        var cells = new Grid { ColumnSpacing = 0 };
        if (HasParts)
        {
            var used = 0.0;
            foreach (var part in Parts!)
            {
                var share = Math.Clamp(PartAmount(part) / EffectiveMax * 100.0, 0, 100 - used);
                used += share;
                cells.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(share, GridUnitType.Star)));
                cells.Add(Fill(KlyTheme.Resolve(part.Color ?? KlyColor.Primary), 0), cells.ColumnDefinitions.Count - 1);
            }
            cells.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Math.Max(0, 100 - used), GridUnitType.Star)));
        }
        else
        {
            cells.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(Percent, GridUnitType.Star)));
            cells.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(100 - Percent, GridUnitType.Star)));
            cells.Add(Fill(KlyTheme.Resolve(Tone), 9999), 0);
        }
        track.Content = cells;
        Describe(track, name);
        return track;
    }

    private static Border Fill(KlyColorSet set, double radius)
    {
        var fill = new Border { StrokeThickness = 1, Padding = 0 };
        fill.SetThemeColor(BackgroundColorProperty, set.Fill);
        fill.SetStroke(set.Text);
        fill.SetRadius(radius);
        return fill;
    }

    private View BuildScale()
    {
        var levels = Levels!;
        var scale = new Grid { ColumnSpacing = 6 };
        for (var i = 0; i < levels.Count; i++)
        {
            scale.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var reached = Reached(i);
            var cell = new Border { StrokeThickness = 1, Padding = new Thickness(8, 4) };
            cell.SetRadius(KlyTheme.RadiusOf(1));
            cell.SetStroke(reached ? DisplayParts.PrimaryText : KlyTheme.Border);
            cell.SetThemeColor(BackgroundColorProperty, reached ? KlyTheme.BackgroundMuted : KlyThemeColor.Transparent);
            var label = DisplayParts.NewLabel(12, reached ? KlyTheme.TextHighlighted : KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
            label.Text = levels[i];
            label.HorizontalTextAlignment = TextAlignment.Center;
            cell.Content = label;
            if (CurrentLevel == i) SemanticProperties.SetDescription(cell, $"{levels[i]}, {CurrentLevelLabel}");
            scale.Add(cell, i);
        }
        return scale;
    }

    private View BuildRing(string name)
    {
        var ring = new DisplayMeterRing { Arcs = RingArcs() };
        var center = DisplayParts.NewLabel(14, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.NoWrap);
        center.Text = ValueText ?? $"{Math.Round(Percent).ToString(CultureInfo.CurrentCulture)}%";
        center.HorizontalOptions = LayoutOptions.Center;
        center.VerticalOptions = LayoutOptions.Center;
        var drawing = new Grid { WidthRequest = 80, HeightRequest = 80, VerticalOptions = LayoutOptions.Center, Children = { ring, center } };
        Describe(drawing, name);

        var texts = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        if (!string.IsNullOrEmpty(Title))
        {
            var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            label.Text = Title;
            texts.Children.Add(label);
        }
        if (!string.IsNullOrEmpty(Description))
        {
            var caption = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            caption.Text = Description;
            texts.Children.Add(caption);
        }
        return new HorizontalStackLayout { Spacing = 16, Children = { drawing, texts } };
    }

    // What a screen reader says for the gauge: its name, then the value as displayed.
    private void Describe(View view, string name)
    {
        SemanticProperties.SetDescription(view, name);
        SemanticProperties.SetHint(view, ValueLabel);
    }

    private View BuildLegend()
    {
        var legend = new FlexLayout { Wrap = FlexWrap.Wrap, AlignItems = FlexAlignItems.Center };
        foreach (var part in Parts!)
        {
            var set = KlyTheme.Resolve(part.Color ?? KlyColor.Primary);
            var item = new HorizontalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 16, 4), VerticalOptions = LayoutOptions.Center };
            if (!string.IsNullOrEmpty(part.Icon))
            {
                var icon = DisplayParts.SmallIcon(part.Icon, set.Text);
                icon.Scale = 1;
                icon.Size = KlySize.Sm;
                item.Children.Add(icon);
            }
            else
            {
                var dot = DisplayParts.Tile(8, 4, set.Fill);
                dot.StrokeThickness = 1;
                dot.SetStroke(set.Text);
                item.Children.Add(dot);
            }
            var label = DisplayParts.NewLabel(12, KlyTheme.TextMuted);
            label.Text = part.Label;
            var value = DisplayParts.NewLabel(12, KlyTheme.TextHighlighted, FontAttributes.Bold);
            value.Text = PartValueText(part);
            item.Children.Add(label);
            item.Children.Add(value);
            legend.Children.Add(item);
        }
        return legend;
    }
}
