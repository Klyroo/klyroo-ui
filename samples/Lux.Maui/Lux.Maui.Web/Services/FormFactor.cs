using Lux.Maui.Shared.Services;

namespace Lux.Maui.Web.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() => "Web";

    public string GetPlatform() => Environment.OSVersion.ToString();
}
