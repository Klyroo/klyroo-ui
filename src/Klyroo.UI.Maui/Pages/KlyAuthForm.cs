using System.Globalization;
using System.Text.RegularExpressions;

namespace Klyroo.UI.Maui;

/// <summary>
/// A sign-in, sign-up or password reset form built from a list of <see cref="Fields"/>, with optional provider buttons, validation and a submit button. The fields are
/// <see cref="KlyInput"/>, <see cref="KlySelect"/> and <see cref="KlyCheckbox"/> of the library, or the native date and time pickers. Pressing the submit button checks the
/// values (required, email shape, one-time code length, then <see cref="Validate"/>) and raises <see cref="ValidSubmit"/> or <see cref="InvalidSubmit"/>.
/// </summary>
public class KlyAuthForm : ContentView
{
    /// <summary>Identifies <see cref="Fields"/>.</summary>
    public static readonly BindableProperty FieldsProperty = BindableProperty.Create(nameof(Fields), typeof(IReadOnlyList<KlyAuthField>), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).Rebuild());
    /// <summary>Identifies <see cref="Title"/>.</summary>
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowHeader());
    /// <summary>Identifies <see cref="HeadingLevel"/>.</summary>
    public static readonly BindableProperty HeadingLevelProperty = BindableProperty.Create(nameof(HeadingLevel), typeof(int), typeof(KlyAuthForm), 2, propertyChanged: (b, _, v) => PagesUi.Heading(((KlyAuthForm)b)._title, (int)v));
    /// <summary>Identifies <see cref="Description"/>.</summary>
    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(nameof(Description), typeof(string), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowHeader());
    /// <summary>Identifies <see cref="Icon"/>.</summary>
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(string), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowHeader());
    /// <summary>Identifies <see cref="Header"/>.</summary>
    public static readonly BindableProperty HeaderProperty = BindableProperty.Create(nameof(Header), typeof(View), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowHeader());
    /// <summary>Identifies <see cref="Providers"/>.</summary>
    public static readonly BindableProperty ProvidersProperty = BindableProperty.Create(nameof(Providers), typeof(IReadOnlyList<KlyAuthProvider>), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowProviders());
    /// <summary>Identifies <see cref="Separator"/>.</summary>
    public static readonly BindableProperty SeparatorProperty = BindableProperty.Create(nameof(Separator), typeof(string), typeof(KlyAuthForm), "or", propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowProviders());
    /// <summary>Identifies <see cref="SubmitLabel"/>.</summary>
    public static readonly BindableProperty SubmitLabelProperty = BindableProperty.Create(nameof(SubmitLabel), typeof(string), typeof(KlyAuthForm), "Continue", propertyChanged: (b, _, v) => ((KlyAuthForm)b)._submit.Text = (string?)v);
    /// <summary>Identifies <see cref="Loading"/>.</summary>
    public static readonly BindableProperty LoadingProperty = BindableProperty.Create(nameof(Loading), typeof(bool), typeof(KlyAuthForm), false, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowBusy());
    /// <summary>Identifies <see cref="Error"/>.</summary>
    public static readonly BindableProperty ErrorProperty = BindableProperty.Create(nameof(Error), typeof(string), typeof(KlyAuthForm), null, propertyChanged: (b, _, v) => ((KlyAuthForm)b).ShowError((string?)v));
    /// <summary>Identifies <see cref="FieldErrors"/>.</summary>
    public static readonly BindableProperty FieldErrorsProperty = BindableProperty.Create(nameof(FieldErrors), typeof(IReadOnlyDictionary<string, string>), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowFieldErrors());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyAuthForm), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyAuthForm)b).Restyle());
    /// <summary>Identifies <see cref="Validation"/>.</summary>
    public static readonly BindableProperty ValidationProperty = BindableProperty.Create(nameof(Validation), typeof(View), typeof(KlyAuthForm), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyAuthForm)b)._validation, (View?)v));
    /// <summary>Identifies <see cref="Submit"/>.</summary>
    public static readonly BindableProperty SubmitProperty = BindableProperty.Create(nameof(Submit), typeof(View), typeof(KlyAuthForm), null, propertyChanged: (b, _, _) => ((KlyAuthForm)b).ShowSubmit());
    /// <summary>Identifies <see cref="Footer"/>.</summary>
    public static readonly BindableProperty FooterProperty = BindableProperty.Create(nameof(Footer), typeof(View), typeof(KlyAuthForm), null, propertyChanged: (b, _, v) => PagesUi.Put(((KlyAuthForm)b)._footer, (View?)v));
    /// <summary>Identifies <see cref="RequiredMessage"/>.</summary>
    public static readonly BindableProperty RequiredMessageProperty = BindableProperty.Create(nameof(RequiredMessage), typeof(string), typeof(KlyAuthForm), "{0} is required.");
    /// <summary>Identifies <see cref="InvalidEmailMessage"/>.</summary>
    public static readonly BindableProperty InvalidEmailMessageProperty = BindableProperty.Create(nameof(InvalidEmailMessage), typeof(string), typeof(KlyAuthForm), "Enter a valid email address.");
    /// <summary>Identifies <see cref="IncompleteCodeMessage"/>.</summary>
    public static readonly BindableProperty IncompleteCodeMessageProperty = BindableProperty.Create(nameof(IncompleteCodeMessage), typeof(string), typeof(KlyAuthForm), "Enter all {0} characters.");
    /// <summary>Identifies <see cref="ShowPasswordLabel"/>.</summary>
    public static readonly BindableProperty ShowPasswordLabelProperty = BindableProperty.Create(nameof(ShowPasswordLabel), typeof(string), typeof(KlyAuthForm), "Show password");

    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(200));
    private const int MaxEmailLength = 254;

    private readonly Dictionary<string, string?> _values = new(StringComparer.Ordinal);
    private Dictionary<string, string> _errors = new(StringComparer.Ordinal);
    private readonly List<FieldView> _views = [];
    private readonly ContentView _headerHost = PagesUi.Slot();
    private readonly Border _leading = new() { IsVisible = false, WidthRequest = 40, HeightRequest = 40, StrokeThickness = 0, HorizontalOptions = LayoutOptions.Center };
    private readonly KlyIcon _icon = new() { Size = KlySize.Md, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center };
    private readonly Label _title = PagesUi.Text(20, KlyTheme.TextHighlighted, FontAttributes.Bold);
    private readonly Label _description = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly VerticalStackLayout _defaultHeader = new() { Spacing = 8 };
    private readonly VerticalStackLayout _providers = new() { Spacing = 12, IsVisible = false };
    private readonly Grid _separator = new() { ColumnSpacing = 12, IsVisible = false, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
    private readonly Label _separatorLabel = PagesUi.Text(14, KlyTheme.TextMuted);
    private readonly KlyAlert _alert = new() { Color = KlyColor.Error, Variant = KlyVariant.Soft, IsVisible = false };
    private readonly VerticalStackLayout _fields = new() { Spacing = 20 };
    private readonly ContentView _validation = PagesUi.Slot();
    private readonly ContentView _submitSlot = new();
    private readonly KlyButton _submit = new() { Block = true, Size = KlySize.Lg, Text = "Continue" };
    private readonly VerticalStackLayout _form = new() { Spacing = 20, IsVisible = false };
    private readonly ContentView _footer = PagesUi.Slot();
    private readonly VerticalStackLayout _root;

    /// <summary>Creates an auth form.</summary>
    public KlyAuthForm()
    {
        _leading.SetRadius(20);
        _leading.Content = _icon;
        AutomationProperties.SetIsInAccessibleTree(_leading, false);
        _title.HorizontalTextAlignment = TextAlignment.Center;
        _description.HorizontalTextAlignment = TextAlignment.Center;
        _defaultHeader.Children.Add(_leading);
        _defaultHeader.Children.Add(_title);
        _defaultHeader.Children.Add(_description);
        PagesUi.Heading(_title, HeadingLevel);

        _separator.Add(new KlySeparator { VerticalOptions = LayoutOptions.Center }, 0, 0);
        _separator.Add(_separatorLabel, 1, 0);
        _separator.Add(new KlySeparator { VerticalOptions = LayoutOptions.Center }, 2, 0);
        _footer.HorizontalOptions = LayoutOptions.Center;

        _submitSlot.Content = _submit;
        _submit.Clicked += (_, _) => SubmitForm();
        _form.Children.Add(_alert);
        _form.Children.Add(_fields);
        _form.Children.Add(_validation);
        _form.Children.Add(_submitSlot);
        _root = new VerticalStackLayout { Spacing = 24, MaximumWidthRequest = 384, HorizontalOptions = LayoutOptions.Fill, Children = { _headerHost, _providers, _separator, _form, _footer } };
        Content = _root;
        Restyle();
    }

    /// <summary>Raised once when the form is submitted and every value is valid, with the values by field name.</summary>
    public event EventHandler<KlyAuthFormSubmitEventArgs>? ValidSubmit;

    /// <summary>Raised when the form is submitted and validation fails, with the current values by field name.</summary>
    public event EventHandler<KlyAuthFormSubmitEventArgs>? InvalidSubmit;

    /// <summary>Raised when the user presses a provider button. <see cref="PageLinkEventArgs.Item"/> is the <see cref="KlyAuthProvider"/>; an address nobody handled opens in the system browser.</summary>
    public event EventHandler<PageLinkEventArgs>? ProviderClicked;

    /// <summary>The fields, in order.</summary>
    public IReadOnlyList<KlyAuthField>? Fields { get => (IReadOnlyList<KlyAuthField>?)GetValue(FieldsProperty); set => SetValue(FieldsProperty, value); }

    /// <summary>Title of the form, such as "Sign in".</summary>
    public string? Title { get => (string?)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>Level of the title heading for a screen reader: 2 by default.</summary>
    public int HeadingLevel { get => (int)GetValue(HeadingLevelProperty); set => SetValue(HeadingLevelProperty, value); }

    /// <summary>Text under the title of the form.</summary>
    public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>) above the title.</summary>
    public string? Icon { get => (string?)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Replaces the title block.</summary>
    public View? Header { get => (View?)GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <summary>Buttons that leave for another address (OAuth), above the fields.</summary>
    public IReadOnlyList<KlyAuthProvider>? Providers { get => (IReadOnlyList<KlyAuthProvider>?)GetValue(ProvidersProperty); set => SetValue(ProvidersProperty, value); }

    /// <summary>Text between the providers and the fields: "or" by default, in the language of the app. An empty string hides the line.</summary>
    public string Separator { get => (string)GetValue(SeparatorProperty); set => SetValue(SeparatorProperty, value); }

    /// <summary>Label of the submit button: "Continue" by default, in the language of the app.</summary>
    public string SubmitLabel { get => (string)GetValue(SubmitLabelProperty); set => SetValue(SubmitLabelProperty, value); }

    /// <summary>Disables the fields and the submit button while the app handles the submit. The control does not know when the app is done: set it back to <c>false</c>.</summary>
    public bool Loading { get => (bool)GetValue(LoadingProperty); set => SetValue(LoadingProperty, value); }

    /// <summary>A message about the whole form (wrong password): put it here after the app handled <see cref="ValidSubmit"/> and failed.</summary>
    public string? Error { get => (string?)GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Messages by field name, for what only the server can tell (the email is taken). Shown after the built-in ones.</summary>
    public IReadOnlyDictionary<string, string>? FieldErrors { get => (IReadOnlyDictionary<string, string>?)GetValue(FieldErrorsProperty); set => SetValue(FieldErrorsProperty, value); }

    /// <summary>Extra validation after the required, email and code checks, with the values by field name. Returns messages by field name.</summary>
    public Func<IReadOnlyDictionary<string, string?>, IReadOnlyDictionary<string, string>?>? Validate { get; set; }

    /// <summary>Semantic color of the form: the leading icon and the submit button.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Content between the fields and the submit button (a "forgot password" link).</summary>
    public View? Validation { get => (View?)GetValue(ValidationProperty); set => SetValue(ValidationProperty, value); }

    /// <summary>Replaces the submit button.</summary>
    public View? Submit { get => (View?)GetValue(SubmitProperty); set => SetValue(SubmitProperty, value); }

    /// <summary>Content under the submit button, such as a link to another form.</summary>
    public View? Footer { get => (View?)GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Message of a required field left empty, with the label of the field as <c>{0}</c>, in the language of the app.</summary>
    public string RequiredMessage { get => (string)GetValue(RequiredMessageProperty); set => SetValue(RequiredMessageProperty, value); }

    /// <summary>Message of an email field that does not hold an address, in the language of the app.</summary>
    public string InvalidEmailMessage { get => (string)GetValue(InvalidEmailMessageProperty); set => SetValue(InvalidEmailMessageProperty, value); }

    /// <summary>Message of a one-time code that is too short, with the length as <c>{0}</c>, in the language of the app.</summary>
    public string IncompleteCodeMessage { get => (string)GetValue(IncompleteCodeMessageProperty); set => SetValue(IncompleteCodeMessageProperty, value); }

    /// <summary>What a screen reader says for the button that shows a password, in the language of the app.</summary>
    public string ShowPasswordLabel { get => (string)GetValue(ShowPasswordLabelProperty); set => SetValue(ShowPasswordLabelProperty, value); }

    /// <summary>The current values by field name. A checkbox is <c>"true"</c> or <c>"false"</c>.</summary>
    public IReadOnlyDictionary<string, string?> Values => _values;

    internal bool Busy => Loading;

    internal VerticalStackLayout FieldStack => _fields;

    internal VerticalStackLayout ProviderStack => _providers;

    internal Grid SeparatorRow => _separator;

    internal Label SeparatorLabel => _separatorLabel;

    internal KlyAlert Alert => _alert;

    internal KlyButton SubmitButton => _submit;

    internal VerticalStackLayout DefaultHeader => _defaultHeader;

    internal Border Leading => _leading;

    internal Label TitleLabel => _title;

    internal IReadOnlyList<string> ErrorTexts => _views.Select(v => v.ErrorText.IsVisible ? v.ErrorText.Text : "").ToList();

    internal View? ControlOf(string name) => _views.FirstOrDefault(v => v.Field.Name == name)?.Control;

    internal void SubmitForm()
    {
        if (Busy) return;
        _errors = Check();
        ShowFieldErrors();
        var values = new Dictionary<string, string?>(_values, StringComparer.Ordinal);
        if (_errors.Count > 0)
        {
            InvalidSubmit?.Invoke(this, new KlyAuthFormSubmitEventArgs(values));
            return;
        }
        ValidSubmit?.Invoke(this, new KlyAuthFormSubmitEventArgs(values));
    }

    private static bool IsEmail(string value)
    {
        if (value.Length > MaxEmailLength) return false;
        try
        {
            return EmailPattern.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private Dictionary<string, string> Check()
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var field in Fields ?? [])
        {
            var value = _values.GetValueOrDefault(field.Name);
            var label = field.Label ?? field.Name;
            var empty = field.Type == KlyAuthFieldType.Checkbox ? value != "true" : string.IsNullOrWhiteSpace(value);
            if (empty)
            {
                if (field.Required) errors[field.Name] = string.Format(CultureInfo.CurrentCulture, RequiredMessage, label);
            }
            else if (field.Type == KlyAuthFieldType.Email && !IsEmail(value!.Trim()))
            {
                errors[field.Name] = InvalidEmailMessage;
            }
            else if (field.Type == KlyAuthFieldType.Otp && value!.Length < field.Length)
            {
                errors[field.Name] = string.Format(CultureInfo.CurrentCulture, IncompleteCodeMessage, field.Length);
            }
        }
        if (errors.Count == 0 && Validate?.Invoke(_values) is { } custom)
        {
            foreach (var (name, message) in custom) errors[name] = message;
        }
        return errors;
    }

    private string? ErrorOf(KlyAuthField field) => _errors.GetValueOrDefault(field.Name) ?? FieldErrors?.GetValueOrDefault(field.Name);

    private void Set(KlyAuthField field, string? value) => _values[field.Name] = value;

    // The title block: a round icon, the title and the description, or the view that replaces them.
    private void ShowHeader()
    {
        _icon.Name = Icon ?? "";
        _leading.IsVisible = !string.IsNullOrEmpty(Icon);
        PagesUi.Show(_title, Title);
        PagesUi.Show(_description, Description);
        View? header = Header;
        if (header is null && (_leading.IsVisible || _title.IsVisible || _description.IsVisible)) header = _defaultHeader;
        PagesUi.Put(_headerHost, header);
    }

    private void ShowProviders()
    {
        _providers.Children.Clear();
        var providers = Providers ?? [];
        _providers.IsVisible = providers.Count > 0;
        foreach (var provider in providers)
        {
            var button = new KlyButton { Text = provider.Label, Block = true, Variant = ButtonVariant.Outline, Color = KlyColor.Neutral, Size = KlySize.Lg };
            button.Clicked += (_, _) => PagesLinks.Follow(this, ProviderClicked, provider.Href, provider);
            _providers.Children.Add(button);
        }
        PagesUi.Show(_separatorLabel, Separator);
        _separator.IsVisible = providers.Count > 0 && (Fields?.Count ?? 0) > 0 && Separator != string.Empty;
    }

    private void ShowBusy()
    {
        _fields.IsEnabled = !Busy;
        _submit.IsEnabled = !Busy;
        // The button of the app is its own: it follows the form too, as the web fieldset and button do.
        if (Submit is { } custom) custom.IsEnabled = !Busy;
    }

    private void ShowError(string? message)
    {
        _alert.Title = message;
        _alert.IsVisible = !string.IsNullOrEmpty(message);
    }

    private void ShowSubmit()
    {
        _submitSlot.Content = Submit ?? _submit;
        ShowBusy();
    }

    private void Restyle()
    {
        var set = KlyTheme.Resolve(Color);
        _leading.SetThemeColor(BackgroundColorProperty, set.Fill.WithAlpha(0.10f));
        _icon.IconColor = set.Text;
        _submit.Color = Color;
    }

    private void ShowFieldErrors()
    {
        foreach (var view in _views)
        {
            var error = ErrorOf(view.Field);
            PagesUi.Show(view.ErrorText, error);
            view.Help.IsVisible = error is null && !string.IsNullOrEmpty(view.Field.Help);
            if (view.Control is KlyFieldBase input) input.Invalid = error is not null;
            else if (view.Control is Grid { Children.Count: > 0 } wrapper && wrapper.Children[0] is KlyFieldBase inner) inner.Invalid = error is not null;
        }
    }

    private void Rebuild()
    {
        _fields.Children.Clear();
        _views.Clear();
        _values.Clear();
        _errors = new Dictionary<string, string>(StringComparer.Ordinal);
        var fields = Fields ?? [];
        _form.IsVisible = fields.Count > 0;
        foreach (var field in fields)
        {
            _values.TryAdd(field.Name, field.Value);
            var view = FieldBlock(field);
            _views.Add(view);
            _fields.Children.Add(view.Root);
        }
        WireReturnKey();
        ShowProviders();
        ShowFieldErrors();
    }

    // The return key of the keyboard goes to the next text field, and submits from the last one, as Enter does in a web form.
    private void WireReturnKey()
    {
        var inputs = _views.Select(v => v.Control as KlyInput ?? (v.Control as Grid)?.Children.OfType<KlyInput>().FirstOrDefault()).OfType<KlyInput>().ToList();
        for (var i = 0; i < inputs.Count; i++)
        {
            var last = i == inputs.Count - 1;
            if (inputs[i].ReturnType == ReturnType.Default) inputs[i].ReturnType = last ? ReturnType.Done : ReturnType.Next;
            if (last) inputs[i].Completed += (_, _) => SubmitForm();
        }
    }

    private sealed record FieldView(KlyAuthField Field, VerticalStackLayout Root, View Control, Label ErrorText, Label Help);

    private FieldView FieldBlock(KlyAuthField field)
    {
        var control = Control(field);
        var checkbox = field.Type == KlyAuthFieldType.Checkbox;

        var root = new VerticalStackLayout { Spacing = 6 };
        if (!checkbox)
        {
            var label = PagesUi.Text(14, KlyTheme.TextHighlighted);
            label.Text = field.Required ? $"{field.Label ?? field.Name} *" : field.Label ?? field.Name;
            label.IsVisible = true;
            root.Children.Add(label);
        }
        var description = PagesUi.Text(14, KlyTheme.TextMuted);
        PagesUi.Show(description, field.Description);
        root.Children.Add(description);
        root.Children.Add(control);
        var help = PagesUi.Text(12, KlyTheme.TextMuted);
        PagesUi.Show(help, field.Help);
        root.Children.Add(help);
        // The message of an error is announced when it appears: it is a live region on the web, a plain label here.
        var error = PagesUi.Text(12, KlyTheme.Resolve(KlyColor.Error).Text);
        root.Children.Add(error);
        return new FieldView(field, root, control, error, help);
    }

    private View Control(KlyAuthField f)
    {
        var name = f.Label ?? f.Name;
        switch (f.Type)
        {
            case KlyAuthFieldType.Checkbox:
            {
                var box = new KlyCheckbox { Text = f.Label ?? f.Name, IsChecked = _values.GetValueOrDefault(f.Name) == "true" };
                box.CheckedChanged += (_, e) => Set(f, e.Value ? "true" : "false");
                _values[f.Name] = box.IsChecked ? "true" : "false";
                return box;
            }
            case KlyAuthFieldType.Select:
            {
                var select = new KlySelect { ItemsSource = f.Items.ToList(), Placeholder = f.Placeholder, SelectedItem = _values.GetValueOrDefault(f.Name), AccessibleName = name };
                select.SelectedItemChanged += (_, _) => Set(f, select.SelectedItem as string);
                return select;
            }
            case KlyAuthFieldType.Date:
            {
                var date = DateTime.TryParseExact(_values.GetValueOrDefault(f.Name), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : DateTime.Today;
                var picker = new DatePicker { Date = date };
                _values[f.Name] = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                picker.DateSelected += (_, e) => Set(f, IsoDate(e.NewDate));
                SemanticProperties.SetDescription(picker, name);
                return new PagesPickerField(picker);
            }
            case KlyAuthFieldType.Time:
            {
                var time = TimeSpan.TryParseExact(_values.GetValueOrDefault(f.Name), "hh\\:mm", CultureInfo.InvariantCulture, out var parsed) ? parsed : DateTime.Now.TimeOfDay;
                var picker = new TimePicker { Time = new TimeSpan(time.Hours, time.Minutes, 0) };
                _values[f.Name] = Clock(picker.Time);
                picker.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(TimePicker.Time)) Set(f, Clock(picker.Time));
                };
                SemanticProperties.SetDescription(picker, name);
                return new PagesPickerField(picker);
            }
            case KlyAuthFieldType.Password:
                return PasswordField(f, name);
            default:
            {
                var input = new KlyInput
                {
                    Placeholder = f.Placeholder,
                    AccessibleName = name,
                    Text = _values.GetValueOrDefault(f.Name),
                    Keyboard = f.Type switch
                    {
                        KlyAuthFieldType.Email => Keyboard.Email,
                        KlyAuthFieldType.Tel => Keyboard.Telephone,
                        KlyAuthFieldType.Url => Keyboard.Url,
                        KlyAuthFieldType.Number or KlyAuthFieldType.Otp => Keyboard.Numeric,
                        _ => Keyboard.Default,
                    },
                    ReturnType = f.Type == KlyAuthFieldType.Search ? ReturnType.Search : ReturnType.Default,
                };
                if (f.Type == KlyAuthFieldType.Otp) input.MaxLength = f.Length;
                input.TextChanged += (_, e) => Set(f, e.NewTextValue);
                return input;
            }
        }
    }

    private static string IsoDate(DateTime? date) => (date ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Clock(TimeSpan? time) => (time ?? TimeSpan.Zero).ToString("hh\\:mm", CultureInfo.InvariantCulture);

    // A password input with a button over its end that shows or hides the characters.
    private Grid PasswordField(KlyAuthField f, string name)
    {
        var input = new KlyInput { Placeholder = f.Placeholder, AccessibleName = name, IsPassword = true, Text = _values.GetValueOrDefault(f.Name) };
        input.TextChanged += (_, e) => Set(f, e.NewTextValue);

        var icon = PagesUi.Icon(KlyIconNames.Eye, KlySize.Md, KlyTheme.TextMuted);
        icon.HorizontalOptions = LayoutOptions.Center;
        icon.VerticalOptions = LayoutOptions.Center;
        var toggle = new ContentView { WidthRequest = 44, HeightRequest = 40, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Center, Content = icon };
        SemanticProperties.SetDescription(toggle, ShowPasswordLabel);
        PagesUi.Tappable(toggle, () =>
        {
            input.IsPassword = !input.IsPassword;
            icon.Name = input.IsPassword ? KlyIconNames.Eye : KlyIconNames.EyeOff;
        });
        return new Grid { Children = { input, toggle } };
    }
}
