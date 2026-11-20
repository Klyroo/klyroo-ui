using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;

namespace Blux.Playground.Client.Docs;

public sealed record ApiParameter(string Name, string Type, string? Default, string? Description);

/// <summary>Lists a component's parameters with their types, defaults and XML documentation.</summary>
public static class ApiReader
{
    private static readonly Lazy<Dictionary<string, string>> Docs = new(LoadDocs);

    public static IReadOnlyList<ApiParameter> Parameters(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type component)
    {
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(component);
        }
        catch (MissingMethodException)
        {
            // No defaults to show.
        }

        return component.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetCustomAttribute<ParameterAttribute>() is { CaptureUnmatchedValues: false })
            .OrderBy(p => p.Name == "ChildContent" ? 1 : 0)
            .ThenBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => new ApiParameter(p.Name, TypeName(p.PropertyType), DefaultOf(p, instance), Describe(p)))
            .ToList();
    }

    private static string? DefaultOf(PropertyInfo property, object? instance)
    {
        if (instance is null) return null;
        var value = property.GetValue(instance);
        return value switch
        {
            null => null,
            string s => $"\"{s}\"",
            bool b => b ? "true" : "false",
            Enum e => $"{e.GetType().Name}.{e}",
            TimeSpan t => t.ToString(),
            int or double => value.ToString(),
            Blux.UI.BxColor c => $"\"{c.Value}\"",
            _ => null,
        };
    }

    /// <summary>The XML documentation summary of a property, or null when it has none.</summary>
    public static string? Describe(PropertyInfo property)
    {
        var declaring = property.DeclaringType!;
        var typeName = declaring.IsGenericType ? declaring.GetGenericTypeDefinition().FullName : declaring.FullName;
        return Docs.Value.TryGetValue($"P:{typeName}.{property.Name}", out var text) ? text : null;
    }

    /// <summary>The XML documentation summary of a type, or null when it has none.</summary>
    public static string? Describe(Type type)
    {
        var typeName = type.IsGenericType ? type.GetGenericTypeDefinition().FullName : type.FullName;
        return Docs.Value.TryGetValue($"T:{typeName}", out var text) ? text : null;
    }

    public static string TypeName(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } inner) return TypeName(inner) + "?";
        if (type == typeof(string)) return "string";
        if (type == typeof(bool)) return "bool";
        if (type == typeof(int)) return "int";
        if (type == typeof(object)) return "object";
        if (!type.IsGenericType) return type.Name;
        var name = type.Name[..type.Name.IndexOf('`')];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }

    private static Dictionary<string, string> LoadDocs()
    {
        using var stream = typeof(ApiReader).Assembly.GetManifestResourceStream("Blux.UI.xml");
        if (stream is null) return [];
        return XDocument.Load(stream).Descendants("member")
            .Where(m => m.Element("summary") is not null)
            .ToDictionary(
                m => (string)m.Attribute("name")!,
                m => string.Join(' ', m.Element("summary")!.Nodes().Select(n => n is XElement e
                        ? (string?)e.Attribute("cref")?.Value.Split('.').Last() ?? (string?)e.Attribute("langword") ?? e.Value
                        : n.ToString()))
                    .Replace("\n", " ").Split(' ', StringSplitOptions.RemoveEmptyEntries) is var words ? string.Join(' ', words) : "");
    }
}
