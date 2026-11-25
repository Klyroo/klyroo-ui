using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyKbd"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Kbd { get; } = Themes.Kbd();
    /// <summary>
    /// Theme of <see cref="KlySeparator"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Separator { get; } = Themes.Separator();
    /// <summary>
    /// Theme of <see cref="KlySkeleton"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Skeleton { get; } = Themes.Skeleton();
    /// <summary>
    /// Theme of <see cref="KlyInfiniteScroll"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InfiniteScroll { get; } = Themes.InfiniteScroll();
    /// <summary>
    /// Theme of <see cref="KlyChip"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Chip { get; } = Themes.Chip();
    /// <summary>
    /// Theme of <see cref="KlyLink"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Link { get; } = Themes.Link();
    /// <summary>
    /// Theme of <see cref="KlyIcon"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Icon { get; } = Themes.Icon();
    /// <summary>
    /// Theme of <see cref="KlyContainer"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Container { get; } = Themes.Container();
    /// <summary>
    /// Theme of <see cref="KlyProgress"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Progress { get; } = Themes.Progress();
    /// <summary>
    /// Theme of <see cref="KlyBanner"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Banner { get; } = Themes.Banner();
    /// <summary>
    /// Theme of <see cref="KlyEmpty"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Empty { get; } = Themes.Empty();
    /// <summary>
    /// Theme of <see cref="KlyUser"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme User { get; } = Themes.User();
    /// <summary>
    /// Theme of <see cref="KlyAvatarGroup"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme AvatarGroup { get; } = Themes.AvatarGroup();
    /// <summary>
    /// Theme of <see cref="KlyFieldGroup"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme FieldGroup { get; } = Themes.FieldGroup();

    /// <summary>
    /// The icons components draw (<c>Close</c>, <c>Check</c>, <c>ChevronDown</c>...), replaceable by name, and <c>Register</c> to add icons in code.
    /// Any Iconify icon works by name, see <see cref="KlyIcon"/>.
    /// </summary>
    public KlyrooIconOptions Icons { get; } = new();
}
