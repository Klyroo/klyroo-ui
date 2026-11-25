using Klyroo.UI;

namespace Klyroo.Maui.Services;

/// <summary>An <see cref="IKlyCultureStore"/> on MAUI Preferences: native storage that MauiProgram can read before the web view exists.</summary>
public sealed class PreferencesCultureStore : IKlyCultureStore
{
    public const string Key = "klyroo-culture";

    public ValueTask<string?> GetAsync() => ValueTask.FromResult(Preferences.Default.Get<string?>(Key, null));

    public ValueTask SetAsync(string culture)
    {
        Preferences.Default.Set(Key, culture);
        return ValueTask.CompletedTask;
    }
}
