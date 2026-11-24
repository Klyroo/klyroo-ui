using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Blux.UI.Tools.Tests;

public sealed class AgentUiSdkTests
{
    [Fact]
    public async Task AgentBridgeService_snapshots_queries_acts_and_observes_with_redaction_and_permissions()
    {
        var services = new ServiceCollection();
        services.AddBluxUiTools();
        using var provider = services.BuildServiceProvider();
        var agent = provider.GetRequiredService<AgentBridgeService>();

        var formSchema = new FormSchema
        {
            FormKey = "project-settings",
            Title = "Project Settings",
            Fields =
            [
                new FormFieldSchema
                {
                    Key = "projectName",
                    Label = "Project name",
                    DefaultValue = "Apollo"
                },
                new FormFieldSchema
                {
                    Key = "region",
                    Label = "Région",
                    Kind = FormFieldKind.Select,
                    DefaultValue = "us-east",
                    Options =
                    [
                        new FormOptionItem("us-east", "US East"),
                        new FormOptionItem("eu-west", "Europe West"),
                        new FormOptionItem("ap-south", "Asia Pacific")
                    ]
                },
                new FormFieldSchema
                {
                    Key = "apiToken",
                    Label = "API Secret",
                    Kind = FormFieldKind.Password,
                    DefaultValue = "sk_live_secret_123"
                }
            ]
        };

        var form = new FormController(formSchema);
        agent.RegisterForm(form, confirmSubmit: true);

        var dashboard = new DashboardController("sales");
        dashboard.AddFilter(new DashboardFilterControl(
            "year",
            "Year",
            items: [new DashboardOption("2025", "2025"), new DashboardOption("2026", "2026")],
            defaultValues: ["2026"]));
        agent.RegisterDashboard(dashboard);

        // 1. Snapshot lists components, redacts sensitive password fields, and exposes actions
        var snap = agent.Snapshot();
        Assert.Equal(6, snap.Count);
        var projectSnap = snap.First(s => s.Label == "Project name");
        Assert.Equal("Apollo", projectSnap.Value);
        Assert.Contains("setValue", projectSnap.Actions);

        var secretSnap = snap.First(s => s.Label == "API Secret");
        Assert.Equal(AgentBridgeService.RedactedValue, secretSnap.Value);

        // 2. Query inspects choices inside a select (matching accent-insensitively "region") without opening popups
        var regionQuery = agent.Query(new AgentQueryRequest { Within = "region", Text = "Europe" });
        Assert.Single(regionQuery);
        Assert.Single(regionQuery[0].Items);
        Assert.Equal("eu-west", regionQuery[0].Items[0].Value);
        Assert.Equal("Europe West", regionQuery[0].Items[0].Label);

        // 3. Observe records every change with source = Agent and redacts sensitive fields
        var observed = new List<ObservedChange>();
        using var sub = agent.Observe(c => observed.Add(c));

        // 4. Act changes project name, selects region, updates password, and changes dashboard filter
        var actResult = await agent.ActAsync([
            new AgentActStep("project name", "setValue", ["Atlas"]),
            new AgentActStep("region", "setValue", ["eu-west"]),
            new AgentActStep("API Secret", "setValue", ["sk_live_new_999"]),
            new AgentActStep("Year", "setValue", ["2025"])
        ]);

        Assert.True(actResult.Ok);
        Assert.Equal("Atlas", form.GetValue<string>("projectName"));
        Assert.Equal("eu-west", form.GetValue<string>("region"));
        Assert.Equal("sk_live_new_999", form.GetValue<string>("apiToken"));
        Assert.Equal("2025", dashboard.Controls[0].Value);

        Assert.Equal(4, observed.Count);
        Assert.Equal(AgentChangeSource.Agent, observed[0].Source);
        Assert.Equal("Apollo", observed[0].Previous);
        Assert.Equal("Atlas", observed[0].Value);

        // Password change in observed stream must have both Previous and Value redacted
        var secretChange = observed[2];
        Assert.Equal(AgentBridgeService.RedactedValue, secretChange.Previous);
        Assert.Equal(AgentBridgeService.RedactedValue, secretChange.Value);

        // 5. Permission gate blocks consequential confirm-required step when policy returns false
        var denied = await agent.ActAsync(
            [new AgentActStep("Project Settings", "submit")],
            allow: (_, confirmRequired) => Task.FromResult(!confirmRequired));
        Assert.False(denied.Ok);
        Assert.Contains("refused", denied.Error);
    }

    [Fact]
    public void UndoStack_records_undoes_and_redoes_state_snapshots()
    {
        var stack = new UndoStack<string>("v1", limit: 3);
        Assert.False(stack.CanUndo);

        stack.Record("v2");
        stack.Record("v3");
        Assert.Equal("v3", stack.Current);
        Assert.True(stack.CanUndo);

        Assert.Equal("v2", stack.Undo());
        Assert.True(stack.CanRedo);
        Assert.Equal("v3", stack.Redo());
    }
}
