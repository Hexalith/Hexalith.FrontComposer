---
title: "Debug generated output"
description: "Use generated-output paths and IDE parity evidence to diagnose FrontComposer source-generation issues."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.how-to.generated-output-debugging
slug: how-to/generated-output-debugging/
---

# Debug generated output

Use this workflow when a generated Razor, Fluxor, or MCP artifact does not match the domain model.

1. Rebuild the project with the same configuration and target framework used by the host.
2. Inspect the project with the CLI reference: [CLI inspect and migrate](../reference/cli.md).
3. Compare the emitted path with [generated-output paths](../reference/generated-output.md).
4. Check [IDE parity](../reference/ide-parity.md) when completion, hover, or go-to-definition differs between tools.
5. Open the relevant [diagnostic reference](../reference/diagnostics/index.md) when the build emits an HFC code.

Generated files are evidence. The source of truth remains the annotated domain type and customization code.
