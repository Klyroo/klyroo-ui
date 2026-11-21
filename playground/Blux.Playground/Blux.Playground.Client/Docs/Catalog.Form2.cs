using Blux.UI;

namespace Blux.Playground.Client.Docs;

public static partial class Catalog
{
    private static readonly ComponentDoc[] Form2 =
    [
        new("select-menu", "SelectMenu", "Form", "A select with a filter field inside its list, for one or several values. Each value posts through a hidden input.",
            [new("Single", typeof(Examples.SelectMenuExample)), new("Multiple with creation", typeof(Examples.SelectMenuMultipleExample), "Type a label that is not in the list to add it as a value."),
             new("In an EditForm", typeof(Examples.SelectMenuFormExample), "Posts in static rendering too: every selected value becomes one form entry."),
             new("Clearing", typeof(Examples.SelectMenuClearExample), "Emptying a multiple selection posts one empty value, so a default list on the server is replaced instead of kept: ignore empty entries."),
             new("Not bound", typeof(Examples.SelectMenuUnboundExample), "Without a binding the browser keeps the selection and posts it under the name."),
             new("Groups and counts", typeof(Examples.SelectMenuGroupsExample), "Group puts items under a heading (role group, named by it); typing in the filter hides a heading together with its last matching item."),
             new("Remote options", typeof(Examples.SelectMenuRemoteExample), "OptionsProvider loads the options from a source (simulated here, 400 ms per call): the filter field searches it after Debounce (250 ms by default), the next page loads as the list scrolls, ResolveSelected names values that were never listed. Interactive only: a statically rendered page shows the field disabled, with the values it was given."),
             new("Cursor paging and restored values", typeof(Examples.SelectMenuRemoteMultipleExample), "The provider returns NextCursor instead of counting with Skip, MinLength keeps short texts from being searched, and the two initial values are named by ResolveSelected without their page being loaded."),
             new("Loading and error states", typeof(Examples.SelectMenuRemoteErrorExample), "The list is aria-busy while it loads, announces it, and shows a translated error with a Retry button (Enter in the filter field also retries) when the provider throws. Turn the switch on, open the list, turn it off and retry."),
             new("Named values", typeof(Examples.SelectMenuRemoteNamedExample), "Items are not listed with a provider but name values: France shows as France in every render mode, with no call to the source."),
             new("Create with remote options", typeof(Examples.SelectMenuRemoteCreateExample), "The option that creates the typed text follows the options that came back: it disappears when the source returns that exact option, and a text that is already a value shows no results instead of an empty list."),
             new("OptionsKey: cities of a country", typeof(Examples.SelectMenuRemoteKeyExample), "OptionsKey is what the options depend on besides the text, here the country. When it changes the loaded pages, the search text, the cursor and the resolved labels are forgotten and the list loads again when it opens; a provider that is a new delegate on every render (as here) keeps what was loaded as long as the key is the same.")],
            [typeof(BxSelectMenu)]),
        new("input-menu", "InputMenu", "Form", "A text field with a list of suggestions: free text, or restricted to the suggestions.",
            [new("Free text and strict", typeof(Examples.InputMenuExample)),
             new("Groups, icons and counts", typeof(Examples.InputMenuGroupsExample), "The same Count, Icon and Group of the items as the other menus; a heading disappears with its last suggestion while you type."),
             new("Remote suggestions", typeof(Examples.InputMenuRemoteExample), "OptionsProvider loads the suggestions from a source (simulated here, 400 ms per call) as you type, after Debounce; the next page loads as the list scrolls. The strict field restores its value from a cursor-paged source and ResolveSelected names it. Interactive only: a statically rendered page shows a free text field without suggestions, and a strict field read only."),
             new("Short text and error states", typeof(Examples.InputMenuRemoteErrorExample), "MinLength keeps a text shorter than two letters from being searched. When the provider throws, the list shows a translated error with a Retry button (Enter in the field also retries). Turn the switch on, type, turn it off and retry."),
             new("Named strict value", typeof(Examples.InputMenuRemoteNamedExample), "Items name a value without listing it: a strict field shows France, not fr, in every render mode, with no call to the source. OptionsKey works as on SelectMenu: when it changes, what was loaded is forgotten.")],
            [typeof(BxInputMenu)]),
        new("input-number", "InputNumber", "Form", "A numeric field with increment and decrement buttons, formatted for a culture.",
            [new("Quantity and price", typeof(Examples.InputNumberExample), "The posted value is always invariant, whatever the culture shown."),
             new("Not bound", typeof(Examples.InputNumberUnboundExample), "Without a binding the browser keeps the number, even when other parameters change.")], [typeof(BxInputNumber)]),
        new("input-tags", "InputTags", "Form", "A field that turns typed values into removable tags; each tag posts as its own entry.",
            [new("Tags", typeof(Examples.InputTagsExample)), new("Not bound", typeof(Examples.InputTagsUnboundExample), "Without a binding the browser keeps the tags.")], [typeof(BxInputTags)]),
        new("pin-input", "PinInput", "Form", "One box per character, with auto-advance and paste support.",
            [new("Code and masked PIN", typeof(Examples.PinInputExample))], [typeof(BxPinInput)]),
        new("slider", "Slider", "Form", "A native range input for one value, or two thumbs for a range.",
            [new("Single and range", typeof(Examples.SliderExample))], [typeof(BxSlider), typeof(BxRangeSlider)]),
        new("checkbox-group", "CheckboxGroup", "Form", "A fieldset of native checkboxes bound to a list of values.",
            [new("Checkbox group", typeof(Examples.CheckboxGroupExample))], [typeof(BxCheckboxGroup<string>)]),
        new("toggle", "Toggle", "Form", "A button that stays pressed. Works in the browser without being bound.",
            [new("Toggle", typeof(Examples.ToggleExample))], [typeof(BxToggle)]),
        new("toggle-group", "ToggleGroup", "Form", "Toggle buttons where one or several can be pressed, with arrow key navigation.",
            [new("Single and multiple", typeof(Examples.ToggleGroupExample)), new("Not bound", typeof(Examples.ToggleGroupUnboundExample), "Without a binding the browser keeps what is pressed.")], [typeof(BxToggleGroup)]),
        new("listbox", "Listbox", "Form", "An always visible list of options for one or several values, with typeahead.",
            [new("Single and multiple", typeof(Examples.ListboxExample)), new("Not bound", typeof(Examples.ListboxUnboundExample), "Without a binding the browser keeps the selection."),
             new("Groups, icons and counts", typeof(Examples.ListboxGroupsExample), "Group lists the items under headings; the arrow keys and the typeahead skip the headings.")], [typeof(BxListbox)]),
        new("input-rating", "InputRating", "Form", "A star rating built on radio buttons, with an optional half step and a read-only display.",
            [new("Rating", typeof(Examples.InputRatingExample))], [typeof(BxInputRating)]),
        new("form", "Form", "Form", "A thin wrapper over EditForm: validation summary and a busy state while submitting.",
            [new("Busy while submitting", typeof(Examples.FormExample)),
             new("Enhanced post", typeof(Examples.FormEnhancedExample), "Static rendering with Enhance: the post updates the page in place, and what the server renders back wins over what the browser showed.")], [typeof(BxForm)]),
    ];
}
