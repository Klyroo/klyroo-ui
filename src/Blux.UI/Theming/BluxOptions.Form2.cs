using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme SelectMenu { get; } = Themes.SelectMenu();
    public ComponentTheme InputMenu { get; } = Themes.InputMenu();
    public ComponentTheme InputNumber { get; } = Themes.InputNumber();
    public ComponentTheme InputTags { get; } = Themes.InputTags();
    public ComponentTheme PinInput { get; } = Themes.PinInput();
    public ComponentTheme Slider { get; } = Themes.Slider();
    public ComponentTheme CheckboxGroup { get; } = Themes.CheckboxGroup();
    public ComponentTheme Toggle { get; } = Themes.Toggle();
    public ComponentTheme ToggleGroup { get; } = Themes.ToggleGroup();
    public ComponentTheme Listbox { get; } = Themes.Listbox();
    public ComponentTheme InputRating { get; } = Themes.InputRating();
    public ComponentTheme Form { get; } = Themes.Form();
}
