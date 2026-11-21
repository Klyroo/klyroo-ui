using Blux.UI.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

public abstract class BluxToolsTestContext : BunitContext
{
    protected Bunit.TestDoubles.BunitPersistentComponentState? Persisted { get; }

    protected BluxToolsTestContext(Action<BluxToolsOptions>? configure = null, Action<BluxOptions>? configureCore = null, bool interactive = true, Action<IServiceCollection>? configureServices = null, bool persistentState = false)
    {
        if (configureCore is not null) Services.AddBluxUi(configureCore);
        Services.AddBluxUiTools(configure);
        configureServices?.Invoke(Services);
        // What a prerender handed to the interactive render: the test persists the keys it wants the list to find.
        if (persistentState) Persisted = this.AddBunitPersistentComponentState();
        // The browser side (time zone, focus) answers nothing here: the components fall back, and the tests that care set up the calls.
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo(interactive ? "Server" : "Static", interactive));
    }
}
