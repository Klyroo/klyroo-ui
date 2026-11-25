using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Klyroo.UI.Build;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Klyroo.UI.Tests.Icons;

/// <summary>An id of an icon is global to the page: every drawing gets its own, with every reference to it rewritten.</summary>
public class IconIdTests : KlyrooTestContext
{
    private const string Gradient =
        """<defs><linearGradient id="g"><stop offset="0" stop-color="#f00"/><stop offset="1" stop-color="#00f"/></linearGradient><clipPath id="c"><rect width="8" height="8"/></clipPath></defs><path d="M0 0h8v8H0z" fill="url(#g)" clip-path="url(#c)"/>""";

    private static string Unique(string body)
    {
        string? suffix = null;
        return IconIds.Uniquify(body, ref suffix);
    }

    private static string[] Ids(string markup) => [.. Regex.Matches(markup, "\\sid=\"([^\"]+)\"").Select(m => m.Groups[1].Value)];

    private static string[] References(string markup) => [.. Regex.Matches(markup, "url\\(#([^)]+)\\)").Select(m => m.Groups[1].Value)];

    [Fact]
    public void A_body_without_ids_is_returned_untouched()
    {
        var body = "<path d=\"M0 0h8v8H0z\" fill=\"currentColor\"/>";
        string? suffix = null;

        Assert.Same(body, IconIds.Uniquify(body, ref suffix));
        Assert.Null(suffix);
    }

    [Fact]
    public void Ids_and_every_reference_to_them_are_rewritten_together()
    {
        var result = Unique(Gradient);

        var ids = Ids(result);
        Assert.Equal(2, ids.Length);
        Assert.All(ids, i => Assert.Matches(@"^(g|c)-kly[0-9a-f]+$", i));
        Assert.All(References(result), r => Assert.Contains(r, ids));
        Assert.Equal(2, References(result).Length);
    }

    [Fact]
    public void Two_drawings_of_the_same_icon_never_share_an_id()
    {
        var first = Unique(Gradient);
        var second = Unique(Gradient);

        Assert.Empty(Ids(first).Intersect(Ids(second)));
        Assert.Empty(References(first).Intersect(References(second)));
    }

    [Fact]
    public void The_same_suffix_gives_the_same_markup_so_a_component_that_draws_again_changes_nothing()
    {
        string? suffix = null;
        var a = IconIds.Uniquify(Gradient, ref suffix);
        var b = IconIds.Uniquify(Gradient, ref suffix);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Href_xlink_href_and_animation_timing_references_follow()
    {
        const string body = "<linearGradient id=\"a\"/><linearGradient id=\"b\" xlink:href=\"#a\"/><use href=\"#b\"/>"
            + "<rect><animate id=\"x0\" attributeName=\"y\" begin=\"0;x1.end-0.1s\" dur=\"1s\"/><animate id=\"x1\" attributeName=\"y\" begin=\"x0.begin+0.1s;x0.repeat(2)\" end=\"click\" dur=\"1s\"/></rect>";

        var result = Unique(body);

        var suffix = Regex.Match(Ids(result)[0], "-kly[0-9a-f]+$").Value;
        Assert.Contains("xlink:href=\"#a" + suffix + "\"", result);
        Assert.Contains("<use href=\"#b" + suffix + "\"/>", result);
        Assert.Contains("begin=\"0;x1" + suffix + ".end-0.1s\"", result);
        Assert.Contains("begin=\"x0" + suffix + ".begin+0.1s;x0" + suffix + ".repeat(2)\"", result);
        Assert.Contains("end=\"click\"", result);
    }

    [Fact]
    public void References_to_ids_outside_the_icon_are_left_alone()
    {
        var result = Unique("<defs><clipPath id=\"c\"/></defs><path clip-path=\"url(#c)\" fill=\"url(#elsewhere)\" href=\"#elsewhere\"/>");

        Assert.Contains("fill=\"url(#elsewhere)\"", result);
        Assert.Contains("href=\"#elsewhere\"", result);
    }

    [Fact]
    public void Single_quoted_markup_registered_in_code_works_too()
    {
        var result = Unique("<linearGradient id='g'/><path fill='url(#g)'/>");

        var id = Regex.Match(result, "id='([^']+)'").Groups[1].Value;
        Assert.Matches("^g-kly[0-9a-f]+$", id);
        Assert.Contains("fill='url(#" + id + ")'", result);
    }

    [Fact]
    public void Two_instances_of_an_icon_with_a_gradient_have_distinct_ids_and_consistent_references()
    {
        KlyrooIconBundle.Add("idtest", "grad", Gradient, "0 0 8 8");

        var first = Render<KlyIcon>(p => p.Add(i => i.Name, "idtest:grad")).Markup;
        var second = Render<KlyIcon>(p => p.Add(i => i.Name, "i-idtest-grad")).Markup;

        Assert.Empty(Ids(first).Intersect(Ids(second)));
        foreach (var markup in new[] { first, second })
        {
            Assert.Equal(2, Ids(markup).Length);
            Assert.All(References(markup), r => Assert.Contains(r, Ids(markup)));
        }
    }

    [Fact]
    public void An_icon_keeps_its_ids_when_the_component_renders_again()
    {
        KlyrooIconBundle.Add("idtest", "grad2", Gradient, "0 0 8 8");
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "idtest:grad2"));
        var before = Ids(cut.Markup);

        cut.Render(p => p.Add(i => i.Name, "idtest:grad2").Add(i => i.Label, "Icon"));

        Assert.Equal(before, Ids(cut.Markup));
    }

    [Fact]
    public void An_icon_without_ids_renders_exactly_the_body()
    {
        var cut = Render<KlyIcon>(p => p.Add(i => i.Name, "search"));

        Assert.DoesNotContain("-kly", cut.Markup);
    }

    [Fact]
    public void The_render_fragment_and_the_script_markup_give_each_drawing_its_own_ids()
    {
        KlyrooIconBundle.Add("idtest", "grad3", Gradient, "0 0 8 8");

        var one = Render(IconRender.Fragment(null, "idtest:grad3", "size-4")).Markup;
        var two = Render(IconRender.Fragment(null, "idtest:grad3", "size-4")).Markup;
        var svgOne = IconRender.Svg(null, "idtest:grad3", "size-4");
        var svgTwo = IconRender.Svg(null, "idtest:grad3", "size-4");

        Assert.Empty(Ids(one).Intersect(Ids(two)));
        Assert.Empty(Ids(svgOne).Intersect(Ids(svgTwo)));
        Assert.All(References(svgOne), r => Assert.Contains(r, Ids(svgOne)));
    }
}

/// <summary>
/// A Blazor Web App with an interactive client project renders a component of the client project on the server before any client code ran,
/// and the generated module initializer only runs with the first code of its assembly. The bundles of the app are registered before the first lookup.
/// </summary>
public class IconRegistrationTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "klyroo-icons-asm-" + Guid.NewGuid().ToString("N"));
    private readonly Func<AssemblyLoadContext, AssemblyName, Assembly?> _resolver;

    public IconRegistrationTests()
    {
        Directory.CreateDirectory(_dir);
        _resolver = (context, name) =>
        {
            var path = Path.Combine(_dir, name.Name + ".dll");
            return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
        };
        AssemblyLoadContext.Default.Resolving += _resolver;
    }

    private sealed class Context : KlyrooTestContext;

    public void Dispose()
    {
        AssemblyLoadContext.Default.Resolving -= _resolver;
        try { Directory.Delete(_dir, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    private static readonly Lazy<List<MetadataReference>> Platform = new(() =>
        [.. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))]);

    private string Compile(string name, string source, params string[] referencedDlls)
    {
        var references = Platform.Value.Concat([MetadataReference.CreateFromFile(typeof(KlyrooIconBundle).Assembly.Location)])
            .Concat(referencedDlls.Select(r => (MetadataReference)MetadataReference.CreateFromFile(r)));
        var compilation = CSharpCompilation.Create(name, [CSharpSyntaxTree.ParseText(source, cancellationToken: Xunit.TestContext.Current.CancellationToken)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        var path = Path.Combine(_dir, name + ".dll");
        var result = compilation.Emit(path, cancellationToken: Xunit.TestContext.Current.CancellationToken);
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));
        return path;
    }

    /// <summary>The file the Klyroo.UI build generates for a project that bundles these icons.</summary>
    private static string Generated(params (string Prefix, string Name, string Body)[] icons) =>
        IconBundleBuilder.Emit(icons.Select(i => (new IconRef(i.Prefix, i.Name), new IconBody(i.Body, "0 0 8 8"))), null, null, out _);

    private static string Unique() => "a" + Guid.NewGuid().ToString("N")[..8];

    [Fact]
    public void An_assembly_loaded_after_the_first_icon_was_drawn_registers_its_icons_when_it_loads()
    {
        var prefix = Unique();
        var client = Compile("Client" + prefix, Generated((prefix, "dot", "<circle r=\"3\"/>")) + "\npublic class Marker { }");
        Assert.True(IconResolver.TryResolve("search", null, out _)); // the app already drew something
        Assert.False(KlyrooIconBundle.HasPrefix(prefix));

        Assembly.LoadFrom(client);

        Assert.True(KlyrooIconBundle.HasPrefix(prefix));
        Assert.True(IconResolver.TryResolve("i-" + prefix + "-dot", null, out var body));
        Assert.Equal("<circle r=\"3\"/>", body.Body);
        using var context = new Context();
        var cut = context.Render<KlyIcon>(p => p.Add(i => i.Name, "i-" + prefix + "-dot"));
        Assert.Equal("3", cut.Find("svg circle").GetAttribute("r"));
    }

    [Fact]
    public void The_server_registers_the_icons_of_the_client_project_it_references_before_any_client_code_ran()
    {
        var prefix = Unique();
        var clientName = "Client" + prefix;
        var client = Compile(clientName, Generated((prefix, "dot", "<circle r=\"5\"/>")) + "\npublic class Page { }");
        var server = Compile("Server" + prefix, "public class Program { public static object Use() => new " + "Page" + "(); }", client);
        var serverAssembly = Assembly.LoadFrom(server);
        // The client assembly is referenced and not loaded: nothing ran from it.
        Assert.DoesNotContain(AppDomain.CurrentDomain.GetAssemblies(), a => a.GetName().Name == clientName);
        Assert.False(KlyrooIconBundle.HasPrefix(prefix));

        KlyrooIconBundle.DiscoverReferences(serverAssembly);

        Assert.True(KlyrooIconBundle.HasPrefix(prefix));
        Assert.True(IconResolver.TryResolve(prefix + ":dot", null, out var body));
        Assert.Equal("<circle r=\"5\"/>", body.Body);
    }

    [Fact]
    public void A_library_referenced_by_a_library_is_found_when_it_uses_klyroo()
    {
        var prefix = Unique();
        var deep = Compile("Deep" + prefix, Generated((prefix, "deep", "<rect width=\"2\" height=\"2\"/>")) + "\npublic class DeepPage { }");
        var middle = Compile("Middle" + prefix, Generated((prefix + "m", "mid", "<g/>")) + "\npublic class MiddlePage { public static object Use() => new DeepPage(); }", deep);
        var server = Compile("Host" + prefix, "public class Host { public static object Use() => new MiddlePage(); }", middle, deep);

        KlyrooIconBundle.DiscoverReferences(Assembly.LoadFrom(server));

        Assert.True(KlyrooIconBundle.HasPrefix(prefix));
        Assert.True(KlyrooIconBundle.HasPrefix(prefix + "m"));
    }

    [Fact]
    public void An_i_name_of_a_client_collection_reads_the_same_before_and_after_the_client_ran()
    {
        var prefix = Unique();
        var client = Compile("Client" + prefix, Generated((prefix, "logo", "<path d=\"M0 0\"/>")) + "\npublic class Page { }");
        KlyrooIconBundle.EnsureDiscovered();
        Assembly.LoadFrom(client);

        // Registered through the options (as AddKlyrooUi would) or drawn by a component: the name is the same icon, registered or not yet.
        var options = new KlyrooIconOptions();
        options.Register("i-" + prefix + "-logo", "<g/>");
        Assert.True(IconNames.TryParse("i-" + prefix + "-logo", KlyrooIconBundle.HasPrefix, out var icon, out var form));
        Assert.Equal(prefix + ":logo", icon.Key);
        Assert.Equal(IconNameForm.IPrefix, form);
        Assert.True(IconResolver.TryResolve("i-" + prefix + "-logo", options, out _));
    }

    [Fact]
    public void Running_the_registration_twice_is_harmless()
    {
        var prefix = Unique();
        var client = Compile("Client" + prefix, Generated((prefix, "dot", "<circle r=\"1\"/>")) + "\npublic class Page { }");
        var assembly = Assembly.LoadFrom(client);

        KlyrooIconBundle.Discover(assembly);
        KlyrooIconBundle.DiscoverReferences(assembly);

        Assert.True(IconResolver.TryResolve(prefix + ":dot", null, out _));
    }
}
