namespace Blux.Templates.Tests;

/// <summary>
/// The Markdown content code is shared by copying: templates/_shared/Content is the reference, and every template that uses it carries a copy in
/// <c>src/&lt;Project&gt;/Content</c>, so each template stays a complete repository with no reference to the monorepo. These tests keep the copies equal to
/// the reference. A fix to the shared code is made in the reference and then copied to every template, in the same commit.
/// </summary>
public class TemplateCopyTests
{
    /// <summary>The templates that use the shared Markdown code, and so must carry the whole reference. Add Portfolio when it exists.</summary>
    private static readonly string[] ContentTemplates = ["docs", "changelog", "saas"];

    private static readonly string Root = RepositoryRoot();
    private static readonly string Templates = Path.Combine(Root, "templates");
    private static readonly string Reference = Path.Combine(Templates, "_shared", "Content");

    private static string RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Blux.UI.slnx")))
            {
                return dir.FullName;
            }
        }
        throw new DirectoryNotFoundException("Blux.UI.slnx not found above the test output directory.");
    }

    private static IEnumerable<string> Files(string folder) =>
        Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(part => part is "bin" or "obj"))
            .Select(f => Path.GetRelativePath(folder, f).Replace('\\', '/'))
            .Order(StringComparer.Ordinal);

    // Line endings are the business of git, not of the content of a file.
    private static string Text(string path) => File.ReadAllText(path).Replace("\r\n", "\n");

    /// <summary>
    /// The Content folders that must equal the reference: every <c>src/&lt;Project&gt;/Content</c> folder of a template in <see cref="ContentTemplates"/>
    /// (found or not, so a template that lost its copy fails), and of any other template when it holds a file with the name of a reference file (a copy
    /// that was started, or a file that was added next to the shared ones). A Content folder with its own files, like the one of the Landing, is left alone.
    /// </summary>
    private static IEnumerable<(string Template, string Folder)> Copies()
    {
        var referenceNames = Files(Reference).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var template in Directory.EnumerateDirectories(Templates).Where(t => !Path.GetFileName(t).StartsWith('_')))
        {
            var name = Path.GetFileName(template);
            var src = Path.Combine(template, "src");
            var contents = Directory.Exists(src)
                ? Directory.EnumerateDirectories(src).Select(project => Path.Combine(project, "Content")).Where(Directory.Exists).ToList()
                : [];
            foreach (var content in contents.Where(c => ContentTemplates.Contains(name) || Files(c).Any(referenceNames.Contains)))
            {
                yield return (name, content);
            }
        }
    }

    [Fact]
    public void The_reference_is_there_and_has_code()
    {
        Assert.True(Directory.Exists(Reference), $"{Reference} is missing");
        Assert.Contains("MarkdownStore.cs", Files(Reference));
        Assert.All(Files(Reference), file => Assert.EndsWith(".cs", file));
    }

    [Fact]
    public void Every_template_that_uses_the_shared_code_has_exactly_one_content_folder_for_it()
    {
        var copies = Copies().ToList();
        foreach (var template in ContentTemplates)
        {
            Assert.True(Directory.Exists(Path.Combine(Templates, template)), $"templates/{template} is listed in ContentTemplates but does not exist");
            Assert.True(copies.Count(c => c.Template == template) == 1, $"templates/{template} must have one src/<Project>/Content folder with the shared code");
        }
    }

    [Fact]
    public void Every_copy_has_the_files_of_the_reference_and_nothing_else_and_each_file_is_equal()
    {
        var reference = Files(Reference).ToList();
        foreach (var (template, folder) in Copies())
        {
            var copy = Files(folder).ToList();
            Assert.True(reference.SequenceEqual(copy),
                $"templates/{template}: the files of Content differ from templates/_shared/Content.\n  missing: [{string.Join(", ", reference.Except(copy))}]\n  extra: [{string.Join(", ", copy.Except(reference))}]\n" +
                "Put template code outside Content, and change shared code in templates/_shared/Content first.");
            foreach (var file in reference)
            {
                Assert.True(Text(Path.Combine(Reference, file)) == Text(Path.Combine(folder, file)),
                    $"templates/{template}/{Path.GetRelativePath(Templates, folder).Split(Path.DirectorySeparatorChar, 3)[^1]}/{file} differs from templates/_shared/Content/{file}. " +
                    "Copy the reference over it (cp -r templates/_shared/Content/. <template>/src/<Project>/Content/).");
            }
        }
    }

    [Fact]
    public void The_reference_does_not_name_a_template_because_a_rename_would_rewrite_the_copies()
    {
        // rename.cs replaces the name of its template in every file, so a copy that mentioned another template, or its own, would stop being equal to the reference.
        var names = Directory.EnumerateDirectories(Templates).Select(Path.GetFileName).Where(n => !n!.StartsWith('_')).Concat(["saas", "portfolio", "changelog"]).Distinct().ToList();
        foreach (var file in Files(Reference))
        {
            var text = Text(Path.Combine(Reference, file));
            Assert.All(names, name => Assert.DoesNotContain(name!, text, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void The_shared_folder_is_not_a_template()
    {
        // The registry of the gallery lists templates by name; a folder that starts with an underscore holds what they share.
        Assert.DoesNotContain(Blux.Playground.Client.Templates.TemplateRegistry.All, t => t.Name.StartsWith('_'));
        Assert.False(Directory.Exists(Path.Combine(Templates, "_shared", "src")));
        Assert.False(File.Exists(Path.Combine(Templates, "_shared", "rename.cs")));
    }
}
