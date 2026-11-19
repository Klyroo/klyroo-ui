namespace Blux.UI.E2E;

public class AccessibilityTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(EveryPageInEveryMode))]
    public async Task Component_page_has_no_accessibility_violations(string component, string mode)
    {
        await Open(mode, component);

        await AssertNoAccessibilityViolations();
    }
}
