# FC-MCP-TOOLS v1 Command Tool Invocation Contract

Date: 2026-06-05
Owner: FrontComposer MCP surface
Status: v1 implementation contract

## Scope

FC-MCP-TOOLS v1 defines how generated FrontComposer command descriptors are exposed as MCP tools and
how side-effecting command tool calls are admitted, validated, constructed, dispatched, and
acknowledged.

The MCP command surface is descriptor-driven. Each visible `McpCommandDescriptor` becomes one MCP
tool during `tools/list`; the fixed `frontcomposer.lifecycle.subscribe` tool remains part of the MCP
catalog but is not counted as a generated command tool.

## Tool Catalog

`tools/list` builds its generated command catalog dynamically from
`FrontComposerMcpDescriptorRegistry.Commands` on every list operation. Tenant visibility and command
policy visibility are evaluated before a command tool is exposed.

Every generated command tool preserves the descriptor protocol name, title, bounded context,
authorization policy metadata, fingerprint, and per-command non-derivable parameter schema.
Descriptor names are stable and unique. Duplicate command base names must use namespace
disambiguation instead of overwriting descriptors.

Tool input schemas are advisory client contracts and must use `additionalProperties=false`. Runtime
validation still rejects unknown, unsupported, server-controlled, duplicate case-variant, malformed,
object, array, and oversized arguments before command construction or dispatch.

## Invocation Order

Side-effecting `tools/call` follows this exact order:

1. Visible-tool admission through the current visible catalog.
2. Schema negotiation.
3. Primitive argument validation.
4. Command construction.
5. Non-derivable argument application.
6. Server-side derivable injection.
7. DataAnnotations and current-contract validation.
8. `ICommandService.DispatchAsync<TCommand>`.
9. Lifecycle acknowledgement.

Any failure before dispatch must short-circuit the remaining steps. Stale or incompatible schema
categories stop before command construction, derivable injection, lifecycle tracking, and dispatch.
Hidden, unauthorized, tenant-hidden, and unknown tools return opaque hidden-equivalent failures.

## Server-Controlled Identity

Tool input must never accept caller-provided `TenantId`, `UserId`, `MessageId`, `CommandId`, or
`CorrelationId`. These fields are blocked before any server-side identity allocation or command
dispatch.

`TenantId` and `UserId` are injected from the MCP server context. `MessageId` and `CorrelationId` are
created through `IUlidFactory.NewUlid()` as canonical 26-character Crockford ULIDs. The command
`MessageId` and command `CorrelationId` use separate factory allocations unless a future explicit
contract changes that requirement.

GUIDs, activity trace IDs, raw transport correlation values, and client-provided identity handles are
out of contract for command `MessageId` and `CorrelationId`. Missing identity generation support is a
fail-closed schema/support failure before dispatch.

## Acknowledgement

A successful dispatch returns an `McpCommandAcknowledgement` containing the accepted lifecycle
handles issued by the framework. The acknowledgement message and correlation identifiers must match
the values injected into the dispatched command.

Lifecycle acknowledgement happens only after `ICommandService.DispatchAsync<TCommand>` accepts the
command. It must not run for hidden tools, schema negotiation failures, primitive validation
failures, unsupported parameter shapes, server-controlled input, construction failures, validation
failures, or dispatch failures.

## Security and Failure Semantics

`[RequiresPolicy]` visibility and admission checks stay in
`FrontComposerMcpToolAdmissionService`. The host command service remains protected by the existing
`AuthorizingCommandServiceDecorator` backstop when the Shell/EventStore direct-dispatch path is used.

Opaque failures must not leak tool names, tenant IDs, user IDs, policy names, exception messages, raw
payloads, or server-controlled identity values for authorization, tenant, policy-hidden,
unknown-tool, or hidden-equivalent paths.

## Non-Goals

FC-MCP-TOOLS v1 does not introduce an MCP retry loop, queue policy, batch policy, custom
authorization framework, bypass of `[RequiresPolicy]`, projection resource work, skill-corpus
resource work, lifecycle subscription changes, resource visibility changes, or Stories 5.2 through
5.5 behavior.
