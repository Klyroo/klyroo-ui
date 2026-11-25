namespace Klyroo.UI.Theming;

/// <summary>Themes of the chat family.</summary>
internal static partial class Themes
{
    private const string ChatFocus =
        "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-(--klyroo-primary-text)";

    private const string ChatTrigger =
        "inline-flex max-w-full min-w-0 items-center gap-1.5 text-start text-sm text-(--klyroo-text-muted) transition-colors hover:text-(--klyroo-text) " +
        "disabled:cursor-not-allowed disabled:opacity-60 " + ChatFocus;

    public static ComponentTheme ChatMessage()
    {
        const string surface = "rounded-(--klyroo-radius) px-3 py-2";
        return new ComponentTheme("chatMessage", "root", "container", "leading", "wrapper", "header", "files", "content", "text", "actions")
            .Slot("root", "flex w-full min-w-0 text-sm text-(--klyroo-text)")
            .Slot("container", "flex min-w-0 max-w-full items-start gap-3")
            .Slot("leading", "shrink-0")
            .Slot("wrapper", "flex min-w-0 flex-col gap-1")
            .Slot("header", "text-xs text-(--klyroo-text-muted)")
            .Slot("files", "flex flex-wrap gap-2")
            .Slot("content", "min-w-0 max-w-full wrap-anywhere")
            .Slot("text", "whitespace-pre-wrap")
            .Slot("actions", "flex items-center gap-1 text-(--klyroo-text-muted)")

            .Variant("side", "left", "root", "justify-start")
            .Variant("side", "right", "root", "justify-end")
            .Variant("side", "right", "container", "max-w-[75%] flex-row-reverse")
            .Variant("side", "right", "wrapper", "items-end")

            .Variant("variant", "solid", "content", surface + " bg-(--kly-color) ring ring-inset ring-(--kly-color-text) text-(--kly-color-contrast)")
            .Variant("variant", "outline", "content", surface + " ring ring-inset ring-(--kly-color-text)/75 text-(--klyroo-text)")
            .Variant("variant", "soft", "content", surface + " bg-(--kly-color)/10 text-(--klyroo-text)")
            .Variant("variant", "subtle", "content", surface + " bg-(--kly-color)/10 ring ring-inset ring-(--kly-color-text)/75 text-(--klyroo-text)")
            .Variant("variant", "naked", "content", "py-1")

            .Variant("role", "system", "root", "justify-center")
            .Variant("role", "system", "wrapper", "items-center text-center")
            .Variant("role", "system", "content", "text-xs text-(--klyroo-text-muted)")

            .Compound(new() { ["compact"] = "true", ["variant"] = "solid" }, "content", "px-2.5 py-1.5")
            .Compound(new() { ["compact"] = "true", ["variant"] = "outline" }, "content", "px-2.5 py-1.5")
            .Compound(new() { ["compact"] = "true", ["variant"] = "soft" }, "content", "px-2.5 py-1.5")
            .Compound(new() { ["compact"] = "true", ["variant"] = "subtle" }, "content", "px-2.5 py-1.5")
            .Variant("compact", "true", "root", "text-sm")
            .Variant("compact", "true", "container", "gap-2")

            .Variant("streaming", "true", "content",
                "after:ms-0.5 after:inline-block after:h-4 after:w-1.5 after:translate-y-0.5 after:rounded-[1px] after:bg-current after:content-[''] motion-safe:after:animate-pulse")

            .Default("side", "left")
            .Default("variant", "naked")
            .Default("role", "assistant")
            .Default("compact", "false")
            .Default("streaming", "false");
    }

    public static ComponentTheme ChatMessages() =>
        new ComponentTheme("chatMessages", "root", "viewport", "content", "indicator", "dot", "error", "jump", "jumpIcon", "srOnly", "status")
            .Slot("root", "relative flex min-h-0 min-w-0 flex-1 flex-col")
            .Slot("viewport", "flex min-h-0 flex-1 flex-col overflow-y-auto overscroll-contain focus-visible:outline-2 focus-visible:-outline-offset-2 focus-visible:outline-(--klyroo-primary-text)")
            .Slot("content", "flex flex-col gap-4 p-4")
            .Slot("indicator", "flex items-center gap-1 px-1 py-2")
            .Slot("dot", "size-1.5 rounded-full bg-(--klyroo-text-muted) motion-safe:animate-pulse nth-2:[animation-delay:150ms] nth-3:[animation-delay:300ms]")
            .Slot("error", "rounded-(--klyroo-radius) bg-(--klyroo-error)/10 px-3 py-2 text-sm text-(--klyroo-error-text)")
            .Slot("jump", "absolute bottom-3 start-1/2 z-10 inline-flex -translate-x-1/2 rtl:translate-x-1/2 items-center gap-1.5 rounded-full bg-(--klyroo-bg-elevated) px-3 py-1.5 text-xs font-medium text-(--klyroo-text) " +
                          "shadow-md ring ring-inset ring-(--klyroo-border-accented) transition-colors hover:bg-(--klyroo-bg-accented) " + ChatFocus)
            .Slot("jumpIcon", "size-4")
            .Slot("srOnly", "sr-only")
            .Slot("status", "sr-only");

    public static ComponentTheme ChatPrompt() =>
        new ComponentTheme("chatPrompt", "root", "form", "attachments", "row", "textarea", "trailing", "footer")
            .Slot("root", "block w-full min-w-0")
            .Slot("form", "flex w-full flex-col gap-1.5 rounded-xl p-2 transition-colors")
            .Slot("attachments", "flex flex-wrap gap-2")
            .Slot("row", "flex items-end gap-1.5")
            .Slot("textarea", "block min-h-0 w-full min-w-0 flex-1 resize-none border-0 bg-transparent px-1.5 py-1.5 text-sm text-(--klyroo-text) " +
                              "placeholder:text-(--klyroo-text-dimmed) focus:outline-none disabled:cursor-not-allowed disabled:opacity-60")
            .Slot("trailing", "flex shrink-0 items-center gap-1")
            .Slot("footer", "flex items-center justify-between gap-2")
            .Variant("variant", "outline", "form", "bg-(--klyroo-bg) ring ring-inset ring-(--klyroo-border-accented) focus-within:ring-2 focus-within:ring-(--kly-color-text)")
            .Variant("variant", "soft", "form", "bg-(--klyroo-bg-elevated)/50 hover:bg-(--klyroo-bg-elevated) focus-within:bg-(--klyroo-bg-elevated) focus-within:ring-2 focus-within:ring-inset focus-within:ring-(--kly-color-text)")
            .Variant("variant", "subtle", "form", "bg-(--klyroo-bg-elevated) ring ring-inset ring-(--klyroo-border-accented) focus-within:ring-2 focus-within:ring-(--kly-color-text)")
            .Variant("variant", "ghost", "form", "bg-transparent hover:bg-(--klyroo-bg-elevated) focus-within:bg-(--klyroo-bg-elevated)")
            .Variant("variant", "none", "form", "bg-transparent")
            .Default("variant", "outline");

    public static ComponentTheme ChatPromptSubmit() =>
        new ComponentTheme("chatPromptSubmit", "base", "icon", "stopIcon")
            .Slot("base", "shrink-0")
            .Slot("icon", "size-5")
            .Slot("stopIcon", "size-4 fill-current");

    public static ComponentTheme ChatPalette() =>
        new ComponentTheme("chatPalette", "root", "content", "prompt")
            .Slot("root", "flex size-full min-h-0 min-w-0 flex-col")
            .Slot("content", "flex min-h-0 flex-1 flex-col")
            .Slot("prompt", "shrink-0 border-t border-(--klyroo-border) p-2");

    public static ComponentTheme ChatAttachment()
    {
        var t = new ComponentTheme("chatAttachment", "root", "leading", "leadingIcon", "image", "wrapper", "name", "description", "remove", "removeIcon", "srOnly")
            .Slot("root", "relative flex min-w-0 max-w-full items-center gap-2 text-start text-(--klyroo-text)")
            .Slot("leading", "relative flex shrink-0 items-center justify-center overflow-hidden rounded-[calc(var(--klyroo-radius)-2px)] bg-(--klyroo-bg-accented) text-(--klyroo-text-muted)")
            .Slot("leadingIcon", "size-1/2 shrink-0")
            .Slot("image", "size-full object-cover")
            .Slot("wrapper", "flex min-w-0 flex-col")
            .Slot("name", "truncate font-medium text-(--klyroo-text)")
            .Slot("description", "truncate text-xs text-(--klyroo-text-muted)")
            .Slot("remove", "ms-auto shrink-0")
            .Slot("removeIcon", "size-4")
            .Slot("srOnly", "sr-only")

            .Variant("variant", "card", "root", "rounded-(--klyroo-radius) bg-(--klyroo-bg-elevated) p-1.5 ring ring-inset ring-(--klyroo-border-accented)")
            .Variant("variant", "thumbnail", "root", "overflow-hidden rounded-(--klyroo-radius) p-0 ring ring-inset ring-(--klyroo-border-accented)")
            .Variant("variant", "thumbnail", "leading", "size-full rounded-none")
            .Variant("variant", "thumbnail", "wrapper", "sr-only")
            .Variant("variant", "thumbnail", "remove", "absolute end-0.5 top-0.5 ms-0")

            .Variant("size", "xs", "root", "text-xs")
            .Variant("size", "sm", "root", "text-xs")
            .Variant("size", "md", "root", "text-sm")
            .Variant("size", "lg", "root", "text-sm")
            .Variant("size", "xl", "root", "text-base")
            .Variant("size", "xs", "leading", "h-6 w-6")
            .Variant("size", "sm", "leading", "h-7 w-7")
            .Variant("size", "md", "leading", "h-9 w-9")
            .Variant("size", "lg", "leading", "h-10 w-10")
            .Variant("size", "xl", "leading", "h-12 w-12")

            .Variant("status", "uploading", "image", "opacity-60")
            .Variant("status", "uploading", "leadingIcon", "motion-safe:animate-spin")
            .Variant("status", "error", "root", "bg-(--klyroo-error)/10 ring-(--klyroo-error)/50")
            .Variant("status", "error", "leading", "bg-(--klyroo-error)/15 text-(--klyroo-error-text)")
            .Variant("status", "error", "description", "text-(--klyroo-error-text)")

            .Compound(new() { ["variant"] = "card", ["orientation"] = "vertical" }, "root", "flex-col items-stretch")
            .Compound(new() { ["variant"] = "card", ["orientation"] = "vertical" }, "wrapper", "px-0.5")
            .Compound(new() { ["variant"] = "card", ["orientation"] = "vertical", ["preview"] = "true" }, "leading", "h-32 w-full")
            .Compound(new() { ["variant"] = "card", ["orientation"] = "vertical" }, "remove", "absolute end-2 top-2 ms-0")

            .Default("variant", "card")
            .Default("size", "md")
            .Default("status", "idle")
            .Default("orientation", "horizontal")
            .Default("preview", "false");

        foreach (var (size, box) in new[] { ("xs", "h-10 w-10"), ("sm", "h-12 w-12"), ("md", "h-16 w-16"), ("lg", "h-20 w-20"), ("xl", "h-24 w-24") })
        {
            t.Compound(new() { ["variant"] = "thumbnail", ["size"] = size }, "root", box);
        }
        return t;
    }

    public static ComponentTheme ChatReasoning() =>
        new ComponentTheme("chatReasoning", "root", "trigger", "leading", "leadingIcon", "chevronIcon", "label", "content", "body")
            .Slot("root", "group/disclosure min-w-0 text-sm")
            .Slot("trigger", "rounded-(--klyroo-radius) py-1 " + ChatTrigger)
            .Slot("leading", "relative inline-flex size-4 shrink-0 items-center justify-center")
            .Slot("leadingIcon", "size-4")
            .Slot("chevronIcon", "size-4 shrink-0 transition-transform motion-reduce:transition-none group-data-[state=open]/disclosure:rotate-180")
            .Slot("label", "truncate")
            .Slot("content", "overflow-hidden")
            .Slot("body", "max-h-[200px] overflow-y-auto border-s-2 border-(--klyroo-border) py-1 ps-3 text-(--klyroo-text-muted) " + ChatFocus);

    public static ComponentTheme ChatShimmer() =>
        new ComponentTheme("chatShimmer", "base")
            .Slot("base", "inline-block animate-[kly-chat-shimmer_2s_linear_infinite] bg-[linear-gradient(110deg,var(--klyroo-text-muted)_40%,var(--klyroo-text-highlighted)_50%,var(--klyroo-text-muted)_60%)] " +
                          "bg-[length:250%_100%] bg-clip-text text-transparent " +
                          "motion-reduce:animate-none motion-reduce:bg-none motion-reduce:text-(--klyroo-text-muted)");

    public static ComponentTheme ChatTool() =>
        new ComponentTheme("chatTool", "root", "trigger", "leading", "leadingIcon", "chevronIcon", "label", "suffix", "content", "body", "actions", "spin", "srOnly")
            .Slot("root", "group/disclosure min-w-0 text-sm")
            .Slot("trigger", "rounded-(--klyroo-radius) py-1 " + ChatTrigger)
            .Slot("leading", "relative inline-flex size-4 shrink-0 items-center justify-center")
            .Slot("leadingIcon", "size-4")
            .Slot("chevronIcon", "size-4 shrink-0 transition-transform motion-reduce:transition-none group-data-[state=open]/disclosure:rotate-180")
            .Slot("label", "truncate font-medium")
            .Slot("suffix", "truncate font-normal")
            .Slot("content", "overflow-hidden")
            .Slot("body", "max-h-[200px] overflow-auto font-mono text-xs text-(--klyroo-text-muted) " + ChatFocus)
            .Slot("actions", "flex flex-wrap items-center gap-2 py-1")
            .Slot("spin", "motion-safe:animate-spin")
            .Slot("srOnly", "sr-only")
            .Variant("variant", "inline", "body", "border-s-2 border-(--klyroo-border) py-1 ps-3")
            .Variant("variant", "card", "root", "rounded-(--klyroo-radius) bg-(--klyroo-bg-elevated)/50 ring ring-inset ring-(--klyroo-border-accented)")
            .Variant("variant", "card", "trigger", "w-full px-3 py-2")
            .Variant("variant", "card", "body", "border-t border-(--klyroo-border) p-3")
            .Variant("variant", "card", "actions", "px-3 pb-2")
            .Variant("error", "true", "label", "text-(--klyroo-error-text)")
            .Variant("error", "true", "leadingIcon", "text-(--klyroo-error-text)")
            .Default("variant", "inline")
            .Default("error", "false");
}
