# OCAP — Asistente de Despliegue (Deployment Manager Foundation)

## Descripción General

**OCAP Deployment Manager** es la herramienta oficial de asistencia interactiva para el autohospedaje (*self-hosting*) seguro y guiado de la plataforma OCAP.

Diseñada como una herramienta de consola independiente (`OCAP.DeploymentManager`), guía a los administradores paso a paso sin realizar instalaciones silenciosas ni no supervisadas.

---

## Flujo del Asistente

```
OCAP Deployment Manager
        │
        ▼
1. Selección del Modo de Instalación
   ├── [1] Desarrollo Local
   ├── [2] Servidor Personal
   └── [3] Servidor Empresarial
        │
        ▼
2. Configuración de Base de Datos PostgreSQL
   ├── Host, Puerto, Base de Datos, Usuario y Contraseña
        │
        ▼
3. Configuración de Canales (WhatsApp Evolution API / Telegram)
        │
        ▼
4. Configuración de Google Workspace (OAuth Client ID / Secret)
        │
        ▼
5. Generación de Claves de Seguridad & Validación (.env)
        │
        ▼
6. Verificación de Docker Compose & Ejecución de Contenedores
```

---

## Generación del Archivo `.env`

El servicio `EnvironmentGenerator` produce un archivo `.env` estandarizado en la raíz del proyecto con variables como:

- `POSTGRES_HOST`, `POSTGRES_PORT`, `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`
- `EVOLUTION_API_URL`, `EVOLUTION_API_KEY`
- `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`, `GOOGLE_REDIRECT_URI`
- `JWT_SECRET_KEY`
