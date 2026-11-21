using Blux.UI.Theming;

namespace Blux.UI.Tools;

/// <summary>Default themes of Blux.UI.Tools. Every class is a literal, so the class extractor and Tailwind see it.</summary>
internal static partial class ToolsThemes
{
    public static ComponentTheme DataListEmpty() =>
        new ComponentTheme("dataListEmpty", "root", "icon", "title", "description", "actions")
            .Slot("root", "flex w-full min-w-0 flex-col items-center justify-center gap-3 px-6 py-12 text-center")
            .Slot("icon", "size-10 shrink-0 text-(--blux-text-muted)")
            .Slot("title", "text-sm font-medium text-(--blux-text-highlighted)")
            .Slot("description", "max-w-sm text-sm text-(--blux-text-muted)")
            .Slot("actions", "mt-2 flex flex-wrap items-center justify-center gap-2");
}
