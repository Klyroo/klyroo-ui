namespace Klyroo.UI.Maui;

/// <summary>What a <see cref="KlyFormField"/> tells the control inside it: the error state, the accessible name and description, and the size to take when the control has none of its own.</summary>
/// <param name="Invalid">The field shows an error.</param>
/// <param name="Name">The label of the field, for a screen reader.</param>
/// <param name="Hint">The description, hint and error of the field, for a screen reader.</param>
/// <param name="Size">The size of the field, when the form field sets one.</param>
internal readonly record struct InputsFieldState(bool Invalid, string? Name, string? Hint, KlySize? Size);

/// <summary>A control that takes its error state, label and size from the <see cref="KlyFormField"/> around it, and is not a <see cref="KlyFieldBase"/>.</summary>
internal interface IInputsFieldTarget
{
    void ApplyField(InputsFieldState state);
}

/// <summary>Which of its settings a control took from the form field around it: a value the app set itself always wins over the form field.</summary>
internal sealed class InputsFieldTaken
{
    public bool Name { get; set; }

    public bool Size { get; set; }

    public bool Invalid { get; set; }

    /// <summary>Shows the error of the form field, and takes it away when the error is gone. An error the app set itself is not cleared.</summary>
    public void TakeInvalid(BindableObject target, BindableProperty invalidProperty, bool invalid)
    {
        if (invalid)
        {
            // A control the app already marked invalid stays so when the error goes.
            if (!(bool)target.GetValue(invalidProperty)) Invalid = true;
            target.SetValue(invalidProperty, true);
        }
        else if (Invalid)
        {
            Invalid = false;
            target.SetValue(invalidProperty, false);
        }
    }

    /// <summary>Takes the label of the form field, unless the app gave the control a name of its own.</summary>
    public void TakeName(BindableObject target, BindableProperty nameProperty, string? name)
    {
        var current = (string?)target.GetValue(nameProperty);
        if (!string.IsNullOrEmpty(current) && !Name) return;
        Name = name is not null;
        target.SetValue(nameProperty, name);
    }

    /// <summary>Takes the size of the form field, unless the app gave the control a size of its own.</summary>
    public void TakeSize(BindableObject target, BindableProperty sizeProperty, KlySize? size)
    {
        if (size is null || (target.IsSet(sizeProperty) && !Size)) return;
        Size = true;
        target.SetValue(sizeProperty, size.Value);
    }
}
