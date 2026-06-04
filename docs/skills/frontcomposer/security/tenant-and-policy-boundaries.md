---
id: security-tenant-and-policy-boundaries
title: Tenant and policy boundaries
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/security/tenant-and-policy-boundaries
order: 60
sourceDoc: docs/skills/frontcomposer/security/tenant-and-policy-boundaries.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Contracts.Attributes.RequiresPolicyAttribute, Hexalith.FrontComposer.Mcp.IFrontComposerMcpTenantToolGate]
---
<!-- frontcomposer:section narrative -->
# Tenant And Policy Boundaries

Human docs may add diagrams and security explanations.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Tenant and Policy Boundaries

Use `[RequiresPolicy]` for command authorization metadata. Policy names are host-owned stable identifiers, not tenant data, claim data, command payloads, or tokens. Protected commands fail closed in presentation and dispatch: missing policy metadata, missing auth state, stale tenant state, evaluator failures, and denied decisions must not run command side effects.

Tenant isolation is host-owned and fail-closed. Do not accept `TenantId`, `UserId`, `MessageId`, or `CorrelationId` from agent or operator input; FrontComposer injects framework identity from host context and `IUlidFactory`. Skill resources are framework reference material and are not tenant-filtered because they contain no domain data.

Do not persist prompt secrets, tenant IDs, tokens, claims, customer data, raw provider internals, or generated payload values in benchmark artifacts.
<!-- /frontcomposer:section -->
