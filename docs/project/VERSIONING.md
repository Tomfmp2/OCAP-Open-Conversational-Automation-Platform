# OCAP — Política de Versionamiento y Gestión de Releases

## Visión General

Este documento establece la política obligatoria de control de versiones, estándares de commit y ciclo de lanzamientos (*releases*) para la plataforma **Open Conversational Automation Platform (OCAP)**.

Todos los contribuyentes y agentes de desarrollo deben adherirse estrictamente a estas reglas en cada fase del proyecto.

---

## 1. Semantic Versioning (SemVer 2.0.0)

OCAP sigue la especificación de **[Semantic Versioning](https://semver.org/)** con el formato `MAJOR.MINOR.PATCH`:

- **MAJOR (`X.0.0`)**: Cambios incompatibles en la API, reestructuración profunda de la arquitectura o entregas principales de la plataforma.
- **MINOR (`0.X.0`)**: Incorporación de nuevas funcionalidades, nuevos adaptadores o módulos manteniendo compatibilidad hacia atrás.
- **PATCH (`0.0.X`)**: Correcciones de errores (*bugfixes*), refactorizaciones internas menores o parches de seguridad.

---

## 2. Conventional Commits

Todos los commits del repositorio deben utilizar la convención **[Conventional Commits 1.0.0](https://www.conventionalcommits.org/)**:

### Formato
```text
<tipo>(<alcance opcional>): <descripción corta>

[cuerpo opcional]

[nota de cambio rompedor opcional]
```

### Tipos Permitidos
- `feat`: Nueva funcionalidad agregada a la plataforma.
- `fix`: Corrección de un error o bug.
- `docs`: Modificaciones únicamente en la documentación.
- `refactor`: Cambios de código que no corrigen un bug ni agregan una funcionalidad.
- `test`: Adición o corrección de pruebas unitarias o de integración.
- `chore`: Tareas de mantenimiento, configuración de build o dependencias.

---

## 3. Git Tags

Cada versión publicada debe estar asociada a una etiqueta anotada (*annotated Git Tag*) en el repositorio con el prefijo `v`:

```bash
git tag -a v1.0.0 -m "OCAP Generative AI Engine Foundation Release"
```

---

## 4. Keep a Changelog

El archivo `CHANGELOG.md` en la raíz del proyecto es la fuente única de verdad para el historial de cambios.

### Reglas Obligatorias:
1. El archivo se mantiene siguiendo el estándar **[Keep a Changelog](https://keepachangelog.com/en/1.1.0/)**.
2. Cada nueva versión se agrega inmediatamente **encima de la versión anterior**.
3. **Nunca se elimina ni se sobreescribe** el historial de versiones pasadas.
4. Las secciones utilizadas para cada versión son:
   - `### Added` (Nuevas funcionalidades)
   - `### Changed` (Modificaciones a código existente)
   - `### Deprecated` (Funcionalidades marcadas para desuso)
   - `### Removed` (Funcionalidades eliminadas)
   - `### Fixed` (Correcciones de errores)
   - `### Security` (Parches de seguridad)
   - `### Documentation` (Cambios significativos en docs)

---

## 5. Flujo de Publicación de Futuras Versiones (Release Workflow)

Al finalizar el desarrollo de cualquier nueva fase o versión del proyecto, es **obligatorio** ejecutar el siguiente flujo en orden:

```text
1. Desarrollo & Pruebas Completadas (dotnet build & dotnet test en 100%)
                   │
                   ▼
2. Actualización de CHANGELOG.md con la nueva sección ## [X.Y.Z]
                   │
                   ▼
3. Actualización de la documentación correspondiente en /docs
                   │
                   ▼
4. Creación del Commit Convencional (git commit -m "tipo(scope): descripción")
                   │
                   ▼
5. Creación del Tag Git Anotado (git tag -a vX.Y.Z -m "Mensaje de Release")
                   │
                   ▼
6. Verificación de Historial (git log -1 & git tag -l)
```
