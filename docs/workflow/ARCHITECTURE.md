# OCAP — Arquitectura del Motor de Workflows

## Visión General
El módulo `OCAP.Workflow` implementa un motor de automatización de procesos empresariales declarativo y desacoplado, diseñado bajo los principios de Arquitectura Hexagonal y DDD.

## Estructura Modular
- `OCAP.Workflow.Domain`: Entidades DDD puras (`Workflow`, `WorkflowDefinition`, `WorkflowExecution`, `WorkflowStep`, `WorkflowTransition`).
- `OCAP.Workflow.Abstractions`: Abstracciones agnósticas (`IWorkflowNode`, `IWorkflowEngine`, `WorkflowStepResult`).
- `OCAP.Workflow.Application`: Implementación del motor (`WorkflowEngine`), tipos de nodos y máquina de estados.
- `OCAP.Workflow.Infrastructure`: Persistencia en EF Core y PostgreSQL.
