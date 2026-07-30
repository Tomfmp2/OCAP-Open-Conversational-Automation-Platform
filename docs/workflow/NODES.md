# OCAP — Catálogo de Nodos del Workflow Engine

Estado alineado con el código de producción en `OCAP.Workflow.Application.Nodes`.

## Control de flujo
- **Start / End**: inicio y finalización de ejecución.
- **Condition / Switch**: evaluación segura de expresiones (`IWorkflowExpressionEvaluator`) y enrutamiento por rama.
- **Loop / ForEach**: iteración condicionada o sobre colecciones con límite `maxIterations`.
- **Parallel / Merge**: ejecución concurrente de ramas y consolidación de resultados (`parallelResults`).
- **Delay**: retardo inline (≤30s) o pausa programada (`WaitUntilUtc` + señal `__delay__`).
- **Wait**: pausa hasta señal externa; `ResumeWithSignalAsync` reanuda el flujo.
- **HumanApproval**: pausa de aprobación; señales `approved` / `rejected`.
- **ErrorHandler**: ruta de captura cuando un paso falla.

## Integraciones
- **LLM**: prompt/system interpolados vía `IAiProviderSelector`.
- **Agent**: ejecución de agente con `IAgentRuntime`.
- **Tool**: resolución por nombre en `IToolRegistry`.
- **ApiRequest / Webhook**: HTTP real con timeout, headers y fallos configurables.
- **Database**: solo `SELECT` parametrizado (`IWorkflowDatabaseExecutor`), con `@tenantId`.
- **Email**: envío vía `IWorkflowEmailSender`.
- **VariableAssign / Script**: asignación de variables con interpolación `{{var}}`.
- **SubWorkflow**: arranque anidado de otra definición (scope factory).

## Knowledge
- **KnowledgeSearch / SemanticSearch / RetrieveContext / AskKnowledgeBase / DocumentUpload / Reindex**: leen `query`, `knowledgeBaseId`, `topK`, `minScore` desde `ConfigurationJson`.

## Runtime por paso (ConfigurationJson)
Cualquier nodo puede incluir:
```json
{
  "retryCount": 3,
  "retryDelayMs": 200,
  "retryOnFailure": true,
  "timeoutSeconds": 30,
  "compensationStepId": "compensate_step"
}
```
