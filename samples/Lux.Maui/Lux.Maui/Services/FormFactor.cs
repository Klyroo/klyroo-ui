using Lux.Maui.Shared.Services;

namespace Lux.Maui.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor()
    {
#if LUX_TV
        return "TV";
#else
        return DeviceInfo.Idiom == DeviceIdiom.TV ? "TV" : DeviceInfo.Idiom.ToString();
#endif
    }

    public string GetPlatform() => DeviceInfo.Platform + " - " + DeviceInfo.VersionString;
}
