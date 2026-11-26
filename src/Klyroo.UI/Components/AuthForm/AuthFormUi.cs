using Klyroo.UI.Theming;
using Microsoft.AspNetCore.Components;

namespace Klyroo.UI;

/// <summary>The kind of control of a <see cref="KlyAuthField"/>.</summary>
public enum KlyAuthFieldType
{
    /// <summary>A text input (the default).</summary>
    Text,
    /// <summary>An email address; the form checks its shape.</summary>
    Email,
    /// <summary>A secret with a button that shows it.</summary>
    Password,
    /// <summary>A search term.</summary>
    Search,
    /// <summary>A telephone number.</summary>
    Tel,
    /// <summary>A web address.</summary>
    Url,
    /// <summary>A number.</summary>
    Number,
    /// <summary>A date.</summary>
    Date,
    /// <summary>A time of day.</summary>
    Time,
    /// <summary>A date and a time without a time zone.</summary>
    DateTimeLocal,
    /// <summary>A month of a year.</summary>
    Month,
    /// <summary>A week of a year.</summary>
    Week,
    /// <summary>A checkbox; its value is <c>"true"</c> or <c>"false"</c>.</summary>
    Checkbox,
    /// <summary>A select over <see cref="KlyAuthField.Items"/>.</summary>
    Select,
    /// <summary>A one-time code of <see cref="KlyAuthField.Length"/> characters.</summary>
    Otp,
}

/// <summary>
/// A field of a <see cref="KlyAuthForm"/>. <see cref="Type"/> is <see cref="KlyAuthFieldType.Checkbox"/>, <see cref="KlyAuthFieldType.Select"/> (give <see cref="Items"/>),
/// <see cref="KlyAuthFieldType.Otp"/> (a one-time code, see <see cref="Length"/>), <see cref="KlyAuthFieldType.Password"/> (with a button that shows it) or one of the
/// kinds of text input: <see cref="KlyAuthFieldType.Text"/>, <see cref="KlyAuthFieldType.Email"/>, <see cref="KlyAuthFieldType.Tel"/>...
/// </summary>
/// <param name="Name">Name of the field: the <c>name</c> of the control, and the key of its value in the submitted values.</param>
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
