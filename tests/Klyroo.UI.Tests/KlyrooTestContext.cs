using Microsoft.Extensions.DependencyInjection;

namespace Klyroo.UI.Tests;

public abstract class KlyrooTestContext : BunitContext
{
    protected KlyrooTestContext(Action<KlyrooOptions>? configure = null, bool interactive = true)
    {
        Services.AddKlyrooUi(configure);
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}
