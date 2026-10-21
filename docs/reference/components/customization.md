---
title: "Customization"
description: "Public customization contracts, states, accessibility and customization ownership."
genre: reference
audience: adopter
ownerStory: 15-2-i-doc-a-reconcile-component-and-page-pattern-documentation
status: published
reviewed: 2026-10-21
uid: frontcomposer.reference.components.customization
slug: reference/components/customization/
---

# Customization

## Overview

Use Level 1 annotations, Level 2 typed projection templates, Level 3 field slots and Level 4 full projection-body replacements. Adopters implement typed contexts and register through supported attributes/extensions; the three hosts and diagnostic panel are framework infrastructure, not additional customization seams.

## Usage

For a Level 2 template, put the body in `OrderTemplate.razor`:

```razor
@namespace Acme.Orders
@Context.DefaultBody
```

Declare the typed context and marker in the user-authored companion `OrderTemplate.razor.cs`. The generator discovers the marker there; it does not inspect Razor-generated attribute declarations.

```csharp no-compile reason="The companion uses the adopter-owned annotated OrderProjection and generated OrderTemplate Razor partial class."
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;
using Microsoft.AspNetCore.Components;

namespace Acme.Orders;

[ProjectionTemplate(typeof(OrderProjection), ProjectionTemplateContractVersion.Current)]
public partial class OrderTemplate
{
    [Parameter, EditorRequired]
    public ProjectionTemplateContext<OrderProjection> Context { get; set; } = default!;
}
```

Register that assembly through the supported extension after adding FrontComposer services:

```csharp no-compile reason="Application registration uses the adopter-owned OrderTemplate Razor type."
services.AddHexalithProjectionTemplates<OrderTemplate>();
```

Import `Acme.Orders` and `Hexalith.FrontComposer.Shell.Extensions` in startup. The generated manifest discovers the typed attribute; callers do not mutate the registry. The [customization cookbook](../../how-to/customization-gradient-cookbook.md) gives complete annotation, template, slot and full-replacement examples.

In a typed replacement, `DefaultBody` renders generated output. `FieldRenderer`, `RowRenderer` and `SectionRenderer` reuse generated parts. A slot uses `Context.RenderDefault` for its generated field. Never mount the same generated projection component from its replacement: that re-enters Level 4 lookup recursively.

## Parameters / slots

The tables describe the public component parameters. Injected services and internal Fluent composition are implementation details.

### `ProjectionViewContext`

| Property | Type | Contract |
|---|---|---|
| `ProjectionType` | `Type` | Gets the projection type metadata. |
| `BoundedContext` | `string?` | Gets the bounded-context name declared for the projection, if any. |
| `Role` | `ProjectionRole?` | Gets the projection role; `null` means the default role. |
| `Items` | `IReadOnlyList<TProjection>` | Gets the current framework-owned render/query window. |
| `RenderContext` | `RenderContext?` | Gets the current render context. |
| `Columns` | `IReadOnlyList<ProjectionTemplateColumnDescriptor>` | Gets generated field descriptors. |
| `Sections` | `IReadOnlyList<ProjectionTemplateSectionDescriptor>` | Gets framework-owned section descriptors. |
| `LifecycleState` | `string` | Gets the lifecycle summary. |
| `EntityLabel` | `string` | Gets the localization-safe singular projection label. |
| `EntityPluralLabel` | `string` | Gets the localization-safe plural projection label. |
| `DefaultBody` | `RenderFragment` | Gets the default generated body renderer. |
| `SectionRenderer` | `ProjectionTemplateSectionRenderer` | Gets a generated section renderer. |
| `RowRenderer` | `ProjectionTemplateRowRenderer<TProjection>` | Gets a generated row renderer. |
| `FieldRenderer` | `ProjectionTemplateFieldRenderer<TProjection>` | Gets a generated field renderer. |
| `DensityLevel` | `DensityLevel` | Gets the effective density level for the current render. |
| `IsReadOnly` | `bool` | Gets a value indicating whether this render should be treated as read-only. |
| `IsDevMode` | `bool` | Gets a value indicating whether developer diagnostics affordances are active. |

### `ProjectionTemplateContext`

| Property | Type | Contract |
|---|---|---|
| `ProjectionType` | `Type` | Gets the projection type metadata. |
| `BoundedContext` | `string?` | Gets the optional bounded-context name attached to the projection. |
| `Role` | `Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole?` | Gets the projection role; `null` for the Default role. |
| `RenderContext` | `RenderContext?` | Gets the cascading render context. |
| `Items` | `IReadOnlyList<TProjection>` | Gets the read-only collection of projection rows currently in scope. |
| `Columns` | `IReadOnlyList<ProjectionTemplateColumnDescriptor>` | Gets the immutable column descriptors for generated fields available to the template. |
| `Sections` | `IReadOnlyList<ProjectionTemplateSectionDescriptor>` | Gets immutable descriptors for framework-owned render sections. |
| `DefaultBody` | `RenderFragment` | Gets a render delegate emitting the framework's default body. |
| `SectionRenderer` | `ProjectionTemplateSectionRenderer` | Gets a render delegate emitting a named generated section. |
| `RowRenderer` | `ProjectionTemplateRowRenderer<TProjection>` | Gets a render delegate emitting one generated row/item body. |
| `FieldRenderer` | `ProjectionTemplateFieldRenderer<TProjection>` | Gets a render delegate emitting a single generated field. |

### `FieldSlotContext`

| Property | Type | Contract |
|---|---|---|
| `Value` | `TField?` | Gets the selected field value for this render. |
| `Parent` | `TProjection` | Gets the parent projection item for this render. |
| `Field` | `FieldDescriptor` | Gets generated field metadata, including label, description, format, order, and hints. |
| `RenderContext` | `RenderContext` | Gets the current tenant/user/render-mode context. |
| `ProjectionRole` | `ProjectionRole?` | Gets the projection role, or `null` for the default role. |
| `DensityLevel` | `DensityLevel` | Gets the active shell density. |
| `IsReadOnly` | `bool` | Gets a value indicating whether the field should render read-only. |
| `IsDevMode` | `bool` | Gets a value indicating whether developer diagnostics are active. |
| `RenderDefault` | `RenderFragment<FieldSlotContext<TProjection, TField>>?` | Gets a generated-default renderer that bypasses the active Level 3 slot for this same projection/role/field, preventing recursive slot resolution. |

The following host parameters are listed for framework composition and diagnostics; adopters normally supply the typed contexts above through generated wiring.

### `FcProjectionViewOverrideHost`

| Parameter | Type | Contract |
|---|---|---|
| `Descriptor` | `ProjectionViewOverrideDescriptor` | Gets or sets the selected descriptor. |
| `Context` | `ProjectionViewContext<TProjection>` | Gets or sets the per-render Level 4 context. |

### `FcProjectionTemplateHost`

| Parameter | Type | Contract |
|---|---|---|
| `Descriptor` | `ProjectionTemplateDescriptor` | Framework-selected compatible template descriptor from the existing registry. |
| `Context` | `ProjectionTemplateContext<TProjection>` | Framework-created typed context for the current template render. |

### `FcFieldSlotHost`

| Parameter | Type | Contract |
|---|---|---|
| `Parent` | `TProjection` | Gets or sets the parent projection row. |
| `Value` | `TField?` | Gets or sets the selected field value. |
| `Field` | `FieldDescriptor` | Gets or sets generated field metadata. |
| `RenderContext` | `RenderContext` | Gets or sets the current render context. |
| `ProjectionRole` | `ProjectionRole?` | Gets or sets the projection role. |
| `RenderDefault` | `RenderFragment<FieldSlotContext<TProjection, TField>>?` | Gets or sets the default generated field renderer. This bypasses slot lookup. |
| `Key` | `object?` | Gets or sets the optional render-tree key used to anchor the slot host across virtualized row reuse. Generated emitters supply (rowKey, fieldName) so a re-rendered row does not leak slot component state to a neighbour row. |

### `FcCustomizationDiagnosticPanel`

| Parameter | Type | Contract |
|---|---|---|
| `Diagnostic` | `CustomizationDiagnostic` | Bounded framework diagnostic metadata; never supply payload or exception details. |
| `CanRetry` | `bool` | Whether the Development-only guidance offers boundary recovery. |
| `OnRetry` | `EventCallback` | Callback for retrying the affected read or boundary; the caller owns the work. |

## Layout (FC-LYT)

Resolution selects a compatible Level 4 replacement, then compatible Level 2 template, then the generated body. A Level 4 render fault resolves Level 2 directly using the current context; if it also faults, generated `DefaultBody` remains. A Level 2 fault uses generated `DefaultBody`. A Level 3 fault uses its generated field delegate. Lower-level fallbacks bypass the failing lookup. An independent internal boundary contains a generated delegate or child failure and displays localized unavailable copy without publishing the fallback exception or attempting another fallback. Primary-host sanitized logging and retry behavior are retained; no new registry is involved.

Registries skip incompatible major versions under the existing `LogAndSkip` default. `FailClosedOnMajorMismatch` rejects startup instead; minor drift remains selectable with rebuild guidance. Do not infer a policy change from render-fault recovery. Registry descriptors can be cached; contexts, data, culture and rendered fragments cannot.

Diagnostic panels, Retry controls and `IDiagnosticSink` events require `Hexalith.FrontComposer.Shell` itself to be compiled with `DEBUG`, plus an `IHostEnvironment` registered in DI whose environment is Development. The published Release NuGet package cannot expose them, even when the adopter application is built in Debug or runs in Development. Production, Staging and missing `IHostEnvironment` fail closed; registering only `IWebAssemblyHostEnvironment` in a WebAssembly host does not satisfy this gate. The shell-level contract-mismatch panel additionally requires `AddFrontComposerDevMode(...)` with a Development environment, which registers its mismatch diagnostic provider; keep that environment registered as `IHostEnvironment` in DI for the panel gate. Generated fallback remains usable in every combination. Retry retains the existing boundary recovery behavior; Level 4 also recovers when its descriptor or render context changes. Items-only churn does not create a new fault/log episode.

## Accessibility (FC-A11Y)

Development fault panels carry bounded What/Expected/Got/Fix/Fallback guidance and an assertive alert, with focus once per diagnostic identity. Their messages contain type/category metadata, never exception message, stack, payload or tenant/user values. Normal fallback content keeps its generated accessibility and announcement ownership. A direct panel follows the same environment/build gate. Keep caller content named and keyboard reachable; see [HFC1050](../../diagnostics/HFC1050.md) and [HFC1051](../../diagnostics/HFC1051.md).

## Localization (FC-L10N)

Generated labels retain the current per-render localized context. Diagnostic panel chrome uses shell resources. Custom templates and replacements own their copy, labels and keyboard semantics; diagnostics are developer guidance and must never become operator data or a support payload channel.

## Related

- [Page patterns](page-patterns.md)
- [Projection status](projection-status.md)
- [Command lifecycle](command-lifecycle.md)
- [Components](index.md)
