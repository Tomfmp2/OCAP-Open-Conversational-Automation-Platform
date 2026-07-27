using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Abstractions;

public interface IChunkerFactory
{
    IChunker GetChunker(ChunkingStrategy strategy);
}
