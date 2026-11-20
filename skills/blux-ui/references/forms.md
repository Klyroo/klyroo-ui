# Forms

Blux form controls plug into Blazor's `EditContext`: they validate with data annotations, show messages through `BxFormField`, and post in static server rendering. Two ways to build a form:

- `BxForm` for the full treatment: busy state that disables the controls while a handler runs, an optional error summary, and `Enhance` for enhanced form handling.
- A plain `EditForm` with `DataAnnotationsValidator` when you want Blazor's own pieces and only Blux controls and fields.

## The model

Data annotations on a class, bound through `[SupplyParameterFromForm]` so the same page works on a static post and in an interactive session:

```razor
@using System.ComponentModel.DataAnnotations

<BxForm Model="Model" FormName="bx-form" OnValidSubmit="SaveAsync" ShowSummary class="flex w-full max-w-sm flex-col">
    <BxFormField Label="Name" Required For="() => Model!.Name">
        <BxInput @bind-Value="Model!.Name" />
    </BxFormField>
    <BxFormField Label="Seats" For="() => Model!.Seats">
        <BxInputNumber @bind-Value="Model!.Seats" Min="1" Max="50" />
    </BxFormField>
    <BxButton Type="submit" Label="Create workspace" />
</BxForm>

@code {
    [SupplyParameterFromForm(FormName = "bx-form")]
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

## BxFormField

`BxFormField` renders the label, description, hint, help and error message and wires them to the control inside it, with `For` pointing at the model property (it takes the message from the `EditContext`). Slots and parameters it takes: `Label`, `Description`, `Hint`, `Help`, `Required`, `Error`, `Size`, `For`, `Id`.

```razor
<EditForm Model="Model" FormName="signup" OnValidSubmit="Submit" class="flex w-full max-w-sm flex-col gap-4" novalidate>
    <DataAnnotationsValidator />
    <BxFormField Label="Email" Required For="() => Model!.Email" Description="We never share it.">
        <BxInput Type="email" @bind-Value="Model!.Email" Placeholder="you@example.com" />
    </BxFormField>
    <BxFormField Label="Role" Hint="Optional">
        <BxSelect @bind-Value="Model!.Role" Items="Roles" Placeholder="Pick one" />
    </BxFormField>
    <BxFormField Label="About you" Help="Markdown is supported." For="() => Model!.Bio">
        <BxTextarea @bind-Value="Model!.Bio" Rows="3" />
    </BxFormField>
    <BxCheckbox @bind-Value="Model!.Newsletter" Label="Send me the newsletter" />
    <BxButton Type="submit" Label="Create account" Block />
</EditForm>
```

- A control inside a `BxFormField` takes its id and adds the field's description, hint and error to its `aria-describedby`; do not set them by hand.
- Inside an `EditForm` a control posts under the name of the bound property. Outside one, give it `Name`.
- A control that stands alone (`BxCheckbox`, `BxSwitch` with their own `Label`) needs no field. Show its error with Blazor's `ValidationMessage For="..."`.
- `Hint` is the quiet text next to the label ("Optional"); `Help` and `Description` sit below. Use `Error` to show a message that does not come from validation, such as a server error.

## Choosing the control

| Data | Control | Value type |
|---|---|---|
| Text, email, password | `BxInput` | `string?` |
| Long text | `BxTextarea` | `string?` |
| Number | `BxInputNumber` | `decimal?` |
| Yes/no | `BxSwitch`, `BxCheckbox` | `bool` |
| One of a few | `BxRadioGroup`, `BxSelect` | `string?` |
| Several of a few | `BxCheckboxGroup`, `BxSelectMenu` with `Multiple`, `BxToggleGroup` with `Multiple` | `IReadOnlyList<string>` |
| Date | `BxInputDate`, `BxCalendar` | `DateOnly?` |
| Time | `BxInputTime` | `TimeOnly?` |
| Code, tags, rating | `BxPinInput`, `BxInputTags`, `BxInputRating` | `string`, `IReadOnlyList<string>`, `double` |
| Rich text | `BxEditor` | HTML `string?` |

Check the type of `Value` with `get-component-metadata` before binding: binding a `string` to a `decimal?` control fails at compile time.

## Validation messages

- Put the message in the annotation (`[Required(ErrorMessage = "The name is required.")]`); `BxFormField` and `ShowSummary` display it.
- A boolean that must be true: `[Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms.")]`.
- Cross-field or server-side rules: add the error to the `EditContext` (`ValidationMessageStore`), or pass `Error` to the field or the form.

## Enhanced forms and server resets

`BxForm Enhance` turns on Blazor enhanced form handling: the post is a fetch and the page is patched. If your handler replaces the model after saving (`Model = new();`), the server's page wins over what the browser showed, which is what you want after a successful save.

## Authentication forms

`BxAuthForm` builds a sign-in, sign-up or verification form from a description of its fields, with provider buttons, validation and a busy state. See `recipes.md`.
