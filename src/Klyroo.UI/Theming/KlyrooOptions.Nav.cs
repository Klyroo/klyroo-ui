using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyBreadcrumb"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Breadcrumb { get; } = Themes.Breadcrumb();
    /// <summary>
    /// Theme of <see cref="KlyPagination"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Pagination { get; } = Themes.Pagination();
    /// <summary>
    /// Theme of <see cref="KlyStepper"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Stepper { get; } = Themes.Stepper();
    /// <summary>
    /// Theme of <see cref="KlyTimeline"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Timeline { get; } = Themes.Timeline();
    /// <summary>
    /// Theme of <see cref="KlyNavigationMenu"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme NavigationMenu { get; } = Themes.NavigationMenu();
    /// <summary>
    /// Theme of <see cref="KlyCollapsible"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Collapsible { get; } = Themes.Collapsible();
    /// <summary>
    /// Theme of <see cref="KlyTree"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Tree { get; } = Themes.Tree();
    /// <summary>
    /// Theme of <see cref="KlyMarquee"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Marquee { get; } = Themes.Marquee();
    /// <summary>
    /// Theme of <see cref="KlyScrollArea"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ScrollArea { get; } = Themes.ScrollArea();
    /// <summary>
    /// Theme of <see cref="KlySplitter"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Splitter { get; } = Themes.Splitter();
}
