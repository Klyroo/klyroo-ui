using Microsoft.Maui.Accessibility;
using Microsoft.Maui.ApplicationModel;

namespace Klyroo.UI.Maui;

/// <summary>Small helpers shared by the overlay controls.</summary>
internal static class OverlaysSupport
{
    private static readonly string[] LinkSchemes = ["http", "https", "mailto", "tel", "sms"];

    /// <summary>Takes a view out of its parent, so it can be placed in another one. A view cannot have two parents.</summary>
    public static void Detach(View view)
    {
        switch (view.Parent)
        {
            case ScrollView scroll: scroll.Content = null; break;
            case Border border: border.Content = null; break;
            case ContentView content: content.Content = null; break;
            case Layout layout: layout.Remove(view); break;
        }
    }

    /// <summary>Runs <paramref name="action"/> on the UI thread of <paramref name="owner"/>, or at once when it has no dispatcher (a unit test).</summary>
    public static void Post(Element owner, Action action)
    {
        IDispatcher? dispatcher;
        try
        {
            dispatcher = owner.Dispatcher;
        }
        catch (InvalidOperationException)
        {
            // No app and no dispatcher on this thread (a unit test): there is no UI thread to go back to.
            dispatcher = null;
        }
        if (dispatcher is { IsDispatchRequired: true }) dispatcher.Dispatch(action);
        else action();
    }

    /// <summary>Tells a screen reader a message, such as the number of results or a new toast. Does nothing where there is no screen reader service (the plain framework target).</summary>
    public static void Announce(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        try
        {
            SemanticScreenReader.Default.Announce(text);
        }
        catch (NotImplementedException)
        {
            // The plain net10.0 target has no screen reader.
        }
        catch (FeatureNotSupportedException)
        {
            // The platform has no announcement service.
        }
    }

    /// <summary>True for an address the system can open (web, mail, phone, sms); a relative route is the app's to handle in the selection event.</summary>
    public static bool IsExternalLink(string? href) =>
        Uri.TryCreate(href, UriKind.Absolute, out var uri) && LinkSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase);

    /// <summary>Opens an external address with the system, as a link of the web library navigates. A relative address is left to the selection event.</summary>
    public static void OpenLink(string? href)
    {
        if (IsExternalLink(href)) _ = Launcher.Default.TryOpenAsync(new Uri(href!));
    }

    /// <summary>Whether a view flows right to left, for the start and end of a placement.</summary>
    public static bool IsRtl(VisualElement view) => view.FlowDirection == FlowDirection.RightToLeft;
}
