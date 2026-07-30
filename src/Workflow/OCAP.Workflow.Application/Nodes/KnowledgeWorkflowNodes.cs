using System.Text.Json;
using OCAP.Intelligence.Abstractions;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Application.Expressions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Nodes;

public class KnowledgeSearchNode : IWorkflowNodeExecutor
{
    private readonly IKnowledgeRetriever _retriever;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public KnowledgeSearchNode(IKnowledgeRetriever retriever, IWorkflowExpressionEvaluator evaluator)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.KnowledgeSearch;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<KnowledgeNodeConfig>(step.ConfigurationJson);
        var query = _evaluator.Interpolate(config.Query ?? string.Empty, context.Variables);
        Guid? kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : null;
        var topK = config.TopK > 0 ? config.TopK : 5;
        var minScore = config.MinScore > 0 ? config.MinScore : 0.5;

        var results = await _retriever.SearchAsync(context.TenantId, kbId, query, SearchStrategyType.Keyword, topK, minScore, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { searchCount = results.Count, query, results }));
    }
}

public class SemanticSearchNode : IWorkflowNodeExecutor
{
    private readonly IKnowledgeRetriever _retriever;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public SemanticSearchNode(IKnowledgeRetriever retriever, IWorkflowExpressionEvaluator evaluator)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.SemanticSearch;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<KnowledgeNodeConfig>(step.ConfigurationJson);
        var query = _evaluator.Interpolate(config.Query ?? string.Empty, context.Variables);
        Guid? kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : null;
        var topK = config.TopK > 0 ? config.TopK : 5;
        var minScore = config.MinScore > 0 ? config.MinScore : 0.5;

        var results = await _retriever.SearchAsync(context.TenantId, kbId, query, SearchStrategyType.Semantic, topK, minScore, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { semanticMatches = results.Count, query, results }));
    }
}

public class RetrieveContextNode : IWorkflowNodeExecutor
{
    private readonly IKnowledgeRetriever _retriever;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public RetrieveContextNode(IKnowledgeRetriever retriever, IWorkflowExpressionEvaluator evaluator)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.RetrieveContext;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<KnowledgeNodeConfig>(step.ConfigurationJson);
        var query = _evaluator.Interpolate(config.Query ?? string.Empty, context.Variables);
        Guid? kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : null;
        var topK = config.TopK > 0 ? config.TopK : 3;
        var minScore = config.MinScore > 0 ? config.MinScore : 0.5;

        var results = await _retriever.SearchAsync(context.TenantId, kbId, query, SearchStrategyType.Hybrid, topK, minScore, cancellationToken);
        var contextText = string.Join("\n\n", results.Select(r => r.Content));
        context.Variables["retrievedContext"] = contextText;
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { retrievedContextLength = contextText.Length, query }));
    }
}

public class AskKnowledgeBaseNode : IWorkflowNodeExecutor
{
    private readonly IKnowledgeRetriever _retriever;
    private readonly IAiProviderSelector _aiSelector;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public AskKnowledgeBaseNode(IKnowledgeRetriever retriever, IAiProviderSelector aiSelector, IWorkflowExpressionEvaluator evaluator)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _aiSelector = aiSelector ?? throw new ArgumentNullException(nameof(aiSelector));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.AskKnowledgeBase;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<KnowledgeNodeConfig>(step.ConfigurationJson);
        var query = _evaluator.Interpolate(config.Query ?? string.Empty, context.Variables);
        Guid? kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : null;
        var topK = config.TopK > 0 ? config.TopK : 3;
        var minScore = config.MinScore > 0 ? config.MinScore : 0.5;

        var docs = await _retriever.SearchAsync(context.TenantId, kbId, query, SearchStrategyType.Hybrid, topK, minScore, cancellationToken);
        var ragContext = string.Join("\n", docs.Select(d => d.Content));

        var req = new AiRequest { UserMessage = $"Con base en este contexto:\n{ragContext}\n\nResponde a: {query}" };
        var res = await _aiSelector.ExecuteWithFailoverAsync(req, cancellationToken);

        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { answer = res.GeneratedText, query }));
    }
}

public class DocumentUploadNode : IWorkflowNodeExecutor
{
    private readonly KnowledgeService _knowledgeService;
    private readonly IWorkflowExpressionEvaluator _evaluator;

    public DocumentUploadNode(KnowledgeService knowledgeService, IWorkflowExpressionEvaluator evaluator)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.DocumentUpload;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<DocumentUploadNodeConfig>(step.ConfigurationJson);
        var content = _evaluator.Interpolate(config.Content ?? "Contenido de documento desde workflow.", context.Variables);
        var fileName = _evaluator.Interpolate(config.FileName ?? "workflow_upload.txt", context.Variables);
        Guid? kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : Guid.NewGuid();

        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
        var doc = await _knowledgeService.UploadDocumentAsync(
            context.TenantId, kbId.Value, stream, fileName,
            DocumentType.Txt, DocumentCategory.General, "WorkflowNode", cancellationToken);

        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { uploadedDocumentId = doc.Id, fileName }));
    }
}

public class ReindexNode : IWorkflowNodeExecutor
{
    private readonly KnowledgeService _knowledgeService;

    public ReindexNode(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Reindex;

    public async Task<NodeExecutionResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var config = NodeConfiguration.Deserialize<ReindexNodeConfig>(step.ConfigurationJson);
        Guid kbId = Guid.TryParse(config.KnowledgeBaseId, out var parsed) ? parsed : Guid.NewGuid();

        await _knowledgeService.ReindexAsync(context.TenantId, kbId, cancellationToken);
        return new WorkflowStepResult(true, "next", JsonSerializer.Serialize(new { reindexed = true, knowledgeBaseId = kbId }));
    }
}

public class KnowledgeNodeConfig
{
    public string? Query { get; set; }
    public string? KnowledgeBaseId { get; set; }
    public int TopK { get; set; } = 5;
    public double MinScore { get; set; } = 0.5;
}

public class DocumentUploadNodeConfig
{
    public string? Content { get; set; }
    public string? FileName { get; set; }
    public string? KnowledgeBaseId { get; set; }
}

public class ReindexNodeConfig
{
    public string? KnowledgeBaseId { get; set; }
}
