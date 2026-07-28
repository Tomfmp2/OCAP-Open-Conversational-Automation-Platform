# OCAP Architecture Specification: CAP-02 Dynamic Channel Configuration & Runtime Management

## 1. Executive Summary

CAP-02 establishes the enterprise runtime management system for communication channels in OCAP (Open Conversational Automation Platform), transforming static configuration files into a dynamic SaaS multi-tenant system.

## 2. Architecture Principles

1. **Channels as Adapters Only:** Channels handle input/output translation to/from `IncomingChannelMessage` and `OutgoingChannelMessage`. Zero business logic, reasoning, or prompt engineering resides in channels.
2. **Strict Multi-Tenant Isolation:** Every `ChannelConnection` is tied to a specific `TenantId`. Unique composite index on `(TenantId, Provider)`.
3. **Zero Plaintext Secrets (`ICredentialVault`):** All bot tokens, API keys, and refresh tokens are encrypted using AES-256 (CBC with per-tenant key derivation) and stored securely. Plaintext secrets are never returned in API models or logs.
4. **Hexagonal Boundaries:** Channel management components in `OCAP.Channels.Abstractions` have zero dependencies on `OCAP.Intelligence`, `OCAP.Workflow`, or `OCAP.Knowledge`.

## 3. Integration Points

### 3.1 WhatsApp QR Integration (Future SaaS Onboarding)
- When a user selects WhatsApp in the guided setup, `ChannelManagementController` creates a `ChannelConnection` with status `PendingActivation`.
- `IChannelHealthChecker` queries the WhatsApp Evolution API for instance QR status, exposing auto-refreshing QR streams.

### 3.2 Google Workspace OAuth Integration
- Google Workspace connection is mandatory for full enterprise capabilities (Email, Calendar, Drive).
- Uses `ICredentialVault` to store OAuth refresh tokens generated after browser authorization flow.

### 3.3 Guided Installer Integration
- The CLI/Guided Installer interacts directly with `/api/channels` and `/api/channels/connect` to provision tenant channels dynamically.
