namespace Klyroo.UI.Maui;

/// <summary>
/// The mention menu: opens when "@" is typed and puts the label of the chosen entry in the text, <c>@Ada Lovelace</c>, followed by a space.
/// Markdown has no mention, so the text keeps the label only: <see cref="Mentioned"/> hands the entry, with its id, to the app.
/// </summary>
public sealed class KlyEditorMentionMenu : KlyEditorMenuBase
{
    /// <summary>Identifies <see cref="Items"/>.</summary>
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IReadOnlyList<EditorMentionItem>), typeof(KlyEditorMentionMenu), (IReadOnlyList<EditorMentionItem>)[], propertyChanged: (b, _, _) => ((KlyEditorMenuBase)b).Refresh());

    /// <summary>Raised when the user chooses an entry, after its label went into the text.</summary>
    public event EventHandler<EditorMentionItem>? Mentioned;

    /// <summary>Who or what can be mentioned.</summary>
    public IReadOnlyList<EditorMentionItem> Items { get => (IReadOnlyList<EditorMentionItem>)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }

    internal override string DefaultChar => "@";

    internal override string DefaultLabel => "Mentions";

    internal override string Kind => "mention";

    internal override IEnumerable<EditorMenuOption> BuildOptions() =>
        Items.Select(i => new EditorMenuOption(i.Id, i.Label, i.Description, Disabled: i.Disabled, Prefix: "@"));

    internal override void Accepted(EditorMenuOption option)
    {
        if (Items.FirstOrDefault(i => i.Id == option.Value) is { } item) Mentioned?.Invoke(this, item);
    }
}
