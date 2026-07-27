# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-07-27

### Added
- Complete Identity, Authentication & Multi-Tenant Security Foundation (`OCAP.Security.Domain`, `OCAP.Security.Abstractions`, `OCAP.Security.Application`, `OCAP.Security.Infrastructure`).
- Domain entities: `UserIdentity`, `Tenant`, `TenantMember`, `Role`, `Permission`, `RefreshToken`, `ApiKey`, `UserSession`, `AuditLog`.
- Secure Password Hashing using PBKDF2 with SHA256 and dynamic salt (`PasswordHasher`).
- Structured JWT Access Token generation with tenant, role, and granular permission claims (`JwtTokenService`).
- Secure API Key generation and SHA256 hashed validation (`ApiKeyService`).
- Security Audit Logging service for tracking login, logout, tenant, role, and API Key events (`SecurityAuditService`).
- Security API Gateway Controllers (`AuthController`, `UsersController`, `RolesController`, `PermissionsController`, `TenantsController`, `ApiKeysController`, `SessionsController`).
- HTTP Security Headers Middleware (`SecurityHeadersMiddleware` with CSP, HSTS, X-Frame-Options, X-Content-Type-Options).
- Dashboard SPA pages in Blazor WebAssembly (`/login`, `/profile`, `/users`, `/roles`, `/permissions`, `/tenants`, `/api-keys`, `/sessions`).
- Comprehensive Security test suite in `tests/OCAP.Security.Tests`.

### Documentation
- Authentication, JWT, RBAC, Multi-Tenant, API Keys, and Security guides in `docs/security/`.

## [1.0.0] - 2026-07-27

### Added
- Provider-agnostic Generative AI Engine Foundation (`OCAP.Intelligence.Abstractions`, `OCAP.Intelligence.Domain`).
- Dynamic Prompt System (`OCAP.Prompts`, `PromptTemplate`, `SystemPromptBuilder`).
- Agent Reasoning Engine (`AgentReasoningService` in `OCAP.Intelligence.Application`).
- Mock AI Provider (`MockAiProvider` in `OCAP.Intelligence.Mock`).
- Provider adapter structures for OpenAI (`OCAP.Providers.OpenAI`), Google Gemini (`OCAP.Providers.Gemini`), and Ollama (`OCAP.Providers.Ollama`).
- Database entities `AiConversationMemory` and `AiExecutionLog` in `OCAPDbContext`.
- AI Gateway Endpoints (`GET /api/ai/status`, `GET /api/ai/usage`, `GET /api/ai/models`).
- AI Monitoring panel in Blazor WASM Dashboard (`Pages/AI.razor`).
- Comprehensive unit test suite `OCAP.Intelligence.Tests`.

### Documentation
- Architecture, Prompts, and AI Providers guides in `docs/intelligence/`.

## [0.9.0] - 2026-07-27

### Added
- Blazor WebAssembly Administration Dashboard (`OCAP.Dashboard`).
- Interactive SPA pages (`Home`, `Conversations`, `Agents`, `Tools`, `Integrations`).
- Expanded API Gateway controllers (`DashboardController`, `AgentsController`, `ToolsController`, `ExecutionsController`, `IntegrationsController`).
- Guided self-hosting CLI tool (`OCAP.DeploymentManager`).
- Environment generator, deployment validator, and Docker Compose helper services.
- Multi-container Docker infrastructure (`backend`, `dashboard`, `postgres`, `evolution-api`, `nginx`).
- Security RBAC foundation and `CustomAuthenticationStateProvider`.
- Unit and integration tests for Dashboard, Deployment Manager, and API endpoints.

### Documentation
- Dashboard and Deployment Manager guides in `docs/dashboard/` and `docs/deployment/`.

## [0.8.0] - 2026-07-27

### Added
- Extensible Tool Execution System (`OCAP.Tools.Abstractions`, `ITool`, `IToolRegistry`).
- Permission System & Security Abstractions (`OCAP.Security.Abstractions`, `IPermissionValidator`).
- Google Workspace Provider Abstractions & Mock Providers (`Calendar`, `Gmail`, `Sheets`).
- Executable Google Workspace Tools (`CreateCalendarEventTool`, `SendEmailTool`, `AppendSpreadsheetRowTool`).
- Agent Tool Permissions, Tool Executions, and OAuth Connection entities in `OCAPDbContext`.
- Unit and integration test suite `OCAP.Tools.Tests`.

### Documentation
- Tool execution architecture and Google Workspace provider guides in `docs/architecture/TOOLS.md` and `docs/providers/GOOGLE_WORKSPACE.md`.

## [0.7.0] - 2026-07-27

### Added
- Conversational Agent Engine Foundation (`OCAP.Agents.Domain`, `OCAP.Agents.Abstractions`, `OCAP.Agents.Application`).
- Entities `Agent`, `ConversationContext`, `AgentAction`, `Intent`.
- Intent Resolution Engine (`RuleBasedIntentResolver`) and Action Dispatcher (`ActionDispatcher`).
- `ProcessAgentMessageUseCase` for orchestrating agent decision loops.
- Unit and integration test suite `OCAP.Agents.Tests`.

### Documentation
- Agent Engine Architecture guide in `docs/architecture/AGENT_ENGINE.md`.

## [0.6.0] - 2026-07-27

### Added
- WhatsApp Evolution API Channel Adapter (`OCAP.Channels.WhatsApp`).
- Evolution API Client, DTOs, and Webhook Receiver Endpoint (`/api/webhooks/whatsapp`).
- HMAC Security validation middleware for webhooks.
- Channel registration and dependency injection extensions.
- Docker Compose configuration for local Evolution API container.
- Test suite for WhatsApp channel adapter in `OCAP.Channels.Tests`.

### Documentation
- WhatsApp Evolution API integration guide in `docs/channels/WHATSAPP_EVOLUTION.md`.

## [0.5.0] - 2026-07-27

### Added
- Provider-agnostic Channel Architecture Foundation (`OCAP.Channels.Abstractions`).
- Interfaces `IChannelAdapter`, `IChannelMessageReceiver`, `IChannelMessageSender`.
- Decoupled Inbound and Outbound Channel DTOs (`ChannelMessage`, `ChannelSender`).
- Channel Registry and Router (`ChannelRegistry`, `ChannelRouter`).
- Mock Channel Provider for testing (`OCAP.Channels.Mock`).
- Channel architecture test suite in `OCAP.Channels.Tests`.

### Documentation
- Channel Architecture guide in `docs/architecture/CHANNEL_ARCHITECTURE.md`.

## [0.4.1] - 2026-07-27

### Added
- Comprehensive API Gateway Quality & Integration Testing Foundation.
- Integration test suite using `WebApplicationFactory` (`tests/OCAP.Api.Tests`).
- Endpoints integration tests (`MessagesEndpointTests`, `ConversationsEndpointTests`, `HealthCheckTests`).
- Exception Handling Middleware tests (`ExceptionHandlingMiddlewareTests`).
- Rate Limiting and Security Header configuration.

### Documentation
- API Quality & Testing guide in `docs/api/QUALITY_AND_TESTING.md`.

## [0.4.0] - 2026-07-27

### Added
- API Gateway Foundation (`OCAP.Api`).
- REST Controllers (`MessagesController`, `ConversationsController`, `HealthController`).
- DTO Contracts (`IncomingMessageRequest`, `ConversationHistoryResponse`).
- Global Exception Handling Middleware (`ExceptionHandlingMiddleware`).
- Swagger/OpenAPI documentation and Kestrel request limit configuration.

### Documentation
- API Gateway Specification in `docs/api/GATEWAY.md`.

## [0.3.0] - 2026-07-27

### Added
- Persistence Foundation (`OCAP.Infrastructure`).
- Entity Framework Core with PostgreSQL integration (`OCAPDbContext`).
- Repository implementations (`UserRepository`, `ConversationRepository`, `MessageRepository`).
- EF Core Entity Configurations and initial database migrations.
- Unit and Integration test suite `OCAP.IntegrationTests`.

### Documentation
- Database Schema and Persistence guide in `docs/persistence/PERSISTENCE.md`.

## [0.2.0] - 2026-07-27

### Added
- Core Conversational Engine (`OCAP.Core` and `OCAP.Application`).
- Pure Domain Entities (`User`, `Conversation`, `Message`, `Session`).
- Business Use Cases (`ReceiveMessageUseCase`, `GetConversationHistoryUseCase`).
- Core unit test suite `OCAP.UnitTests`.

### Changed
- Refactored core domain models to follow strict DDD value objects and invariants.

## [0.1.0] - 2026-07-27

### Added
- Initial OCAP architecture foundation.
- Hexagonal architecture structure (Ports & Adapters).
- Modular monolith organization (`src/Core`, `src/Application`, `src/Infrastructure`, `src/Api`).
- Open source documentation standards and repository setup.
