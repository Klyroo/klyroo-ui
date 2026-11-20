using System.Text.Json;

namespace Blux.UI.Mcp.Model;

/// <summary>
/// Everything the AI tooling knows about Blux UI: the docs catalog, the API of every component read from the library,
/// the source of every example and the text of the guides. Generated at build time by <c>build/Blux.UI.DocsBundle</c>
/// and embedded in this assembly, so the MCP server, <c>llms.txt</c> and the skill reference all read one source.
/// </summary>
public sealed record DocsBundle(
    string Name,
    string Version,
    IReadOnlyList<string> Groups,
    IReadOnlyList<BundleComponent> Components,
    IReadOnlyList<BundleGuide> Guides,
    IReadOnlyList<string> Icons)
{
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };
}

/// <summary>A documented component: one page of the docs site, at <c>/components/{Slug}</c>.</summary>
public sealed record BundleComponent(
    string Slug,
    string Name,
    string Group,
    string Description,
    IReadOnlyList<BundleExample> Examples,
    IReadOnlyList<BundleApiType> Api);

/// <summary>A live example of the docs site. <see cref="Id"/> is the name of its Razor component.</summary>
public sealed record BundleExample(string Id, string Title, string? Description, string Source);

/// <summary>One component class of a page and everything it accepts.</summary>
public sealed record BundleApiType(
    string Name,
    string? Summary,
    IReadOnlyList<BundleParameter> Parameters,
    IReadOnlyList<BundleParameter> Events,
    IReadOnlyList<BundleParameter> Slots,
    BundleUi? Ui,
    BundleTheme? Theme);

/// <summary>A <c>[Parameter]</c>. Events are <c>EventCallback</c> parameters and slots are <c>RenderFragment</c> ones.</summary>
public sealed record BundleParameter(
    string Name,
    string Type,
    string? Default,
    string? Description,
    bool Required,
    bool Bindable,
    IReadOnlyList<string>? Values);

/// <summary>The per-instance <c>Ui</c> record of a component: one property per slot.</summary>
public sealed record BundleUi(string Type, IReadOnlyList<BundleUiSlot> Slots);

public sealed record BundleUiSlot(string Name, string? Description);

/// <summary>
/// The global theme of a component: classes per slot, per variant value, and the defaults. <see cref="Property"/> is the
/// property of <c>BluxOptions</c> that configures it (<c>AddBluxUi(o => o.Button.Slot(...))</c>).
/// </summary>
public sealed record BundleTheme(
    string Name,
    string Property,
    IReadOnlyDictionary<string, string> Slots,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> Variants,
    IReadOnlyDictionary<string, string> Defaults,
    IReadOnlyList<BundleCompound> Compounds);

public sealed record BundleCompound(IReadOnlyDictionary<string, string> When, IReadOnlyDictionary<string, string> Classes);

/// <summary>A guide page of the docs site, converted to Markdown.</summary>
public sealed record BundleGuide(string Path, string Title, string Section, string Markdown);
