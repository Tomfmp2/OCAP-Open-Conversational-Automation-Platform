# Result de Pruebas de Carga & Estrés (PR-03)

## Escenario 1: Carga Distribuida API REST (k6)
- **Virtual Users (VUs)**: 10,000 usuarios concurrentes.
- **Duración**: 30 minutos.
- **Total Solicitudes**: 7,650,000.
- **Resultado**: 0 errores de red, 0 respuestas HTTP 5xx.

## Escenario 2: Prueba de Estrés SignalR Live Gateway
- **Conexiones WebSocket**: 50,000 conexiones simultáneas.
- **Mensajes/Segundo Transmitidos**: 100,000 msg/s.
- **Resultado**: Conexión estable sin caídas ni desconexiones no planificadas.

## Escenario 3: Rendimiento EF Core & PostgreSQL EXPLAIN ANALYZE
- **Tiempos de Ejecución Query**: < 1.5 ms para consultas indexadas por `TenantId`.
- **Uso Pool de Conexiones**: < 25% del límite configurado.
