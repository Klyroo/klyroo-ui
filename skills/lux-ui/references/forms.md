# Forms

Lux form controls plug into Blazor's `EditContext`: they validate with data annotations, show messages through `LxFormField`, and post in static server rendering. Two ways to build a form:

- `LxForm` for the full treatment: busy state that disables the controls while a handler runs, an optional error summary, and `Enhance` for enhanced form handling.
- A plain `EditForm` with `DataAnnotationsValidator` when you want Blazor's own pieces and only Lux controls and fields.

## The model

Data annotations on a class, bound through `[SupplyParameterFromForm]` so the same page works on a static post and in an interactive session:

```razor
@using System.ComponentModel.DataAnnotations

<LxForm Model="Model" FormName="lx-form" OnValidSubmit="SaveAsync" ShowSummary class="flex w-full max-w-sm flex-col">
    <LxFormField Label="Name" Required For="() => Model!.Name">
        <LxInput @bind-Value="Model!.Name" />
    </LxFormField>
    <LxFormField Label="Seats" For="() => Model!.Seats">
        <LxInputNumber @bind-Value="Model!.Seats" Min="1" Max="50" />
    </LxFormField>
    <LxButton Type="submit" Label="Create workspace" />
</LxForm>

@code {
    [SupplyParameterFromForm(FormName = "lx-form")]
    private Workspace? Model { get; set; }

    protected override void OnInitialized() => Model ??= new();

    private async Task SaveAsync()
    {
        // Persist. While this runs the form is busy and its controls are disabled.
        await Task.Delay(300);
    }

    private sealed class Workspace
    {
        [Required(ErrorMessage = "The name is required.")]
        public string? Name { get; set; }

        [Range(1, 50)]
        public decimal? Seats { get; set; } = 5;
    }
}
```

Rules that make it work in every render mode:

- Give the form a `FormName` and match it in `[SupplyParameterFromForm(FormName = "...")]`.
- Initialize the model with `Model ??= new();` in `OnInitialized`: on a post the framework fills it, on first render you create it.
- Use `Type="submit"` on the button; a submit button works without interactivity.
- Server-side validation runs the same annotations on the post. Never rely on the browser alone.

## LxFormField

`LxFormField` renders the label, description, hint, help and error message and wires them to the control inside it, with `For` pointing at the model property (it takes the message from the `EditContext`). Slots and parameters it takes: `Label`, `Description`, `Hint`, `Help`, `Required`, `Error`, `Size`, `For`, `Id`.

```razor
<EditForm Model="Model" FormName="signup" OnValidSubmit="Submit" class="flex w-full max-w-sm flex-col gap-4" novalidate>
    <DataAnnotationsValidator />
    <LxFormField Label="Email" Required For="() => Model!.Email" Description="We never share it.">
        <LxInput Type="email" @bind-Value="Model!.Email" Placeholder="you@example.com" />
    </LxFormField>
    <LxFormField Label="Role" Hint="Optional">
        <LxSelect @bind-Value="Model!.Role" Items="Roles" Placeholder="Pick one" />
    </LxFormField>
    <LxFormField Label="About you" Help="Markdown is supported." For="() => Model!.Bio">
        <LxTextarea @bind-Value="Model!.Bio" Rows="3" />
    </LxFormField>
    <LxCheckbox @bind-Value="Model!.Newsletter" Label="Send me the newsletter" />
    <LxButton Type="submit" Label="Create account" Block />
</EditForm>
```

- A control inside a `LxFormField` takes its id and adds the field's description, hint and error to its `aria-describedby`; do not set them by hand.
- Inside an `EditForm` a control posts under the name of the bound property. Outside one, give it `Name`.
- A control that stands alone (`LxCheckbox`, `LxSwitch` with their own `Label`) needs no field. Show its error with Blazor's `ValidationMessage For="..."`.
- `Hint` is the quiet text next to the label ("Optional"); `Help` and `Description` sit below. Use `Error` to show a message that does not come from validation, such as a server error.

## Choosing the control

| Data | Control | Value type |
|---|---|---|
| Text, email, password | `LxInput` | `string?` |
| Long text | `LxTextarea` | `string?` |
| Number | `LxInputNumber` | `decimal?` |
| Yes/no | `LxSwitch`, `LxCheckbox` | `bool` |
| One of a few | `LxRadioGroup`, `LxSelect` | `string?` |
| Several of a few | `LxCheckboxGroup`, `LxSelectMenu` with `Multiple`, `LxToggleGroup` with `Multiple` | `IReadOnlyList<string>` |
| Date | `LxInputDate`, `LxCalendar` | `DateOnly?` |
| Time | `LxInputTime` | `TimeOnly?` |
| Code, tags, rating | `LxPinInput`, `LxInputTags`, `LxInputRating` | `string`, `IReadOnlyList<string>`, `double` |
| Rich text | `LxEditor` | HTML `string?` |

Check the type of `Value` with `get-component-metadata` before binding: binding a `string` to a `decimal?` control fails at compile time.

## Validation messages

- Put the message in the annotation (`[Required(ErrorMessage = "The name is required.")]`); `LxFormField` and `ShowSummary` display it.
- A boolean that must be true: `[Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms.")]`.
- Cross-field or server-side rules: add the error to the `EditContext` (`ValidationMessageStore`), or pass `Error` to the field or the form.

## Enhanced forms and server resets

`LxForm Enhance` turns on Blazor enhanced form handling: the post is a fetch and the page is patched. If your handler replaces the model after saving (`Model = new();`), the server's page wins over what the browser showed, which is what you want after a successful save.

## Authentication forms

`LxAuthForm` builds a sign-in, sign-up or verification form from a description of its fields, with provider buttons, validation and a busy state. See `recipes.md`.
