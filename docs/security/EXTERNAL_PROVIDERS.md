# Proveedores de Identidad Externos (External Identity Providers - CAP-15)

OCAP soporta autenticación federada y Single Sign-On (SSO) utilizando proveedores externos basados en OAuth2 y OpenID Connect.

## Proveedores Soportados

1. **Google Workspace**: `GoogleExternalAuthProvider`
2. **Microsoft Entra ID (Azure AD / Office 365)**: `MicrosoftExternalAuthProvider`
3. **GitHub**: `GitHubExternalAuthProvider`
4. **Generic OpenID Connect (OIDC)**: `GenericOidcExternalAuthProvider` (Keycloak, Okta, Auth0)

## Endpoints REST API

- `GET /api/auth/external/providers`: Lista de proveedores habilitados.
- `GET /api/auth/external/challenge/{provider}`: Inicia desafío OAuth (retorna URL de autorización y `state`).
- `GET /api/auth/external/callback/{provider}`: Procesa el callback del proveedor y emite tokens JWT de OCAP.
- `GET /api/auth/external/linked`: Lista proveedores vinculados al usuario autenticado.
- `DELETE /api/auth/external/linked/{provider}`: Desvincula un proveedor externo del usuario.

## Aprovisionamiento Automático y Vinculación

- **Vinculación de Cuentas**: Si un usuario con el mismo email ya existe en la organización (`Tenant`), la identidad externa se vincula automáticamente a dicho usuario.
- **Auto-Provisioning**: Si el usuario no existe y `Authentication:AutoProvisionUsers` es `true`, OCAP crea automáticamente la cuenta `UserIdentity` y la vincula.
- **Aislamiento Multi-Tenant**: Todas las identidades externas están estrictamente aisladas por `TenantId`.
