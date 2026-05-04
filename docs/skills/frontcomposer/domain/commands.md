---
id: domain-commands
title: Domain command records
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/domain/commands
order: 30
sourceDoc: docs/skills/frontcomposer/domain/commands.md
narrative: true
references: true
publicApiReferences: [Hexalith.FrontComposer.Contracts.Attributes.CommandAttribute, Hexalith.FrontComposer.Contracts.Attributes.BoundedContextAttribute, Hexalith.FrontComposer.Contracts.Attributes.DestructiveAttribute]
---
<!-- frontcomposer:section narrative -->
# Commands

Conceptual command modeling guidance belongs here for docs.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# Domain Commands

Commands are partial C# types marked with `[Command]` and usually `[BoundedContext("Name")]`. Include framework-required identity fields such as `MessageId`, but do not invent tenant or user input fields; tenant and user identity come from host context.

```csharp
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Billing;

[Command]
[BoundedContext("Billing")]
public partial class CreateInvoiceCommand {
    public string MessageId { get; set; } = "";
    public string InvoiceNumber { get; set; } = "";
}
```
<!-- /frontcomposer:section -->
