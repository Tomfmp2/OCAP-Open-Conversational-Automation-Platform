# OCAP — Control de Acceso Basado en Roles (RBAC)

## Permisos Granulares
El modelo RBAC de OCAP define los siguientes permisos estándar:
- `Conversation.Read`, `Conversation.Write`, `Conversation.Delete`
- `Agent.Read`, `Agent.Write`, `Agent.Execute`
- `Tool.Execute`
- `Dashboard.Read`, `Dashboard.Admin`
- `Deployment.Manage`
- `AI.Execute`
- `Settings.Manage`, `OAuth.Manage`

## Roles por Defecto
1. **Admin**: Acceso completo a la plataforma y administración de usuarios/tenants.
2. **Operator**: Operación conversacional, edición de agentes e invocación de herramientas.
3. **Viewer**: Visualización de métricas e historial conversacional sin permisos de modificación.
