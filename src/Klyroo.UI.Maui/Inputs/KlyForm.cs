using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Klyroo.UI.Maui;

/// <summary>
/// A container that validates a model with its data annotations and tells the <see cref="KlyFormField"/>s inside it which fields have an error. It validates the whole model on <see cref="Submit"/>,
/// and a field each time the model raises <see cref="INotifyPropertyChanged.PropertyChanged"/> for it; it can show the list of every message above its content.
/// </summary>
[ContentProperty(nameof(Content))]
public class KlyForm : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Model"/>.</summary>
    public static readonly BindableProperty ModelProperty = BindableProperty.Create(nameof(Model), typeof(object), typeof(KlyForm), null, propertyChanged: (b, o, v) => ((KlyForm)b).ShowModel(o, v));
    /// <summary>Identifies <see cref="Validate"/>.</summary>
    public static readonly BindableProperty ValidateProperty = BindableProperty.Create(nameof(Validate), typeof(bool), typeof(KlyForm), true);
    /// <summary>Identifies <see cref="ShowSummary"/>.</summary>
    public static readonly BindableProperty ShowSummaryProperty = BindableProperty.Create(nameof(ShowSummary), typeof(bool), typeof(KlyForm), false, propertyChanged: (b, _, _) => ((KlyForm)b).Show());
    /// <summary>Identifies <see cref="IsBusy"/>.</summary>
    public static readonly BindableProperty IsBusyProperty = BindableProperty.Create(nameof(IsBusy), typeof(bool), typeof(KlyForm), false, propertyChanged: (b, _, _) => ((KlyForm)b).ShowEnabled());
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyForm), null, propertyChanged: (b, _, v) => ((KlyForm)b)._host.Content = (View?)v);

    private readonly Dictionary<string, List<string>> _messages = new(StringComparer.Ordinal);
    private readonly List<string> _modelMessages = [];
    private readonly ContentView _host = new();
    private readonly Border _summary = new() { IsVisible = false, Padding = new Thickness(12), StrokeThickness = 1 };
    private INotifyPropertyChanged? _watched;

    /// <summary>Creates a form.</summary>
    public KlyForm()
    {
        Spacing = 16;
        _summary.SetRadius(KlyTheme.RadiusOf(1));
        var error = KlyTheme.Resolve(KlyColor.Error);
        _summary.SetThemeColor(BackgroundColorProperty, error.Fill.WithAlpha(0.10f));
        _summary.SetStroke(error.Fill);
        Children.Add(_summary);
        Children.Add(_host);
    }

    /// <summary>Raised with a valid form, when <see cref="Submit"/> is called.</summary>
    public event EventHandler? ValidSubmit;

    /// <summary>Raised when <see cref="Submit"/> is called and validation fails.</summary>
    public event EventHandler? InvalidSubmit;

    /// <summary>Raised when the validation messages change.</summary>
    public event EventHandler? ValidationChanged;

    /// <summary>The object edited, validated with its data annotations. When it implements <see cref="INotifyPropertyChanged"/>, a change of a property validates that property.</summary>
    public object? Model { get => GetValue(ModelProperty); set => SetValue(ModelProperty, value); }

    /// <summary>Runs data annotation validation on submit and on field changes. Defaults to <c>true</c>.</summary>
    public bool Validate { get => (bool)GetValue(ValidateProperty); set => SetValue(ValidateProperty, value); }

    /// <summary>Shows the list of every validation message above the fields.</summary>
    public bool ShowSummary { get => (bool)GetValue(ShowSummaryProperty); set => SetValue(ShowSummaryProperty, value); }

    /// <summary>The form is busy, such as while it saves: its fields are disabled. Set it from the handler of <see cref="ValidSubmit"/> and clear it when the work is done.</summary>
    public bool IsBusy { get => (bool)GetValue(IsBusyProperty); set => SetValue(IsBusyProperty, value); }

    /// <summary>The fields of the form.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Every validation message, the ones of the fields first.</summary>
    public IReadOnlyList<string> Messages => [.. _messages.Values.SelectMany(m => m), .. _modelMessages];

    internal Border Summary => _summary;

    internal ContentView Host => _host;

    /// <summary>The validation messages of a field, by the name of the property of the model.</summary>
    public IReadOnlyList<string> GetMessages(string field) => _messages.TryGetValue(field, out var list) ? list : [];

    /// <summary>Validates the whole model and shows the messages. Without a model, or with <see cref="Validate"/> off, the form is valid.</summary>
    /// <returns><c>true</c> when the model is valid.</returns>
    public bool ValidateModel()
    {
        _messages.Clear();
        _modelMessages.Clear();
        var valid = true;
        if (Validate && Model is { } model)
        {
            var results = new List<ValidationResult>();
            valid = Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            foreach (var result in results)
            {
                var text = result.ErrorMessage ?? string.Empty;
                var members = result.MemberNames.Where(name => !string.IsNullOrEmpty(name)).ToList();
                if (members.Count == 0) _modelMessages.Add(text);
                foreach (var member in members) Add(member, text);
            }
        }
        Changed();
        return valid;
    }

    /// <summary>Validates one property of the model and shows its messages. The messages of the other fields stay.</summary>
    /// <param name="field">The name of the property.</param>
    /// <returns><c>true</c> when the property is valid.</returns>
    public bool ValidateField(string field)
    {
        _messages.Remove(field);
        var valid = true;
        if (Validate && Model is { } model && model.GetType().GetProperty(field) is { } property)
        {
            var results = new List<ValidationResult>();
            valid = Validator.TryValidateProperty(property.GetValue(model), new ValidationContext(model) { MemberName = field }, results);
            foreach (var result in results) Add(field, result.ErrorMessage ?? string.Empty);
        }
        Changed();
        return valid;
    }

    /// <summary>Validates the model, then raises <see cref="ValidSubmit"/> or <see cref="InvalidSubmit"/>. Does nothing while the form is busy.</summary>
    /// <returns><c>true</c> when the form is valid.</returns>
    public bool Submit()
    {
        if (IsBusy) return false;
        var valid = ValidateModel();
        (valid ? ValidSubmit : InvalidSubmit)?.Invoke(this, EventArgs.Empty);
        return valid;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) ShowEnabled();
    }

    private void Add(string field, string message)
    {
        if (!_messages.TryGetValue(field, out var list)) _messages[field] = list = [];
        list.Add(message);
    }

    private void ShowModel(object? old, object? model)
    {
        if (_watched is not null) _watched.PropertyChanged -= OnModelChanged;
        _watched = model as INotifyPropertyChanged;
        if (_watched is not null) _watched.PropertyChanged += OnModelChanged;
        _messages.Clear();
        _modelMessages.Clear();
        Changed();
    }

    private void OnModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (Validate && !string.IsNullOrEmpty(e.PropertyName)) ValidateField(e.PropertyName);
    }

    private void ShowEnabled()
    {
        // Same as the fieldset of the web form: disabled:opacity-60, and no input while it works.
        var enabled = IsEnabled && !IsBusy;
        _host.IsEnabled = enabled;
        _host.Opacity = enabled ? 1 : 0.6;
    }

    private void Changed()
    {
        Show();
        foreach (var field in InputsVisualTree.Descendants(this).OfType<KlyFormField>().ToList()) field.Refresh();
        ValidationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Show()
    {
        var messages = Messages;
        _summary.IsVisible = ShowSummary && messages.Count > 0;
        if (!_summary.IsVisible)
        {
            _summary.Content = null;
            return;
        }
        var error = KlyTheme.Resolve(KlyColor.Error);
        var stack = new VerticalStackLayout { Spacing = 4 };
        var title = new Label { Text = "Please fix the following errors", FontSize = 14, FontAttributes = FontAttributes.Bold };
        title.SetThemeColor(Label.TextColorProperty, error.Text);
        SemanticProperties.SetHeadingLevel(title, SemanticHeadingLevel.Level2);
        stack.Children.Add(title);
        foreach (var message in messages)
        {
            var line = new Label { Text = "• " + message, FontSize = 14 };
            line.SetThemeColor(Label.TextColorProperty, error.Text);
            stack.Children.Add(line);
        }
        _summary.Content = stack;
    }
}
