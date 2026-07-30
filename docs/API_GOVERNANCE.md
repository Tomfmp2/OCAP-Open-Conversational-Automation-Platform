# API Governance — OCAP

Documento de gobernanza del gateway HTTP (`src/Api/OCAP.Api`). Describe comportamientos **implementados**.

## Versionado

- Versión por defecto: **1.0**
- Lectores: cabecera `X-Api-Version` y query `api-version`
- `AssumeDefaultVersionWhenUnspecified = true` (rutas `/api/...` existentes siguen válidas)
- Cabeceras de respuesta: `api-supported-versions` / `api-deprecated-versions`

## Autenticación y autorización

- JWT Bearer por defecto
- **Authorize-by-default** en todos los controladores salvo `[AllowAnonymous]`
- Públicos: `Auth`, `Health` / diagnostic, webhooks de canal, Connect OpenIddict, callbacks externos selectos
- Multi-tenant: claim `tenant_id` obligatorio en JWT; sin claim → `Guid.Empty` (fail-safe)
- Header `X-Tenant-ID` solo en Development/Testing para escenarios anónimos

## Validación

- DataAnnotations + `ApiBehavior` → `application/problem+json`
- FluentValidation (`IValidator<T>`) vía `FluentValidationActionFilter`
- Excepciones `ValidationException` → HTTP 400 ProblemDetails con `errors`

## Errores (RFC 7807)

`ExceptionHandlingMiddleware` emite ProblemDetails con:

- `type`, `title`, `status`, `detail`, `instance`
- extensiones: `correlationId`, `requestId`, `traceId`
- sin stack traces fuera de Development

## Correlación

- Entrada: `X-Correlation-Id`, `X-Request-Id`
- Salida: mismas cabeceras (generadas si faltan)
- Middleware: `CorrelationIdMiddleware`

## Paginación (contrato)

Modelo `PagedQuery` / `PagedResult<T>`:

- `page` (≥1), `pageSize` (1–200), `search`, `sortBy`, `sortDirection`

## OpenAPI

- Swagger solo en Development
- Bearer JWT documentado
- XML comments incluidos cuando existe `OCAP.Api.xml`

## Idempotencia y reintentos

- Inbox / Outbox del Event Bus (capa infraestructura)
- Refresh token rotation en `/api/auth/refresh`
- Webhooks requieren secret HMAC explícito (no se auto-genera)

## Seguridad operativa

- CORS fail-closed si no hay orígenes configurados
- Rate limiting opcional (`RateLimiting:EnableRateLimiting`)
- Security headers middleware
- Deny-by-default en validación de permisos (`DefaultPermissionValidator`)
