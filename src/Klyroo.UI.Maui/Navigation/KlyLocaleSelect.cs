using System.Globalization;

namespace Klyroo.UI.Maui;

/// <summary>
/// A choice of language: the native picker in a Klyroo frame, listing <see cref="Locales"/> by name. Choosing one applies the culture and stores it (<see cref="KlyCulture.Set"/>),
/// then raises <see cref="LocaleChanged"/>; apply the stored culture at startup with <see cref="KlyCulture.ApplyStored"/>. It is a <see cref="KlySelect"/>, so <c>Color</c>, <c>Variant</c>, <c>Size</c>
/// and <c>AccessibleName</c> work as on any select.
/// </summary>
public class KlyLocaleSelect : KlySelect
{
    /// <summary>Identifies <see cref="Locales"/>.</summary>
    public static readonly BindableProperty LocalesProperty = BindableProperty.Create(nameof(Locales), typeof(IReadOnlyList<KlyLocaleOption>), typeof(KlyLocaleSelect), null, propertyChanged: (b, _, _) => ((KlyLocaleSelect)b).ShowLocales());
    /// <summary>Identifies <see cref="Value"/>.</summary>
    public static readonly BindableProperty ValueProperty = BindableProperty.Create(nameof(Value), typeof(string), typeof(KlyLocaleSelect), null, BindingMode.TwoWay, propertyChanged: (b, _, v) => ((KlyLocaleSelect)b).OnValueChanged((string?)v));

    private bool _syncing;

    /// <summary>Creates a locale select. It starts on the locale that matches the culture of the app.</summary>
    public KlyLocaleSelect()
    {
        AccessibleName = "Language";
        MinimumWidthRequest = 160;
        SelectedItemChanged += (_, _) =>
        {
            if (!_syncing && SelectedItem is KlyLocaleOption option) Value = option.Code;
        };
        ShowLocales();
    }

    /// <summary>Raised with the code of the chosen locale, after the culture was applied and stored: when the user chooses, or when <see cref="Value"/> is set by code.</summary>
    public event EventHandler<string>? LocaleChanged;

    /// <summary>The locales to offer, shown by name (with <see cref="KlyLocaleOption.Flag"/> when set). Defaults to <see cref="KlyLocaleOption.All"/>.</summary>
    public IReadOnlyList<KlyLocaleOption>? Locales { get => (IReadOnlyList<KlyLocaleOption>?)GetValue(LocalesProperty); set => SetValue(LocalesProperty, value); }

    /// <summary>
    /// The code of the selected locale. It starts as the code of the locale that matches the current culture. Setting it applies and stores that culture. Two-way bindable.
    /// </summary>
    public string? Value { get => (string?)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    private IReadOnlyList<KlyLocaleOption> Offered => Locales ?? KlyLocaleOption.All;

    /// <summary>The locale of <paramref name="code"/>: the one with the same code, else the first of the same language, else <c>null</c>. Codes compare case-insensitively.</summary>
    internal static KlyLocaleOption? Resolve(IReadOnlyList<KlyLocaleOption> locales, string code)
    {
        var exact = locales.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));
        if (exact is not null) return exact;
        var language = code.Split('-')[0];
        return locales.FirstOrDefault(l => string.Equals(l.Code.Split('-')[0], language, StringComparison.OrdinalIgnoreCase));
    }

    private void ShowLocales()
    {
        var locales = Offered;
        _syncing = true;
        ItemsSource = locales.ToList();
        _syncing = false;
        // The first time (and when the list changes) the selection is what the app already speaks, without applying or storing anything.
        var current = Value ?? CultureInfo.CurrentUICulture.Name;
        var option = Resolve(locales, current);
        _syncing = true;
        SelectedItem = option;
        SetValue(ValueProperty, option?.Code);
        _syncing = false;
    }

    private void OnValueChanged(string? code)
    {
        if (_syncing) return;
        var option = code is null ? null : Resolve(Offered, code);
        _syncing = true;
        SelectedItem = option;
        _syncing = false;
        if (option is null) return;
        KlyCulture.Set(option.Code);
        LocaleChanged?.Invoke(this, option.Code);
    }
}
