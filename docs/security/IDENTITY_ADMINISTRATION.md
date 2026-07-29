# Módulo de Administración de Identidades (CAP-16)

Documentación técnica de arquitectura y API REST del módulo de administración de identidades, usuarios, grupos, roles y tenants en OCAP.

## Endpoints REST API

### Usuarios (`/api/users`)
- `GET /api/users`: Listar usuarios de la organización.
- `GET /api/users/{id}`: Detalles de un usuario por ID.
- `POST /api/users/invite`: Invitar usuario a la plataforma.
- `POST /api/users/reset-password`: Iniciar solicitud de restablecimiento de contraseña.
- `POST /api/users/change-password`: Cambiar contraseña del usuario autenticado.
- `POST /api/users/{id}/lock`: Bloquear usuario administrativamente.
- `POST /api/users/{id}/unlock`: Desbloquear usuario.
- `POST /api/users/{id}/activate`: Activar usuario.
- `POST /api/users/{id}/deactivate`: Desactivar usuario.

### Grupos (`/api/groups`)
- `GET /api/groups`: Listar grupos de la organización.
- `GET /api/groups/{id}`: Obtener detalle de grupo.
- `POST /api/groups`: Crear nuevo grupo.
- `DELETE /api/groups/{id}`: Eliminar grupo.
- `POST /api/groups/{id}/users`: Añadir usuario a un grupo.
- `DELETE /api/groups/{id}/users/{userId}`: Remover usuario de un grupo.
- `POST /api/groups/{id}/roles`: Asignar rol a un grupo.

### Perfil (`/api/profile`)
- `GET /api/profile`: Obtener información del perfil del usuario autenticado.
- `POST /api/profile/change-password`: Cambiar contraseña del usuario en sesión.

### Organizaciones (`/api/tenants`)
- `GET /api/tenants`: Listar tenants.
- `GET /api/tenants/{id}`: Detalle de tenant.
- `POST /api/tenants`: Crear nuevo tenant.
- `GET /api/tenants/{id}/members`: Listar miembros del tenant.
- `POST /api/tenants/{id}/members`: Añadir miembro al tenant.
