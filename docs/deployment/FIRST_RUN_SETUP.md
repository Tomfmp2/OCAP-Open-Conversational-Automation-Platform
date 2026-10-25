# Instalación y configuración inicial

OCAP separa la instalación en dos partes con responsables distintos:

| Parte | Qué cubre | Dónde se hace | Cuándo |
| --- | --- | --- | --- |
| Infraestructura | Puertos, base de datos, mensajería, secretos | `./scripts/ocap-setup.sh` → `.env` | Antes de arrancar |
| Producto | Organización, administrador, proveedor de IA | Panel: `http://localhost:3000/setup` | En el primer arranque |

La API nunca escribe archivos de configuración: lo que necesita reiniciar contenedores (puertos, base de datos,
secretos) se decide antes de arrancar, y lo del producto se guarda en la base de datos.

## 1. Infraestructura: `ocap-setup.sh`

```bash
./ocap install   # una vez: ejecuta ocap-setup.sh, construye e instala el comando
ocap             # levanta todo y espera a que responda
```

`ocap-setup.sh` crea `.env` desde `.env.example` (permisos `600`) y rellena con valores aleatorios los secretos
vacíos: `POSTGRES_PASSWORD`, `RABBITMQ_PASSWORD`, `JWT_SECRET_KEY`, `VAULT_MASTER_KEY` y `EVOLUTION_API_KEY`.
Es idempotente: nunca cambia un valor que ya tenga contenido. `docker compose` se niega a arrancar si falta
alguno de esos secretos; no hay valores por defecto publicados en el repositorio.

> Cambiar `POSTGRES_PASSWORD` después del primer arranque rompe la conexión con la base existente:
> para empezar de cero usa `ocap reset`.

## 2. Producto: configuración inicial en `/setup`

Mientras la base no tenga usuarios, la API imprime en sus logs un **código de configuración** de un solo uso
(`ABCD-EFGH-JKLM`). `ocap` lo muestra al terminar; también se ve con:

```bash
docker compose logs ocap-api | grep 'código'
```

El asistente tiene tres pasos:

1. **Cuenta** (obligatorio): código, nombre de la organización y cuenta del administrador. Al enviarlo se crea la
   organización y el admin, y la sesión queda iniciada.
2. **IA** (opcional): proveedor, modelo y API key; se guarda cifrada y se prueba en el momento.
3. **Listo**: accesos a Canales, Conocimiento e IA y modelos.

Después, todo se cambia desde el panel (IA y modelos, Canales, Ajustes); `/setup` deja de estar disponible.

### Por qué un código

Sin él, quien llegue primero a la URL de una instalación nueva podría crear el administrador. El código solo
lo ve quien tiene acceso al servidor (logs de la API), el mismo patrón que usan Grafana o Jenkins. Además:

- La comparación es de tiempo constante y el endpoint está detrás del rate limiting global.
- Dos envíos simultáneos no pueden crear dos administradores (se serializan y se vuelve a comprobar).
- Nunca se modifica una cuenta existente: con usuarios en la base, `POST /api/setup` responde `409`.

### API

| Método | Ruta | Respuesta |
| --- | --- | --- |
| `GET` | `/api/setup/status` | `{ "required": true \| false }` |
| `POST` | `/api/setup` | `{ setupCode, organizationName, adminName, adminEmail, adminPassword }` → sesión (`accessToken`, `refreshToken`, …) |

Errores: `400` (validación por campo), `403` (código inválido), `409` (ya configurado).
La contraseña debe tener al menos 12 caracteres con letras y números.

## Administrador por entorno (opcional)

Para despliegues automatizados (CI, demos efímeras) o desarrollo con base en memoria, define
`BOOTSTRAP_ADMIN_EMAIL` y `BOOTSTRAP_ADMIN_PASSWORD` en `.env`: la API crea ese admin al arrancar si la base está
vacía y no pide la configuración inicial. Se aplica la misma política de contraseña; si no se cumple, no se crea.
Quita la contraseña del entorno una vez creado el admin.
