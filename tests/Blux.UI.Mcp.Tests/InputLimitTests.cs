using System.Diagnostics;
using ModelContextProtocol;

namespace Blux.UI.Mcp.Tests;

public class InputLimitTests
{
    // A long query must cost about the same as a short one: the work is bounded, not proportional to the input.
    private static string Long(int words) => "modal " + string.Join(' ', Enumerable.Range(0, words).Select(i => "word" + i));

    [Fact]
    public void A_400_word_query_is_answered_fast_and_still_finds_the_component()
    {
        var query = Long(400);
        Assert.True(query.Length > 2000);
        TestSupport.Service.SearchComponents("warmup");

        var watch = Stopwatch.StartNew();
        var hits = TestSupport.Service.SearchComponents(query, null, 5);
        watch.Stop();

        Assert.True(watch.ElapsedMilliseconds < 500, $"took {watch.ElapsedMilliseconds} ms");
        Assert.Contains(hits, h => h.Item.Slug == "modal");
    }

    [Fact]
    public void An_unknown_name_that_is_very_long_resolves_fast_through_the_tools()
    {
        var name = string.Join(' ', Enumerable.Range(0, 400).Select(i => "button" + i));
        TestSupport.Tools.SearchComponents("warmup");

        var watch = Stopwatch.StartNew();
        var error = Assert.Throws<McpException>(() => TestSupport.Tools.GetComponent(name));
        watch.Stop();

        Assert.True(watch.ElapsedMilliseconds < 500, $"took {watch.ElapsedMilliseconds} ms");
        Assert.Contains("No component named", error.Message);
    }

    [Fact]
    public void Close_components_of_a_huge_name_is_bounded()
    {
        var watch = Stopwatch.StartNew();
        TestSupport.Service.CloseComponents(new string((char)120, 3000) + " " + Long(300));
        watch.Stop();
        Assert.True(watch.ElapsedMilliseconds < 500, $"took {watch.ElapsedMilliseconds} ms");
    }
}
