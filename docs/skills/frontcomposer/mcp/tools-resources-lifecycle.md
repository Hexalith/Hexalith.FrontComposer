---
id: mcp-tools-resources-lifecycle
title: MCP tools resources and lifecycle
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/mcp/tools-resources-lifecycle
order: 70
sourceDoc: docs/skills/frontcomposer/mcp/tools-resources-lifecycle.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Mcp.FrontComposerMcpOptions, Hexalith.FrontComposer.Mcp.Invocation.FrontComposerMcpLifecycleTracker]
---
<!-- frontcomposer:section narrative -->
# MCP Flow

The documentation site can include broader protocol background.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# MCP Tools, Resources, And Lifecycle

Use MCP tools to submit commands and resources to read projections. For command write flows, expect a two-call lifecycle: invoke the command, then read the framework-issued lifecycle URI or call the lifecycle subscribe tool until terminal state.

Treat hidden or unknown tools/resources as safe, sanitized categories. Do not infer hidden command names from policy results.
<!-- /frontcomposer:section -->
