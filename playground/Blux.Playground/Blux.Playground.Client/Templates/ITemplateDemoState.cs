namespace Blux.Playground.Client.Templates;

/// <summary>
/// What the docs host knows about the live demos, for the gallery: the state of each (ready, starting, restarting, unavailable, stopped), or null
/// when the demos are switched off on this host. The /templates page shows a demo as live only when this says it can be reached.
/// </summary>
public interface ITemplateDemoState
{
    string? StateOf(string name);
}
