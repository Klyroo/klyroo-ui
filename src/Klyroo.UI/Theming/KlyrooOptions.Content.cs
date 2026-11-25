using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyContentNavigation"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ContentNavigation { get; } = Themes.ContentNavigation();
    /// <summary>
    /// Theme of <see cref="KlyContentToc"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ContentToc { get; } = Themes.ContentToc();
    /// <summary>
    /// Theme of <see cref="KlyContentSurround"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ContentSurround { get; } = Themes.ContentSurround();
    /// <summary>
    /// Theme of <see cref="KlyContentSearchButton"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ContentSearchButton { get; } = Themes.ContentSearchButton();
}
