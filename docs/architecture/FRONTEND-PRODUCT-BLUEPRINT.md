# OCAP v1.6.0 — FRONTEND PRODUCT BLUEPRINT (REFRAINED & EXPANDED)
**Documento Oficial de Arquitectura Funcional, UX y Diseño de Producto Frontend**

> **Estado**: DISEÑO FUNCIONAL (PROHIBIDO IMPLEMENTAR CÓDIGO)  
> **Ámbito**: Plana Mayor de Arquitectura (Product, UX, Frontend, Design System & Security)  
> **Referencia**: `docs/product/OCAP-VISION.md`  

---

## 1. MAPA COMPLETO DEL PRODUCTO (PRODUCT TAXONOMY & EXPANDED MODULES)

El ecosistema OCAP se organiza en 10 super-módulos y 35 áreas enterprise:

```
OCAP ENTERPRISE ECOSYSTEM
├── 1. PLATFORM OVERVIEW & ANALYTICS
│   ├── 1.1 Executive Summary & Customizable Dashboards (Grafana-style)
│   ├── 1.2 Standalone Monitoring Center (CPU, RAM, DB, Queue, Telemetry)
│   └── 1.3 Cost & Token Consumption Financial Analytics
├── 2. ENTERPRISE AGENTS & CAPABILITIES (CAP-03)
│   ├── 2.1 Enterprise Assistant Agent Orchestrator
│   ├── 2.2 Specialized Sub-Agents & Versioning Registry
│   ├── 2.3 Knowledge Base, RAG & Vector Store Indexing
│   ├── 2.4 Semantic Memory & Reasoning Memory Traces
│   └── 2.5 Tools Registry & Agent Capability Matrix
├── 3. AI CENTER & PROVIDERS (CAP-04)
│   ├── 3.1 AI Provider Management (OpenAI, Gemini, Ollama, Local)
│   ├── 3.2 Credential Vault & Encrypted API Key Storage (AES-256)
│   ├── 3.3 Failover Policies, Health Checks & Latency Benchmarks
│   └── 3.4 Embeddings, Fine-Tuning & Memory Indexing
├── 4. OMNICHANNEL ADAPTER MATRIX (CAP-01 / CAP-02)
│   ├── 4.1 Telegram Native Adapter (QR Auth Pairing)
│   ├── 4.2 WhatsApp Business Adapter (Cloud & Web QR API)
│   ├── 4.3 Google Workspace Adapter (OAuth 2.0 Integration)
│   └── 4.4 Enterprise Channels (Slack, Discord, MS Teams, WebChat, Email)
├── 5. WORKFLOW STUDIO & AUTOMATION
│   ├── 5.1 Visual Drag-and-Drop Workflow Builder
│   ├── 5.2 Versioning, Simulation Sandbox, Rollback & Publishing
│   └── 5.3 Workflow Executions, Step Traces & Error Recovery
├── 6. MARKETPLACE & EXTENSIONS
│   ├── 6.1 Agent Marketplace
│   ├── 6.2 Tool & Integration Extensions Catalog
│   └── 6.3 Module Installer & License Management
├── 7. DEVELOPER CENTER
│   ├── 7.1 API Key Management & OAuth App Registrations
│   ├── 7.2 Webhook Subscriptions & Event Delivery Tracing
│   └── 7.3 SDK Center, OpenAPI Docs & Interactive Console
├── 8. INSTALLER & SYSTEM CENTER
│   ├── 8.1 Guided Interactive Setup & Onboarding Tour
│   ├── 8.2 Dependency Management (PostgreSQL, Redis, AI Models)
│   └── 8.3 Backups, Disaster Recovery & System Updates
├── 9. SECURITY, AUDIT & MULTI-TENANCY
│   ├── 9.1 Tenant & Organization Units Isolation
│   ├── 9.2 RBAC Roles & Fine-Grained Permission Matrix
│   └── 9.3 Visual Timeline Audit Trail & Security Alerts
└── 10. SYSTEM SERVICES & USER PREFERENCES
    ├── 10.1 Command Palette Global (Ctrl+K)
    ├── 10.2 Notification Center & Alerts Manager
    ├── 10.3 Help, Feedback & Diagnostic Center
    └── 10.4 Advanced User Preferences & Theme Customization
```

---

## 2. PANTALLAS Y VISTAS EXPANDIDAS

### 2.1 Center de Monitoreo Independiente (`/monitoring`)
- **Objetivo**: Visualización en vivo tipo Grafana/Datadog de métricas de bajo nivel del servidor y procesos en segundo plano.
- **Información Mostrada**: Uso de CPU por núcleo, consumo de RAM, espacio en disco, I/O de base de datos PostgreSQL, estado de la cola Background Processing (Outbox & Retention Background Service).
- **Acciones**: Purga manual de caché, reinicio de servicios secundarios, exportación de métricas PromQL/OpenTelemetry.

### 2.2 Marketplace (`/marketplace`)
- **Objetivo**: Descubrimiento e instalación modular de agentes, herramientas y conectores creados por OCAP o la comunidad.
- **Información Mostrada**: Catálogo categorizado (Agentes de Ventas, Conectores CRM, Adaptadores de Chat, Modelos de IA), calificaciones, compatibilidad de versión, licencias requeridas.
- **Acciones**: `[Instalar Módulo]`, `[Actualizar]`, `[Desinstalar]`, `[Publicar en Marketplace]`.

### 2.3 Developer Center (`/developer`)
- **Objetivo**: Portal para desarrolladores e ingenieros de integración.
- **Información Mostrada**: API Keys de Tenant, registros OAuth 2.0 Client, suscripciones de Webhooks con tasa de entrega HTTP, descargas de SDKs (C#, TypeScript, Python), consola OpenAPI interactiva.
- **Acciones**: `[Crear API Key]`, `[Suscribir Webhook]`, `[Probar Payload]`, `[Rotar Secreto]`.

### 2.4 Workflow Studio (`/workflows/studio`)
- **Objetivo**: Entorno de desarrollo de flujos autónomos.
- **Información Mostrada**: Canvas interactivo con nodos, panel de versiones (`v1.0.0`, `v1.1.0-draft`), modo simulación en tiempo real sin efecto secundario en producción, botón de Rollback instantáneo.
- **Acciones**: `[Simular Flujo]`, `[Publicar Versión]`, `[Revertir Versión]`, `[Exportar JSON]`.

---

## 3. CATÁLOGO COMPLETO DE WIDGETS REUTILIZABLES (50 WIDGETS DOCUMENTADOS)

Cada widget implementa el contrato `IOcapWidgetContract<TData>`:

```typescript
export interface IOcapWidgetContract<TData = unknown> {
  widgetId: string;
  title: string;
  category: 'System' | 'AI' | 'Channels' | 'Agents' | 'Workflows' | 'Security' | 'Developer';
  size: '1x1' | '2x1' | '2x2' | '4x2' | '4x4';
  allowedRoles: string[];
  refreshIntervalMs?: number;
  data: TData;
  isLoading: boolean;
  error?: string;
  onRefresh?: () => void;
  onRemove?: () => void;
}
```

### Tabla Completa de los 50 Widgets:

| # | Widget ID | Categoría | Tamaño | Propósito / Entradas / Salidas |
|---|---|---|---|---|
| 1 | `SystemHealthWidget` | System | `2x1` | Muestra estado general de salud del servidor OCAP. |
| 2 | `CpuUsageWidget` | System | `1x1` | Gráfico de uso de CPU porcentual por núcleo. |
| 3 | `MemoryUsageWidget` | System | `1x1` | Monitor de memoria RAM usada y disponible. |
| 4 | `DiskStorageWidget` | System | `1x1` | Uso de almacenamiento de archivos y adjuntos. |
| 5 | `NetworkThroughputWidget` | System | `2x1` | Tráfico de red entrante/saliente (KB/s). |
| 6 | `DatabasePerformanceWidget` | System | `2x1` | Latencia de queries PostgreSQL y conexiones activas. |
| 7 | `OutboxQueueWidget` | System | `1x1` | Conteo de mensajes en cola `OutboxProcessor`. |
| 8 | `ExecutionCounterWidget` | System | `1x1` | Contador global de ejecuciones procesadas. |
| 9 | `AiCostWidget` | AI | `2x1` | Costo acumulado en $USD por proveedor de IA. |
| 10 | `TokenUsageWidget` | AI | `2x2` | Ratio de Tokens de Entrada vs Salida. |
| 11 | `AiProviderLatencyWidget` | AI | `2x1` | Histograma de latencia en milisegundos por modelo. |
| 12 | `AiProviderStatusWidget` | AI | `2x2` | Matriz de disponibilidad (OpenAI, Gemini, Ollama, Local). |
| 13 | `ActiveChannelsWidget` | Channels | `2x2` | Lista de adaptadores activos con indicador verde/rojo. |
| 14 | `ChannelTrafficWidget` | Channels | `2x1` | Volumen de mensajes procesados por canal. |
| 15 | `TelegramStatusWidget` | Channels | `1x1` | Estado de vinculación del Bot de Telegram. |
| 16 | `WhatsAppStatusWidget` | Channels | `1x1` | Estado del adaptador WhatsApp Cloud/Web. |
| 17 | `GoogleSuiteStatusWidget` | Channels | `1x1` | Estado de autorización OAuth 2.0 de Google. |
| 18 | `EnterpriseAssistantWidget` | Agents | `2x2` | Resumen de actividad del `EnterpriseAssistantAgent`. |
| 19 | `SubAgentListWidget` | Agents | `2x1` | Subagentes especializados registrados y estado. |
| 20 | `AgentReasoningTraceWidget` | Agents | `4x2` | Log streaming del proceso de razonamiento del agente. |
| 21 | `AgentExecutionLogsWidget` | Agents | `2x2` | Historial de ejecuciones recientes de agentes. |
| 22 | `AgentCapabilityMatrixWidget` | Agents | `2x2` | Visualizador de capacidades activas por agente. |
| 23 | `ToolExecutionStatusWidget` | Agents | `2x1` | Conteo de invocaciones de herramientas del Tools Registry. |
| 24 | `RAGKnowledgeBaseWidget` | Agents | `2x1` | Documentos indexados en la base de conocimientos RAG. |
| 25 | `VectorStoreIndexWidget` | Agents | `1x1` | Estado e índice de embeddings vectoriales. |
| 26 | `SemanticMemoryWidget` | Agents | `2x1` | Tasa de retención y aciertos de memoria semántica. |
| 27 | `WorkflowStatusWidget` | Workflows | `2x2` | Donut chart de flujos activos, exitosos y fallidos. |
| 28 | `WorkflowExecutionTimelineWidget` | Workflows | `4x2` | Trazabilidad temporal paso a paso de automatizaciones. |
| 29 | `WorkflowErrorRateWidget` | Workflows | `1x1` | Porcentaje de fallas en ejecuciones de flujos. |
| 30 | `WorkflowVersionHistoryWidget` | Workflows | `2x1` | Lista de versiones publicadas y activas de flujos. |
| 31 | `TenantOverviewWidget` | Security | `2x1` | Información del Tenant activo y límites de cuota. |
| 32 | `UserActivityWidget` | Security | `2x1` | Usuarios concurrentes y sesiones activas. |
| 33 | `RolePermissionsWidget` | Security | `2x2` | Resumen de roles RBAC configurados en el Tenant. |
| 34 | `ApiKeyUsageWidget` | Developer | `2x1` | Llamadas API procesadas por API Key. |
| 35 | `OAuthAppStatusWidget` | Developer | `1x1` | Aplicaciones OAuth 2.0 integradas. |
| 36 | `AuditLogsTimelineWidget` | Security | `4x2` | Timeline cronológico visual de auditoría de seguridad. |
| 37 | `SecurityThreatAlertsWidget` | Security | `2x1` | Alertas de acceso no autorizado o tokens revocados. |
| 38 | `RecentActivityFeedWidget` | System | `2x2` | Feed unificado de eventos recientes de la plataforma. |
| 39 | `NotificationCenterWidget` | System | `2x1` | Alertas del sistema y notificaciones pendientes. |
| 40 | `InstallerStatusWidget` | System | `1x1` | Estado de la versión OCAP y parches aplicados. |
| 41 | `MarketplaceFeaturedWidget` | Marketplace| `2x1` | Módulos recomendados para instalar. |
| 42 | `MarketplaceInstalledWidget` | Marketplace| `2x1` | Módulos e integraciones instaladas. |
| 43 | `WebhookDeliveryWidget` | Developer | `2x1` | Tasa de éxito de envío de webhooks. |
| 44 | `DeveloperSdkCallsWidget` | Developer | `2x1` | Consumo por lenguaje de SDK. |
| 45 | `SystemBackupStatusWidget` | System | `1x1` | Último respaldo de BD y Vault completado. |
| 46 | `LicensingStatusWidget` | System | `1x1` | Estado de la licencia Enterprise OCAP. |
| 47 | `HelpdeskTicketsWidget` | System | `1x1` | Solicitudes de soporte abiertas. |
| 48 | `FeedbackSubmissionsWidget` | System | `1x1` | Comentarios e ideas del equipo. |
| 49 | `OnboardingProgressWidget` | System | `2x1` | Porcentaje de completitud del Setup inicial. |
| 50 | `UserSettingsSummaryWidget` | System | `1x1` | Preferencias personales (Idioma, Tema, Notificaciones). |

---

## 4. DASHBOARD PERSONALIZABLE (DASHBOARD ARCHITECTURE)

- **Grid System**: Basado en un sistema flexible de 12 columnas receptivas.
- **Operaciones Soportadas**:
  - Drag-and-Drop para reposicionamiento.
  - Resize dinámico (anchos y altos soportados según la matriz del catálogo).
  - Guardar múltiples Dashboards personalizados ("Ops NOC", "Executive AI Summary", "Security Audit").
  - Importación/Exportación vía esquema JSON validado.
  - Definición de Dashboard por defecto por perfil de usuario.

---

## 5. RECORRIDOS DE USUARIO & UX FLOWS

### 5.1 Onboarding e Instalación Guiada (Setup Wizard)
1. **Verificación de Entorno**: Chequeo de dependencias (PostgreSQL, Docker, conexión a internet).
2. **Configuración de Base de Datos**: Entrada de usuario/contraseña de PostgreSQL y ejecución de migraciones automáticas.
3. **Selección de Proveedor IA Inicial**: Elección entre OpenAI, Gemini, Ollama o Local, y prueba de API Key (guardada cifrada en Vault).
4. **Conexión Guiada de Canales**: Pregunta interactiva "¿Deseas conectar Telegram/WhatsApp ahora?", mostrando código QR dinámico si responde "Sí".
5. **Completado**: Redirección directa al Dashboard Principal con el tour interactivo habilitado.

---

## 6. COMMAND PALETTE (CTRL + K) & ACCESIBILIDAD

- **Activación**: Atajo global `Ctrl + K` / `Cmd + K`.
- **Funcionalidades**:
  - Búsqueda difusa (Fuzzy Search) de pantallas, agenas, workflows y canales.
  - Ejecución directa de comandos: `> Conectar Telegram`, `> Probar OpenAI`, `> Cambiar a Tema Oscuro`, `> Cambiar a Idioma Alemán`.
  - Conmutación instantánea de Tenant activo.

---

## 7. VALIDACIÓN DE ARQUITECTURA HEXAGONAL & CONSISTENCIA

- **Sin Inconsistencias Backend**: Todos los módulos corresponden a capas del backend OCAP (`OCAP.Intelligence`, `OCAP.Agents`, `OCAP.Channels`, `OCAP.Security`, `OCAP.Workflow`, `OCAP.Infrastructure`).
- **Aislamiento Estricto de Canales**: Los adaptadores de canales permanecen como componentes puros de comunicación sin lógica de negocio o decisiones de IA.
- **Cero Dependencias Circulares**: La navegación y comunicación entre módulos frontend utiliza contratos aislados y eventos globales sin dependencias cruzadas directas.
