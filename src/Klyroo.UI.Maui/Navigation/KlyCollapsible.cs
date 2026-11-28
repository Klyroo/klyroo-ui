namespace Klyroo.UI.Maui;

/// <summary>A disclosure: a trigger and the content it shows or hides. Set <see cref="Content"/> to the revealed view and <see cref="Label"/> (or <see cref="Trigger"/>) to the trigger.</summary>
[ContentProperty(nameof(Content))]
public class KlyCollapsible : VerticalStackLayout
{
    /// <summary>Identifies <see cref="Content"/>.</summary>
    public static readonly BindableProperty ContentProperty = BindableProperty.Create(nameof(Content), typeof(View), typeof(KlyCollapsible), null, propertyChanged: (b, _, v) => ((KlyCollapsible)b)._body.Content = (View?)v);
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyCollapsible), null, propertyChanged: (b, _, _) => ((KlyCollapsible)b).ShowTrigger());
    /// <summary>Identifies <see cref="Trigger"/>.</summary>
    public static readonly BindableProperty TriggerProperty = BindableProperty.Create(nameof(Trigger), typeof(View), typeof(KlyCollapsible), null, propertyChanged: (b, _, _) => ((KlyCollapsible)b).ShowTrigger());
    /// <summary>Identifies <see cref="Open"/>.</summary>
    public static readonly BindableProperty OpenProperty = BindableProperty.Create(nameof(Open), typeof(bool), typeof(KlyCollapsible), false, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyCollapsible)b).ShowOpen((bool)v));
    /// <summary>Identifies <see cref="ShowIcon"/>.</summary>
    public static readonly BindableProperty ShowIconProperty = BindableProperty.Create(nameof(ShowIcon), typeof(bool), typeof(KlyCollapsible), true, propertyChanged: (b, _, v) => ((KlyCollapsible)b)._chevron.IsVisible = (bool)v);

    private readonly NavigationButton _trigger = new();
    private readonly Grid _row = new() { ColumnSpacing = 8, ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
    private readonly ContentView _triggerHost = new() { VerticalOptions = LayoutOptions.Center };
    private readonly Label _label = NavigationLayout.TextLabel(14, KlyTheme.Text);
    private readonly KlyIcon _chevron = NavigationLayout.Icon(KlyIconNames.ChevronDown, KlySize.Sm, KlyTheme.TextMuted);
    private readonly ContentView _body = new() { IsVisible = false };

    /// <summary>Creates a collapsible.</summary>
    public KlyCollapsible()
    {
        Spacing = 0;
        _trigger.Face.HorizontalOptions = LayoutOptions.Fill;
        _trigger.Face.Content = _row;
        _trigger.Clicked += (_, _) => Open = !Open;
        _row.Add(_triggerHost, 0);
        _row.Add(_chevron, 1);
        Children.Add(_trigger);
        Children.Add(_body);
        ShowTrigger();
    }

    /// <summary>Raised with the new state when the content opens or closes, by the user or by code.</summary>
    public event EventHandler<ToggledEventArgs>? OpenChanged;

    /// <summary>The revealed content.</summary>
    public View? Content { get => (View?)GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Text of the trigger.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Custom content of the trigger; replaces <see cref="Label"/>. It should not contain controls that take taps.</summary>
    public View? Trigger { get => (View?)GetValue(TriggerProperty); set => SetValue(TriggerProperty, value); }

    /// <summary>Open state. Two-way bindable.</summary>
    public bool Open { get => (bool)GetValue(OpenProperty); set => SetValue(OpenProperty, value); }

    /// <summary>Shows the chevron that turns when the content opens.</summary>
    public bool ShowIcon { get => (bool)GetValue(ShowIconProperty); set => SetValue(ShowIconProperty, value); }

    internal NavigationButton TriggerButton => _trigger;

    internal View BodyView => _body;

    internal KlyIcon Chevron => _chevron;

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web trigger: disabled:opacity-60. A disabled collapsible cannot be toggled by the user.
        if (propertyName == nameof(IsEnabled)) _trigger.IsEnabled = IsEnabled;
    }

    private void ShowTrigger()
    {
        // A custom trigger takes the place of the label in the first column.
        _label.Text = Label;
        _triggerHost.Content = Trigger ?? _label;
        SemanticProperties.SetDescription(_trigger, Trigger is null ? Label : null);
    }

    private void ShowOpen(bool open)
    {
        _body.IsVisible = open;
        NavigationLayout.Rotate(_chevron, open ? 180 : 0);
        OpenChanged?.Invoke(this, new ToggledEventArgs(open));
    }
}
