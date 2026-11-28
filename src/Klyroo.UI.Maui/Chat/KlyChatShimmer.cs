using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A sweeping highlight over a text, to tell that something is still arriving. The text is muted and a band of the highlighted color crosses it, one character at a time.
/// The sweep runs while the control is on screen; where the system turns animations off the text stays plain and muted.
/// </summary>
public class KlyChatShimmer : ContentView
{
    private const string AnimationName = "KlyChatShimmer";

    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChatShimmer), "", propertyChanged: (b, _, _) => ((KlyChatShimmer)b).Refresh());
    /// <summary>Identifies <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(double), typeof(KlyChatShimmer), 2d, propertyChanged: (b, _, _) => ((KlyChatShimmer)b).SyncMotion());
    /// <summary>Identifies <see cref="Spread"/>.</summary>
    public static readonly BindableProperty SpreadProperty = BindableProperty.Create(nameof(Spread), typeof(double), typeof(KlyChatShimmer), 2d);
    /// <summary>Identifies <see cref="FontSize"/>.</summary>
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(double), typeof(KlyChatShimmer), 14d, propertyChanged: (b, _, v) => ((KlyChatShimmer)b)._label.FontSize = (double)v);
    /// <summary>Identifies <see cref="FontAttributes"/>.</summary>
    public static readonly BindableProperty FontAttributesProperty = BindableProperty.Create(nameof(FontAttributes), typeof(FontAttributes), typeof(KlyChatShimmer), FontAttributes.None, propertyChanged: (b, _, v) => ((KlyChatShimmer)b)._label.FontAttributes = (FontAttributes)v);
    /// <summary>Identifies <see cref="LineBreakMode"/>.</summary>
    public static readonly BindableProperty LineBreakModeProperty = BindableProperty.Create(nameof(LineBreakMode), typeof(LineBreakMode), typeof(KlyChatShimmer), LineBreakMode.WordWrap, propertyChanged: (b, _, v) => ((KlyChatShimmer)b)._label.LineBreakMode = (LineBreakMode)v);

    private readonly Label _label = new() { FontSize = 14 };
    private bool _sweeping;

    /// <summary>Creates a shimmer.</summary>
    public KlyChatShimmer()
    {
        Content = _label;
        Refresh();
    }

    /// <summary>The text to shimmer.</summary>
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>Seconds one sweep takes.</summary>
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>Width of the highlight, relative to the text: 2 by default, bigger is a wider band.</summary>
    public double Spread { get => (double)GetValue(SpreadProperty); set => SetValue(SpreadProperty, value); }

    /// <summary>Font size of the text.</summary>
    public double FontSize { get => (double)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }

    /// <summary>Weight of the text: <c>Bold</c> for the medium weight of a name.</summary>
    public FontAttributes FontAttributes { get => (FontAttributes)GetValue(FontAttributesProperty); set => SetValue(FontAttributesProperty, value); }

    /// <summary>What happens to a text that does not fit.</summary>
    public LineBreakMode LineBreakMode { get => (LineBreakMode)GetValue(LineBreakModeProperty); set => SetValue(LineBreakModeProperty, value); }

    /// <summary>The label that shows the text. It holds the text while still, and one span per character while sweeping.</summary>
    internal Label Label => _label;

    /// <summary>The share of the text width the highlight covers: 0.2 of the 250% background of the web gradient at the default spread.</summary>
    internal double BandWidth => 0.2 * (0.5 + Math.Max(Spread, 0.5));

    /// <summary>The weight of the highlight, from 0 to 1, on each character for a sweep that is at <paramref name="phase"/> (0 to 1) of its way.</summary>
    internal double[] Weights(int count, double phase)
    {
        var band = BandWidth;
        var center = -band / 2 + phase * (1 + band);
        var weights = new double[count];
        for (var i = 0; i < count; i++)
        {
            var position = (i + 0.5) / count;
            weights[i] = Math.Clamp(1 - Math.Abs(position - center) / (band / 2), 0, 1);
        }
        return weights;
    }

    /// <summary>Colors the characters for a sweep that is at <paramref name="phase"/> (0 to 1) of its way.</summary>
    internal void ApplyPhase(double phase)
    {
        var elements = Elements(Text);
        if (elements.Count == 0) return;
        var weights = Weights(elements.Count, phase);
        var muted = KlyTheme.TextMuted.Resolve();
        var highlighted = KlyTheme.TextHighlighted.Resolve();
        var spans = _label.FormattedText?.Spans;
        if (spans is null || spans.Count != elements.Count)
        {
            var formatted = new FormattedString();
            foreach (var element in elements) formatted.Spans.Add(new Span { Text = element, TextColor = muted });
            _label.Text = null;
            _label.FormattedText = formatted;
            spans = formatted.Spans;
        }
        for (var i = 0; i < spans.Count; i++) spans[i].TextColor = Blend(muted, highlighted, weights[i]);
    }

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        SyncMotion();
    }

    private static Color Blend(Color from, Color to, double weight)
    {
        var w = (float)weight;
        return new Color(from.Red + (to.Red - from.Red) * w, from.Green + (to.Green - from.Green) * w, from.Blue + (to.Blue - from.Blue) * w, from.Alpha + (to.Alpha - from.Alpha) * w);
    }

    private static List<string> Elements(string? text)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(text)) return list;
        var enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext()) list.Add((string)enumerator.Current);
        return list;
    }

    private void Refresh()
    {
        SemanticProperties.SetDescription(this, Text);
        ShowStill();
        SyncMotion();
    }

    // The plain look: the muted color of the theme, which follows light and dark by itself.
    private void ShowStill()
    {
        _label.FormattedText = null;
        _label.Text = Text;
        _label.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
    }

    private void SyncMotion()
    {
        var wanted = Handler is not null && !string.IsNullOrEmpty(Text) && !ChatDataMotion.Reduced;
        if (wanted == _sweeping)
        {
            if (wanted)
            {
                ChatDataMotion.Stop(this, AnimationName);
                Sweep();
            }
            return;
        }
        _sweeping = wanted;
        if (wanted)
        {
            Sweep();
        }
        else
        {
            ChatDataMotion.Stop(this, AnimationName);
            ShowStill();
        }
    }

    private void Sweep() => ChatDataMotion.Loop(this, AnimationName, ApplyPhase, (uint)(Math.Max(Duration, 0.1) * 1000), 33);
}
