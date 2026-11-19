namespace Blux.UI.Theming;

/// <summary>
/// Cascaded by <see cref="BxFormField"/> to the control inside it, which takes the field's input id and adds the
/// field's description, hint and error to its <c>aria-describedby</c>.
/// </summary>
public sealed class FormFieldContext
{
    public required string InputId { get; init; }
    public string? LabelId { get; init; }
    public string? DescribedBy { get; init; }
    public bool Invalid { get; init; }
    public bool Required { get; init; }
    public BxSize Size { get; init; }

    internal static string? Join(params string?[] ids)
    {
        var joined = string.Join(' ', ids.Where(id => !string.IsNullOrEmpty(id)));
        return joined.Length == 0 ? null : joined;
    }
}
