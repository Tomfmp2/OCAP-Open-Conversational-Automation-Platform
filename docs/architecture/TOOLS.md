# OCAP — Arquitectura del Sistema de Herramientas (Tool Execution Pipeline)

## Visión General

El sistema de herramientas de **OCAP** permite a los Agentes Conversacionales interactuar de manera segura y estandarizada con capacidades empresariales y servicios de terceros (Google Workspace, bases de datos, APIs de automatización).

Diseñado bajo la **Arquitectura Hexagonal**, el núcleo del Agent Engine permanece totalmente agnóstico de los proveedores externos.

---

## Ciclo de Vida de Ejecución de una Tool

```
Usuario
   │
   ▼
[ Incoming Channel Message ]
   │
   ▼
[ Agent Engine ]
   │
   ├──► Resolutor de Intenciones (IIntentResolver)
   │
   ├──► Acción Determinada (AgentAction)
   │
   ▼
[ ActionDispatcher ]
   │
   ├──► 1. Búsqueda de Herramienta (IToolRegistry)
   │
   ├──► 2. Validación de Permisos (IPermissionValidator & AgentPermissionPolicy)
   │
   ├──► 3. Construcción de Contexto Inmutable (ToolExecutionContext)
   │
   ├──► 4. Ejecución del Adaptador de Herramienta (ITool.ExecuteAsync)
   │
   ▼
[ Provider Externo (ej. Google Calendar / Gmail / Sheets) ]
   │
   ▼
[ ToolResult (Success, ErrorCode, Message, Data) ]
```

---

## Permisos y Seguridad (`OCAP.Security.Abstractions`)

Las herramientas definen permisos requeridos explícitos en su `ToolDefinition.RequiredPermissions` (ej. `"Calendar.Create"`, `"Gmail.Send"`).

El servicio `IPermissionValidator` garantiza que un agente solo ejecute herramientas para las cuales su `AgentPermissionPolicy` le otorgue acceso explícito:

- **Otorga autorización**: `policy.Allow("Calendar.Create");`
- **Bloquea ejecución**: `policy.Deny("Drive.Delete");`

---

## Pasos para Crear un Nuevo Plugin / Tool

1. **Definir el Contrato del Proveedor** en la capa Abstracciones (ej. `IInvoiceProvider`).
2. **Crear la Clase que Implemente `ITool`**:
   - Definir `ToolDefinition` con ID, nombre, descripción y `RequiredPermissions`.
   - Implementar `Task<ToolResult> ExecuteAsync(ToolExecutionContext context, CancellationToken cancellationToken)`.
3. **Registrar la Tool** en `IToolRegistry` durante el arranque de la aplicación.
