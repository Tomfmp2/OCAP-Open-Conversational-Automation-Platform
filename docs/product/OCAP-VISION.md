# OCAP — Visión del Producto e Identidad Estratégica

## 1. Definición del Producto

OCAP (Open Conversational Automation Platform) es una plataforma empresarial inteligente de gestión de agentes autónomos, donde un **Enterprise Assistant Agent** actúa como agente global encargado de comprender usuarios, gestionar capacidades empresariales, coordinar agentes especializados y ejecutar acciones autorizadas mediante herramientas y sistemas conectados.

OCAP no se define como un chatbot ni como un simple agregador de mensajería. Es la infraestructura de orquestación, gobernanza e inteligencia conversacional sobre la cual las organizaciones despliegan su fuerza laboral digital autónoma.

---

## 2. Agente Principal: Enterprise Assistant Agent

El **Enterprise Assistant Agent** es el núcleo conversacional y orquestador primario del sistema.

### Responsabilidades
- **Dominio Global:** Posee visibilidad y comprensión del ecosistema operativo empresarial.
- **Comprensión Contextual:** Procesa y analiza las solicitudes del usuario derivando su intención real.
- **Orquestación y Coordinación:** Identifica qué agente especializado o herramienta debe intervenir para resolver la solicitud.
- **Ejecución Autorizada:** Invoca acciones permitidas dentro del marco de seguridad del usuario autenticado.

### Invariantes y Restricciones
- **No creación autónoma no gobernada:** No genera ni aprovisiona agentes de forma automática sin configuración explícita.
- **Respeto a la Gobernanza:** No puede sobrepasar las políticas de seguridad ni los permisos asignados al usuario o al tenant.
- **Validación Obligatoria:** No ejecuta acciones destructivas ni modificaciones de estado sin previa validación o aprobación explicita cuando el flujo lo requiera.

---

## 3. Agentes Especializados (Specialized Agents)

Los **Specialized Agents** son agentes secundarios enfocados en dominios de negocio acotados. Cada agente especializado posee herramientas, fuentes de conocimiento (RAG) y permisos específicos asignados a su rol.

### Ejemplos de Agentes Especializados
- **Sales Agent:** Orientado a la calificación de leads, consulta de catálogos y gestión de oportunidades en CRM.
- **HR Agent:** Enfocado en la gestión de consultas de empleados, políticas internas y solicitudes de permisos.
- **Finance Agent:** Encargado del seguimiento de facturas, consultas presupuestarias y reportes financieros.
- **Support Agent:** Especializado en el diagnóstico de incidentes, consulta de documentación técnica y resolución de tickets.

---

## 4. Capa de Capacidades Empresariales (Enterprise Capability Layer)

La capa de capacidades empresariales encapsula los servicios conectables mediante herramientas (`ITool`) y proveedores (`IProvider`) desacoplados:

- **Correo Electrónico:** Integración con servicios como Gmail, Microsoft 365 Email o SMTP/IMAP.
- **Calendarios:** Gestión de eventos en Google Calendar o Outlook Calendar.
- **CRM / ERP:** Conexión con sistemas de gestión empresarial mediante REST/gRPC APIs.
- **Documentos & Archivos:** Procesamiento e ingesta de documentos (PDF, DOCX, TXT, CSV, JSON, HTML, XML).
- **Bases de Datos & Vectores:** Persistencia relacional (PostgreSQL) y almacenamiento vectorial (`PgVector`, `Qdrant`, `ChromaDB`, `Pinecone`).
- **Almacenamiento:** Almacenamiento local, S3 y Google Drive.

---

## 5. Gobernanza y Seguridad (Governance & Security)

Toda acción ejecutada dentro de OCAP debe ser validada rigurosamente a través de los siguientes controles de seguridad inmutables:

1. **Identidad del Usuario (`IUserContext`):** Resolución y verificación de las credenciales del usuario autenticado.
2. **Aislamiento Multi-Tenant (`ITenantContext`):** Garantía de frontera lógica inviolable por `TenantId` en nivel de dominio, consultas y persistencia.
3. **Permisos y RBAC (`IPermissionValidator`):** Validación previa antes de permitir la ejecución de cualquier herramienta o workflow.
4. **Políticas Empresariales & Auditoría (`AuditLogs`):** Registro inmutable y estructurado de todas las operaciones conversacionales, ejecuciones de workflows y cambios de estado.

---

## 6. Interfaces de Comunicación y Canales

Los canales (WhatsApp, Telegram, WebChat, Slack, Discord, Microsoft Teams) son adaptadores primarios e interfaces de entrada/salida.

### Principio de Independencia de Canales
- Los canales **no representan la inteligencia del sistema**.
- Su única función es recibir eventos externos, traducirlos al formato agnóstico `ChannelMessage`, y entregar las respuestas generadas por el motor central.
- Las reglas de negocio, intenciones, workflows y procesamiento RAG permanecen 100% aislados e independientes del canal utilizado.

---

## 7. Estado de Capacidades: Visión Actual vs. Visión Futura

Para mantener la precisión técnica, se explicita el estado de cada capacidad dentro de la plataforma:

### Estado Actual Implementado (v1.5.2 / v1.6.0 Baseline)
- Motor conversacional central y arquitectura de agentes basada en intenciones y reglas.
- Motor RAG multiformato con embeddings, chunking y bases vectoriales (`PgVector`, `Qdrant`, `ChromaDB`, `Pinecone`).
- Workflow Automation Engine con máquina de estados (17 nodos base) y Diseñador Visual en Blazor WASM.
- Autenticación JWT, RBAC, API Keys, `ITenantContext`, `IUserContext` y Validador defensivo de archivos.
- Adaptador de canal WhatsApp (vía Evolution API) y ejecutor de herramientas Google Workspace.

### Visión Futura (Evolución Planificada)
- **Administración Dinámica de Agentes:** Interfaz de usuario para la creación, configuración y monitoreo de agentes especializados sin necesidad de recompilación.
- **Ecosistema Empresarial de Agentes:** Coordinación jerárquica y delegación entre múltiples agentes especializados (Multi-Agent Swarm / Orchestration).
- **Canales Adicionales Operativos:** Telegram, WebChat SignalR en tiempo real, Slack, Discord y Teams.
- **Panel de Operaciones Centralizado:** Métricas de rendimiento de agentes, costos de tokens LLM y telemetría avanzada por Tenant.

---

## 8. Guía para Desarrolladores y Agentes de IA

Cualquier desarrollo, refactorización o extensión del código fuente debe respetar los siguientes principios:

1. **No acoplar inteligencia a los canales:** No escribir lógica de negocio dentro de controladores o adaptadores de canales.
2. **Preservar el aislamiento Multi-Tenant:** Cualquier nueva consulta o entidad debe incluir el contexto de `TenantId`.
3. **Inversión de Dependencias:** Todo conector empresarial debe ser abstraído mediante interfaces (`ITool` / `IProvider`).
4. **Seguridad por defecto:** Ninguna acción destructiva o integración externa debe ejecutarse sin consultar los permisos del contexto.
