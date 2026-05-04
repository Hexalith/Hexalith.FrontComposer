---
id: domain-projections
title: Domain projection records
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/domain/projections
order: 40
sourceDoc: docs/skills/frontcomposer/domain/projections.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Contracts.Attributes.ProjectionAttribute, Hexalith.FrontComposer.Contracts.Attributes.ProjectionRoleAttribute, Hexalith.FrontComposer.Contracts.Attributes.ProjectionEmptyStateCtaAttribute]
---
<!-- frontcomposer:section narrative -->
# Projections

Human docs can describe role selection and display design.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Domain Projections

Projection types are partial C# types marked with `[Projection]`. Prefer supported projection role attributes instead of custom renderer forks. Use display annotations and field attributes from `Contracts` so SourceTools can emit the manifest and view code.

```csharp
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Billing;

[Projection]
[BoundedContext("Billing")]
[ProjectionRole(ProjectionRole.ActionQueue)]
public partial class InvoiceProjection {
    public string InvoiceNumber { get; set; } = "";
}
```
<!-- /frontcomposer:section -->
