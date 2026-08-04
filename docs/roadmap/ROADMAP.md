# ROADMAP OCAP (Open Conversational Automation Platform)

Plan de evolución técnica y entregables por CAP (Capabilities Delivery Roadmap).

---

## Estado real (actualizado)

El roadmap de CAPs 01–20 describe capacidades **entregadas en código base** (API, identidad enterprise, bus de eventos, workflows, RAG PgVector, etc.). Eso **no implica** que cada ítem del catálogo de producto esté listo para producción omnicanal.

### Implementado y operable
- API, JWT/OIDC, MFA, WebAuthn, SAML, SCIM/LDAP, RBAC, API keys
- Workflows (motor + designer API + UI Next.js `/workflows/designer` + Blazor auxiliar)
- IA: OpenAI, Gemini, Ollama, Claude
- Knowledge/RAG: **PgVector** (+ InMemory para pruebas)
- Canales runtime: **Telegram**, **WhatsApp**, **WebChat**
- Event bus: InMemory, RabbitMQ, NATS
- Frontend Next.js + Docker Compose

### Pendiente / parcial
- Canales: Slack, Discord, Microsoft Teams, Google Workspace como canal de mensajería
- Vector stores externos: Qdrant, Chroma, Pinecone (stubs; no backends reales)
- Marketplace, analytics de costos de tokens, unificación total Blazor→Next.js

---

## CAPs históricos (referencia)

- [x] **CAP-01 a CAP-06**: Core Hexagonal, Persistencia EF Core PostgreSQL, Canales Telegram/WhatsApp (+ WebChat), Agentes IA e Inteligencia Multi-Provider.
- [x] **CAP-07**: Workflow Node Execution Framework & Runtime State Machine.
- [x] **CAP-08**: Real-Time Event Bus In-Memory & Distributed System.
- [x] **CAP-09**: API Keys Platform & Webhook Delivery Engine (HMAC SHA-256).
- [x] **CAP-10**: Identity Foundation & OpenIddict OAuth2/OIDC Authorization Server.
- [x] **CAP-11**: SignalR Live Gateway (`/hubs/events`) con Aislamiento Multi-Tenant.
- [x] **CAP-12**: Dashboard Backend Integration REST API (Overview, Workflows, Agents, Channels, Diagnostics).
- [x] **CAP-13**: Frontend Live Integration Next.js SPA + SignalR Streaming + React Query.
- [x] **CAP-14**: OAuth2 Authorization Code Flow + PKCE (RFC 7636) & User Consent Management.
- [x] **CAP-15**: External Identity Providers (Google, Microsoft Entra ID, GitHub, Generic OIDC) & Account Linking/Auto-Provisioning.
- [x] **CAP-16**: Identity & Administration Management (Users, Roles, Permissions, Groups, Tenants, Profile, Lock/Unlock, Invites).
- [x] **CAP-17**: Multi-Factor Authentication (MFA / TOTP RFC 6238) & Passkeys (WebAuthn / FIDO2 Level 2).
- [x] **CAP-18**: Enterprise Single Sign-On SAML 2.0 (SP Metadata, ACS, SLO, AuthnRequest, Claims Mapping).
- [x] **CAP-19**: Enterprise Directory Synchronization (SCIM 2.0 RFC 7643/7644 & LDAP / Active Directory Sync Engine).
- [x] **CAP-20**: Distributed Event Bus (RabbitMQ / NATS JetStream / Outbox / Inbox / DLQ / HA Cluster / Horizontal Scaling).

---

## Fase actual
- **Fase**: Consolidación producto (honestidad de catálogo + WebChat + designer Next.js)
- **Progreso CAPs núcleo**: 20 / 20 en backend de plataforma
- **Estado producción**: Apto para despliegue self-hosted del núcleo; no declarar omnicanal completo ni vector stores externos como listos.
