namespace Klyroo.UI.Maui;

/// <summary>A row of overlapping avatars with a "+N" avatar for the ones that do not fit.</summary>
public class KlyAvatarGroup : HorizontalStackLayout
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<KlyAvatarItem>), typeof(KlyAvatarGroup), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Max"/>.</summary>
    public static readonly BindableProperty MaxProperty = BindableProperty.Create(nameof(Max), typeof(int?), typeof(KlyAvatarGroup), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyAvatarGroup), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyAvatarGroup), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyAvatarGroup)b, (string?)v));
    /// <summary>Identifies <see cref="MoreFormat"/>.</summary>
    public static readonly BindableProperty MoreFormatProperty = BindableProperty.Create(nameof(MoreFormat), typeof(string), typeof(KlyAvatarGroup), "{0} more", propertyChanged: Rebuild);

    // The ring of 2 around each avatar is part of its size here (it is a shadow on the web), so the spacing makes up for it: -8 of the web plus the 4 of the two rings.
    private const double Overlap = -12;
    private const double RingWidth = 2;

    /// <summary>Creates an empty group: set <see cref="Items"/>.</summary>
    public KlyAvatarGroup()
    {
        Spacing = Overlap;
        HorizontalOptions = LayoutOptions.Start;
        VerticalOptions = LayoutOptions.Center;
        Rebuild();
    }

    /// <summary>The avatars to show, as <see cref="KlyAvatarItem"/>.</summary>
    public IReadOnlyList<KlyAvatarItem>? Items { get => (IReadOnlyList<KlyAvatarItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary>Most avatars to show; the others collapse into a "+N" avatar. <see langword="null"/> shows them all.</summary>
    public int? Max { get => (int?)GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Size of the avatars, from <c>Xs</c> (24) to <c>Xl</c> (48).</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the group ("Project members").</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    /// <summary>The accessible name of the "+N" avatar, where <c>{0}</c> is the number of hidden avatars, in the language of the app.</summary>
    public string MoreFormat { get => (string)GetValue(MoreFormatProperty); set => SetValue(MoreFormatProperty, value); }

    internal static double DiameterOf(KlySize size) => size switch
    {
        KlySize.Xs => 24,
        KlySize.Sm => 28,
        KlySize.Md => 32,
        KlySize.Lg => 40,
        KlySize.Xl => 48,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private IReadOnlyList<KlyAvatarItem> All => Items ?? [];

    private IEnumerable<KlyAvatarItem> Visible => Max is { } max && max >= 0 ? All.Take(max) : All;

    private int Hidden => Max is { } max && max >= 0 ? Math.Max(All.Count - max, 0) : 0;

    // The name of the avatar and what a screen reader says for it: the name, then the description and the badge when there are any (the stack has no room for text).
    private static string? LabelOf(KlyAvatarItem item) =>
        item.Description is null && item.Badge is null
            ? item.Alt
            : string.Join(" · ", new[] { item.Alt, item.Description, item.Badge?.Label }.Where(s => !string.IsNullOrWhiteSpace(s)));

    // KlyAvatar takes its initials from the first letters of the first two words: a text that is already the initials ("AL") is spread so that it stays as it is.
    // A name (Alt) needs no help: it gives its initials the same way.
    internal static string? Spread(string? text) =>
        text is null ? null : string.Join(' ', text.Where(c => !char.IsWhiteSpace(c)).Take(2));

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyAvatarGroup)bindable).Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        var diameter = DiameterOf(Size);
        foreach (var item in Visible)
        {
            var avatar = new KlyAvatar { Source = item.Src, Text = item.Text is { } text ? Spread(text) : item.Alt, Alt = LabelOf(item), Color = item.Color, Size = Size };
            Children.Add(Ring(avatar, diameter));
        }

        if (Hidden > 0)
        {
            var fontSize = Size switch { KlySize.Xs => 10d, KlySize.Sm => 12d, KlySize.Md => 14d, KlySize.Lg => 16d, _ => 18d };
            var more = new Border { WidthRequest = diameter, HeightRequest = diameter, StrokeThickness = 0, Padding = 0 };
            more.SetRadius(diameter / 2);
            more.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundAccented);
            var label = DisplayParts.NewLabel(fontSize, KlyTheme.Text, FontAttributes.Bold, LineBreakMode.NoWrap);
            label.Text = $"+{Hidden}";
            label.HorizontalOptions = LayoutOptions.Center;
            label.VerticalOptions = LayoutOptions.Center;
            more.Content = label;
            SemanticProperties.SetDescription(more, string.Format(MoreFormat, Hidden));
            Children.Add(Ring(more, diameter));
        }
    }

    private static Border Ring(View inner, double diameter)
    {
        var ring = new Border { StrokeThickness = RingWidth, Padding = 0, Content = inner };
        ring.SetStroke(KlyTheme.Background);
        ring.SetRadius((diameter + 2 * RingWidth) / 2);
        return ring;
    }
}
