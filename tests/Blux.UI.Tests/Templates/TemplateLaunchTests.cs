using Blux.Playground.Client.Templates;
using Blux.Playground.Templates;

namespace Blux.UI.Tests.Templates;

public class TemplateLaunchTests
{
    [Theory]
    [InlineData(null, "/templates/starter/demo")]
    [InlineData("", "/templates/starter/demo")]
    [InlineData("/", "/templates/starter/demo")]
    [InlineData("/sub", "/sub/templates/starter/demo")]
    [InlineData("sub/", "/sub/templates/starter/demo")]
    [InlineData("/a/b/", "/a/b/templates/starter/demo")]
    public void The_path_base_of_a_demo_follows_the_one_of_the_docs(string? docs, string expected) =>
        Assert.Equal(expected, TemplateLaunch.DemoPathBase(docs, "starter"));

    [Theory]
    [InlineData("/templates/starter/demo", true)]
    [InlineData("/templates/starter/demo/", true)]
    [InlineData("/templates/starter/demo/about", true)]
    [InlineData("/templates/starter/demo/_framework/blazor.web.js", true)]
    [InlineData("/templates/starter", false)]
    [InlineData("/templates", false)]
    [InlineData("/templates/other/demo/", false)]
    [InlineData("/templates/starter/demos", false)]
    [InlineData("/docs/templates/starter/demo", false)]
    [InlineData("/", false)]
    public void A_demo_path_is_below_templates_name_demo(string path, bool expected) =>
        Assert.Equal(expected, TemplateLaunch.IsDemoPath(path, ["starter"]));

    private static readonly TemplateLaunch.ChildSettings NoWatchdog = new(null, 1, 0);

    [Theory]
    [InlineData("/Templates/Starter/Demo/about", true)]
    [InlineData("/TEMPLATES/STARTER/DEMO", true)]
    public void Demo_paths_are_matched_without_regard_to_case(string path, bool expected) =>
        Assert.Equal(expected, TemplateLaunch.IsDemoPath(path, ["starter"]));

    [Fact]
    public void A_published_demo_is_started_with_dotnet_on_its_dll_on_a_loopback_port_below_its_path_base()
    {
        var command = TemplateLaunch.Build(TemplateRunMode.Published, "Starter.Web", "/srv/demos/starter", "/srv/demos/starter", 41234, "/templates/starter/demo", [], NoWatchdog);

        Assert.Equal("dotnet", command.FileName);
        Assert.Equal(
            [Path.Combine("/srv/demos/starter", "Starter.Web.dll"), "--urls", "http://127.0.0.1:41234", "--PathBase=/templates/starter/demo"],
            command.Arguments);
        Assert.Equal("/srv/demos/starter", command.WorkingDirectory);
        Assert.Equal("Production", command.Environment["ASPNETCORE_ENVIRONMENT"]);
        Assert.Equal("true", command.Environment["ASPNETCORE_FORWARDEDHEADERS_ENABLED"]);
    }

    [Fact]
    public void A_development_demo_runs_the_build_output_from_the_project_folder_in_Development()
    {
        var command = TemplateLaunch.Build(TemplateRunMode.Development, "Starter.Web", "/repo/templates/starter/src/Starter.Web/bin/Debug/net10.0", "/repo/templates/starter/src/Starter.Web", 5000, "/sub/templates/starter/demo", [], NoWatchdog);

        Assert.Equal(Path.Combine("/repo/templates/starter/src/Starter.Web/bin/Debug/net10.0", "Starter.Web.dll"), command.Arguments[0]);
        Assert.Equal("/repo/templates/starter/src/Starter.Web", command.WorkingDirectory);
        Assert.Equal("Development", command.Environment["ASPNETCORE_ENVIRONMENT"]);
        Assert.Contains("--PathBase=/sub/templates/starter/demo", command.Arguments);
    }

    private static Dictionary<string, string> Env(Dictionary<string, string?> host, TemplateLaunch.ChildSettings? settings = null, TemplateRunMode mode = TemplateRunMode.Published) =>
        new(TemplateLaunch.ChildEnvironment(host, mode, settings ?? NoWatchdog));

    [Fact]
    public void A_demo_gets_what_a_dotnet_app_needs_from_the_host_and_nothing_else()
    {
        var host = new Dictionary<string, string?>
        {
            ["PATH"] = "/usr/bin", ["HOME"] = "/home/x", ["LANG"] = "fr_FR.UTF-8", ["LC_ALL"] = "C", ["TZ"] = "UTC", ["TMPDIR"] = "/tmp",
            ["DOTNET_ROOT"] = "/usr/share/dotnet", ["DOTNET_gcServer"] = "0",
            ["Kestrel__Endpoints__Https__Url"] = "https://*:443", ["ASPNETCORE_Kestrel__Certificates__Default__Password"] = "hunter2",
            ["ASPNETCORE_URLS"] = "http://*:80", ["Blux__Templates__PublishRoot"] = "/srv", ["Blux__Ai__Key"] = "secret",
            ["ConnectionStrings__Main"] = "Server=db", ["GITHUB_TOKEN"] = "ghp_x", ["PathBase"] = "/sub",
            ["DOTNET_URLS"] = "http://*:81", ["DOTNET_ENVIRONMENT"] = "Staging", ["DOTNET_STARTUP_HOOKS"] = "/evil.dll",
        };

        var env = Env(host);

        Assert.Equal("/usr/bin", env["PATH"]);
        Assert.Equal("/home/x", env["HOME"]);
        Assert.Equal("fr_FR.UTF-8", env["LANG"]);
        Assert.Equal("C", env["LC_ALL"]);
        Assert.Equal("UTC", env["TZ"]);
        Assert.Equal("/usr/share/dotnet", env["DOTNET_ROOT"]);
        Assert.Equal("0", env["DOTNET_gcServer"]);
        string[] leaked = ["Kestrel__Endpoints__Https__Url", "ASPNETCORE_Kestrel__Certificates__Default__Password", "ASPNETCORE_URLS", "Blux__Templates__PublishRoot",
            "Blux__Ai__Key", "ConnectionStrings__Main", "GITHUB_TOKEN", "PathBase", "DOTNET_URLS", "DOTNET_ENVIRONMENT", "DOTNET_STARTUP_HOOKS"];
        Assert.All(leaked, name => Assert.DoesNotContain(name, env.Keys));
    }

    [Fact]
    public void The_supervisor_decides_the_environment_the_memory_the_hook_and_the_parent()
    {
        var host = new Dictionary<string, string?> { ["ASPNETCORE_ENVIRONMENT"] = "Staging", ["DOTNET_GCHeapHardLimit"] = "1", ["DOTNET_GCConserveMemory"] = "0" };

        var env = Env(host, new TemplateLaunch.ChildSettings("/app/Blux.Playground.DemoWatchdog.dll", 4242, 512));

        Assert.Equal("Production", env["ASPNETCORE_ENVIRONMENT"]);
        Assert.Equal("5", env["DOTNET_GCConserveMemory"]);
        Assert.Equal("20000000", env["DOTNET_GCHeapHardLimit"]);
        Assert.Equal("/app/Blux.Playground.DemoWatchdog.dll", env["DOTNET_STARTUP_HOOKS"]);
        Assert.Equal("1", env["BLUX_WATCHDOG_STDIN"]);
        Assert.Equal("4242", env["BLUX_WATCHDOG_PARENT_PID"]);
        Assert.Equal("Development", Env(host, mode: TemplateRunMode.Development)["ASPNETCORE_ENVIRONMENT"]);
    }

    [Fact]
    public void Without_a_limit_or_a_hook_neither_is_set_even_if_the_host_had_one()
    {
        var env = Env(new Dictionary<string, string?> { ["DOTNET_GCHeapHardLimit"] = "1", ["DOTNET_STARTUP_HOOKS"] = "/x.dll" });

        Assert.DoesNotContain("DOTNET_GCHeapHardLimit", env.Keys);
        Assert.DoesNotContain("DOTNET_STARTUP_HOOKS", env.Keys);
        Assert.DoesNotContain("BLUX_WATCHDOG_STDIN", env.Keys);
        Assert.DoesNotContain("BLUX_WATCHDOG_PARENT_PID", env.Keys);
    }

    [Theory]
    [InlineData(200, "<head><base href=\"/templates/starter/demo/\" /></head>", "/templates/starter/demo", true)]
    [InlineData(302, "<BASE HREF=\"/templates/starter/demo/\">", "/templates/starter/demo/", true)]
    [InlineData(404, "<base href=\"/templates/starter/demo/\" />", "/templates/starter/demo", false)]
    [InlineData(200, "<html>some other program</html>", "/templates/starter/demo", false)]
    [InlineData(200, "<base href=\"/\" />", "/templates/starter/demo", false)]
    [InlineData(502, "", "/templates/starter/demo", false)]
    public void An_answer_is_the_demos_when_it_succeeds_and_is_its_own_page(int status, string body, string pathBase, bool expected) =>
        Assert.Equal(expected, TemplateLaunch.IsDemoResponse(status, body, pathBase));

    [Fact]
    public void The_probe_asks_for_the_home_page_below_the_path_base_and_the_proxy_for_the_root()
    {
        Assert.Equal("http://127.0.0.1:41234/templates/starter/demo/", TemplateLaunch.ProbeUrl(41234, "/templates/starter/demo"));
        Assert.Equal("http://127.0.0.1:41234/", TemplateLaunch.Destination(41234));
    }

    [Fact]
    public void A_port_the_system_hands_out_twice_is_asked_for_again_until_it_is_unreserved()
    {
        var offered = new Queue<int>([5000, 5000, 5001, 5000, 5002]);
        var reserved = new HashSet<int>();

        Assert.Equal(5000, TemplateLaunch.AllocatePort(offered.Dequeue, reserved));
        Assert.Equal(5001, TemplateLaunch.AllocatePort(offered.Dequeue, reserved));
        Assert.Equal(5002, TemplateLaunch.AllocatePort(offered.Dequeue, reserved));
        Assert.Equal([5000, 5001, 5002], reserved.Order());
    }

    [Fact]
    public void A_released_port_can_be_taken_again()
    {
        var reserved = new HashSet<int>();
        Assert.Equal(7000, TemplateLaunch.AllocatePort(() => 7000, reserved));
        reserved.Remove(7000);

        Assert.Equal(7000, TemplateLaunch.AllocatePort(() => 7000, reserved));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(70000)]
    public void An_impossible_port_is_not_used(int bad)
    {
        var offered = new Queue<int>([bad, 6000]);

        Assert.Equal(6000, TemplateLaunch.AllocatePort(offered.Dequeue, new HashSet<int>()));
    }

    [Fact]
    public void Allocation_gives_up_when_no_port_is_ever_free()
    {
        var reserved = new HashSet<int> { 5000 };

        Assert.Throws<InvalidOperationException>(() => TemplateLaunch.AllocatePort(() => 5000, reserved, attempts: 3));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 4)]
    [InlineData(4, 8)]
    [InlineData(5, 16)]
    [InlineData(6, 30)]
    [InlineData(50, 30)]
    public void The_restart_delay_doubles_with_each_failure_in_a_row_up_to_thirty_seconds(int failures, double seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), TemplateLaunch.RestartDelay(failures));

    [Fact]
    public void A_process_that_ran_a_minute_did_not_fail_in_a_loop_and_the_count_starts_over()
    {
        Assert.Equal(1, TemplateLaunch.NextFailureCount(7, TimeSpan.FromSeconds(60)));
        Assert.Equal(1, TemplateLaunch.NextFailureCount(7, TimeSpan.FromMinutes(10)));
        Assert.Equal(8, TemplateLaunch.NextFailureCount(7, TimeSpan.FromSeconds(59)));
        Assert.Equal(1, TemplateLaunch.NextFailureCount(0, TimeSpan.FromSeconds(1)));
    }

    // ---- The registry and the repository address

    [Fact]
    public void The_gallery_lists_ten_templates_and_only_available_ones_can_be_run()
    {
        Assert.Equal(10, TemplateRegistry.All.Count);
        Assert.Equal(TemplateRegistry.All.Count, TemplateRegistry.All.Select(t => t.Name).Distinct().Count());
        Assert.All(TemplateRegistry.All, t => Assert.Equal(3, t.Features.Count));
        Assert.All(TemplateRegistry.All.Where(t => t.HasDemo), t =>
        {
            Assert.False(string.IsNullOrEmpty(t.WebProject));
            Assert.False(string.IsNullOrEmpty(t.WebAssembly));
        });
        Assert.All(TemplateRegistry.All.Where(t => !t.HasDemo), t => Assert.Null(t.WebAssembly));
        Assert.Equal(["starter", "landing"], TemplateRegistry.All.Where(t => t.HasDemo).Select(t => t.Name));
    }

    [Fact]
    public void The_demo_path_is_relative_to_the_site_root_with_a_trailing_slash()
    {
        var starter = TemplateRegistry.Find("starter")!;

        Assert.Equal("templates/starter/demo/", starter.DemoPath);
        Assert.Equal("img/template-starter-dark.png", starter.Screenshot("dark"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/acme")]
    public void Without_a_usable_repository_base_there_is_no_source_link(string? value)
    {
        var settings = new TemplateSettings(value);
        var starter = TemplateRegistry.Find("starter")!;

        Assert.False(settings.IsPublished);
        Assert.Null(settings.SourceUrl(starter));
        Assert.Null(settings.GitHubTemplate(starter));
    }

    [Theory]
    [InlineData("https://github.com/acme")]
    [InlineData("https://github.com/acme/")]
    public void A_repository_base_gives_the_source_link_and_the_gh_template(string value)
    {
        var settings = new TemplateSettings(value);
        var starter = TemplateRegistry.Find("starter")!;

        Assert.True(settings.IsPublished);
        Assert.Equal("https://github.com/acme/blux-template-starter", settings.SourceUrl(starter));
        Assert.Equal("acme/blux-template-starter", settings.GitHubTemplate(starter));
    }

    [Fact]
    public void A_base_outside_github_links_to_the_source_but_has_no_gh_command()
    {
        var settings = new TemplateSettings("https://git.example.com/team");
        var starter = TemplateRegistry.Find("starter")!;

        Assert.Equal("https://git.example.com/team/blux-template-starter", settings.SourceUrl(starter));
        Assert.Null(settings.GitHubTemplate(starter));
    }
}
