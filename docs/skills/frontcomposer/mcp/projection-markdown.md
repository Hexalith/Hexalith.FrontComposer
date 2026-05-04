---
id: mcp-projection-markdown
title: Projection Markdown reading
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/mcp/projection-markdown
order: 80
sourceDoc: docs/skills/frontcomposer/mcp/projection-markdown.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Mcp.Rendering.McpMarkdownProjectionRenderer, Hexalith.FrontComposer.Contracts.Mcp.McpResourceDescriptor]
---
<!-- frontcomposer:section narrative -->
# Projection Markdown

Human docs can show screenshots and rendered examples.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Projection Markdown Reading

Projection resources return bounded `text/markdown`. Respect truncation markers and `IsTruncated` metadata. Do not fork label, humanizer, status ordering, or Markdown table rules; consume renderer output as the framework contract.
<!-- /frontcomposer:section -->
