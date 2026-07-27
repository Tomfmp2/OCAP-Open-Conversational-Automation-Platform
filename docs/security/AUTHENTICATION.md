# OCAP — Arquitectura de Autenticación de Usuarios

## Visión General
El módulo `OCAP.Security` proporciona la infraestructura completa para la autenticación de usuarios en entornos SaaS Multi-Tenant.

## Flujo de Autenticación
1. El usuario envía sus credenciales (`Email`, `Password`) a través de `POST /api/auth/login`.
2. `AuthenticateUserUseCase` valida la existencia de la cuenta y verifica el Hash mediante `IPasswordHasher` (PBKDF2 con SHA256 y 100,000 iteraciones).
3. Se emite un **Access Token JWT** de corta duración y un **Refresh Token** de 7 días.
4. Toda la actividad de inicio de sesión se audita en `AuditLog` vía `ISecurityAuditService`.
