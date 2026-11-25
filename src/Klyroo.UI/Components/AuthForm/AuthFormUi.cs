using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>
/// A field of a <see cref="KlyAuthForm"/>. <see cref="Type"/> is <c>checkbox</c>, <c>select</c> (give <see cref="Items"/>),
/// <c>otp</c> (a one-time code, see <see cref="Length"/>), <c>password</c> (with a button that shows it) or any
/// <c>type</c> of an <c>input</c>: <c>text</c>, <c>email</c>, <c>tel</c>...
/// </summary>
/// <param name="Name">Name of the field: the <c>name</c> of the control, and the key of its value in the submitted values.</param>
/// <param name="Type">Kind of the field: <c>text</c> by default, or <c>checkbox</c>, <c>select</c>, <c>otp</c>, <c>password</c>, or any <c>type</c> of an <c>input</c>.</param>
public sealed record KlyAuthField(string Name, string Type = "text")
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

    /// <summary>The options of a <c>select</c>.</summary>
    public IReadOnlyList<KlySelectItem> Items { get; init; } = [];

    /// <summary>Number of characters of an <c>otp</c>.</summary>
    public int Length { get; init; } = 6;

    /// <summary>The <c>autocomplete</c> token of the control: <c>email</c>, <c>current-password</c>, <c>new-password</c>...</summary>
    public string? Autocomplete { get; init; }
}

/// <summary>A button of a <see cref="KlyAuthForm"/> that leaves for another address, such as an OAuth provider.</summary>
/// <param name="Label">Text of the button, such as "Continue with GitHub".</param>
/// <param name="Href">Address the button leaves for.</param>
public sealed record KlyAuthProvider(string Label, string Href)
{
    /// <summary>Name of a built-in icon (see <see cref="KlyIcon"/>).</summary>
    public string? Icon { get; init; }

    /// <summary>A custom icon, instead of <see cref="Icon"/>.</summary>
    public RenderFragment? IconContent { get; init; }
}

/// <summary>Per-instance class overrides of the slots of <see cref="KlyAuthForm"/>, passed to its <c>Ui</c> parameter. Each property holds the classes merged into one slot.</summary>
public sealed record AuthFormUi : ISlotOverrides
{
    /// <summary>Classes merged into the <c>root</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Root { get; init; }
    /// <summary>Classes merged into the <c>header</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Header { get; init; }
    /// <summary>Classes merged into the <c>leading</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Leading { get; init; }
    /// <summary>Classes merged into the <c>title</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Title { get; init; }
    /// <summary>Classes merged into the <c>description</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Description { get; init; }
    /// <summary>Classes merged into the <c>providers</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Providers { get; init; }
    /// <summary>Classes merged into the <c>form</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Form { get; init; }
    /// <summary>Classes merged into the <c>fieldset</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Fieldset { get; init; }
    /// <summary>Classes merged into the <c>error</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Error { get; init; }
    /// <summary>Classes merged into the <c>footer</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? Footer { get; init; }
    /// <summary>Classes merged into the <c>passwordRoot</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
    public string? PasswordRoot { get; init; }
    /// <summary>Classes merged into the <c>passwordButton</c> slot of <see cref="KlyAuthForm"/>, after the theme and the app configuration.</summary>
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
