# Layouts

Page-level patterns, each built from components that exist in the library. Fetch the metadata of the ones you use; the markup below shows how they fit together.

## App shell

`KlyApp` wraps the app once, in the main layout: it sets the language and direction, hosts the toaster that `IToastService` writes to, and takes global slot overrides. Do not also render a `KlyToaster`.

```razor
@inherits LayoutComponentBase

<KlyApp Lang="en">
    <KlyHeader Title="Acme" Items="_items" AriaLabel="Site" MenuTitle="Site menu">
        <Right>
            <KlyButton Label="Sign in" Size="KlySize.Sm" Variant="ButtonVariant.Outline" Color="KlyColor.Neutral" Href="/sign-in" />
        </Right>
    </KlyHeader>
    <KlyMain>@Body</KlyMain>
    <KlyFooter>
        <Left><p>Copyright 2026 Acme Inc.</p></Left>
    </KlyFooter>
</KlyApp>

@code {
    private static readonly KlyNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true },
        new("Docs") { Href = "/docs" },
    ];
}
```

- `KlyHeader` collapses its items into a menu button below the `lg` breakpoint (`MenuTitle` names that menu). `Sticky="false"` turns the sticky behavior off.
- `KlyMain` fills the screen below the header so a short page does not float the footer up. It is the page's `<main>`; use `As="div"` where a `main` already exists.
- `KlyFooter` has `Left`, `Right` and the default slot; `KlyFooterColumns` puts columns of links above it.

## Dashboard

`KlyDashboardGroup` is the frame: it holds a `KlyDashboardSidebar` and any number of `KlyDashboardPanel`s, remembers the sizes and the collapsed state in the browser under `StorageKey`, and gives the collapse and toggle buttons (which can sit in a navbar) a way to find the sidebar. Below the `lg` breakpoint the sidebar becomes an off-canvas panel opened by the toggle. The group fills its parent: give it the viewport (`h-dvh`) in an app.

```razor
<KlyDashboardGroup StorageKey="app-dashboard">
    <KlyDashboardSidebar Resizable>
        <Header><span class="font-semibold text-(--klyroo-text-highlighted)">Acme</span></Header>
        <ChildContent>
            <KlyDashboardSearchButton />
            <KlyNavigationMenu Items="_items" Orientation="KlyOrientation.Vertical" AriaLabel="Workspace" Ui="_navUi" />
        </ChildContent>
        <Footer>
            <span class="min-w-0 truncate text-sm group-data-[state=collapsed]/sidebar:sr-only">Ada Lovelace</span>
        </Footer>
    </KlyDashboardSidebar>

    <KlyDashboardPanel Resizable Size="35" MinSize="20" MaxSize="60" ResizeLabel="Resize inbox">
        <Header>
            <KlyDashboardNavbar Title="Inbox">
                <Leading><KlyDashboardSidebarCollapse /></Leading>
            </KlyDashboardNavbar>
            <KlyDashboardToolbar>
                <Left>
                    <KlyButton Label="All" Size="KlySize.Sm" Variant="ButtonVariant.Soft" Color="KlyColor.Neutral" />
                </Left>
            </KlyDashboardToolbar>
        </Header>
        <Body>@* list *@</Body>
    </KlyDashboardPanel>

    <KlyDashboardPanel>
        <Header>
            <KlyDashboardNavbar Title="Message" Toggle="false">
                <Right><KlyButton Label="Reply" Size="KlySize.Sm" Variant="ButtonVariant.Outline" Color="KlyColor.Neutral" /></Right>
            </KlyDashboardNavbar>
        </Header>
        <Body>@* detail *@</Body>
    </KlyDashboardPanel>

    <KlyDashboardSearch Groups="_groups" OnSelect="item => _last = item.Label" />
</KlyDashboardGroup>

@code {
    private string? _last;
    private static readonly NavigationMenuUi _navUi = new() { Label = "group-data-[state=collapsed]/sidebar:sr-only" };

    private static readonly KlyNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true, Icon = KlyIcons.Named("home", "size-5") },
        new("Inbox") { Href = "/inbox", Icon = KlyIcons.Named("inbox", "size-5") },
    ];

    private static readonly KlyCommandGroup[] _groups =
    [
        new("Go to", [new() { Label = "Inbox", Href = "/inbox", Keywords = "mail messages" }]),
    ];
}
```

- Bind `@bind-Sizes` on the group and `@bind-Collapsed` on the sidebar only when .NET needs them.
- Labels that must disappear when the sidebar shows icons only use `group-data-[state=collapsed]/sidebar:sr-only`, so screen readers still hear them. That class needs Tailwind compiled by the app.
- `KlyDashboardSearch` opens with Ctrl+K or the search button.
- A sidebar outside a dashboard is `KlySidebar` (`Id`, `Header`, `ChildContent`, `Footer`); it collapses and remembers it too.
- `KlyDashboardSidebar` and `KlySidebar` render an `aside`; pass `As="div"` when the component already sits inside a `main` landmark, as in a docs example.

## Docs site

The pieces of a documentation page, all in the Navigation family:

- `KlyContentNavigation` for the left tree. Items are `KlyContentNavigationItem` (title, `Href`, `Icon`, `Open`, `Badge`, `Children`); the current page is found from the address.
- `KlyContentToc` for the right-hand table of contents, with `Links` (`KlyContentTocLink(id, text)`, `Level` for nesting). It follows the heading being read in the browser.
- `KlyContentSurround` for the previous and next links (`KlyContentSurroundLink(title, href)` with `Description`).
- `KlyContentSearch` with `KlyContentSearchButton` for Ctrl+K search over `KlyContentSearchPage` records (title, href, `Group`, `Keywords`, `Headings`).

```razor
<div class="mx-auto flex max-w-screen-xl gap-10 px-4">
    <aside aria-label="Documentation sidebar" class="sticky top-16 hidden h-[calc(100dvh-4rem)] w-56 shrink-0 overflow-y-auto py-8 lg:block">
        <KlyContentNavigation Items="_navigation" AriaLabel="Documentation" />
    </aside>
    <main id="main" class="min-w-0 flex-1 py-8">
        <article class="max-w-4xl">
            @Body
            <KlyContentSurround Previous="_previous" Next="_next" />
        </article>
    </main>
    <aside class="hidden w-44 xl:block">
        <KlyContentToc Links="_toc" Title="Contents" />
    </aside>
</div>
```

Give each heading an `id` that matches a `KlyContentTocLink`, and a `scroll-mt-*` class so the sticky header does not cover it.

## Chat

- `KlyChatMessages` is the list: it keeps the latest message in view while you are at the bottom and offers "Jump to latest" when you scroll up. Its `Status` (`ChatStatus`) drives the indicator and the error slot.
- `KlyChatMessage` is one message: `Role` (`ChatRole.User` or `Assistant`), `Text`, `Streaming`, `Compact`, avatar parameters, and slots for actions and files. Give each a `@key`.
- `KlyChatPrompt` is the input. Enter sends, Shift+Enter adds a line, an IME composition never sends. `OnSubmit` receives the text; with `Status`, `OnStop` and `OnReload` it becomes a stop and a retry button. In static rendering it posts under `FormName`.
- `KlyChatPalette` composes both into a panel that takes the height of its box: messages take what is left, the prompt is pinned under them. Put it in a modal, a slideover or any box with a height.

```razor
<div class="h-96 w-full max-w-xl overflow-hidden rounded-xl border border-(--klyroo-border)">
    <KlyChatPalette>
        <ChildContent>
            <KlyChatMessages Status="_status">
                @foreach (var message in _messages)
                {
                    <KlyChatMessage @key="message.Id" Role="message.Role" Compact Streaming="message.Streaming" Text="@message.Text" />
                }
            </KlyChatMessages>
        </ChildContent>
        <Prompt>
            <KlyChatPrompt FormName="chat" Status="_status" OnSubmit="Send" OnStop="Stop" OnReload="Retry" Placeholder="Ask something..." Label="Message" />
        </Prompt>
    </KlyChatPalette>
</div>
```

To stream a model's answer, append each token to the message text and call `StateHasChanged`, and set `Streaming` until the stream ends so assistive technology announces the finished answer once. `KlyChatReasoning`, `KlyChatTool`, `KlyChatShimmer` and `KlyChatAttachment` show reasoning, tool calls, a loading shimmer and files inside a message.

## Editor

`KlyEditor` is a rich-text editor that binds HTML and joins `KlyFormField` and `EditForm`. Its parts are child components, so you include only what you want:

```razor
<KlyFormField Label="Release notes" Help="Type / for blocks.">
    <KlyEditor @bind-Value="_html" Placeholder="Write something...">
        <KlyEditorToolbar />
        <KlyEditorDragHandle />
        <KlyEditorSuggestionMenu />
        <KlyEditorMentionMenu Items="People" />
        <KlyEditorEmojiMenu />
    </KlyEditor>
</KlyFormField>

@code {
    private string? _html = "<p>Hello <strong>Klyroo</strong>.</p>";

    private static readonly EditorMentionItem[] People =
    [
        new("ada", "Ada Lovelace") { Description = "Mathematician" },
    ];
}
```

`KlyEditorToolbar` takes `Items` (`EditorToolbarItem` with an `EditorAction`, `EditorToolbarItem.Separator`, `EditorToolbarItem.Heading(2)`) to choose its buttons. The value is HTML: sanitize it on the server before you store or render it elsewhere.

## Landing page

Compose a page from sections, each with a `Headline`, `Title`, `Description`, `Links` (`PageLinkItem`) and a `Media` slot. `HeadingLevel` sets the heading element, so keep one `h1` per page.

```razor
<KlyPageHero Headline="Open source" Title="Ship forms faster" Description="Accessible form controls in every render mode." Links="_links">
    <Media><img src="/hero.png" alt="The dashboard" /></Media>
</KlyPageHero>

<KlyPageSection Icon="zap" Headline="Features" Title="Built for the way Blazor renders" HeadingLevel="2" Features="_features" />

<KlyPageGrid Columns="3" Class="gap-4">
    <KlyPageCard Icon="lock" Title="Private" Description="Nothing leaves your servers." />
    <KlyPageCard Icon="globe" Title="Global" Description="Static output caches on any CDN." />
</KlyPageGrid>

<KlyPageCta Variant="KlyVariant.Soft" Title="Start building today" Description="Add the package and render a component." Links="_cta" />

@code {
    private readonly PageLinkItem[] _links = [new("Get started", "/docs/installation") { TrailingIcon = "arrow-right" }];

    private readonly PageFeatureItem[] _features =
    [
        new("Browser-first state", "Menus and dialogs answer in the browser.") { Icon = "globe" },
        new("Themeable", "A handful of CSS variables restyle every component.") { Icon = "pencil", Href = "/docs/theming" },
    ];

    private readonly PageLinkItem[] _cta = [new("Read the docs", "/docs/installation") { Color = KlyColor.Neutral }];
}
```

Add `KlyPageLogos` for a logo strip, `KlyPricingPlans` for pricing, `KlyBlogPosts` for a blog index and `KlyChangelogVersions` for a changelog. Icon names are the built-in ones (`search-icons`).
