namespace Klyroo.UI.Maui;

/// <summary>
/// A switch that is on in dark mode: turning it on sets the app to dark, turning it off sets it to light (<see cref="KlyColorMode.Set"/>), and it follows the mode when something else changes it.
/// It is a <see cref="KlySwitch"/>: set <see cref="Label"/> (the <see cref="KlySwitch.Text"/>) and <see cref="KlySwitch.Color"/> as on any switch.
/// </summary>
public class KlyColorModeSwitch : KlySwitch
{
    /// <summary>Identifies <see cref="AccessibleName"/>.</summary>
    public static readonly BindableProperty AccessibleNameProperty = BindableProperty.Create(nameof(AccessibleName), typeof(string), typeof(KlyColorModeSwitch), "Toggle dark mode", propertyChanged: (b, _, _) => ((KlyColorModeSwitch)b).ShowName());

    private readonly NavigationColorModeWatcher _watcher;
    private bool _syncing;

    /// <summary>Creates a color mode switch.</summary>
    public KlyColorModeSwitch()
    {
        _watcher = new NavigationColorModeWatcher(this, Sync);
        Sync();
        Toggled += (_, e) =>
        {
            if (!_syncing) KlyColorMode.Set(e.Value ? AppTheme.Dark : AppTheme.Light);
        };
        ShowName();
    }

    /// <summary>Visible label of the switch: the same as <see cref="KlySwitch.Text"/>. Without it the switch needs <see cref="AccessibleName"/>.</summary>
    public string? Label { get => Text; set => Text = value; }

    /// <summary>What a screen reader says when there is no visible label. Defaults to "Toggle dark mode".</summary>
    public string? AccessibleName { get => (string?)GetValue(AccessibleNameProperty); set => SetValue(AccessibleNameProperty, value); }

    internal NavigationColorModeWatcher Watcher => _watcher;

    /// <summary>Makes the switch say what the app shows now.</summary>
    internal void Sync()
    {
        _syncing = true;
        IsToggled = KlyColorMode.IsDark;
        _syncing = false;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == TextProperty.PropertyName) ShowName();
    }

    private void ShowName() => SemanticProperties.SetDescription(this, string.IsNullOrEmpty(Text) ? AccessibleName : null);
}
