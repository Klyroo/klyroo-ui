using Microsoft.Maui.Controls.Shapes;

namespace Klyroo.UI.Maui;

/// <summary>
/// One message of a conversation: an optional avatar, a header, attachments, the text in a bubble and actions.
/// A user message sits on the right in a soft bubble; the others sit on the left with no surface; a system message is a centered notice.
/// Give <see cref="Text"/> for plain text or <see cref="Content"/> for any view: the library renders no markdown.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyChatMessage : Grid
{
    private const string CursorName = "KlyChatMessageCursor";

    /// <summary>Identifies <see cref="Role"/>.</summary>
    public static readonly BindableProperty RoleProperty = BindableProperty.Create(nameof(Role), typeof(ChatRole), typeof(KlyChatMessage), ChatRole.Assistant, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Text"/>.</summary>
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Streaming"/>.</summary>
    public static readonly BindableProperty StreamingProperty = BindableProperty.Create(nameof(Streaming), typeof(bool), typeof(KlyChatMessage), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Side"/>.</summary>
    public static readonly BindableProperty SideProperty = BindableProperty.Create(nameof(Side), typeof(ChatSide?), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(ChatMessageVariant?), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyChatMessage), KlyColor.Neutral, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Compact"/>.</summary>
    public static readonly BindableProperty CompactProperty = BindableProperty.Create(nameof(Compact), typeof(bool), typeof(KlyChatMessage), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AvatarSource"/>.</summary>
    public static readonly BindableProperty AvatarSourceProperty = BindableProperty.Create(nameof(AvatarSource), typeof(ImageSource), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AvatarAlt"/>.</summary>
    public static readonly BindableProperty AvatarAltProperty = BindableProperty.Create(nameof(AvatarAlt), typeof(string), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AvatarText"/>.</summary>
    public static readonly BindableProperty AvatarTextProperty = BindableProperty.Create(nameof(AvatarText), typeof(string), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Leading"/>.</summary>
    public static readonly BindableProperty LeadingProperty = BindableProperty.Create(nameof(Leading), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Files"/>.</summary>
    public static readonly BindableProperty FilesProperty = BindableProperty.Create(nameof(Files), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyChatMessage), null, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="AuthorLabel"/>.</summary>
    public static readonly BindableProperty AuthorLabelProperty = BindableProperty.Create(nameof(AuthorLabel), typeof(string), typeof(KlyChatMessage), null, propertyChanged: Restyle);

    private readonly Grid _container = new() { VerticalOptions = LayoutOptions.Start };
    private readonly ContentView _leadingHost = new() { VerticalOptions = LayoutOptions.Start };
    private readonly KlyAvatar _avatar = new() { Size = KlySize.Sm };
    private readonly Border _iconCircle = new() { StrokeThickness = 0, WidthRequest = 28, HeightRequest = 28, StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) } };
    private readonly VerticalStackLayout _wrapper = new() { Spacing = 4 };
    private readonly ContentView _header = new() { IsVisible = false };
    private readonly ContentView _files = new() { IsVisible = false };
    private readonly Border _bubble = new() { StrokeThickness = 0, IsVisible = false };
    private readonly Grid _bubbleBody = new() { RowSpacing = 4 };
    private readonly Label _text = new() { IsVisible = false };
    private readonly ContentView _content = new() { IsVisible = false };
    private readonly BoxView _cursor = new() { WidthRequest = 6, HeightRequest = 16, CornerRadius = 1, HorizontalOptions = LayoutOptions.Start, IsVisible = false };
    private readonly ContentView _actions = new() { IsVisible = false };

    /// <summary>Creates a message.</summary>
    public KlyChatMessage()
    {
        _iconCircle.Content = null;
        _leadingHost.Content = _avatar;
        _bubbleBody.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _bubbleBody.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        _bubbleBody.Children.Add(_text);
        _bubbleBody.Children.Add(_content);
        _bubbleBody.Children.Add(_cursor);
        ChatDataGrid.Row(_cursor, 1);
        _bubble.Content = _bubbleBody;
        _wrapper.Children.Add(_header);
        _wrapper.Children.Add(_files);
        _wrapper.Children.Add(_bubble);
        _wrapper.Children.Add(_actions);
        _container.Children.Add(_leadingHost);
        _container.Children.Add(_wrapper);
        ColumnDefinitions.Add(new ColumnDefinition(new GridLength(1, GridUnitType.Star)));
        ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));
        Children.Add(_container);
        ApplyStyle();
    }

    /// <summary>Who wrote it: sets the default side and look, and the accessible name of the avatar.</summary>
    public ChatRole Role { get => (ChatRole)GetValue(RoleProperty); set => SetValue(RoleProperty, value); }

    /// <summary>The message as plain text, line breaks kept. Ignored when <see cref="Content"/> is set.</summary>
    public string? Text { get => (string?)GetValue(TextProperty); set => SetValue(TextProperty, value); }

    /// <summary>The message as a view: rendered markdown, an image, a chart. Style its text yourself: nothing is inherited from the bubble.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Marks the message as still arriving: a cursor pulses under the text. <see cref="KlyChatMessages"/> announces it once it is complete.</summary>
    public bool Streaming { get => (bool)GetValue(StreamingProperty); set => SetValue(StreamingProperty, value); }

    /// <summary>The side of the conversation. A user message sits on the right, the others on the left.</summary>
    public ChatSide? Side { get => (ChatSide?)GetValue(SideProperty); set => SetValue(SideProperty, value); }

    /// <summary>The surface of the message. A user message is soft, the others have none.</summary>
    public ChatMessageVariant? Variant { get => (ChatMessageVariant?)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Semantic color of the bubble.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Tighter spacing, for narrow panels such as <see cref="KlyChatPalette"/>.</summary>
    public bool Compact { get => (bool)GetValue(CompactProperty); set => SetValue(CompactProperty, value); }

    /// <summary>Image of the author; the initials of <see cref="AvatarText"/> show when there is none.</summary>
    public ImageSource? AvatarSource { get => (ImageSource?)GetValue(AvatarSourceProperty); set => SetValue(AvatarSourceProperty, value); }

    /// <summary>Name of the author for the avatar. Defaults to the name of the role.</summary>
    public string? AvatarAlt { get => (string?)GetValue(AvatarAltProperty); set => SetValue(AvatarAltProperty, value); }

    /// <summary>Initials of the avatar when there is no picture.</summary>
    public string? AvatarText { get => (string?)GetValue(AvatarTextProperty); set => SetValue(AvatarTextProperty, value); }

    /// <summary>A view, such as a <see cref="KlyIcon"/>, in place of the avatar picture.</summary>
    public View? Icon { get => (View?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Replaces the avatar altogether. Without any of the avatar properties nothing is shown.</summary>
    public View? Leading { get => (View?)GetValue(LeadingProperty); set => SetValue(LeadingProperty, value); }

    /// <summary>Above the message: a name, a time.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Attachments of the message, above the text: a layout of <see cref="KlyChatAttachment"/> items.</summary>
    public View? Files { get => (View?)GetValue(FilesProperty); set => SetValue(FilesProperty, value); }

    /// <summary>Below the message: copy, retry, thumbs.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    /// <summary>Name of the author, for the avatar of a screen reader. Defaults to <c>You</c>, <c>Assistant</c> or <c>System</c> by <see cref="Role"/>: set it in the language of the app.</summary>
    public string? AuthorLabel { get => (string?)GetValue(AuthorLabelProperty); set => SetValue(AuthorLabelProperty, value); }

    internal ChatSide EffectiveSide => Side ?? (Role == ChatRole.User ? ChatSide.Right : ChatSide.Left);
    internal ChatMessageVariant EffectiveVariant => Variant ?? (Role == ChatRole.User ? ChatMessageVariant.Soft : ChatMessageVariant.Naked);
    internal string Author => AuthorLabel ?? Role switch { ChatRole.User => "You", ChatRole.System => "System", _ => "Assistant" };
    internal bool HasLeading => Leading is not null || Icon is not null || AvatarSource is not null || AvatarAlt is not null || AvatarText is not null;
    internal Border Bubble => _bubble;
    internal Label TextLabel => _text;
    internal BoxView Cursor => _cursor;
    internal KlyAvatar Avatar => _avatar;
    internal Grid Container => _container;
    internal ContentView LeadingHost => _leadingHost;

    /// <inheritdoc />
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        SyncMotion();
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyChatMessage)bindable).ApplyStyle();

    private void ApplyStyle()
    {
        var system = Role == ChatRole.System;
        var side = system ? (ChatSide?)null : EffectiveSide;
        var variant = EffectiveVariant;
        var set = KlyTheme.Resolve(Color);
        var author = Author;

        // The avatar: a view of the app, an icon in a circle, or the picture or initials.
        _avatar.Source = AvatarSource;
        _avatar.Text = AvatarText;
        _avatar.Alt = AvatarAlt ?? author;
        _iconCircle.SetThemeColor(BackgroundColorProperty, KlyTheme.BackgroundAccented);
        SemanticProperties.SetDescription(_iconCircle, AvatarAlt ?? author);
        if (_iconCircle.Content != Icon) _iconCircle.Content = Icon;
        View avatar = Leading ?? (Icon is not null ? _iconCircle : _avatar);
        if (_leadingHost.Content != avatar) _leadingHost.Content = avatar;
        _leadingHost.IsVisible = HasLeading;

        // The side: a user message in the last three quarters of the line, a system notice in the middle.
        var align = system ? LayoutOptions.Center : side == ChatSide.Right ? LayoutOptions.End : LayoutOptions.Start;
        HorizontalOptions = LayoutOptions.Fill;
        _container.HorizontalOptions = align;
        _wrapper.HorizontalOptions = align;
        foreach (var part in new View[] { _header, _files, _bubble, _actions }) part.HorizontalOptions = align;
        ChatDataGrid.Column(_container, side == ChatSide.Right ? 1 : 0);
        ChatDataGrid.ColumnSpan(_container, side == ChatSide.Right ? 1 : 2);
        // The gap between the avatar and the text is a margin of the avatar, so a message without one has no empty gap.
        var gap = Compact ? 8d : 12d;
        _leadingHost.Margin = side == ChatSide.Right ? new Thickness(gap, 0, 0, 0) : new Thickness(0, 0, gap, 0);
        _container.ColumnDefinitions.Clear();
        _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        _container.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        var leadingColumn = side == ChatSide.Right ? 1 : 0;
        ChatDataGrid.Column(_leadingHost, leadingColumn);
        ChatDataGrid.Column(_wrapper, 1 - leadingColumn);

        // The parts around the bubble.
        Show(_header, Header);
        Show(_files, Files);
        Show(_actions, Actions);
        _actions.Content = Actions;

        // The bubble: the surface of the variant around the text or the content.
        var surface = variant switch
        {
            ChatMessageVariant.Solid => new Surface(set.Fill, set.Text, 1, set.Contrast),
            ChatMessageVariant.Outline => new Surface(KlyThemeColor.Transparent, set.Text.WithAlpha(0.75f), 1, KlyTheme.Text),
            ChatMessageVariant.Soft => new Surface(set.Fill.WithAlpha(0.1f), KlyThemeColor.Transparent, 0, KlyTheme.Text),
            ChatMessageVariant.Subtle => new Surface(set.Fill.WithAlpha(0.1f), set.Text.WithAlpha(0.75f), 1, KlyTheme.Text),
            ChatMessageVariant.Naked => new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.Text),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), variant, null),
        };
        _bubble.Apply(surface);
        _bubble.SetRadius(KlyTheme.RadiusOf(1));
        _bubble.Padding = variant == ChatMessageVariant.Naked ? new Thickness(0, 4) : Compact ? new Thickness(10, 6) : new Thickness(12, 8);
        _bubble.IsVisible = Content is not null || Text is not null;

        var textColor = system ? KlyTheme.TextMuted : surface.Text;
        _text.Text = Text;
        _text.FontSize = system ? 12 : 14;
        _text.HorizontalTextAlignment = system ? TextAlignment.Center : TextAlignment.Start;
        _text.SetThemeColor(Label.TextColorProperty, textColor);
        _text.IsVisible = Content is null && Text is not null;
        _content.Content = Content;
        _content.IsVisible = Content is not null;
        _cursor.SetThemeColor(BoxView.ColorProperty, textColor);
        _cursor.IsVisible = Streaming && _bubble.IsVisible;
        SyncMotion();
    }

    private static void Show(ContentView host, View? view)
    {
        host.Content = view;
        host.IsVisible = view is not null;
    }

    // The cursor of a streaming message pulses while the message is on screen.
    private void SyncMotion()
    {
        ChatDataMotion.Stop(_cursor, CursorName);
        _cursor.Opacity = 1;
        if (Handler is not null && Streaming) ChatDataMotion.Loop(_cursor, CursorName, cycle => _cursor.Opacity = ChatDataMotion.Pulse(cycle), 2000);
    }
}
