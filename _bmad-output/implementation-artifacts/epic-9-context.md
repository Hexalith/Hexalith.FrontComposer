# Epic 9 Context: Fresh-Row Producer and Row Identity

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 9 completes trustworthy fresh-row behavior for generated projection grids. Commands that create or materially change a row carry explicit framework-owned target identity through one terminal-outcome path, publish a scope-safe indicator at most once for the active row, and update an already-rendered grid immediately. This matters because projection refresh signals and transport acceptance do not prove which row changed; the epic closes only when the complete generated-command-to-grid flow works for supported command shapes and is demonstrated in a running system.

## Stories

- Story 9.1: FC-NIP row-identity producer decision record
- Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer
- Story 9.3: Define explicit command target identity
- Story 9.4: Converge terminal outcomes on one producer boundary
- Story 9.5: Make indicator state observable and scope-safe
- Story 9.6: Enforce atomic per-row first-wins
- Story 9.7: Add story-ID and commit-scope evidence
- Story 9.8: Prove composed and live Epic 9 acceptance

## Requirements & Constraints

Automatic row marking is permitted only through FC-NIP. Each eligible outcome must have an immutable target snapshot containing the projection type, canonical view or lane, exact target `EntityKey`, material-change kind, applicable prior and expected status, and framework capture time; its accepted command `MessageId` is associated afterward. Target and indicator dispositions must be explicit for standalone create, same-row update, cross-row change, status move, delete, idempotent, rejected, no-op, and unresolved outcomes.

Projection nudges, visible-row diffs, unrelated refreshes, EventStore `AggregateId`, and untyped result payloads are not row-identity fallbacks. EventStore remains a lifecycle/status source keyed by `MessageId`, and transport acceptance is not projection-confirmed success. Unknown identity or unknown materiality suppresses publication.

Callback, polling, reconnect, and other terminal observations must produce identical pending-command and fresh-row behavior. Early callbacks must survive pending-registration ordering and replay exactly once; duplicate observations must not republish. Every effective add, materialization or filter dismissal, TTL expiry, explicit clear, and tenant/user scope transition must notify an already-rendered generated grid once. Scope validation must occur before state is read or rendered, and concurrent mutation, timer, scope, and disposal paths must remain bounded and race-safe.

An active indicator is unique by `(ViewKey, EntityKey)`. The first eligible publication atomically owns its `MessageId`, capture time, expiry, and provenance. Later observations or distinct commands targeting that active row cannot replace the entry or extend its lifetime; a new entry is eligible only after expiry or dismissal.

Completion requires composed coverage across generated command handling, pending registration, terminal resolution, indicator state, and generated-grid rendering, plus live browser evidence for create and update paths. A live-environment blocker must be recorded with the exact failed command; focused unit evidence cannot substitute for the release gate. Generated-output snapshots and affected public-surface tests must change intentionally. Story completion evidence must reconcile story IDs, candidate commits, changed paths, declared file ownership, and unrelated or interleaved work without absorbing pre-existing workspace changes.

## Technical Decisions

`IPendingCommandOutcomeResolver` is the single owner of terminal pending-state application and eligible fresh-row publication. Generated callbacks and infrastructure adapters emit terminal observations to this boundary; they do not mutate pending terminal state directly. Bounded callback buffering and replay preserve cancellation, disposal, and `MessageId` matching when an observation precedes durable pending registration.

Command target identity comes from an explicit command-to-projection declaration. Dynamic values resolve through typed `ICommandTargetIdentityProvider<TCommand>` implementations; only an explicitly declared `SameAsSource` mode may copy a pre-dispatch generated source snapshot. There is no ambient-row fallback. Exactly one target snapshot is validated before asynchronous dispatch, and terminal observation time cannot overwrite its capture time.

Terminal materiality is evaluated independently from target intent and is closed to `Material`, `NoOp`, or `Unknown`. No-op, unknown, delete, rejected, and needs-review outcomes do not publish. A material idempotent confirmation retains the eligible ten-second indicator lifetime.

Indicator state provides scoped change notifications. Generated grids subscribe, marshal rendering through the Blazor dispatcher, and unsubscribe safely. The state boundary enforces tenant/user scope before snapshots are exposed and atomic first-wins behavior across both duplicate and distinct message IDs. These changes preserve the existing Shell dependency direction: state owns state-service contracts and mutation coordination, while components consume snapshots and notifications.

## UX & Interaction Patterns

Fresh-row indicators appear and disappear automatically in a grid rendered before the state change; no unrelated projection or Fluxor render may be required. Indicators are restricted to the active lane, tenant, and user, and previous-scope data must clear before it can render. `FcNewItemIndicator` uses localized, useful, non-noisy live feedback with `role="status"` and `aria-live="polite"`. Behavior must remain understandable with keyboard navigation, visible focus, reduced motion, forced colors, and without color as the only signal. Rejected, no-op, or uncertain outcomes must not imply freshness, and accepted-but-waiting lifecycle state remains distinct from projection-confirmed results.

## Cross-Story Dependencies

Stories 9.1 and 9.2 remain historical decision and implementation records, not accepted proof of composed behavior. Story 9.3 supplies the approved target-identity decision required by Stories 9.4-9.6. Story 9.7 can proceed alongside the composition work. Story 9.8 depends on Stories 9.3-9.7 and is the release regression gate for the epic and its fresh-row requirements. Epic 9 reuses the completed projection-refresh and command-lifecycle foundations without reopening them or requiring an EventStore identity change.
