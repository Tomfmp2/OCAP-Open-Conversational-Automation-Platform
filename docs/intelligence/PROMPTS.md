# OCAP — Sistema de Prompts Dinámicos

## Descripción General

El módulo **OCAP.Prompts** gestiona la construcción contextual de instrucciones (*prompts*) para los agentes inteligentes de la plataforma.

Permite inyectar dinámicamente las directivas del agente, el historial de conversación, las variables de usuario y el catálogo de herramientas ejecutables autorizadas.

---

## Componentes

### 1. `PromptTemplate`
Estructura que contiene:
- `Name`: Identificador único de la plantilla.
- `Version`: Versión semántica del prompt.
- `DynamicVariables`: Diccionario clave-valor para reemplazo dinámico.
- `SystemPrompt`: Instrucciones base de comportamiento e inyección de herramientas.
- `UserPrompt`: Mensaje final del usuario procesado.

### 2. `IPromptBuilder` / `SystemPromptBuilder`
Ensambla el `PromptTemplate` tomando como insumo:
- `Agent`: Rol del agente, nombre y `SystemPrompt` base.
- `AvailableTools`: Descripción y esquema de las herramientas que el agente tiene permitido ejecutar.
- `ConversationContext`: Intención activa y marcas de tiempo de interacción.

---

## Ejemplo de Prompt Generado

Para un agente configurado como Asistente Administrativo con acceso a la herramienta `CreateCalendarEventTool`:

```text
SYSTEM PROMPT:
Eres el asistente virtual por defecto de OCAP. Especialista en gestión de agendas.

Herramientas ejecutables disponibles:
- CreateCalendarEventTool: Agenda un evento en el calendario de Google Workspace (Permisos: Calendar.Create)

USER PROMPT:
Agendar una reunión de revisión para mañana a las 10am
```
