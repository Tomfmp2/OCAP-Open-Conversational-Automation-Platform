# OCAP v1.5.2 - Implementation Log

**Fecha:** 2026-07-28  
**Versión:** v1.5.2 (Enterprise Reliability & Operational Excellence)

## Resumen de Cambios Técnicos

1. **Resolución de Deuda Técnica y Contexto**:
   - Se definió la interfaz `IUserContext` en `OCAP.Security.Abstractions` e `HttpUserContext` en `OCAP.Security.Infrastructure`.
   - Se actualizó `AuditSaveChangesInterceptor.cs` para inyectar y resolver de forma segura `ITenantContext` y `IUserContext` desde claims/HTTP context sin valores harcodeados ni TODOs.
   - Se registraron los servicios en el contenedor de Inyección de Dependencias.

2. **Resiliencia en Servicios en Segundo Plano**:
   - Se mejoró `OutboxProcessorBackgroundService.cs` incorporando un retraso inicial de arranque (3s), manejo elegante de cancelación (`CancellationToken`), resiliencia con políticas de *exponential backoff* en fallos de conexión (10s a 60s max) y registro de logs de advertencia estructurados en lugar de excepciones no controladas.

3. **Actualización de Dependencias**:
   - Se actualizaron las librerías de seguridad `Microsoft.IdentityModel.Tokens` y `System.IdentityModel.Tokens.Jwt` a la versión `8.21.0` en `OCAP.Security.Infrastructure`.

4. **Suite de Pruebas**:
   - Se crearon pruebas unitarias e integración de confiabilidad y seguridad:
     - `UserContextTests.cs` (Validación de extracción de claims del usuario).
     - `AuditSaveChangesInterceptorTests.cs` (Verificación de generación de logs de auditoría con Tenant y User resolution).
     - `OutboxProcessorResilienceTests.cs` (Prueba de procesamiento del Outbox pattern sin fallos).
