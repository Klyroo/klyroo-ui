using Blux.UI.Tools;
using Blux.UI.Tools.Query;
using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

public class RegistrationTests
{
    private static BluxOptions Core(IServiceCollection services) =>
        (BluxOptions)services.Single(d => d.ServiceType == typeof(BluxOptions)).ImplementationInstance!;

    [Fact]
    public void AddBluxUiTools_registers_Blux_UI_when_it_is_missing()
    {
        var services = new ServiceCollection();

        services.AddBluxUiTools();

        Assert.Single(services, d => d.ServiceType == typeof(BluxOptions));
        Assert.Contains(services, d => d.ServiceType == typeof(IToastService));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void It_shares_the_options_of_Blux_UI_whichever_registers_first(bool coreFirst)
    {
        var services = new ServiceCollection();
        var coreCalls = 0;

        if (coreFirst) services.AddBluxUi(_ => coreCalls++);
        services.AddBluxUiTools(t => t.DataListEmpty.Slot("root", "p-1"));
        if (!coreFirst) services.AddBluxUi(_ => coreCalls++);

        Assert.Single(services, d => d.ServiceType == typeof(BluxOptions));
        var tools = Core(services).Extension<BluxToolsOptions>();
        Assert.Equal(1, coreCalls);
        Assert.Contains("p-1", tools.DataListEmpty.Slots["root"]);
    }

    [Fact]
    public void The_tools_themes_are_listed_with_those_of_Blux_UI()
    {
        var services = new ServiceCollection();
        services.AddBluxUiTools();
        var options = Core(services);

        Assert.Contains(options.Extension<BluxToolsOptions>().DataListEmpty, options.AllThemes);
        Assert.Contains(options.Button, options.AllThemes);
    }

    [Fact]
    public void Calling_it_without_a_configuration_still_creates_the_options_of_Tools()
    {
        var services = new ServiceCollection();
        services.AddBluxUiTools();

        Assert.Single(Core(services).Extensions, e => e is BluxToolsOptions);
    }

    [Fact]
    public void Two_calls_configure_the_same_theme()
    {
        var services = new ServiceCollection();
        services.AddBluxUiTools(t => t.DataListEmpty.Slot("root", "a-1"));
        services.AddBluxUiTools(t => t.DataListEmpty.Slot("root", "b-1"));

        var root = Core(services).Extension<BluxToolsOptions>().DataListEmpty.Slots["root"];
        Assert.Contains("a-1", root);
        Assert.Contains("b-1", root);
    }

    [Fact]
    public void The_themes_of_Tools_freeze_with_the_first_render_like_those_of_Blux_UI()
    {
        using var context = new FreezeContext();
        context.Render<BxDataListEmpty>();
        var tools = context.Services.GetRequiredService<BluxOptions>().Extension<BluxToolsOptions>();

        Assert.Throws<InvalidOperationException>(() => tools.DataListEmpty.Slot("root", "late"));
    }

    private sealed class FreezeContext : BluxToolsTestContext;

    [Fact]
    public void The_query_package_is_referenced_and_has_no_aspnet_or_blux_dependency()
    {
        var assembly = typeof(QueryAssembly).Assembly;

        Assert.Equal(QueryAssembly.Name, assembly.GetName().Name);
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(), r => r.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) || r.Name.StartsWith("Blux.UI", StringComparison.Ordinal));
    }
}
