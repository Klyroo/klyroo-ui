using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyApp"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme App { get; } = Themes.App();
    /// <summary>
    /// Theme of <see cref="KlyHeader"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Header { get; } = Themes.Header();
    /// <summary>
    /// Theme of <see cref="KlyFooter"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Footer { get; } = Themes.Footer();
    /// <summary>
    /// Theme of <see cref="KlyFooterColumns"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme FooterColumns { get; } = Themes.FooterColumns();
    /// <summary>
    /// Theme of <see cref="KlyMain"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Main { get; } = Themes.Main();
    /// <summary>
    /// Theme of <see cref="KlySkipLink"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme SkipLink { get; } = Themes.SkipLink();
    /// <summary>
    /// Theme of <see cref="KlySidebar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Sidebar { get; } = Themes.Sidebar();
    /// <summary>
    /// Theme of <see cref="KlyError"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Error { get; } = Themes.Error();
    /// <summary>
    /// Theme of <see cref="KlyLocaleSelect"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme LocaleSelect { get; } = Themes.LocaleSelect();
}
