# Checklist E2E — flujo crítico OCAP

Validación manual (o Playwright `frontend/e2e/smoke.spec.ts`) del camino feliz tras el plan de mejora.

## Prerrequisitos

- `./scripts/ocap-up.sh` o `docker compose up --build -d`
- Configuración inicial hecha en `/setup`, o admin por entorno (`BOOTSTRAP_ADMIN_EMAIL` / `BOOTSTRAP_ADMIN_PASSWORD`);
  exporta esas credenciales como `OCAP_ADMIN_EMAIL` / `OCAP_ADMIN_PASSWORD` para Playwright

## Pasos

1. **Configuración inicial** — en una instalación nueva, `http://localhost:3000` lleva a `/setup`; con el código de
   los logs se crea el admin y la sesión queda iniciada. En una instalación configurada, `/setup` redirige a `/login`.
2. **Login** — `http://localhost:3000/login` → dashboard `/`.
3. **Canales** — `/channels` → conectar **WebChat** (solo DisplayName + título).
4. **WebChat** — `/channels/webchat` → enviar un mensaje; debe devolver reply del Enterprise Assistant o fallback.
5. **Workflow designer** — `/workflows/designer` → añadir Start → LLM → End → Validar → Guardar.
6. **Knowledge** — crear KB solo con **PgVector** o **InMemory** (Qdrant ya no aparece en el selector).
7. **Catálogo API** — `GET /api/channels` debe incluir `isImplemented: true` solo para Telegram, WhatsApp y WebChat.

## Automatizado

```bash
cd frontend
npm run e2e
```

Cubre login, navegación (incluye `/channels/webchat` y `/workflows/designer`), la configuración inicial pública y smoke del toolbox del diseñador.
