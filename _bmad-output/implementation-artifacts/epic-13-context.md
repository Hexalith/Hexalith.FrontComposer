# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success. Complete tenant-isolation, interaction, and accessibility proof while preserving delivered surfaces. Product acceptance of the fresh-row live proof and Fluent UI V5 posture remain separate decisions.

## Stories

- Story 13.1: Prove Tenant-Safe Operator State End to End
- Story 13.2: Make Shell Navigation and Route Focus Deterministic
- Story 13.3: Preserve Focus and Input Through Command Safety Outcomes
- Story 13.4: Announce Projection and Command State Without Noise
- Story 13.5: Preserve Fresh-Row Meaning Across Visual and Data Changes
- Story 13.6: Verify Responsive and Assistive Accessibility
- Story 13.7: Provide Reusable UX Assertions for Adopters
- Story 13.8: Accept the Completed Fresh-Row Live Proof
- Story 13.9: Decide the Exact Fluent UI V5 Posture

## Requirements & Constraints

- Queries, subscriptions, counts, preferences, pending commands, and fresh rows share resolved tenant/user scope. Unavailable or stale context blocks prior-scope state before rendering with explicit feedback. Scope changes clear state/groups; unresolved scope skips persistence. Prove isolation through production EventStore seams.
- Transport acceptance never implies confirmation. Preserve distinct lifecycle identities, including IdempotentConfirmed, NeedsReview, Warning, and Degraded. Degraded begins at 10 seconds; silent status polling runs every second and stops at 120 seconds. Exhaustion closes the local lifecycle without success or later automatic mutation. Allow zero pre-accept retries and one transient retry after 250 ms reusing MessageId. Authorization surrounds transformation/dispatch; destructive actions require confirmation; a second local command is blocked without queueing.
- Projection loading, empty, filter-no-results, query failure, offline, stale, reconnecting, fallback, slow-query, and result-limit states remain distinguishable. SlowQuery starts at two seconds. Recovery retries indefinitely with jittered backoff capped at 30 seconds, restarts closed connections within ten seconds, polls every 15 seconds across at most eight lanes, and shows a three-second recovery notice.
- Fresh-row publication requires an immutable pre-dispatch target and independent terminal Material evidence. Unknown identity/materiality, NoOp, delete, Rejected, NeedsReview, and server-allocated keys suppress the cue. Atomic first-wins publication per view/entity preserves provenance and the ten-second window; scope removal, duplicate suppression, dismissal, and expiry are silent.
- Every changed operator surface meets WCAG 2.2 AA: 320 CSS-pixel reflow, 400% zoom, text spacing, target size, fully visible focus, contrast, forced colors, reduced motion, and keyboard semantics. Reuse focused evidence and deterministic time; automation supplements required manual assistive-technology and real-device proof. UI and evidence exclude credentials, tenant payloads, raw backend metadata, stack traces, and unrestricted personal data.

## Technical Decisions

- Preserve Shell dependency direction and scoped Fluxor single-writer state. Routing stays pure; State owns slices, contracts, and mutation coordinators without render dependencies. Components consume these layers; concrete polling workers belong in Infrastructure, scheduler contracts in State.
- One pending-outcome resolver applies terminal state and publishes eligible fresh rows. Callbacks/adapters supply observations only. Explicit generated metadata uses typed resolution or declared SameAsSource capture; nudges, row differences, AggregateId, and untyped payloads never substitute for identity. Consumers observe mutations and dispose scoped subscriptions.
- Each bounded context is one Module with a default tab. Routes use `/{module}/{tab}`; `/{module}` selects the default. Projection flyouts remain secondary. Active entries use longest segment-prefix matching; generated commands use `/commands/{BoundedContext}/{CommandTypeName}`.
- Interactive UI uses FrontComposer/Fluent UI Blazor V5 and Fluent 2 tokens. Preserve typography/density contracts while allowing reflow; inherited Fluent styling owns semantic treatments. Rendering contracts belong in Contracts.UI, UI-neutral contracts in Contracts, fakes in Testing. Update intentional public API baselines; reconcile exact catalog and token/API identity.

## UX & Interaction Patterns

- The canonical announcement, validation, focus, overlay, surface-state, and accessibility matrices govern behavior; visual/journey supplements cannot override them. Use canonical localized microcopy and text plus icon/shape for status meaning.
- Each event has one speech path: shared polite status, palette-owned polite combobox status, focused non-live summary, or focused non-live heading. Deduplicate by state/result identity; group intermediate changes by lifecycle operation, connection epoch, operator load/filter, or palette session using a trailing 250 ms window. Discard stale results. Terminal outcomes, focused summaries, and navigation failures cancel pending intermediate speech and act immediately. Retry/poll ticks, repeated renders, unchanged background refresh, and expiry stay silent.
- Successful navigation focuses the unique route h1 outside sticky chrome; failures preserve usable focus. Tab selection keeps tab focus. Modal dialogs capture an invoker before activation and restore it, falling back to the route heading; modal depth is one. The abandonment guard stays in the form flow with Stay initially focused and Escape preserving input. Validation focuses a complete linked summary after insertion and preserves values; safely mapped rejection uses that summary, while unmapped rejection stays in lifecycle context. The `/` shortcut focuses the sole eligible page-search input and otherwise does nothing.

## Cross-Story Dependencies

- Tenant-scope proof underpins preferences, fail-closed visibility, and fresh-row clearing. Navigation, command, lifecycle, and fresh-row stories share canonical speech channels/copy. Accessibility verification and adopter helpers cover their changed surfaces.
- Implementation completion does not close OI-16, G-4, Fluent approval, or Product re-approval. Closure needs complete evidence and named owner decisions; accessibility review stays separate.
- Fresh-row acceptance cites immutable Story 9.8 evidence without rewriting completed Epic 9 history or expanding accepted non-goals. Fluent approval consumes UX-A through UX-F evidence for one exact catalog identity and defines breaking-change, GA re-decision, and revalidation triggers before separate Product re-approval.
