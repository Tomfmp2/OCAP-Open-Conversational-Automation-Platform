# OCAP Frontend

Next.js 16 App Router UI for OCAP Enterprise.

## Scripts

```bash
npm ci
npm run lint
npm run type-check
npm run build
npm run dev
```

## Runtime

- Auth: JWT + refresh via `ApiClient` / `AuthProvider`
- API base: `NEXT_PUBLIC_API_URL` (empty = same-origin / nginx proxy)
- Guarded routes except `/login`

## Pages

Dashboard, Agents, Channels, Intelligence, Knowledge, Workflows, Monitoring, Security, Developer, Installer, Settings, Login.

Billing/Marketplace analytics pages are not shipped in navigation until backend billing APIs are productized.
