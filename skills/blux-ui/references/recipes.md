# Recipes

Short, complete patterns for the jobs that come up most. Check parameters with `get-component-metadata` before adapting them.

## Data table

`BxTable` takes `Items`; each `BxColumn` names its `TItem`, a `Title`, and either a `Field` (shown and used for sorting) or a `Template` for custom cells. A `Sortable` column with a `Field` gets a sort button; sorting runs in .NET, so the buttons stay disabled until the page is interactive. A `Caption` names the table for assistive technology.

```razor
<BxTable Items="_people" Caption="Team members" Class="rounded-xl border border-(--blux-border)">
    <BxColumn TItem="Person" Title="Name" Field="p => p.Name" Sortable Class="font-medium text-(--blux-text)" />
    <BxColumn TItem="Person" Title="Role" Field="p => p.Role" Sortable />
    <BxColumn TItem="Person" Title="Joined" Field="p => p.Joined" Sortable>
        <Template>@context.Joined.ToString("MMM yyyy")</Template>
    </BxColumn>
    <BxColumn TItem="Person" Title="Status">
        <Template>
            <BxBadge Label="@context.Status" Variant="BxVariant.Subtle" Size="BxSize.Sm"
                     Color="@(context.Status == "Active" ? BxColor.Success : BxColor.Neutral)" />
        </Template>
    </BxColumn>
</BxTable>

@code {
    private sealed record Person(string Name, string Role, DateOnly Joined, string Status);

    private static readonly Person[] _people = [new("Lindsay Walton", "Front-end developer", new(2024, 3, 1), "Active")];
}
```

- `Template` receives the row as `@context`. Use it for badges, buttons and formatting.
- `Sticky` keeps the header visible while the table scrolls; give the table a maximum height through `Class`.
- `Empty` is the slot for the no-data state (use `BxEmpty` inside it).
- Sorting, filtering and paging of large data sets belong in your query: feed `Items` the current page and drive `BxPagination` with an `HrefTemplate` (links, so it works in static pages) from the query string.

## Sign-in

`BxAuthForm` builds the form from `BxAuthField` records (name, type, then `Label`, `Placeholder`, `Required`, `Autocomplete`, `Help`, `Items` for a select, `Length` for a code). It validates with a `Validate` function, shows a busy state while `OnValidSubmit` runs, and posts under its `FormName` in static rendering.

```razor
<BxAuthForm FormName="auth-sign-in" Title="Welcome back" Description="Sign in to your account to continue."
            Icon="@BxIcons.Named("lock")" Fields="Fields" Providers="Providers" SubmitLabel="Sign in"
            Validate="CheckPassword" Error="@_error" OnValidSubmit="SignInAsync">
    <Validation>
        <a href="/forgot" class="text-sm text-(--blux-primary-text) underline underline-offset-4">Forgot your password?</a>
    </Validation>
    <Footer>New here? <a href="/sign-up" class="font-medium text-(--blux-primary-text) underline underline-offset-4">Create an account</a></Footer>
</BxAuthForm>

@code {
    private string? _error;

    private static readonly BxAuthField[] Fields =
    [
        new("email", "email") { Label = "Email", Placeholder = "you@example.com", Required = true, Autocomplete = "email" },
        new("password", "password") { Label = "Password", Required = true, Autocomplete = "current-password" },
        new("remember", "checkbox") { Label = "Remember me" },
    ];

    private static readonly BxAuthProvider[] Providers =
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

Modal, slideover and drawer share one shape: the default slot is the trigger, `Body` and `Footer` are the content, `Title` and `Description` name it. Anything marked `data-bx-close` closes the overlay, in the browser, with no handler.

```razor
<BxModal @bind-Open="_open" Title="Edit profile" Description="Changes are saved when you close the dialog.">
    <ChildContent><BxButton Label="Edit profile" Variant="ButtonVariant.Outline" /></ChildContent>
    <Body>
        <BxFormField Label="Name"><BxInput Value="Ada Lovelace" /></BxFormField>
    </Body>
    <Footer>
        <BxButton Label="Cancel" Color="BxColor.Neutral" Variant="ButtonVariant.Ghost" data-bx-close />
        <BxButton Label="Save" data-bx-close />
    </Footer>
</BxModal>
```

- **Confirmation:** `Dismissible="false" Close="false" Size="BxSize.Sm"` so Escape and the backdrop do nothing and the user must choose; put `data-bx-close` on both buttons.

```razor
<BxModal Title="Delete project?" Description="This removes the project and its history. It cannot be undone." Dismissible="false" Close="false" Size="BxSize.Sm">
    <ChildContent><BxButton Label="Delete project" Color="BxColor.Error" Variant="ButtonVariant.Soft" /></ChildContent>
    <Footer>
        <BxButton Label="Cancel" Color="BxColor.Neutral" Variant="ButtonVariant.Outline" data-bx-close />
        <BxButton Label="Delete" Color="BxColor.Error" data-bx-close />
    </Footer>
</BxModal>
```

- A `data-bx-close` button does not run your code. To act on the choice, bind `@bind-Open` and react to it, or use a form post.
- **Popover:** the default slot is the trigger (it must contain a `<button>`), `Content` is the panel, `Placement` is `"bottom-start"` and the like.

```razor
<BxPopover Placement="bottom-start">
    <ChildContent><BxButton Label="Details" Variant="ButtonVariant.Outline" /></ChildContent>
    <Content><p class="w-64 p-4 text-sm">Escape or a click outside closes me.</p></Content>
</BxPopover>
```

- **Menu:** `BxDropdownMenu` with `Items` (`BxMenuItem` with `Label`, `Href`, `Shortcut`, `Color`, `Disabled`; `BxMenuItem.Heading("...")` and `BxMenuItem.Separator()`), `OnSelect` for the chosen item, the trigger as the default slot. `BxContextMenu` takes the same items around the area that opens it.
- **Toast:** inject `IToastService` and add a `BxToast` (`Title`, `Description`, `Color`, `Duration`). It needs a toaster, which `BxApp` provides.

```razor
@inject IToastService Toasts

<BxButton Label="Save" OnClick="SavedAsync" />

@code {
    private async Task SavedAsync() => await Toasts.Add(new BxToast
    {
        Title = "Changes saved",
        Description = "Your profile is up to date.",
        Color = BxColor.Success,
    });
}
```

## Navigation

- **Site links:** `BxNavigationMenu` with `BxNavigationItem` records. The current page is marked from the address (`Exact = true` for the home link). Items with `Children` open as a panel or, when vertical, a collapsible group. Always give it an `AriaLabel`.

```razor
<BxNavigationMenu Items="_items" AriaLabel="Primary" />

@code {
    private static readonly BxNavigationItem[] _items =
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

- **Tabs:** `BxTabs` with `BxTabItem` records. Each has a `Value` and `Content` (a `RenderFragment`, written `@<p>...</p>` in a field), `Disabled` as needed. Bind `@bind-Value` only if .NET needs the selected tab.

```razor
<BxTabs Items="_tabs" @bind-Value="_tab" AriaLabel="Account settings" />

@code {
    private string? _tab;

    private static readonly BxTabItem[] _tabs =
    [
        new("Account") { Value = "account", Content = @<p class="p-4 text-sm">Make changes to your account.</p> },
        new("Password") { Value = "password", Content = @<p class="p-4 text-sm">Change your password.</p> },
    ];
}
```

- **Breadcrumb:** `BxBreadcrumb` with `BxBreadcrumbItem` records; the last item has no `Href`.
- **Accordion:** `BxAccordion` with `BxAccordionItem` records (`Value`, `Content`); `Value` opens items initially.
- **Pagination:** `BxPagination` takes `Total`, `PageSize` and `Page`. With `HrefTemplate="/items?page={page}"` the pages are links and your page reads the number from the URL, which works without interactivity; otherwise it renders buttons.
- **Command palette:** `BxCommandPalette` with `BxCommandGroup` records for a searchable list of commands; `BxDashboardSearch` and `BxContentSearch` wrap the same idea with a Ctrl+K dialog.
