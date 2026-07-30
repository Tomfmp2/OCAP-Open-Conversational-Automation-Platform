# OCAP — Arquitectura del Motor de Workflows

## Visión General
El módulo `OCAP.Workflow` implementa un motor de automatización declarativo bajo Clean Architecture / Hexagonal / DDD.

## Estructura Modular
- `OCAP.Workflow.Domain`: entidades (`WorkflowDefinition`, `WorkflowExecution`, `WorkflowStep`, `WorkflowTransition`, `WorkflowVariable`, `WorkflowVersion`).
- `OCAP.Workflow.Abstractions`: puertos (`IWorkflowEngine`, `IWorkflowNodeExecutor`, `IWorkflowExpressionEvaluator`, `IWorkflowDatabaseExecutor`, `IWorkflowEmailSender`, `IWorkflowScheduler`, repositorios).
- `OCAP.Workflow.Application`: `WorkflowEngine`, evaluador de expresiones, ejecutores de nodos, validación/designer mapping.
- `OCAP.Workflow.Infrastructure`: repositorios EF, adaptadores DB/Email, scheduler hosted service.
- `OCAP.Workflow.Designer`: contratos visuales del builder.

## Runtime
El engine orquesta nodos registrados por `WorkflowNodeType`, persiste estado/historial/variables, soporta retry/timeout/compensation/resume por señal y versionado de definiciones.
