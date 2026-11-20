using Blux.UI.Theming;

namespace Blux.UI;

public sealed partial class BluxOptions
{
    public ComponentTheme Calendar { get; } = Themes.Calendar();
    public ComponentTheme InputDate { get; } = Themes.InputDate();
    public ComponentTheme InputTime { get; } = Themes.InputTime();
    public ComponentTheme ColorPicker { get; } = Themes.ColorPicker();
    public ComponentTheme FileUpload { get; } = Themes.FileUpload();
    public ComponentTheme Carousel { get; } = Themes.Carousel();
}
