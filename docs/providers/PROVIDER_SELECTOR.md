# OCAP — Selector u Orquestador Inteligente de Proveedores (IAiProviderSelector)

## Arquitectura de Orquestación
El `AiProviderSelector` administra la conmutación por error (Failover), priorización de proveedores y evaluación de salud en tiempo real.

## Criterios de Selección
1. **Prioridad Configurada**: `Primary` (OpenAI) -> `Secondary` (Gemini) -> `Tertiary` (Ollama) -> `Fallback` (MockAI).
2. **Failover Automático**: Ante fallos HTTP, latencias altas o errores de red, la petición conmuta automáticamente al siguiente proveedor disponible.
3. **Caché en Memoria**: Caché de respuestas idénticas con TTL de 5 minutos mediante `IAiResponseCache`.
4. **Monitoreo de Salud**: Endpoint `HealthAsync()` que mide la latencia y disponibilidad de cada API.
