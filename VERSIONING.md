# OCAP Versioning Policy & History

This document outlines the versioning strategy, semantic versioning rules, and historical release log for **OCAP (Open Conversational Automation Platform)**.

---

## Versioning Strategy

OCAP follows [Semantic Versioning (SemVer 2.0.0)](https://semver.org/):

`MAJOR.MINOR.PATCH`

- **MAJOR**: Breaking structural or architectural changes that alter public APIs or domain contracts.
- **MINOR**: New modules, engine capabilities, framework components, or feature expansions backward-compatible with previous versions.
- **PATCH**: Backward-compatible bug fixes, performance optimizations, and security patches.

---

## Release History

| **v1.5.2** | 2026-07-28 | **Enterprise Reliability & Operational Excellence** (User Context IUserContext/HttpUserContext, Resilient Background Processing with Exponential Backoff, Audit Trail Tenant/User Resolution, IdentityModel 8.21.0 Security Updates) | **Production Ready** |
| **v1.5.1** | 2026-07-27 | **Enterprise Hardening, Security Audit & Production Validation** (Multi-Tenant Context ITenantContext, Security File Upload Validator, EF Core Persistence, OpenTelemetry Knowledge Telemetry, Security & Tenant Test Suite) | Production Ready |
| **v1.5.0** | 2026-07-27 | **Enterprise Knowledge Base & RAG Engine** (PDF, DOCX, TXT, MD, CSV, JSON, HTML, XML parsers, Chunking engine, Embeddings OpenAI/Gemini/Ollama, Vector DBs PgVector/Qdrant/ChromaDB/Pinecone, RAG Retriever, Workflow Nodes, Blazor Dashboard UI) | Production Ready |
| **v1.4.0** | 2026-07-27 | **Visual Workflow Designer** (Interactive Blazor canvas, drag-and-drop nodes, Property Inspector, Execution Engine API integration) | Production Ready |
| **v1.3.1** | 2026-07-27 | **Production Readiness Audit & Bug Fixes** (EF Core constructor hydration fix, warning CS8618 cleanup, 100% test pass rate) | Production Ready |
| **v1.3.0** | 2026-07-27 | **Workflow Automation Engine** (State machine, 17 node types, tool execution, Blazor Dashboard SPA) | Production Ready |
| **v1.2.0** | 2026-07-27 | **AI Providers & Intelligence Engine** (OpenAI, Claude, Gemini, Ollama adapters) | Production Ready |
| **v1.1.0** | 2026-07-27 | **Multi-Tenant Security & RBAC** (JWT authentication, tenant isolation, role management) | Production Ready |
| **v1.0.0** | 2026-07-27 | **Initial Core Platform Release** (Clean Architecture, DDD, Modular Monolith foundation) | Production Ready |

---

## Compatibility Commitments

- **Multi-Tenant Isolation**: Zero cross-tenant data leakage guaranteed across all releases.
- **Workflow & Knowledge Compatibility**: All v1.x workflow nodes and knowledge retriever contracts remain fully backward compatible.
- **API Stability**: Public REST API contracts follow strict depreciation policies prior to any breaking changes.
