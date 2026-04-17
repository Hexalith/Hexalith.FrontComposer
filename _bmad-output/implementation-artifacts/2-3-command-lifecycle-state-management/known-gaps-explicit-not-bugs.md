# Known Gaps (Explicit, Not Bugs)

These are cross-story deferrals intentionally out of scope for Story 2-3. QA should NOT file these as defects.

| Gap | Owning Story | Reason |
|---|---|---|
| Visual lifecycle feedback (spinner, sync pulse 300ms+, "Still syncing..." text 2-10s, action prompt >10s, `aria-live` announcements) | Story 2-4 (FcLifecycleWrapper) | UX rendering is 2-4's scope; 2-3 ships the state source-of-truth the wrapper reads |
| Real `HubConnectionState` observation, reconnection/disconnection handling | Story 5-3 (SignalR connection) | `ConnectionState` returns `Connected` until 5-3 wires real state |
| Reconnection reconciliation (replayed projections, batched catch-up sweep) | Story 5-4 (reconnection reconciliation) | Grace-window idempotency (ADR-019) is the substrate 5-4 builds on; 2-3 does not trigger reconciliation itself |
| Domain-specific rejection message formatting (`[What failed]: [Why]. [What happened to the data]`) | Story 2-5 (rejection + confirmation) | 2-3 propagates Reason+Resolution strings; formatting lives in wrapper/messageBar |
| Destructive command confirmation dialog and form abandonment warning | Story 2-5 | Cross-cutting UX concern |
| Agent-surface two-call lifecycle tool (`lifecycle/subscribe`) | Epic 8 (Story 8-3) | `ILifecycleStateService` is the reusable substrate; agent-surface is a separate transport |
| `IdempotentConfirmed` as a distinct `CommandLifecycleState` enum value | Deferred (v1.x) | For v0.1, idempotent resolution is signalled via `CommandLifecycleTransition.IdempotencyResolved=true` while `NewState=Confirmed`. Promoting `IdempotentConfirmed` to its own enum value requires updating per-command Fluxor features (Story 2-1 reducers) — not worth the blast radius for v0.1 |
| Singleton-registration-detection Roslyn analyzer (guard against adopter mis-registration) | Epic 9 (Story 9.4) | Analyzer diagnostic tooling is 9.4; 2-3 documents the contract in XML doc + sample call site |
| LRU-on-`_entries` for chatty-circuit memory pressure | Epic 9 | ADR-019 revision 2026-04-16 chose scope-lifetime eviction. If real adopter pressure surfaces, add bounded-count LRU on `_entries` alongside the existing `_seenMessageIds` LRU. Not speculative v0.1 work. |
| `ILifecycleStateService` exposure of `IEnumerable<string> ActiveCorrelationIds` for admin/debug surface | Story 9.2 (CLI inspection) | Debug/inspection surface is Epic 9's domain |
| Lifecycle cross-surface mirroring (same lifecycle observed on web + chat agent simultaneously per PRD §523) | Epic 8 (Story 8-4) | 2-3 supports this by being CorrelationId-keyed; 8-4 bridges the MCP surface |
| `Subscribe()` replaying transitions that happened on a PRIOR circuit (closed tab + reopened → fresh circuit → empty state) | Story 5-4 (reconnection reconciliation) | Sally Story B — the `ILifecycleStateService` interface is designed so Epic 5 can back it with durable server-side lookup (event-store query by CorrelationId) WITHOUT changing the interface. v0.1 is circuit-local; v0.3+ silently gains cross-circuit replay. Story 2-4 MUST NOT code against "lifecycle is durable" assumptions — document in 2-4's Dev Notes. |
| MessageId cache exhaustion DoS (`_seenMessageIds` evictable via `Transition()` flood) | Epic 9 (Fluxor middleware rate-limit) | Red Team R3 — attacker bypassing the bridge with crafted `Transition()` calls can flood the LRU cache, evicting legitimate recent MessageIds. Deterministic duplicate detection breaks. Mitigation for v0.1: `LifecycleOptions.MessageIdCacheCapacity` is configurable (default 1024); adopters with high legitimate throughput raise it. Epic 9 adds Fluxor middleware that rate-limits `ICommandLifecycleAction` dispatch per second per scope. |
| Multi-assembly `[Command]` registration (plug-in model) — `AddHexalithDomain<T>` scans only the marker type's assembly | Story 7-2 (tenant context + plug-in isolation) | Pre-mortem PM1 — commands declared in dynamically-loaded NuGet packages won't get their bridge registered. In v0.1 adopters using plug-in patterns must explicitly register each command bridge type. Story 7-2's tenant-aware plug-in loader standardizes the pattern. |
| Bridge emitter analyzer to verify bridge subscribes to EXACTLY the 6 generated action types and nothing more | Epic 9 | Structural analyzer is 9.4 domain; for 2-3, snapshot test (Task 11.5) guards |
| Exported Public API surface review of `ILifecycleStateService` (SemVer discipline) | PublicApiAnalyzer (existing infra) | Shipping public surface auto-triggers existing analyzer; if it flags, update ShippedApi.txt |

---
