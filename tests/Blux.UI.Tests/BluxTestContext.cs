using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tests;

public abstract class BluxTestContext : BunitContext
{
    protected BluxTestContext(Action<BluxOptions>? configure = null, bool interactive = true)
    {
        Services.AddBluxUi(configure);
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}
