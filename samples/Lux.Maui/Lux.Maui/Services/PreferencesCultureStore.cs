using Lux.UI;

namespace Lux.Maui.Services;

/// <summary>An <see cref="ILxCultureStore"/> on MAUI Preferences: native storage that MauiProgram can read before the web view exists.</summary>
public sealed class PreferencesCultureStore : ILxCultureStore
{
    public const string Key = "lux-culture";

    public ValueTask<string?> GetAsync() => ValueTask.FromResult(Preferences.Default.Get<string?>(Key, null));

    public ValueTask SetAsync(string culture)
    {
        Preferences.Default.Set(Key, culture);
        return ValueTask.CompletedTask;
    }
}
