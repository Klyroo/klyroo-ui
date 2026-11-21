using Blux.UI;

namespace Dashboard.Maui.Services;

/// <summary>An <see cref="IBxCultureStore"/> on MAUI Preferences: native storage that MauiProgram can read before the web view exists.</summary>
public sealed class PreferencesCultureStore : IBxCultureStore
{
    public const string Key = "blux-culture";

    public ValueTask<string?> GetAsync() => ValueTask.FromResult(Preferences.Default.Get<string?>(Key, null));

    public ValueTask SetAsync(string culture)
    {
        Preferences.Default.Set(Key, culture);
        return ValueTask.CompletedTask;
    }
}
