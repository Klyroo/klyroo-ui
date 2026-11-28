namespace Klyroo.UI.Maui;

/// <summary>
/// Sends on the Enter key of a hardware keyboard from a multi-line <see cref="Editor"/>: the soft keyboard keeps Enter as a new line, which is what mobile chats do.
/// Only Android intercepts the key; the other platforms keep the new line and rely on the send button.
/// </summary>
internal sealed class ChatDataEnterKey
{
    private readonly Func<bool, bool, bool> _handle;
#if ANDROID
    private Android.Widget.EditText? _hooked;
#endif

    /// <param name="editor">The editor to watch.</param>
    /// <param name="handle">Called with whether Shift and whether Ctrl or Meta are held. Returns true when it used the key.</param>
    public ChatDataEnterKey(Editor editor, Func<bool, bool, bool> handle)
    {
        _handle = handle;
        editor.HandlerChanged += (_, _) => Hook(editor);
    }

    private void Hook(Editor editor)
    {
#if ANDROID
        if (_hooked is not null) _hooked.KeyPress -= OnKeyPress;
        _hooked = editor.Handler?.PlatformView as Android.Widget.EditText;
        if (_hooked is not null) _hooked.KeyPress += OnKeyPress;
#endif
    }

#if ANDROID
    private void OnKeyPress(object? sender, Android.Views.View.KeyEventArgs e)
    {
        e.Handled = false;
        if (e.Event is { Action: Android.Views.KeyEventActions.Down } key && e.KeyCode is Android.Views.Keycode.Enter or Android.Views.Keycode.NumpadEnter)
        {
            e.Handled = _handle(key.IsShiftPressed, key.IsCtrlPressed || key.IsMetaPressed);
        }
    }
#endif
}
