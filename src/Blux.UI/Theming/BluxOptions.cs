using Blux.UI.Theming;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Blux.UI;

/// <summary>
/// Global configuration of Blux UI. Register it from code shared by the server and the WebAssembly client,
/// otherwise components change appearance when an Auto render mode switches runtime.
/// </summary>
public sealed class BluxOptions
{
    public ComponentTheme Button { get; } = Themes.Button();
    public ComponentTheme Badge { get; } = Themes.Badge();
    public ComponentTheme Avatar { get; } = Themes.Avatar();
    public ComponentTheme Card { get; } = Themes.Card();
    public ComponentTheme Alert { get; } = Themes.Alert();
    public ComponentTheme Input { get; } = Themes.Input();
    public ComponentTheme Textarea { get; } = Themes.Textarea();
    public ComponentTheme Checkbox { get; } = Themes.Checkbox();
    public ComponentTheme Switch { get; } = Themes.Switch();
    public ComponentTheme RadioGroup { get; } = Themes.RadioGroup();
    public ComponentTheme Select { get; } = Themes.Select();
    public ComponentTheme FormField { get; } = Themes.FormField();
    public ComponentTheme Popover { get; } = Themes.Popover();
    public ComponentTheme Modal { get; } = Themes.Modal();
    public ComponentTheme Tooltip { get; } = Themes.Tooltip();
    public ComponentTheme DropdownMenu { get; } = Themes.DropdownMenu();
    public ComponentTheme Toast { get; } = Themes.Toaster();
    public ComponentTheme Tabs { get; } = Themes.Tabs();
    public ComponentTheme Accordion { get; } = Themes.Accordion();
    public ComponentTheme Table { get; } = Themes.Table();

    /// <summary>Built-in texts rendered by components, for localization.</summary>
    public BluxStrings Strings { get; } = new();

    internal IEnumerable<ComponentTheme> AllThemes =>
    [
        Button, Badge, Avatar, Card, Alert, Input, Textarea, Checkbox, Switch, RadioGroup, Select, FormField,
        Popover, Modal, Tooltip, DropdownMenu, Toast, Tabs, Accordion, Table,
    ];
}

public sealed class BluxStrings
{
    public string Loading { get; set; } = "Loading";
    public string Close { get; set; } = "Close";
    public string Notifications { get; set; } = "Notifications";
    public string NoData { get; set; } = "No data";
    public string SortAscending { get; set; } = "Sort ascending";
    public string SortDescending { get; set; } = "Sort descending";
}

public static class BluxServiceCollectionExtensions
{
    /// <summary>
    /// Registers Blux UI. Call it in the server <c>Program.cs</c> and in the client one with the same configuration,
    /// ideally through a shared method in the client project.
    /// </summary>
    public static IServiceCollection AddBluxUi(this IServiceCollection services, Action<BluxOptions>? configure = null)
    {
        // A second call (from a library and the app, say) configures the same options instead of being ignored.
        var options = services.FirstOrDefault(d => d.ServiceType == typeof(BluxOptions))?.ImplementationInstance as BluxOptions;
        if (options is null)
        {
            options = new BluxOptions();
            services.AddSingleton(options);
        }
        configure?.Invoke(options);

        services.TryAddSingleton<IClassMerger, TailwindClassMerger>();
        services.TryAddSingleton<ThemeResolver>();
        services.TryAddScoped<IToastService, ToastService>();
        return services;
    }
}
