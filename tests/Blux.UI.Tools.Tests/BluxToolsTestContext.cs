using Blux.UI.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

public abstract class BluxToolsTestContext : BunitContext
{
    protected BluxToolsTestContext(Action<BluxToolsOptions>? configure = null, Action<BluxOptions>? configureCore = null, bool interactive = true)
    {
        if (configureCore is not null) Services.AddBluxUi(configureCore);
        Services.AddBluxUiTools(configure);
        // The browser side (time zone, focus) answers nothing here: the components fall back, and the tests that care set up the calls.
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}
