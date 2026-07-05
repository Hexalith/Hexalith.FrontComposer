# Epic 9 Context: Fresh-Row Producer and Row Identity

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Epic 9 resolves the accepted-deferred fresh-row indicator gap without reopening the completed projection refresh and command lifecycle epics. The goal is to make automatic `FcNewItemIndicator` producer wiring depend on a precise, framework-controlled row identity payload, so generated projection grids can mark newly created or materially changed rows without guessing from projection nudges.

## Stories

- Story 9.1: Confirm the FC-NIP row-identity producer contract
- Story 9.2: Wire `FcNewItemIndicator` producer and generated-grid consumer

## Requirements & Constraints

Fresh-row indicators must only be produced through FC-NIP when command outcome context carries exact row identity. The payload must include the generated view or lane key, exact row `EntityKey`, command `MessageId`, projection type, and status-slot metadata when needed to avoid ambiguity.

The product must not infer row-level freshness from projection nudges, visible-row diffs, or broad lane marking. `FcNewItemIndicator` is a confirmed component, but automatic row marking stays gated until the row identity source is approved and pinned.

## Technical Decisions

FC-NIP owns the automatic row-level producer contract. FC-TBL owns the DataGrid component/state primitive surface, and FC-CMD owns command identity, lifecycle, pending state, and message/correlation semantics.

EventStore status `AggregateId` is not automatically a FrontComposer generated-grid `EntityKey`; it can only be used if a specific projection contract proves identity equivalence. Generic EventStore `ResultPayload` must not become a hidden FC-NIP contract. If EventStore or another upstream source supplies row identity, it must publish a bounded typed payload.

The existing pending-command model already has optional `ProjectionTypeName`, `LaneKey`, `EntityKey`, `ExpectedStatusSlot`, and `PriorStatusSlot` slots. Story 9.2 may rely on them only after a source-level or contract-level artifact proves they are populated from framework-controlled metadata.

## UX & Interaction Patterns

Fresh-row indicators should surface useful live state without noisy announcements. They remain blocked until row identity is confirmed; broad row marking and diff-based inference are not acceptable UX fallbacks.

## Cross-Story Dependencies

Story 9.1 is the prerequisite for Story 9.2. Story 9.2 cannot implement producer or generated-grid consumer behavior until the FC-NIP row identity payload source is approved, documented, and mechanically pinned by tests.
