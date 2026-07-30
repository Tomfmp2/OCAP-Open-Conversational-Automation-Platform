namespace OCAP.Knowledge.Abstractions;

public sealed class KnowledgeOptions
{
    public const string SectionName = "Knowledge";

    /// <summary>
    /// When true, uses in-memory repositories and vector store (Development/Testing only).
    /// Production must keep this false.
    /// </summary>
    public bool UseInMemory { get; set; }

    /// <summary>
    /// Vector backend: PgVector (default) or InMemory.
    /// </summary>
    public string VectorStore { get; set; } = "PgVector";

    /// <summary>
    /// Expected embedding dimensionality for the PgVector column.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    public string DefaultEmbeddingProvider { get; set; } = "OpenAI";

    public string DefaultEmbeddingModel { get; set; } = "text-embedding-3-small";

    public EmbeddingProviderOptions OpenAI { get; set; } = new()
    {
        BaseUrl = "https://api.openai.com/v1",
        DefaultModel = "text-embedding-3-small"
    };

    public EmbeddingProviderOptions Gemini { get; set; } = new()
    {
        BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
        DefaultModel = "text-embedding-004"
    };

    public EmbeddingProviderOptions Ollama { get; set; } = new()
    {
        BaseUrl = "http://localhost:11434",
        DefaultModel = "nomic-embed-text"
    };

    public bool IsInMemoryVectorStore =>
        UseInMemory || string.Equals(VectorStore, "InMemory", StringComparison.OrdinalIgnoreCase);
}

public sealed class EmbeddingProviderOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string DefaultModel { get; set; } = string.Empty;
}
