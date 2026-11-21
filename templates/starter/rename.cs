// Gives this template your own name, then deletes itself. Run it once, from the root of the repository:
//
//     dotnet run rename.cs MyApp
//
// It is a .NET 10 file-based app: no project, nothing to install. MyApp is one identifier (letters, digits and underscores, starting with a
// letter): it becomes the name of the solution, the projects, the namespaces, the MAUI application title and the lowercase part of the
// application id (com.companyname.myapp), and replaces every other occurrence of the template name, in file contents and in file and folder names.
using System.Text;
using System.Text.RegularExpressions;

const string Template = "Starter";

var skippedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".git", ".vs", "bin", "obj", "node_modules" };
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
    Console.WriteLine($"Usage: dotnet run rename.cs <NewName>      (for example: dotnet run rename.cs Acme)");
    return args.Length == 1 ? 0 : 1;
}

var name = args[0];
if (!Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_]*$") || csharpKeywords.Contains(name))
{
    return Fail($"'{name}' cannot be used: give one identifier made of letters, digits and underscores, starting with a letter, that is not a C# keyword (Acme, MyApp, TaskBoard).");
}
if (name == Template)
{
    return Fail($"'{name}' is the name of the template: give your own.");
}

var root = Directory.GetCurrentDirectory();
if (!File.Exists(Path.Combine(root, "rename.cs")) || !File.Exists(Path.Combine(root, $"{Template}.slnx")))
{
    return Fail($"Run this from the root of the repository, the folder that holds rename.cs and {Template}.slnx (it was already renamed if the solution has another name).");
}

// Longest and most specific forms first is not needed: the three spellings never overlap.
(string From, string To)[] replacements =
[
    (Template, name),
    (Template.ToLowerInvariant(), name.ToLowerInvariant()),
    (Template.ToUpperInvariant(), name.ToUpperInvariant()),
];

string Replace(string text)
{
    foreach (var (from, to) in replacements)
    {
        text = text.Replace(from, to, StringComparison.Ordinal);
    }
    return text;
}

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

var files = Walk(root).Where(f => !string.Equals(Path.GetFileName(f), "rename.cs", StringComparison.Ordinal)).ToList();

// Names first: nothing is changed if one of the new names is already taken.
var folders = files.Select(Path.GetDirectoryName).Distinct().SelectMany(Ancestors).Distinct().Where(d => d != root && d.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal)).ToList();
var moves = new List<(string From, string To, bool IsFolder)>();
foreach (var path in files.Concat(folders).Distinct())
{
    var leaf = Path.GetFileName(path);
    if (Replace(leaf) != leaf)
    {
        moves.Add((path, Path.Combine(Path.GetDirectoryName(path)!, Replace(leaf)), Directory.Exists(path)));
    }
}
var taken = moves.Where(m => File.Exists(m.To) || Directory.Exists(m.To)).ToList();
if (taken.Count > 0)
{
    return Fail($"Cannot rename, these already exist: {string.Join(", ", taken.Select(m => Path.GetRelativePath(root, m.To)))}");
}

// Contents of the text files. A file is text when it has no NUL byte at its start and is valid UTF-8; the BOM and the line endings are kept.
var changed = 0;
foreach (var file in files)
{
    var bytes = File.ReadAllBytes(file);
    if (bytes.Length > 8 * 1024 * 1024 || Array.IndexOf(bytes, (byte)0, 0, Math.Min(bytes.Length, 8192)) >= 0)
    {
        continue;
    }
    var bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
    string text;
    try
    {
        text = new UTF8Encoding(false, true).GetString(bytes, bom ? 3 : 0, bytes.Length - (bom ? 3 : 0));
    }
    catch (DecoderFallbackException)
    {
        continue;
    }
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

// Files and folders, the deepest first so a parent is still where its children expect it.
foreach (var move in moves.OrderByDescending(m => m.From.Length))
{
    if (move.IsFolder)
    {
        Directory.Move(move.From, move.To);
    }
    else
    {
        File.Move(move.From, move.To);
    }
}

File.Delete(Path.Combine(root, "rename.cs"));

Console.WriteLine($"Renamed {Template} to {name}: {changed} file(s) edited, {moves.Count} file(s) and folder(s) renamed. rename.cs is deleted.");
Console.WriteLine();
Console.WriteLine("Next:");
Console.WriteLine($"  dotnet run --project src/{name}.Web");
Console.WriteLine($"  The MAUI app id is com.companyname.{name.ToLowerInvariant()} (src/{name}.Maui/{name}.Maui.csproj): set your own before you publish.");
return 0;

static IEnumerable<string> Ancestors(string? folder)
{
    while (folder is not null)
    {
        yield return folder;
        folder = Path.GetDirectoryName(folder);
    }
}

static int Fail(string message)
{
    Console.Error.WriteLine($"rename: {message}");
    return 1;
}
