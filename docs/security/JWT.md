# OCAP — Especificación de JWT (JSON Web Tokens)

## Estructura del JWT Token
Los Access Tokens emitidos por `IJwtTokenService` incluyen los siguientes Claims esenciales:
- `sub`: Identificador de usuario (`UserIdentity.Id`).
- `email`: Correo electrónico del usuario.
- `tenant_id`: Identificador único de la organización (`Tenant.Id`).
- `tenant_slug`: Alfanumérico amigable del tenant.
- `role`: Nombre del rol asignado (ej. `Admin`, `Operator`).
- `permission`: Lista de permisos granulares concedidos.

## Firma y Expiración
- Algoritmo: `HMAC SHA-256`.
- Validez por defecto: 60 minutos.
- Emisor (`iss`): `OCAP`.
- Audiencia (`aud`): `OCAP.Clients`.
