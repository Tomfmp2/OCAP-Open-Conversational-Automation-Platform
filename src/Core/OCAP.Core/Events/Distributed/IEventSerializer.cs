namespace OCAP.Core.Events.Distributed;

// Contrato de serialización/deserialización para el bus de eventos distribuido (CAP-20).
public interface IEventSerializer
{
    string Serialize<T>(T obj);
    T Deserialize<T>(string payload);
    object? Deserialize(string payload, Type type);
}
