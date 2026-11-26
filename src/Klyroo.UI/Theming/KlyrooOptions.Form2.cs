using Klyroo.UI.Theming;

namespace Klyroo.UI;

public sealed partial class KlyrooOptions
{
    /// <summary>
    /// Theme of <see cref="KlySelectMenu{TValue}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme SelectMenu { get; } = Themes.SelectMenu();
    /// <summary>
    /// Theme of <see cref="KlyInputMenu{TValue}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputMenu { get; } = Themes.InputMenu();
    /// <summary>
    /// Theme of <see cref="KlyInputNumber{TNumber}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputNumber { get; } = Themes.InputNumber();
    /// <summary>
    /// Theme of <see cref="KlyInputTags"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputTags { get; } = Themes.InputTags();
    /// <summary>
    /// Theme of <see cref="KlyPinInput"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme PinInput { get; } = Themes.PinInput();
    /// <summary>
    /// Theme of <see cref="KlySlider{TNumber}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Slider { get; } = Themes.Slider();
    /// <summary>
    /// Theme of <see cref="KlyCheckboxGroup{TValue}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme CheckboxGroup { get; } = Themes.CheckboxGroup();
    /// <summary>
    /// Theme of <see cref="KlyToggle"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Toggle { get; } = Themes.Toggle();
    /// <summary>
    /// Theme of <see cref="KlyToggleGroup"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme ToggleGroup { get; } = Themes.ToggleGroup();
    /// <summary>
    /// Theme of <see cref="KlyListbox{TValue}"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Listbox { get; } = Themes.Listbox();
    /// <summary>
    /// Theme of <see cref="KlyInputRating"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme InputRating { get; } = Themes.InputRating();
    /// <summary>
    /// Theme of <see cref="KlyForm"/>: the default classes of its slots, its variants and its defaults. Configure it in <c>AddKlyrooUi</c>; it becomes read-only the first time a component renders with it.
    /// </summary>
    public ComponentTheme Form { get; } = Themes.Form();
}
