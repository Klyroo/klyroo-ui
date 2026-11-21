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
             new("Groups and counts", typeof(Examples.SelectMenuGroupsExample), "Group puts items under a heading (role group, named by it); typing in the filter hides a heading together with its last matching item.")],
            [typeof(BxSelectMenu)]),
        new("input-menu", "InputMenu", "Form", "A text field with a list of suggestions: free text, or restricted to the suggestions.",
            [new("Free text and strict", typeof(Examples.InputMenuExample)),
             new("Groups, icons and counts", typeof(Examples.InputMenuGroupsExample), "The same Count, Icon and Group of the items as the other menus; a heading disappears with its last suggestion while you type.")], [typeof(BxInputMenu)]),
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
