using Lux.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Lux.UI;

/// <summary>
/// A field of a <see cref="LxAuthForm"/>. <see cref="Type"/> is <c>checkbox</c>, <c>select</c> (give <see cref="Items"/>),
/// <c>otp</c> (a one-time code, see <see cref="Length"/>), <c>password</c> (with a button that shows it) or any
/// <c>type</c> of an <c>input</c>: <c>text</c>, <c>email</c>, <c>tel</c>...
/// </summary>
public sealed record LxAuthField(string Name, string Type = "text")
{
    public string? Label { get; init; }
    public string? Placeholder { get; init; }

    /// <summary>Text under the label.</summary>
    public string? Description { get; init; }

    /// <summary>Text under the control while there is no error.</summary>
    public string? Help { get; init; }
    public bool Required { get; init; }

    /// <summary>The initial value. A checkbox is checked when it is <c>"true"</c>.</summary>
    public string? Value { get; init; }

    /// <summary>The options of a <c>select</c>.</summary>
    public IReadOnlyList<LxSelectItem> Items { get; init; } = [];

    /// <summary>Number of characters of an <c>otp</c>.</summary>
    public int Length { get; init; } = 6;

    /// <summary>The <c>autocomplete</c> token of the control: <c>email</c>, <c>current-password</c>, <c>new-password</c>...</summary>
    public string? Autocomplete { get; init; }
}

/// <summary>A button of a <see cref="LxAuthForm"/> that leaves for another address, such as an OAuth provider.</summary>
public sealed record LxAuthProvider(string Label, string Href)
{
    /// <summary>Name of a built-in icon (see <see cref="LxIcon"/>).</summary>
    public string? Icon { get; init; }

    /// <summary>A custom icon, instead of <see cref="Icon"/>.</summary>
    public RenderFragment? IconContent { get; init; }
}

public sealed record AuthFormUi : ISlotOverrides
{
    public string? Root { get; init; }
    public string? Header { get; init; }
    public string? Leading { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Providers { get; init; }
    public string? Form { get; init; }
    public string? Fieldset { get; init; }
    public string? Error { get; init; }
    public string? Footer { get; init; }
    public string? PasswordRoot { get; init; }
    public string? PasswordButton { get; init; }

    string? ISlotOverrides.For(string slot) => slot switch
    {
        "root" => Root,
        "header" => Header,
        "leading" => Leading,
        "title" => Title,
        "description" => Description,
        "providers" => Providers,
        "form" => Form,
        "fieldset" => Fieldset,
        "error" => Error,
        "footer" => Footer,
        "passwordRoot" => PasswordRoot,
        "passwordButton" => PasswordButton,
        _ => null,
    };
}
