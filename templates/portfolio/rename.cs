// Gives this template your own name, then deletes itself. Run it once, from the root of the repository:
//
//     dotnet run rename.cs MyApp
//
// It is a .NET 10 file-based app: no project, nothing to install. MyApp is one name made of letters and digits, starting with a letter. It becomes the
// name of the solution, the projects and the namespaces,
// and replaces every other occurrence of the template name, in file contents and in file and folder names. The case you type is kept in the names
// and namespaces (MyApp, myApp), the lowercase spelling uses lowercase.
//
// If it stops half way (a file is open in an editor, a folder is locked), it says which path failed and keeps its progress in .rename-state: fix the
// cause and run the same command again, it continues where it stopped.
using System.Text;
using System.Text.RegularExpressions;

const string Template = "Portfolio";
const string StateFile = ".rename-state";

var skippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".vs", "bin", "obj", "node_modules" };

// Names that would shadow a type or a namespace the projects use (System, Task, Microsoft, Blux...), collide with a folder or a namespace of the
// template itself (Web, Shared, Layout...), or are the name of the template.
var reserved = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    Template,
    // Namespaces and frameworks.
    "System", "Microsoft", "Blux", "Windows", "Android", "Xamarin", "Maui", "Mono", "Blazor", "AspNetCore", "Razor", "Tailwind", "Newtonsoft", "Foundation", "UIKit", "ObjCRuntime", "CoreFoundation", "Java", "Javax", "Kotlin",
    // Types of the framework the pages and hosts use unqualified.
    "Task", "Tasks", "Object", "String", "Boolean", "Byte", "SByte", "Char", "Decimal", "Double", "Single", "Float", "Int16", "Int32", "Int64", "UInt16", "UInt32", "UInt64", "IntPtr", "Void",
    "Action", "Func", "Type", "Exception", "Attribute", "Enum", "Guid", "DateTime", "TimeSpan", "Environment", "Console", "Math", "Convert", "File", "Path", "Directory", "Stream", "Thread", "Timer",
    "Tuple", "Uri", "Version", "Nullable", "Lazy", "Random", "Buffer", "Delegate", "Array", "List", "Dictionary", "Collections", "Linq", "Text", "Net", "Http", "Json", "Threading", "Diagnostics",
    "Application", "Window", "Color", "Colors", "Page", "ContentPage", "Preferences", "Platform", "Router", "Component", "ComponentBase", "Services", "Logging", "Configuration",
    // Folders, namespaces and files of the template.
    "App", "Program", "Web", "Layout", "Pages", "Components", "Sections", "Content", "Site", "SiteInfo", "Routes", "Styles", "Properties", "Home", "NotFound", "Error",
    "Project", "ProjectLink", "ProjectCard", "ProjectPage", "Article", "Library", "Resume", "Experience", "SkillGroup", "Talk", "Talks", "TalkKind", "TalkYear", "SocialLink", "ContactMessage",
    "About", "Contact", "Speaking", "Writing", "Work", "WorkIndex", "WritingIndex", "WritingArticle", "PlainPage", "Markdown", "MarkdownPage", "MarkdownSite", "MarkdownStore", "MarkdownRenderer", "MainLayout",
    "HeroSection", "WorkSection", "SkillsSection", "WritingSection", "CtaSection", "Src", "Test", "Tests",
};
var csharpKeywords = new HashSet<string>(StringComparer.Ordinal)
{
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default",
    "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto",
    "if", "implicit", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out",
    "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
    "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
    "using", "virtual", "void", "volatile", "while",
};

if (args.Length != 1 || args[0] is "-h" or "--help" or "/?")
{
    Console.WriteLine("Usage: dotnet run rename.cs <NewName>      (for example: dotnet run rename.cs Acme)");
    return args.Length == 1 ? 0 : 1;
}

var name = args[0];
if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9]*$") || name.Length > 40)
{
    return Fail($"'{name}' cannot be used: give one name of at most 40 letters and digits, starting with a letter (Acme, MyApp, TaskBoard). Underscores and dots are out: the name is also the iOS bundle id and the prefix of every namespace.");
}
if (reserved.Contains(name) || csharpKeywords.Contains(name))
{
    return Fail($"'{name}' cannot be used: it is a C# keyword or the name of a framework namespace or type (System, Task, Microsoft, Blux...), or of a part of this template (Web, Shared...), and it would make namespaces and type names ambiguous. Pick another one (Acme, TaskBoard).");
}

var root = Directory.GetCurrentDirectory();
var statePath = Path.Combine(root, StateFile);
var resuming = File.Exists(statePath);
if (!File.Exists(Path.Combine(root, "rename.cs")) || (!resuming && !File.Exists(Path.Combine(root, $"{Template}.slnx"))))
{
    return Fail($"Run this from the root of the repository, the folder that holds rename.cs and {Template}.slnx (it was already renamed if the solution has another name).");
}

// One pass over the three spellings, so a new name that contains the template name is never replaced again.
var spellings = new Dictionary<string, string>
{
    [Template] = name,
    [Template.ToLowerInvariant()] = name.ToLowerInvariant(),
    [Template.ToUpperInvariant()] = name.ToUpperInvariant(),
};
var spellingPattern = new Regex(string.Join("|", spellings.Keys.Select(Regex.Escape)));
string Replace(string text) => spellingPattern.Replace(text, match => spellings[match.Value]);

string Relative(string path) => Path.GetRelativePath(root, path).Replace('\\', '/');
string Absolute(string relative) => Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));

IEnumerable<string> Walk(string folder)
{
    foreach (var file in Directory.EnumerateFiles(folder))
    {
        yield return file;
    }
    foreach (var sub in Directory.EnumerateDirectories(folder))
    {
        if (!skippedFolders.Contains(Path.GetFileName(sub)))
        {
            foreach (var file in Walk(sub))
            {
                yield return file;
            }
        }
    }
}

IEnumerable<string> Ancestors(string? folder)
{
    while (folder is not null && folder != root && folder.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    {
        yield return folder;
        folder = Path.GetDirectoryName(folder);
    }
}

// The state: what is planned and what is done, one line each, so that a second run does not do anything twice (the content edit is not safe
// to repeat when the new name contains the template name) and finishes what the first one did not.
var edited = new HashSet<string>(StringComparer.Ordinal);
var done = new HashSet<string>(StringComparer.Ordinal);
var plan = new List<(string From, string To, bool IsFolder)>();
var contentsDone = false;

if (resuming)
{
    var lines = File.ReadAllLines(statePath);
    var recorded = lines.FirstOrDefault(l => l.StartsWith("name ", StringComparison.Ordinal))?[5..];
    if (recorded != name)
    {
        return Fail($"A rename to '{recorded}' did not finish. Run the same command again (dotnet run rename.cs {recorded}) to complete it; to start over with another name, first restore the repository (git checkout . && git clean -fd) and delete {StateFile}.");
    }
    foreach (var line in lines)
    {
        var space = line.IndexOf(' ');
        if (space < 0) continue;
        var (kind, value) = (line[..space], line[(space + 1)..]);
        switch (kind)
        {
            case "edited": edited.Add(value); break;
            case "done": done.Add(value); break;
            case "contents-done": contentsDone = true; break;
            case "plan":
                var parts = value.Split('|');
                plan.Add((parts[0], parts[1], parts[2] == "dir"));
                break;
        }
    }
    Console.WriteLine($"Continuing the rename to {name}.");
}
else
{
    var files = Walk(root).Where(f => !IsOurs(f)).ToList();
    var folders = files.Select(Path.GetDirectoryName).Distinct().SelectMany(Ancestors).Distinct().ToList();
    var moves = new List<(string From, string To, bool IsFolder)>();
    foreach (var path in files.Concat(folders).Distinct())
    {
        var leaf = Path.GetFileName(path);
        if (Replace(leaf) != leaf)
        {
            moves.Add((path, Path.Combine(Path.GetDirectoryName(path)!, Replace(leaf)), Directory.Exists(path)));
        }
    }
    // Nothing is changed if one of the new names is already taken.
    var taken = moves.Where(m => File.Exists(m.To) || Directory.Exists(m.To)).ToList();
    if (taken.Count > 0)
    {
        return Fail($"Cannot rename, these already exist: {string.Join(", ", taken.Select(m => Relative(m.To)))}");
    }
    // Deepest first, so a parent is still where its children expect it.
    plan = [.. moves.OrderByDescending(m => m.From.Length).Select(m => (Relative(m.From), Relative(m.To), m.IsFolder))];
    var header = new List<string> { $"name {name}" };
    header.AddRange(plan.Select(m => $"plan {m.From}|{m.To}|{(m.IsFolder ? "dir" : "file")}"));
    if (!TryRun("write " + StateFile, () => File.WriteAllLines(statePath, header)))
    {
        return 1;
    }
}

bool IsOurs(string path) =>
    string.Equals(Path.GetFileName(path), "rename.cs", StringComparison.Ordinal) || string.Equals(Path.GetFileName(path), StateFile, StringComparison.Ordinal);

bool TryRun(string what, Action action)
{
    try
    {
        action();
        return true;
    }
    catch (Exception error) when (error is IOException or UnauthorizedAccessException)
    {
        Console.Error.WriteLine($"rename: could not {what}: {error.Message}");
        Console.Error.WriteLine($"rename: the repository is partly renamed. Fix the cause (close the file or the program that holds it) and run the same command again: dotnet run rename.cs {name}. It continues where it stopped.");
        return false;
    }
}

// Contents of the text files. A file is text when it has no NUL byte at its start and is valid UTF-8; the BOM and the line endings are kept.
var changed = 0;
if (!contentsDone)
{
    foreach (var file in Walk(root).Where(f => !IsOurs(f)).ToList())
    {
        var relative = Relative(file);
        if (edited.Contains(relative)) continue;
        var ok = TryRun($"edit {relative}", () =>
        {
            var bytes = File.ReadAllBytes(file);
            if (bytes.Length <= 8 * 1024 * 1024 && Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8192)) < 0)
            {
                var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                try
                {
                    var text = new UTF8Encoding(false, true).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
                    var updated = Replace(text);
                    if (string.Equals(Path.GetFileName(file), "README.md", StringComparison.Ordinal))
                    {
                        // The README explains this script before it runs: that part has no use afterwards.
                        updated = Regex.Replace(updated, @"<!-- rename:start -->.*?<!-- rename:end -->\r?\n?", "", RegexOptions.Singleline);
                    }
                    if (updated != text)
                    {
                        File.WriteAllText(file, updated, new UTF8Encoding(bom));
                        changed++;
                    }
                }
                catch (DecoderFallbackException)
                {
                    // Not UTF-8: a binary file with no NUL byte at its start.
                }
            }
            File.AppendAllText(statePath, $"edited {relative}{Environment.NewLine}");
        });
        if (!ok) return 1;
    }
    if (!TryRun("write " + StateFile, () => File.AppendAllText(statePath, $"contents-done x{Environment.NewLine}")))
    {
        return 1;
    }
}

// Files and folders. An entry already moved by an earlier run is skipped; one whose source and target are both missing is an error.
var moved = 0;
foreach (var (from, to, isFolder) in plan)
{
    if (done.Contains(from)) continue;
    var source = Absolute(from);
    var target = Absolute(to);
    var exists = isFolder ? Directory.Exists(source) : File.Exists(source);
    if (exists)
    {
        var ok = TryRun($"rename '{from}' to '{to}'", () =>
        {
            if (isFolder)
            {
                Directory.Move(source, target);
            }
            else
            {
                File.Move(source, target);
            }
            File.AppendAllText(statePath, $"done {from}{Environment.NewLine}");
        });
        if (!ok) return 1;
        moved++;
    }
    else if (!(isFolder ? Directory.Exists(target) : File.Exists(target)))
    {
        return Fail($"'{from}' is gone and '{to}' does not exist. The repository was changed by something else since the rename started: restore it (git checkout . && git clean -fd), delete {StateFile} and run the command again.");
    }
}

if (!TryRun("delete rename.cs", () =>
{
    File.Delete(statePath);
    File.Delete(Path.Combine(root, "rename.cs"));
}))
{
    return 1;
}

Console.WriteLine($"Renamed {Template} to {name}: {changed} file(s) edited, {moved} file(s) and folder(s) renamed. rename.cs is deleted.");
Console.WriteLine();
Console.WriteLine("Next:");
Console.WriteLine($"  dotnet run --project src/{name}.Web");
return 0;

static int Fail(string message)
{
    Console.Error.WriteLine($"rename: {message}");
    return 1;
}
