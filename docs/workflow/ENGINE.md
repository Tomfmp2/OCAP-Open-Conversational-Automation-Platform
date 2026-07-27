# OCAP — Especificación del Motor de Ejecución (IWorkflowEngine)

## Capacidades del Motor
El `WorkflowEngine` gestiona el ciclo de vida completo de ejecución de los flujos de trabajo:
1. **Inicio de Ejecución**: `StartWorkflowAsync` inicializa variables y arranca el ciclo.
2. **Pausa & Reanudación**: `PauseWorkflowAsync` y `ResumeWorkflowAsync` permiten intervenciones humanas.
3. **Cancelación Segura**: `CancelWorkflowAsync` detiene la ejecución inmediatamente.
4. **Persistencia & Reintentos**: Auditoría e historial en `WorkflowExecutionHistory`.
