---
id: samples-new-bounded-context
title: New bounded context sample
version: 1.0.0
audience: agent
docfx: true
mcpResource: true
resourceUri: frontcomposer://skills/samples/new-bounded-context
order: 90
sourceDoc: docs/skills/frontcomposer/samples/new-bounded-context.md
narrative: true
references: true
owningStory: Story 8-5
publicApiReferences: [Hexalith.FrontComposer.Contracts.Registration.FrontComposerRegistryExtensions, Hexalith.FrontComposer.Contracts.Mcp.McpManifest]
samplePaths: [samples/Counter/Counter.Domain, samples/Counter/Counter.Web]
---
<!-- frontcomposer:section narrative -->
# New Bounded Context Sample

Human docs can compare this to the Counter sample under `samples/Counter/`.
<!-- /frontcomposer:section -->
<!-- frontcomposer:section agent-reference -->
# New Bounded Context Sample

Use this skeleton when generating a new bounded context. Mirror the layout of
`samples/Counter/Counter.Domain` and `samples/Counter/Counter.Web`; the framework's
SourceTools generator emits the matching `.g.cs` partial under `obj/` at build time.
Never edit the generated files; never reference packages outside the approved list.

## Project layout

```text
Billing/
  Billing.Domain/
    Billing.Domain.csproj
    CreateInvoiceCommand.cs
    InvoiceCreatedEvent.cs
    InvoiceProjection.cs
    CreateInvoiceCommandValidator.cs
    BillingRegistration.cs
  Billing.Domain.Tests/
    CreateInvoiceCommandTests.cs
```

## Command record

```csharp
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Billing;

[Command]
[BoundedContext("Billing")]
public partial class CreateInvoiceCommand
{
    public string MessageId { get; init; } = string.Empty;
    public string InvoiceNumber { get; init; } = string.Empty;
    public decimal Amount { get; init; }
}
```

## Projection record

```csharp
using Hexalith.FrontComposer.Contracts.Attributes;

namespace Billing;

[Projection]
[BoundedContext("Billing")]
public partial class InvoiceProjection
{
    public string InvoiceNumber { get; init; } = string.Empty;
    public string Status { get; init; } = "Draft";
}
```

## Validator

```csharp
namespace Billing;

public sealed class CreateInvoiceCommandValidator
{
    public bool IsValid(CreateInvoiceCommand command)
        => !string.IsNullOrEmpty(command.InvoiceNumber) && command.Amount > 0;
}
```

## Registration

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Billing;

public static class BillingRegistration
{
    public static IServiceCollection AddBillingFrontComposer(this IServiceCollection services)
        => services
            .AddScoped<CreateInvoiceCommandValidator>();
}
```

## Rules

- Do not edit `.g.cs` files under `obj/`. The SourceTools generator owns them.
- Do not add `TenantId`, `UserId`, `Token`, or claim fields to command records — those are
  supplied by the framework from the agent context.
- Do not call EventStore directly from command code. Use the framework's command dispatcher.
- Add tests under a sibling `*.Tests` project; keep validators and tests in the domain project.
- Cite the Counter sample (`samples/Counter/Counter.Domain`, `samples/Counter/Counter.Web`)
  for any layout question this skill does not cover.
<!-- /frontcomposer:section -->
