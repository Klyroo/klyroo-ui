using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Klyroo.UI.Internal;

/// <summary>Internal building block, not part of the supported API. A heading of a given level (1 to 6) that can carry an id, so a region can be named by it.</summary>
public sealed class SectionHeading : ComponentBase
{
    [Parameter] public int Level { get; set; } = 2;
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "h" + Math.Clamp(Level, 1, 6));
        builder.AddAttribute(1, "id", Id);
        builder.AddAttribute(2, "class", Class);
        builder.AddAttribute(3, "data-slot", "title");
        builder.AddContent(4, ChildContent);
        builder.CloseElement();
    }
}

/// <summary>Markup shared by the page components.</summary>
internal static class PageRender
{
    /// <summary>The links as a row of buttons; nothing when there are none.</summary>
    public static RenderFragment Buttons(IReadOnlyList<PageLinkItem>? links, string? @class, KlySize size = KlySize.Lg, KlyrooIconOptions? icons = null) => builder =>
    {
        if (links is not { Count: > 0 })
        {
            return;
        }
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", @class);
        builder.AddAttribute(2, "data-slot", "links");
        foreach (var link in links)
        {
            builder.OpenComponent<KlyButton>(3);
            builder.AddComponentParameter(4, nameof(KlyButton.Label), link.Label);
            builder.AddComponentParameter(5, nameof(KlyButton.Href), link.Href);
            builder.AddComponentParameter(6, nameof(KlyButton.Target), link.Target);
            if (link.Color is { } color)
            {
                builder.AddComponentParameter(7, nameof(KlyButton.Color), color);
            }
            builder.AddComponentParameter(8, nameof(KlyButton.Variant), link.Variant);
            builder.AddComponentParameter(9, nameof(KlyButton.Size), size);
            builder.AddComponentParameter(10, nameof(KlyButton.Disabled), link.Disabled);
            builder.AddComponentParameter(11, nameof(KlyButton.LeadingIcon), link.Icon is null ? null : IconRender.Fragment(icons, link.Icon, "size-full"));
            builder.AddComponentParameter(12, nameof(KlyButton.TrailingIcon), link.TrailingIcon is null ? null : IconRender.Fragment(icons, link.TrailingIcon, "size-full"));
            builder.CloseComponent();
        }
        builder.CloseElement();
    };
}
