namespace Lux.UI.Theming;

// Themes of the "Editor" family: Editor, EditorToolbar, EditorDragHandle and the suggestion menus (EditorMenu).
internal static partial class Themes
{
    private const string EditorFocus = "has-[.ProseMirror:focus-visible]:ring-2 has-[.ProseMirror:focus-visible]:ring-(--bx-color-text)";

    /// <summary>Typography of the content, for the live editor and the HTML shown before it is ready.</summary>
    private const string Prose =
        "[&_p]:my-0 [&_p+p]:mt-3 [&_:is(h1,h2,h3,h4,h5,h6)]:font-semibold [&_:is(h1,h2,h3,h4,h5,h6)]:text-(--blux-text-highlighted) " +
        "[&_:is(h1,h2,h3,h4,h5,h6)]:mt-4 [&_:is(h1,h2,h3,h4,h5,h6)]:mb-2 [&_h1]:text-2xl [&_h2]:text-xl [&_h3]:text-lg [&_h4]:text-base " +
        "[&_ul]:list-disc [&_ul]:ps-6 [&_ul]:my-3 [&_ol]:list-decimal [&_ol]:ps-6 [&_ol]:my-3 [&_li_p]:m-0 " +
        "[&_blockquote]:my-3 [&_blockquote]:border-s-4 [&_blockquote]:border-(--blux-border-accented) [&_blockquote]:ps-4 [&_blockquote]:text-(--blux-text-muted) " +
        "[&_code]:rounded-sm [&_code]:bg-(--blux-bg-elevated) [&_code]:px-1 [&_code]:py-0.5 [&_code]:font-mono [&_code]:text-[0.9em] " +
        "[&_pre]:my-3 [&_pre]:overflow-x-auto [&_pre]:rounded-(--blux-radius) [&_pre]:bg-(--blux-bg-elevated) [&_pre]:p-3 [&_pre]:font-mono [&_pre]:text-sm " +
        "[&_pre_code]:bg-transparent [&_pre_code]:p-0 [&_hr]:my-4 [&_hr]:border-(--blux-border) " +
        "[&_a]:text-(--blux-primary-text) [&_a]:underline [&_a]:underline-offset-2 " +
        "[&_.bx-mention]:rounded-sm [&_.bx-mention]:bg-(--bx-color)/10 [&_.bx-mention]:px-1 [&_.bx-mention]:font-medium [&_.bx-mention]:text-(--bx-color-text) " +
        "[&_ul[data-type=taskList]]:list-none [&_ul[data-type=taskList]]:ps-0 [&_li[data-checked]]:flex [&_li[data-checked]]:items-start [&_li[data-checked]]:gap-2 " +
        "[&_li[data-checked]>label]:mt-1 [&_li[data-checked]>div]:min-w-0 [&_li[data-checked]>div]:flex-1 [&_li[data-checked=true]>div]:text-(--blux-text-muted) [&_li[data-checked=true]>div]:line-through " +
        "[&_img]:my-3 [&_img]:h-auto [&_img]:max-w-full [&_img]:rounded-(--blux-radius) " +
        "[&_.ProseMirror-selectednode]:outline-2 [&_.ProseMirror-selectednode]:outline-(--bx-color-text) " +
        "[&_.is-empty]:before:pointer-events-none [&_.is-empty]:before:float-left [&_.is-empty]:before:h-0 [&_.is-empty]:before:text-(--blux-text-dimmed) " +
        "[&_.is-empty]:before:content-[attr(data-placeholder)]";

    public static ComponentTheme Editor()
    {
        var t = new ComponentTheme("editor", "root", "content", "base", "fallback")
            .Slot("root", "relative flex w-full min-w-0 flex-col rounded-(--blux-radius) transition-colors [&[disabled]]:cursor-not-allowed [&[disabled]]:opacity-60")
            .Slot("content", "min-w-0 flex-1 px-3 py-2 " + Prose)
            .Slot("base", "[&_.ProseMirror]:min-h-32 [&_.ProseMirror]:outline-none [&_.ProseMirror]:whitespace-pre-wrap [&_.ProseMirror]:wrap-break-word")
            .Slot("fallback", "min-h-32 whitespace-pre-wrap wrap-break-word " + HiddenAttribute)
            .Variant("size", "xs", "content", "text-xs").Variant("size", "sm", "content", "text-xs").Variant("size", "md", "content", "text-sm")
            .Variant("size", "lg", "content", "text-sm").Variant("size", "xl", "content", "text-base")
            .Default("size", "md");
        return FieldLook(t, "root", EditorFocus, ContainerInvalid);
    }

    public static ComponentTheme EditorToolbar()
    {
        const string button =
            "inline-flex shrink-0 cursor-pointer items-center justify-center rounded-[calc(var(--blux-radius)_-_2px)] text-(--blux-text-muted) transition-colors " +
            "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) aria-pressed:bg-(--bx-color)/10 aria-pressed:text-(--bx-color-text) aria-pressed:ring aria-pressed:ring-inset aria-pressed:ring-(--bx-color-text)/75 " +
            "disabled:cursor-not-allowed disabled:opacity-40 disabled:hover:bg-transparent focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-(--bx-color-text)";
        return new ComponentTheme("editorToolbar", "root", "button", "icon", "separator", "linkForm", "linkInput", "linkButton", "linkMessage")
            .Slot("root", "items-center gap-0.5")
            .Variant("layout", "fixed", "root", "flex flex-wrap border-b border-(--blux-border) p-1.5")
            .Variant("layout", "bubble", "root", "m-0 inset-auto flex flex-wrap overflow-visible rounded-(--blux-radius) border border-(--blux-border) bg-(--blux-bg-overlay) p-1 " +
                                                  "text-(--blux-text) shadow-lg [&:not(:popover-open)]:hidden")
            .Slot("button", button)
            .Slot("icon", "pointer-events-none shrink-0")
            .Slot("separator", "mx-1 h-5 w-px shrink-0 bg-(--blux-border)")
            .Slot("linkForm", "flex w-full basis-full flex-wrap items-center gap-1 pt-1 " + HiddenAttribute)
            .Slot("linkInput", "min-w-0 flex-1 rounded-[calc(var(--blux-radius)_-_2px)] border-0 bg-(--blux-bg) px-2 py-1 text-sm text-(--blux-text) ring ring-inset ring-(--blux-border-accented) " +
                               "placeholder:text-(--blux-text-dimmed) focus:outline-none focus-visible:ring-2 focus-visible:ring-(--bx-color-text)")
            .Slot("linkButton", "cursor-pointer rounded-[calc(var(--blux-radius)_-_2px)] px-2 py-1 text-xs font-medium text-(--bx-color-text) transition-colors hover:bg-(--blux-bg-elevated) " +
                                "focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-(--bx-color-text)")
            .Slot("linkMessage", "w-full basis-full px-1 text-xs text-(--blux-error-text) empty:hidden")
            .Variant("size", "xs", "button", "size-6").Variant("size", "xs", "icon", "size-3.5")
            .Variant("size", "sm", "button", "size-7").Variant("size", "sm", "icon", "size-4")
            .Variant("size", "md", "button", "size-8").Variant("size", "md", "icon", "size-4")
            .Variant("size", "lg", "button", "size-9").Variant("size", "lg", "icon", "size-5")
            .Variant("size", "xl", "button", "size-10").Variant("size", "xl", "icon", "size-5")
            .Default("size", "sm").Default("layout", "fixed");
    }

    public static ComponentTheme EditorDragHandle() =>
        new ComponentTheme("editorDragHandle", "root", "handle", "icon")
            .Slot("root", "absolute z-10 max-sm:hidden " + HiddenAttribute)
            .Slot("handle", "inline-flex size-6 cursor-grab items-center justify-center rounded-[calc(var(--blux-radius)_-_2px)] text-(--blux-text-muted) transition-colors " +
                            "hover:bg-(--blux-bg-elevated) hover:text-(--blux-text) active:cursor-grabbing focus-visible:outline-2 focus-visible:outline-(--bx-color-text)")
            .Slot("icon", "pointer-events-none size-4");

    public static ComponentTheme EditorMenu() =>
        new ComponentTheme("editorMenu", "root", "content", "listbox", "item", "icon", "body", "itemLabel", "itemDescription", "empty")
            .Slot("root", "contents")
            .Slot("content", Popup + " min-w-56 max-w-xs")
            .Slot("listbox", "flex max-h-72 flex-col overflow-y-auto p-1 focus:outline-none")
            .Slot("item", ListItem)
            .Slot("icon", "size-5 shrink-0 text-(--blux-text-muted)")
            .Slot("body", "flex min-w-0 flex-1 flex-col")
            .Slot("itemLabel", "truncate")
            .Slot("itemDescription", "truncate text-xs text-(--blux-text-muted)")
            .Slot("empty", "px-3 py-2 text-sm text-(--blux-text-muted) " + HiddenAttribute)
            .Variant("size", "xs", "item", "px-1.5 py-1 text-xs").Variant("size", "sm", "item", "px-2 py-1 text-xs")
            .Variant("size", "md", "item", "px-2 py-1.5 text-sm").Variant("size", "lg", "item", "px-2.5 py-2 text-sm")
            .Variant("size", "xl", "item", "px-3 py-2 text-base")
            .Default("size", "md");
}
