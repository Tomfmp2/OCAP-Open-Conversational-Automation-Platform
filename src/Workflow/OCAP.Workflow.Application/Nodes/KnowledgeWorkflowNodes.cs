using OCAP.Intelligence.Abstractions;
using OCAP.Knowledge.Abstractions;
using OCAP.Knowledge.Application.Services;
using OCAP.Knowledge.Domain.Enums;
using OCAP.Workflow.Abstractions;
using OCAP.Workflow.Domain.Entities;
using OCAP.Workflow.Domain.Enums;

namespace OCAP.Workflow.Application.Nodes;

public class KnowledgeSearchNode : IWorkflowNode
{
    private readonly IKnowledgeRetriever _retriever;

    public KnowledgeSearchNode(IKnowledgeRetriever retriever)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.KnowledgeSearch;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var results = await _retriever.SearchAsync(context.TenantId, null, "Búsqueda en base de conocimiento", SearchStrategyType.Keyword, 5, 0.5, cancellationToken);
        return new WorkflowStepResult(true, "next", $"{{\"searchCount\": {results.Count}}}");
    }
}

public class SemanticSearchNode : IWorkflowNode
{
    private readonly IKnowledgeRetriever _retriever;

    public SemanticSearchNode(IKnowledgeRetriever retriever)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.SemanticSearch;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var results = await _retriever.SearchAsync(context.TenantId, null, "Búsqueda semántica vectorial", SearchStrategyType.Semantic, 5, 0.5, cancellationToken);
        return new WorkflowStepResult(true, "next", $"{{\"semanticMatches\": {results.Count}}}");
    }
}

public class RetrieveContextNode : IWorkflowNode
{
    private readonly IKnowledgeRetriever _retriever;

    public RetrieveContextNode(IKnowledgeRetriever retriever)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.RetrieveContext;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var results = await _retriever.SearchAsync(context.TenantId, null, "Recuperar contexto RAG", SearchStrategyType.Hybrid, 3, 0.5, cancellationToken);
        var contextText = string.Join("\n\n", results.Select(r => r.Content));
        return new WorkflowStepResult(true, "next", $"{{\"retrievedContextLength\": {contextText.Length}}}");
    }
}

public class AskKnowledgeBaseNode : IWorkflowNode
{
    private readonly IKnowledgeRetriever _retriever;
    private readonly IAiProviderSelector _aiSelector;

    public AskKnowledgeBaseNode(IKnowledgeRetriever retriever, IAiProviderSelector aiSelector)
    {
        _retriever = retriever ?? throw new ArgumentNullException(nameof(retriever));
        _aiSelector = aiSelector ?? throw new ArgumentNullException(nameof(aiSelector));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.AskKnowledgeBase;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        var query = "Consulta sobre la base de conocimiento";
        var docs = await _retriever.SearchAsync(context.TenantId, null, query, SearchStrategyType.Hybrid, 3, 0.5, cancellationToken);
        var ragContext = string.Join("\n", docs.Select(d => d.Content));

        var req = new AiRequest { UserMessage = $"Con base en este contexto:\n{ragContext}\n\nResponde a: {query}" };
        var res = await _aiSelector.ExecuteWithFailoverAsync(req, cancellationToken);

        return new WorkflowStepResult(true, "next", $"{{\"answer\": \"{res.GeneratedText}\"}}");
    }
}

public class DocumentUploadNode : IWorkflowNode
{
    private readonly KnowledgeService _knowledgeService;

    public DocumentUploadNode(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.DocumentUpload;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("Contenido simulado de subida en workflow."));
        var doc = await _knowledgeService.UploadDocumentAsync(context.TenantId, Guid.NewGuid(), stream, "workflow_upload.txt", DocumentType.Txt, DocumentCategory.General, "WorkflowNode", cancellationToken);
        return new WorkflowStepResult(true, "next", $"{{\"uploadedDocumentId\": \"{doc.Id}\"}}");
    }
}

public class ReindexNode : IWorkflowNode
{
    private readonly KnowledgeService _knowledgeService;

    public ReindexNode(KnowledgeService knowledgeService)
    {
        _knowledgeService = knowledgeService ?? throw new ArgumentNullException(nameof(knowledgeService));
    }

    public WorkflowNodeType NodeType => WorkflowNodeType.Reindex;

    public async Task<WorkflowStepResult> ExecuteAsync(WorkflowStep step, WorkflowContext context, CancellationToken cancellationToken = default)
    {
        await _knowledgeService.ReindexAsync(context.TenantId, Guid.NewGuid(), cancellationToken);
        return new WorkflowStepResult(true, "next", "{\"reindexed\": true}");
    }
}
