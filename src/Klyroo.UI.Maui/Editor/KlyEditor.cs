using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Klyroo.UI.Maui;

/// <summary>
/// A Markdown editor: the native <see cref="Editor"/> in a Klyroo.UI frame, with a formatting toolbar, a drag handle and menus for "/" commands, "@" mentions and ":" emoji.
/// <para>
/// <b>It is not a rich text editor.</b> The web <c>KlyEditor</c> is a WYSIWYG TipTap editor that shows bold text as bold text and edits HTML. MAUI has no native
/// control that edits rich text, so this one edits Markdown source: a toolbar button writes the marks (<c>**bold**</c>, <c>## Heading</c>, <c>- item</c>) around the
/// selection (<see cref="InputView.CursorPosition"/> and <see cref="InputView.SelectionLength"/>) and the text shows them as typed, with no inline rendering of the formatting.
/// <see cref="Value"/> is the Markdown, not HTML. Undo and redo are kept by the editor itself, as a native text field offers no way to reach its own.
/// </para>
/// Put toolbars (<see cref="KlyEditorToolbar"/>), a drag handle (<see cref="KlyEditorDragHandle"/>) and menus (<see cref="KlyEditorSuggestionMenu"/>, <see cref="KlyEditorMentionMenu"/>,
/// <see cref="KlyEditorEmojiMenu"/>) in <see cref="Children"/>; the toolbars go above the text, the handle beside it and the menus under it.
/// </summary>
[ContentProperty(nameof(Children))]
public class KlyEditor : Border
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyEditor), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyEditor)b).ShowValue((string?)o, (string?)v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlyEditor), null, propertyChanged: (b, _, v) => ((KlyEditor)b)._editor.Placeholder = (string?)v);
    /// <summary>Identifies <see cref="IsReadOnly"/>.</summary>
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool), typeof(KlyEditor), false, propertyChanged: (b, _, v) => ((KlyEditor)b).ShowReadOnly((bool)v));
    /// <summary>Identifies <see cref="Label"/>.</summary>
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(KlyEditor), null, propertyChanged: (b, _, _) => ((KlyEditor)b).Describe());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyEditor), FieldVariant.Outline, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyEditor), KlySize.Md, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyEditor), KlyColor.Primary, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyEditor), false, propertyChanged: Restyle);
    /// <summary>Identifies <see cref="MaxLength"/>.</summary>
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int?), typeof(KlyEditor), null, propertyChanged: (b, _, v) => ((KlyEditor)b)._editor.MaxLength = (int?)v is int n and > 0 ? n : int.MaxValue);
    /// <summary>Identifies <see cref="LinkSchemes"/>.</summary>
    public static readonly BindableProperty LinkSchemesProperty = BindableProperty.Create(nameof(LinkSchemes), typeof(IReadOnlyList<string>), typeof(KlyEditor), null);
    /// <summary>Identifies <see cref="ImageSchemes"/>.</summary>
    public static readonly BindableProperty ImageSchemesProperty = BindableProperty.Create(nameof(ImageSchemes), typeof(IReadOnlyList<string>), typeof(KlyEditor), null);

    private const string DefaultLabel = "Markdown editor";
    private const double MinimumTextHeight = 128;

    private readonly Editor _editor = new() { AutoSize = EditorAutoSizeOption.TextChanges, BackgroundColor = Colors.Transparent, MinimumHeightRequest = MinimumTextHeight, Margin = new Thickness(12, 8) };
    private readonly VerticalStackLayout _toolbars = new();
    private readonly VerticalStackLayout _handles = new() { VerticalOptions = LayoutOptions.Start };
    private readonly VerticalStackLayout _menus = new();
    private readonly EditorChildren _children = new();
    private readonly EditorHistory _history = new();
    private EditorEdit _current = new("", 0, 0);
    private EditorFormats _formats = EditorFormats.None;
    private bool _focused;
    private bool _applying;

    /// <summary>Creates an editor.</summary>
    public KlyEditor()
    {
        _children.CollectionChanged += OnChildrenChanged;
        _editor.TextChanged += OnEditorTextChanged;
        _editor.PropertyChanged += OnEditorPropertyChanged;
        _editor.Focused += (_, _) => SetFocused(true);
        _editor.Unfocused += (_, _) => SetFocused(false);
        _editor.HandlerChanged += (_, _) => PlatformTweaks.RemoveNativeFieldChrome(_editor);

        var body = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) } };
        body.Add(_handles, 0);
        body.Add(_editor, 1);
        Content = new VerticalStackLayout { Spacing = 0, Children = { _toolbars, body, _menus } };

        this.SetRadius(KlyTheme.RadiusOf(1));
        HorizontalOptions = LayoutOptions.Fill;
        Describe();
        ApplyStyle();
        RefreshState();
    }

    /// <summary>Raised when <see cref="Value"/> changes, by the user, by an action of the toolbar or by code.</summary>
    public event EventHandler<TextChangedEventArgs>? ValueChanged;

    /// <summary>The Markdown text. Two-way bindable.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Shown while the editor is empty.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>The text can be selected and copied but not changed: the toolbar and the menus are off.</summary>
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>What a screen reader says for the text area. Defaults to <c>Markdown editor</c>.</summary>
    public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Look of the editor frame: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the text, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Semantic color of the focus ring.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the editor.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>
    /// The most characters of Markdown, or null for no limit. The native field stops typing at the limit, and an action of the toolbar or a menu that would go past
    /// it does nothing. The web editor also raises an event past the limit; this one does not.
    /// </summary>
    public int? MaxLength { get => (int?)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }

    /// <summary>
    /// The schemes a link may have, as absolute addresses (for example <c>https</c>, <c>http</c> and <c>mailto</c>). Without a list, the editor takes the usual schemes
    /// (http, https, ftp, mailto, tel and a few more) and relative addresses. With one, the link form of the toolbar refuses any other address.
    /// </summary>
    public IReadOnlyList<string>? LinkSchemes { get => (IReadOnlyList<string>?)GetValue(LinkSchemesProperty); set => SetValue(LinkSchemesProperty, value); }

    /// <summary>The schemes an image address may have: <c>http</c> and <c>https</c> (the default, null) or one of them.</summary>
    public IReadOnlyList<string>? ImageSchemes { get => (IReadOnlyList<string>?)GetValue(ImageSchemesProperty); set => SetValue(ImageSchemesProperty, value); }

    /// <summary>
    /// Toolbars, drag handle and suggestion menus. Only these can be added, and a toolbar or a menu works for the editor it is added to.
    /// </summary>
    public IList<View> Children => _children;

    /// <summary>The clock the undo history reads to tell a pause in the typing. Tests replace it.</summary>
    internal TimeProvider Clock { get; set; } = TimeProvider.System;

    /// <summary>The native control that edits the text.</summary>
    internal Editor NativeEditor => _editor;

    internal EditorFormats Formats => _formats;

    internal bool HasSelection => _current.Length > 0;

    internal (int Start, int Length) Selection => (_current.Start, _current.Length);

    /// <summary>Raised when the text, the selection or what can run changed: toolbars repaint.</summary>
    internal event EventHandler? StateChanged;

    /// <summary>Whether a button for the action should be pressed: the selection has the format.</summary>
    internal bool IsActive(EditorToolbarItem item) => item.Action switch
    {
        EditorAction.Bold => _formats.Bold,
        EditorAction.Italic => _formats.Italic,
        EditorAction.Strike => _formats.Strike,
        EditorAction.Underline => _formats.Underline,
        EditorAction.Code => _formats.Code,
        EditorAction.Heading => _formats.Heading == item.Level,
        EditorAction.BulletList => _formats.BulletList,
        EditorAction.OrderedList => _formats.OrderedList,
        EditorAction.TaskList => _formats.TaskList,
        EditorAction.Blockquote => _formats.Blockquote,
        EditorAction.CodeBlock => _formats.CodeBlock,
        EditorAction.Link => _formats.Link is not null,
        _ => false,
    };

    /// <summary>Whether the action can run now: the editor is enabled and writable, and there is something to undo, redo or move for those actions.</summary>
    internal bool CanRun(EditorAction action)
    {
        if (!IsEnabled || IsReadOnly) return false;
        return action switch
        {
            EditorAction.Undo => _history.CanUndo,
            EditorAction.Redo => _history.CanRedo,
            EditorAction.MoveUp => _formats.CanMoveUp,
            EditorAction.MoveDown => _formats.CanMoveDown,
            _ => true,
        };
    }

    /// <summary>
    /// Runs an action on the selection, or on <paramref name="at"/> when the caller kept the selection while the focus was elsewhere.
    /// Returns false when nothing changed: the action cannot run, the address is not accepted, or the limit would be passed.
    /// </summary>
    internal bool Run(EditorAction action, int level = 0, string? argument = null, (int Start, int Length)? at = null)
    {
        if (!CanRun(action)) return false;

        if (action is EditorAction.Undo or EditorAction.Redo)
        {
            var state = action == EditorAction.Undo ? _history.Undo(_current) : _history.Redo(_current);
            if (state is not { } target) return false;
            Place(target);
            SetValue(ValueProperty, target.Text);
            FocusText();
            return true;
        }

        if (action == EditorAction.Link && !string.IsNullOrWhiteSpace(argument) && !EditorAddress.IsLink(argument, LinkSchemes)) return false;
        if (action == EditorAction.Image && !EditorAddress.IsImage(argument, ImageSchemes)) return false;

        var (start, length) = at ?? (_current.Start, _current.Length);
        if (EditorMarkdown.Apply(action, level, argument, _current.Text, start, length) is not { } edit || edit.Text == _current.Text) return false;
        if (!Commit(edit)) return false;
        FocusText();
        return true;
    }

    /// <summary>Gives the focus back to the text, as pressing a button of the web toolbar does not take it away.</summary>
    internal void FocusText() => _editor.Focus();

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as the web editor: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            _editor.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
            RefreshState();
            EvaluateMenus();
        }
    }

    private static void Restyle(BindableObject bindable, object oldValue, object newValue) => ((KlyEditor)bindable).ApplyStyle();

    private void Describe()
    {
        SemanticProperties.SetDescription(_editor, Label ?? DefaultLabel);
        SemanticProperties.SetHint(_editor, "The text is written in Markdown");
    }

    private void ShowReadOnly(bool readOnly)
    {
        _editor.IsReadOnly = readOnly;
        RefreshState();
        EvaluateMenus();
    }

    private void SetFocused(bool focused)
    {
        _focused = focused;
        ApplyStyle();
    }

    private void ApplyStyle()
    {
        var set = KlyTheme.Resolve(Color);
        var (background, ring) = Variant switch
        {
            FieldVariant.Outline => (KlyTheme.Background, KlyTheme.BorderAccented),
            FieldVariant.Soft => (KlyTheme.BackgroundElevated.WithAlpha(0.5f), KlyThemeColor.Transparent),
            FieldVariant.Subtle => (KlyTheme.BackgroundElevated, KlyTheme.BorderAccented),
            FieldVariant.Ghost or FieldVariant.None => (KlyThemeColor.Transparent, KlyThemeColor.Transparent),
            _ => throw new ArgumentOutOfRangeException(nameof(Variant), Variant, null),
        };
        this.SetThemeColor(BackgroundColorProperty, background);

        // The error ring wins over the focus ring; the variant that asks for no ring shows neither.
        if (Invalid)
        {
            this.SetStroke(KlyTheme.Resolve(KlyColor.Error).Fill);
            StrokeThickness = 2;
        }
        else if (_focused && Variant != FieldVariant.None)
        {
            this.SetStroke(set.Text);
            StrokeThickness = 2;
        }
        else
        {
            this.SetStroke(ring);
            StrokeThickness = ring == KlyThemeColor.Transparent ? 0 : 1;
        }

        _editor.FontSize = Size switch
        {
            KlySize.Xs or KlySize.Sm => 12d,
            KlySize.Md or KlySize.Lg => 14d,
            KlySize.Xl => 16d,
            _ => throw new ArgumentOutOfRangeException(nameof(Size), Size, null),
        };
        _editor.SetThemeColor(InputView.TextColorProperty, KlyTheme.Text);
        _editor.SetThemeColor(InputView.PlaceholderColorProperty, KlyTheme.TextDimmed);
    }

    // ---- The text

    private EditorEdit Snapshot()
    {
        var text = _editor.Text ?? "";
        var start = Math.Clamp(_editor.CursorPosition, 0, text.Length);
        return new EditorEdit(text, start, Math.Clamp(_editor.SelectionLength, 0, text.Length - start));
    }

    private void OnEditorTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_applying) return;
        var before = _current;
        _current = Snapshot();
        if (_current.Text == before.Text) return;
        _history.Record(before, Clock.GetUtcNow(), typing: true);
        SetValue(ValueProperty, _current.Text);
        RefreshState();
        EvaluateMenus();
    }

    private void OnEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_applying || e.PropertyName is not (nameof(InputView.CursorPosition) or nameof(InputView.SelectionLength))) return;
        _current = Snapshot();
        RefreshState();
        EvaluateMenus();
    }

    /// <summary>Writes the text and the selection into the native control.</summary>
    private void Place(EditorEdit edit)
    {
        _applying = true;
        try
        {
            if ((_editor.Text ?? "") != edit.Text) _editor.Text = edit.Text;
            _editor.CursorPosition = edit.Start;
            _editor.SelectionLength = edit.Length;
        }
        finally
        {
            _applying = false;
        }
        _current = edit;
        RefreshState();
        EvaluateMenus();
    }

    /// <summary>One undoable change made by the editor itself (an action or a menu): the text, the selection and <see cref="Value"/>.</summary>
    private bool Commit(EditorEdit edit)
    {
        if (MaxLength is int max and > 0 && edit.Text.Length > max) return false;
        _history.Record(_current, Clock.GetUtcNow(), typing: false);
        Place(edit);
        SetValue(ValueProperty, edit.Text);
        return true;
    }

    private void ShowValue(string? old, string? value)
    {
        // A text the editor shows already is its own change. Any other was set by the app: the editor shows it, and it can be undone.
        var text = value ?? "";
        if (text != _current.Text)
        {
            _history.Record(_current, Clock.GetUtcNow(), typing: false);
            Place(new EditorEdit(text, Math.Min(_current.Start, text.Length), 0));
        }
        ValueChanged?.Invoke(this, new TextChangedEventArgs(old, value));
    }

    private void RefreshState()
    {
        _formats = EditorMarkdown.Inspect(_current.Text, _current.Start, _current.Length);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    // ---- Menus

    /// <summary>Opens the menu whose trigger the word before the cursor starts with, and closes the others.</summary>
    private void EvaluateMenus()
    {
        KlyEditorMenuBase? open = null;
        var query = "";
        var menus = _menus.Children.OfType<KlyEditorMenuBase>().ToList();
        if (!_applying && IsEnabled && !IsReadOnly && _current.Length == 0)
        {
            foreach (var menu in menus)
            {
                if (EditorMarkdown.FindTrigger(_current.Text, _current.Start, menu.Trigger) is { } trigger && trigger.Query.Length >= menu.MinQuery)
                {
                    open = menu;
                    query = trigger.Query;
                    break;
                }
            }
        }
        foreach (var menu in menus)
        {
            if (menu != open) menu.Hide();
        }
        open?.Show(query);
    }

    private void OnMenuChosen(object? sender, EditorMenuOption option)
    {
        if (sender is not KlyEditorMenuBase menu || !IsEnabled || IsReadOnly) return;
        if (EditorMarkdown.FindTrigger(_current.Text, _current.Start, menu.Trigger) is not { } trigger) return;

        var before = _current.Text[..trigger.Start];
        var after = _current.Text[(trigger.Start + trigger.Length)..];
        string inserted;
        EditorEdit edit;
        switch (menu.Kind)
        {
            case "mention":
                inserted = menu.Trigger + option.Label + " ";
                edit = new EditorEdit(before + inserted + after, trigger.Start + inserted.Length, 0);
                break;
            case "emoji":
                inserted = option.Value;
                edit = new EditorEdit(before + inserted + after, trigger.Start + inserted.Length, 0);
                break;
            default:
                // The typed word goes, then the action runs on the line it was on.
                var removed = new EditorEdit(before + after, trigger.Start, 0);
                edit = option.Action is { } action ? EditorMarkdown.Apply(action, option.Level, null, removed.Text, removed.Start, 0) ?? removed : removed;
                break;
        }

        if (!Commit(edit)) return;
        menu.Hide();
        menu.Accepted(option);
        FocusText();
    }

    // ---- Children

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var toolbar in _toolbars.Children.OfType<KlyEditorToolbar>()) toolbar.Owner = null;
        foreach (var handle in _handles.Children.OfType<KlyEditorDragHandle>()) handle.Dragged -= OnHandleDragged;
        foreach (var menu in _menus.Children.OfType<KlyEditorMenuBase>()) menu.Chosen -= OnMenuChosen;
        _toolbars.Children.Clear();
        _handles.Children.Clear();
        _menus.Children.Clear();

        foreach (var child in _children)
        {
            switch (child)
            {
                case KlyEditorToolbar toolbar:
                    toolbar.Owner = this;
                    _toolbars.Children.Add(toolbar);
                    break;
                case KlyEditorDragHandle handle:
                    handle.Dragged += OnHandleDragged;
                    _handles.Children.Add(handle);
                    break;
                case KlyEditorMenuBase menu:
                    menu.Chosen += OnMenuChosen;
                    _menus.Children.Add(menu);
                    break;
            }
        }
        EvaluateMenus();
    }

    private void OnHandleDragged(object? sender, EditorDragEventArgs e) => Run(e.Direction < 0 ? EditorAction.MoveUp : EditorAction.MoveDown);

    /// <summary>The children of the editor: a toolbar, a drag handle or a menu, nothing else.</summary>
    private sealed class EditorChildren : ObservableCollection<View>
    {
        protected override void InsertItem(int index, View item)
        {
            Check(item);
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, View item)
        {
            Check(item);
            base.SetItem(index, item);
        }

        private static void Check(View item)
        {
            if (item is not (KlyEditorToolbar or KlyEditorDragHandle or KlyEditorMenuBase))
            {
                throw new ArgumentException("A KlyEditor holds toolbars, a drag handle and suggestion menus only.", nameof(item));
            }
        }
    }
}
