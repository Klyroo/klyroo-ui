using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyDrawer"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Drawer { get; } = Themes.Drawer();
    /// <summary>
    /// Theme of <see cref="KlySlideover"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Slideover { get; } = Themes.Slideover();
    /// <summary>
    /// Theme of <see cref="KlyContextMenu"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ContextMenu { get; } = Themes.ContextMenu();
    /// <summary>
    /// Theme of <see cref="KlyCommandPalette"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme CommandPalette { get; } = Themes.CommandPalette();
    /// <summary>
    /// Theme of <see cref="KlyColorModeButton"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorModeButton { get; } = Themes.ColorModeButton();
    /// <summary>
    /// Theme of <see cref="KlyColorModeSwitch"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorModeSwitch { get; } = Themes.ColorModeSwitch();
    /// <summary>
    /// Theme of <see cref="KlyColorModeSelect"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorModeSelect { get; } = Themes.ColorModeSelect();
    /// <summary>
    /// Theme of <see cref="KlyColorModeAvatar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorModeAvatar { get; } = Themes.ColorModeAvatar();
    /// <summary>
    /// Theme of <see cref="KlyColorModeImage"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorModeImage { get; } = Themes.ColorModeImage();
}
