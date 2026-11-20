using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Elements =
    [
        new("kbd", "Kbd", "Elements", "A keyboard key or shortcut, in four variants and five sizes.",
            [new("Variants, sizes and shortcuts", typeof(Examples.KbdExample))], [typeof(BxKbd)]),
        new("separator", "Separator", "Elements", "A line between sections, optionally with a label or an icon, horizontal or vertical.",
            [new("Horizontal", typeof(Examples.SeparatorExample)), new("Vertical", typeof(Examples.SeparatorVerticalExample))], [typeof(BxSeparator)]),
        new("skeleton", "Skeleton", "Elements", "A pulsing placeholder for content that is loading. Mark the loading region with aria-busy.",
            [new("Loading profile", typeof(Examples.SkeletonExample), "The placeholders are hidden from assistive technology; the busy region is announced once.")], [typeof(BxSkeleton)]),
        new("chip", "Chip", "Elements", "A small dot or count placed on a corner of another element.",
            [new("Positions and states", typeof(Examples.ChipExample))], [typeof(BxChip)]),
        new("link", "Link", "Elements", "An anchor that knows whether it points at the current page and says so with aria-current.",
            [new("Active state", typeof(Examples.LinkExample), "Matching ignores the query string and the fragment; the active state follows navigation in interactive pages.")], [typeof(BxLink)]),
        new("icon", "Icon", "Elements", "Any Iconify icon by name (i-ph-house, mdi:home, a bare Lucide name), bundled into the app at build time, with the Lucide icons that components use built in.",
            [new("Built-in icons", typeof(Examples.IconExample)),
             new("Iconify collections", typeof(Examples.IconCollectionsExample), "The build finds these names in the example's source, downloads them from the Iconify API and compiles them into the app. custom:blux comes from a folder of SVG files. See the Icons guide."),
             new("Names built at runtime", typeof(Examples.IconDynamicExample), "The scan reads literals only: the icons such a name can produce are listed in BluxIcon items of the project file.")],
            [typeof(BxIcon)]),
        new("container", "Container", "Elements", "A centered wrapper with a maximum width and responsive padding.",
            [new("Container", typeof(Examples.ContainerExample))], [typeof(BxContainer)]),
        new("progress", "Progress", "Elements", "A determinate or indeterminate progress bar, with optional step labels.",
            [new("Determinate and indeterminate", typeof(Examples.ProgressExample), "The indeterminate animation stops when the user prefers reduced motion."),
             new("Steps", typeof(Examples.ProgressStepsExample), "The value is the index of the current step; a button with OnClick stays disabled until the page is interactive.")],
            [typeof(BxProgress)]),
        new("banner", "Banner", "Elements", "An announcement bar. Its close button remembers the dismissal per Id in the browser, static pages included.",
            [new("Banner", typeof(Examples.BannerExample), "Close it and reload the page: it stays hidden.")], [typeof(BxBanner)]),
        new("empty", "Empty", "Elements", "An empty state with an icon, a title, a description and actions.",
            [new("Empty state", typeof(Examples.EmptyExample))], [typeof(BxEmpty)]),
        new("user", "User", "Elements", "An avatar with a name and a description, optionally a link.",
            [new("Sizes, link and vertical", typeof(Examples.UserExample))], [typeof(BxUser)]),
        new("avatar-group", "AvatarGroup", "Elements", "A stack of avatars with a +N avatar for the overflow.",
            [new("Max and overflow", typeof(Examples.AvatarGroupExample))], [typeof(BxAvatarGroup)]),
        new("field-group", "FieldGroup", "Elements", "Attaches buttons and fields to each other: the children lose the inner radii, with CSS only.",
            [new("Horizontal and vertical", typeof(Examples.FieldGroupExample))], [typeof(BxFieldGroup)]),
    ];
}
