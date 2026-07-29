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
- [x] **CAP-12**: Dashboard Backend Integration REST API (Overview, Workflows, Agents, Channels, Diagnostics).
- [x] **CAP-13**: Frontend Live Integration Next.js SPA + SignalR Streaming + React Query.
- [x] **CAP-14**: OAuth2 Authorization Code Flow + PKCE (RFC 7636) & User Consent Management.
- [x] **CAP-15**: External Identity Providers (Google, Microsoft Entra ID, GitHub, Generic OIDC) & Account Linking/Auto-Provisioning.
- [x] **CAP-16**: Identity & Administration Management (Users, Roles, Permissions, Groups, Tenants, Profile, Lock/Unlock, Invites).
- [x] **CAP-17**: Multi-Factor Authentication (MFA / TOTP RFC 6238) & Passkeys (WebAuthn / FIDO2 Level 2).
- [x] **CAP-18**: Enterprise Single Sign-On SAML 2.0 (SP Metadata, ACS, SLO, AuthnRequest, Claims Mapping).
- [x] **CAP-19**: Enterprise Directory Synchronization (SCIM 2.0 RFC 7643/7644 & LDAP / Active Directory Sync Engine).

---

## 🚀 Próximos CAPs

- [ ] **CAP-20**: Distributed Event Bus (RabbitMQ / NATS) & High Availability Cluster.

---

## 📊 Estado de Progreso General
- **Fase Actual**: Fase 7 — Seguridad & Escalabilidad Empresarial
- **Progreso CAPs**: 19 / 20 completados (**95.0%**)
- **Dependencias**: .NET 10, OpenIddict, EF Core, PostgreSQL, SignalR, Next.js, TanStack Query.
