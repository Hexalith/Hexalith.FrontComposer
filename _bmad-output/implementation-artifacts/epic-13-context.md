# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators must be able to browse projections, run generated commands, recover from transport failures, and read lifecycle and fresh-row state without seeing another tenant's data and without being told a command succeeded when it has not. This is a brownfield completion epic. The projection, lifecycle, FC-NIP fresh-row, and shell runtime baseline is already delivered and must be preserved. Epic 13 adds the missing parts: end-to-end tenant-scope proof (TEN-SCOPE-1), deterministic focus, announcement, validation, state, and accessibility behavior with its evidence (UX-A through UX-F), and two owner decisions: Product acceptance of the Epic 9 live proof, and the exact Fluent UI V5 posture. The Fluent decision depends on the accessibility evidence, so it comes after it.

## Stories

- Story 13.1: [I · TEN-SCOPE-1] Prove Tenant-Safe Operator State End to End
- Story 13.2: [I · UX-A] Make Shell Navigation and Route Focus Deterministic
- Story 13.3: [I · UX-B] Preserve Focus and Input Through Command Safety Outcomes
- Story 13.4: [I · UX-C] Announce Projection and Command State Without Noise
- Story 13.5: [I · UX-D] Preserve Fresh-Row Meaning Across Visual and Data Changes
- Story 13.6: [I · UX-E] Verify Responsive and Assistive Accessibility
- Story 13.7: [I · UX-F] Provide Reusable UX Assertions for Adopters
- Story 13.8: [A · E9-APP-1] Accept the Completed Fresh-Row Live Proof
- Story 13.9: [A · FLUENT-APP-1] Decide the Exact Fluent UI V5 Posture

## Requirements & Constraints

- **Tenant scope (FR30):**
  - Every query, SignalR subscription, count, pending state, persisted preference, and fresh-row operation is scoped to the resolved tenant and user.
  - Missing, invalid, mismatched, or stale tenant identity fails closed before any prior-scope data renders. The operator sees an explicit blocking state, never an empty-looking result.
  - There is no default tenant.
  - The proof must run through the real production EventStore adapters. Unit-only proof is not enough.
- **Projections (FR11, FR12, FR29.2):**
  - Filtering is debounced and resettable.
  - Virtualization starts at 500 rows, and unfiltered results cap at 10,000.
  - Column prioritization applies above 15 columns.
  - SlowQuery appears at 2,000ms.
  - Realtime recovery:
    - Retries are unbounded, with jittered backoff capped at 30,000ms.
    - A closed connection restarts within 10s.
    - Fallback polling runs every 15s across at most 8 lanes.
    - The Reconnected notice shows for 3,000ms.
  - A SignalR nudge never counts as command success.
- **Command lifecycle (FR14, FR15, FR16):**
  - Lifecycle states: Submitting, Acknowledged, Syncing, Confirmed, IdempotentConfirmed, Rejected, NeedsReview, Warning, and Degraded. Acknowledged is never shown as Confirmed.
  - Degraded starts at 10,000ms. Status polling runs every 1,000ms up to 120,000ms, which is a terminal ceiling with no success claim.
  - Zero retries before acceptance. Exactly one transient retry runs 250ms after acknowledgement and reuses the same MessageId.
  - Authorization runs before and after BeforeSubmit and again at the service boundary.
  - Destructive actions need explicit confirmation.
  - Forms edited for 30 seconds or more get an abandonment guard.
  - FC-CNC allows one in-flight local command. A second submit is blocked, not queued, batched, or raced.
- **Fresh rows (FR13, FR26):**
  - Published only by the resolver, from an immutable pre-dispatch target identity plus an independent Material terminal outcome.
  - Suppressed for Unknown identity or materiality, NoOp, delete, Rejected, NeedsReview, and server-allocated keys. The server-allocated-key case (DW-679) is an accepted non-goal.
  - Identity is first-wins by (ViewKey, EntityKey).
  - The active window is ten seconds, and expiry is silent.
- **Accessibility (NFR3):**
  - WCAG 2.2 AA throughout.
  - 320 CSS-px reflow and 400% zoom.
  - WCAG 1.4.12 text spacing.
  - 24×24 CSS-px targets, or exactly one recorded exception.
  - Focus is never obscured by app-owned content, which is stricter than WCAG 2.4.11.
  - Forced colors and reduced motion preserve meaning.
  - Axe runs with WCAG 2.2 AA tags; the current WCAG 2.1-only tag set is not enough.
  - A manual assistive-technology and real-device pass is mandatory. Automated evidence never replaces it.
- **Support safety (NFR5, NFR6):** UI, logs, telemetry, snapshots, and evidence never expose tokens, JWTs, tenant payloads, raw EventStore metadata, stack traces, or unrestricted PII.
- **Preserved coverage (FR29.4):** Keep the existing direct coverage of ReturnPathValidator and the single StorageKeys builder.
- **Evidence discipline:**
  - Reuse existing focused bUnit, e2e, and Shell tests, and add only the missing row assertions.
  - Do not create new report wrappers, workflows, or lanes just to restate proof that already exists.
  - Use deterministic fake time for every timing boundary and assert both sides of it (for example 249/250ms, 1,999/2,000ms, 119,999/120,000ms).
- **Gate separation:**
  - No implementation story closes or implies OI-16, G-4, FLUENT-APP-1, or Product approval. Each closes only through its own record and owner decision.
  - A-class stories (13.8, 13.9) close only through a dated decision that cites immutable evidence.
  - A rejection creates a new bounded residual item. It never rewrites completed history.

## Technical Decisions

- **Tenant-scope mechanics:**
  - Tenant is resolved through the tenant-context accessor. A missing tenant raises `TenantContextException` instead of querying.
  - SignalR groups are keyed by (projectionType, tenantId, scope). A stale tenant marks its group Blocked.
  - Storage keys use the form `{tenant}:{user}:{feature}[:{discriminator}]`. When scope is missing, persistence is skipped with diagnostic HFC2105.
  - Without a tenant, counts read 0 and reconciliation is skipped.
  - `StorageReady` is not dispatched without both tenant and user.
- **FC-NIP invariants:**
  - `IPendingCommandOutcomeResolver` is the only terminal pending-command owner and the only fresh-row publisher. Callbacks and adapters only emit observations.
  - Target identity comes only from explicit generated metadata: a typed `ICommandTargetIdentityProvider<TCommand>` or a declared `SameAsSource` snapshot.
  - Nudges, row diffs, AggregateId, and untyped payloads are forbidden as identity sources.
  - Every add, dismiss, expiry, clear, or scope mutation invalidates subscribed consumers.
  - Scope is enforced before state is read or rendered.
- **Shell layering:**
  - Components render.
  - Routing is pure.
  - State never depends on Components.
  - Polling and background workers stay in Infrastructure.
  - Fluxor single-writer and scoped-lifetime discipline apply.
- **Routes and information architecture:**
  - Each bounded context is one Module with exactly one primary entry and one default tab.
  - Module tab routes use `/{module}/{tab}`, and `/{module}` is the alias for the default tab.
  - Commands use `/commands/{BoundedContext}/{CommandTypeName}`.
  - Projection flyouts are secondary navigation only.
  - Exactly one navigation item is current, chosen by longest segment-prefix match.
- **UI system:**
  - Use FrontComposer or Fluent UI Blazor V5 components only. No raw `<button>`, `<input>`, `<select>`, or `<textarea>`.
  - Use Fluent 2 tokens and parameters. No V4/FAST tokens, hard-coded semantic palettes, custom type ramps, or invented numeric breakpoints.
  - Use the shared breakpoint watcher.
- **Preserved metrics:**
  - 72px labelled rail and 48px icon-only rail.
  - 32px compact rows at default text settings only. This is not a clipping ceiling.
  - `75rem` constrained measure.
  - The nine `FcTypoToken` mappings with `TypographyMappingVersion = "3.1.0"`.
- **Accent:** `--fc-color-accent`, if present, is only an alias of the active Fluent V5 accent role. It has no independent seed or palette, and it is a thread, never a chrome fill. This canonical rule wins over the older "#0097A7 default" wording in the epics UX-DR list. The exact accent API name is pending FLUENT-APP-1.
- **Public API:** Removing the `ShowAccountMenu`, `HeaderStart`, and conditional-navigation opt-outs (13.2), or adding Testing helpers (13.7), must update the intentional PublicAPI baseline.

## UX & Interaction Patterns

- **Authority:** `ux-design.md` canonical matrix rows are binding: UX-AM-1 (AM-01 to AM-31), UX-VR-1, UX-FM-1, UX-OF-1, UX-SS-1 (SS-01 to SS-49), and UX-AE-1. The DESIGN.md and EXPERIENCE.md supplements lose on conflict. Each story names the rows it implements and the baseline it preserves.
- **Announcement channels:** Each surface has exactly one channel per event:
  - Polite status: one shared `role="status"`/`aria-live="polite"` node per surface.
  - Palette combobox status: a single `aria-atomic` owner; inherited result-count speech is omitted.
  - Focused summary: `role="group"`, labelled, `tabindex="-1"`, no live attributes.
  - Focused heading: no live attributes.
  - Never `role="alert"`, never assertive, and never both a live region and focus for the same event.
- **Dedupe and coalescing:**
  - The dedupe key includes the state or result identity.
  - The group key is one of: operation ID, surface plus connection epoch, surface plus operator load/filter, or palette session.
  - A trailing 250ms window applies, the last eligible message wins, and stale results are discarded.
  - A terminal outcome, focused summary, or navigation failure cancels the pending message and announces immediately.
  - Retries, polls, skeleton frames, virtualization batches, re-renders, and expiry stay silent.
- **Copy:** Canonical AM-row copy is final microcopy. Only the visual treatment of Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal is unresolved. Until UX and Product decide, inherit the nearest Fluent semantic treatment.
- **Focus rules:**
  - Successful navigation focuses the unique route h1, clear of sticky chrome.
  - Failed navigation keeps focus where it was and announces AM-23.
  - Overlays (palette, settings, destructive dialog, abandonment guard) capture their origin as a direct handle before activation. On close, focus returns to that origin. If the origin is gone, focus goes to the route h1, or to the form heading for the guard. Focus never lands on `body`.
  - Modal depth is at most one.
  - The abandonment guard is in-flow, not a dialog. Stay has initial focus, and Escape keeps the operator on the form.
- **Validation:**
  - Client errors produce a complete linked summary that is focused after it is inserted.
  - A safely field-mapped server rejection uses the focused summary (AM-19) and suppresses AM-14.
  - An unmapped rejection stays in the lifecycle region with AM-14 through polite status and invents no field errors.
  - Useful input is preserved.
- **`/` shortcut:** Focuses the single enabled `FcPageToolbar` search input; otherwise it does nothing. Remove the current behavior that targets the first grid column filter.
- **Status meaning:** Always icon or shape plus text. Never color, hover, or motion alone.

## Cross-Story Dependencies

- 13.2 through 13.5 split announcement ownership:
  - 13.2 owns AM-23, AM-25, AM-28, AM-29, and AM-31.
  - 13.3 owns AM-18, AM-19, and AM-20.
  - 13.4 owns AM-01 to AM-17, AM-22, AM-24, AM-26, AM-27, AM-30, and the coalescing rule.
  - 13.5 owns AM-21.
  - Consume another story's row; do not redefine it.
- 13.6 covers every surface changed by 13.2 to 13.5. Its opt-in UX reviewer gate runs on 13.6's own evidence without waiting for the others.
- 13.7 extends the existing Testing harness to express all the matrices. Consumer tests use realistic failure or policy states.
- 13.1's scope rules underpin the fail-closed states in 13.4 (SS-05, SS-06), fresh-row scope clearing in 13.5 (SS-48), and scoped preference persistence in 13.2.
- 13.9 (FLUENT-APP-1) requires UX-A through UX-F evidence for one exact catalog identity. It feeds G-4, together with DOC-A to DOC-C in Epic 15 and Product re-approval.
- 13.8 (E9-APP-1) cites the immutable Story 9.8 live record at candidate `7a573763` and closes G-5. It does not reopen Stories 9.1 to 9.8.
- The user-visible auth, scope-loss, and oracle behavior (SS-05, SS-06, SS-38, SS-42, AM-26, VR-04) cross-references MCP-SEC-2 (Epic 14, Story 14.2). This epic is not security approval.
- The shell frame, account control, and information-architecture structure belong to Epic 12. Epic 13 owns only their interaction and focus behavior.
