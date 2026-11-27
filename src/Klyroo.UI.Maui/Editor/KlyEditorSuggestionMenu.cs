namespace Klyroo.UI.Maui;

/// <summary>
/// The slash menu: opens when "/" is typed (after a space or at the start of a line) and applies a block or a format to the line, in place of the typed text.
/// </summary>
public sealed class KlyEditorSuggestionMenu : KlyEditorMenuBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<EditorSuggestionItem>), typeof(KlyEditorSuggestionMenu), null, propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Refresh());

    /// <summary>Entries of the menu. Defaults to <see cref="EditorSuggestionItem.Defaults"/>.</summary>
    public IReadOnlyList<EditorSuggestionItem>? Items { get => (IReadOnlyList<EditorSuggestionItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    internal override string DefaultChar => "/";

    internal override string DefaultLabel => "Commands";

    internal override string Kind => "suggestion";

    internal override IEnumerable<EditorMenuOption> BuildOptions() =>
        (Items ?? EditorSuggestionItem.Defaults).Select(i => new EditorMenuOption(
            i.Action.ToString(), i.Label, i.Description, i.Keywords, i.Action, i.Level, i.Disabled,
            IconName: EditorActionInfo.IconName(i.Action, i.Level)));
}
