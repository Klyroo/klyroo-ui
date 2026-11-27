namespace Klyroo.UI.Maui;

/// <summary>
/// Formatting toolbar of a <see cref="KlyEditor"/>: a row of icon buttons that scrolls sideways when it does not fit. Put it in <see cref="KlyEditor.Children"/>;
/// a button applies its action to the Markdown at the cursor or the selection, and stays pressed while the selection has the format.
/// The link and image buttons open a small address form under the row.
/// </summary>
public class KlyEditorToolbar : Border
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<EditorToolbarItem>), typeof(KlyEditorToolbar), null, propertyChanged: (b, _, _) => ((KlyEditorToolbar)b).Rebuild());
    /// <summary>Identifies <see cref="Layout"/>.</summary>
    public static readonly BindableProperty LayoutProperty = BindableProperty.Create(nameof(Layout), typeof(EditorToolbarLayout), typeof(KlyEditorToolbar), EditorToolbarLayout.Fixed, propertyChanged: (b, _, _) => ((KlyEditorToolbar)b).Rebuild());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyEditorToolbar), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyEditorToolbar)b).ApplyStyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyEditorToolbar), KlySize.Sm, propertyChanged: (b, _, _) => ((KlyEditorToolbar)b).ApplyStyle());
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyEditorToolbar), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyEditorToolbar)b, (string?)v ?? DefaultLabel));

    private const string DefaultLabel = "Formatting";

    private readonly HorizontalStackLayout _row = new() { Spacing = 2 };
    private readonly BoxView _rule = new() { HeightRequest = 1, AutomationId = "rule" };
    private readonly EditorAddressForm _linkForm = new("Link address", withRemove: true);
    private readonly EditorAddressForm _imageForm = new("Image address", withRemove: false);
    private readonly List<EditorToolbarButton> _buttons = [];
    private KlyEditor? _owner;
    private (int Start, int Length) _selection;

    /// <summary>Creates a toolbar with the default buttons.</summary>
    public KlyEditorToolbar()
    {
        var scroll = new ScrollView { Orientation = ScrollOrientation.Horizontal, HorizontalScrollBarVisibility = ScrollBarVisibility.Never, Content = _row };
        var body = new VerticalStackLayout { Padding = new Thickness(6), Children = { scroll, _linkForm, _imageForm } };
        Content = new VerticalStackLayout { Children = { body, _rule } };
        Padding = new Thickness(0);
        SemanticProperties.SetDescription(this, DefaultLabel);

        _linkForm.Submitted += (_, address) => Submit(EditorAction.Link, address, _linkForm);
        _linkForm.Removed += (_, _) => Submit(EditorAction.Link, "", _linkForm);
        _imageForm.Submitted += (_, address) => Submit(EditorAction.Image, address, _imageForm);
        Rebuild();
    }

    /// <summary>The buttons, in order. Defaults to <see cref="EditorToolbarItem.Defaults"/>, or <see cref="EditorToolbarItem.BubbleDefaults"/> for a bubble toolbar.</summary>
    public IReadOnlyList<EditorToolbarItem>? Items { get => (IReadOnlyList<EditorToolbarItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    /// <summary><c>Fixed</c> (always visible, above the text) or <c>Bubble</c> (shown while some text is selected).</summary>
    public new EditorToolbarLayout Layout { get => (EditorToolbarLayout)GetValue(LayoutProperty); set => SetValue(LayoutProperty, value); }

    /// <summary>Color of pressed buttons.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Size of the buttons, from <c>Xs</c> to <c>Xl</c>. A button is at least 40 wide and high to tap, whatever its size.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>What a screen reader says for the toolbar. Defaults to <c>Formatting</c>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>The editor the buttons act on: set when the toolbar is added to <see cref="KlyEditor.Children"/>.</summary>
    internal KlyEditor? Owner
    {
        get => _owner;
        set
        {
            if (_owner is not null) _owner.StateChanged -= OnStateChanged;
            _owner = value;
            if (_owner is not null) _owner.StateChanged += OnStateChanged;
            Refresh();
        }
    }

    internal IReadOnlyList<EditorToolbarButton> Buttons => _buttons;

    internal EditorAddressForm LinkForm => _linkForm;

    internal EditorAddressForm ImageForm => _imageForm;

    /// <summary>Repaints the buttons from the state of the editor: which are pressed, which can run.</summary>
    internal void Refresh()
    {
        foreach (var button in _buttons)
        {
            var item = button.Item;
            var pressed = _owner?.IsActive(item) ?? false;
            var enabled = IsEnabled && (_owner?.CanRun(item.Action) ?? false);
            button.Update(Size, Color, pressed, enabled);
        }
        IsVisible = Layout == EditorToolbarLayout.Fixed || (_owner?.HasSelection ?? false) || _linkForm.IsVisible || _imageForm.IsVisible;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(IsEnabled)) Refresh();
    }

    private void OnStateChanged(object? sender, EventArgs e) => Refresh();

    private void Rebuild()
    {
        var bubble = Layout == EditorToolbarLayout.Bubble;
        _row.Children.Clear();
        _buttons.Clear();
        foreach (var item in Items ?? (bubble ? EditorToolbarItem.BubbleDefaults : EditorToolbarItem.Defaults))
        {
            if (item.IsSeparator)
            {
                var line = new BoxView { WidthRequest = 1, HeightRequest = 20, Margin = new Thickness(4, 0), VerticalOptions = LayoutOptions.Center };
                line.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
                AutomationProperties.SetIsInAccessibleTree(line, false);
                _row.Children.Add(line);
                continue;
            }
            var button = new EditorToolbarButton(item);
            button.Clicked += (_, _) => OnButton(item);
            _buttons.Add(button);
            _row.Children.Add(button);
        }
        ApplyStyle();
    }

    private void OnButton(EditorToolbarItem item)
    {
        if (_owner is null) return;
        switch (item.Action)
        {
            case EditorAction.Link:
                Toggle(_linkForm, _imageForm, _owner.Formats.Link ?? "", _owner.Formats.Link is not null);
                break;
            case EditorAction.Image:
                Toggle(_imageForm, _linkForm, "", false);
                break;
            default:
                _owner.Run(item.Action, item.Level);
                break;
        }
    }

    /// <summary>Opens a form, or closes it when it is open. The selection is kept: the field takes the focus, and the editor may forget it.</summary>
    private void Toggle(EditorAddressForm form, EditorAddressForm other, string address, bool removable)
    {
        other.Close();
        if (form.IsVisible)
        {
            form.Close();
        }
        else
        {
            _selection = _owner!.Selection;
            form.Open(address, removable);
        }
        Refresh();
    }

    private void Submit(EditorAction action, string address, EditorAddressForm form)
    {
        if (_owner is null) return;
        if (_owner.Run(action, 0, address, _selection) || (action == EditorAction.Link && address.Length == 0))
        {
            form.Close();
            _owner.FocusText();
            Refresh();
        }
        else
        {
            form.ShowProblem(EditorAddress.Refusal(action == EditorAction.Link ? _owner.LinkSchemes : _owner.ImageSchemes));
        }
    }

    private void ApplyStyle()
    {
        var bubble = Layout == EditorToolbarLayout.Bubble;
        if (bubble)
        {
            // A small raised panel over the text, like the floating toolbar of the web.
            this.SetRadius(KlyTheme.RadiusOf(1));
            this.Apply(new Surface(KlyTheme.Background, KlyTheme.Border, 1, KlyTheme.Text));
            Margin = new Thickness(6, 6, 6, 0);
        }
        else
        {
            // A bar with a line under it.
            this.Apply(new Surface(KlyThemeColor.Transparent, KlyThemeColor.Transparent, 0, KlyTheme.Text));
            Margin = new Thickness(0);
        }
        _rule.IsVisible = !bubble;
        _rule.SetThemeColor(BoxView.ColorProperty, KlyTheme.Border);
        _linkForm.ApplyStyle(Color);
        _imageForm.ApplyStyle(Color);
        Refresh();
    }
}
