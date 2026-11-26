using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI.Internal;

/// <summary>
/// Lets a <c>Width</c> or <c>Height</c> parameter typed <c>int?</c> also receive a text ("120" or "120px") when the value comes from a
/// dictionary of attributes or a lowercase attribute, where Blazor would otherwise fail with an invalid cast at render.
/// </summary>
internal static class PixelParameters
{
    /// <summary>
    /// The parameters with the text values of <paramref name="names"/> turned into integers. Returns <paramref name="parameters"/> itself when there is nothing to change.
    /// Throws an <see cref="InvalidOperationException"/> naming the parameter for a text that is not an integer, or a value that is neither text nor <see cref="int"/>.
    /// The cascading values do not go through a dictionary, so they are handed to <paramref name="applyCascading"/>.
    /// </summary>
    public static ParameterView Coerce(ParameterView parameters, string component, ReadOnlySpan<string> names, Action<ParameterValue> applyCascading)
    {
        var needed = false;
        foreach (var parameter in parameters)
        {
            if (!parameter.Cascading && IsPixelParameter(parameter.Name, names) && parameter.Value is not (null or int))
            {
                needed = true;
                break;
            }
        }
        if (!needed) return parameters;

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            if (parameter.Cascading) applyCascading(parameter);
            else values[parameter.Name] = IsPixelParameter(parameter.Name, names) ? ToPixels(component, parameter.Name, parameter.Value) : parameter.Value;
        }
        return ParameterView.FromDictionary(values);
    }

    private static bool IsPixelParameter(string name, ReadOnlySpan<string> names)
    {
        foreach (var candidate in names)
        {
            if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static int? ToPixels(string component, string name, object? value)
    {
        if (value is null) return null;
        if (value is int number) return number;
        if (value is string text)
        {
            var digits = text.Trim();
            if (digits.EndsWith("px", StringComparison.OrdinalIgnoreCase)) digits = digits[..^2].TrimEnd();
            if (int.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var parsed)) return parsed;
        }
        var shown = value is string s ? $"\"{s}\"" : $"a {value.GetType().Name}";
        throw new InvalidOperationException(
            $"{component}: {name} must be an integer number of pixels (an int, or a text such as \"120\" or \"120px\"), but it was {shown}.");
    }
}
