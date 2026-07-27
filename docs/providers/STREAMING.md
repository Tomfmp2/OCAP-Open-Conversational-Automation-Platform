# OCAP — Arquitectura de Streaming y SSE (Server-Sent Events)

## Transmisión Continuada Token por Token
OCAP soporta streaming en tiempo real mediante `IAsyncEnumerable<string>` a lo largo de todas las capas del sistema.

## Pipeline HTTP SSE
1. **Cliente Web / Dashboard**: Consume `POST /api/providers/stream` escuchando eventos `text/event-stream`.
2. **API Gateway**: Mantiene la conexión HTTP abierta y realiza `FlushAsync()` por cada fragmento recibido.
3. **Proveedor de IA**: Procesa la respuesta por fragmentos en streaming.
