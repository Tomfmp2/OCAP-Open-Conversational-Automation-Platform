# OCAP Persistence Foundation

Esta fase define la persistencia en OCAP utilizando Entity Framework Core y PostgreSQL, respetando los límites de la arquitectura hexagonal.

## Arquitectura

- **Capa de Dominio (`OCAP.Core`)**: Contiene las entidades, value objects, domain events, y los *puertos* o contratos (`IConversationRepository`, etc.).
- **Capa de Aplicación (`OCAP.Application`)**: Orquesta los casos de uso implementando lógica de negocio sin depender de bases de datos.
- **Capa de Infraestructura (`OCAP.Infrastructure`)**: Implementa los puertos definidos por el Core mediante *adaptadores* (e.g. `UserRepository`, `ConversationRepository`). Aquí es donde reside EF Core, Npgsql y el `DbContext`.

### Decisión de PostgreSQL
PostgreSQL es la base de datos principal para el almacenamiento de memoria y persistencia de OCAP debido a su rendimiento, fiabilidad y amplia adopción en el entorno Open Source.

### Estrategia de Migraciones y Auto Migration
El sistema utiliza el mecanismo nativo de migraciones de EF Core. En el arranque de la aplicación, el método de extensión `ApplyMigrationsAsync` revisa las migraciones faltantes en la base de datos y las ejecuta si es necesario. Esto facilita los despliegues empresariales y la ejecución en contenedores Docker sin requerir herramientas externas.

## Repositorios y Separación de Responsabilidades
Los repositorios creados (como `ConversationRepository`) encapsulan todo acceso a datos para las entidades específicas mediante los `DbSet`. No exponen tipos de base de datos a las capas superiores, traduciendo a las interfaces limpias (como `IConversationRepository`).
