using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Klyroo.UI.Internal;

/// <summary>Markup and helpers shared by the read-only block components.</summary>
internal static class BlockRender
{
    /// <summary>A <see cref="KlyBadgeItem"/> as a subtle small badge.</summary>
    public static RenderFragment Badge(KlyBadgeItem badge, KlyrooIconOptions? icons, string? @class = null) => builder =>
    {
        builder.OpenComponent<KlyBadge>(0);
        builder.AddComponentParameter(1, nameof(KlyBadge.Label), badge.Label);
        builder.AddComponentParameter(2, nameof(KlyBadge.Color), badge.Color ?? KlyColor.Neutral);
        builder.AddComponentParameter(3, nameof(KlyBadge.Variant), KlyVariant.Subtle);
        builder.AddComponentParameter(4, nameof(KlyBadge.Size), KlySize.Sm);
        if (!string.IsNullOrEmpty(badge.Icon))
        {
            builder.AddComponentParameter(5, nameof(KlyBadge.LeadingIcon), IconRender.Fragment(icons, badge.Icon, "size-full"));
        }
        if (@class is not null)
        {
            builder.AddComponentParameter(6, nameof(KlyBadge.Class), @class);
        }
        builder.AddComponentParameter(7, "data-slot", "badge");
        builder.CloseComponent();
    };

    /// <summary>The actions as small buttons in a wrapper; nothing when there are none.</summary>
    public static RenderFragment Actions(IReadOnlyList<KlyActionItem>? actions, KlyrooIconOptions? icons, string? @class, Microsoft.AspNetCore.Components.ComponentBase receiver) => builder =>
    {
        if (actions is not { Count: > 0 })
        {
            return;
        }
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", @class);
        builder.AddAttribute(2, "data-slot", "actions");
        foreach (var action in actions)
        {
            builder.OpenComponent<KlyButton>(3);
            builder.AddComponentParameter(4, nameof(KlyButton.Label), action.Label);
            builder.AddComponentParameter(5, nameof(KlyButton.Href), action.Href);
            builder.AddComponentParameter(6, nameof(KlyButton.Target), action.Target);
            builder.AddComponentParameter(7, nameof(KlyButton.Color), action.Color);
            builder.AddComponentParameter(8, nameof(KlyButton.Variant), action.Variant);
            builder.AddComponentParameter(9, nameof(KlyButton.Size), KlySize.Sm);
            if (action.Icon is not null)
            {
                builder.AddComponentParameter(10, nameof(KlyButton.LeadingIcon), IconRender.Fragment(icons, action.Icon, "size-full"));
            }
            if (action.OnClick.HasDelegate)
            {
                var click = action.OnClick;
                builder.AddComponentParameter(11, nameof(KlyButton.OnClick), EventCallback.Factory.Create<Microsoft.AspNetCore.Components.Web.MouseEventArgs>(receiver, () => click.InvokeAsync()));
            }
            builder.AddComponentParameter(12, "data-slot", "action");
            builder.CloseComponent();
        }
        builder.CloseElement();
    };

    /// <summary>The placeholder shown by a block while it loads.</summary>
    public static RenderFragment Skeleton(string? @class, string? lineClass) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", @class);
        builder.AddAttribute(2, "data-slot", "skeleton");
        builder.AddAttribute(3, "aria-busy", "true");
        foreach (var width in new[] { "h-5 w-1/3", "h-4 w-2/3", "h-4 w-1/2" })
        {
            builder.OpenComponent<KlySkeleton>(4);
            builder.AddComponentParameter(5, nameof(KlySkeleton.Class), width);
            builder.CloseComponent();
        }
        builder.CloseElement();
    };

    /// <summary>Copies a text to the clipboard. Nothing happens in a static render or when the browser refuses.</summary>
    public static async Task CopyAsync(IJSRuntime js, bool interactive, string? text)
    {
        if (string.IsNullOrEmpty(text) || !interactive) return;
        try
        {
            await js.InvokeVoidAsync("navigator.clipboard.writeText", text);
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>A number with at most two decimals, in the given culture, with no <c>-0</c>.</summary>
    public static string Number(double value, CultureInfo culture) => (value == 0 ? 0 : value).ToString("#,0.##", culture);

    /// <summary>A number for a machine-read attribute or a style: invariant, at most two decimals.</summary>
    public static string Invariant(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The initials of a name: the first letter of the first and last word, or a question mark.</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "?";
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}

/// <summary>The state of the "Show more" reveal of the list-like blocks: how many items show, and how many the next click adds.</summary>
internal sealed class Reveal
{
    private int _revealed;

    /// <summary>Keeps at least <paramref name="max"/> items visible; call from <c>OnParametersSet</c>.</summary>
    public void Sync(int? max)
    {
        if (max is { } m && _revealed < m) _revealed = m;
    }

    /// <summary>The items to show and the number the next click reveals (0 when everything shows).</summary>
    public (IReadOnlyList<T> Visible, int Remaining) Apply<T>(IReadOnlyList<T>? source, int? max, int? step)
    {
        if (source is null || source.Count == 0) return ([], 0);
        if (max is not { } limitMax || limitMax <= 0) return (source, 0);
        var limit = Math.Max(limitMax, _revealed);
        if (source.Count <= limit) return (source, 0);
        var next = step is > 0 ? Math.Min(step.Value, source.Count - limit) : source.Count - limit;
        return (source.Take(limit).ToList(), next);
    }

    /// <summary>Shows the next batch.</summary>
    public void More(int total, int? max, int? step)
    {
        var current = Math.Max(max ?? 0, _revealed);
        _revealed = step is > 0 ? Math.Min(total, current + step.Value) : total;
    }
}
