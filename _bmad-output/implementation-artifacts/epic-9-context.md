# Epic 9 Context: Fresh-Row Producer and Row Identity

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 9 completes trustworthy fresh-row behavior for generated projection grids without reopening the completed projection-refresh or command-lifecycle foundations. A confirmed command that creates or materially changes a row must carry explicit framework-owned target identity through one terminal-outcome path, publish at most one scope-safe indicator, and update an already-rendered grid automatically. The epic closes only when that composition works for the supported command shapes and is proven in a running system, rather than inferred from isolated component tests or identity-poor projection signals.

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

Automatic row marking is allowed only through the FC-NIP contract. Each material command outcome must identify the target projection, view or lane, target `EntityKey`, material-change kind, prior and expected status when applicable, command `MessageId`, and capture time. Semantics must be explicit for standalone create, same-row update, cross-row change, status move, delete, idempotent, rejected, and no-op outcomes, including when no indicator should be produced.

Projection nudges, visible-row diffs, unrelated refreshes, EventStore `AggregateId`, and untyped result payloads cannot be used as universal row identity or as a broad-row-marking fallback. EventStore remains the lifecycle/status authority by `MessageId`; transport acceptance is not projection-confirmed success.

Callback, polling, reconnect, and other terminal observations must yield identical pending-command and fresh-row behavior. An early callback must survive accepted-pending registration ordering, and duplicate terminal observations must not republish. Effective add, materialization, filter or re-query dismissal, TTL expiry, explicit clear, and tenant/user scope transition must update subscribed grids immediately and exactly once. Previous-scope entries must be rejected before they can be read or rendered.

An active indicator is unique by `(ViewKey, EntityKey)`. The first eligible publication owns its original `MessageId`, timestamp, expiry, and provenance atomically; later messages for the same active row cannot overwrite it or extend its lifetime. A new indication may be accepted only after the active entry expires or is dismissed.

Completion requires composition coverage across generated command handling, pending registration, terminal resolution, indicator state, and generated-grid rendering. It also requires live browser evidence from a running FrontComposer system for create and update paths. If live verification is environment-blocked, the exact command and blocker must remain recorded; focused unit evidence alone cannot close the epic.

## Technical Decisions

`IPendingCommandOutcomeResolver` is the single owner of terminal pending-state application and eligible fresh-row publication. Generated lifecycle callbacks and infrastructure adapters emit observations to that boundary; they do not mutate terminal pending state directly. Bounded buffering and replay handle callbacks that arrive before durable pending registration while preserving cancellation, disposal, and `MessageId` matching.

Command target metadata is immutable, explicit, and independent of the UI row that launched a command. The established FrontComposer-owned pending-command metadata remains the base for row-context commands, but the successor target-identity contract must also cover commands with no existing row and commands whose target differs from the source row. This work stays within FrontComposer Shell, SourceTools, tests, and governance tooling; it does not authorize an EventStore contract change, dependency upgrade, package-boundary change, schema-fingerprint change, submodule edit, or deployment change.

Indicator state exposes scoped change notifications. Generated grids subscribe, marshal render invalidation through the Blazor dispatcher, and unsubscribe safely. State mutation, timers, clearing, scope changes, and disposal must be race-safe and bounded. Source-generator output changes require intentional generated-output snapshots and public table-surface regression coverage.

Story evidence must mechanically reconcile the candidate commit range with story IDs, changed paths, declared file ownership, and unrelated or interleaved work. Existing unrelated workspace changes remain separate; published history is not rewritten to manufacture story ownership.

## UX & Interaction Patterns

Fresh-row indicators appear and disappear automatically in a grid that was rendered before the state change. They are restricted to the active view/lane, tenant, and user scope and use localized, useful, non-noisy announcements with `role="status"` and `aria-live="polite"`. Indicator meaning must remain accessible under keyboard use, reduced motion, and forced colors. Rejection and no-op outcomes must not create misleading freshness, and the UI must continue to distinguish accepted-but-waiting lifecycle state from projection-confirmed results.

## Cross-Story Dependencies

Stories 9.1 and 9.2 are retained as historical base-decision and implementation records, not as accepted proof of composed behavior. Story 9.3 is the decision gate for Stories 9.4-9.6. Story 9.7 may proceed alongside the target-identity and composition work. Story 9.8 depends on Stories 9.3-9.7 and is the release regression gate for Epic 9 and the fresh-row requirements. The epic reuses the completed projection-refresh and command-lifecycle foundations but does not change their completion state.
