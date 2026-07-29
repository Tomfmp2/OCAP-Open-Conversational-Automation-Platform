# ADR-007: Enterprise Single Sign-On mediante SAML 2.0 (CAP-18)

## Estado
Aprobado

## Contexto
Para posibilitar la integración federada en organizaciones Enterprise (Microsoft Entra ID / Azure AD, Okta, Auth0, Keycloak, Ping Identity, ADFS), OCAP implementa el rol de Service Provider (SP) para el estándar SAML 2.0 (Security Assertion Markup Language).

## Decisiones de Diseño

1. **Service Provider (SP) Metadata XML**:
   - Generación dinámica de Metadatos de SP por Tenant (`/api/auth/saml/metadata?tenantId=...`) especificando los bindings HTTP-POST y HTTP-Redirect para ACS y SLO.

2. **Validación Criptográfica y Estricta de Assertions**:
   - Validaciones mandatorias en el Callback ACS (`/api/auth/saml/acs`):
     - **StatusCode**: `urn:oasis:names:tc:SAML:2.0:status:Success`.
     - **Issuer Validation**: El emisor del IdP debe coincidir con el `EntityId` configurado en el Tenant.
     - **AudienceRestriction**: La Assertion debe incluir el `EntityID` del SP de OCAP.
     - **Timestamps**: `NotBefore` y `NotOnOrAfter` con 5 minutos de holgura por Clock Skew.
     - **Protección XSW**: Análisis XML seguro sin resolución de entidades externas DTD.

3. **Auto-Aprovisionamiento y Vinculación de Cuentas**:
   - Mapeo directo del `NameID` o atributo `email` a la entidad `UserIdentity` de OCAP, con generación transparente de JWTs y Refresh Tokens de sesión.

## Consecuencias
- Integración Enterprise SSO sin dependencias inseguras de terceros.
- Aislamiento completo por Tenant de las configuraciones de IdP SAML 2.0.
