namespace Chat.Shared.Services;

/// <summary>Lets the page tell the host to dress the system bars (status bar, gesture bar) in the color of the app.</summary>
public interface ISystemBars
{
    void Apply(bool dark);
}

/// <summary>For hosts without system bars (the web app).</summary>
public sealed class NoSystemBars : ISystemBars
{
    public void Apply(bool dark)
    {
    }
}
