---
title: "Source generation and MCP split"
description: "Why FrontComposer keeps human documentation and agent reference slices in one source model."
genre: concept
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.concepts.source-generation-and-mcp-split
slug: concepts/source-generation-and-mcp-split/
---

# Source generation and MCP split

FrontComposer uses domain attributes as the source of truth, then generates UI, state, diagnostics, CLI evidence, and MCP-safe reference material from that model.

Human documentation explains sequence and tradeoffs. Agent documentation needs stable reference sections, sanitized examples, and reproducible fingerprints. The single-source docs model keeps both needs in one Markdown source by validating narrative/reference markers and deriving MCP slices during docs validation.
