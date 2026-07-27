namespace OCAP.Workflow.Domain.Enums;

// Estado del ciclo de vida de un Workflow o Ejecución.
public enum WorkflowStatus
{
    Draft = 0,
    Active = 1,
    Running = 2,
    Paused = 3,
    Completed = 4,
    Failed = 5,
    Cancelled = 6
}

// Catálogo de tipos de nodos soportados por el motor de workflow.
public enum WorkflowNodeType
{
    Start = 1,
    End = 2,
    Condition = 3,
    LLM = 4,
    Tool = 5,
    Delay = 6,
    Wait = 7,
    HumanApproval = 8,
    Loop = 9,
    Switch = 10,
    Parallel = 11,
    Merge = 12,
    Webhook = 13,
    ApiRequest = 14,
    Script = 15,
    SubWorkflow = 16,
    ErrorHandler = 17,
    KnowledgeSearch = 18,
    SemanticSearch = 19,
    RetrieveContext = 20,
    AskKnowledgeBase = 21,
    DocumentUpload = 22,
    Reindex = 23
}
