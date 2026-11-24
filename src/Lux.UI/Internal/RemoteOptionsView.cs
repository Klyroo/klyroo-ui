using System.Globalization;
using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Lux.UI.Internal;

/// <summary>
/// The scrolled area of a menu whose options come from an <see cref="LxOptionsProvider"/>: the listbox, then the states of the
/// load (loading, no results, text too short, error with a retry button) and <see cref="LxInfiniteScroll"/>, whose sentinel watches
/// this area and asks for the next page before the end is reached. The states sit beside the listbox, not in it: a listbox
/// may only own options and groups. One polite status carries what a screen reader should hear.
/// Shared by <see cref="LxSelectMenu"/> and <see cref="LxInputMenu"/>, which pass their own slots and texts.
/// </summary>
internal static class RemoteOptionsView
{
    /// <summary>What the browser side needs to find the area: it scrolls here, and the sentinel is watched against it.</summary>
    public static string Selector(string scrollerId) => "#" + scrollerId;

    public static string Announcement(LxRemoteOptions source, LxMessages messages, CultureInfo culture, string emptyText, bool hideEmpty) => source.Status switch
    {
        LxRemoteStatus.Loading => messages.RemoteOptionsLoading,
        LxRemoteStatus.TooShort => string.Format(culture, messages.RemoteOptionsMinLength, source.MinLength),
        LxRemoteStatus.Loaded when source.Items.Count == 0 && !hideEmpty => emptyText,
        _ => "",
    };

    // receiver: the component, owner of the callbacks of the retry button and of the infinite scroll. spinner: the loading icon, already
    // themed. emptyText: the "no results" text of the menu. hideEmpty: something else is offered when nothing matches (the option that
    // creates the typed value), so "no results" would contradict it.
    public static RenderFragment Scroller(
        LxRemoteOptions source, SlotSet slots, string scrollerId, LxMessages messages, CultureInfo culture,
        RenderFragment spinner, string emptyText, bool hideEmpty, IHandleEvent receiver, RenderFragment listbox) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "id", scrollerId);
        builder.AddAttribute(2, "class", slots["scroller"]);
        builder.AddAttribute(3, "data-slot", "scroller");
        builder.AddContent(4, listbox);

        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "role", "status");
        builder.AddAttribute(7, "aria-live", "polite");
        builder.AddAttribute(8, "aria-atomic", "true");
        builder.AddAttribute(9, "class", slots["announcer"]);
        builder.AddAttribute(10, "data-slot", "announcer");
        builder.AddContent(11, Announcement(source, messages, culture, emptyText, hideEmpty));
        builder.CloseElement();

        if (source.Status == LxRemoteStatus.Loading && source.Items.Count == 0)
        {
            builder.OpenElement(12, "div");
            builder.AddAttribute(13, "class", slots["loading"]);
            builder.AddAttribute(14, "data-slot", "loading");
            // The status above says it; the visible row is not read a second time.
            builder.AddAttribute(15, "aria-hidden", "true");
            builder.AddContent(16, spinner);
            builder.OpenElement(17, "span");
            builder.AddContent(18, messages.RemoteOptionsLoading);
            builder.CloseElement();
            builder.CloseElement();
        }
        else if (source.Status == LxRemoteStatus.TooShort)
        {
            builder.OpenElement(19, "p");
            builder.AddAttribute(20, "class", slots["empty"]);
            builder.AddAttribute(21, "data-slot", "hint");
            builder.AddContent(22, string.Format(culture, messages.RemoteOptionsMinLength, source.MinLength));
            builder.CloseElement();
        }
        else if (source.Status == LxRemoteStatus.Loaded && source.Items.Count == 0 && !hideEmpty)
        {
            builder.OpenElement(23, "p");
            builder.AddAttribute(24, "class", slots["empty"]);
            builder.AddAttribute(25, "data-slot", "empty");
            builder.AddContent(26, emptyText);
            builder.CloseElement();
        }

        if (source.Status == LxRemoteStatus.Error)
        {
            builder.OpenElement(27, "div");
            builder.AddAttribute(28, "role", "alert");
            builder.AddAttribute(29, "class", slots["error"]);
            builder.AddAttribute(30, "data-slot", "error");
            builder.OpenElement(31, "span");
            builder.AddContent(32, messages.RemoteOptionsError);
            builder.CloseElement();
            builder.OpenComponent<LxButton>(33);
            builder.AddAttribute(34, "Variant", ButtonVariant.Outline);
            builder.AddAttribute(35, "Size", LxSize.Xs);
            builder.AddAttribute(36, "Label", messages.RemoteOptionsRetry);
            builder.AddAttribute(37, "OnClick", EventCallback.Factory.Create<MouseEventArgs>(receiver, () => source.RetryAsync()));
            builder.AddAttribute(38, "data-slot", "retry");
            builder.CloseComponent();
            builder.CloseElement();
        }

        if (source.Items.Count > 0 && source.Status is LxRemoteStatus.Loaded or LxRemoteStatus.LoadingMore)
        {
            builder.OpenComponent<LxInfiniteScroll>(39);
            builder.AddAttribute(40, "HasMore", source.HasMore);
            builder.AddAttribute(41, "Loading", source.Status == LxRemoteStatus.LoadingMore);
            builder.AddAttribute(42, "OnLoadMore", EventCallback.Factory.Create(receiver, () => source.LoadMoreAsync()));
            builder.AddAttribute(43, "Root", Selector(scrollerId));
            // Starts the next page one list height before the end.
            builder.AddAttribute(44, "RootMargin", "0px 0px 100% 0px");
            builder.CloseComponent();
        }

        builder.CloseElement();
    };
}
