namespace Klyroo.UI.Maui;

/// <summary>An avatar with a name and a description, optionally tappable, with a badge and contact lines.</summary>
public class KlyUser : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Name"/>.</summary>
    public static readonly BindableProperty NameProperty = BindableProperty.Create(nameof(Name), typeof(string), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AvatarSrc"/>.</summary>
    public static readonly BindableProperty AvatarSrcProperty = BindableProperty.Create(nameof(AvatarSrc), typeof(ImageSource), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AvatarIcon"/>.</summary>
    public static readonly BindableProperty AvatarIconProperty = BindableProperty.Create(nameof(AvatarIcon), typeof(View), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="AvatarColor"/>.</summary>
    public static readonly BindableProperty AvatarColorProperty = BindableProperty.Create(nameof(AvatarColor), typeof(KlyColor?), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Badge"/>.</summary>
    public static readonly BindableProperty BadgeProperty = BindableProperty.Create(nameof(Badge), typeof(KlyBadgeItem), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Contacts"/>.</summary>
    public static readonly BindableProperty ContactsProperty = BindableProperty.Create(nameof(Contacts), typeof(IReadOnlyList<KlyContactItem>), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="ShowAvatar"/>.</summary>
    public static readonly BindableProperty ShowAvatarProperty = BindableProperty.Create(nameof(ShowAvatar), typeof(bool), typeof(KlyUser), true, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Href"/>.</summary>
    public static readonly BindableProperty HrefProperty = BindableProperty.Create(nameof(Href), typeof(string), typeof(KlyUser), null, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Target"/>.</summary>
    public static readonly BindableProperty TargetProperty = BindableProperty.Create(nameof(Target), typeof(KlyLinkTarget?), typeof(KlyUser), null);
    /// <summary>Identifies <see cref="Orientation"/>.</summary>
    public static readonly BindableProperty OrientationProperty = BindableProperty.Create(nameof(Orientation), typeof(KlyOrientation), typeof(KlyUser), KlyOrientation.Horizontal, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyUser), KlySize.Md, propertyChanged: Rebuild);
    /// <summary>Identifies <see cref="CopyLabelFormat"/>.</summary>
    public static readonly BindableProperty CopyLabelFormatProperty = BindableProperty.Create(nameof(CopyLabelFormat), typeof(string), typeof(KlyUser), "Copy {0}", propertyChanged: Rebuild);

    /// <summary>Creates an empty user: set <see cref="Name"/>.</summary>
    public KlyUser()
    {
        Spacing = 12;
        HorizontalOptions = LayoutOptions.Fill;
        Rebuild();
    }

    /// <summary>Raised when the user is tapped, when it has an <see cref="Href"/> or a handler. The address opens first.</summary>
    public event EventHandler? Clicked;

    /// <summary>Name of the person or entity.</summary>
    public string? Name { get => (string?)GetValue(NameProperty); set => SetValue(NameProperty, value); }

    /// <summary>Text under the name, such as a role or an email address.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>The picture of the avatar. A string or a <see cref="Uri"/> converts to an <see cref="ImageSource"/>.</summary>
    public ImageSource? AvatarSrc { get => (ImageSource?)GetValue(AvatarSrcProperty); set => SetValue(AvatarSrcProperty, value); }

    /// <summary>The icon of the avatar when there is no picture, such as a <see cref="KlyIcon"/>. Without a picture and without a <see cref="Name"/>, a person icon is shown.</summary>
    public View? AvatarIcon { get => (View?)GetValue(AvatarIconProperty); set => SetValue(AvatarIconProperty, value); }

    /// <summary>A color that tints the initials avatar.</summary>
    public KlyColor? AvatarColor { get => (KlyColor?)GetValue(AvatarColorProperty); set => SetValue(AvatarColorProperty, value); }

    /// <summary>A badge shown at the end of the row.</summary>
    public KlyBadgeItem? Badge { get => (KlyBadgeItem?)GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }

    /// <summary>Contact lines under the user, each with an optional icon, address and copy button. They are not part of the tappable user.</summary>
    public IReadOnlyList<KlyContactItem>? Contacts { get => (IReadOnlyList<KlyContactItem>?)GetValue(ContactsProperty); set => SetValue(ContactsProperty, value); }

    /// <summary>Shows the avatar: the picture, else the initials of <see cref="Name"/>.</summary>
    public bool ShowAvatar { get => (bool)GetValue(ShowAvatarProperty); set => SetValue(ShowAvatarProperty, value); }

    /// <summary>Makes the whole user open this address when it is tapped.</summary>
    public string? Href { get => (string?)GetValue(HrefProperty); set => SetValue(HrefProperty, value); }

    /// <summary>Where <see cref="Href"/> opens: <see cref="KlyLinkTarget.Blank"/> in the external browser, anything else in the in-app browser when the platform has one.</summary>
    public KlyLinkTarget? Target { get => (KlyLinkTarget?)GetValue(TargetProperty); set => SetValue(TargetProperty, value); }

    /// <summary>Whether the name sits beside the avatar (<c>Horizontal</c>) or under it (<c>Vertical</c>).</summary>
    public KlyOrientation Orientation { get => (KlyOrientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Size of the user, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>The accessible name of a copy button, where <c>{0}</c> is the value it copies, in the language of the app.</summary>
    public string CopyLabelFormat { get => (string)GetValue(CopyLabelFormatProperty); set => SetValue(CopyLabelFormatProperty, value); }

    internal View Person => (View)Children[0];

    internal void SendTapped() => Tap();

    private static void Rebuild(BindableObject bindable, object oldValue, object newValue) => ((KlyUser)bindable).Rebuild();

    private void Rebuild()
    {
        Children.Clear();
        Children.Add(BuildPerson());

        if (Contacts is { Count: > 0 } contacts)
        {
            var list = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(0, 8, 0, 0) };
            var top = DisplayParts.Divider(KlyTheme.Border);
            Children.Add(top);
            foreach (var contact in contacts) list.Children.Add(BuildContact(contact));
            Children.Add(list);
        }
    }

    private void Tap()
    {
        if (Href is null && Clicked is null) return;
        DisplayLaunch.Open(Href, Target);
        Clicked?.Invoke(this, EventArgs.Empty);
    }

    private View BuildPerson()
    {
        var gap = Size switch { KlySize.Xs => 6d, KlySize.Sm => 8d, KlySize.Md => 10d, KlySize.Lg => 12d, KlySize.Xl => 14d, _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null) };
        var (nameSize, descriptionSize) = Size switch
        {
            KlySize.Xs => (12d, 11d),
            KlySize.Sm => (12d, 12d),
            KlySize.Md => (14d, 12d),
            KlySize.Lg => (14d, 14d),
            KlySize.Xl => (16d, 14d),
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        var vertical = Orientation == KlyOrientation.Vertical;

        var name = DisplayParts.NewLabel(nameSize, KlyTheme.TextHighlighted, FontAttributes.Bold, LineBreakMode.TailTruncation);
        DisplayParts.Show(name, Name);
        var description = DisplayParts.NewLabel(descriptionSize, KlyTheme.TextMuted, lineBreak: LineBreakMode.TailTruncation);
        DisplayParts.Show(description, Description);
        if (vertical)
        {
            name.HorizontalTextAlignment = TextAlignment.Center;
            description.HorizontalTextAlignment = TextAlignment.Center;
        }
        var texts = new VerticalStackLayout { Spacing = 0, VerticalOptions = LayoutOptions.Center, Children = { name, description } };

        Border root;
        if (vertical)
        {
            var column = new VerticalStackLayout { Spacing = gap, HorizontalOptions = LayoutOptions.Center };
            if (ShowAvatar) column.Children.Add(BuildAvatar());
            if (Name is not null || Description is not null) column.Children.Add(texts);
            if (Badge is { } badge) column.Children.Add(DisplayParts.Badge(badge));
            root = new Border { StrokeThickness = 0, Padding = 0, Content = column };
        }
        else
        {
            var row = new Grid { ColumnSpacing = gap };
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            if (ShowAvatar) row.Add(BuildAvatar(), 0);
            if (Name is not null || Description is not null) row.Add(texts, 1);
            if (Badge is { } badge)
            {
                var view = DisplayParts.Badge(badge);
                view.HorizontalOptions = LayoutOptions.End;
                row.Add(view, 2);
            }
            root = new Border { StrokeThickness = 0, Padding = 0, Content = row };
        }

        // The whole user is one tap target when it goes somewhere, and one thing for a screen reader: name, then description.
        root.SetRadius(KlyTheme.RadiusOf(1));
        DisplayParts.Tap(root, Tap);
        if (Href is not null)
        {
            SemanticProperties.SetDescription(root, string.Join(", ", new[] { Name, Description }.Where(s => !string.IsNullOrEmpty(s))));
        }
        return root;
    }

    // The name is next to the avatar, so the avatar is decoration: hidden from assistive technology.
    private View BuildAvatar()
    {
        var empty = string.IsNullOrWhiteSpace(Name) && AvatarSrc is null;
        DisplayParts.Detach(AvatarIcon);
        View avatar;
        if (AvatarIcon is not null || empty)
        {
            var diameter = KlyAvatarGroup.DiameterOf(Size);
            var tile = DisplayParts.Tile(diameter, diameter / 2, AvatarColor is { } c ? KlyTheme.Resolve(c).Fill.WithAlpha(0.15f) : KlyTheme.BackgroundAccented);
            tile.Content = AvatarIcon ?? new KlyIcon
            {
                Name = "user",
                Size = Size is KlySize.Xs or KlySize.Sm ? KlySize.Sm : KlySize.Md,
                IconColor = AvatarColor is { } color ? KlyTheme.Resolve(color).Text : KlyTheme.Text,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
            };
            avatar = tile;
        }
        else
        {
            avatar = new KlyAvatar { Source = AvatarSrc, Text = Name, Alt = Name, Color = AvatarColor, Size = Size };
        }
        AutomationProperties.SetIsInAccessibleTree(avatar, false);
        return avatar;
    }

    private View BuildContact(KlyContactItem contact)
    {
        var row = new Grid { ColumnSpacing = 6 };
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        row.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        if (!string.IsNullOrEmpty(contact.Icon))
        {
            row.Add(DisplayParts.SmallIcon(contact.Icon, KlyTheme.TextMuted), 0);
        }

        View value;
        if (!string.IsNullOrEmpty(contact.Href))
        {
            value = DisplayParts.LinkLabel(contact.Value, DisplayParts.PrimaryText, 12, contact.Href, null);
        }
        else
        {
            var text = DisplayParts.NewLabel(12, KlyTheme.Text, lineBreak: LineBreakMode.TailTruncation);
            text.Text = contact.Value;
            text.VerticalTextAlignment = TextAlignment.Center;
            value = text;
        }
        row.Add(value, 1);

        if (contact.Copy)
        {
            var copy = new DisplayIconButton(KlyIconNames.Copy, string.Format(CopyLabelFormat, contact.Value));
            copy.Clicked += (_, _) => DisplayClipboard.Copy(contact.Value);
            row.Add(copy, 2);
        }
        return row;
    }
}
