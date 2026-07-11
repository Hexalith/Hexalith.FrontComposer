---
id: setup-package-and-hosting
title: Package setup and MCP hosting
version: 2.0.0
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

Choose packages by responsibility:

- `Hexalith.FrontComposer.Contracts` for UI-clean attributes, communication contracts, registration,
  schema/MCP descriptors, diagnostics, and UI-neutral seams.
- `Hexalith.FrontComposer.Contracts.UI` for typography, Blazor render-fragment customization
  contexts, and keyboard-event shortcut contracts. Existing public namespaces are retained.
- `Hexalith.FrontComposer.Shell` for the runtime Blazor shell, options, services, and Fluxor actions.
- `Hexalith.FrontComposer.Testing` for the bUnit host, deterministic services, evidence helpers, and
  `InMemoryStorageService`.
- `Hexalith.FrontComposer.SourceTools` as the analyzer package; it remains netstandard2.0 and depends
  only on Contracts even when its generated UI requires Contracts.UI and Shell in the consumer.
- `Hexalith.FrontComposer.Mcp` for MCP hosting. Do not add MCP SDK references to Contracts,
  Contracts.UI, or SourceTools.

Register host-supplied `IFrontComposerMcpTenantToolGate` and
`IFrontComposerMcpResourceVisibilityGate` implementations before `AddFrontComposerMcp`; startup
fails closed when either is absent. Map endpoints with `MapFrontComposerMcp`. Skill resources are
packaged with `.Mcp`; do not create a separate skills package.
<!-- /frontcomposer:section -->
