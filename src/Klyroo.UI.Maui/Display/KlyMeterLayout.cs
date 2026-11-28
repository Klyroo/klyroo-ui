namespace Klyroo.UI.Maui;

/// <summary>The drawing of a <see cref="KlyMeter"/>.</summary>
public enum KlyMeterLayout
{
    /// <summary>A horizontal bar.</summary>
    Bar,
    /// <summary>A ring with the value in its center.</summary>
    Ring,
    /// <summary>Discrete levels, the ones up to <see cref="KlyMeter.CurrentLevel"/> highlighted.</summary>
    Scale,
}
