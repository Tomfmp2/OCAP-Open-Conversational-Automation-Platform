# OCAP Domain Model

Este documento describe el modelo de dominio central de OCAP (Open Conversational Automation Platform), el cual forma el corazón inteligente del motor conversacional.

## Principios del Dominio
- **Aislamiento Total**: El dominio de OCAP es agnóstico de infraestructura. No conoce detalles de bases de datos, APIs de terceros (WhatsApp, OpenAI) ni frameworks web (ASP.NET).
- **Enfoque en el Negocio**: Únicamente encapsula reglas, entidades, agregados y eventos del negocio conversacional.

## Entidades Principales

### 1. User (Entidad)
Representa al usuario interactuando con la plataforma OCAP.
- **Propiedades**: `Id`, `DisplayName`, `CreatedAt`, `UpdatedAt`, `Status`.
- **Estados (UserStatus)**: `Active`, `Blocked`, `Inactive`.
- **Reglas de Negocio**:
  - Todo usuario debe poseer un identificador (`Id`) válido.
  - La transición de estados está controlada (ej. no se puede interactuar si el estado es `Blocked`).

### 2. Conversation (Agregado Raíz)
Representa una conversación entre un usuario y el agente (sistema o humano).
- **Propiedades**: `Id`, `UserId`, `Status`, `CreatedAt`, `LastActivityAt`.
- **Estados (ConversationStatus)**: `Active`, `Paused`, `Closed`, `WaitingHuman`.
- **Comportamiento**:
  - Iniciar conversación.
  - Cerrar conversación.
  - Pausar conversación.
  - Solicitar intervención humana (`RequestHumanIntervention()`).

### 3. Message (Entidad)
Representa una unidad de comunicación dentro de una conversación.
- **Propiedades**: `Id`, `ConversationId`, `Content`, `SenderType`, `CreatedAt`.
- **Remitentes (SenderType)**: `User`, `Agent`, `System`.
- **Reglas de Negocio**:
  - Debe pertenecer a una `Conversation` válida.
  - El contenido no puede estar vacío (validado a través del Value Object `MessageContent`).
  - La fecha de creación debe ser coherente.
  - El remitente debe estar dentro de los tipos permitidos.

### 4. Session (Entidad)
Administra el contexto temporal para la interacción y flujo conversacional.
- **Propiedades**: `Id`, `ConversationId`, `ContextData`, `CreatedAt`, `ExpiresAt`.
- **Comportamiento**:
  - Almacenar variables temporales o estado del flujo.
  - Evaluar y controlar la expiración de la sesión.
  - Reiniciar el contexto.

## Value Objects (Objetos de Valor)

Los Objetos de Valor encapsulan validación y semántica de datos primitivos.

### MessageContent
- Valida que el texto o payload del mensaje cumpla con requerimientos básicos de negocio: no puede estar vacío y debe respetar un límite máximo de longitud configurable.

### UserIdentifier
- Representa de manera abstracta un identificador de un canal externo (ej. WhatsApp ID, número de teléfono, Telegram ID, Email) sin acoplarse a un proveedor específico.

## Domain Events (Eventos de Dominio)

Los eventos de dominio permiten que distintos módulos reaccionen a cambios en el Core.

- `ConversationStartedEvent`: Ocurre cuando se crea e inicia una nueva conversación.
- `MessageReceivedEvent`: Emitido tras registrar un nuevo mensaje entrante de un usuario.
- `ConversationClosedEvent`: Ocurre cuando una conversación se da por finalizada.
- `HumanInterventionRequestedEvent`: Señala que la conversación actual ha transicionado para requerir la atención de un operador humano.

## Ports (Contratos)

El dominio define Puertos (Interfaces) para permitir a las capas externas interactuar con él (Inbound) y para requerir servicios del exterior (Outbound) según los principios de la Arquitectura Hexagonal.

- `IMessageSender`: Permite enviar respuestas o mensajes al usuario sin importar el canal.
- `IMessageReceiver`: Abstracción de entrada para recepcionar eventos o mensajes.
- `IConversationRepository`: Define las operaciones de persistencia (`GetById`, `Save`, `Exists`) aisladas de cualquier ORM como Entity Framework.
