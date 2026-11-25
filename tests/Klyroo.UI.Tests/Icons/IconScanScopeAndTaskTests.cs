using System.Collections;
using Klyroo.UI.Build;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Klyroo.UI.Tests.Icons;

/// <summary>Which files the scan reads, and when the task does nothing because nothing changed.</summary>
public sealed class IconSourceFinderTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "klyroo-icons-scan-" + Guid.NewGuid().ToString("N"));

    public IconSourceFinderTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    private string Write(string relative, string text = "x")
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        return path;
    }

    private List<string> Found(ISet<string>? skip = null, List<string>? notes = null) =>
        IconSourceFinder.Find([_root], skip ?? new HashSet<string>(), m => notes?.Add(m)).Select(s => Path.GetRelativePath(_root, s.Path)).OrderBy(p => p, StringComparer.Ordinal).ToList();

    [Fact]
    public void Lists_razor_cshtml_and_cs_files_and_nothing_else()
    {
        Write("A.razor"); Write("B.cshtml"); Write("sub/C.cs"); Write("D.txt"); Write("E.css");

        Assert.Equal(["A.razor", "B.cshtml", Path.Combine("sub", "C.cs")], Found());
    }

    [Fact]
    public void Dot_folders_and_the_folders_of_the_build_are_not_scanned()
    {
        Write("A.razor");
        Write(".git/hooks/X.cs"); Write(".claude/worktrees/w/Y.razor"); Write(".vs/Z.cs"); Write(".idea/Q.cs");
        Write("bin/Debug/B.cs"); Write("obj/C.cs"); Write("node_modules/pkg/D.cs"); Write("wwwroot/E.cs");

        Assert.Equal(["A.razor"], Found());
    }

    [Fact]
    public void Configured_excludes_are_not_scanned()
    {
        Write("A.razor"); Write("Generated/B.cs");

        Assert.Equal(["A.razor"], Found(new HashSet<string> { IconSourceFinder.Normalize(Path.Combine(_root, "Generated")) }));
    }

    [Fact]
    public void A_folder_that_is_a_link_is_not_followed_so_a_link_to_a_parent_cannot_loop()
    {
        Write("A.razor");
        Write("real/B.cs");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_root, "loop"), _root);
            Directory.CreateSymbolicLink(Path.Combine(_root, "alias"), Path.Combine(_root, "real"));
        }
        catch (Exception e) when (e is UnauthorizedAccessException || e is IOException || e is PlatformNotSupportedException)
        {
            return; // creating links needs a privilege on some systems
        }

        Assert.Equal(["A.razor", Path.Combine("real", "B.cs")], Found());
    }

    [Fact]
    public void A_file_over_one_mebibyte_is_not_read_and_is_mentioned_at_low_importance()
    {
        Write("A.razor");
        Write("Big.cs", new string('x', 1024 * 1024 + 1));
        var notes = new List<string>();

        Assert.Equal(["A.razor"], Found(notes: notes));
        Assert.Contains(notes, n => n.Contains("Big.cs") && n.Contains("1 MiB"));
    }

    [Fact]
    public void Listing_does_not_read_the_files()
    {
        if (OperatingSystem.IsWindows()) return;
        var path = Write("A.razor");
        File.SetUnixFileMode(path, UnixFileMode.None);

        try
        {
            Assert.Equal(["A.razor"], Found());
        }
        finally
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}

public sealed class KlyrooResolveIconsTaskTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "klyroo-icons-task-" + Guid.NewGuid().ToString("N"));
    private readonly Engine _engine = new();

    public KlyrooResolveIconsTaskTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, true);

    private string Output => Path.Combine(_root, "obj", "klyroo", "icons", "KlyrooIcons.g.cs");

    private KlyrooResolveIcons Task(bool collection = true) => new()
    {
        BuildEngine = _engine,
        ProjectDirectory = _root,
        CacheDir = Path.Combine(_root, "cache"),
        OutputFile = Output,
        Offline = true,
        Collections = collection ? [new TaskItem("Icons", new Dictionary<string, string> { ["Prefix"] = "custom" })] : [],
        ExcludeDirectories = [new TaskItem(Path.Combine(_root, "obj"))],
    };

    private void Svg(string name, string body = "<path d=\"M0 0h4\"/>")
    {
        Directory.CreateDirectory(Path.Combine(_root, "Icons"));
        File.WriteAllText(Path.Combine(_root, "Icons", name + ".svg"), "<svg viewBox=\"0 0 8 8\">" + body + "</svg>");
    }

    [Fact]
    public void Generates_the_file_and_says_there_are_icons()
    {
        Svg("dot");
        var task = Task();

        Assert.True(task.Execute());

        Assert.True(task.HasIcons);
        Assert.Contains("KlyrooIconBundle.Add(\"custom\", \"dot\"", File.ReadAllText(Output));
    }

    [Fact]
    public void A_second_build_with_nothing_changed_reads_and_writes_nothing()
    {
        Svg("dot");
        File.WriteAllText(Path.Combine(_root, "App.razor"), "<KlyIcon Name=\"search\" />");
        Assert.True(Task().Execute());
        File.WriteAllText(Output, File.ReadAllText(Output) + "// marker\n");

        var second = Task();
        Assert.True(second.Execute());

        Assert.True(second.HasIcons);
        Assert.EndsWith("// marker\n", File.ReadAllText(Output)); // not regenerated
        Assert.Contains(_engine.Messages, m => m.Contains("nothing changed"));
    }

    [Fact]
    public void A_changed_source_or_icon_file_runs_the_task_again()
    {
        Svg("dot");
        var source = Path.Combine(_root, "App.razor");
        File.WriteAllText(source, "<KlyIcon Name=\"search\" />");
        Assert.True(Task().Execute());
        File.WriteAllText(Output, "// stale\n");

        File.WriteAllText(source, "<KlyIcon Name=\"search\" /> <!-- longer -->");
        Assert.True(Task().Execute());
        Assert.Contains("KlyrooIconBundle.Add(\"custom\", \"dot\"", File.ReadAllText(Output));

        File.WriteAllText(Output, "// stale\n");
        Svg("dot", "<path d=\"M0 0h6\"/>");
        File.SetLastWriteTimeUtc(Path.Combine(_root, "Icons", "dot.svg"), DateTime.UtcNow.AddMinutes(5));
        Assert.True(Task().Execute());
        Assert.Contains("M0 0h6", File.ReadAllText(Output));
    }

    [Fact]
    public void A_deleted_output_is_generated_again_even_when_the_inputs_did_not_change()
    {
        Svg("dot");
        Assert.True(Task().Execute());
        File.Delete(Output);

        var task = Task();
        Assert.True(task.Execute());

        Assert.True(File.Exists(Output));
        Assert.True(task.HasIcons);
    }

    [Fact]
    public void When_no_icon_is_left_the_file_is_removed_and_the_task_says_so()
    {
        Svg("dot");
        Assert.True(Task().Execute());
        Assert.True(File.Exists(Output));

        var task = Task(collection: false);
        Assert.True(task.Execute());

        Assert.False(task.HasIcons);
        Assert.False(File.Exists(Output));
        // And it stays that way on the next build.
        var again = Task(collection: false);
        Assert.True(again.Execute());
        Assert.False(again.HasIcons);
        Assert.False(File.Exists(Output));
    }

    [Fact]
    public void A_build_with_a_problem_is_not_remembered_so_the_problem_is_reported_again()
    {
        Svg("dot");
        File.WriteAllText(Path.Combine(_root, "App.razor"), "<KlyIcon Name=\"custom:nope\" />");
        var first = Task();
        first.FailOnError = false;
        Assert.True(first.Execute());
        Assert.NotEmpty(_engine.Warnings);
        _engine.Warnings.Clear();

        var second = Task();
        second.FailOnError = false;
        Assert.True(second.Execute());

        Assert.NotEmpty(_engine.Warnings);
    }

    [Fact]
    public void A_scanned_source_in_a_dot_folder_is_not_read()
    {
        Svg("dot");
        Directory.CreateDirectory(Path.Combine(_root, ".worktree"));
        File.WriteAllText(Path.Combine(_root, ".worktree", "Other.razor"), "<KlyIcon Name=\"custom:ghost\" />");
        var task = Task();
        task.FailOnError = true;

        Assert.True(task.Execute());
        Assert.Empty(_engine.Errors);
    }

    private sealed class Engine : IBuildEngine
    {
        public List<string> Messages { get; } = [];
        public List<string> Warnings { get; } = [];
        public List<string> Errors { get; } = [];

        public bool ContinueOnError => false;
        public int LineNumberOfTaskNode => 0;
        public int ColumnNumberOfTaskNode => 0;
        public string ProjectFileOfTaskNode => "test.csproj";

        public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e.Message ?? "");
        public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e.Message ?? "");
        public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e.Message ?? "");
        public void LogCustomEvent(CustomBuildEventArgs e) { }

        public bool BuildProjectFile(string projectFileName, string[] targetNames, IDictionary globalProperties, IDictionary targetOutputs) => true;
    }
}
