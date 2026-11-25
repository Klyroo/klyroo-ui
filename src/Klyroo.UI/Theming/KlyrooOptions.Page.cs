using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyPage"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Page { get; } = Themes.Page();
    /// <summary>
    /// Theme of <see cref="KlyPageHeader"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageHeader { get; } = Themes.PageHeader();
    /// <summary>
    /// Theme of <see cref="KlyPageHero"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageHero { get; } = Themes.PageHero();
    /// <summary>
    /// Theme of <see cref="KlyPageSection"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageSection { get; } = Themes.PageSection();
    /// <summary>
    /// Theme of <see cref="KlyPageFeature"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageFeature { get; } = Themes.PageFeature();
    /// <summary>
    /// Theme of <see cref="KlyPageGrid"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageGrid { get; } = Themes.PageGrid();
    /// <summary>
    /// Theme of <see cref="KlyPageColumns"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageColumns { get; } = Themes.PageColumns();
    /// <summary>
    /// Theme of <see cref="KlyPageCard"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageCard { get; } = Themes.PageCard();
    /// <summary>
    /// Theme of <see cref="KlyPageCta"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageCta { get; } = Themes.PageCta();
    /// <summary>
    /// Theme of <see cref="KlyPageLinks"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageLinks { get; } = Themes.PageLinks();
    /// <summary>
    /// Theme of <see cref="KlyPageList"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageList { get; } = Themes.PageList();
    /// <summary>
    /// Theme of <see cref="KlyPageLogos"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageLogos { get; } = Themes.PageLogos();
    /// <summary>
    /// Theme of <see cref="KlyPageAnchors"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageAnchors { get; } = Themes.PageAnchors();
    /// <summary>
    /// Theme of <see cref="KlyPageAside"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageAside { get; } = Themes.PageAside();
    /// <summary>
    /// Theme of <see cref="KlyPageBody"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PageBody { get; } = Themes.PageBody();
}
