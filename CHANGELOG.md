# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.6.0] - 2026-07-29

### Added
- **External Identity Providers (CAP-15)**: Added OAuth2/OIDC authentication providers for Google, Microsoft Entra ID (Azure AD / Office 365), GitHub, and Generic OIDC.
- **External Auth Abstractions & Service (`IExternalAuthProvider`, `IExternalAuthenticationService`)**: Extensible hexagonal architecture for external identity providers (`GoogleExternalAuthProvider`, `MicrosoftExternalAuthProvider`, `GitHubExternalAuthProvider`, `GenericOidcExternalAuthProvider`).
- **Account Linking & Auto-Provisioning (`IExternalIdentityResolver`)**: Account linking, unlinking, querying linked providers, and configurable automatic user provisioning (`AutoProvisionUsers`).
- **REST API Endpoints (`ExternalAuthController`)**: Endpoints `/api/auth/external/providers`, `/api/auth/external/challenge/{provider}`, `/api/auth/external/callback/{provider}`, `/api/auth/external/linked`, `/api/auth/external/linked/{provider}`.
- **Security & Multi-Tenant**: Multi-tenant data isolation, OAuth state validation, security audit logging for external login, account link and unlink events.
- **Architecture Decision Record**: Created `docs/adr/ADR-004-external-identity-providers.md`.

## [1.5.2] - 2026-07-28

### Added
- **User Context Resolution (`IUserContext`)**: Added `IUserContext` interface and `HttpUserContext` implementation to resolve authenticated user details (UserId, UserName, Email) from Claims.
- **Resilient Background Processing (`OutboxProcessorBackgroundService`)**: Added initial startup delay, exponential backoff (10s to 60s max) upon database unavailability, graceful cancellation handling, and structured warning logs.
- **Reliability & Security Unit/Integration Tests**: Added `UserContextTests`, `AuditSaveChangesInterceptorTests`, and `OutboxProcessorResilienceTests`.

### Fixed
- **AuditSaveChangesInterceptor Debt**: Fully resolved `TODO` comments by dynamically injecting `ITenantContext` and `IUserContext` to resolve active TenantId, UserId, and Client IP.

### Security & Dependencies
- **Security Package Updates**: Updated `Microsoft.IdentityModel.Tokens` and `System.IdentityModel.Tokens.Jwt` from `8.14.0` to `8.21.0` in `OCAP.Security.Infrastructure`.

## [1.5.1] - 2026-07-27

### Added
- **Multi-Tenant Context Resolution (`ITenantContext`)**: Added `ITenantContext` and `HttpTenantContext` resolving `TenantId` dynamically from JWT claims (`tenant_id`), HTTP Header (`X-Tenant-ID`), or default fallback.
- **Enterprise File Upload Validator (`IFileUploadValidator`)**: Implemented strict security file validator supporting extension whitelisting (`.pdf`, `.docx`, `.txt`, `.md`, `.csv`, `.json`, `.html`, `.xml`), 25MB max size limit, cross-platform Path Traversal sanitization, corrupt file detection, and SHA256 checksum calculation.
- **EF Core Persistence for Knowledge Base**: Created `KnowledgeConfigurations` EF Core entity maps and `EfKnowledgeRepositories` (`EfKnowledgeBaseRepository`, `EfKnowledgeDocumentRepository`, `EfKnowledgeChunkRepository`, `EfDocumentProcessingJobRepository`) with strict `.AsNoTracking()` and tenant-scoped query filters.
- **OpenTelemetry Knowledge Telemetry (`IKnowledgeTelemetry`)**: Added decoupled `KnowledgeTelemetry` service utilizing `ActivitySource` ("OCAP.Knowledge") and `Meter` ("OCAP.Knowledge.Metrics") tracking document processing, chunk generation, embedding creation, search latency, and error counts.
- **Security & Multi-Tenant Test Suite**: Expanded unit and security test suite (`OCAP.Knowledge.Tests`) with Path Traversal, disallowed file extensions, file size limits, SHA256 hashing, and multi-tenant data isolation tests.

### Fixed
- **API Multi-Tenant Hardening**: Updated `KnowledgeController` endpoints to consume `ITenantContext` instead of dummy tenant GUIDs.
- **Path Traversal Cross-Platform Normalization**: Normalized backslashes and forward slashes in `SanitizeFileName` preventing bypasses on Linux runtimes.

### Performance
- **Search Term Matching**: Optimized BM25/keyword search term matching using `HashSet<string>` $O(1)$ lookups and early cancellation checks.

## [1.5.0] - 2026-07-27

### Added
- **Enterprise Knowledge Base & RAG Engine**: Added 4 new modular Clean Architecture projects (`OCAP.Knowledge.Domain`, `OCAP.Knowledge.Abstractions`, `OCAP.Knowledge.Application`, `OCAP.Knowledge.Infrastructure`).
- **Domain Model (DDD)**: Aggregate Root (`KnowledgeBase`), Entities (`KnowledgeDocument`, `KnowledgeChunk`, `DocumentProcessingJob`, `DocumentPermission`), Value Objects (`EmbeddingVector`, `DocumentVersion`, `DocumentMetadata`, `KnowledgeSearchResult`), and Enums (`DocumentType`, `DocumentStatus`, `KnowledgeSource`, `DocumentCategory`, `ChunkingStrategy`, `VectorDbProviderType`, `SearchStrategyType`).
- **Document Parsers**: Universal multi-format parser (`IDocumentParser`) with native support for PDF, DOCX, TXT, Markdown, CSV, JSON, HTML, and XML with SHA256 digital signature extraction.
- **Chunking Engine**: Configurable strategies (`SentenceChunker`, `ParagraphChunker`, `SemanticChunker`, `SlidingWindowChunker`) with token window and overlap parameters.
- **Embeddings Providers**: Provider abstraction (`IEmbeddingProvider`) with implementations for OpenAI, Gemini, and Ollama.
- **Vector Database Adapters**: Plug-and-play vector storage abstraction (`IVectorDatabase`) supporting PostgreSQL (`pgvector`), `Qdrant`, `ChromaDB`, and `Pinecone`.
- **Knowledge Retriever & RAG Engine**: Multi-strategy search (`KnowledgeRetriever`) supporting Similarity, Hybrid (Reciprocal Rank Fusion - RRF), Keyword, and Semantic search with strict multi-tenant isolation.
- **AI & Prompt Builder Integration**: Context-injected dynamic RAG system prompt builder (`IPromptBuilder`) with Top-K snippets and exact source citations.
- **Workflow Engine Nodes**: Added 6 Knowledge workflow nodes (`KnowledgeSearch`, `SemanticSearch`, `RetrieveContext`, `AskKnowledgeBase`, `DocumentUpload`, `Reindex`).
- **Blazor Dashboard SPA**: 6 Blazor WASM management pages (`KnowledgeIndex`, `Documents`, `Uploads`, `SearchPlayground`, `EmbeddingsConfig`, `VectorDbStatus`) with main menu navigation integration.
- **API Endpoints**: Gateway controller (`KnowledgeController`) with upload, search, document management, reindexing, and vector status endpoints.
- **Testing & Documentation**: Suite of unit tests (`OCAP.Knowledge.Tests`) and 7 architecture/RAG documentation guides under `docs/knowledge/`.

## [1.4.0] - 2026-07-27

### Added
- Visual Workflow Builder (`OCAP.Workflow.Designer`) inside the Dashboard.
- Drag-and-drop interactive canvas for building workflow graphs using Blazor.
- Property Inspector with two-way data binding for dynamic node configuration.
- API endpoints integration for validating, saving, and executing visual workflows from the Dashboard.
- Workflow Designer DTOs (`WorkflowDesignerSaveRequest`, `WorkflowValidationResult`) and Models (`VisualNode`, `VisualEdge`, `VisualWorkflowGraph`).

## [1.3.1] - 2026-07-27
### Fixed
- Fixed EF Core constructor binding in `WorkflowExecution` aggregate root to ensure parameterless private hydration constructor.
- Resolved constructor parameter warning CS8618 in `User`, `Session`, and `Message` core entities.
- Unified MSBuild resolution settings in `Directory.Build.props` eliminating MSB3277 EF Core version mismatch warnings.
- Fixed `ToolExecutionContext` parameter binding in `ToolNode` workflow node execution.

### Quality & Performance
- Verified 100% test pass rate across all 74 unit and integration tests.
- Reached 0 Errors and 0 Warnings across the entire solution compilation.
- Completed full Production Readiness Audit certified in `docs/reports/PRODUCTION_READINESS_REPORT.md`.

## [1.3.0] - 2026-07-27

### Added
- Workflow Automation Engine Foundation (`OCAP.Workflow.Domain`, `OCAP.Workflow.Abstractions`, `OCAP.Workflow.Application`, `OCAP.Workflow.Infrastructure`).
- Domain Aggregate Root and Entities (`Workflow`, `WorkflowDefinition`, `WorkflowVersion`, `WorkflowExecution`, `WorkflowStep`, `WorkflowTransition`, `WorkflowContext`, `WorkflowVariable`, `WorkflowExecutionHistory`, `WorkflowStatus`, `WorkflowTrigger`, `WorkflowResult`, `WorkflowError`).
- Node Abstractions and 17 Node implementations (`StartNode`, `EndNode`, `ConditionNode`, `LLMNode`, `ToolNode`, `DelayNode`, `WaitNode`, `HumanApprovalNode`, `LoopNode`, `SwitchNode`, `ParallelNode`, `MergeNode`, `WebhookNode`, `ApiRequestNode`, `ScriptNode`, `SubWorkflowNode`, `ErrorHandlerNode`).
- Workflow Execution Engine (`WorkflowEngine`) with step-by-step execution, state machine, pause, resume, cancellation, retries, and history logging.
- Agent & Tool Integration allowing AI Agents to trigger, query, and manage workflows, and executing registered enterprise tools (`IToolRegistry`) within nodes.
- Persistence integration in `OCAPDbContext` with EF Core Fluent API mappings for Workflow definitions, versions, executions, history, and variables.
- API Gateway Endpoints (`GET /api/workflows`, `POST /api/workflows`, `PUT /api/workflows/{id}`, `DELETE /api/workflows/{id}`, `POST /api/workflows/{id}/execute`, `POST /api/workflows/{id}/cancel`, `GET /api/workflows/executions`, `GET /api/workflows/executions/{id}`).
- Dashboard SPA pages in Blazor WebAssembly (`/workflows`, `/workflows/editor`, `/workflows/executions`, `/workflows/history`).
- Unit and integration tests in `tests/OCAP.Workflow.Tests`.

### Documentation
- Workflow Architecture, Engine, Nodes, API, and Execution guides in `docs/workflow/ARCHITECTURE.md`, `ENGINE.md`, `NODES.md`, `API.md`, `EXECUTION.md`.

## [1.2.0] - 2026-07-27

### Added
- Complete AI Provider Integration & Intelligent Orchestration (`OCAP.Providers.OpenAI`, `OCAP.Providers.Gemini`, `OCAP.Providers.Ollama`, `OCAP.Intelligence.Application`).
- Production-ready `OpenAiProvider` with Chat Completions, SSE Streaming, JSON Response format, Timeouts, and Retries via `IHttpClientFactory`.
- Official `GeminiAiProvider` integrating Google Gemini REST API with Safety Settings, System Instructions, and SSE Streaming.
- Self-Hosted `OllamaAiProvider` compatible with localhost, Docker containers, and remote servers with Model Discovery (`/api/tags`).
- Intelligent AI Provider Orchestrator (`AiProviderSelector`) with dynamic selection based on priority, availability, cost, latency, and automatic Failover.
- In-memory AI Response Caching (`InMemoryAiResponseCache`) with configurable TTL for prompt reuse and latency reduction.
- Full Server-Sent Events (SSE) streaming support via `IAsyncEnumerable<string>` across API Gateway and Dashboard.
- API Endpoints for AI Providers (`GET /api/providers`, `GET /api/providers/status`, `GET /api/providers/models`, `POST /api/providers/select`, `POST /api/providers/test`, `POST /api/providers/stream`).
- Dashboard SPA page in Blazor WebAssembly (`Pages/Providers.razor`) for real-time model configuration, failover policies, streaming control, and prompt testing.
- Unit and integration tests for OpenAI, Gemini, Ollama, Provider Selector, and Streaming in `tests/OCAP.Intelligence.Tests`.

### Documentation
- AI Provider, Selector, and Streaming documentation in `docs/providers/OPENAI.md`, `GEMINI.md`, `OLLAMA.md`, `PROVIDER_SELECTOR.md`, `STREAMING.md`.

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
