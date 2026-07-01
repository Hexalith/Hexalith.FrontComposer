# Sprint Change Proposal — FrontComposer.Shell: metadata-rich, scope-aware projection-changed client

- **Date:** 2026-06-20
- **Author:** Jerome (via Senior Developer review of ChatBot Story 10.6b)
- **Repo / submodule:** `Hexalith.FrontComposer`
- **Driving change:** ChatBot Epic 10, Story 10.6b — "Streaming AI response + Stop/Cancel" AC1 progress transport
- **Scope classification:** **Moderate** (additive Shell client + public-contract change; back-compatible)
- **Release order:** **2 of 3** — depends on the EventStore detail/scope contract (proposal 1) being published; must publish before the ChatBot change (proposal 3).
- **Status:** **APPROVED** by Administrator on 2026-07-01. Implementation evidence is present in the current FrontComposer source tree; release/package coordination remains the active handoff.
- **Companion proposals:**
  - `Hexalith.EventStore/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-20-ai-response-progress-transport.md`
  - `<chatbot>/_bmad-output/planning-artifacts/sprint-change-proposal-2026-06-20-ai-response-progress-transport.md`

---

## Section 1 — Issue Summary

The FrontComposer Shell is the **mandated client-side reuse target** for the ChatBot AI-response streaming ADR. It already ships a robust projection-changed subscription stack:

- `IProjectionHubConnectionFactory` / `SignalRProjectionHubConnection` (wraps `HubConnection`, `WithAutomaticReconnect`).
- `ProjectionSubscriptionService : IProjectionSubscription` — connection lifecycle, group tracking, **automatic rejoin** on reconnect (`RejoinActiveGroupsAsync`), group health (`Active`/`Degraded`/`Blocked`).
- Public notifier `IProjectionChangeNotifier` / `IProjectionChangeNotifierWithTenant` (in `Hexalith.FrontComposer.Contracts.Communication`).

But the entire stack is **signal-only**: the wire handler is `Func<string,string,Task>` for the `"ProjectionChanged"` event carrying `(projectionType, tenantId)`, and `JoinGroupAsync(projectionType, tenantId)` joins a **tenant-scoped** group only. ChatBot needs the richer `ProjectionChangedDetail` payload (ids/version/sequence/state) plus a **conversation-scoped** group so its UI can run the stale/out-of-order fail-closed gate and avoid tenant-wide re-query storms.

**Decision (from `correct-course`):** extend the Shell client **additively** to receive the new EventStore `ProjectionChangedDetail` message and to join **scoped** groups, surfacing both through the public notifier contract. Existing signal-only consumers (e.g. `BadgeCountService`) are untouched.

## Section 2 — Impact Analysis

- **Epic impact:** None to FrontComposer's roadmap; enabling change for ChatBot Epic 10.
- **Story impact:** New FrontComposer story (suggest `FC-xx: Scope-aware metadata-rich projection-changed subscription`). No existing story invalidated.
- **Artifact conflicts:**
  - Public contract `Hexalith.FrontComposer.Contracts.Communication` gains an additive detail notifier → **feature version bump**, not breaking.
  - Architecture (EventStore integration / projection subscription) section updated for the detail + scope path and rejoin-with-scope.
- **Technical impact (files):**
  - `Shell/Infrastructure/EventStore/IProjectionHubConnection.cs`
  - `Shell/Infrastructure/EventStore/SignalRProjectionHubConnectionFactory.cs`
  - `Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs` (group key + rejoin)
  - `Shell/Communication/ProjectionChangeNotifier.cs` + `Contracts.Communication` interfaces
  - `Shell/Extensions/EventStoreServiceExtensions.cs` (DI), `EventStoreOptions`
  - **Package bump:** consume the new `Hexalith.EventStore.Client` version from proposal 1.

## Section 3 — Recommended Approach

**Direct Adjustment — additive Shell client extension.** Keep `OnProjectionChanged` / `JoinGroupAsync(type, tenant)` and the existing notifier events exactly as-is; add a parallel detail receiver, an optional `scope` on join/rejoin, and a detail notifier event.

- **Effort:** ~M (2–4 dev-days incl. reconnect/rejoin-with-scope tests).
- **Risk:** Low–Medium. The riskiest area is **rejoin correctness with scoped groups** (the existing `RejoinActiveGroupsAsync` + group-health logic must carry scope without regressing the signal-only rejoin). Covered by tests.
- **Timeline impact:** Gated by EventStore publish; gates the ChatBot UI receiver.

## Section 4 — Detailed Change Proposals

### 4.1 `IProjectionHubConnection` — additive detail handler + scoped join

```
OLD:
  IDisposable OnProjectionChanged(Func<string, string, Task> handler);
  Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);
  Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);

NEW (additive — keep all the above):
  IDisposable OnProjectionChanged(Func<string, string, Task> handler);
  IDisposable OnProjectionChangedDetail(Func<ProjectionChangedDetail, Task> handler);   // new
  Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);
  Task JoinGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken);   // new
  Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);
  Task LeaveGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken);  // new
```

`ProjectionChangedDetail` is the type published by `Hexalith.EventStore.Client` (proposal 1) — reuse it; do not redefine.

### 4.2 `SignalRProjectionHubConnectionFactory` — wire the new message + invoke

```
OLD:
  public IDisposable OnProjectionChanged(Func<string,string,Task> handler)
      => _connection.On("ProjectionChanged", handler);
  public Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken ct)
      => _connection.InvokeAsync("JoinGroup", projectionType, tenantId, ct);

NEW (add):
  public IDisposable OnProjectionChangedDetail(Func<ProjectionChangedDetail,Task> handler)
      => _connection.On<string,string,string?,IReadOnlyDictionary<string,string>>(
             "ProjectionChangedDetail",
             (type, tenant, scope, meta) => handler(new ProjectionChangedDetail(type, tenant, scope, meta)));
  public Task JoinGroupAsync(string projectionType, string tenantId, string? scope, CancellationToken ct)
      => _connection.InvokeAsync("JoinGroup", projectionType, tenantId, scope, ct);
```

### 4.3 `ProjectionSubscriptionService` — carry scope through group state + rejoin

```
OLD:  GroupKey(string ProjectionType, string TenantId)
NEW:  GroupKey(string ProjectionType, string TenantId, string? Scope)
```

- `SubscribeAsync` / `UnsubscribeAsync` accept an optional `scope`.
- `RejoinActiveGroupsAsync` invokes `JoinGroupAsync(type, tenant, scope, ct)` per tracked key — **scope must survive reconnect** (this is the ADR's "rejoin only server-authorized project/conversation groups" requirement).
- Group health (`Active`/`Degraded`/`Blocked`) and context-freshness checks apply per scoped key.

### 4.4 Public notifier — additive detail event

In `Hexalith.FrontComposer.Contracts.Communication`, add an additive surface (new interface preferred to keep existing ones stable):

```csharp
public interface IProjectionChangeDetailNotifier
{
    // Raised when a scoped, metadata-rich projection-changed message is received.
    event Func<ProjectionChangedDetail, Task>? ProjectionChangedDetail;
}
```

`ProjectionChangeNotifier` implements it alongside the existing `IProjectionChangeNotifier` / `IProjectionChangeNotifierWithTenant`; `ProjectionSubscriptionService.OnProjectionChangedDetailAsync` raises it. Existing `Action<string>` / `Action<string,string>` events unchanged.

### 4.5 DI + options

- `EventStoreServiceExtensions`: `TryAddScoped` the detail notifier alias (same instance as `ProjectionChangeNotifier`).
- `EventStoreOptions`: reuse existing `ProjectionChangesHubPath`; no new endpoint. Optionally expose a `MaxScopedGroupsPerConnection` guard consistent with the host's per-connection cap.

### 4.6 Tests (new)

- Detail message → `IProjectionChangeDetailNotifier.ProjectionChangedDetail` raised with full payload; signal-only `ProjectionChanged` still raised for legacy path.
- `JoinGroupAsync(type, tenant, scope)` invokes `"JoinGroup"` with scope; reconnect **rejoins the scoped group** (extend existing rejoin tests).
- Mixed subscriptions (one scoped, one tenant-wide) rejoin independently; group-health transitions preserved.
- `BadgeCountService` (existing consumer) regression: unchanged behavior on the signal-only path.

## Section 5 — Implementation Handoff

- **Scope:** Moderate. Route to **FrontComposer DEV** (Architect sign-off on the additive `Contracts.Communication` surface + version bump).
- **Deliverables:** additive detail receiver, scoped join/rejoin, detail notifier, tests, **published Shell package version** consuming the new EventStore client.
- **Success criteria:**
  1. Existing signal-only consumers build and pass unchanged (esp. `BadgeCountService` and reconnect/rejoin suites).
  2. Scoped-group rejoin proven by reconnect tests.
  3. Detail payload surfaced verbatim (opaque) to consumers; Shell adds no AI-domain knowledge.
  4. New version published and pinned for ChatBot (proposal 3).
- **Do NOT:** change the signal-only handler signature or existing notifier events; interpret/transform the metadata map (pass it through opaque); drop scope on reconnect.

---

## Approval and Handoff Log

- **2026-07-01 — Approved by Administrator.** User directive: `$bmad-correct-course approve change proposals`.
- **Scope classification:** Moderate.
- **Routed to:** FrontComposer Developer for direct implementation/release coordination, with Architect sign-off on the additive public communication surface.
- **Implementation evidence observed in this approval pass:** `ProjectionChangedDetail`, `IProjectionChangeDetailNotifier`, `OnProjectionChangedDetail`, scoped `JoinGroupAsync`/`LeaveGroupAsync`, scoped `GroupKey`, DI registration, and focused projection subscription tests are present in `src/` and `tests/`.
- **Accepted implementation deviation:** the approved implementation uses a FrontComposer-local, wire-compatible `ProjectionChangedDetail` DTO instead of referencing the EventStore client DTO, because `Hexalith.FrontComposer.Contracts` multi-targets `netstandard2.0` and cannot reference the net10-only EventStore client. Scoped joins/leaves use explicit `JoinGroupScoped` / `LeaveGroupScoped` wire methods for non-empty scope.
- **Remaining handoff:** publish/pin the FrontComposer package version required by the downstream ChatBot proposal. No root sprint-status update is required unless a release-tracking story is added.
