# OCAP — Adaptador de Proveedor Google Gemini

## Visión General
El adaptador `GeminiAiProvider` integra la API oficial de Google Gemini (Gemini 1.5 Pro y Flash) en la arquitectura de Inteligencia de OCAP.

## Capacidades
- **Modelos soportados**: `gemini-1.5-pro`, `gemini-1.5-flash`, `gemini-1.0-pro`.
- **Streaming SSE**: Transferencia continua de datos mediante `:streamGenerateContent`.
- **System Instructions**: Instrucciones de sistema aisladas en la carga útil HTTP.
- **Safety Settings**: Filtros de seguridad configurados en `BLOCK_MEDIUM_AND_ABOVE`.
- **Soporte de Contexto Extendido**: Diseñado para procesar ventanas de contexto de hasta 1M de tokens.
