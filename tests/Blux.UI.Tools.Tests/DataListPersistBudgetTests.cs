using Microsoft.Extensions.DependencyInjection;

namespace Blux.UI.Tools.Tests;

public class DataListPersistBudgetTests
{
    private static IServiceProvider Scope() => new ServiceCollection().BuildServiceProvider();

    [Fact]
    public void A_list_over_its_own_limit_is_refused_and_takes_nothing()
    {
        var scope = Scope();

        Assert.False(DataListPersistBudget.Reserve(scope, 20_000, 16_384, 24_576));
        Assert.True(DataListPersistBudget.Reserve(scope, 16_384, 16_384, 24_576));
    }

    [Fact]
    public void Lists_of_one_page_share_the_page_budget_in_the_order_they_persist()
    {
        var scope = Scope();

        Assert.True(DataListPersistBudget.Reserve(scope, 10_000, 16_384, 24_576));
        Assert.True(DataListPersistBudget.Reserve(scope, 10_000, 16_384, 24_576));
        Assert.False(DataListPersistBudget.Reserve(scope, 10_000, 16_384, 24_576));
        Assert.True(DataListPersistBudget.Reserve(scope, 4_000, 16_384, 24_576));
    }

    [Fact]
    public void Another_page_has_its_own_budget()
    {
        Assert.True(DataListPersistBudget.Reserve(Scope(), 16_000, 16_384, 24_576));
        Assert.True(DataListPersistBudget.Reserve(Scope(), 16_000, 16_384, 24_576));
    }

    [Fact]
    public void The_limits_default_to_16_and_8_KB_and_refuse_a_negative_value()
    {
        var options = new BluxToolsOptions();

        Assert.Equal(16 * 1024, options.PersistMaxBytes);
        Assert.Equal(8 * 1024, options.PersistPageMaxBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.PersistMaxBytes = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => options.PersistPageMaxBytes = -1);
    }
}
