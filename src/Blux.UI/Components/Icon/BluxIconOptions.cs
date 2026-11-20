using System.Collections.Concurrent;
using Blux.UI.Internal;

namespace Blux.UI;

/// <summary>
/// The icons components draw, as Nuxt UI's <c>ui.icons</c>: every property is an icon name (<c>i-lucide-x</c>, <c>ph:x</c>, <c>mdi:close</c>,
/// or a bare Lucide name) with a Lucide default. Set them in <c>AddBluxUi</c> to replace an icon everywhere components use it:
/// <c>o.Icons.Close = "ph:x"</c>. A name that is not one of the Lucide icons built into the library is bundled at build time when the
/// assignment is a literal (see the Icons guide); a name written another way goes in a <c>BluxIcon</c> item.
/// The values become read-only the first time a component draws an icon.
/// </summary>
public sealed class BluxIconOptions
{
    private enum Key
    {
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        ArrowUp,
        Caution,
        Check,
        ChevronDoubleLeft,
        ChevronDoubleRight,
        ChevronDown,
        ChevronLeft,
        ChevronRight,
        ChevronUp,
        Close,
        Copy,
        CopyCheck,
        Dark,
        Drag,
        Edit,
        Ellipsis,
        Error,
        External,
        Eye,
        EyeOff,
        File,
        Folder,
        FolderOpen,
        Hash,
        Info,
        Light,
        Loading,
        Menu,
        Minus,
        PanelClose,
        PanelOpen,
        Plus,
        Reload,
        Search,
        Stop,
        Star,
        Success,
        System,
        Tip,
        Trash,
        Upload,
        Warning,
        ChevronUpDown,
        Calendar,
        Play,
        Pause,
    }

    private static readonly string[] Defaults =
    {
        "i-lucide-arrow-down",
        "i-lucide-arrow-left",
        "i-lucide-arrow-right",
        "i-lucide-arrow-up",
        "i-lucide-circle-alert",
        "i-lucide-check",
        "i-lucide-chevrons-left",
        "i-lucide-chevrons-right",
        "i-lucide-chevron-down",
        "i-lucide-chevron-left",
        "i-lucide-chevron-right",
        "i-lucide-chevron-up",
        "i-lucide-x",
        "i-lucide-copy",
        "i-lucide-copy-check",
        "i-lucide-moon",
        "i-lucide-grip-vertical",
        "i-lucide-pencil",
        "i-lucide-ellipsis",
        "i-lucide-circle-x",
        "i-lucide-arrow-up-right",
        "i-lucide-eye",
        "i-lucide-eye-off",
        "i-lucide-file",
        "i-lucide-folder",
        "i-lucide-folder-open",
        "i-lucide-hash",
        "i-lucide-info",
        "i-lucide-sun",
        "i-lucide-loader-circle",
        "i-lucide-menu",
        "i-lucide-minus",
        "i-lucide-panel-left-close",
        "i-lucide-panel-left-open",
        "i-lucide-plus",
        "i-lucide-rotate-ccw",
        "i-lucide-search",
        "i-lucide-square",
        "i-lucide-star",
        "i-lucide-circle-check",
        "i-lucide-monitor",
        "i-lucide-lightbulb",
        "i-lucide-trash",
        "i-lucide-upload",
        "i-lucide-triangle-alert",
        "i-lucide-chevrons-up-down",
        "i-lucide-calendar",
        "i-lucide-play",
        "i-lucide-pause",
    };

    private readonly string[] _values = (string[])Defaults.Clone();
    private readonly ConcurrentDictionary<string, IconBody> _registered = new(StringComparer.Ordinal);
    private volatile bool _frozen;

    private string Get(Key key) => _values[(int)key];

    private void Set(Key key, string value)
    {
        ThrowIfFrozen();
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("An icon name cannot be empty.", nameof(value));
        _values[(int)key] = value.Trim();
    }

    /// <summary>Arrow pointing down. Default <c>i-lucide-arrow-down</c>.</summary>
    public string ArrowDown { get => Get(Key.ArrowDown); set => Set(Key.ArrowDown, value); }

    /// <summary>Arrow pointing left (previous page of a content surround). Default <c>i-lucide-arrow-left</c>.</summary>
    public string ArrowLeft { get => Get(Key.ArrowLeft); set => Set(Key.ArrowLeft, value); }

    /// <summary>Arrow pointing right. Default <c>i-lucide-arrow-right</c>.</summary>
    public string ArrowRight { get => Get(Key.ArrowRight); set => Set(Key.ArrowRight, value); }

    /// <summary>Arrow pointing up (sort ascending, send a chat message). Default <c>i-lucide-arrow-up</c>.</summary>
    public string ArrowUp { get => Get(Key.ArrowUp); set => Set(Key.ArrowUp, value); }

    /// <summary>Something needs attention (chat attachment error). Default <c>i-lucide-circle-alert</c>.</summary>
    public string Caution { get => Get(Key.Caution); set => Set(Key.Caution, value); }

    /// <summary>A check mark: checkboxes, selected items, steps. Default <c>i-lucide-check</c>.</summary>
    public string Check { get => Get(Key.Check); set => Set(Key.Check, value); }

    /// <summary>First page. Default <c>i-lucide-chevrons-left</c>.</summary>
    public string ChevronDoubleLeft { get => Get(Key.ChevronDoubleLeft); set => Set(Key.ChevronDoubleLeft, value); }

    /// <summary>Last page. Default <c>i-lucide-chevrons-right</c>.</summary>
    public string ChevronDoubleRight { get => Get(Key.ChevronDoubleRight); set => Set(Key.ChevronDoubleRight, value); }

    /// <summary>Disclosure chevrons, carousel arrows and select triggers. Default <c>i-lucide-chevron-down</c>.</summary>
    public string ChevronDown { get => Get(Key.ChevronDown); set => Set(Key.ChevronDown, value); }

    /// <summary>Previous page, back. Default <c>i-lucide-chevron-left</c>.</summary>
    public string ChevronLeft { get => Get(Key.ChevronLeft); set => Set(Key.ChevronLeft, value); }

    /// <summary>Next page, breadcrumb separator, tree chevron. Default <c>i-lucide-chevron-right</c>.</summary>
    public string ChevronRight { get => Get(Key.ChevronRight); set => Set(Key.ChevronRight, value); }

    /// <summary>Chevron pointing up. Default <c>i-lucide-chevron-up</c>.</summary>
    public string ChevronUp { get => Get(Key.ChevronUp); set => Set(Key.ChevronUp, value); }

    /// <summary>Dismiss buttons of alerts, banners, modals, drawers and slideovers, tag removal. Default <c>i-lucide-x</c>.</summary>
    public string Close { get => Get(Key.Close); set => Set(Key.Close, value); }

    /// <summary>Copy to the clipboard. Default <c>i-lucide-copy</c>.</summary>
    public string Copy { get => Get(Key.Copy); set => Set(Key.Copy, value); }

    /// <summary>Copied. Default <c>i-lucide-copy-check</c>.</summary>
    public string CopyCheck { get => Get(Key.CopyCheck); set => Set(Key.CopyCheck, value); }

    /// <summary>Dark color mode. Default <c>i-lucide-moon</c>.</summary>
    public string Dark { get => Get(Key.Dark); set => Set(Key.Dark, value); }

    /// <summary>Drag handle. Default <c>i-lucide-grip-vertical</c>.</summary>
    public string Drag { get => Get(Key.Drag); set => Set(Key.Drag, value); }

    /// <summary>Edit. Default <c>i-lucide-pencil</c>.</summary>
    public string Edit { get => Get(Key.Edit); set => Set(Key.Edit, value); }

    /// <summary>More actions. Default <c>i-lucide-ellipsis</c>.</summary>
    public string Ellipsis { get => Get(Key.Ellipsis); set => Set(Key.Ellipsis, value); }

    /// <summary>Error. Default <c>i-lucide-circle-x</c>.</summary>
    public string Error { get => Get(Key.Error); set => Set(Key.Error, value); }

    /// <summary>Link to another site. Default <c>i-lucide-arrow-up-right</c>.</summary>
    public string External { get => Get(Key.External); set => Set(Key.External, value); }

    /// <summary>Show a password. Default <c>i-lucide-eye</c>.</summary>
    public string Eye { get => Get(Key.Eye); set => Set(Key.Eye, value); }

    /// <summary>Hide a password. Default <c>i-lucide-eye-off</c>.</summary>
    public string EyeOff { get => Get(Key.EyeOff); set => Set(Key.EyeOff, value); }

    /// <summary>A file. Default <c>i-lucide-file</c>.</summary>
    public string File { get => Get(Key.File); set => Set(Key.File, value); }

    /// <summary>A closed folder. Default <c>i-lucide-folder</c>.</summary>
    public string Folder { get => Get(Key.Folder); set => Set(Key.Folder, value); }

    /// <summary>An open folder. Default <c>i-lucide-folder-open</c>.</summary>
    public string FolderOpen { get => Get(Key.FolderOpen); set => Set(Key.FolderOpen, value); }

    /// <summary>Hash, anchor. Default <c>i-lucide-hash</c>.</summary>
    public string Hash { get => Get(Key.Hash); set => Set(Key.Hash, value); }

    /// <summary>Information. Default <c>i-lucide-info</c>.</summary>
    public string Info { get => Get(Key.Info); set => Set(Key.Info, value); }

    /// <summary>Light color mode. Default <c>i-lucide-sun</c>.</summary>
    public string Light { get => Get(Key.Light); set => Set(Key.Light, value); }

    /// <summary>Spinner of loading buttons and busy states (spun by the component). Default <c>i-lucide-loader-circle</c>.</summary>
    public string Loading { get => Get(Key.Loading); set => Set(Key.Loading, value); }

    /// <summary>Opens a navigation menu. Default <c>i-lucide-menu</c>.</summary>
    public string Menu { get => Get(Key.Menu); set => Set(Key.Menu, value); }

    /// <summary>Decrement, not included. Default <c>i-lucide-minus</c>.</summary>
    public string Minus { get => Get(Key.Minus); set => Set(Key.Minus, value); }

    /// <summary>Collapse a side panel. Default <c>i-lucide-panel-left-close</c>.</summary>
    public string PanelClose { get => Get(Key.PanelClose); set => Set(Key.PanelClose, value); }

    /// <summary>Expand a side panel. Default <c>i-lucide-panel-left-open</c>.</summary>
    public string PanelOpen { get => Get(Key.PanelOpen); set => Set(Key.PanelOpen, value); }

    /// <summary>Increment, add. Default <c>i-lucide-plus</c>.</summary>
    public string Plus { get => Get(Key.Plus); set => Set(Key.Plus, value); }

    /// <summary>Retry, reload. Default <c>i-lucide-rotate-ccw</c>.</summary>
    public string Reload { get => Get(Key.Reload); set => Set(Key.Reload, value); }

    /// <summary>Search. Default <c>i-lucide-search</c>.</summary>
    public string Search { get => Get(Key.Search); set => Set(Key.Search, value); }

    /// <summary>Stop a running action. Default <c>i-lucide-square</c>.</summary>
    public string Stop { get => Get(Key.Stop); set => Set(Key.Stop, value); }

    /// <summary>A rating star. Default <c>i-lucide-star</c>.</summary>
    public string Star { get => Get(Key.Star); set => Set(Key.Star, value); }

    /// <summary>Success. Default <c>i-lucide-circle-check</c>.</summary>
    public string Success { get => Get(Key.Success); set => Set(Key.Success, value); }

    /// <summary>System color mode. Default <c>i-lucide-monitor</c>.</summary>
    public string System { get => Get(Key.System); set => Set(Key.System, value); }

    /// <summary>A tip. Default <c>i-lucide-lightbulb</c>.</summary>
    public string Tip { get => Get(Key.Tip); set => Set(Key.Tip, value); }

    /// <summary>Delete. Default <c>i-lucide-trash</c>.</summary>
    public string Trash { get => Get(Key.Trash); set => Set(Key.Trash, value); }

    /// <summary>Upload a file. Default <c>i-lucide-upload</c>.</summary>
    public string Upload { get => Get(Key.Upload); set => Set(Key.Upload, value); }

    /// <summary>Warning. Default <c>i-lucide-triangle-alert</c>.</summary>
    public string Warning { get => Get(Key.Warning); set => Set(Key.Warning, value); }

    /// <summary>Trigger of selects and sortable columns. Default <c>i-lucide-chevrons-up-down</c>.</summary>
    public string ChevronUpDown { get => Get(Key.ChevronUpDown); set => Set(Key.ChevronUpDown, value); }

    /// <summary>Opens the calendar of a date input. Default <c>i-lucide-calendar</c>.</summary>
    public string Calendar { get => Get(Key.Calendar); set => Set(Key.Calendar, value); }

    /// <summary>Resume a carousel autoplay. Default <c>i-lucide-play</c>.</summary>
    public string Play { get => Get(Key.Play); set => Set(Key.Play, value); }

    /// <summary>Pause a carousel autoplay. Default <c>i-lucide-pause</c>.</summary>
    public string Pause { get => Get(Key.Pause); set => Set(Key.Pause, value); }

    /// <summary>
    /// Registers an icon under a name, to use it like any other: <c>o.Icons.Register("custom:logo", "&lt;path d=\"...\" /&gt;", 24, 24)</c>.
    /// <paramref name="body"/> is the markup inside the <c>&lt;svg&gt;</c> element (what Iconify calls an icon's body): give the paths
    /// their own <c>fill</c> or <c>stroke</c> (<c>currentColor</c> follows the text color). A bare name replaces a built-in Lucide icon.
    /// It is markup written by the app, rendered as it is, so never register content from end users (<see cref="RegisterSvg"/> sanitizes its input, this method does not).
    /// </summary>
    /// <param name="name">The name, in any form (<c>custom:logo</c>, <c>i-custom-logo</c>, <c>logo</c>).</param>
    /// <param name="body">Markup of the icon.</param>
    /// <param name="width">Width of the drawing area.</param>
    /// <param name="height">Height of the drawing area.</param>
    public void Register(string name, string body, double width = 24, double height = 24)
    {
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width), "The icon needs a positive width and height.");
        Register(name, new IconBody(body ?? "", FormattableString.Invariant($"0 0 {width} {height}")));
    }

    /// <summary>
    /// Registers an icon from the text of an SVG file: its content becomes the body and its <c>viewBox</c> the drawing area. The file is sanitized like the icons
    /// the build bundles (scripts, handlers, images, external references and styles are removed), but it is still markup your app owns: never register content from end users.
    /// </summary>
    public void RegisterSvg(string name, string svg)
    {
        IconBody parsed;
        try
        {
            parsed = SvgImport.Parse(svg);
        }
        catch (FormatException e)
        {
            throw new ArgumentException("The SVG of \"" + name + "\" cannot be used: " + e.Message, nameof(svg));
        }
        Register(name, parsed);
    }

    private void Register(string name, IconBody body)
    {
        ThrowIfFrozen();
        if (string.IsNullOrWhiteSpace(body.Body)) throw new ArgumentException("An icon needs a body.", nameof(body));
        if (!IconNames.TryParse(name, BluxIconBundle.HasPrefix, out var icon, out _))
        {
            throw new ArgumentException($"\"{name}\" is not an icon name: use prefix:name (custom:logo), i-prefix-name or a bare lowercase name.", nameof(name));
        }
        _registered[icon.Key] = body;
        BluxIconBundle.AddPrefix(icon.Prefix);
    }

    internal bool TryGetRegistered(string key, out IconBody body) => _registered.TryGetValue(key, out body);

    internal void Freeze() => _frozen = true;

    private void ThrowIfFrozen()
    {
        if (_frozen)
        {
            throw new InvalidOperationException("BluxOptions.Icons cannot change once a component has drawn an icon. Configure it in AddBluxUi, before the app starts rendering.");
        }
    }
}
