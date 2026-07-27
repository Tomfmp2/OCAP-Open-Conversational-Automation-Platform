# OCAP — Catálogo de Nodos del Workflow Engine

## Nodos Soportados
- **Start / End**: Control inicial y final del flujo.
- **Condition / Switch**: Bifurcación condicional evaluada en contexto.
- **LLM**: Invocación de proveedores de IA Generativa.
- **Tool**: Ejecución de herramientas registradas en `IToolRegistry`.
- **Delay / Wait**: Retardo de tiempo y espera de señales.
- **HumanApproval**: Solicitud de intervención y aprobación manual.
- **Loop**: Iteración sobre colecciones de datos.
- **Parallel / Merge**: Ejecución y convergencia de tareas concurrentes.
- **Webhook / ApiRequest**: Disparo de endpoints e integración HTTP externa.
- **Script / SubWorkflow / ErrorHandler**: Lógica personalizada, flujos anidados y manejo de fallos.
