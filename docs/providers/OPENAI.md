# OCAP — Adaptador de Proveedor OpenAI

## Visión General
El adaptador `OpenAiProvider` integra la API oficial de OpenAI (Chat Completions y Streaming SSE) en OCAP sin exponer claves de API ni acoplar el dominio.

## Capacidades
- **Modelos soportados**: `gpt-4o`, `gpt-4o-mini`, `gpt-4-turbo`, `gpt-3.5-turbo`.
- **Streaming SSE**: Flujo continuo token a token mediante Server-Sent Events (`stream: true`).
- **Respuesta JSON**: Formato estructurado nativo mediante `response_format: { type: "json_object" }`.
- **Function / Tool Calling**: Preparado para invocar herramientas registradas en OCAP.
- **Configuración mediante `HttpClientFactory`**: Manejo eficiente de sockets, reintentos y timeouts.
