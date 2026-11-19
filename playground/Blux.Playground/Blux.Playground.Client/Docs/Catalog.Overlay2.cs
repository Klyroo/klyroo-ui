using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Overlay2 =
    [
        new("drawer", "Drawer", "Overlay", "A sheet docked to the bottom, top, left or right edge, on the native modal dialog.",
            [new("Drawer", typeof(Examples.DrawerExample), "Bound open state; the slide respects prefers-reduced-motion."),
             new("Directions", typeof(Examples.DrawerDirectionExample), "The handle shows on top and bottom drawers; Inset floats the drawer.")],
            [typeof(BxDrawer)]),
        new("slideover", "Slideover", "Overlay", "A full-height panel that slides in from any edge, for details and forms.",
            [new("Slideover", typeof(Examples.SlideoverExample)), new("Sides", typeof(Examples.SlideoverSidesExample))],
            [typeof(BxSlideover)]),
        new("context-menu", "ContextMenu", "Overlay", "A menu opened at the pointer by right click, long press, Shift+F10 or the ContextMenu key.",
            [new("Context menu", typeof(Examples.ContextMenuExample), "Same items and keyboard behavior as DropdownMenu.")],
            [typeof(BxContextMenu)]),
        new("command-palette", "CommandPalette", "Overlay", "A searchable list of commands, filtered in the browser as you type.",
            [new("Inline", typeof(Examples.CommandPaletteExample)),
             new("In a modal with Ctrl+K", typeof(Examples.CommandPaletteModalExample), "data-bx-hotkey opens the modal from the keyboard, without .NET.")],
            [typeof(BxCommandPalette)]),
        new("color-mode-button", "ColorModeButton", "Overlay", "A button that switches between light and dark.",
            [new("Color mode button", typeof(Examples.ColorModeButtonExample), "Handled in the browser: it works on static pages and every color mode control stays in sync.")],
            [typeof(BxColorModeButton)]),
        new("color-mode-switch", "ColorModeSwitch", "Overlay", "A switch that is on in dark mode.",
            [new("Color mode switch", typeof(Examples.ColorModeSwitchExample))], [typeof(BxColorModeSwitch)]),
        new("color-mode-select", "ColorModeSelect", "Overlay", "A select of System, Light and Dark.",
            [new("Color mode select", typeof(Examples.ColorModeSelectExample))], [typeof(BxColorModeSelect)]),
        new("color-mode-avatar", "ColorModeAvatar", "Overlay", "An avatar with one image per color mode.",
            [new("Color mode avatar", typeof(Examples.ColorModeAvatarExample), "Both images are rendered and CSS shows the right one.")], [typeof(BxColorModeAvatar)]),
        new("color-mode-image", "ColorModeImage", "Overlay", "An image with one source per color mode.",
            [new("Color mode image", typeof(Examples.ColorModeImageExample))], [typeof(BxColorModeImage)]),
    ];
}
