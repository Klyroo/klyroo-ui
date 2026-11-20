namespace Blux.Maui.Shared.Services;

/// <summary>What the host tells the shared pages about the device (the MAUI app and the web app each implement it).</summary>
public interface IFormFactor
{
    /// <summary>"Phone", "Tablet", "Desktop", "TV" or "Web".</summary>
    string GetFormFactor();

    string GetPlatform();

    /// <summary>True on a TV screen: the shared layout turns on the remote control mode of Blux.</summary>
    bool IsTv => GetFormFactor() == "TV";
}
