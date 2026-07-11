---
title: "Source generation and MCP split"
description: "Why FrontComposer keeps human documentation and agent reference slices in one source model."
genre: concept
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-07-11
uid: frontcomposer.concepts.source-generation-and-mcp-split
slug: concepts/source-generation-and-mcp-split/
---

# Source generation and MCP split

FrontComposer uses domain attributes as the source of truth, then generates UI, state, diagnostics, CLI evidence, and MCP-safe reference material from that model.

The dependency direction is intentionally split:

- `Hexalith.FrontComposer.Contracts` is the UI-clean `net10.0;netstandard2.0` kernel for attributes,
  communication, registration, schema, MCP descriptors, diagnostics, and UI-neutral seams.
- `Hexalith.FrontComposer.Contracts.UI` is the net10-only package for Blazor/Fluent typography,
  render-fragment customization contexts, and keyboard-event shortcut contracts.
- `Hexalith.FrontComposer.SourceTools` is a netstandard2.0 analyzer that references and embeds only
  Contracts. Generated UI code is compiled by consumers that reference Contracts.UI and Shell.
- Shell owns runtime options, registries, and Fluxor actions; Testing owns adopter fakes; Schema and
  MCP stay on kernel contracts unless an explicitly approved contract changes that boundary.

Query criteria follow the same composition principle: `ProjectionQuery` owns projection criteria,
while `QueryRequest.Create` adds tenant, EventStore routing, ETags, and cache metadata. The v1.12
flattened source and JSON surface remains a migration shim; JSON stays flat.

Human documentation explains sequence and tradeoffs. Agent documentation needs stable reference sections, sanitized examples, and reproducible fingerprints. The single-source docs model keeps both needs in one Markdown source by validating narrative/reference markers and deriving MCP slices during docs validation.
