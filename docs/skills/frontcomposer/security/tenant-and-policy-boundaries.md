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

Use `[RequiresPolicy]` for command authorization metadata. Tenant isolation is host-owned and fail-closed. Skill resources are framework reference material and are not tenant-filtered because they contain no domain data.

Do not persist prompt secrets, tenant IDs, tokens, claims, customer data, raw provider internals, or generated payload values in benchmark artifacts.
<!-- /frontcomposer:section -->
