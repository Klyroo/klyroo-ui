namespace Klyroo.UI.Theming;

/// <summary>
/// Cascaded by <see cref="KlyFormField"/> to the control inside it, which takes the field's input id and adds the
/// field's description, hint and error to its <c>aria-describedby</c>.
/// </summary>
public sealed class FormFieldContext
{
    /// <summary>The <c>id</c> the control inside the field must use.</summary>
    public required string InputId { get; init; }
    /// <summary>The <c>id</c> of the label of the field, if it has one.</summary>
    public string? LabelId { get; init; }
    /// <summary>The ids of the description, hint and error elements, to add to <c>aria-describedby</c>.</summary>
    public string? DescribedBy { get; init; }
    /// <summary>Whether the field shows an error.</summary>
    public bool Invalid { get; init; }
    /// <summary>Whether the field is required.</summary>
    public bool Required { get; init; }
    /// <summary>Size the control inside the field takes when it has none of its own.</summary>
    public KlySize Size { get; init; }

    internal static string? Join(params string?[] ids)
    {
        var joined = string.Join(' ', ids.Where(id => !string.IsNullOrEmpty(id)));
        return joined.Length == 0 ? null : joined;
    }
}
