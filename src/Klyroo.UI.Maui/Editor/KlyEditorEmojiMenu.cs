namespace Klyroo.UI.Maui;

/// <summary>The emoji menu: opens when ":" is typed followed by at least one character, and puts the emoji in the text in place of what was typed.</summary>
public sealed class KlyEditorEmojiMenu : KlyEditorMenuBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<EditorEmojiItem>), typeof(KlyEditorEmojiMenu), null, propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Refresh());

    /// <summary>Emoji of the menu. Defaults to <see cref="EditorEmojiItem.Defaults"/>.</summary>
    public IReadOnlyList<EditorEmojiItem>? Items { get => (IReadOnlyList<EditorEmojiItem>?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    internal override string DefaultChar => ":";

    internal override string DefaultLabel => "Emoji";

    internal override string Kind => "emoji";

    internal override int MinQuery => 1;

    internal override IEnumerable<EditorMenuOption> BuildOptions() =>
        (Items ?? EditorEmojiItem.Defaults).Select(i => new EditorMenuOption(i.Emoji, i.Name, null, i.Keywords, Prefix: i.Emoji));
}
