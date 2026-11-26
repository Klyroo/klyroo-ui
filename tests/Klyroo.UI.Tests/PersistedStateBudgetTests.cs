using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Klyroo.UI.Tests;

public sealed class PersistedStateBudgetTests
{
    [Fact]
    public void The_libraries_of_a_scope_share_one_budget()
    {
        var services = new ServiceCollection().AddKlyrooUi(o => o.PersistedStateMaxBytes = 100).BuildServiceProvider();
        using var scope = services.CreateScope();
        var budget = scope.ServiceProvider.GetRequiredService<KlyPersistedStateBudget>();

        Assert.True(budget.TryReserve(60));
        Assert.False(budget.TryReserve(50));
        Assert.Equal(60, budget.UsedBytes);
        Assert.True(budget.TryReserve(40));
        Assert.Same(budget, scope.ServiceProvider.GetRequiredService<KlyPersistedStateBudget>());

        // Another request or circuit starts from zero.
        using var other = services.CreateScope();
        Assert.Equal(0, other.ServiceProvider.GetRequiredService<KlyPersistedStateBudget>().UsedBytes);
    }

    [Fact]
    public void The_sent_size_counts_the_escaping_and_both_encodings()
    {
        var plain = KlyPersistedStateBudget.SentBytes("k", "abcdefghi");
        var quoted = KlyPersistedStateBudget.SentBytes("k", "\"\"\"\"\"\"\"\"\"");

        // Nine letters escape to nine bytes plus the quotes; nine quotes escape to six bytes each (").
        Assert.InRange(plain, 11 * 16 / 9, 11 * 16 / 9 + 12);
        Assert.InRange(quoted, 56 * 16 / 9, 56 * 16 / 9 + 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => new KlyrooOptions().PersistedStateMaxBytes = -1);
    }
}
