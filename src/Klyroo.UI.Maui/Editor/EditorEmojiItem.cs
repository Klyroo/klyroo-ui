namespace Klyroo.UI.Maui;

/// <summary>An entry of <see cref="KlyEditorEmojiMenu"/>.</summary>
public sealed record EditorEmojiItem
{
    /// <summary>Creates an emoji entry.</summary>
    /// <param name="emoji">The emoji inserted into the text.</param>
    /// <param name="name">What the user types after the colon to find it.</param>
    public EditorEmojiItem(string emoji, string name)
    {
        Emoji = emoji;
        Name = name;
    }

    /// <summary>The emoji inserted into the text.</summary>
    public string Emoji { get; }

    /// <summary>What the user types after the colon to find it: <c>:smile</c>.</summary>
    public string Name { get; }

    /// <summary>Extra words the typed text is matched against.</summary>
    public string? Keywords { get; init; }

    /// <summary>A few common emoji: what the emoji menu offers by default.</summary>
    public static IReadOnlyList<EditorEmojiItem> Defaults { get; } =
    [
        new("😀", "grinning") { Keywords = "smile happy" }, new("😄", "smile") { Keywords = "happy joy" }, new("😂", "joy") { Keywords = "laugh lol" },
        new("🙂", "slight_smile"), new("😉", "wink"), new("😍", "heart_eyes") { Keywords = "love" }, new("🤔", "thinking") { Keywords = "hmm" },
        new("😎", "sunglasses") { Keywords = "cool" }, new("😢", "cry") { Keywords = "sad tear" }, new("😡", "angry") { Keywords = "mad" },
        new("👍", "thumbsup") { Keywords = "+1 yes ok" }, new("👎", "thumbsdown") { Keywords = "-1 no" }, new("👏", "clap") { Keywords = "applause bravo" },
        new("🙏", "pray") { Keywords = "thanks please" }, new("👋", "wave") { Keywords = "hello bye" }, new("💪", "muscle") { Keywords = "strong" },
        new("❤️", "heart") { Keywords = "love" }, new("🔥", "fire") { Keywords = "hot lit" }, new("⭐", "star"), new("✨", "sparkles") { Keywords = "magic new" },
        new("🎉", "tada") { Keywords = "party celebrate" }, new("🚀", "rocket") { Keywords = "launch ship" }, new("💡", "bulb") { Keywords = "idea" },
        new("✅", "white_check_mark") { Keywords = "done check" }, new("❌", "x") { Keywords = "cross no" }, new("⚠️", "warning") { Keywords = "careful" },
        new("🐛", "bug"), new("📌", "pushpin") { Keywords = "pin" }, new("📎", "paperclip") { Keywords = "attach" }, new("☕", "coffee"),
    ];
}
