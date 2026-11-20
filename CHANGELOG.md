# Changelog

All notable changes to Blux.UI are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and the project follows
[Semantic Versioning](https://semver.org/spec/v2.0.0.html) (while below 1.0, a minor version may contain breaking changes).

## [Unreleased]

### Added

- **AI tooling**, generated from the docs catalog and the library so it cannot go stale: the `Blux.UI.Mcp` dotnet tool (`blux-ui-mcp`, an MCP server over stdio),
  the same server over HTTP at `/mcp` on the docs site (`X-MCP-Tools` restricts the tools), `/llms.txt` and `/llms-full.txt`, and the `blux-ui` agent skill in `skills/blux-ui`.
  An "AI" section in the docs explains how to install each.

## [0.1.0] - Unreleased

First public release. Components work in Interactive Server, WebAssembly, Auto and static server rendering; UI state
(open, close, focus, keyboard, selection) runs in the browser through custom elements, so overlays work without a .NET circuit.

### Added

- **Elements**: Button, Badge, Avatar, AvatarGroup, Card, Alert, Banner, Chip, Container, Empty, FieldGroup, Icon, Kbd, Link, Progress, Separator, Skeleton, User.
- **Form**: Input, Textarea, Checkbox, CheckboxGroup, Switch, RadioGroup, Select, SelectMenu, InputMenu, InputNumber, InputTags, InputRating, InputDate, InputTime, PinInput, Slider, Toggle, ToggleGroup, Listbox, Calendar, ColorPicker, FileUpload, Form and FormField (EditForm integration, `aria-invalid`, posted names).
- **Overlay**: Modal, Drawer, Slideover, Popover, Tooltip, DropdownMenu, ContextMenu, CommandPalette, Toast with `IToastService`, and the ColorMode components (Button, Switch, Select, Avatar, Image).
- **Navigation**: Tabs, Accordion, Breadcrumb, Pagination, Stepper, Timeline, NavigationMenu, Collapsible, Tree, Marquee, ScrollArea, Splitter, and the Content components (ContentNavigation, ContentSearch, ContentSearchButton, ContentSurround, ContentToc).
- **Data**: Table, Chart, Carousel.
- **Layout**: App, Header, Footer, FooterColumns, Main, Sidebar, Error, LocaleSelect, Theme.
- **Page**: Page, PageHeader, PageHero, PageSection, PageFeature, PageGrid, PageColumns, PageCard, PageCta, PageLinks, PageList, PageLogos, PageAnchors, PageAside, PageBody, PricingPlan, PricingPlans, PricingTable, BlogPost, BlogPosts, ChangelogVersion, ChangelogVersions, AuthForm.
- **Dashboard**: DashboardGroup, DashboardSidebar (with collapse and toggle), DashboardPanel, DashboardResizeHandle, DashboardNavbar, DashboardToolbar, DashboardSearch, DashboardSearchButton.
- **Chat**: ChatMessage, ChatMessages, ChatPrompt, ChatPromptSubmit, ChatPalette, ChatAttachment, ChatReasoning, ChatShimmer, ChatTool.
- **Editor**: Editor, EditorToolbar, EditorDragHandle, and the suggestion, mention and emoji menus (rich text engine in `blux-editor.js`).
- Theme engine with `AddBluxUi(options => ...)`, per-slot `Ui` overrides, `Class` merging, `--blux-*` tokens, seven built-in colors plus custom colors, light and dark mode (`BxColorModeScript`).
- Two styling modes: precompiled `blux.css` with `blux.preflight.css`, or Tailwind v4 compiled by the app with `obj/blux/blux.theme.css` copied by the package's `buildTransitive` targets.
- Package: XML documentation, SourceLink, embedded untracked sources and a symbol package (`.snupkg`), MIT license.

