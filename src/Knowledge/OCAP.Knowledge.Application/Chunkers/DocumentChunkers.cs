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

    private const int MaxWordsPerChunk = 300;

    private static int WordCount(string text) => text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Parte un bloque largo por líneas y, si una línea sigue siendo larga, por palabras.</summary>
    private static IEnumerable<string> SplitLong(string block, int target)
    {
        if (WordCount(block) <= target) { yield return block; yield break; }
        var buffer = new List<string>();
        var words = 0;
        foreach (var line in block.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0))
        {
            var lineWords = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (lineWords.Length > target)
            {
                if (buffer.Count > 0) { yield return string.Join("\n", buffer); buffer.Clear(); words = 0; }
                for (var i = 0; i < lineWords.Length; i += target) yield return string.Join(' ', lineWords.Skip(i).Take(target));
                continue;
            }
            if (words + lineWords.Length > target && buffer.Count > 0) { yield return string.Join("\n", buffer); buffer.Clear(); words = 0; }
            buffer.Add(line);
            words += lineWords.Length;
        }
        if (buffer.Count > 0) yield return string.Join("\n", buffer);
    }

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

        // Fragmentos de tamaño útil: los párrafos cortos (líneas de Word, filas de Excel) se juntan y los largos (páginas
        // de PDF) se parten por líneas o palabras. Fragmentos más chicos dan respuestas más precisas.
        var target = Math.Max(50, Math.Min(chunkSize, MaxWordsPerChunk));
        var pieces = content.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .SelectMany(p => SplitLong(p, target));

        var current = new List<string>();
        var currentWords = 0;
        var index = 0;
        var position = 0;
        void Flush()
        {
            if (current.Count == 0) return;
            var text = string.Join("\n\n", current);
            chunks.Add(new KnowledgeChunk(
                Guid.NewGuid(), documentId, knowledgeBaseId, tenantId, index++, text,
                currentWords, position, position + text.Length,
                JsonSerializer.Serialize(new { Strategy = Strategy.ToString() })));
            position += text.Length + 2;
            current.Clear();
            currentWords = 0;
        }

        foreach (var piece in pieces)
        {
            var words = WordCount(piece);
            if (currentWords > 0 && currentWords + words > target) Flush();
            current.Add(piece);
            currentWords += words;
        }
        Flush();

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
