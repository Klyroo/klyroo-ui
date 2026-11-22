using System.Text.RegularExpressions;

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

    public static TheoryData<string> DataListPages => new("data-list", "data-list-filters", "data-list-filter-panel", "data-list-facets", "data-list-selection");

    /// <summary>
    /// The state a prerendered page persists is what the client sends back to the server when a Server or Auto circuit starts, and SignalR refuses a message past 32 KB. The data list docs are split
    /// over several pages so that none of them comes near it: each state comment of a page (the text between the markers, encrypted and encoded) stays under 24 KB, three quarters of the limit.
    /// </summary>
    [Theory]
    [MemberData(nameof(DataListPages))]
    public async Task The_state_a_data_list_page_persists_stays_well_under_what_the_client_can_send_back(string slug)
    {
        foreach (var mode in new[] { "server", "auto" })
        {
            var response = await Page.APIRequest.GetAsync($"/{mode}/components/{slug}");
            Assert.Equal(200, response.Status);
            var html = await response.TextAsync();
            var comments = Regex.Matches(html, @"<!--Blazor-[A-Za-z-]*Component-State:([^>]*?)-->").Select(m => m.Groups[1].Length).ToList();

            Assert.NotEmpty(comments);
            Assert.True(comments.Max() < 24 * 1024, $"/{mode}/components/{slug} persists {string.Join(" and ", comments)} characters of state");
        }
    }
}
