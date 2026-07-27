# OCAP — Integración con Google Workspace Foundation

## Descripción General

La integración de **Google Workspace** proporciona conectividad extensible con **Google Calendar, Gmail y Google Sheets** sin acoplar el dominio ni el motor de agentes a SDKs de terceros.

---

## Arquitectura de Capas

```
[ Agent Engine / ActionDispatcher ]
             │
             ▼
   [ OCAP.Tools.Google ]
             │
             ▼
[ OCAP.Providers.Google.Abstractions ]
   (ICalendarProvider, IEmailProvider, ISpreadsheetProvider)
             │
             ▼
[ Implementación del Proveedor ]
   (InMemory / Production REST Client)
```

---

## Autenticación OAuth2 (`GoogleAuthentication`)

La infraestructura de autenticación utiliza tokens OAuth2 gestionados de forma segura:

- **`GoogleSettings`**: Encapsula `ClientId`, `ClientSecret`, `RedirectUri` y `Scopes`.
- **`OAuthCredential`**: Representa el `AccessToken`, `RefreshToken` y la expiración UTC.

### Prácticas de Seguridad de Secretos
- **Variables de Entorno / Secrets**: Los secretos nunca se almacenan en código ni se suben al repositorio.
- **Sin Logging Sensible**: Los conectores ocultan y omiten `AccessToken` y `RefreshToken` en los registros de telemetría.

---

## Herramientas Google Disponibles

### 1. `CreateCalendarEventTool`
- **Permisos requeridos**: `Calendar.Create`
- **Entradas**: `Title`, `Description`, `StartDate`, `EndDate`, `Attendees`

### 2. `SendEmailTool`
- **Permisos requeridos**: `Gmail.Send`
- **Entradas**: `To`, `Subject`, `Body`

### 3. `AppendSpreadsheetRowTool`
- **Permisos requeridos**: `Sheets.Append`
- **Entradas**: `SpreadsheetId`, `SheetName`, `Values`
