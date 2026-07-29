# Enterprise Single Sign-On (SAML 2.0 - CAP-18)

Documentación técnica y especificaciones del Service Provider (SP) SAML 2.0 en OCAP.

## Endpoints API REST

- `GET /api/auth/saml/metadata`: Descarga el archivo XML de Metadatos del Service Provider (SP) de OCAP para configurar en el IdP (Microsoft Entra ID, Okta, Keycloak, etc.).
- `POST /api/auth/saml/metadata/import`: Importa los metadatos XML exportados desde el Identity Provider (IdP) para configurar automáticamente el proveedor en el tenant.
- `GET /api/auth/saml/config`: Obtiene la configuración activa del proveedor SAML 2.0.
- `POST /api/auth/saml/config`: Actualiza manualmente los parámetros del IdP (EntityID, SSO URL, SLO URL, Certificado X.509 PEM).
- `GET /api/auth/saml/login`: Inicia el flujo de autenticación SP-Initiated construyendo el `AuthnRequest` y redirigiendo al IdP.
- `POST /api/auth/saml/acs`: Assertion Consumer Service (ACS) endpoint que recibe la respuesta `SAMLResponse` firmada y emite el token JWT de OCAP.
- `GET/POST /api/auth/saml/slo`: Procesa solicitudes de Single Logout (SLO).
- `GET /api/auth/saml/status`: Consulta el estado de configuración e integración SAML 2.0 del tenant.
