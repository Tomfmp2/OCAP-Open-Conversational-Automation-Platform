using Microsoft.EntityFrameworkCore;
using OCAP.Infrastructure.Events.Distributed;
using OCAP.Infrastructure.Persistence.Context;
using Xunit;

namespace OCAP.IntegrationTests;

public class EfInboxStoreTests
{
    [Fact]
    public async Task TryClaimAsync_SameMessageFromAnotherContext_IsRejected()
    {
        // Dos contextos = dos peticiones (o dos réplicas) que reciben el mismo reintento del webhook.
        var options = new DbContextOptionsBuilder<OCAPDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var first = new OCAPDbContext(options);
        await using var second = new OCAPDbContext(options);

        Assert.True(await new EfInboxStore(first).TryClaimAsync("telegram-update:1", "channel-webhook"));
        Assert.False(await new EfInboxStore(second).TryClaimAsync("telegram-update:1", "channel-webhook"));
        Assert.True(await new EfInboxStore(second).TryClaimAsync("telegram-update:1", "otro-grupo"));
    }
}
