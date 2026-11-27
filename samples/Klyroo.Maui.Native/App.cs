namespace Klyroo.Maui.Native;

public class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        new(new ShowcasePage()) { Title = "Klyroo.UI.Maui" };
}
