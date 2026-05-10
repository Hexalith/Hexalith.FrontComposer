---
title: "Build your first FrontComposer projection"
description: "Create a small projection and inspect generated FrontComposer output."
genre: tutorial
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.tutorials.getting-started
slug: tutorials/getting-started/
---

# Build your first FrontComposer projection

This tutorial starts from a blank application and ends with a projection that FrontComposer can render and inspect.

1. Add the FrontComposer packages used by your host and domain projects.
2. Define a small projection in the domain project.
3. Build the solution so the source generator emits Razor, Fluxor, and MCP metadata.
4. Inspect generated output with the CLI.

```csharp compile
using System;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Acme.Orders;

[Projection]
public sealed class OrderSummaryProjection
{
    public string OrderId { get; init; } = "";

    public DateTimeOffset UpdatedAt { get; init; }
}
```

After the first build, keep generated files read-only. Edit the domain type, rebuild, and use the generated-output reference when an IDE or CLI report points to an `obj/.../generated/HexalithFrontComposer` path.
