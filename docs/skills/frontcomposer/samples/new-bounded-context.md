---
id: samples-new-bounded-context
title: New bounded context sample
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/samples/new-bounded-context
order: 90
sourceDoc: docs/skills/frontcomposer/samples/new-bounded-context.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Contracts.Registration.FrontComposerRegistryExtensions, Hexalith.FrontComposer.Contracts.Mcp.McpManifest]
samplePaths: [samples/Counter/Counter.Domain, samples/Counter/Counter.Web]
---
<!-- frontcomposer:section narrative -->
# New Bounded Context Sample

Human docs can compare this to the Counter sample.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# New Bounded Context Sample

Create a domain project, command type, projection type, validator/tests, and registration. Let SourceTools emit generated partials and MCP manifest output. Do not edit `.g.cs` files. Keep infrastructure calls behind approved framework services; do not call EventStore directly from generated command inputs.
<!-- /frontcomposer:section -->
