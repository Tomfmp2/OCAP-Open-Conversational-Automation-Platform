using FluentAssertions;
using OCAP.Api.Services.Memory;

namespace OCAP.Api.Tests.Services;

public class MemoryBatchingTests
{
    [Fact]
    public void ABusyOrganization_DoesNotTakeTheWholePass_AndOthersKeepTheirTurn()
    {
        var busy = Guid.NewGuid();
        var quiet = Guid.NewGuid();
        var candidates = Enumerable.Range(0, 30).Select(_ => (Id: Guid.NewGuid(), TenantId: busy)).ToList();
        var late = (Id: Guid.NewGuid(), TenantId: quiet);
        candidates.Add(late);

        var batch = candidates.FairBatch(perTenant: 5, total: 20);

        batch.Count(c => c.TenantId == busy).Should().Be(5);
        batch.Should().Contain(late);
        batch.Take(5).Should().Equal(candidates.Take(5), "se conserva el orden: primero lo más antiguo");
    }

    [Fact]
    public void TheTotalPerPass_IsStillRespected()
    {
        var candidates = Enumerable.Range(0, 40).Select(_ => (Id: Guid.NewGuid(), TenantId: Guid.NewGuid())).ToList();

        candidates.FairBatch(perTenant: 5, total: 20).Should().HaveCount(20);
    }
}
