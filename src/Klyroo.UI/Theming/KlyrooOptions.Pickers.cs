using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlyCalendar"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Calendar { get; } = Themes.Calendar();
    /// <summary>
    /// Theme of <see cref="KlyInputDate"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputDate { get; } = Themes.InputDate();
    /// <summary>
    /// Theme of <see cref="KlyInputTime"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputTime { get; } = Themes.InputTime();
    /// <summary>
    /// Theme of <see cref="KlyColorPicker"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ColorPicker { get; } = Themes.ColorPicker();
    /// <summary>
    /// Theme of <see cref="KlyFileUpload"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme FileUpload { get; } = Themes.FileUpload();
    /// <summary>
    /// Theme of <see cref="KlyCarousel"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Carousel { get; } = Themes.Carousel();
}
