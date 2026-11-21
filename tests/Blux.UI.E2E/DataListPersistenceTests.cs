namespace Blux.UI.E2E;

/// <summary>
/// What a prerendered data list hands to the interactive render: a small result is adopted (the provider of "list-url" is never called), a large one (the first page of "list-large", 300 rows)
/// is left out, because everything persisted travels to the server when a Server circuit starts and SignalR refuses a message past 32 KB. The page must still turn interactive in every mode.
/// </summary>
public class DataListPersistenceTests(PlaygroundServer server) : PlaygroundTest(server)
{
    [Theory]
    [MemberData(nameof(AllModes))]
    public async Task A_large_first_result_does_not_break_the_circuit_and_small_results_are_still_adopted(string mode)
    {
        await Page.GotoAsync($"/{mode}/components/data-list");
        await Page.WaitForFunctionAsync("() => !!customElements.get('bx-popover')");
        if (mode != "ssr")
        {
            await Assertions.Expect(Page.GetByTestId("renderer")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 30_000 });
        }
        await Assertions.Expect(Page.Locator("[data-slot=skeleton-row]")).ToHaveCountAsync(0, new() { Timeout = 15_000 });
        var large = Page.GetByTestId("list-large");

        // The big list shows its 300 rows, prerendered and after the interactive start.
        await Assertions.Expect(large.Locator("tbody tr[data-slot=row]")).ToHaveCountAsync(300);
        await Assertions.Expect(large.Locator("[data-slot=result-count]")).ToHaveTextAsync("1–300 of 600");
        // The small list of the page kept its result: the interactive render did not call its provider (the static render calls it once).
        await Assertions.Expect(Page.GetByTestId("url-calls")).ToHaveTextAsync(mode == "ssr" ? "1" : "0");

        if (mode != "ssr")
        {
            // The circuit (or the WebAssembly runtime) is alive: a sort runs.
            await large.GetByRole(AriaRole.Button, new() { Name = "Sort by No., descending" }).ClickAsync();
            await Assertions.Expect(large.Locator("tbody tr[data-slot=row]").First.Locator("td").First).ToHaveTextAsync("600");
            await Assertions.Expect(Page.GetByTestId("url-calls")).ToHaveTextAsync("0");
        }
    }
}
