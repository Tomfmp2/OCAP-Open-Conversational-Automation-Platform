using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Abstractions;

public interface IChunker
{
    ChunkingStrategy Strategy { get; }

    List<KnowledgeChunk> ChunkDocument(
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50);

    List<KnowledgeChunk> ChunkDocument(
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50) => ChunkDocument(content, chunkSize, overlap, maxTokens, minTokens);
}
