# OCAP — Arquitectura de Inteligencia Generativa (Generative AI Engine)

## Descripción General

El motor de Inteligencia Generativa de OCAP (**Generative AI Engine Foundation**) proporciona la infraestructura agnóstica de proveedor (*Provider Agnostic*) para integrar modelos de lenguaje avanzados (LLMs) dentro del ciclo de vida conversacional de la plataforma.

La arquitectura sigue estrictamente los principios de **Arquitectura Hexagonal (Ports & Adapters)**, **Domain Driven Design (DDD)** y **Desacoplamiento Total**. El Dominio Puro (`OCAP.Core`) y las abstracciones no tienen dependencia de SDKs de terceros ni librerías propietarias de IA.

---

## Diagrama de Flujo de Razonamiento del Agente (Agent Reasoning Loop)

```text
Entrada de Mensaje
       │
       ▼
[ Agent Reasoning Service ]
       │
       ├─► 1. Carga Agente & Configuración (IAgentRepository)
       ├─► 2. Carga Herramientas Asignadas (IToolRegistry)
       ├─► 3. Construye Prompt Dinámico (IPromptBuilder)
       │
       ▼
[ IAiProvider ] (MockAI / OpenAI / Gemini / Ollama)
       │
       ├─► 4. Genera Respuesta Conversacional (GenerateResponseAsync)
       └─► 5. Analiza Intención & Parámetros (AnalyzeIntentAsync)
       │
       ▼
[ Intención Requiere Herramienta? ]
       ├── SÍ ──► [ IPermissionValidator ] ──► [ ActionDispatcher ] ──► [ ITool Execution ]
       └── NO ──► Retorna Respuesta Texto Directo
       │
       ▼
[ IAiUsageTracker ] (Auditoría de Tokens y Métricas)
```

---

## Capas del Módulo

### 1. Dominio (`OCAP.Intelligence.Domain`)
Define las estructuras puras y entidades persistentes sin dependencias externas:
- `AiModelInformation`: Propiedades y capacidades del modelo (ContextSize, Capabilities).
- `AiConversationMemory`: Memoria de corto y largo plazo vinculada a conversaciones.
- `AiExecutionLog`: Registro de auditoría para consumo de tokens y tiempos de ejecución.

### 2. Abstracciones (`OCAP.Intelligence.Abstractions`)
Contratos de integración y modelos de datos agnósticos:
- `IAiProvider`: Contrato principal para cualquier proveedor LLM.
- `AiRequest` & `AiResponse`: Transferencia de datos estandarizada.
- `AiProviderSettings`: Seguridad y credenciales pasadas mediante variables de entorno.
- `IAiUsageTracker`: Rastreo de tokens y uso por usuario/agente.

### 3. Aplicación (`OCAP.Intelligence.Application`)
Servicio de orquestación de inteligencia:
- `IAgentReasoningService` / `AgentReasoningService`: Coordina el prompt dinámico, la consulta al proveedor de IA, la detección de intenciones, la validación de permisos y la ejecución de herramientas a través del `ActionDispatcher`.

### 4. Proveedor Mock (`OCAP.Intelligence.Mock`)
Implementación desacoplada para desarrollo local y ejecución de suite de pruebas automatizadas sin consumo de tokens ni dependencias de red.
