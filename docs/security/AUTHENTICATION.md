# OCAP — Arquitectura de Autenticación de Usuarios

## Visión General
El módulo `OCAP.Security` proporciona la infraestructura completa para la autenticación de usuarios en entornos SaaS Multi-Tenant.

## Flujo de Autenticación
1. El usuario envía sus credenciales (`Email`, `Password`) a través de `POST /api/auth/login`.
2. `AuthenticateUserUseCase` valida la existencia de la cuenta y verifica el Hash mediante `IPasswordHasher` (PBKDF2 con SHA256 y 600.000 iteraciones).
3. Se emite un **Access Token JWT** de corta duración y un **Refresh Token** de 7 días.
4. Toda la actividad de inicio de sesión se audita en `AuditLog` vía `ISecurityAuditService`.

## Sesión del panel
- El **refresh token nunca llega a JavaScript**: viaja en la cookie `ocap_refresh` (`HttpOnly`, `SameSite=Strict`,
  ruta `/api/auth`, `Secure` cuando la petición llega por HTTPS). Login, MFA y configuración inicial la fijan;
  `POST /api/auth/refresh` y `POST /api/auth/logout` la leen, sin cuerpo.
- El **access token** vive solo en la memoria de la pestaña. Al recargar, el panel canjea la cookie por uno nuevo.
  Nada de la sesión se guarda en `localStorage` ni en `sessionStorage`.
- La base guarda el **SHA-256** de cada refresh token, no el valor: una copia de la base no abre sesiones.
- **Rotación con detección de reutilización**: cada canje revoca el token y emite otro de la misma familia. Si
  aparece un token ya rotado (alguien tiene una copia), se revoca toda la familia y esa sesión se cierra en todos
  los dispositivos. Revocar el token y emitir su sucesor ocurre en una sola transacción con un `UPDATE`
  condicional: si dos peticiones presentan el mismo token a la vez, se trata como reutilización. Entre pestañas, el
  panel serializa la renovación con Web Locks para no provocarlo.
