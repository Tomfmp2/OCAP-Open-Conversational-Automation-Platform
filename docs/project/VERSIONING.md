# Estándar de Versionado y Ciclo de Lanzamiento de OCAP

## Visión General
El proyecto **OCAP (Open Conversational Automation Platform)** sigue estrictamente los estándares internacionales de la industria de software para la gestión de versiones, cambios e historial de publicación.

---

## 1. Semantic Versioning 2.0.0 (SemVer)
El número de versión se representa en el formato de tres componentes:

$$\text{MAJOR}.\text{MINOR}.\text{PATCH}$$

- **MAJOR**: Incrementado ante cambios incompatibles en la API pública, contratos de dominio o arquitectura breaking changes (ej. `v1.0.0`).
- **MINOR**: Incrementado ante la adición de nuevas funcionalidades, módulos o adaptadores compatibles con versiones anteriores (ej. `v1.1.0`, `v1.2.0`, `v1.3.0`).
- **PATCH**: Incrementado ante correcciones de errores, parches de seguridad o parches de calidad completamente retrocompatibles (ej. `v0.4.1`, `v1.3.1`).

---

## 2. Convención de Commits (Conventional Commits 1.0.0)
Todos los commits en el repositorio Git deben seguir la sintaxis de mensajes estructurados:

$$\text{tipo}(\text{alcance opcional}): \text{descripción en minúsculas sin punto final}$$

### Tipos de Commit Permitidos:
- `feat`: Nuevas características o capacidades agregadas al sistema.
- `fix`: Corrección de errores en código de producción.
- `docs`: Cambios o adiciones exclusivas a archivos de documentación (`.md`).
- `test`: Adición o refactorización de suites de prueba unitarias o de integración.
- `refactor`: Cambios en código que ni corrigen errores ni agregan características.
- `chore`: Tareas de mantenimiento, actualización de dependencias o scripts de compilación.

---

## 3. Registro en CHANGELOG.md (Keep a Changelog 1.1.0)
Cada lanzamiento o tagging de versión requiere la actualización del archivo `CHANGELOG.md` en la raíz del proyecto. El archivo agrupa los cambios bajo las siguientes secciones estandarizadas:

- `### Added`: Nuevas funcionalidades introducidas.
- `### Changed`: Modificaciones en funcionalidades existentes.
- `### Deprecated`: Funcionalidades que serán removidas en versiones futuras.
- `### Removed`: Funcionalidades eliminadas.
- `### Fixed`: Corrección de errores.
- `### Security`: Mejoras o vulnerabilidades corregidas.
- `### Documentation`: Cambios y creaciones en la documentación.

---

## 4. Historial de Versiones Publicadas de OCAP

| Versión | Nombre del Hito | Descripción Clave |
| :--- | :--- | :--- |
| `v0.1.0` | Architecture Foundation | Base de Arquitectura Hexagonal y Modular Monolith |
| `v0.2.0` | Core Conversational Engine | Entidades puras del dominio conversacional y casos de uso |
| `v0.3.0` | Persistence Foundation | Integración de EF Core con PostgreSQL |
| `v0.4.0` | API Gateway Foundation | Gateway HTTP REST, controladores DTO y Swagger |
| `v0.4.1` | API Quality Foundation | Middleware de excepciones, Rate Limiting y tests de integración |
| `v0.5.0` | Channel Architecture Foundation | Arquitectura agnóstica de canales con router desacoplado |
| `v0.6.0` | WhatsApp Evolution API Adapter | Adaptador nativo para el canal de WhatsApp Evolution API |
| `v0.7.0` | Agent Engine Foundation | Motor base de agentes conversacionales e intenciones |
| `v0.8.0` | Tool Execution & Google Workspace | Sistema extensible de herramientas y suite Google Workspace |
| `v0.9.0` | Dashboard & Deployment Manager | Panel Blazor WASM y CLI de autohospedaje automatizado |
| `v1.0.0` | Generative AI Engine Foundation | Motor agnóstico de IA Generativa y Prompts dinámicos |
| `v1.1.0` | Identity & Multi-Tenant Security | Autenticación JWT, RBAC granular, API Keys y Multi-Tenant |
| `v1.2.0` | AI Provider Integration & Orchestration | Adaptadores OpenAI, Gemini, Ollama, Failover y Streaming SSE |
| `v1.3.0` | Workflow Automation Engine Foundation | Motor de automatización de workflows con 17 tipos de nodos |
| `v1.3.1` | Production Readiness & Audit Cert. | Auditoría completa, 0 errores, 0 warnings, 74 tests pasando |
| `v1.4.0` | Visual Workflow Builder Foundation | Diseñador visual Drag-and-Drop integrado en Blazor Dashboard |
