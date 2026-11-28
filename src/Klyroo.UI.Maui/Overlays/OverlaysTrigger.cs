namespace Klyroo.UI.Maui;

/// <summary>Wires the trigger of an overlay control: a click of its first button (a tap when it has none), the hover of a pointer, its focus and a long press.</summary>
internal static class OverlaysTrigger
{
    /// <summary>Runs <paramref name="activate"/> when the first button of <paramref name="trigger"/> is clicked, or when <paramref name="trigger"/> is tapped if it holds no button. Returns the way to undo it.</summary>
    public static Action Activate(View trigger, Action activate)
    {
        switch (FindButton(trigger))
        {
            case Button button:
                EventHandler onClick = (_, _) => activate();
                button.Clicked += onClick;
                return () => button.Clicked -= onClick;
            case ImageButton imageButton:
                EventHandler onImageClick = (_, _) => activate();
                imageButton.Clicked += onImageClick;
                return () => imageButton.Clicked -= onImageClick;
            default:
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => activate();
                trigger.GestureRecognizers.Add(tap);
                return () => trigger.GestureRecognizers.Remove(tap);
        }
    }

    /// <summary>Runs the callbacks as a pointer enters and leaves the view. Only platforms with a pointer (desktop, iPad) raise them.</summary>
    public static Action Hover(View view, Action entered, Action exited)
    {
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => entered();
        pointer.PointerExited += (_, _) => exited();
        view.GestureRecognizers.Add(pointer);
        return () => view.GestureRecognizers.Remove(pointer);
    }

    /// <summary>Runs the callbacks as the view gets and loses the focus (a keyboard or a switch device).</summary>
    public static Action Focus(View view, Action focused, Action unfocused)
    {
        EventHandler<FocusEventArgs> onFocus = (_, _) => focused();
        EventHandler<FocusEventArgs> onUnfocus = (_, _) => unfocused();
        view.Focused += onFocus;
        view.Unfocused += onUnfocus;
        return () =>
        {
            view.Focused -= onFocus;
            view.Unfocused -= onUnfocus;
        };
    }

    /// <summary>Runs <paramref name="onLongPress"/> when the user presses the view for a while with a finger: the place of the touch in the view when the platform says it, else null. Android, iOS and Mac Catalyst; other platforms never raise it.</summary>
    public static Action LongPress(View view, Action<Point?> onLongPress)
    {
        var detach = () => { };
        void Attach()
        {
            detach();
            detach = AttachPlatform(view, onLongPress);
        }

        void OnHandlerChanged(object? sender, EventArgs e) => Attach();
        view.HandlerChanged += OnHandlerChanged;
        Attach();
        return () =>
        {
            view.HandlerChanged -= OnHandlerChanged;
            detach();
            detach = () => { };
        };
    }

    private static Action AttachPlatform(View view, Action<Point?> onLongPress)
    {
#if ANDROID
        if (view.Handler?.PlatformView is Android.Views.View native)
        {
            native.LongClickable = true;
            EventHandler<Android.Views.View.LongClickEventArgs> onLongClick = (_, e) =>
            {
                e.Handled = true;
                onLongPress(null);
            };
            native.LongClick += onLongClick;
            return () => native.LongClick -= onLongClick;
        }
#elif IOS || MACCATALYST
        if (view.Handler?.PlatformView is UIKit.UIView native)
        {
            var recognizer = new UIKit.UILongPressGestureRecognizer(gesture =>
            {
                if (gesture.State != UIKit.UIGestureRecognizerState.Began) return;
                var point = gesture.LocationInView(native);
                onLongPress(new Point(point.X, point.Y));
            });
            native.AddGestureRecognizer(recognizer);
            return () => native.RemoveGestureRecognizer(recognizer);
        }
#endif
        return () => { };
    }

    private static View? FindButton(View root)
    {
        if (root is Button or ImageButton) return root;
        if (root is not IVisualTreeElement parent) return null;
        foreach (var child in parent.GetVisualChildren())
        {
            if (child is View view && FindButton(view) is { } found) return found;
        }
        return null;
    }
}
