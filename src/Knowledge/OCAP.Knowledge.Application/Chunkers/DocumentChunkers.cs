using System.Text.Json;
using System.Text.RegularExpressions;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Domain.Entities;
using OCAP.Knowledge.Domain.Enums;

namespace OCAP.Knowledge.Application.Chunkers;

public class SentenceChunker : IChunker
{
    public ChunkingStrategy Strategy => ChunkingStrategy.Sentence;

    public List<KnowledgeChunk> ChunkDocument(
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50) => ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), content, chunkSize, overlap, maxTokens, minTokens);

    public List<KnowledgeChunk> ChunkDocument(
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50)
    {
        var chunks = new List<KnowledgeChunk>();
        if (string.IsNullOrWhiteSpace(content)) return chunks;

        var sentences = Regex.Split(content, @"(?<=[.!?])\s+");
        var currentChunkText = new System.Text.StringBuilder();
        int index = 0;
        int startChar = 0;

        foreach (var sentence in sentences)
        {
            if (currentChunkText.Length + sentence.Length > chunkSize && currentChunkText.Length >= minTokens)
            {
                var text = currentChunkText.ToString().Trim();
                int tokenCount = EstimateTokens(text);
                chunks.Add(new KnowledgeChunk(
                    Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index++, text,
                    tokenCount, startChar, startChar + text.Length,
                    JsonSerializer.Serialize(new { Strategy = Strategy.ToString() })
                ));

                startChar += Math.Max(0, text.Length - overlap);
                currentChunkText.Clear();
            }

            currentChunkText.Append(sentence).Append(" ");
        }

        if (currentChunkText.Length > 0)
        {
            var text = currentChunkText.ToString().Trim();
            int tokenCount = EstimateTokens(text);
            chunks.Add(new KnowledgeChunk(
                Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index, text,
                tokenCount, startChar, startChar + text.Length,
                JsonSerializer.Serialize(new { Strategy = Strategy.ToString() })
            ));
        }

        return chunks;
    }

    private static int EstimateTokens(string text) => text.Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
}

public class ParagraphChunker : IChunker
{
    public ChunkingStrategy Strategy => ChunkingStrategy.Paragraph;

    public List<KnowledgeChunk> ChunkDocument(
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50) => ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), content, chunkSize, overlap, maxTokens, minTokens);

    public List<KnowledgeChunk> ChunkDocument(
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50)
    {
        var chunks = new List<KnowledgeChunk>();
        if (string.IsNullOrWhiteSpace(content)) return chunks;

        var paragraphs = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        int currentPos = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            int tokenCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            chunks.Add(new KnowledgeChunk(
                Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index++, trimmed,
                tokenCount, currentPos, currentPos + trimmed.Length,
                JsonSerializer.Serialize(new { Strategy = Strategy.ToString() })
            ));

            currentPos += trimmed.Length + 2;
        }

        return chunks;
    }
}

public class SemanticChunker : IChunker
{
    public ChunkingStrategy Strategy => ChunkingStrategy.Semantic;

    public List<KnowledgeChunk> ChunkDocument(
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50) => ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), content, chunkSize, overlap, maxTokens, minTokens);

    public List<KnowledgeChunk> ChunkDocument(
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50)
    {
        var chunks = new List<KnowledgeChunk>();
        if (string.IsNullOrWhiteSpace(content)) return chunks;

        // Group by Markdown headers / section boundaries
        var sections = Regex.Split(content, @"(?=(?:\r?\n#+\s+))");
        int index = 0;
        int currentPos = 0;

        foreach (var section in sections)
        {
            var trimmed = section.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            int tokenCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            chunks.Add(new KnowledgeChunk(
                Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index++, trimmed,
                tokenCount, currentPos, currentPos + trimmed.Length,
                JsonSerializer.Serialize(new { Strategy = Strategy.ToString(), Type = "SemanticSection" })
            ));

            currentPos += trimmed.Length;
        }

        return chunks;
    }
}

public class SlidingWindowChunker : IChunker
{
    public ChunkingStrategy Strategy => ChunkingStrategy.SlidingWindow;

    public List<KnowledgeChunk> ChunkDocument(
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50) => ChunkDocument(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), content, chunkSize, overlap, maxTokens, minTokens);

    public List<KnowledgeChunk> ChunkDocument(
        Guid documentId,
        Guid knowledgeBaseId,
        Guid tenantId,
        string content,
        int chunkSize = 500,
        int overlap = 50,
        int maxTokens = 1000,
        int minTokens = 50)
    {
        var chunks = new List<KnowledgeChunk>();
        if (string.IsNullOrWhiteSpace(content)) return chunks;

        int step = Math.Max(10, chunkSize - overlap);
        int index = 0;

        for (int i = 0; i < content.Length; i += step)
        {
            int length = Math.Min(chunkSize, content.Length - i);
            var chunkText = content.Substring(i, length).Trim();
            if (chunkText.Length == 0) continue;

            int tokenCount = chunkText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            chunks.Add(new KnowledgeChunk(
                Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index++, chunkText,
                tokenCount, i, i + length,
                JsonSerializer.Serialize(new { Strategy = Strategy.ToString(), Step = step })
            ));
        }

        return chunks;
    }
}

public class ChunkerFactory : IChunkerFactory
{
    private readonly IEnumerable<IChunker> _chunkers;

    public ChunkerFactory(IEnumerable<IChunker> chunkers)
    {
        _chunkers = chunkers ?? throw new ArgumentNullException(nameof(chunkers));
    }

    public IChunker GetChunker(ChunkingStrategy strategy)
    {
        var chunker = _chunkers.FirstOrDefault(c => c.Strategy == strategy);
        return chunker ?? new ParagraphChunker();
    }
}
