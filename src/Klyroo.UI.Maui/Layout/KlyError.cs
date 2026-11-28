namespace Klyroo.UI.Maui;

/// <summary>
/// The content of an error page: a status code, a title, a message and a way back. Use it for a page that is not found, a route that is refused or an unhandled exception.
/// 404, 401, 403 and 500 have built-in English texts; set <see cref="StatusMessage"/>, <see cref="Message"/> and <see cref="ClearLabel"/> to translate them.
/// </summary>
[ContentProperty(nameof(ChildContent))]
public class KlyError : ContentView
{
    /// <summary>Identifies <see cref="StatusCode"/>.</summary>
    public static readonly BindableProperty StatusCodeProperty = BindableProperty.Create(nameof(StatusCode), typeof(int?), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="StatusMessage"/>.</summary>
    public static readonly BindableProperty StatusMessageProperty = BindableProperty.Create(nameof(StatusMessage), typeof(string), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Message"/>.</summary>
    public static readonly BindableProperty MessageProperty = BindableProperty.Create(nameof(Message), typeof(string), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Exception"/>.</summary>
    public static readonly BindableProperty ExceptionProperty = BindableProperty.Create(nameof(Exception), typeof(Exception), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="ShowDetails"/>.</summary>
    public static readonly BindableProperty ShowDetailsProperty = BindableProperty.Create(nameof(ShowDetails), typeof(bool), typeof(KlyError), false, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Clear"/>.</summary>
    public static readonly BindableProperty ClearProperty = BindableProperty.Create(nameof(Clear), typeof(bool), typeof(KlyError), true, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="ClearLabel"/>.</summary>
    public static readonly BindableProperty ClearLabelProperty = BindableProperty.Create(nameof(ClearLabel), typeof(string), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyError), 1, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="ChildContent"/>.</summary>
    public static readonly BindableProperty ChildContentProperty = BindableProperty.Create(nameof(ChildContent), typeof(View), typeof(KlyError), null, propertyChanged: Refresh);
    /// <summary>Identifies <see cref="Actions"/>.</summary>
    public static readonly BindableProperty ActionsProperty = BindableProperty.Create(nameof(Actions), typeof(View), typeof(KlyError), null, propertyChanged: Refresh);

    private readonly VerticalStackLayout _stack = new() { Spacing = 12 };
    private readonly Label _code = new() { FontSize = 16, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _title = new() { FontSize = 36, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center };
    private readonly Label _message = new() { FontSize = 16, HorizontalTextAlignment = TextAlignment.Center, HorizontalOptions = LayoutOptions.Center, MaximumWidthRequest = 520 };
    private readonly KlyButton _clear = new() { HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 24, 0, 0) };

    /// <summary>Creates an error.</summary>
    public KlyError()
    {
        Padding = new Thickness(16, 64);
        VerticalOptions = LayoutOptions.Center;
        Content = _stack;
        _code.SetThemeColor(Label.TextColorProperty, KlyTheme.Resolve(KlyColor.Primary).Text);
        _title.SetThemeColor(Label.TextColorProperty, KlyTheme.TextHighlighted);
        _message.SetThemeColor(Label.TextColorProperty, KlyTheme.TextMuted);
        _clear.Clicked += (_, _) => ClearClicked?.Invoke(this, EventArgs.Empty);
        Rebuild();
    }

    /// <summary>Raised when the user presses the button that leads back, so the app can navigate home.</summary>
    public event EventHandler? ClearClicked;

    /// <summary>The HTTP style status code, shown above the title. 404, 401, 403 and 500 have built-in texts.</summary>
    public int? StatusCode { get => (int?)GetValue(StatusCodeProperty); set => SetValue(StatusCodeProperty, value); }

    /// <summary>The title. Defaults to a text for <see cref="StatusCode"/>.</summary>
    public string? StatusMessage { get => (string?)GetValue(StatusMessageProperty); set => SetValue(StatusMessageProperty, value); }

    /// <summary>The explanation under the title. Defaults to a text for <see cref="StatusCode"/>.</summary>
    public string? Message { get => (string?)GetValue(MessageProperty); set => SetValue(MessageProperty, value); }

    /// <summary>The exception to report. Sets the status code to 500 when none is given.</summary>
    public Exception? Exception { get => (Exception?)GetValue(ExceptionProperty); set => SetValue(ExceptionProperty, value); }

    /// <summary>Shows the message of <see cref="Exception"/>. Leave it off in production: exception messages can leak internals.</summary>
    public bool ShowDetails { get => (bool)GetValue(ShowDetailsProperty); set => SetValue(ShowDetailsProperty, value); }

    /// <summary>Shows the button that leads back and raises <see cref="ClearClicked"/> (default). Ignored when <see cref="Actions"/> is set.</summary>
    public bool Clear { get => (bool)GetValue(ClearProperty); set => SetValue(ClearProperty, value); }

    /// <summary>Text of the back button. Defaults to "Back to home".</summary>
    public string? ClearLabel { get => (string?)GetValue(ClearLabelProperty); set => SetValue(ClearLabelProperty, value); }

    /// <summary>Level of the title heading for a screen reader, 1 to 6: 1 by default, 2 when the error replaces a part of a page.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Extra content between the message and the actions.</summary>
    public View? ChildContent { get => (View?)GetValue(ChildContentProperty); set => SetValue(ChildContentProperty, value); }

    /// <summary>Replaces the back button.</summary>
    public View? Actions { get => (View?)GetValue(ActionsProperty); set => SetValue(ActionsProperty, value); }

    private static void Refresh(BindableObject bindable, object oldValue, object newValue) => ((KlyError)bindable).Rebuild();

    private void Rebuild()
    {
        var code = StatusCode ?? (Exception is not null ? 500 : (int?)null);
        var (title, message) = code switch
        {
            404 => ("Page not found", "The page you are looking for does not exist or has moved."),
            401 => ("Sign in required", "You need to sign in to view this page."),
            403 => ("Access denied", "You do not have permission to view this page."),
            _ => ("Something went wrong", "An unexpected error occurred. Please try again later."),
        };
        _code.Text = code?.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _title.Text = StatusMessage ?? title;
        _message.Text = Message ?? (ShowDetails && Exception is not null ? Exception.Message : message);
        _clear.Text = ClearLabel ?? "Back to home";
        SemanticProperties.SetHeadingLevel(_title, LayoutParts.Heading(HeadingLevel));

        // A view has one parent: take the children out before they are placed again.
        _stack.Children.Clear();
        if (code is not null) _stack.Add(_code);
        _stack.Add(_title);
        _stack.Add(_message);
        if (ChildContent is { } content) _stack.Add(content);
        if (Actions is { } actions)
        {
            actions.Margin = new Thickness(0, 24, 0, 0);
            _stack.Add(actions);
        }
        else if (Clear)
        {
            _stack.Add(_clear);
        }
    }
}
