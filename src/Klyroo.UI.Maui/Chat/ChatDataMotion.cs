namespace Klyroo.UI.Maui;

/// <summary>Looping animations of the chat controls (spinner, typing dots, streaming cursor, shimmer), which stop where the system asks for no animation.</summary>
internal static class ChatDataMotion
{
    /// <summary>True when the user turned animations off in the system settings (<c>motion-reduce</c> on the web). Only Android reports it.</summary>
    public static bool Reduced
    {
        get
        {
#if ANDROID
            var resolver = Android.App.Application.Context.ContentResolver;
            return resolver is not null && Android.Provider.Settings.Global.GetFloat(resolver, Android.Provider.Settings.Global.AnimatorDurationScale, 1f) == 0f;
#else
            return false;
#endif
        }
    }

    /// <summary>Calls <paramref name="step"/> with 0 to 1, over and over, until <see cref="Stop"/>. Does nothing while <see cref="Reduced"/>.</summary>
    public static void Loop(VisualElement owner, string name, Action<double> step, uint length, uint rate = 16)
    {
        if (Reduced) return;
        new Animation(step).Commit(owner, name, rate, length, Easing.Linear, repeat: () => true);
    }

    /// <summary>Stops a loop started with <see cref="Loop"/>.</summary>
    public static void Stop(VisualElement owner, string name) => owner.AbortAnimation(name);

    /// <summary>The fade of the web <c>animate-pulse</c>: full, half, full over one cycle.</summary>
    public static double Pulse(double cycle) => 1 - 0.5 * Math.Sin(((cycle % 1 + 1) % 1) * Math.PI);

    /// <summary>The monospace font of the platform.</summary>
    public static string MonospaceFont
    {
        get
        {
#if IOS || MACCATALYST
            return "Menlo";
#elif WINDOWS
            return "Consolas";
#else
            return "monospace";
#endif
        }
    }
}
