namespace OCAP.Knowledge.Domain.ValueObjects;

public record EmbeddingVector(
    Guid ChunkId,
    string Provider,
    string Model,
    int Dimensions,
    float[] Values
);
