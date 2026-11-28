namespace Klyroo.UI.Maui;

/// <summary>
/// A field of a <see cref="KlyAuthForm"/>. <see cref="Type"/> is <see cref="KlyAuthFieldType.Checkbox"/>, <see cref="KlyAuthFieldType.Select"/> (give <see cref="Items"/>),
/// <see cref="KlyAuthFieldType.Otp"/> (a one-time code, see <see cref="Length"/>), <see cref="KlyAuthFieldType.Password"/> (with a button that shows it) or one of the
/// kinds of text input: <see cref="KlyAuthFieldType.Text"/>, <see cref="KlyAuthFieldType.Email"/>, <see cref="KlyAuthFieldType.Tel"/>...
/// </summary>
/// <param name="Name">Name of the field: the key of its value in the submitted values.</param>
/// <param name="Type">Kind of the field: <see cref="KlyAuthFieldType.Text"/> by default.</param>
public sealed record KlyAuthField(string Name, KlyAuthFieldType Type = KlyAuthFieldType.Text)
{
    /// <summary>Label of the field. Defaults to the name of the field.</summary>
    public string? Label { get; init; }

    /// <summary>Hint shown while the field is empty.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Text under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Text under the control while there is no error.</summary>
    public string? Help { get; init; }

    /// <summary>Marks the field as required.</summary>
    public bool Required { get; init; }

    /// <summary>The initial value. A checkbox is checked when it is <c>"true"</c>.</summary>
    public string? Value { get; init; }

    /// <summary>The options of a <see cref="KlyAuthFieldType.Select"/>.</summary>
    public IReadOnlyList<string> Items { get; init; } = [];

    /// <summary>Number of digits of an <see cref="KlyAuthFieldType.Otp"/>.</summary>
    public int Length { get; init; } = 6;
}
