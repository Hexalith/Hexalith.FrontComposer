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

Commands are partial C# types marked with `[Command]` and usually `[BoundedContext("Name")]`. Include the framework-required `MessageId` property, but do not invent tenant, user, command identity, or correlation input fields; tenant and user identity come from host context, and FrontComposer supplies `MessageId` / `CorrelationId` as 26-character Crockford ULIDs through `IUlidFactory`.

Generated command forms render only non-derivable properties. `MessageId`, `CommandId`, `CorrelationId`, `TenantId`, `UserId`, timestamp fields, and properties marked `[DerivedFrom]` are infrastructure-owned and excluded from operator input. The non-derivable property count controls form density: 0-1 fields are inline, 2-4 fields are compact inline, and 5 or more fields get a full command page.

Command lifecycle is framework-owned after submission: `Submitting -> Acknowledged -> Syncing -> Confirmed / Rejected`, with idempotent-confirmed and NeedsReview outcomes surfaced by the shell. EventStore-backed hosts query command status by the accepted pending `MessageId`; do not add a second resolver path in agent-authored examples.

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
