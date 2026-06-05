# FC-MCP-LIFECYCLE v1 Lifecycle Subscription Contract

Date: 2026-06-05
Owner: FrontComposer MCP surface
Status: v1 implementation contract

## Scope

FC-MCP-LIFECYCLE v1 defines the fixed MCP lifecycle subscription tool used by agents after a command
tool acknowledgement. It lets an agent read a bounded snapshot for a framework-issued command
`correlationId` or `messageId`.

The lifecycle surface is a polling read contract. It does not add streaming transport, an MCP retry
loop, EventStore command-status HTTP polling, projection/resource behavior, schema-fingerprint
negotiation changes, direct state-store mutation, or a new authorization framework.

## Tool Identity

The lifecycle tool name is `FrontComposerMcpOptions.LifecycleToolName`; the default and v1 public
name is:

```text
frontcomposer.lifecycle.subscribe
```

`tools/list` includes exactly one fixed lifecycle tool after the generated visible command tools.
This fixed lifecycle tool is not a generated `McpCommandDescriptor` and must not be counted as a
generated command descriptor.

## Input Contract

The lifecycle tool accepts exactly one JSON object property:

```json
{ "correlationId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0C" }
```

or:

```json
{ "messageId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0B" }
```

The schema is advisory and must use:

- `type: object`
- `additionalProperties: false`
- `properties.correlationId.type: string`
- `properties.messageId.type: string`
- `properties.correlationId.maxLength: 64`
- `properties.messageId.maxLength: 64`
- `properties.correlationId.pattern: ^[0-9A-HJKMNP-TV-Z]{26}$`
- `properties.messageId.pattern: ^[0-9A-HJKMNP-TV-Z]{26}$`
- `oneOf` with exactly one required handle, either `correlationId` or `messageId`

Runtime validation remains authoritative. Handles must be uppercase canonical 26-character Crockford
ULIDs, ASCII-only, with no leading/trailing whitespace, lowercase folding, Unicode confusables,
percent/path/query/fragment material, nulls, numbers, booleans, objects, arrays, or oversized
values.

## Snapshot Shape

A successful read returns structured content with this v1 JSON shape:

```json
{
  "state": "Confirmed",
  "terminal": true,
  "messageId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0B",
  "correlationId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0C",
  "retry": {
    "retryAfterMs": 250,
    "maxLongPollMs": 1000
  },
  "transitions": [
    {
      "sequence": 1,
      "state": "Acknowledged",
      "observedAtUtc": "2026-06-05T00:00:00.0000000+00:00",
      "idempotencyResolved": false,
      "messageId": "01JZ0R5K9N8W4Y7V3Q2P6C1A0B"
    }
  ],
  "historyTruncated": false,
  "outcome": {
    "category": "confirmed",
    "retryAppropriate": false,
    "success": {
      "message": "Command completed: the requested change was applied.",
      "dataImpact": "The change has been applied."
    }
  }
}
```

The v1 wire shape intentionally nests retry guidance under `retry.retryAfterMs` and
`retry.maxLongPollMs`. Top-level `retryAfterMs` and `maxLongPollMs` fields are not part of this
contract.

`outcome` is present only after a terminal state is known. Terminal outcome categories are
`confirmed`, `rejected`, `timed_out`, `needs_review`, and `idempotent_confirmed`.

## Semantics

The command acknowledgement always reports `Acknowledged` and emits a lifecycle reference. Later
snapshots may report `Syncing`, `Confirmed`, `Rejected`, `timed_out`, `needs_review`, or
`idempotent_confirmed` through the `state` and `outcome.category` fields.

Lookup by `correlationId` and lookup by `messageId` must return the same lifecycle entry and current
state. Agent-visible transition history starts at `Acknowledged`, excludes `Idle` and `Submitting`,
is ordered by monotonic `sequence`, is bounded by `MaxLifecycleTransitionHistory`, and sets
`historyTruncated` when older visible history has been removed.

Terminal state is first-wins. Late non-terminal observations, late opposite terminal observations,
duplicate terminal observations, replayed observations, and parallel reads cannot change the first
terminal outcome or grow history incorrectly.

`retry.retryAfterMs` uses the dispatcher-provided retry hint when present, clamped to configured
`MinLifecycleRetryAfterMs` and `MaxLifecycleRetryAfterMs`; otherwise it uses the validated default.
`retry.maxLongPollMs`, timeout, active-entry capacity, retained-terminal capacity, and history length
come from validated `FrontComposerMcpOptions`.

## Security and Failure Semantics

`tools/call` must match `frontcomposer.lifecycle.subscribe` case-sensitively. The route resolves
`IFrontComposerMcpAgentContextAccessor.GetContext()` before handle lookup so unauthenticated or
tenantless calls cannot probe whether a lifecycle handle exists.

Malformed, unknown, tenant-hidden, policy-hidden, unauthenticated, and stale-after-visibility-loss
handles return the same opaque hidden/unknown structured content produced by
`FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent()`. These failures must not
leak internal state, submitted handles, command arguments, tenant IDs, user IDs, policy names,
exception messages, stack traces, raw rejection text, or raw payloads.

Before a known lifecycle snapshot is returned, the current visible catalog is revalidated through
`FrontComposerMcpToolAdmissionService.ResolveAsync(entry.Descriptor.ProtocolName, ...)`. A handle
that was valid when acknowledged becomes hidden/unknown if the command tool is no longer visible for
the current tenant or policy context.

## Non-Goals

FC-MCP-LIFECYCLE v1 does not implement streaming subscription transport, long-running server-side
subscribe loops, MCP-level retry, EventStore command-status endpoint polling, Shell pending-command
polling budget changes, projection/resource work, skill-corpus resources, schema-fingerprint
negotiation changes, generated command descriptor changes, or a new authorization framework.
