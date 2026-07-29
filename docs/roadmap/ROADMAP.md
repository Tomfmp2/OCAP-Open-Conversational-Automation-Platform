# ROADMAP OCAP (Open Conversational Automation Platform)

Plan de evolución técnica y entregables por CAP (Capabilities Delivery Roadmap).

---

## 🏆 CAPs Completados

- [x] **CAP-01 a CAP-06**: Core Hexagonal, Persistencia EF Core PostgreSQL, Canales Telegram/WhatsApp, Agentes IA e Inteligencia Multi-Provider.
- [x] **CAP-07**: Workflow Node Execution Framework & Runtime State Machine.
- [x] **CAP-08**: Real-Time Event Bus In-Memory & Distributed System.
- [x] **CAP-09**: API Keys Platform & Webhook Delivery Engine (HMAC SHA-256).
- [x] **CAP-10**: Identity Foundation & OpenIddict OAuth2/OIDC Authorization Server.
- [x] **CAP-11**: SignalR Live Gateway (`/hubs/events`) con Aislamiento Multi-Tenant.
- [x] **CAP-12**: Dashboard Backend Integration REST API (Overview, Workflows, Agents, Channels, Security, Diagnostics).
- [x] **CAP-13**: Frontend Live Integration Next.js SPA + SignalR Streaming + React Query.
- [x] **CAP-14**: OAuth2 Authorization Code Flow + PKCE (RFC 7636) & User Consent Management.
- [x] **CAP-15**: External Identity Providers (Google, Microsoft Entra ID, GitHub, Generic OIDC) & Account Linking/Auto-Provisioning.
- [x] **CAP-16**: Identity & Administration Management (Users, Roles, Permissions, Groups, Tenants, Profile, Lock/Unlock, Activation, Invites).

---

## 🚀 Próximos CAPs

- [ ] **CAP-17**: Multi-Factor Authentication (MFA / TOTP) & Passkeys (WebAuthn).
- [ ] **CAP-18**: Enterprise SSO SAML2 & Directory Synchronization (SCIM).
- [ ] **CAP-19**: Distributed Event Bus (RabbitMQ / NATS) & High Availability Cluster.

---

## 📊 Estado de Progreso General
- **Fase Actual**: Fase 7 — Seguridad & Escalabilidad Empresarial
- **Progreso CAPs**: 16 / 19 completados (**84.2%**)
- **Dependencias**: .NET 10, OpenIddict, EF Core, PostgreSQL, SignalR, Next.js, TanStack Query.
