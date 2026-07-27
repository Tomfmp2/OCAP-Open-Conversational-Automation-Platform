# OCAP — Especificación de API REST de Workflows

## Endpoints HTTP
- `GET /api/workflows`: Listado de definiciones de workflows.
- `POST /api/workflows`: Creación de un nuevo workflow declarativo JSON.
- `PUT /api/workflows/{id}`: Edición de un workflow.
- `DELETE /api/workflows/{id}`: Eliminación de un workflow.
- `POST /api/workflows/{id}/execute`: Ejecución de una instancia del workflow.
- `POST /api/workflows/{id}/cancel`: Cancelación de una ejecución activa.
- `GET /api/workflows/executions`: Consulta de ejecuciones por Tenant.
- `GET /api/workflows/executions/{id}`: Detalle de ejecución.
