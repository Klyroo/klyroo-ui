# Recipes

Short, complete patterns for the jobs that come up most. Check parameters with `get-component-metadata` before adapting them.

## Data table

`KlyTable` takes `Items`; Blazor infers `TItem` for the columns from it. A `KlyColumn` has a `Title` and a typed `Field` (`Func<TItem, TValue>`, shown and used for sorting, with `TValue` inferred from the lambda); give it a `Template` too to show markup while the `Field` still sorts. A `KlyTemplateColumn` has only a `Template` and cannot be sorted. A `Sortable` column gets a sort button; sorting runs in .NET, compared by the type of the value (numbers by number, dates by date), so the buttons stay disabled until the page is interactive. A `Caption` names the table for assistive technology.

```razor
<KlyTable Items="_people" Caption="Team members" Class="rounded-xl border border-(--klyroo-border)">
    <KlyColumn Title="Name" Field="p => p.Name" Sortable Class="font-medium text-(--klyroo-text)" />
    <KlyColumn Title="Role" Field="p => p.Role" Sortable />
    <KlyColumn Title="Joined" Field="p => p.Joined" Sortable>
        <Template>@context.Joined.ToString("MMM yyyy")</Template>
    </KlyColumn>
    <KlyTemplateColumn Title="Status">
        <Template>
            <KlyBadge Label="@context.Status" Variant="KlyVariant.Subtle" Size="KlySize.Sm"
                     Color="@(context.Status == "Active" ? KlyColor.Success : KlyColor.Neutral)" />
        </Template>
    </KlyTemplateColumn>
</KlyTable>

@code {
    private sealed record Person(string Name, string Role, DateOnly Joined, string Status);

    private static readonly Person[] _people = [new("Lindsay Walton", "Front-end developer", new(2024, 3, 1), "Active")];
}
```

- `Template` receives the row as `@context`. Use it for badges, buttons and formatting.
- `Sticky` keeps the header visible while the table scrolls; give the table a maximum height through `Class`.
- `Empty` is the slot for the no-data state (use `KlyEmpty` inside it).
- Sorting, filtering and paging of large data sets belong in your query: feed `Items` the current page and drive `KlyPagination` with an `HrefTemplate` (links, so it works in static pages) from the query string.

## Sign-in

`KlyAuthForm` builds the form from `KlyAuthField` records (name, a `KlyAuthFieldType`, then `Label`, `Placeholder`, `Required`, `Autocomplete`, `Help`, `Items` for a select, `Length` for a code). It validates with a `Validate` function, shows a busy state while `OnValidSubmit` runs, and posts under its `FormName` in static rendering.

```razor
<KlyAuthForm FormName="auth-sign-in" Title="Welcome back" Description="Sign in to your account to continue."
            Icon="@KlyIcons.Named("lock")" Fields="Fields" Providers="Providers" SubmitLabel="Sign in"
            Validate="CheckPassword" Error="@_error" OnValidSubmit="SignInAsync">
    <Validation>
        <a href="/forgot" class="text-sm text-(--klyroo-primary-text) underline underline-offset-4">Forgot your password?</a>
    </Validation>
    <Footer>New here? <a href="/sign-up" class="font-medium text-(--klyroo-primary-text) underline underline-offset-4">Create an account</a></Footer>
</KlyAuthForm>

@code {
    private string? _error;

    private static readonly KlyAuthField[] Fields =
    [
        new("email", KlyAuthFieldType.Email) { Label = "Email", Placeholder = "you@example.com", Required = true, Autocomplete = "email" },
        new("password", KlyAuthFieldType.Password) { Label = "Password", Required = true, Autocomplete = "current-password" },
        new("remember", KlyAuthFieldType.Checkbox) { Label = "Remember me" },
    ];

    private static readonly KlyAuthProvider[] Providers =
    [
        new("Continue with GitHub", "/auth/github") { Icon = "code" },
    ];

    private static IReadOnlyDictionary<string, string>? CheckPassword(IReadOnlyDictionary<string, string?> values) =>
        values.GetValueOrDefault("password") is { Length: < 8 }
            ? new Dictionary<string, string> { ["password"] = "Use at least 8 characters." }
            : null;

    private async Task SignInAsync(IReadOnlyDictionary<string, string?> values)
    {
        _error = null;
        // Call your identity provider here; set _error to show a form-level message.
    }
}
```

- `Error` is the form-level message; `FieldErrors` (a dictionary by field name) shows per-field server errors, which is how a verification code is rejected.
- Field types seen in the library: `text`, `email`, `password`, `checkbox`, `select`, `otp`. A sign-up form is the same component with more fields; a verification form is one `otp` field with `Length`.
- The form only collects and validates. Authentication itself (cookies, identity, password hashing, antiforgery) stays in your ASP.NET Core code; providers are plain links to your own endpoints.

## Overlays

Modal, slideover and drawer share one shape: the default slot is the trigger, `Body` and `Footer` are the content, `Title` and `Description` name it. Anything marked `data-kly-close` closes the overlay, in the browser, with no handler.

```razor
<KlyModal @bind-Open="_open" Title="Edit profile" Description="Changes are saved when you close the dialog.">
    <ChildContent><KlyButton Label="Edit profile" Variant="ButtonVariant.Outline" /></ChildContent>
    <Body>
        <KlyFormField Label="Name"><KlyInput Value="Ada Lovelace" /></KlyFormField>
    </Body>
    <Footer>
        <KlyButton Label="Cancel" Color="KlyColor.Neutral" Variant="ButtonVariant.Ghost" data-kly-close />
        <KlyButton Label="Save" data-kly-close />
    </Footer>
</KlyModal>
```

- **Confirmation:** `Dismissible="false" Close="false" Size="KlySize.Sm"` so Escape and the backdrop do nothing and the user must choose; put `data-kly-close` on both buttons.

```razor
<KlyModal Title="Delete project?" Description="This removes the project and its history. It cannot be undone." Dismissible="false" Close="false" Size="KlySize.Sm">
    <ChildContent><KlyButton Label="Delete project" Color="KlyColor.Error" Variant="ButtonVariant.Soft" /></ChildContent>
    <Footer>
        <KlyButton Label="Cancel" Color="KlyColor.Neutral" Variant="ButtonVariant.Outline" data-kly-close />
        <KlyButton Label="Delete" Color="KlyColor.Error" data-kly-close />
    </Footer>
</KlyModal>
```

- A `data-kly-close` button does not run your code. To act on the choice, bind `@bind-Open` and react to it, or use a form post.
- **Popover:** the default slot is the trigger (it must contain a `<button>`), `Content` is the panel, `Placement` is a `KlyPlacement` such as `KlyPlacement.BottomStart`.

```razor
<KlyPopover Placement="KlyPlacement.BottomStart">
    <ChildContent><KlyButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="w-64 p-4 text-sm">Escape or a click outside closes me.</p></Content>
</KlyPopover>
```

- **Menu:** `KlyDropdownMenu` with `Items` (`KlyMenuItem` with `Label`, `Href`, `Shortcut`, `Color`, `Disabled`; `KlyMenuItem.Heading("...")` and `KlyMenuItem.Separator()`), `OnSelect` for the chosen item, the trigger as the default slot. `KlyContextMenu` takes the same items around the area that opens it.
- **Toast:** inject `IToastService` and add a `KlyToast` (`Title`, `Description`, `Color`, `Duration`). It needs a toaster, which `KlyApp` provides.

```razor
@inject IToastService Toasts

<KlyButton Label="Save" OnClick="SavedAsync" />

@code {
    private async Task SavedAsync() => await Toasts.Add(new KlyToast
    {
        Title = "Changes saved",
        Description = "Your profile is up to date.",
        Color = KlyColor.Success,
    });
}
```

## Navigation

- **Site links:** `KlyNavigationMenu` with `KlyNavigationItem` records. The current page is marked from the address (`Exact = true` for the home link). Items with `Children` open as a panel or, when vertical, a collapsible group. Always give it an `AriaLabel`.

```razor
<KlyNavigationMenu Items="_items" AriaLabel="Primary" />

@code {
    private static readonly KlyNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true },
        new("Components")
        {
            Children = [new("Button") { Href = "/components/button" }, new("Tabs") { Href = "/components/tabs" }],
        },
        new("Docs") { Href = "/docs" },
    ];
}
```

- **Tabs:** `KlyTabs` with `KlyTabItem` records. Each has a `Value` and `Content` (a `RenderFragment`, written `@<p>...</p>` in a field), `Disabled` as needed. Bind `@bind-Value` only if .NET needs the selected tab.

```razor
<KlyTabs Items="_tabs" @bind-Value="_tab" AriaLabel="Account settings" />

@code {
    private string? _tab;

    private static readonly KlyTabItem[] _tabs =
    [
        new("Account") { Value = "account", Content = @<p class="p-4 text-sm">Make changes to your account.</p> },
        new("Password") { Value = "password", Content = @<p class="p-4 text-sm">Change your password.</p> },
    ];
}
```

- **Breadcrumb:** `KlyBreadcrumb` with `KlyBreadcrumbItem` records; the last item has no `Href`.
- **Accordion:** `KlyAccordion` with `KlyAccordionItem` records (`Value`, `Content`); `Value` opens items initially.
- **Pagination:** `KlyPagination` takes `Total`, `PageSize` and `Page`. With `HrefTemplate="/items?page={page}"` the pages are links and your page reads the number from the URL, which works without interactivity; otherwise it renders buttons.
- **Command palette:** `KlyCommandPalette` with `KlyCommandGroup` records for a searchable list of commands; `KlyDashboardSearch` and `KlyContentSearch` wrap the same idea with a Ctrl+K dialog.
