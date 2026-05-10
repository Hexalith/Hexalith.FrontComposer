---
title: "Customize a relative-time field at four levels"
description: "Day-1 cookbook for solving one DateTimeOffset relative-time rendering problem across the FrontComposer customization gradient."
genre: how-to
audience: adopter
ownerStory: 9-5-diataxis-documentation-site
status: published
reviewed: 2026-05-10
uid: frontcomposer.how-to.customization-gradient-cookbook
slug: how-to/customization-gradient-cookbook/
---

# Customize a relative-time field at four levels

Use the lowest level that solves the field problem. The shared example is an `UpdatedAt` `DateTimeOffset` field that should render as relative time.

| Level | Use when | Preserves |
| --- | --- | --- |
| 1. Annotation | The generated renderer already has the behavior you need. | Lifecycle wrapper, accessibility contract, generated metadata, hot reload, diagnostics, and generated tests. |
| 2. Typed template | You need to rearrange sections or rows while keeping field rendering framework-owned. | Lifecycle wrapper, field diagnostics, generated metadata, and default field rendering. |
| 3. Typed slot | One field needs custom rendering. | Generated view shell, lifecycle wrapper, field identity, registry diagnostics, and default fallback rendering. |
| 4. Full replacement | The projection body needs complete replacement. | Shell, lifecycle wrapper boundary, authorization, telemetry context, and override diagnostics. |

## Level 1: annotation

```csharp compile
using System;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Acme.Orders;

[Projection]
public sealed class OrderActivityProjection
{
    public string OrderId { get; init; } = "";

    [RelativeTime(relativeWindowDays: 14)]
    public DateTimeOffset UpdatedAt { get; init; }
}
```

Choose this first. It keeps the generated metadata authoritative and is the friendliest path for hot reload, diagnostics, MCP schemas, and adopter tests.

## Level 2: typed Razor template

```csharp compile
#if NET10_0_OR_GREATER
using System;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Microsoft.AspNetCore.Components;

namespace Acme.Orders;

[Projection]
public sealed class OrderActivityProjection
{
    public string OrderId { get; init; } = "";

    [RelativeTime]
    public DateTimeOffset UpdatedAt { get; init; }
}

[ProjectionTemplate(typeof(OrderActivityProjection), ProjectionTemplateContractVersion.Current)]
public sealed class OrderActivityTemplate : ComponentBase
{
    [Parameter]
    public ProjectionTemplateContext<OrderActivityProjection> Context { get; set; } = default!;
}
#endif
```

Use Level 2 when the problem is layout, not field rendering. Render `Context.DefaultBody`, `Context.RowRenderer(row)`, or `Context.FieldRenderer(row, nameof(OrderActivityProjection.UpdatedAt))` from the `.razor` companion so FrontComposer still owns the field behavior.

## Level 3: typed slot

```csharp compile
#if NET10_0_OR_GREATER
using System;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Microsoft.AspNetCore.Components;

namespace Acme.Orders;

[Projection]
public sealed class OrderActivityProjection
{
    public string OrderId { get; init; } = "";

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class UpdatedAtRelativeTimeSlot : ComponentBase
{
    [Parameter]
    public FieldSlotContext<OrderActivityProjection, DateTimeOffset> Context { get; set; } = default!;
}

public static class OrderActivitySlots
{
    public static ProjectionSlotDescriptor UpdatedAtDescriptor { get; } = new(
        typeof(OrderActivityProjection),
        ProjectionSlotSelector.Parse<OrderActivityProjection, DateTimeOffset>(x => x.UpdatedAt).Name,
        typeof(DateTimeOffset),
        Role: null,
        typeof(UpdatedAtRelativeTimeSlot),
        ProjectionSlotContractVersion.Current);
}
#endif
```

Use Level 3 when one field needs custom rendering. The slot context preserves field metadata, parent projection, render context, density, read-only state, dev-mode state, and the generated fallback renderer.

## Level 4: full projection-body replacement

```csharp compile
#if NET10_0_OR_GREATER
using System;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Microsoft.AspNetCore.Components;

namespace Acme.Orders;

[Projection]
public sealed class OrderActivityProjection
{
    public string OrderId { get; init; } = "";

    public DateTimeOffset UpdatedAt { get; init; }
}

public sealed class OrderActivityViewReplacement : ComponentBase
{
    [Parameter]
    public ProjectionViewContext<OrderActivityProjection> Context { get; set; } = default!;
}

public static class OrderActivityViewOverrides
{
    public static ProjectionViewOverrideDescriptor Descriptor { get; } = new(
        typeof(OrderActivityProjection),
        Role: null,
        typeof(OrderActivityViewReplacement),
        ProjectionViewOverrideContractVersion.Current,
        "Acme.Orders.OrderActivityViewReplacement");
}
#endif
```

Use Level 4 only when the generated projection body is the wrong shape. The shell still owns lifecycle, loading and empty states, authorization boundaries, telemetry, diagnostics, density, and disposal hooks.
