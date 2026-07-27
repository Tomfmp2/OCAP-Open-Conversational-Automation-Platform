# OCAP — Arquitectura del Motor de Agentes (Agent Engine Foundation)

## Descripción General

El **Agent Engine** constituye el núcleo de inteligencia conversacional y orquestación de OCAP. Transforma el paradigma tradicional de respuestas estáticas en un motor dinámico impulsado por **Agentes, Contexto Conversacional, Resolución de Intenciones, Despacho de Acciones y Ejecución de Herramientas**.

La arquitectura está totalmente desacoplada de canales de comunicación (WhatsApp, Telegram) e infraestructura de persistencia (PostgreSQL), garantizando la máxima mantenibilidad y extensibilidad para futuras integraciones de IA (OpenAI, Gemini, Ollama) y herramientas (Google Calendar, Gmail, Sheets).

---

## Flujo del Agent Engine

```
[ Mensaje Entrante ]
        │ (vía Canal o API)
        ▼
[ ProcessAgentMessageUseCase ]
        │
        ├──► 1. Cargar / Crear Agente Activo (IAgentRepository)
        │
        ├──► 2. Cargar Contexto Conversacional (IConversationContextRepository)
        │
        ├──► 3. Clasificar Intención (IIntentResolver)
        │       ├── Greeting
        │       ├── CreateReminder
        │       ├── HumanSupport
        │       ├── GetInformation
        │       └── Unknown
        │
        ├──► 4. Actualizar Estado & Parámetros en Contexto
        │
        ├──► 5. Seleccionar & Despachar Acción / Herramienta (IActionDispatcher)
        │       └── Invocación vía IToolRegistry / ITool
        │
        └─► 6. Retornar Respuesta al Usuario
```

---

## Componentes Clave

### 1. `Agent` (Aggregate Root)
Entidad central que controla la identidad del asistente, su estado operativo (`Active`, `Inactive`, `Maintenance`) y su objeto de valor `AgentConfiguration` (instrucciones del sistema y capacidades permitidas).

### 2. `ConversationContext`
Mantiene el estado temporal y la memoria del diálogo para una conversación. Almacena la intención activa (`CurrentIntent`), los parámetros pendientes por recolectar (`PendingParameters`) y variables de estado general.

### 3. `Intent` & `IIntentResolver`
- **`Intent`**: Representa la intencionalidad identificada en la consulta del usuario junto con su nivel de confianza y parámetros extraídos.
- **`IIntentResolver`**: Puerto que abstrae el clasificador de intenciones. La implementación inicial `RuleBasedIntentResolver` analiza reglas heurísticas y palabras clave sin depender de servicios externos.

### 4. `AgentAction` & `IActionDispatcher`
- **`AgentAction`**: Modela una acción determinada por el agente (ej. `CreateCalendarEvent`, `SendEmail`, `TransferToHuman`).
- **`IActionDispatcher`**: Despacha la ejecución hacia la herramienta registrada en el `IToolRegistry`.

### 5. `ITool` & `IToolRegistry` (`OCAP.Tools.Abstractions`)
Abstracciones para capacidades externas. `ITool` define el contrato estandarizado de ejecución (`ExecuteAsync`) y metadatos (`ToolMetadata`), mientras que `IToolRegistry` actúa como el registro central del sistema.

---

## Preparación para Integraciones Futuras

El diseño preparado en la versión `v0.7.0` permite conectar fácilmente:
- **LLMs e IA Generativa**: Sustituyendo `RuleBasedIntentResolver` por una implementación que consuma OpenAI, Gemini u Ollama.
- **Herramientas Empresariales**: Registrando herramientas reales que implementen `ITool` para Google Calendar, Gmail, Google Sheets y webhooks de automatización.
