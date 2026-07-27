# OCAP — Arquitectura de Canales (Channel Architecture Foundation)

## Descripción General

La capa de canales de OCAP (Open Conversational Automation Platform) proporciona una arquitectura extensible basada en el patrón **Ports & Adapters (Arquitectura Hexagonal)**. Permite conectar proveedores externos de comunicación (tales como WhatsApp, Telegram, Discord, Slack o webhooks personalizados) sin acoplar ni modificar el dominio ni los casos de uso principales.

---

## Principios Arquitectónicos

1. **Independencia del Dominio**: `OCAP.Core` y `OCAP.Application` no tienen ninguna referencia ni conocimiento de plataformas externas concretas.
2. **Inversión de Dependencias**: Los canales actúan como adaptadores externos que dependen de las abstracciones definidas en `OCAP.Channels.Abstractions`.
3. **Pluggability (Canales Intercambiables)**: Cada canal es un plugin independiente que puede habilitarse, reemplazarse o deshabilitarse por configuración sin recompilar el núcleo.

---

## Dirección de Dependencia

```
[ Cliente Externo ]
        │ (Webhook / Evento)
        ▼
[ Adaptador de Canal ] (ej. OCAP.Channels.WhatsApp)
        │
        ▼
[ Contratos del Canal ] (OCAP.Channels.Abstractions / IMessageReceiver)
        │
        ▼
[ Casos de Uso ] (OCAP.Application / ReceiveMessageUseCase)
        │
        ▼
[ Entidades del Dominio ] (OCAP.Core)
        │
        ▼
[ Envío de Respuesta ] (SendResponseUseCase)
        │
        ▼
[ Contrato de Salida ] (IMessageSender)
        │
        ▼
[ Adaptador del Canal ] (Cliente / Proveedor Externo)
```

---

## Contratos Principales

### `IMessageReceiver`
Contrato responsable de recibir e ingresar mensajes hacia la plataforma OCAP.
Applica validaciones iniciales de seguridad (longitud máxima, sanitización de identificadores).

### `IMessageSender`
Contrato responsable del despacho de respuestas generadas por OCAP hacia la plataforma de destino.

### `IChannelProvider`
Administra el ciclo de vida completo de un canal de comunicación (`InitializeAsync`, `StopAsync`), expone sus metadatos descriptivos (`ChannelMetadata`) y agrupa sus componentes de recepción y envío.

---

## Modelo de Datos de Canales

- **`IncomingChannelMessage`**: Representa un mensaje entrante genérico (`ExternalUserId`, `Message`, `ChannelName`, `ReceivedAt`, `Metadata`).
- **`OutgoingChannelMessage`**: Representa una respuesta saliente genérica (`DestinationUserId`, `Message`, `ChannelName`, `SentAt`, `Metadata`).
- **`ChannelMetadata`**: Describe la identidad, versión y estado activo del canal (`ChannelId`, `ChannelName`, `Version`, `IsEnabled`).

---

## Canal Simulado (`OCAP.Channels.Mock`)

Se proporciona una implementación de referencia llamada `OCAP.Channels.Mock` para desarrollo, pruebas unitarias e integración continua:

- **`MockMessageReceiver`**: Almacena mensajes entrantes simulados en memoria aplicando límites de seguridad (máximo 10 KB por mensaje).
- **`MockMessageSender`**: Registra las respuestas despachadas en memoria para aserciones de testing.
- **`MockChannelProvider`**: Controla el estado del canal de pruebas en memoria.

---

## Cómo Agregar un Nuevo Canal

Para implementar un nuevo proveedor de canal (ej. Telegram o WhatsApp):

1. Crear un proyecto bajo `src/Channels/OCAP.Channels.<NombreCanal>`.
2. Agregar referencia a `OCAP.Channels.Abstractions`.
3. Implementar las interfaces `IMessageReceiver`, `IMessageSender` e `IChannelProvider`.
4. Registrar los servicios del nuevo canal en el contenedor de Inyección de Dependencias.
5. Configurar los parámetros del canal en `appsettings.json` bajo la sección `Channels:<NombreCanal>`.
