namespace Blux.UI.Tools.Tests;

/// <summary>The rules of the set of ticked rows, without a renderer.</summary>
public class DataListSelectionEngineTests
{
    private static KeyValuePair<string, string> Row(string key) => new(key, "row " + key);

    [Fact]
    public void Ticking_reports_a_change_once_and_unticking_what_is_not_ticked_reports_none()
    {
        var selection = new DataListSelection<string>();

        Assert.True(selection.Set("1", "one", true));
        Assert.False(selection.Set("1", "one again", true));
        Assert.False(selection.Set("2", "two", false));
        Assert.True(selection.Set("1", "one", false));
        Assert.Equal(0, selection.Count);
    }

    [Fact]
    public void Many_rows_tick_and_untick_with_one_answer()
    {
        var selection = new DataListSelection<string>();

        Assert.True(selection.SetMany([Row("1"), Row("2"), Row("3")], true));
        Assert.False(selection.SetMany([Row("2"), Row("3")], true));
        Assert.True(selection.SetMany([Row("1"), Row("9")], false));
        Assert.Equal(["2", "3"], selection.Keys.Order());
    }

    [Fact]
    public void Replacing_keeps_the_rows_already_known_and_waits_for_the_others()
    {
        var selection = new DataListSelection<string>();
        selection.Set("1", "one", true);
        selection.Set("2", "two", true);

        Assert.True(selection.Replace(["2", "3"]));

        Assert.Equal(["2", "3"], selection.Keys.Order());
        Assert.Equal(["two"], selection.Items());
        Assert.False(selection.Replace(["3", "2"]));
    }

    [Fact]
    public void Retaining_drops_the_rows_that_are_not_valid_and_refreshes_the_ones_that_are()
    {
        var selection = new DataListSelection<string>();
        selection.Replace(["1", "2", "3"]);

        var removed = selection.Retain(new Dictionary<string, string> { ["2"] = "fresh two", ["3"] = "fresh three", ["4"] = "four" });

        Assert.True(removed);
        Assert.Equal(["2", "3"], selection.Keys.Order());
        Assert.Equal(["fresh three", "fresh two"], selection.Items().Order());
        Assert.False(selection.Retain(new Dictionary<string, string> { ["2"] = "x", ["3"] = "y" }));
    }

    [Fact]
    public void A_snapshot_does_not_follow_later_changes()
    {
        var selection = new DataListSelection<string>();
        selection.Set("1", "one", true);
        var snapshot = selection.Snapshot();

        selection.Set("2", "two", true);
        selection.Clear();

        Assert.Equal(["1"], snapshot);
        Assert.False(selection.Clear());
    }
}
