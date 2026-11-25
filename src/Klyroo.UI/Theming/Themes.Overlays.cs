namespace Klyroo.UI.Theming;

internal static partial class Themes
{
    private const string CloseButton =
        "rounded-(--klyroo-radius) p-1 text-(--klyroo-text-muted) hover:bg-(--klyroo-bg-elevated) hover:text-(--klyroo-text) " +
        "focus-visible:outline-2 focus-visible:outline-(--klyroo-primary-text)";

    public static ComponentTheme Modal() =>
        new ComponentTheme("modal", "content", "header", "wrapper", "title", "description", "close", "body", "footer")
            .Slot("content", "m-auto flex-col overflow-hidden p-0 open:flex w-[calc(100vw_-_2rem)] max-h-[calc(100dvh_-_2rem)] " +
                             "rounded-[calc(var(--klyroo-radius)*2)] bg-(--klyroo-bg-overlay) text-(--klyroo-text) ring ring-(--klyroo-border) shadow-xl " +
                             "backdrop:bg-zinc-950/60 focus:outline-none")
            .Slot("header", "flex items-start gap-1.5 border-b border-(--klyroo-border) p-4 sm:px-6")
            .Slot("wrapper", "min-w-0 flex-1")
            .Slot("title", "text-base font-semibold text-(--klyroo-text)")
            .Slot("description", "mt-1 text-sm text-(--klyroo-text-muted)")
            .Slot("close", "-me-1 -mt-0.5 shrink-0 " + CloseButton)
            .Slot("body", "flex-1 overflow-y-auto p-4 sm:p-6")
            .Slot("footer", "flex items-center justify-end gap-1.5 border-t border-(--klyroo-border) p-4 sm:px-6")
            .Variant("size", "sm", "content", "max-w-sm")
            .Variant("size", "md", "content", "max-w-lg")
            .Variant("size", "lg", "content", "max-w-2xl")
            .Variant("size", "xl", "content", "max-w-4xl")
            .Variant("size", "xs", "content", "max-w-xs")
            .Default("size", "md");

    public static ComponentTheme Tooltip() =>
        new ComponentTheme("tooltip", "content")
            .Slot("content", "m-0 inset-auto max-w-xs rounded-(--klyroo-radius) border-0 bg-(--klyroo-text) px-2 py-1 text-xs text-(--klyroo-bg) shadow-sm");

    public static ComponentTheme DropdownMenu() =>
        new ComponentTheme("dropdownMenu", "content", "group", "label", "separator", "item", "itemIcon", "itemLabel", "itemShortcut", "itemChevron")
            .Slot("content", "m-0 inset-auto min-w-44 overflow-visible rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg-overlay) p-1 text-(--klyroo-text) shadow-lg")
            .Slot("group", "")
            .Slot("label", "px-2 py-1.5 text-xs font-semibold text-(--klyroo-text-muted)")
            .Slot("separator", "-mx-1 my-1 h-px bg-(--klyroo-border)")
            .Slot("item", "group relative flex w-full select-none items-center gap-1.5 rounded-[calc(var(--klyroo-radius)_-_2px)] px-2 py-1.5 text-start text-sm text-(--klyroo-text) outline-none " +
                          "hover:bg-(--klyroo-bg-elevated) focus:bg-(--klyroo-bg-elevated) aria-disabled:cursor-not-allowed aria-disabled:opacity-60 data-[color=error]:text-(--klyroo-error-text)")
            .Slot("itemIcon", "size-4 shrink-0 text-(--klyroo-text-muted) group-data-[color=error]:text-(--klyroo-error-text)")
            .Slot("itemLabel", "truncate")
            .Slot("itemShortcut", "ms-auto ps-4 text-xs text-(--klyroo-text-dimmed)")
            .Slot("itemChevron", "ms-auto size-4 shrink-0 text-(--klyroo-text-muted) rtl:rotate-180");

    public static ComponentTheme Toaster() =>
        new ComponentTheme("toast", "viewport", "root", "wrapper", "title", "description", "close")
            .Slot("viewport", "pointer-events-none fixed end-[max(1rem,var(--kly-safe-right))] bottom-[max(1rem,var(--kly-safe-bottom))] z-[100] flex w-[calc(100%_-_2rem)] flex-col gap-2 sm:w-96")
            .Slot("root", "pointer-events-auto relative flex gap-2.5 overflow-hidden rounded-(--klyroo-radius) border border-(--klyroo-border) bg-(--klyroo-bg-overlay) p-4 shadow-lg " +
                          "before:absolute before:inset-y-0 before:start-0 before:w-1 before:bg-(--kly-color) " +
                          "transition duration-200 starting:translate-y-2 starting:opacity-0 data-[state=closed]:translate-y-2 data-[state=closed]:opacity-0")
            .Slot("wrapper", "flex min-w-0 flex-1 flex-col gap-1")
            .Slot("title", "text-sm font-medium text-(--klyroo-text)")
            .Slot("description", "text-sm text-(--klyroo-text-muted)")
            .Slot("close", "-m-1 shrink-0 self-start " + CloseButton);
}
