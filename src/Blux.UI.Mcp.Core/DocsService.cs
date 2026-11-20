using System.Text;
using System.Text.RegularExpressions;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp;

/// <summary>A search result: the item and how well it matched (higher is better).</summary>
public sealed record SearchHit<T>(T Item, int Score);

/// <summary>A searchable passage of the documentation: a guide section or a component page.</summary>
public sealed record DocPassage(string Path, string Title, string? Heading, string Kind, string Text);

/// <summary>Lookup and search over the docs bundle. Everything the MCP tools, prompts and resources answer comes from here.</summary>
public sealed partial class DocsService
{
    /// <summary>
    /// Other names people use for a component, or the intent that leads to it. Targets are slugs of the catalog
    /// (a test keeps them valid), listed in the order they should rank.
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string[]> Aliases = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["dialog"] = ["modal"], ["popup"] = ["modal", "popover"], ["lightbox"] = ["modal"], ["confirm"] = ["modal"], ["confirmation"] = ["modal"],
        ["sheet"] = ["slideover", "drawer"], ["offcanvas"] = ["slideover"], ["sidepanel"] = ["slideover"], ["bottomsheet"] = ["drawer"],
        ["snackbar"] = ["toast"], ["notification"] = ["toast"], ["notifications"] = ["toast"], ["flash"] = ["toast", "alert"],
        ["callout"] = ["alert"], ["message"] = ["alert", "chat-message"], ["announcement"] = ["banner"],
        ["dropdown"] = ["dropdown-menu", "select"], ["menu"] = ["dropdown-menu", "navigation-menu", "context-menu"], ["rightclick"] = ["context-menu"],
        ["combobox"] = ["select-menu", "input-menu"], ["autocomplete"] = ["input-menu", "select-menu"], ["typeahead"] = ["input-menu"],
        ["multiselect"] = ["select-menu"], ["picker"] = ["select-menu", "color-picker", "input-date"],
        ["datepicker"] = ["input-date", "calendar"], ["date"] = ["input-date", "calendar"], ["timepicker"] = ["input-time"], ["time"] = ["input-time"],
        ["navbar"] = ["header", "navigation-menu"], ["nav"] = ["navigation-menu", "header"], ["topbar"] = ["header", "dashboard-navbar"],
        ["navigation"] = ["navigation-menu", "tabs", "breadcrumb"], ["sidenav"] = ["sidebar", "dashboard-sidebar"],
        ["breadcrumbs"] = ["breadcrumb"], ["wizard"] = ["stepper"], ["steps"] = ["stepper"], ["progressbar"] = ["progress"],
        ["rating"] = ["input-rating"], ["stars"] = ["input-rating"], ["otp"] = ["pin-input"], ["pin"] = ["pin-input"], ["verification"] = ["pin-input"],
        ["upload"] = ["file-upload"], ["dropzone"] = ["file-upload"], ["file"] = ["file-upload"],
        ["spinner"] = ["progress", "skeleton"], ["loader"] = ["skeleton", "progress"], ["loading"] = ["skeleton", "progress"], ["placeholder"] = ["skeleton"],
        ["tag"] = ["input-tags", "badge"], ["tags"] = ["input-tags"], ["pill"] = ["badge"], ["label"] = ["badge"], ["status"] = ["badge", "chip"],
        ["datatable"] = ["table"], ["grid"] = ["table", "page-grid"], ["list"] = ["listbox", "page-list"], ["listing"] = ["table", "listbox"],
        ["faq"] = ["accordion"], ["collapse"] = ["collapsible", "accordion"], ["expand"] = ["collapsible", "accordion"],
        ["slideshow"] = ["carousel"], ["gallery"] = ["carousel"], ["slider"] = ["slider", "carousel"], ["range"] = ["slider"],
        ["spotlight"] = ["command-palette"], ["command"] = ["command-palette"], ["cmdk"] = ["command-palette"],
        ["wysiwyg"] = ["editor"], ["richtext"] = ["editor"], ["texteditor"] = ["editor"], ["markdown"] = ["editor"], ["mention"] = ["editor-mention-menu"], ["emoji"] = ["editor-emoji-menu"],
        ["bubble"] = ["chat-message"], ["conversation"] = ["chat-messages", "chat-prompt"], ["chatbot"] = ["chat-messages", "chat-prompt"], ["llm"] = ["chat-messages", "chat-reasoning"], ["thinking"] = ["chat-reasoning"],
        ["pricing"] = ["pricing-plans", "pricing-table"], ["plans"] = ["pricing-plans"], ["hero"] = ["page-hero"], ["landing"] = ["page-hero", "page-section"], ["cta"] = ["page-cta"],
        ["features"] = ["page-feature", "page-grid"], ["logos"] = ["page-logos"], ["blog"] = ["blog-posts", "blog-post"], ["changelog"] = ["changelog-versions", "changelog-version"],
        ["login"] = ["auth-form"], ["signin"] = ["auth-form"], ["signup"] = ["auth-form"], ["register"] = ["auth-form"], ["auth"] = ["auth-form"], ["password"] = ["auth-form", "input"],
        ["resizable"] = ["splitter", "dashboard-resize-handle"], ["resize"] = ["splitter"], ["panes"] = ["splitter"],
        ["darkmode"] = ["color-mode-button", "color-mode-switch"], ["dark"] = ["color-mode-button", "color-mode-switch"], ["colormode"] = ["color-mode-button", "color-mode-select"],
        ["themeswitch"] = ["color-mode-switch"], ["language"] = ["locale-select"], ["locale"] = ["locale-select"], ["i18n"] = ["locale-select"],
        ["hint"] = ["tooltip"], ["tip"] = ["tooltip"], ["hover"] = ["tooltip", "popover"],
        ["pager"] = ["pagination"], ["paging"] = ["pagination"], ["checkboxes"] = ["checkbox-group"], ["radio"] = ["radio-group"], ["radios"] = ["radio-group"],
        ["toggle"] = ["switch", "toggle", "toggle-group"], ["onoff"] = ["switch"], ["segmented"] = ["toggle-group", "tabs"], ["buttons"] = ["button", "toggle-group"],
        ["textfield"] = ["input"], ["textbox"] = ["input"], ["text"] = ["input", "textarea"], ["number"] = ["input-number"], ["numeric"] = ["input-number"], ["multiline"] = ["textarea"],
        ["validation"] = ["form", "form-field"], ["forms"] = ["form", "form-field"], ["field"] = ["form-field"],
        ["tabbed"] = ["tabs"], ["tabbar"] = ["tabs"], ["treeview"] = ["tree"], ["filetree"] = ["tree"], ["ticker"] = ["marquee"],
        ["scrollbar"] = ["scroll-area"], ["scrollable"] = ["scroll-area"], ["avatars"] = ["avatar-group"], ["profile"] = ["user", "avatar"],
        ["divider"] = ["separator"], ["line"] = ["separator"], ["shortcut"] = ["kbd"], ["keyboard"] = ["kbd"], ["key"] = ["kbd"],
        ["graph"] = ["chart"], ["charts"] = ["chart"], ["plot"] = ["chart"], ["analytics"] = ["chart", "dashboard-group"], ["dashboard"] = ["dashboard-group", "dashboard-panel", "dashboard-sidebar"],
        ["empty"] = ["empty"], ["zero"] = ["empty"], ["nodata"] = ["empty"], ["error"] = ["error", "alert"], ["404"] = ["error"],
        ["container"] = ["container"], ["wrapper"] = ["container"], ["layout"] = ["app", "main", "page"], ["footer"] = ["footer", "footer-columns"],
        ["toc"] = ["content-toc"], ["tableofcontents"] = ["content-toc"], ["docs"] = ["content-navigation", "content-search", "content-toc", "content-surround"],
        ["search"] = ["content-search", "command-palette", "dashboard-search"], ["searchbox"] = ["content-search-button", "input"],
        ["color"] = ["color-picker"], ["colour"] = ["color-picker"], ["eyedropper"] = ["color-picker"],
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "a", "an", "the", "to", "for", "of", "in", "on", "with", "and", "or", "i", "need", "want", "how", "do", "can", "use", "using", "component", "components", "blazor", "bx", "blux", "ui",
    };

    private readonly Dictionary<string, BundleComponent> _byKey = new(StringComparer.Ordinal);
    private readonly Lazy<IReadOnlyList<DocPassage>> _passages;

    /// <param name="bundle">What to serve.</param>
    /// <param name="siteUrl">Base address of the docs site, put in front of the page paths in answers; null keeps them relative (<c>/components/button</c>).</param>
    public DocsService(DocsBundle bundle, string? siteUrl = null)
    {
        Bundle = bundle;
        SiteUrl = siteUrl;
        foreach (var component in bundle.Components)
        {
            _byKey.TryAdd(Normalize(component.Slug), component);
            _byKey.TryAdd(Normalize(component.Name), component);
        }
        // The types of a page (BxColumn on the Table page) lead to the page too, after every exact name.
        foreach (var component in bundle.Components)
        {
            foreach (var api in component.Api) _byKey.TryAdd(Normalize(TypeKey(api.Name)), component);
        }
        _passages = new(BuildPassages);
    }

    public DocsBundle Bundle { get; }

    public string? SiteUrl { get; }

    public IReadOnlyList<BundleComponent> Components => Bundle.Components;

    /// <summary><c>BxButton</c>, <c>button</c>, <c>Button</c> and <c>dropdown_menu</c> all become a key of letters and digits without the <c>Bx</c> prefix.</summary>
    public static string Normalize(string name)
    {
        var key = new string([.. name.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
        return key.Length > 2 && key.StartsWith("bx", StringComparison.Ordinal) ? key[2..] : key;
    }

    private static string TypeKey(string typeName)
    {
        var generic = typeName.IndexOf('<');
        return generic < 0 ? typeName : typeName[..generic];
    }

    /// <summary>The component a name designates (slug, name or the name of one of its types), or null.</summary>
    public BundleComponent? FindComponent(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : _byKey.GetValueOrDefault(Normalize(name));

    /// <summary>Components with a name close to <paramref name="name"/>, best first: for the "did you mean" of an unknown name.</summary>
    public IReadOnlyList<BundleComponent> CloseComponents(string name, int count = 5)
    {
        var key = Normalize(name);
        var scored = Bundle.Components
            .Select(c => (Component: c, Score: Closeness(key, Normalize(c.Slug))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Component.Name, StringComparer.Ordinal)
            .Select(x => x.Component)
            .ToList();
        // An intent ("modal window") finds nothing by spelling: fall back to the search.
        if (scored.Count < count)
        {
            foreach (var hit in SearchComponents(name, null, count))
            {
                if (!scored.Contains(hit.Item)) scored.Add(hit.Item);
            }
        }
        return scored.Take(count).ToList();
    }

    private static int Closeness(string key, string candidate)
    {
        if (key.Length == 0) return 0;
        if (candidate.StartsWith(key, StringComparison.Ordinal)) return 900 - (candidate.Length - key.Length);
        if (candidate.Contains(key, StringComparison.Ordinal)) return 700 - (candidate.Length - key.Length);
        if (key.Contains(candidate, StringComparison.Ordinal) && candidate.Length >= 4) return 500;
        var distance = Levenshtein(key, candidate);
        var allowed = Math.Max(2, key.Length / 3);
        return distance <= allowed ? 400 - distance * 50 : 0;
    }

    /// <summary>
    /// Ranks components for a free-text query (a name, a category, an alternate name or an intent). An exact name comes first,
    /// then names that start with the query, alternate names, names that contain it, then matches in descriptions.
    /// </summary>
    public IReadOnlyList<SearchHit<BundleComponent>> SearchComponents(string? query, string? group = null, int limit = 10)
    {
        var candidates = Bundle.Components
            .Where(c => string.IsNullOrWhiteSpace(group) || string.Equals(c.Group, group.Trim(), StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(query))
        {
            return [.. candidates.Select(c => new SearchHit<BundleComponent>(c, 0))];
        }

        var whole = Normalize(query);
        var tokens = Tokens(query);
        var hits = new List<SearchHit<BundleComponent>>();
        foreach (var component in candidates)
        {
            var score = Score(component, whole, tokens);
            if (score > 0) hits.Add(new(component, score));
        }
        return [.. hits.OrderByDescending(h => h.Score).ThenBy(h => h.Item.Name, StringComparer.Ordinal).Take(Math.Max(1, limit))];
    }

    private int Score(BundleComponent component, string whole, IReadOnlyList<string> tokens)
    {
        var slugKey = Normalize(component.Slug);
        var nameKey = Normalize(component.Name);
        var score = 0;

        if (whole == nameKey || whole == slugKey) return 10_000;
        if (component.Api.Any(a => Normalize(TypeKey(a.Name)) == whole)) return 9_000;
        if (whole.Length >= 2 && (nameKey.StartsWith(whole, StringComparison.Ordinal))) score = Math.Max(score, 8_000 - (nameKey.Length - whole.Length));

        // Alternate names and intents: the whole query ranks highest, then runs of words ("rich text"), then a single word,
        // which is the weakest because words like "text" or "list" appear in many intents.
        foreach (var (key, weight) in AliasKeys(whole, tokens))
        {
            if (Aliases.TryGetValue(key, out var targets))
            {
                var rank = Array.IndexOf(targets, component.Slug);
                if (rank >= 0) score = Math.Max(score, weight - rank * 100);
            }
        }

        if (whole.Length >= 3 && nameKey.Contains(whole, StringComparison.Ordinal)) score = Math.Max(score, 6_000 - (nameKey.Length - whole.Length));

        if (tokens.Count == 0) return score;
        var words = Words(component.Name + " " + component.Slug.Replace('-', ' '));
        var description = Words(component.Description);
        var examples = Words(string.Join(' ', component.Examples.Select(e => e.Title + " " + e.Description)));
        var parameters = component.Api.SelectMany(a => a.Parameters.Concat(a.Events).Concat(a.Slots)).Select(p => Normalize(p.Name)).ToHashSet(StringComparer.Ordinal);
        var tokenScore = 0;
        var matched = 0;
        foreach (var token in tokens)
        {
            var best = 0;
            if (words.Contains(token)) best = 500;
            else if (token.Length >= 3 && words.Any(w => w.StartsWith(token, StringComparison.Ordinal))) best = 300;
            else if (token.Length >= 4 && Normalize(component.Name).Contains(token, StringComparison.Ordinal)) best = 250;
            else if (description.Contains(token)) best = 120;
            else if (token.Length >= 4 && description.Any(w => w.StartsWith(token, StringComparison.Ordinal))) best = 80;
            else if (examples.Contains(token)) best = 60;
            else if (parameters.Contains(token)) best = 40;
            else if (string.Equals(component.Group, token, StringComparison.OrdinalIgnoreCase)) best = 30;
            else if (token.Length >= 5 && words.Any(w => w.Length >= 5 && Levenshtein(token, w) <= 1)) best = 150;
            if (best > 0) matched++;
            tokenScore += best;
        }
        if (matched == 0) return score;
        // A query whose words all matched beats one where only some did.
        tokenScore += matched == tokens.Count ? 200 : 0;
        return Math.Max(score, Math.Min(tokenScore, 5_000));
    }

    private static IEnumerable<(string Key, int Weight)> AliasKeys(string whole, IReadOnlyList<string> tokens)
    {
        yield return (whole, 7_000);
        for (var length = tokens.Count - 1; length >= 2; length--)
        {
            for (var start = 0; start + length <= tokens.Count; start++)
            {
                yield return (string.Concat(tokens.Skip(start).Take(length)), 6_800);
            }
        }
        foreach (var token in tokens) yield return (token, 3_000);
    }

    private static IReadOnlyList<string> Tokens(string query) =>
        [.. WordSplit().Split(query.ToLowerInvariant()).Where(t => t.Length > 0 && !StopWords.Contains(t)).Select(Stem).Distinct()];

    private static HashSet<string> Words(string text) =>
        [.. WordSplit().Split(CamelBoundary().Replace(text, "$1 $2").ToLowerInvariant()).Where(t => t.Length > 0).Select(Stem)];

    /// <summary>Plural and gerund endings off, so "tabs" finds Tabs and "dropdowns" finds a dropdown.</summary>
    private static string Stem(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal)) return word[..^3] + "y";
        if (word.Length > 3 && word.EndsWith("s", StringComparison.Ordinal)
            && !word.EndsWith("ss", StringComparison.Ordinal) && !word.EndsWith("us", StringComparison.Ordinal) && !word.EndsWith("is", StringComparison.Ordinal))
        {
            return word[..^1];
        }
        return word;
    }

    internal static int Levenshtein(string a, string b)
    {
        if (a == b) return 0;
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) previous[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
            }
            (previous, current) = (current, previous);
        }
        return previous[b.Length];
    }

    // ---- Examples ----

    public IEnumerable<(BundleComponent Component, BundleExample Example)> AllExamples =>
        Bundle.Components.SelectMany(c => c.Examples.Select(e => (Component: c, Example: e)));

    /// <summary>An example by its id (the name of its Razor component), with or without the <c>Example</c> suffix.</summary>
    public (BundleComponent Component, BundleExample Example)? FindExample(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var key = Normalize(name);
        foreach (var item in AllExamples)
        {
            var id = Normalize(item.Example.Id);
            if (id == key || id == key + "example" || (id.EndsWith("example", StringComparison.Ordinal) && id[..^"example".Length] == key)) return item;
        }
        return null;
    }

    public IReadOnlyList<(BundleComponent Component, BundleExample Example)> CloseExamples(string name, int count = 5)
    {
        var key = Normalize(name);
        return [.. AllExamples
            .Select(x => (x.Component, x.Example, Score: Closeness(key, Normalize(x.Example.Id))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Example.Id, StringComparer.Ordinal)
            .Take(count).Select(x => (x.Component, x.Example))];
    }

    // ---- Icons ----

    public IReadOnlyList<string> SearchIcons(string? query, int limit)
    {
        if (string.IsNullOrWhiteSpace(query)) return [.. Bundle.Icons.Take(limit)];
        var whole = query.Trim().ToLowerInvariant().Replace(' ', '-');
        var tokens = Tokens(query);
        return [.. Bundle.Icons
            .Select(icon => (Icon: icon, Score: IconScore(icon, whole, tokens)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score).ThenBy(x => x.Icon, StringComparer.Ordinal)
            .Take(limit).Select(x => x.Icon)];
    }

    private static int IconScore(string icon, string whole, IReadOnlyList<string> tokens)
    {
        if (icon == whole) return 1000;
        if (icon.StartsWith(whole, StringComparison.Ordinal)) return 800;
        if (icon.Contains(whole, StringComparison.Ordinal)) return 600;
        var parts = icon.Split('-').Select(Stem).ToHashSet(StringComparer.Ordinal);
        var matched = tokens.Count(t => parts.Contains(t) || icon.Contains(t, StringComparison.Ordinal));
        return matched == 0 ? 0 : 100 * matched;
    }

    // ---- Guides and documentation search ----

    /// <summary>A guide by path: <c>/docs/theming</c>, <c>docs/theming</c> or <c>theming</c>.</summary>
    public BundleGuide? FindGuide(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        var key = path.Trim().Trim('/').ToLowerInvariant();
        if (key.StartsWith("docs/", StringComparison.Ordinal)) key = key["docs/".Length..];
        else if (key == "docs") key = "";
        foreach (var guide in Bundle.Guides)
        {
            var guideKey = guide.Path.Trim('/').ToLowerInvariant();
            guideKey = guideKey.StartsWith("docs/", StringComparison.Ordinal) ? guideKey["docs/".Length..] : guideKey == "docs" ? "" : guideKey;
            if (guideKey == key) return guide;
        }
        // "introduction" is the page at /docs.
        return Bundle.Guides.FirstOrDefault(g => Normalize(g.Title) == Normalize(path));
    }

    public IReadOnlyList<DocPassage> Passages => _passages.Value;

    private IReadOnlyList<DocPassage> BuildPassages()
    {
        var list = new List<DocPassage>();
        foreach (var guide in Bundle.Guides)
        {
            foreach (var (heading, text) in MarkdownSections.Split(guide.Markdown))
            {
                var anchor = heading.Length == 0 ? "" : "#" + MarkdownSections.Anchor(heading);
                list.Add(new DocPassage(guide.Path + anchor, guide.Title, heading.Length == 0 ? null : heading, "guide", text));
            }
        }
        foreach (var component in Bundle.Components)
        {
            var text = new StringBuilder(component.Description);
            foreach (var example in component.Examples) text.Append($" {example.Title}. {example.Description}");
            foreach (var api in component.Api)
            {
                text.Append($" {api.Summary}");
                foreach (var parameter in api.Parameters.Concat(api.Events)) text.Append($" {parameter.Name}: {parameter.Description}");
            }
            list.Add(new DocPassage("/components/" + component.Slug, component.Name, null, "component", text.ToString()));
        }
        return list;
    }

    /// <summary>Searches the guides and the component pages. <paramref name="section"/> is <c>guides</c>, <c>components</c> or empty for both.</summary>
    public IReadOnlyList<SearchHit<DocPassage>> SearchDocumentation(string query, string? section = null, int limit = 8)
    {
        var tokens = Tokens(query);
        if (tokens.Count == 0) return [];
        var phrase = query.Trim().ToLowerInvariant();
        var hits = new List<SearchHit<DocPassage>>();
        foreach (var passage in Passages)
        {
            if (!SectionMatches(passage, section)) continue;
            var title = Words(passage.Title);
            var heading = passage.Heading is null ? [] : Words(passage.Heading);
            var body = Words(passage.Text);
            var score = 0;
            var matched = 0;
            foreach (var token in tokens)
            {
                var best = 0;
                if (title.Contains(token)) best = 60;
                if (heading.Contains(token)) best = Math.Max(best, 50);
                if (body.Contains(token)) best = Math.Max(best, 15);
                if (best > 0) matched++;
                score += best;
            }
            if (matched == 0) continue;
            if (matched == tokens.Count) score += 40;
            if (phrase.Length > 3 && passage.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase)) score += 60;
            hits.Add(new(passage, score));
        }
        return [.. hits.OrderByDescending(h => h.Score).ThenBy(h => h.Item.Path, StringComparer.Ordinal).Take(Math.Max(1, limit))];
    }

    private static bool SectionMatches(DocPassage passage, string? section) =>
        string.IsNullOrWhiteSpace(section) || section.Trim().ToLowerInvariant() switch
        {
            "all" => true,
            "guides" or "guide" => passage.Kind == "guide",
            "components" or "component" => passage.Kind == "component",
            _ => false,
        };

    /// <summary>The words around the first match of the query, for a search result.</summary>
    public static string Snippet(string text, string query, int length = 220)
    {
        var flat = Whitespace().Replace(text, " ").Trim();
        var index = -1;
        foreach (var token in WordSplit().Split(query.ToLowerInvariant()).Where(t => t.Length > 2 && !StopWords.Contains(t)))
        {
            index = flat.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index >= 0) break;
        }
        var start = Math.Max(0, index < 0 ? 0 : index - length / 4);
        var slice = flat[start..Math.Min(flat.Length, start + length)];
        return (start > 0 ? "..." : "") + slice + (start + length < flat.Length ? "..." : "");
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex WordSplit();

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex CamelBoundary();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
