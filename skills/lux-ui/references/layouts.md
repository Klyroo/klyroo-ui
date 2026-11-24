# Layouts

Page-level patterns, each built from components that exist in the library. Fetch the metadata of the ones you use; the markup below shows how they fit together.

## App shell

`LxApp` wraps the app once, in the main layout: it sets the language and direction, hosts the toaster that `IToastService` writes to, and takes global slot overrides. Do not also render a `LxToaster`.

```razor
@inherits LayoutComponentBase

<LxApp Lang="en">
    <LxHeader Title="Acme" Items="_items" AriaLabel="Site" MenuTitle="Site menu">
        <Right>
            <LxButton Label="Sign in" Size="LxSize.Sm" Variant="ButtonVariant.Outline" Color="LxColor.Neutral" Href="/sign-in" />
        </Right>
    </LxHeader>
    <LxMain>@Body</LxMain>
    <LxFooter>
        <Left><p>Copyright 2026 Acme Inc.</p></Left>
    </LxFooter>
</LxApp>

@code {
    private static readonly LxNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true },
        new("Docs") { Href = "/docs" },
    ];
}
```

- `LxHeader` collapses its items into a menu button below the `lg` breakpoint (`MenuTitle` names that menu). `Sticky="false"` turns the sticky behavior off.
- `LxMain` fills the screen below the header so a short page does not float the footer up. It is the page's `<main>`; use `As="div"` where a `main` already exists.
- `LxFooter` has `Left`, `Right` and the default slot; `LxFooterColumns` puts columns of links above it.

## Dashboard

`LxDashboardGroup` is the frame: it holds a `LxDashboardSidebar` and any number of `LxDashboardPanel`s, remembers the sizes and the collapsed state in the browser under `StorageKey`, and gives the collapse and toggle buttons (which can sit in a navbar) a way to find the sidebar. Below the `lg` breakpoint the sidebar becomes an off-canvas panel opened by the toggle. The group fills its parent: give it the viewport (`h-dvh`) in an app.

```razor
<LxDashboardGroup StorageKey="app-dashboard">
    <LxDashboardSidebar Resizable>
        <Header><span class="font-semibold text-(--lux-text-highlighted)">Acme</span></Header>
        <ChildContent>
            <LxDashboardSearchButton />
            <LxNavigationMenu Items="_items" Orientation="LxOrientation.Vertical" AriaLabel="Workspace" Ui="_navUi" />
        </ChildContent>
        <Footer>
            <span class="min-w-0 truncate text-sm group-data-[state=collapsed]/sidebar:sr-only">Ada Lovelace</span>
        </Footer>
    </LxDashboardSidebar>

    <LxDashboardPanel Resizable Size="35" MinSize="20" MaxSize="60" ResizeLabel="Resize inbox">
        <Header>
            <LxDashboardNavbar Title="Inbox">
                <Leading><LxDashboardSidebarCollapse /></Leading>
            </LxDashboardNavbar>
            <LxDashboardToolbar>
                <Left>
                    <LxButton Label="All" Size="LxSize.Sm" Variant="ButtonVariant.Soft" Color="LxColor.Neutral" />
                </Left>
            </LxDashboardToolbar>
        </Header>
        <Body>@* list *@</Body>
    </LxDashboardPanel>

    <LxDashboardPanel>
        <Header>
            <LxDashboardNavbar Title="Message" Toggle="false">
                <Right><LxButton Label="Reply" Size="LxSize.Sm" Variant="ButtonVariant.Outline" Color="LxColor.Neutral" /></Right>
            </LxDashboardNavbar>
        </Header>
        <Body>@* detail *@</Body>
    </LxDashboardPanel>

    <LxDashboardSearch Groups="_groups" OnSelect="item => _last = item.Label" />
</LxDashboardGroup>

@code {
    private string? _last;
    private static readonly NavigationMenuUi _navUi = new() { Label = "group-data-[state=collapsed]/sidebar:sr-only" };

    private static readonly LxNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true, Icon = LxIcons.Named("home", "size-5") },
        new("Inbox") { Href = "/inbox", Icon = LxIcons.Named("inbox", "size-5") },
    ];

    private static readonly LxCommandGroup[] _groups =
    [
        new("Go to", [new() { Label = "Inbox", Href = "/inbox", Keywords = "mail messages" }]),
    ];
}
```

- Bind `@bind-Sizes` on the group and `@bind-Collapsed` on the sidebar only when .NET needs them.
- Labels that must disappear when the sidebar shows icons only use `group-data-[state=collapsed]/sidebar:sr-only`, so screen readers still hear them. That class needs Tailwind compiled by the app.
- `LxDashboardSearch` opens with Ctrl+K or the search button.
- A sidebar outside a dashboard is `LxSidebar` (`Id`, `Header`, `ChildContent`, `Footer`); it collapses and remembers it too.
- `LxDashboardSidebar` and `LxSidebar` render an `aside`; pass `As="div"` when the component already sits inside a `main` landmark, as in a docs example.

## Docs site

The pieces of a documentation page, all in the Navigation family:

- `LxContentNavigation` for the left tree. Items are `LxContentNavigationItem` (title, `Href`, `Icon`, `Open`, `Badge`, `Children`); the current page is found from the address.
- `LxContentToc` for the right-hand table of contents, with `Links` (`LxContentTocLink(id, text)`, `Level` for nesting). It follows the heading being read in the browser.
- `LxContentSurround` for the previous and next links (`LxContentSurroundLink(title, href)` with `Description`).
- `LxContentSearch` with `LxContentSearchButton` for Ctrl+K search over `LxContentSearchPage` records (title, href, `Group`, `Keywords`, `Headings`).

```razor
<div class="mx-auto flex max-w-screen-xl gap-10 px-4">
    <aside aria-label="Documentation sidebar" class="sticky top-16 hidden h-[calc(100dvh-4rem)] w-56 shrink-0 overflow-y-auto py-8 lg:block">
        <LxContentNavigation Items="_navigation" AriaLabel="Documentation" />
    </aside>
    <main id="main" class="min-w-0 flex-1 py-8">
        <article class="max-w-4xl">
            @Body
            <LxContentSurround Previous="_previous" Next="_next" />
        </article>
    </main>
    <aside class="hidden w-44 xl:block">
        <LxContentToc Links="_toc" Title="Contents" />
    </aside>
</div>
```

Give each heading an `id` that matches a `LxContentTocLink`, and a `scroll-mt-*` class so the sticky header does not cover it.

## Chat

- `LxChatMessages` is the list: it keeps the latest message in view while you are at the bottom and offers "Jump to latest" when you scroll up. Its `Status` (`ChatStatus`) drives the indicator and the error slot.
- `LxChatMessage` is one message: `Role` (`ChatRole.User` or `Assistant`), `Text`, `Streaming`, `Compact`, avatar parameters, and slots for actions and files. Give each a `@key`.
- `LxChatPrompt` is the input. Enter sends, Shift+Enter adds a line, an IME composition never sends. `OnSubmit` receives the text; with `Status`, `OnStop` and `OnReload` it becomes a stop and a retry button. In static rendering it posts under `FormName`.
- `LxChatPalette` composes both into a panel that takes the height of its box: messages take what is left, the prompt is pinned under them. Put it in a modal, a slideover or any box with a height.

```razor
<div class="h-96 w-full max-w-xl overflow-hidden rounded-xl border border-(--lux-border)">
    <LxChatPalette>
        <ChildContent>
            <LxChatMessages Status="_status">
                @foreach (var message in _messages)
                {
                    <LxChatMessage @key="message.Id" Role="message.Role" Compact Streaming="message.Streaming" Text="@message.Text" />
                }
            </LxChatMessages>
        </ChildContent>
        <Prompt>
            <LxChatPrompt FormName="chat" Status="_status" OnSubmit="Send" OnStop="Stop" OnReload="Retry" Placeholder="Ask something..." Label="Message" />
        </Prompt>
    </LxChatPalette>
</div>
```

To stream a model's answer, append each token to the message text and call `StateHasChanged`, and set `Streaming` until the stream ends so assistive technology announces the finished answer once. `LxChatReasoning`, `LxChatTool`, `LxChatShimmer` and `LxChatAttachment` show reasoning, tool calls, a loading shimmer and files inside a message.

## Editor

`LxEditor` is a rich-text editor that binds HTML and joins `LxFormField` and `EditForm`. Its parts are child components, so you include only what you want:

```razor
<LxFormField Label="Release notes" Help="Type / for blocks.">
    <LxEditor @bind-Value="_html" Placeholder="Write something...">
        <LxEditorToolbar />
        <LxEditorDragHandle />
        <LxEditorSuggestionMenu />
        <LxEditorMentionMenu Items="People" />
        <LxEditorEmojiMenu />
    </LxEditor>
</LxFormField>

@code {
    private string? _html = "<p>Hello <strong>Lux</strong>.</p>";

    private static readonly EditorMentionItem[] People =
    [
        new("ada", "Ada Lovelace") { Description = "Mathematician" },
    ];
}
```

`LxEditorToolbar` takes `Items` (`EditorToolbarItem` with an `EditorAction`, `EditorToolbarItem.Separator`, `EditorToolbarItem.Heading(2)`) to choose its buttons. The value is HTML: sanitize it on the server before you store or render it elsewhere.

## Landing page

Compose a page from sections, each with a `Headline`, `Title`, `Description`, `Links` (`PageLinkItem`) and a `Media` slot. `HeadingLevel` sets the heading element, so keep one `h1` per page.

```razor
<LxPageHero Headline="Open source" Title="Ship forms faster" Description="Accessible form controls in every render mode." Links="_links">
    <Media><img src="/hero.png" alt="The dashboard" /></Media>
</LxPageHero>

<LxPageSection Icon="zap" Headline="Features" Title="Built for the way Blazor renders" HeadingLevel="2" Features="_features" />

<LxPageGrid Columns="3" Class="gap-4">
    <LxPageCard Icon="lock" Title="Private" Description="Nothing leaves your servers." />
    <LxPageCard Icon="globe" Title="Global" Description="Static output caches on any CDN." />
</LxPageGrid>

<LxPageCta Variant="LxVariant.Soft" Title="Start building today" Description="Add the package and render a component." Links="_cta" />

@code {
    private readonly PageLinkItem[] _links = [new("Get started", "/docs/installation") { TrailingIcon = "arrow-right" }];

    private readonly PageFeatureItem[] _features =
    [
        new("Browser-first state", "Menus and dialogs answer in the browser.") { Icon = "globe" },
        new("Themeable", "A handful of CSS variables restyle every component.") { Icon = "pencil", Href = "/docs/theming" },
    ];

    private readonly PageLinkItem[] _cta = [new("Read the docs", "/docs/installation") { Color = LxColor.Neutral }];
}
```

Add `LxPageLogos` for a logo strip, `LxPricingPlans` for pricing, `LxBlogPosts` for a blog index and `LxChangelogVersions` for a changelog. Icon names are the built-in ones (`search-icons`).
