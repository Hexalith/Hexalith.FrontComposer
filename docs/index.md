---
title: "Hexalith FrontComposer documentation"
description: "Documentation entry point for learning, building, checking, and understanding FrontComposer."
genre: tutorial
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.docs.index
slug: /
---

# Hexalith FrontComposer documentation

Use the documentation by task:

- [Tutorials](tutorials/index.md) guide first-time project setup.
- [How-to](how-to/index.md) covers concrete customization and migration work.
- [Reference](reference/index.md) publishes API, diagnostics, CLI, IDE, generated-output, and MCP contracts.
- [Concepts](concepts/index.md) explain the source-generation model and human/MCP documentation split.

Build locally with:

```powershell no-compile reason="Local developer command documented for execution outside snippet harness."
dotnet tool restore
pwsh ./eng/validate-docs.ps1
dotnet docfx docs/docfx.json
dotnet docfx serve docs/_site
```
