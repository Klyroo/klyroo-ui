using Klyroo.Maui.Shared.Services;

namespace Klyroo.Maui.Web.Services;

public class FormFactor : IFormFactor
{
    public string GetFormFactor() => "Web";

    public string GetPlatform() => Environment.OSVersion.ToString();
}
