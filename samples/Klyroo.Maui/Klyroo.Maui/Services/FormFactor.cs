using Klyroo.Maui.Shared.Services;

namespace Klyroo.Maui.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
#if KLYROO_TV
        return "TV";
#else
        return DeviceInfo.Idiom == DeviceIdiom.TV ? "TV" : DeviceInfo.Idiom.ToString();
#endif
    }

    public string GetPlatform() => DeviceInfo.Platform + " - " + DeviceInfo.VersionString;
}
