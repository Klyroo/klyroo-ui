namespace Klyroo.UI.Maui;

/// <summary>
/// A one-time code input made of separate boxes. Typing moves to the next box, and pasting into a box fills the boxes from it. Numeric or alphanumeric; the characters that the type does not accept are dropped.
/// </summary>
public class KlyPinInput : HorizontalStackLayout, IInputsFieldTarget
{
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyPinInput), null, BindingMode.TwoWay, propertyChanged: (b, o, v) => ((KlyPinInput)b).ShowValue((string?)o, (string?)v));
    /// <summary>Identifies <see cref="Length"/>.</summary>
    public static readonly BindableProperty LengthProperty = BindableProperty.Create(nameof(Length), typeof(int), typeof(KlyPinInput), 5, validateValue: (_, v) => (int)v >= 1, propertyChanged: (b, _, _) => ((KlyPinInput)b).Rebuild());
    /// <summary>Identifies <see cref="Type"/>.</summary>
    public static readonly BindableProperty TypeProperty = BindableProperty.Create(nameof(Type), typeof(PinInputType), typeof(KlyPinInput), PinInputType.Numeric, propertyChanged: (b, _, _) => ((KlyPinInput)b).Rebuild());
    /// <summary>Identifies <see cref="Mask"/>.</summary>
    public static readonly BindableProperty MaskProperty = BindableProperty.Create(nameof(Mask), typeof(bool), typeof(KlyPinInput), false, propertyChanged: (b, _, _) => ((KlyPinInput)b).Rebuild());
    /// <summary>Identifies <see cref="Color"/>.</summary>
    public static readonly BindableProperty ColorProperty = BindableProperty.Create(nameof(Color), typeof(KlyColor), typeof(KlyPinInput), KlyColor.Primary, propertyChanged: (b, _, _) => ((KlyPinInput)b).Restyle());
    /// <summary>Identifies <see cref="Variant"/>.</summary>
    public static readonly BindableProperty VariantProperty = BindableProperty.Create(nameof(Variant), typeof(FieldVariant), typeof(KlyPinInput), FieldVariant.Outline, propertyChanged: (b, _, _) => ((KlyPinInput)b).Restyle());
    /// <summary>Identifies <see cref="Size"/>.</summary>
    public static readonly BindableProperty SizeProperty = BindableProperty.Create(nameof(Size), typeof(KlySize), typeof(KlyPinInput), KlySize.Md, propertyChanged: (b, _, _) => ((KlyPinInput)b).Restyle());
    /// <summary>Identifies <see cref="Invalid"/>.</summary>
    public static readonly BindableProperty InvalidProperty = BindableProperty.Create(nameof(Invalid), typeof(bool), typeof(KlyPinInput), false, propertyChanged: (b, _, _) => ((KlyPinInput)b).Restyle());
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyPinInput), null, propertyChanged: (b, _, v) => SemanticProperties.SetDescription((KlyPinInput)b, (string?)v));

    private readonly List<InputsPinCell> _cells = [];
    private readonly InputsFieldTaken _taken = new();
    private bool _filling;

    /// <summary>Creates a code input of five boxes.</summary>
    public KlyPinInput()
    {
        Rebuild();
    }

    /// <summary>Raised when the code changes, by the user or by code.</summary>
    public event EventHandler<KlyInputChangedEventArgs<string>>? ValueChanged;

    /// <summary>The characters of the boxes, in order. Two-way bindable.</summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Number of boxes. Defaults to 5.</summary>
    public int Length { get => (int)GetValue(LengthProperty); set => SetValue(LengthProperty, value); }

    /// <summary>Characters the boxes accept: <c>Numeric</c> or <c>Alphanumeric</c>. Defaults to numeric.</summary>
    public PinInputType Type { get => (PinInputType)GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Hides the characters, as in a password field.</summary>
    public bool Mask { get => (bool)GetValue(MaskProperty); set => SetValue(MaskProperty, value); }

    /// <summary>Semantic color of the focus ring.</summary>
    public KlyColor Color { get => (KlyColor)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>Look of the boxes: <c>Outline</c>, <c>Soft</c>, <c>Subtle</c>, <c>Ghost</c> or <c>None</c>.</summary>
    public FieldVariant Variant { get => (FieldVariant)GetValue(VariantProperty); set => SetValue(VariantProperty, value); }

    /// <summary>Size of the boxes, from <c>Xs</c> to <c>Xl</c>.</summary>
    public KlySize Size { get => (KlySize)GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Draws the error ring, in the error color, around the boxes.</summary>
    public bool Invalid { get => (bool)GetValue(InvalidProperty); set => SetValue(InvalidProperty, value); }

    /// <summary>What a screen reader says for the group of boxes, such as its label. Each box says its position.</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal IReadOnlyList<InputsPinCell> Cells => _cells;

    /// <summary>The characters the type accepts, from a typed or pasted text.</summary>
    internal static List<char> Clean(string? text, PinInputType type) =>
        [.. (text ?? string.Empty).Where(c => type == PinInputType.Numeric ? c is >= '0' and <= '9' : char.IsAsciiLetterOrDigit(c))];

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        // Same dimming as a web field: disabled:opacity-60.
        if (propertyName == nameof(IsEnabled))
        {
            foreach (var cell in _cells) cell.IsEnabled = IsEnabled;
            Opacity = IsEnabled ? 1 : 0.6;
        }
    }

    void IInputsFieldTarget.ApplyField(InputsFieldState state)
    {
        _taken.TakeInvalid(this, InvalidProperty, state.Invalid);
        _taken.TakeName(this, AccessibleNameProperty, state.Name);
        _taken.TakeSize(this, SizeProperty, state.Size);
        SemanticProperties.SetHint(this, state.Hint);
    }

    private static double GapOf(KlySize size) => size switch
    {
        KlySize.Xs => 4,
        KlySize.Sm or KlySize.Md => 6,
        KlySize.Lg or KlySize.Xl => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
    };

    private string Read() => string.Concat(_cells.Select(c => c.Entry.Text ?? string.Empty));

    private void ShowValue(string? old, string? value)
    {
        if (Read() != (value ?? string.Empty)) Fill(Clean(value, Type), 0, clearRest: true);
        ValueChanged?.Invoke(this, new KlyInputChangedEventArgs<string>(old, value));
    }

    /// <summary>Puts characters in the boxes from one box on, and reports the code the boxes now spell.</summary>
    private void Fill(IReadOnlyList<char> characters, int from, bool clearRest)
    {
        _filling = true;
        for (var i = from; i < _cells.Count; i++)
        {
            var index = i - from;
            if (index < characters.Count) _cells[i].Entry.Text = characters[index].ToString();
            else if (clearRest) _cells[i].Entry.Text = string.Empty;
        }
        _filling = false;
    }

    private void OnCellChanged(int index, string? oldText, string? newText)
    {
        if (_filling) return;
        var characters = Clean(newText, Type);
        if (characters.Count == 2 && Clean(oldText, Type) is [var previous])
        {
            // A character typed in a box that already had one: the new one wins.
            characters = [characters[0] == previous ? characters[1] : characters[0]];
        }

        if (characters.Count == 0)
        {
            // An emptied box, or a character the type does not accept: the box is left empty.
            _filling = true;
            _cells[index].Entry.Text = string.Empty;
            _filling = false;
        }
        else
        {
            // What does not fit in the boxes after this one, in a long paste, is dropped.
            Fill(characters, index, clearRest: false);
            _cells[Math.Min(index + characters.Count, _cells.Count - 1)].Entry.Focus();
        }
        var code = Read();
        if (code != (Value ?? string.Empty)) Value = code;
    }

    private void Restyle()
    {
        Spacing = GapOf(Size);
        foreach (var cell in _cells)
        {
            cell.Color = Color;
            cell.Variant = Variant;
            cell.Size = Size;
            cell.Invalid = Invalid;
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _cells.Clear();
        for (var i = 0; i < Length; i++)
        {
            var index = i;
            var cell = new InputsPinCell { Color = Color, Variant = Variant, Size = Size, Invalid = Invalid, IsEnabled = IsEnabled };
            cell.AccessibleName = $"Character {i + 1} of {Length}";
            cell.Entry.Keyboard = Type == PinInputType.Numeric ? Keyboard.Numeric : Keyboard.Default;
            cell.Entry.IsPassword = Mask;
            cell.Entry.TextChanged += (_, e) => OnCellChanged(index, e.OldTextValue, e.NewTextValue);
            _cells.Add(cell);
            Children.Add(cell);
        }
        Spacing = GapOf(Size);
        Fill(Clean(Value, Type), 0, clearRest: true);
    }
}
