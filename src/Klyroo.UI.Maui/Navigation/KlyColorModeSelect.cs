namespace Klyroo.UI.Maui;

/// <summary>
/// A choice of System, Light and Dark: the native picker in a Klyroo frame. The choice goes to <see cref="KlyColorMode.Set"/>, and the select follows the mode when something else changes it.
/// It is a <see cref="KlySelect"/>, so <c>Color</c>, <c>Variant</c>, <c>Size</c> and <c>AccessibleName</c> work as on any select.
/// </summary>
public class KlyColorModeSelect : KlySelect
{
    /// <summary>Identifies <see cref="SystemLabel"/>.</summary>
    public static readonly BindableProperty SystemLabelProperty = BindableProperty.Create(nameof(SystemLabel), typeof(string), typeof(KlyColorModeSelect), "System", propertyChanged: (b, _, _) => ((KlyColorModeSelect)b).ShowItems());
    /// <summary>Identifies <see cref="LightLabel"/>.</summary>
    public static readonly BindableProperty LightLabelProperty = BindableProperty.Create(nameof(LightLabel), typeof(string), typeof(KlyColorModeSelect), "Light", propertyChanged: (b, _, _) => ((KlyColorModeSelect)b).ShowItems());
    /// <summary>Identifies <see cref="DarkLabel"/>.</summary>
    public static readonly BindableProperty DarkLabelProperty = BindableProperty.Create(nameof(DarkLabel), typeof(string), typeof(KlyColorModeSelect), "Dark", propertyChanged: (b, _, _) => ((KlyColorModeSelect)b).ShowItems());

    private sealed record Mode(AppTheme Theme, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly NavigationColorModeWatcher _watcher;
    private bool _syncing;

    /// <summary>Creates a color mode select.</summary>
    public KlyColorModeSelect()
    {
        AccessibleName = "Color mode";
        _watcher = new NavigationColorModeWatcher(this, Sync);
        SelectedItemChanged += (_, _) =>
        {
            if (!_syncing && SelectedItem is Mode mode) KlyColorMode.Set(mode.Theme);
        };
        ShowItems();
    }

    /// <summary>Label of the option that follows the system setting.</summary>
    public string SystemLabel { get => (string)GetValue(SystemLabelProperty); set => SetValue(SystemLabelProperty, value); }

    /// <summary>Label of the light option.</summary>
    public string LightLabel { get => (string)GetValue(LightLabelProperty); set => SetValue(LightLabelProperty, value); }

    /// <summary>Label of the dark option.</summary>
    public string DarkLabel { get => (string)GetValue(DarkLabelProperty); set => SetValue(DarkLabelProperty, value); }

    internal NavigationColorModeWatcher Watcher => _watcher;

    /// <summary>The mode that is selected, or <see cref="AppTheme.Unspecified"/> (the system) when nothing is.</summary>
    internal AppTheme SelectedMode => SelectedItem is Mode mode ? mode.Theme : AppTheme.Unspecified;

    /// <summary>Selects the mode the app has now.</summary>
    internal void Sync()
    {
        _syncing = true;
        SelectedItem = (ItemsSource as IList<Mode>)?.FirstOrDefault(m => m.Theme == KlyColorMode.Current);
        _syncing = false;
    }

    private void ShowItems()
    {
        _syncing = true;
        ItemsSource = new List<Mode> { new(AppTheme.Unspecified, SystemLabel), new(AppTheme.Light, LightLabel), new(AppTheme.Dark, DarkLabel) };
        _syncing = false;
        Sync();
    }
}
