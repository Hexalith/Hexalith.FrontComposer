---
title: 'Deferred tenant-context fixes'
type: 'bugfix'
created: '2026-11-25'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent">

## Intent

Complete the next actionable deferred tenant-context items after DW-911 with one commit per item. Bound identity segment validation (DW-912), remove the unused raw EventStore identity overload (DW-914), strengthen diagnostic correlation handles (DW-915), trace command blocks (DW-916), throttle projection notification warnings (DW-917), and surface typed dispatch failures using the existing localized scope feedback (DW-919).

</frozen-after-approval>

## Implementation Notes

- Follow the existing ledger sequence for this round. DW-906 describes an earlier authorization denial affordance that was replaced by a focus-based denial card. DW-913 awaits an identifier normalization decision. DW-918 targets the superseded MCP tool enumeration surface.
- Use the existing EventStore tenant boundary of 64 UTF-16 code units and a separate conservative user boundary of 1024. The scoped projection warning interval is 30 seconds.

## Review Triage Log

- Medium. Start the dispatch span before tenant validation so the diagnostic shares its trace. A blocked attempt carries the outcome and failure category, and sends no HTTP request.
- Medium. Reset the submitted lifecycle on both the final scope recheck and a typed dispatch block. Extend the existing asynchronous scope-switch test and verify a second attempt after the typed block.
- Low. Document tenant and user limits and oversized whitespace behavior in the public contract and diagnostic help.
- Low. The DI-created accessor uses the system monotonic clock. The test injects time into the same limiter through direct construction. A configurable throttle clock would expand the public API.
- Low. Detail-only group recovery is a separate existing behavior. DW-917 applies to the nudge handler and leaves detail handling outside its throttle.
- False. A well-formed dispatcher throws TenantContextException before side effects. An accessor used after an accepted result is outside the new catch filter.
- Low. The existing command dispatch activity represents a client attempt. Its blocked outcome and missing HTTP status distinguish a local tenant validation failure.
- Low. Query and subscription exceptions preserve the origin correlation ID under their existing contract. Joining earlier and later tenant decisions is outside this batch.
- Medium. The default build selects 5.5.1 from the current Builds catalog and the standalone guard requires 5.4.0. The dependency graph already rejects the baseline Platform pin. The catalog repair requires a separate governed change.
