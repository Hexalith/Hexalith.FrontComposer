# Lifecycle Sequence Diagram (Reference)

Canonical click → Confirmed sequence with ILifecycleStateService observation. Tasks must align.

```
USER    FORM (2-1)    CommandService     LifecycleBridge.g.cs    ILifecycleStateService    Observer (FcLifecycleWrapper 2-4)
 |          |              |                    |                        |                                |
 |-- click->|              |                    |                        |                                |
 |          |-- Dispatch Submitted(CorrId, cmd) |                        |                                |
 |          |              |                    |<-- bridge.OnSubmitted(action) -- [Subscribed via IActionSubscriber]
 |          |              |                    |-- service.Transition(CorrId, Submitting, null) -->       |
 |          |              |                    |                        |-- push to observers [if any] -->|
 |          |              |                    |                        |                                |-- render spinner
 |          |              |                    |                        |                                |
 |          |-- await DispatchAsync(cmd, onLifecycleChange, ct) ------->  |                                |
 |          |              |-- HTTP POST (202 + ULID MessageId) -->       |                                |
 |          |              |<-- CommandResult(MessageId=01HXYZ..., Accepted)                               |
 |          |<-- result -- |                    |                        |                                |
 |          |-- Dispatch Acknowledged(CorrId, MsgId=01HXYZ..) ---------- bridge fwd ---->                  |
 |          |              |                    |                        |-- Transition(Acknowledged, MsgId)
 |          |              |                    |                        |-- dedup: seenMessageIds.add("01HXYZ")
 |          |              |                    |                        |-- push Ack transition to obs -> |
 |          |              |                    |                        |                                |
 |          |              |-- onLifecycleChange(Syncing, CorrId) (stub or SignalR) ---->                  |
 |          |-- Dispatch Syncing(CorrId) -- bridge fwd -- Transition(Syncing, MsgId) -- push --> observer  |
 |          |              |                    |                        |                                |
 |          |              |-- onLifecycleChange(Confirmed, CorrId) ---->                                  |
 |          |-- Dispatch Confirmed(CorrId) -- bridge fwd -- Transition(Confirmed, MsgId) [TERMINAL]        |
 |          |              |                    |                        |-- OutcomeNotifications++ (=1)   |
 |          |              |                    |                        |-- push Confirmed transition ->  |
 |          |              |                    |                        |                                |-- render success
 |          |              |                    |                        |-- retain entry until scope dispose (D9 / ADR-019 — no timer prune) |

IDEMPOTENT REPLAY PATH (Epic 5 reconnect, landing Story 5-4 but probed here):
 |          |              |-- [reconnect replays the same Confirmed] -- bridge fwd ->                    |
 |          |              |                    |                        |-- entry exists + same MsgId     |
 |          |              |                    |                        |-- IdempotencyResolved=true     |
 |          |              |                    |                        |-- DROP (no second outcome -- FR30)

DUPLICATE MessageId DIFFERENT CorrelationId (Decision D10 — detection-only, never synthesis):
 |          |              |-- [new submit, same MsgId, new CorrId] ---- bridge fwd ->                    |
 |          |              |                    |                        |-- seenMessageIds.Contains("01HXYZ") == true
 |          |              |                    |                        |-- HFC2005 log (duplicate MsgId across CorrelationIds)
 |          |              |                    |                        |-- fresh LifecycleEntry — normal Submitting→… progression (no terminal pre-fill from cache)
 |          |              |                    |                        |-- IdempotencyResolved only on same-CorrelationId terminal replay (D8), not here

ERROR PATH:
 |          |              |-- throw CommandRejectedException(reason, resolution) -----> |               |
 |          |-- Dispatch Rejected(CorrId, reason, resolution) -- bridge fwd -- Transition(Rejected, null) [TERMINAL]
 |          |              |                    |                        |-- OutcomeNotifications++ (=1)  |
 |          |              |                    |                        |-- push Rejected -- observer -->|
 |          |              |                    |                        |                                |-- render rejection

OUT-OF-ORDER (PROGRAMMER ERROR):
 |          |-- [bug: dispatch Confirmed without Submitted] ------------ bridge fwd -->                   |
 |          |              |                    |                        |-- entry does NOT exist         |
 |          |              |                    |                        |-- HFC2004 log + DROP (no outcome raised; FR30 still "≤1")

CANCELLATION (form disposed):
 |          | [Dispose]    |                    |                        |                                |
 |          |              |                    | bridge.Dispose() -- unsubscribe                          |
 |          |              |                    |                        |                                | observer.Dispose()
 |          |              |                    |                        |-- observer count decremented    |
```

Task 4 service implementation MUST match this sequence. Task 5 bridge emitter MUST match this sequence. Task 8 Counter sample verification MUST observe this sequence via bUnit.

---
