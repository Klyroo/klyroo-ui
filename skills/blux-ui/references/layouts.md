# Layouts

Page-level patterns, each built from components that exist in the library. Fetch the metadata of the ones you use; the markup below shows how they fit together.

## App shell

`BxApp` wraps the app once, in the main layout: it sets the language and direction, hosts the toaster that `IToastService` writes to, and takes global slot overrides. Do not also render a `BxToaster`.

```razor
@inherits LayoutComponentBase

<BxApp Lang="en">
    <BxHeader Title="Acme" Items="_items" AriaLabel="Site" MenuTitle="Site menu">
        <Right>
            <BxButton Label="Sign in" Size="BxSize.Sm" Variant="ButtonVariant.Outline" Color="BxColor.Neutral" Href="/sign-in" />
        </Right>
    </BxHeader>
    <BxMain>@Body</BxMain>
    <BxFooter>
        <Left><p>Copyright 2026 Acme Inc.</p></Left>
    </BxFooter>
</BxApp>

@code {
    private static readonly BxNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true },
        new("Docs") { Href = "/docs" },
    ];
}
```

- `BxHeader` collapses its items into a menu button below the `lg` breakpoint (`MenuTitle` names that menu). `Sticky="false"` turns the sticky behavior off.
- `BxMain` fills the screen below the header so a short page does not float the footer up. It is the page's `<main>`; use `As="div"` where a `main` already exists.
- `BxFooter` has `Left`, `Right` and the default slot; `BxFooterColumns` puts columns of links above it.

## Dashboard

`BxDashboardGroup` is the frame: it holds a `BxDashboardSidebar` and any number of `BxDashboardPanel`s, remembers the sizes and the collapsed state in the browser under `StorageKey`, and gives the collapse and toggle buttons (which can sit in a navbar) a way to find the sidebar. Below the `lg` breakpoint the sidebar becomes an off-canvas panel opened by the toggle. The group fills its parent: give it the viewport (`h-dvh`) in an app.

```razor
<BxDashboardGroup StorageKey="app-dashboard">
    <BxDashboardSidebar Resizable>
        <Header><span class="font-semibold text-(--blux-text-highlighted)">Acme</span></Header>
        <ChildContent>
            <BxDashboardSearchButton />
            <BxNavigationMenu Items="_items" Orientation="BxOrientation.Vertical" AriaLabel="Workspace" Ui="_navUi" />
        </ChildContent>
        <Footer>
            <span class="min-w-0 truncate text-sm group-data-[state=collapsed]/sidebar:sr-only">Ada Lovelace</span>
        </Footer>
    </BxDashboardSidebar>

    <BxDashboardPanel Resizable Size="35" MinSize="20" MaxSize="60" ResizeLabel="Resize inbox">
        <Header>
            <BxDashboardNavbar Title="Inbox">
                <Leading><BxDashboardSidebarCollapse /></Leading>
            </BxDashboardNavbar>
            <BxDashboardToolbar>
                <Left>
                    <BxButton Label="All" Size="BxSize.Sm" Variant="ButtonVariant.Soft" Color="BxColor.Neutral" />
                </Left>
            </BxDashboardToolbar>
        </Header>
        <Body>@* list *@</Body>
    </BxDashboardPanel>

    <BxDashboardPanel>
        <Header>
            <BxDashboardNavbar Title="Message" Toggle="false">
                <Right><BxButton Label="Reply" Size="BxSize.Sm" Variant="ButtonVariant.Outline" Color="BxColor.Neutral" /></Right>
            </BxDashboardNavbar>
        </Header>
        <Body>@* detail *@</Body>
    </BxDashboardPanel>

    <BxDashboardSearch Groups="_groups" OnSelect="item => _last = item.Label" />
</BxDashboardGroup>

@code {
    private string? _last;
    private static readonly NavigationMenuUi _navUi = new() { Label = "group-data-[state=collapsed]/sidebar:sr-only" };

    private static readonly BxNavigationItem[] _items =
    [
        new("Home") { Href = "/", Exact = true, Icon = BxIcons.Named("home", "size-5") },
        new("Inbox") { Href = "/inbox", Icon = BxIcons.Named("inbox", "size-5") },
    ];

    private static readonly BxCommandGroup[] _groups =
    [
        new("Go to", [new() { Label = "Inbox", Href = "/inbox", Keywords = "mail messages" }]),
    ];
}
```

- Bind `@bind-Sizes` on the group and `@bind-Collapsed` on the sidebar only when .NET needs them.
- Labels that must disappear when the sidebar shows icons only use `group-data-[state=collapsed]/sidebar:sr-only`, so screen readers still hear them. That class needs Tailwind compiled by the app.
- `BxDashboardSearch` opens with Ctrl+K or the search button.
- A sidebar outside a dashboard is `BxSidebar` (`Id`, `Header`, `ChildContent`, `Footer`); it collapses and remembers it too.
- `BxDashboardSidebar` and `BxSidebar` render an `aside`; pass `As="div"` when the component already sits inside a `main` landmark, as in a docs example.

## Docs site

The pieces of a documentation page, all in the Navigation family:

- `BxContentNavigation` for the left tree. Items are `BxContentNavigationItem` (title, `Href`, `Icon`, `Open`, `Badge`, `Children`); the current page is found from the address.
- `BxContentToc` for the right-hand table of contents, with `Links` (`BxContentTocLink(id, text)`, `Level` for nesting). It follows the heading being read in the browser.
- `BxContentSurround` for the previous and next links (`BxContentSurroundLink(title, href)` with `Description`).
- `BxContentSearch` with `BxContentSearchButton` for Ctrl+K search over `BxContentSearchPage` records (title, href, `Group`, `Keywords`, `Headings`).

```razor
<div class="mx-auto flex max-w-screen-xl gap-10 px-4">
    <aside aria-label="Documentation sidebar" class="sticky top-16 hidden h-[calc(100dvh-4rem)] w-56 shrink-0 overflow-y-auto py-8 lg:block">
        <BxContentNavigation Items="_navigation" AriaLabel="Documentation" />
    </aside>
    <main id="main" class="min-w-0 flex-1 py-8">
        <article class="max-w-4xl">
            @Body
            <BxContentSurround Previous="_previous" Next="_next" />
        </article>
    </main>
    <aside class="hidden w-44 xl:block">
        <BxContentToc Links="_toc" Title="Contents" />
    </aside>
</div>
```

Give each heading an `id` that matches a `BxContentTocLink`, and a `scroll-mt-*` class so the sticky header does not cover it.

## Chat

- `BxChatMessages` is the list: it keeps the latest message in view while you are at the bottom and offers "Jump to latest" when you scroll up. Its `Status` (`ChatStatus`) drives the indicator and the error slot.
- `BxChatMessage` is one message: `Role` (`ChatRole.User` or `Assistant`), `Text`, `Streaming`, `Compact`, avatar parameters, and slots for actions and files. Give each a `@key`.
- `BxChatPrompt` is the input. Enter sends, Shift+Enter adds a line, an IME composition never sends. `OnSubmit` receives the text; with `Status`, `OnStop` and `OnReload` it becomes a stop and a retry button. In static rendering it posts under `FormName`.
- `BxChatPalette` composes both into a panel that takes the height of its box: messages take what is left, the prompt is pinned under them. Put it in a modal, a slideover or any box with a height.

```razor
<div class="h-96 w-full max-w-xl overflow-hidden rounded-xl border border-(--blux-border)">
    <BxChatPalette>
        <ChildContent>
            <BxChatMessages Status="_status">
                @foreach (var message in _messages)
                {
                    <BxChatMessage @key="message.Id" Role="message.Role" Compact Streaming="message.Streaming" Text="@message.Text" />
                }
            </BxChatMessages>
        </ChildContent>
        <Prompt>
            <BxChatPrompt FormName="chat" Status="_status" OnSubmit="Send" OnStop="Stop" OnReload="Retry" Placeholder="Ask something..." Label="Message" />
        </Prompt>
    </BxChatPalette>
</div>
```

To stream a model's answer, append each token to the message text and call `StateHasChanged`, and set `Streaming` until the stream ends so assistive technology announces the finished answer once. `BxChatReasoning`, `BxChatTool`, `BxChatShimmer` and `BxChatAttachment` show reasoning, tool calls, a loading shimmer and files inside a message.

## Editor

`BxEditor` is a rich-text editor that binds HTML and joins `BxFormField` and `EditForm`. Its parts are child components, so you include only what you want:

```razor
<BxFormField Label="Release notes" Help="Type / for blocks.">
    <BxEditor @bind-Value="_html" Placeholder="Write something...">
        <BxEditorToolbar />
        <BxEditorDragHandle />
        <BxEditorSuggestionMenu />
        <BxEditorMentionMenu Items="People" />
        <BxEditorEmojiMenu />
    </BxEditor>
</BxFormField>

@code {
    private string? _html = "<p>Hello <strong>Blux</strong>.</p>";

    private static readonly EditorMentionItem[] People =
    [
        new("ada", "Ada Lovelace") { Description = "Mathematician" },
    ];
}
```

`BxEditorToolbar` takes `Items` (`EditorToolbarItem` with an `EditorAction`, `EditorToolbarItem.Separator`, `EditorToolbarItem.Heading(2)`) to choose its buttons. The value is HTML: sanitize it on the server before you store or render it elsewhere.

## Landing page

Compose a page from sections, each with a `Headline`, `Title`, `Description`, `Links` (`PageLinkItem`) and a `Media` slot. `HeadingLevel` sets the heading element, so keep one `h1` per page.

```razor
<BxPageHero Headline="Open source" Title="Ship forms faster" Description="Accessible form controls in every render mode." Links="_links">
    <Media><img src="/hero.png" alt="The dashboard" /></Media>
</BxPageHero>

<BxPageSection Icon="zap" Headline="Features" Title="Built for the way Blazor renders" HeadingLevel="2" Features="_features" />

<BxPageGrid Columns="3" Class="gap-4">
    <BxPageCard Icon="lock" Title="Private" Description="Nothing leaves your servers." />
    <BxPageCard Icon="globe" Title="Global" Description="Static output caches on any CDN." />
</BxPageGrid>

<BxPageCta Variant="BxVariant.Soft" Title="Start building today" Description="Add the package and render a component." Links="_cta" />

@code {
    private readonly PageLinkItem[] _links = [new("Get started", "/docs/installation") { TrailingIcon = "arrow-right" }];

    private readonly PageFeatureItem[] _features =
    [
        new("Browser-first state", "Menus and dialogs answer in the browser.") { Icon = "globe" },
        new("Themeable", "A handful of CSS variables restyle every component.") { Icon = "pencil", Href = "/docs/theming" },
    ];

    private readonly PageLinkItem[] _cta = [new("Read the docs", "/docs/installation") { Color = BxColor.Neutral }];
}
```

Add `BxPageLogos` for a logo strip, `BxPricingPlans` for pricing, `BxBlogPosts` for a blog index and `BxChangelogVersions` for a changelog. Icon names are the built-in ones (`search-icons`).
