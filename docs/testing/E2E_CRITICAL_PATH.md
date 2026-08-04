# Checklist E2E — flujo crítico OCAP

Validación manual (o Playwright `frontend/e2e/smoke.spec.ts`) del camino feliz tras el plan de mejora.

## Prerrequisitos

- `./scripts/ocap-up.sh` o `docker compose up --build -d`
- Credenciales admin (bootstrap o `.env`)

## Pasos

1. **Instalador** — abrir `http://localhost:3000/installer` sin login; debe mostrar el asistente.
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

Cubre login, navegación (incluye `/channels/webchat` y `/workflows/designer`), instalador público y smoke del toolbox del diseñador.
