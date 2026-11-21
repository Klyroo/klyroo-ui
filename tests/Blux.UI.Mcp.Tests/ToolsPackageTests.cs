using Blux.UI.Mcp.Markdown;
using Blux.UI.Mcp.Model;

namespace Blux.UI.Mcp.Tests;

/// <summary>Components of Blux.UI.Tools are documented next to those of Blux.UI, and every output says which package to add.</summary>
public class ToolsPackageTests
{
    private static BundleComponent Empty => TestSupport.Service.FindComponent("data-list-empty")!;

    [Fact]
    public void The_placeholder_is_in_the_bundle_with_its_package_and_its_theme()
    {
        Assert.Equal("Blux.UI.Tools", Empty.Package);
        Assert.Equal("dotnet add package Blux.UI.Tools --prerelease", Empty.InstallCommand());
        Assert.Equal("AddBluxUiTools", Empty.RegistrationMethod());

        var api = Assert.Single(Empty.Api);
        Assert.Equal("BxDataListEmpty", api.Name);
        Assert.Contains(api.Parameters, p => p.Name == "Title" && p.Description is not null);
        Assert.Contains(api.Slots, s => s.Name == "Actions");
        Assert.Equal("DataListEmpty", api.Theme!.Property);
        Assert.Equal(["root", "icon", "title", "description", "actions"], api.Theme.Slots.Keys);
        Assert.NotNull(api.Ui);
    }

    [Fact]
    public void Components_of_Blux_UI_have_no_package()
    {
        Assert.All(TestSupport.Bundle.Components.Where(c => c.Slug != "data-list-empty"), c => Assert.Null(c.Package));
    }

    [Fact]
    public void The_icon_of_the_package_is_in_the_icon_list()
    {
        Assert.Contains("search-x", TestSupport.Bundle.Icons);
        Assert.Contains("search", TestSupport.Bundle.Icons);
    }

    [Fact]
    public void The_component_tools_name_the_package()
    {
        Assert.Contains("dotnet add package Blux.UI.Tools --prerelease", TestSupport.Tools.GetComponent("DataListEmpty"));
        Assert.Contains("AddBluxUiTools()", TestSupport.Tools.GetComponent("BxDataListEmpty"));
        Assert.Contains("AddBluxUiTools(o => o.<Property>", TestSupport.Tools.GetComponent("data-list-empty", "theme"));
        Assert.Contains("Package Blux.UI.Tools", TestSupport.Tools.SearchComponents("data list empty"));
        Assert.Contains("\"package\":\"Blux.UI.Tools\"", TestSupport.Tools.GetComponentMetadata("data-list-empty"));
        Assert.DoesNotContain("Package ", TestSupport.Tools.SearchComponents("button"));
    }

    [Fact]
    public void Llms_files_and_the_skill_reference_name_the_package()
    {
        Assert.Contains("Package Blux.UI.Tools: `dotnet add package Blux.UI.Tools --prerelease`", LlmsText.Index(TestSupport.Bundle));
        Assert.Contains("dotnet add package Blux.UI.Tools --prerelease", LlmsText.Full(TestSupport.Bundle));
        Assert.Contains("Package Blux.UI.Tools, not part of Blux.UI: `dotnet add package Blux.UI.Tools --prerelease`", SkillReference.Components(TestSupport.Bundle));
    }

    [Fact]
    public void The_committed_skill_reference_has_the_package()
    {
        var committed = File.ReadAllText(Path.Combine(TestSupport.RepoRoot, SkillReference.RelativePath)).Replace("\r\n", "\n");

        Assert.Contains("### DataListEmpty", committed);
        Assert.Contains("`dotnet add package Blux.UI.Tools --prerelease`", committed);
    }
}
