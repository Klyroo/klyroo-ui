using Microsoft.Extensions.Options;
using TailwindMerge;
using TailwindMerge.Models;

namespace Blux.UI.Theming;

/// <summary>
/// Merges Tailwind classes so that a later class overrides a conflicting earlier one.
/// Kept behind an interface so the third-party implementation can be swapped.
/// </summary>
internal interface IClassMerger
{
    string Merge(params string?[] classes);
}

/// <summary>tailwind-merge port. Thread-safe with its own bounded LRU cache, registered as a singleton.</summary>
internal sealed class TailwindClassMerger : IClassMerger
{
    private readonly TwMerge _merge;

    public TailwindClassMerger()
    {
        var config = new TwMergeConfig { CacheSize = 2000 };
        _merge = new TwMerge(Options.Create(config));
    }

    public string Merge(params string?[] classes) => _merge.Merge(classes) ?? "";
}
