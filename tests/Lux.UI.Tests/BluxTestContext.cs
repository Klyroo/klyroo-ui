using Microsoft.Extensions.DependencyInjection;

namespace Lux.UI.Tests;

public abstract class BluxTestContext : BunitContext
{
    protected BluxTestContext(Action<LuxOptions>? configure = null, bool interactive = true)
    {
        Services.AddLuxUi(configure);
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}
