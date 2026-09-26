# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, execute commands, recover from connection failures, and understand lifecycle and fresh-row state without seeing another tenant's data or mistaking acceptance for success. The existing shell, projection, command, and fresh-row baseline needs tenant-safety proof and complete interaction and accessibility evidence. Separate Product decisions cover the completed live fresh-row proof and exact Fluent UI V5 identity.

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

- Queries, subscriptions, counts, preferences, pending commands, and fresh rows must use the resolved tenant and user scope. Missing or stale context blocks prior-scope data before rendering and shows an explicit unavailable state. Production EventStore seams must prove isolation.
- Projection loading, empty, offline, stale, recovery, slow-query, and result-limit states remain distinguishable. Recovery uses jittered retries capped at 30 seconds, restarts closed connections within 10 seconds, and polls every 15 seconds across at most eight lanes. A connection nudge is never command success.
- Command UI distinguishes submission, acknowledgement, syncing, terminal outcomes, Warning, and Degraded. Degraded starts at 10 seconds; status polling ends by 120 seconds without a success claim. There are no retries before acceptance; one post-acknowledgement retry reuses the command identity. Authorization runs around form transformation and at dispatch. Destructive actions require confirmation; one local command may be in flight.
- A fresh-row cue requires an immutable target identity captured before dispatch and an independent material terminal outcome. Unknown identity or materiality, no-op, delete, rejection, NeedsReview, and server-allocated keys produce no cue. Publication is atomic first-wins per view and entity; the cue lasts ten seconds, clears on scope change, and expires silently.
- Changed surfaces meet WCAG 2.2 AA, including 320 CSS-pixel reflow, 400% zoom, text spacing, target size, unobscured focus, forced colors, and reduced motion. Evidence needs axe, manual assistive-technology, and real-device checks. UI and evidence exclude secrets, tenant payloads, raw backend metadata, stack traces, and unrestricted personal data.
- Reuse focused tests and deterministic time for timing boundaries. Implementation evidence cannot close accessibility readiness, Product approval, or the Fluent decision; owner decisions cite immutable evidence and preserve completed history.

## Technical Decisions

- Scope is enforced before state is read or rendered. Subscription groups and persisted preferences are isolated by tenant and user; old groups and state are cleared when scope changes, and persistence is skipped when scope cannot be resolved.
- One resolver owns terminal pending-command application and fresh-row publication. Callbacks and adapters supply observations only. Target identity comes from declared generated command metadata, never a connection nudge, visible row difference, aggregate identifier, or untyped payload.
- Each bounded context appears as one Module with a default tab. Module tabs use `/{module}/{tab}`; `/{module}` aliases the default. Projection flyouts are secondary navigation. The active navigation item is chosen by longest segment-prefix match. Generated commands use `/commands/{BoundedContext}/{CommandTypeName}`.
- Use FrontComposer and Fluent UI Blazor V5 components with Fluent 2 tokens. Preserve typography and density while allowing reflow. Update the intentional API baseline for changed shell customization or Testing helpers. The exact V5 catalog revision awaits an owner decision.

## UX & Interaction Patterns

- Apply the canonical announcement, validation, focus, overlay, surface-state, and accessibility matrices; supplementary visual and journey guidance cannot override them. Status meaning uses text with an icon or shape, never color, hover, or motion alone.
- Each event has one speech path: a shared polite status, palette-owned combobox status, focused non-live summary, or focused non-live heading. Deduplicate by state identity; coalesce eligible updates over a trailing 250 ms per operation, connection epoch, load/filter, or palette session. Polls, retries, re-renders, and expiry stay silent.
- Successful navigation focuses the unique route heading outside sticky chrome. Failed navigation preserves focus. Palette, settings, and confirmation dialogs return focus to their captured invoker, falling back to the route heading if it disappears. The abandonment warning remains in the form flow, with Stay initially focused and Escape preserving input. Modal depth is one.
- Validation focuses a complete linked error summary after insertion and preserves correct values. A safely mapped server rejection uses that summary; an unmapped rejection remains in lifecycle context with one polite announcement and no invented field errors. Blocked concurrent submission leaves the attempted control focused and does not dispatch.
- The `/` shortcut focuses the sole enabled page-search input when eligible and otherwise does nothing. Fresh-row appearance announces once per tenant, user, and entity transition across views; suppression, dismissal, and expiry remain silent.

## Cross-Story Dependencies

- Tenant-scope proof underpins shell preferences, fail-closed projection and authorization states, and fresh-row clearing. Shell frame and information-architecture structure are inherited from Epic 12; this epic owns their interaction behavior.
- Stories 13.2–13.5 divide ownership of navigation, command, lifecycle, and fresh-row announcements. Consume canonical messages across story boundaries without redefining their copy or channel.
- Story 13.6 verifies all changed operator surfaces; Story 13.7 makes those assertions reusable for adopters. Its reviewer gate remains separate from owner decisions.
- Story 13.8 requires dated Product acceptance of the immutable Story 9.8 live proof. Story 13.9 requires UX-A through UX-F evidence for one exact Fluent catalog identity, then feeds the separate Product re-approval gate. Neither decision is implied by implementation completion.
