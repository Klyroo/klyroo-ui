using Blux.UI;

namespace Blux.Playground.Client.Docs;

public sealed record ExampleEntry(string Title, Type Type, string? Description = null);

public sealed record ComponentDoc(string Slug, string Name, string Group, string Description, ExampleEntry[] Examples, Type[] Api);

/// <summary>Every documented component, in sidebar order.</summary>
public static partial class Catalog
{
    public static readonly string[] Groups =
        ["Elements", "Form", "Overlay", "Navigation", "Data", "Layout", "Page", "Dashboard", "Chat", "Editor"];

    /// <summary>Every documented component. Each family declares its entries in its own Catalog.*.cs file.</summary>
    // Built on first use: static fields of the other partial files may not be initialized yet at type load.
    public static ComponentDoc[] Components => _components ??= [.. Core, .. Nav, .. Overlay2, .. Elements, .. Pickers, .. Form2, .. Page, .. Layout, .. Marketing, .. Dashboard, .. Chat, .. Editor, .. Chart, .. Content];

    private static ComponentDoc[]? _components;

    private static readonly ComponentDoc[] Core =
    [
        new("button", "Button", "Elements", "A button, or a link styled as one, in every color and variant.",
            [new("Variants", typeof(Examples.ButtonVariantsExample)), new("Sizes and icons", typeof(Examples.ButtonSizesExample)),
             new("States", typeof(Examples.ButtonStatesExample), "A button with OnClick stays disabled until the page is interactive.")],
            [typeof(BxButton)]),
        new("badge", "Badge", "Elements", "A short label for a status, a count or a category.",
            [new("Variants", typeof(Examples.BadgeExample))], [typeof(BxBadge)]),
        new("avatar", "Avatar", "Elements", "An image of a person, falling back to initials when there is none or it fails to load.",
            [new("Images and fallbacks", typeof(Examples.AvatarExample))], [typeof(BxAvatar)]),
        new("card", "Card", "Elements", "A surface grouping a header, a body and a footer.",
            [new("Card", typeof(Examples.CardExample))], [typeof(BxCard)]),
        new("alert", "Alert", "Elements", "A callout that draws attention to a message.",
            [new("Variants", typeof(Examples.AlertExample)), new("Actions and close", typeof(Examples.AlertActionsExample))], [typeof(BxAlert)]),

        new("input", "Input", "Form", "A single-line text field.",
            [new("Variants and icons", typeof(Examples.InputExample))], [typeof(BxInput)]),
        new("textarea", "Textarea", "Form", "A multi-line text field that can grow with its content.",
            [new("Textarea", typeof(Examples.TextareaExample))], [typeof(BxTextarea)]),
        new("checkbox", "Checkbox", "Form", "A native checkbox with a label and a description.",
            [new("Checkbox", typeof(Examples.CheckboxExample))], [typeof(BxCheckbox)]),
        new("switch", "Switch", "Form", "An on/off toggle built on a native checkbox, correct even without interactivity.",
            [new("Standalone", typeof(Examples.SwitchExample)), new("In an EditForm", typeof(Examples.SwitchFormExample), "Posts in static rendering too.")],
            [typeof(BxSwitch)]),
        new("radio-group", "RadioGroup", "Form", "A set of native radio buttons in a fieldset.",
            [new("Radio group", typeof(Examples.RadioGroupExample))], [typeof(BxRadioGroup<string>)]),
        new("select", "Select", "Form", "A select-only combobox with keyboard navigation and typeahead; it posts through a hidden input.",
            [new("Select", typeof(Examples.SelectExample))], [typeof(BxSelect)]),
        new("form-field", "FormField", "Form", "Label, description, hint and validation message wired to the control inside.",
            [new("A complete form", typeof(Examples.FormFieldExample))], [typeof(BxFormField)]),

        new("modal", "Modal", "Overlay", "A native modal dialog: the page becomes inert and focus stays inside.",
            [new("Modal", typeof(Examples.ModalExample)), new("Confirmation", typeof(Examples.ModalConfirmExample), "Not dismissible: Escape and the backdrop do nothing."),
             new("Nested", typeof(Examples.ModalNestedExample), "A confirmation opened from a modal closes on its own.")],
            [typeof(BxModal)]),
        new("popover", "Popover", "Overlay", "A non-modal panel anchored to a button.",
            [new("Bound open state", typeof(Examples.PopoverExample)), new("Unbound", typeof(Examples.PopoverUnboundExample), "Opens and closes without leaving the browser.")],
            [typeof(BxPopover)]),
        new("tooltip", "Tooltip", "Overlay", "A short hint shown on hover and keyboard focus.",
            [new("Tooltip", typeof(Examples.TooltipExample))], [typeof(BxTooltip)]),
        new("dropdown-menu", "DropdownMenu", "Overlay", "A menu of actions and links, navigable with the keyboard.",
            [new("Dropdown menu", typeof(Examples.DropdownMenuExample))], [typeof(BxDropdownMenu)]),
        new("toast", "Toast", "Overlay", "Brief notifications, raised from .NET through IToastService.",
            [new("Toasts", typeof(Examples.ToastExample))], [typeof(BxToaster)]),

        new("tabs", "Tabs", "Navigation", "WAI-ARIA tabs with automatic activation.",
            [new("Pill", typeof(Examples.TabsExample)), new("Link", typeof(Examples.TabsLinkExample))], [typeof(BxTabs)]),
        new("accordion", "Accordion", "Navigation", "Stacked headings that reveal their section.",
            [new("Accordion", typeof(Examples.AccordionExample)), new("With controls inside", typeof(Examples.AccordionNestedExample))], [typeof(BxAccordion)]),
        new("table", "Table", "Data", "A data table with sortable columns.",
            [new("Sortable table", typeof(Examples.TableExample))], [typeof(BxTable<object>), typeof(BxColumn<object>)]),
    ];

    public static ComponentDoc? Find(string slug) => Components.FirstOrDefault(c => c.Slug == slug);

    /// <summary>The guide pages, in sidebar order. Each is a Razor page declaring the same route (found by the docs bundle).</summary>
    public static readonly (string Href, string Title, string Section)[] Guides =
    [
        ("/docs", "Introduction", "Getting started"),
        ("/docs/installation", "Installation", "Getting started"),
        ("/docs/theming", "Theming", "Getting started"),
        ("/docs/design-system", "Design system", "Getting started"),
        ("/docs/css-variables", "CSS variables", "Getting started"),
        ("/docs/fonts", "Fonts", "Getting started"),
        ("/docs/icons", "Icons", "Getting started"),
        ("/docs/internationalization", "Internationalization", "Getting started"),
        ("/docs/images", "Images", "Getting started"),
        ("/docs/render-modes", "Render modes", "Getting started"),
        ("/docs/maui", "MAUI and Blazor Hybrid", "Getting started"),
        ("/docs/mcp", "MCP server", "AI"),
        ("/docs/llms-txt", "llms.txt", "AI"),
        ("/docs/skills", "Agent skill", "AI"),
    ];
}
