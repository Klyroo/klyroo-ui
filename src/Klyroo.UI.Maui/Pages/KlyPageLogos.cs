using Microsoft.Maui.Layouts;

namespace Klyroo.UI.Maui;

/// <summary>
/// The logos of customers or partners, as a wrapped and centered list or, with <see cref="Marquee"/>, as a strip that scrolls in a loop.
/// A logo is named by its <see cref="PageLogoItem.Alt"/>: an icon gets it as its label, an image as its description for a screen reader.
/// </summary>
[ContentProperty(nameof(Logos))]
public class KlyPageLogos : ContentView
{
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyPageLogos), null, propertyChanged: (b, _, v) => PagesUi.Show(((KlyPageLogos)b)._title, (string?)v));
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<PageLogoItem>), typeof(KlyPageLogos), null, propertyChanged: (b, _, _) => ((KlyPageLogos)b).Rebuild());
    /// <summary>Identifies <see cref="Marquee"/>.</summary>
    public static readonly BindableProperty MarqueeProperty = BindableProperty.Create(nameof(Marquee), typeof(bool), typeof(KlyPageLogos), false, propertyChanged: (b, _, _) => ((KlyPageLogos)b).Rebuild());
    /// <summary>Identifies <see cref="Duration"/>.</summary>
    public static readonly BindableProperty DurationProperty = BindableProperty.Create(nameof(Duration), typeof(double), typeof(KlyPageLogos), 30d, propertyChanged: (b, _, _) => ((KlyPageLogos)b).RestartMarquee());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int?), typeof(KlyPageLogos), 2, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyPageLogos)b)._title, (int?)v));

    private const string AnimationName = "klyroo-marquee";

    private readonly Label _title = PagesUi.Text(18, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly System.Collections.ObjectModel.ObservableCollection<View> _logos = [];
    private readonly FlexLayout _wrap = new() { Wrap = FlexWrap.Wrap, JustifyContent = FlexJustify.Center, AlignItems = FlexAlignItems.Center };
    private readonly HorizontalStackLayout _strip = new() { HorizontalOptions = LayoutOptions.Start };
    private readonly ContentView _stripHost = new() { IsClippedToBounds = true };

    /// <summary>Creates a list of logos.</summary>
    public KlyPageLogos()
    {
        _title.HorizontalTextAlignment = TextAlignment.Center;
        PagesUi.Heading(_title, HeadingLevel);
        _stripHost.Content = _strip;
        Content = new VerticalStackLayout { Spacing = 32, Children = { _title, _wrap, _stripHost } };

        _logos.CollectionChanged += (_, _) => Rebuild();
        _strip.SizeChanged += (_, _) => RestartMarquee();
        HandlerChanged += (_, _) => RestartMarquee();
        Rebuild();
    }

    /// <summary>Heading above the logos.</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>The logos, each an icon or an image with an accessible name.</summary>
    public IReadOnlyList<PageLogoItem>? Items { get => (IReadOnlyList<PageLogoItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Custom logos, shown after <see cref="Items"/>. Give each one a description for a screen reader. A view has one parent, so the <see cref="Marquee"/> cannot repeat them: it scrolls the <see cref="Items"/> only.</summary>
    public IList<View> Logos => _logos;

    /// <summary>Scrolls the logos in a loop. The scrolling copy is hidden from assistive technology. The loop runs while the control is on screen.</summary>
    public bool Marquee { get => (bool)GetValue(MarqueeProperty); set => SetValue(MarqueeProperty, value); }

    /// <summary>Seconds for one loop of the marquee.</summary>
    public double Duration { get => (double)GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

    /// <summary>Gives the title a heading level for a screen reader, 1 to 6; it is plain text when <c>null</c>. 2 by default.</summary>
    public int? HeadingLevel { get => (int?)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    internal FlexLayout Wrap => _wrap;

    internal HorizontalStackLayout Strip => _strip;

    internal bool IsMarqueeRunning { get; private set; }

    internal void StartMarquee(double loopWidth)
    {
        StopMarquee();
        if (!Marquee || loopWidth <= 0) return;
        var animation = new Animation(v => _strip.TranslationX = -v, 0, loopWidth);
        animation.Commit(this, AnimationName, 16, (uint)Math.Max(1, Duration * 1000), Easing.Linear, repeat: () => true);
        IsMarqueeRunning = true;
    }

    internal void StopMarquee()
    {
        if (!IsMarqueeRunning) return;
        this.AbortAnimation(AnimationName);
        _strip.TranslationX = 0;
        IsMarqueeRunning = false;
    }

    private void RestartMarquee()
    {
        StopMarquee();
        // Two identical halves: the strip scrolls by one half, and the copy takes its place without a jump.
        if (Handler is not null && Marquee) StartMarquee(_strip.Width / 2);
    }

    private IEnumerable<View> Logo(bool includeCustom)
    {
        foreach (var logo in Items ?? [])
        {
            if (logo.Icon is not null)
            {
                // A 24 unit icon drawn at 40: scaled, with the margin that gives the layout the 40 it needs.
                yield return new KlyIcon { Name = logo.Icon, Size = KlySize.Xl, IconColor = KlyTheme.TextMuted, Label = logo.Alt, Scale = 40d / 24, Margin = new Thickness(8) };
            }
            else if (logo.Source is not null)
            {
                var image = new Image { Source = logo.Source, HeightRequest = 40, MaximumWidthRequest = 160, Aspect = Aspect.AspectFit, HorizontalOptions = LayoutOptions.Start };
                SemanticProperties.SetDescription(image, logo.Alt);
                yield return image;
            }
        }
        if (!includeCustom) yield break;
        foreach (var custom in _logos) yield return custom;
    }

    private void Rebuild()
    {
        StopMarquee();
        _wrap.Children.Clear();
        _strip.Children.Clear();
        if (Marquee)
        {
            // A view has one parent, so the custom logos cannot be repeated for the loop: the marquee scrolls the Items only.
            var views = Logo(false).ToList();
            _wrap.IsVisible = false;
            _stripHost.IsVisible = views.Count > 0;
            AddRun(_strip, views, 48);
            // The copy that scrolls in is the same logos again: a screen reader reads them once.
            var copy = new HorizontalStackLayout();
            AutomationProperties.SetIsInAccessibleTree(copy, false);
            AddRun(copy, Logo(false).ToList(), 48);
            _strip.Children.Add(copy);
        }
        else
        {
            var views = Logo(true).ToList();
            _stripHost.IsVisible = false;
            _wrap.IsVisible = views.Count > 0;
            foreach (var view in views)
            {
                view.Margin = new Thickness(view.Margin.Left + 24, view.Margin.Top + 12, view.Margin.Right + 24, view.Margin.Bottom + 12);
                _wrap.Children.Add(view);
            }
        }
        RestartMarquee();
    }

    private static void AddRun(Layout run, List<View> views, double gap)
    {
        foreach (var view in views)
        {
            view.Margin = new Thickness(view.Margin.Left, view.Margin.Top, view.Margin.Right + gap, view.Margin.Bottom);
            view.VerticalOptions = LayoutOptions.Center;
            run.Children.Add(view);
        }
    }
}
