# Recipes

Short, complete patterns for the jobs that come up most. Check parameters with `get-component-metadata` before adapting them.

## Data table

`LxTable` takes `Items`; each `LxColumn` names its `TItem`, a `Title`, and either a `Field` (shown and used for sorting) or a `Template` for custom cells. A `Sortable` column with a `Field` gets a sort button; sorting runs in .NET, so the buttons stay disabled until the page is interactive. A `Caption` names the table for assistive technology.

```razor
<LxTable Items="_people" Caption="Team members" Class="rounded-xl border border-(--lux-border)">
    <LxColumn TItem="Person" Title="Name" Field="p => p.Name" Sortable Class="font-medium text-(--lux-text)" />
    <LxColumn TItem="Person" Title="Role" Field="p => p.Role" Sortable />
    <LxColumn TItem="Person" Title="Joined" Field="p => p.Joined" Sortable>
        <Template>@context.Joined.ToString("MMM yyyy")</Template>
    </LxColumn>
    <LxColumn TItem="Person" Title="Status">
        <Template>
            <LxBadge Label="@context.Status" Variant="LxVariant.Subtle" Size="LxSize.Sm"
                     Color="@(context.Status == "Active" ? LxColor.Success : LxColor.Neutral)" />
        </Template>
    </LxColumn>
</LxTable>

@code {
    private sealed record Person(string Name, string Role, DateOnly Joined, string Status);

    private static readonly Person[] _people = [new("Lindsay Walton", "Front-end developer", new(2024, 3, 1), "Active")];
}
```

- `Template` receives the row as `@context`. Use it for badges, buttons and formatting.
- `Sticky` keeps the header visible while the table scrolls; give the table a maximum height through `Class`.
- `Empty` is the slot for the no-data state (use `LxEmpty` inside it).
- Sorting, filtering and paging of large data sets belong in your query: feed `Items` the current page and drive `LxPagination` with an `HrefTemplate` (links, so it works in static pages) from the query string.

## Sign-in

`LxAuthForm` builds the form from `LxAuthField` records (name, type, then `Label`, `Placeholder`, `Required`, `Autocomplete`, `Help`, `Items` for a select, `Length` for a code). It validates with a `Validate` function, shows a busy state while `OnValidSubmit` runs, and posts under its `FormName` in static rendering.

```razor
<LxAuthForm FormName="auth-sign-in" Title="Welcome back" Description="Sign in to your account to continue."
            Icon="@LxIcons.Named("lock")" Fields="Fields" Providers="Providers" SubmitLabel="Sign in"
            Validate="CheckPassword" Error="@_error" OnValidSubmit="SignInAsync">
    <Validation>
        <a href="/forgot" class="text-sm text-(--lux-primary-text) underline underline-offset-4">Forgot your password?</a>
    </Validation>
    <Footer>New here? <a href="/sign-up" class="font-medium text-(--lux-primary-text) underline underline-offset-4">Create an account</a></Footer>
</LxAuthForm>

@code {
    private string? _error;

    private static readonly LxAuthField[] Fields =
    [
        new("email", "email") { Label = "Email", Placeholder = "you@example.com", Required = true, Autocomplete = "email" },
        new("password", "password") { Label = "Password", Required = true, Autocomplete = "current-password" },
        new("remember", "checkbox") { Label = "Remember me" },
    ];

    private static readonly LxAuthProvider[] Providers =
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

Modal, slideover and drawer share one shape: the default slot is the trigger, `Body` and `Footer` are the content, `Title` and `Description` name it. Anything marked `data-lx-close` closes the overlay, in the browser, with no handler.

```razor
<LxModal @bind-Open="_open" Title="Edit profile" Description="Changes are saved when you close the dialog.">
    <ChildContent><LxButton Label="Edit profile" Variant="ButtonVariant.Outline" /></ChildContent>
    <Body>
        <LxFormField Label="Name"><LxInput Value="Ada Lovelace" /></LxFormField>
    </Body>
    <Footer>
        <LxButton Label="Cancel" Color="LxColor.Neutral" Variant="ButtonVariant.Ghost" data-lx-close />
        <LxButton Label="Save" data-lx-close />
    </Footer>
</LxModal>
```

- **Confirmation:** `Dismissible="false" Close="false" Size="LxSize.Sm"` so Escape and the backdrop do nothing and the user must choose; put `data-lx-close` on both buttons.

```razor
<LxModal Title="Delete project?" Description="This removes the project and its history. It cannot be undone." Dismissible="false" Close="false" Size="LxSize.Sm">
    <ChildContent><LxButton Label="Delete project" Color="LxColor.Error" Variant="ButtonVariant.Soft" /></ChildContent>
    <Footer>
        <LxButton Label="Cancel" Color="LxColor.Neutral" Variant="ButtonVariant.Outline" data-lx-close />
        <LxButton Label="Delete" Color="LxColor.Error" data-lx-close />
    </Footer>
</LxModal>
```

- A `data-lx-close` button does not run your code. To act on the choice, bind `@bind-Open` and react to it, or use a form post.
- **Popover:** the default slot is the trigger (it must contain a `<button>`), `Content` is the panel, `Placement` is `"bottom-start"` and the like.

```razor
<LxPopover Placement="bottom-start">
    <ChildContent><LxButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="w-64 p-4 text-sm">Escape or a click outside closes me.</p></Content>
</LxPopover>
```

- **Menu:** `LxDropdownMenu` with `Items` (`LxMenuItem` with `Label`, `Href`, `Shortcut`, `Color`, `Disabled`; `LxMenuItem.Heading("...")` and `LxMenuItem.Separator()`), `OnSelect` for the chosen item, the trigger as the default slot. `LxContextMenu` takes the same items around the area that opens it.
- **Toast:** inject `IToastService` and add a `LxToast` (`Title`, `Description`, `Color`, `Duration`). It needs a toaster, which `LxApp` provides.

```razor
@inject IToastService Toasts

<LxButton Label="Save" OnClick="SavedAsync" />

@code {
    private async Task SavedAsync() => await Toasts.Add(new LxToast
    {
        Title = "Changes saved",
        Description = "Your profile is up to date.",
        Color = LxColor.Success,
    });
}
```

## Navigation

- **Site links:** `LxNavigationMenu` with `LxNavigationItem` records. The current page is marked from the address (`Exact = true` for the home link). Items with `Children` open as a panel or, when vertical, a collapsible group. Always give it an `AriaLabel`.

```razor
<LxNavigationMenu Items="_items" AriaLabel="Primary" />

@code {
    private static readonly LxNavigationItem[] _items =
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

- **Tabs:** `LxTabs` with `LxTabItem` records. Each has a `Value` and `Content` (a `RenderFragment`, written `@<p>...</p>` in a field), `Disabled` as needed. Bind `@bind-Value` only if .NET needs the selected tab.

```razor
<LxTabs Items="_tabs" @bind-Value="_tab" AriaLabel="Account settings" />

@code {
    private string? _tab;

    private static readonly LxTabItem[] _tabs =
    [
        new("Account") { Value = "account", Content = @<p class="p-4 text-sm">Make changes to your account.</p> },
        new("Password") { Value = "password", Content = @<p class="p-4 text-sm">Change your password.</p> },
    ];
}
```

- **Breadcrumb:** `LxBreadcrumb` with `LxBreadcrumbItem` records; the last item has no `Href`.
- **Accordion:** `LxAccordion` with `LxAccordionItem` records (`Value`, `Content`); `Value` opens items initially.
- **Pagination:** `LxPagination` takes `Total`, `PageSize` and `Page`. With `HrefTemplate="/items?page={page}"` the pages are links and your page reads the number from the URL, which works without interactivity; otherwise it renders buttons.
- **Command palette:** `LxCommandPalette` with `LxCommandGroup` records for a searchable list of commands; `LxDashboardSearch` and `LxContentSearch` wrap the same idea with a Ctrl+K dialog.
