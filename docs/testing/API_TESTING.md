# OCAP API — Estrategia de Testing

## Descripción

Este documento describe la estrategia de pruebas implementada para el API Gateway de OCAP (`OCAP.Api`) a partir de la versión `v0.4.1`.

---

## Herramientas Utilizadas

| Herramienta | Versión | Propósito |
|---|---|---|
| xUnit | 2.9+ | Framework de pruebas principal |
| FluentAssertions | 8.x | Aserciones legibles y expresivas |
| Moq | 4.x | Mocks para puertos y servicios externos |
| Microsoft.AspNetCore.Mvc.Testing | 10.x | Levanta la API completa en memoria |
| Microsoft.EntityFrameworkCore.InMemory | 10.x | Sustituye PostgreSQL en tests |

---

## Estructura de Proyectos de Test

```
tests/
├── OCAP.UnitTests/             # Pruebas unitarias del dominio y casos de uso
├── OCAP.IntegrationTests/      # Pruebas de integración con EF Core InMemory
└── OCAP.Api.Tests/             # Pruebas de integración HTTP del API Gateway
    ├── Infrastructure/
    │   └── OcapApiFactory.cs   # WebApplicationFactory personalizada
    └── Endpoints/
        ├── MessagesEndpointTests.cs
        ├── ConversationsEndpointTests.cs
        └── HealthCheckTests.cs
```

---

## Tipos de Pruebas

### Pruebas Unitarias (`OCAP.UnitTests`)

Validan la lógica pura del dominio y los casos de uso **sin** dependencias externas.

- **Qué prueban:** Entidades, Value Objects, reglas de negocio del Core.
- **Dependencias:** Ninguna (ni base de datos, ni HTTP).
- **Velocidad:** Muy rápidas (< 1 segundo por suite completa).

### Pruebas de Integración (`OCAP.IntegrationTests`)

Validan la interacción entre las capas Application e Infrastructure.

- **Qué prueban:** Repositorios, casos de uso con base de datos InMemory.
- **Dependencias:** EF Core InMemory.
- **Velocidad:** Rápidas (sin red ni disco).

### Pruebas HTTP de API (`OCAP.Api.Tests`)

Validan el comportamiento completo del gateway HTTP usando `WebApplicationFactory`.

- **Qué prueban:** El pipeline completo HTTP → Controller → UseCase → Repository.
- **Dependencias:** EF Core InMemory, todos los middlewares activos.
- **Velocidad:** Moderadas (levantan la app completa en memoria).

---

## Cómo Ejecutar las Pruebas

### Ejecutar todos los tests

```bash
cd ~/Proyectos/OCAP
dotnet test
```

### Ejecutar solo los tests del API

```bash
dotnet test tests/OCAP.Api.Tests/OCAP.Api.Tests.csproj
```

### Ejecutar con reporte de cobertura

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Ejecutar con output detallado

```bash
dotnet test --verbosity normal
```

---

## Entorno de Pruebas

Los tests de API utilizan:

- **`appsettings.Testing.json`**: configuración sin credenciales reales.
- **Base de datos InMemory**: aislamiento completo de PostgreSQL.
- **Rate Limiting deshabilitado**: para no interferir con las pruebas de carga de endpoints.

La `OcapApiFactory` reemplaza automáticamente el DbContext configurado con PostgreSQL por uno InMemory, garantizando que los tests sean deterministas y no dependan de infraestructura externa.

---

## Convenciones de Nomenclatura

Los métodos de test siguen el patrón:

```
{Acción}_{Condición}_{ResultadoEsperado}
```

Ejemplos:

```
PostMessage_WithValidRequest_Returns200Ok
GetConversation_WithNonExistentId_Returns404NotFound
HealthCheck_WhenApiIsRunning_Returns200Ok
```

---

## Diferencias entre Unit e Integration Tests

| Aspecto | Unit | Integration HTTP |
|---|---|---|
| Velocidad | Muy rápida | Moderada |
| Dependencias | Ninguna | InMemory DB + App |
| Confianza en el pipeline | Baja | Alta |
| Facilidad de depuración | Alta | Media |
| Cobertura de middleware | No | Sí |

---

## Notas Importantes

- Los tests de integración HTTP **no usan la base de datos PostgreSQL real**. Están diseñados para ejecutarse en cualquier entorno sin configuración de infraestructura.
- Los tests **no deben compartir estado** entre sí. Cada `OcapApiFactory` crea una base de datos InMemory con nombre único.
- El archivo `appsettings.Testing.json` **nunca debe contener credenciales reales o secretos de producción**.
