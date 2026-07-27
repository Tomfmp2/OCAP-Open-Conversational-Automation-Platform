# OCAP — Panel de Administración (Dashboard Foundation)

## Descripción General

El **OCAP Dashboard** es una interfaz web SPA desarrollada en **Blazor WebAssembly (.NET 10)** para la administración centralizada y monitoreo operacional de la plataforma **Open Conversational Automation Platform**.

El Dashboard opera de manera totalmente desacoplada a través de la **API REST (API Gateway)** de OCAP sin conectarse directamente a la base de datos ni a capas de infraestructura interna.

---

## Secciones del Dashboard

### 1. Resumen Principal (Home)
- **Estado del Sistema**: Indicador en tiempo real de salud, agentes activos, canales conectados y ejecuciones de herramientas.
- **Métricas de Telemetría**: Tiempo de respuesta promedio (ms), porcentaje de éxito y mensajes procesados.
- **Lista de Ejecuciones Recientes**: Registro en tiempo real de invocaciones a herramientas externas.

### 2. Monitoreo de Conversaciones (`/conversations`)
- Monitoreo en tiempo real de los diálogos activos por canal (WhatsApp, WebChat).
- Identificación de usuarios, último mensaje intercambiado y estados de conversación.

### 3. Gestión de Agentes (`/agents`)
- Catálogo de agentes inteligentes configurados.
- Estado operacional (`Active`, `Inactive`, `Maintenance`) y lista de herramientas (Tools) asignadas.

### 4. Catálogo de Herramientas (`/tools`)
- Inspección de capacidades registradas en el `IToolRegistry`.
- Versión semántica, descripción y permisos de seguridad requeridos (`Calendar.Create`, `Gmail.Send`, `Sheets.Append`).

### 5. Integración con Google Workspace (`/integrations`)
- Estado de autenticación OAuth2 y cuenta empresarial vinculada.
- Lista de permisos concedidos (Scopes) y última sincronización UTC.
