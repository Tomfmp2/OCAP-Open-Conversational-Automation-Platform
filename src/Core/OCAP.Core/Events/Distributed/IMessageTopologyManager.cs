namespace OCAP.Core.Events.Distributed;

// Gestor de topología de mensajería (Exchanges, Queues, Topics, Streams, DLX) (CAP-20).
public interface IMessageTopologyManager
{
    Task ProvisionTopologyAsync(string exchangeOrStreamName, string queueOrSubjectName, CancellationToken cancellationToken = default);
}
