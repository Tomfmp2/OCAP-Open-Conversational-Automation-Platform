# ADR-004: Integración de Proveedores de Identidad Externos (External Identity Providers)

- **Estado**: Aprobado
- **Fecha**: 2026-07-29
- **Autor**: Principal Software Architect & Security Lead
- **Contexto**: CAP-15 - Autenticación OAuth2 / OpenID Connect con Proveedores Externos (Google, Microsoft Entra ID, GitHub, Generic OIDC)

---

## 1. Problema y Contexto

OCAP requería soporte para autenticación de usuarios mediante proveedores de identidad externos de clase empresarial (Google Workspace, Microsoft Entra ID / Azure AD, GitHub y cualquier proveedor OpenID Connect / Keycloak corporativo), manteniendo la arquitectura desacoplada, multi-tenant y compatible con el emisor de tokens interno de OpenIddict y la entidad de dominio `UserIdentity`.

## 2. Opciones Consideradas

1. **Utilizar AspNetCore Authentication Handlers Estándar (AddGoogle / AddMicrosoftAccount)**:
   - *Pros*: Integración directa con ASP.NET Core.
   - *Contras*: Acoplamiento fuerte con IIS/Kestrel cookies en un API Gateway Stateless desacoplado.

2. **Abstracción Hexagonal con Proveedores Desacoplados (`IExternalAuthProvider` & `ExternalAuthenticationService`)**:
   - *Pros*: 100% alineado con Clean Architecture y Hexagonal Architecture. Facilita la adición de nuevos proveedores OIDC en runtime mediante configuración sin cambiar código. Permite vinculación multi-tenant estricta y auto-aprovisionamiento configurable.
   - *Contras*: Requiere construir manejadores de clientes HTTP para cada API externa.

## 3. Decisión Arquitectónica

Se eligió la **Opción 2: Abstracción Hexagonal de Proveedores Externos**.

Se implementó el contrato `IExternalAuthProvider` para encapsular la construcción de URL de desafío y el flujo de canje de código/fetch de perfil de usuario.

### Estructura de Componentes:
- `IExternalAuthProvider`: Interfaz única para proveedores externos (`GoogleExternalAuthProvider`, `MicrosoftExternalAuthProvider`, `GitHubExternalAuthProvider`, `GenericOidcExternalAuthProvider`).
- `IExternalIdentityResolver`: Manejo de la relación de dominio `ExternalIdentity` ↔ `UserIdentity` por `TenantId`.
- `IExternalAuthenticationService`: Servicio orquestador del flujo de desafío, callback, vinculación/desvinculación y aprovisionamiento automático.
- `ExternalAuthController`: Endpoints REST para `/api/auth/external/*`.

## 4. Consecuencias

- **Positivas**:
  - Escalabilidad para añadir nuevos proveedores (SAML, LDAP en fases posteriores) sin alterar el dominio central.
  - Aislamiento multi-tenant completo en la vinculación de cuentas.
  - Registro de auditoría de seguridad para inicios de sesión y vinculaciones.
  - Total compatibilidad con la emisión de JWTs y Refresh Tokens de OCAP.
- **Negativas / Mitigaciones**:
  - Requiere mantener las credenciales (`ClientId`, `ClientSecret`) en appsettings o Secret Vault. Mitigado mediante `AesDbCredentialVault` y variables de entorno.
