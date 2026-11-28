using System.Collections;

namespace Klyroo.UI.Maui;

/// <summary>A choice among a list of items: the native <see cref="Picker"/> (a dialog on Android, a wheel on iOS) in a Klyroo.UI frame.</summary>
public class KlySelect : KlyFieldBase
{
    /// <summary>Identifies <see cref="ItemsSource"/>.</summary>
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(KlySelect), null, propertyChanged: (b, _, v) => ((KlySelect)b).ShowItems((IList?)v));
    /// <summary>Identifies <see cref="SelectedItem"/>.</summary>
    public static readonly BindableProperty SelectedItemProperty = BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(KlySelect), null, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlySelect)b).ShowItem(v));
    /// <summary>Identifies <see cref="Placeholder"/>.</summary>
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(KlySelect), null, propertyChanged: (b, _, v) => ((KlySelect)b)._picker.Title = (string?)v);

    private readonly Picker _picker;

    /// <summary>Creates a select.</summary>
    public KlySelect() : this(new Picker())
    {
    }

    private KlySelect(Picker picker) : base(picker)
    {
        _picker = picker;
        _picker.SelectedIndexChanged += (_, _) => SetValue(SelectedItemProperty, _picker.SelectedItem);
        ApplyStyle();
    }

    /// <summary>Raised when the selected item changes, by the user or by code.</summary>
    public event EventHandler? SelectedItemChanged;

    /// <summary>The items to choose from. Each one shows as its <c>ToString()</c>.</summary>
    public IList? ItemsSource { get => (IList?)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>The chosen item, or <c>null</c>. Two-way bindable.</summary>
    public object? SelectedItem { get => GetValue(SelectedItemProperty); set => SetValue(SelectedItemProperty, value); }

    /// <summary>Text shown while nothing is chosen, and the title of the choice dialog.</summary>
    public string? Placeholder { get => (string?)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <inheritdoc />
    protected override void StyleText(KlyThemeColor text, KlyThemeColor placeholder, double fontSize)
    {
        _picker.SetThemeColor(Picker.TextColorProperty, text);
        _picker.SetThemeColor(Picker.TitleColorProperty, placeholder);
        _picker.FontSize = fontSize;
    }

    private void ShowItems(IList? items)
    {
        _picker.ItemsSource = items;
        // The selection can be set before the items, in any order a binding or a XAML file picks.
        if (SelectedItem is { } item) _picker.SelectedItem = item;
    }

    private void ShowItem(object? item)
    {
        if (!Equals(_picker.SelectedItem, item)) _picker.SelectedItem = item;
        SelectedItemChanged?.Invoke(this, EventArgs.Empty);
    }
}
