---
id: setup-package-and-hosting
title: Package setup and MCP hosting
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/setup/package-and-hosting
order: 20
sourceDoc: docs/skills/frontcomposer/setup/package-and-hosting.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Mcp.Extensions.FrontComposerMcpServiceCollectionExtensions, Hexalith.FrontComposer.Mcp.Extensions.FrontComposerMcpEndpointRouteBuilderExtensions]
---
<!-- frontcomposer:section narrative -->
# Package Setup

Human documentation can expand this into hosting walkthroughs.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Package Setup and Hosting

Reference `Hexalith.FrontComposer.Mcp` for MCP hosting. Do not add MCP SDK references to `Hexalith.FrontComposer.Contracts` or `Hexalith.FrontComposer.SourceTools`.

Register host-supplied tenant gates before `AddFrontComposerMcp`. Map endpoints with `MapFrontComposerMcp`. Skill resources are packaged with `.Mcp`; do not create a separate skills package.
<!-- /frontcomposer:section -->
