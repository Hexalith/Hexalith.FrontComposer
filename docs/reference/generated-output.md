---
title: "Generated-output paths"
description: "Reference for generated FrontComposer output locations and troubleshooting links."
genre: reference
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-11-20
uid: frontcomposer.reference.generated-output
slug: reference/generated-output/
---

# Generated-output paths

<!-- hfc:reference:start -->
The flat layout uses the canonical output root and a generated filename.

```text
obj/{Configuration}/{TargetFramework}/generated/HexalithFrontComposer/{TypeName}.g.razor.cs
```

Roslyn normally inserts its analyzer assembly and generator type below this same root. For example, the Counter project emits files under
`obj/Release/net11.0/generated/HexalithFrontComposer/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.FrontComposerGenerator/`.
The CLI reports the file's actual project-relative path in either layout.

Do not edit generated files directly. Change the source domain type, rebuild, inspect with the CLI, then compare generated paths with IDE evidence.
<!-- hfc:reference:end -->

Generated output is linked from [getting started](../tutorials/getting-started.md), [debug generated output](../how-to/generated-output-debugging.md), [CLI inspect and migrate](cli.md), and [IDE parity](ide-parity.md).
