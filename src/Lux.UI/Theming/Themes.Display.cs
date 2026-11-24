namespace Lux.UI.Theming;

internal static partial class Themes
{
    public static ComponentTheme Badge() =>
        new ComponentTheme("badge", "base", "label", "leadingIcon", "trailingIcon")
            .Slot("base", "inline-flex items-center font-medium whitespace-nowrap")
            .Slot("label", "truncate")
            .Slot("leadingIcon", "shrink-0")
            .Slot("trailingIcon", "shrink-0")
            .Variant("variant", "solid", "base", "bg-(--lx-color) ring ring-inset ring-(--lx-color-text) text-(--lx-color-contrast)")
            .Variant("variant", "outline", "base", "ring ring-inset ring-(--lx-color-text)/75 text-(--lx-color-text)")
            .Variant("variant", "soft", "base", "bg-(--lx-color)/10 text-(--lx-color-text)")
            .Variant("variant", "subtle", "base", "bg-(--lx-color)/10 ring ring-inset ring-(--lx-color-text)/75 text-(--lx-color-text)")
            .Variant("size", "xs", "base", "text-[10px]/3 px-1 py-0.5 gap-1 rounded-sm")
            .Variant("size", "xs", "leadingIcon", "size-3")
            .Variant("size", "xs", "trailingIcon", "size-3")
            .Variant("size", "sm", "base", "text-[11px]/3 px-1.5 py-1 gap-1 rounded-sm")
            .Variant("size", "sm", "leadingIcon", "size-3")
            .Variant("size", "sm", "trailingIcon", "size-3")
            .Variant("size", "md", "base", "text-xs px-2 py-1 gap-1 rounded-(--lux-radius)")
            .Variant("size", "md", "leadingIcon", "size-4")
            .Variant("size", "md", "trailingIcon", "size-4")
            .Variant("size", "lg", "base", "text-sm px-2 py-1 gap-1.5 rounded-(--lux-radius)")
            .Variant("size", "lg", "leadingIcon", "size-5")
            .Variant("size", "lg", "trailingIcon", "size-5")
            .Variant("size", "xl", "base", "text-base px-2.5 py-1 gap-1.5 rounded-(--lux-radius)")
            .Variant("size", "xl", "leadingIcon", "size-6")
            .Variant("size", "xl", "trailingIcon", "size-6")
            .Default("variant", "solid")
            .Default("size", "md");

    public static ComponentTheme Avatar() =>
        new ComponentTheme("avatar", "root", "image", "fallback")
            .Slot("root", "relative inline-flex items-center justify-center shrink-0 select-none overflow-hidden rounded-full align-middle bg-(--lux-bg-accented)")
            .Slot("image", "absolute inset-0 size-full object-cover")
            .Slot("fallback", "font-medium leading-none text-(--lux-text) truncate")
            .Variant("size", "xs", "root", "size-6 text-[10px]")
            .Variant("size", "sm", "root", "size-7 text-xs")
            .Variant("size", "md", "root", "size-8 text-sm")
            .Variant("size", "lg", "root", "size-10 text-base")
            .Variant("size", "xl", "root", "size-12 text-lg")
            .Default("size", "md");

    public static ComponentTheme Card() =>
        new ComponentTheme("card", "root", "header", "body", "footer")
            .Slot("root", "overflow-hidden rounded-[calc(var(--lux-radius)*2)]")
            .Slot("header", "p-4 sm:px-6")
            .Slot("body", "p-4 sm:p-6")
            .Slot("footer", "p-4 sm:px-6")
            .Variant("variant", "outline", "root", "bg-(--lux-bg) ring ring-(--lux-border) divide-y divide-(--lux-border)")
            .Variant("variant", "soft", "root", "bg-(--lux-bg-elevated)/50 divide-y divide-(--lux-border)")
            .Variant("variant", "subtle", "root", "bg-(--lux-bg-elevated)/50 ring ring-(--lux-border) divide-y divide-(--lux-border)")
            .Default("variant", "outline");

    public static ComponentTheme Alert() =>
        new ComponentTheme("alert", "root", "icon", "wrapper", "title", "description", "actions", "close")
            .Slot("root", "relative flex w-full gap-2.5 overflow-hidden rounded-(--lux-radius) p-4")
            .Slot("icon", "size-5 shrink-0")
            .Slot("wrapper", "flex min-w-0 flex-1 flex-col gap-1")
            .Slot("title", "text-sm font-medium")
            .Slot("description", "text-sm")
            .Slot("actions", "flex flex-wrap items-center gap-1.5 shrink-0")
            .Slot("close", "-m-1 shrink-0 self-start rounded-(--lux-radius) p-1 opacity-80 hover:opacity-100 disabled:cursor-not-allowed focus-visible:outline-2 focus-visible:outline-current")
            .Variant("variant", "solid", "root", "bg-(--lx-color) ring ring-inset ring-(--lx-color-text) text-(--lx-color-contrast)")
            .Variant("variant", "outline", "root", "text-(--lx-color-text) ring ring-inset ring-(--lx-color-text)/75")
            .Variant("variant", "soft", "root", "bg-(--lx-color)/10 text-(--lx-color-text)")
            .Variant("variant", "subtle", "root", "bg-(--lx-color)/10 text-(--lx-color-text) ring ring-inset ring-(--lx-color-text)/75")
            .Default("variant", "solid");
}
