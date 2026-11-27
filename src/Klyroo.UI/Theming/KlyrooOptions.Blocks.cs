using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyHeading"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Heading { get; } = Themes.Heading();
    /// <summary>
    /// Theme of <see cref="KlyText"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Text { get; } = Themes.Text();
    /// <summary>
    /// Theme of <see cref="KlyStat"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Stat { get; } = Themes.Stat();
    /// <summary>
    /// Theme of <see cref="KlyStatGroup"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme StatGroup { get; } = Themes.StatGroup();
    /// <summary>
    /// Theme of <see cref="KlyFacts"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Facts { get; } = Themes.Facts();
    /// <summary>
    /// Theme of <see cref="KlyBreakdown"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Breakdown { get; } = Themes.Breakdown();
    /// <summary>
    /// Theme of <see cref="KlyDiff"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Diff { get; } = Themes.Diff();
    /// <summary>
    /// Theme of <see cref="KlyRecordList"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme RecordList { get; } = Themes.RecordList();
    /// <summary>
    /// Theme of <see cref="KlyList"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme List { get; } = Themes.List();
    /// <summary>
    /// Theme of <see cref="KlyDateTile"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme DateTile { get; } = Themes.DateTile();
    /// <summary>
    /// Theme of <see cref="KlyMeter"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Meter { get; } = Themes.Meter();
    /// <summary>
    /// Theme of <see cref="KlyImageGallery"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ImageGallery { get; } = Themes.ImageGallery();
}
