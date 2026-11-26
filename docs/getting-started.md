# Getting started

This tutorial adds Klyroo.UI to a Blazor Web App and builds a small newsletter sign-up form with it. The form works in
static server rendering, so it also works when the page has no interactivity at all. It takes about ten minutes.

Check the [prerequisites](../README.md#prerequisites) first: the .NET 10 SDK, and for the components a Blazor app with Klyroo.UI set up.

## 1. Create the app

```bash
dotnet new blazor -o Acme.Web
cd Acme.Web
```

Any Blazor Web App works, whatever its interactivity mode. If you use WebAssembly or Auto, the steps that touch
`Program.cs` apply to the server project and to the `.Client` project, see [Render modes](platforms.md#render-modes).

## 2. Install the package

```bash
dotnet add package Klyroo.UI --prerelease
```

The package carries the components, the themes, the twelve locales, the stylesheets and the MSBuild targets for fonts,
icons and Tailwind. Nothing else is needed to render the components.

## 3. Register the services

`AddKlyrooUi` registers the options, the class merger, the toast service and the culture store.

```csharp
using Klyroo.UI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKlyrooUi();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
```

Call it again with the same configuration in the `Program.cs` of the `.Client` project when the app has one. A
shared extension method in the client project keeps both calls identical, which matters because an Auto app that
renders on the server and then on WebAssembly must not change appearance when the runtime switches.

## 4. Add the imports

In `Components/_Imports.razor`:

```razor
@using Klyroo.UI
```

## 5. Load the styles

The Blazor template links Bootstrap in `Components/App.razor`. Remove that link and the Bootstrap files of
`wwwroot/lib`: CSS outside a cascade layer overrides the layered CSS that Tailwind v4 generates, and the two
stylesheets would fight. The template's own `app.css` and `Acme.Web.styles.css` style its sample layout, which you
replace in the next step, so remove their links too.

Put the Klyroo head components and the two stylesheets in `<head>`, before your own stylesheet:

```razor
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <BasePath />
    <KlyColorModeScript />
    <KlyThemeStyle />
    <ResourcePreloader />
    <link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.preflight.css"]" />
    <link rel="stylesheet" href="@Assets["_content/Klyroo.UI/klyroo.css"]" />
    <ImportMap />
    <HeadOutlet />
</head>
```

`KlyColorModeScript` applies the stored color mode before the first paint, so a dark page never flashes light.
`KlyThemeStyle` renders nothing until you configure palettes, and then renders the variables that deliver them.
`klyroo.preflight.css` is a small reset and `klyroo.css` holds the tokens and every class the components use.

If your app compiles Tailwind CSS v4 itself, load your own stylesheet instead of these two. See
[Compile Tailwind in your app](guides.md#compile-tailwind-in-your-app).

## 6. Wrap the layout in KlyApp

`KlyApp` goes once in the layout, around the body. It renders the toaster, resolves the locale, writes `lang` and
`dir`, and cascades global class overrides.

In `Components/Layout/MainLayout.razor`:

```razor
@inherits LayoutComponentBase

<KlyApp>
    <KlyMain>
        @Body
    </KlyMain>
</KlyApp>
```

## 7. Build the form

Create `Components/Pages/Subscribe.razor`. The model carries data annotations, the page binds it with
`[SupplyParameterFromForm]`, and `KlyForm` renders the form, the validation messages and the busy state.

```razor
@page "/subscribe"
@using System.ComponentModel.DataAnnotations

<PageTitle>Subscribe</PageTitle>

<KlyContainer Class="py-12">
    <KlyForm Model="Model" FormName="subscribe" OnValidSubmit="SubscribeAsync" ShowSummary Class="flex max-w-sm flex-col gap-4">
        <KlyFormField Label="Email" Required For="() => Model!.Email" Description="We send one message a month.">
            <KlyInput Type="email" @bind-Value="Model!.Email" Placeholder="you@example.com" />
        </KlyFormField>
        <KlyFormField Label="Topic" For="() => Model!.Topic">
            <KlySelect @bind-Value="Model!.Topic" Items="Topics" Placeholder="Pick a topic" />
        </KlyFormField>
        <KlyCheckbox @bind-Value="Model!.Terms" Label="I accept the terms" />
        <KlyButton Type="submit" Label="Subscribe" Block />
    </KlyForm>

    @if (_done)
    {
        <KlyAlert Class="mt-6" Color="KlyColor.Success" Variant="KlyVariant.Soft" Title="You are subscribed"
                  Description="Check your inbox to confirm the address." />
    }
</KlyContainer>

@code {
    private bool _done;

    [SupplyParameterFromForm(FormName = "subscribe")]
    private SubscribeModel? Model { get; set; }

    private static readonly KlySelectItem[] Topics =
    [
        new("releases", "Releases"),
        new("tips", "Tips and recipes"),
    ];

    protected override void OnInitialized() => Model ??= new();

    private Task SubscribeAsync()
    {
        _done = true;
        return Task.CompletedTask;
    }

    private sealed class SubscribeModel
    {
        [Required(ErrorMessage = "Enter your email address."), EmailAddress]
        public string? Email { get; set; }

        public string? Topic { get; set; } = "releases";

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms.")]
        public bool Terms { get; set; }
    }
}
```

Run `dotnet run` and open `/subscribe`. Submit the form empty to see the messages.

Three things in that page matter in every render mode:

- The form has a `FormName` that matches `[SupplyParameterFromForm(FormName = "subscribe")]`. In static rendering the
  browser posts the form, the framework fills `Model`, and `OnValidSubmit` runs on the server.
- The submit button has `Type="submit"`. A `KlyButton` with `OnClick` renders disabled until the page is interactive,
  because a click handler cannot run before that, while a submit button works without any interactivity.
- `Model ??= new()` creates the model on the first render. On a post the framework has already filled it.

## 8. Show a toast (interactive pages only)

`IToastService` shows a toast in the `KlyToaster` that `KlyApp` renders. It calls into the browser, so it needs an
interactive render mode.

```razor
@inject IToastService Toasts

<KlyButton Label="Save" OnClick="SaveAsync" />

@code {
    private async Task SaveAsync() =>
        await Toasts.Add(new KlyToast { Title = "Saved", Description = "Your changes are stored.", Color = KlyColor.Success });
}
```

## Next steps

- [Guides](guides.md): theming, Tailwind, fonts, icons, images, localization, forms and testing, one task at a time.
- [Concepts](concepts.md): slots, the `Ui` and `Class` overrides, and how the browser and .NET share the work.
- [Platforms](platforms.md): render modes, .NET MAUI Blazor Hybrid, trimming and NativeAOT.
- [API reference](api-reference.md): every public type and member.
