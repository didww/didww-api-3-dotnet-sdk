using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class BalanceTest : BaseTest
{
    [Fact]
    public async Task TestFindBalance()
    {
        StubGet("balance", "balance/index.json");

        var response = await Client.Balance().FindAsync();
        var balance = response.Data;

        balance.Id.Should().Be("4c39e0bf-683b-4697-9322-5abaf4011883");
        balance.TotalBalance.Should().Be(60.0m);
        balance.Credit.Should().Be(10.0m);
        balance.BalanceAmount.Should().Be(50.0m);
    }
}
