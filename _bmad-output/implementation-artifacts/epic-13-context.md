# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, run commands, recover from transport failures, and read lifecycle and fresh-row state with no stale-tenant leakage and no false success. The epic proves tenant/user scope end to end through real production seams (TEN-SCOPE-1). It makes the focus, announcement, validation, state, accessibility, and testing-helper contracts deterministic and evidenced (UX-A to UX-F). It then records two owner decisions: Product acceptance of the Epic 9 fresh-row live proof (E9-APP-1), and finally the Product Owner + Architect decision on the exact Fluent UI V5 catalog identity and RC/GA posture (FLUENT-APP-1). That last decision is made against the identity the UX evidence actually tested. The delivered projection, lifecycle, and FC-NIP runtime is a preserved baseline and is not reimplemented.

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

- **Scope:** tenant/user scope is resolved before every query, subscription, count, preference, pending state, and fresh-row operation. Missing, stale, or mismatched scope fails closed to an explicit blocking state, never to empty-looking data. There is no default tenant. Without scope, persistence is skipped with the governed diagnostic.
- **Timing truth:** Acknowledged is never success. Degraded starts at 10,000ms, polling runs every 1,000ms for at most 120,000ms, there are zero pre-accept retries, and there is one 250ms same-MessageId transient retry. Realtime retries back off with jitter, capped at 30,000ms, without limit; a closed connection restarts within 10s; fallback polling runs every 15s on at most 8 lanes; the Reconnected notice shows for 3,000ms.
- **Fresh rows:** a marker appears only for a resolver-owned, eligible Material outcome with an immutable pre-dispatch identity. It is first-wins by ViewKey/EntityKey and expires silently after 10s. No marker for Unknown, NoOp, delete, Rejected, NeedsReview, or server-allocated keys.
- **Accessibility:** WCAG 2.2 AA, covering 320px reflow, 400% zoom, text spacing, 24×24 targets (or a recorded exception), unobscured focus, forced colors, and reduced motion. Axe runs with WCAG 2.2 AA tags. A manual assistive-technology and real-device pass is mandatory.
- **Support-safe output:** no tokens, JWTs, EventStore metadata or payloads, stack traces, or unrestricted PII in UI, logs, or evidence.
- **Evidence economy:** reuse focused tests and immutable artifacts. Never add duplicate packets, wrapper reports, or validators. Approvals cite evidence; they do not reproduce it.
- **Gate discipline:** I stories claim implementation only. A stories (13.8, 13.9) close only through a dated, named decision receipt. Green evidence never implies OI-16, G-4, FLUENT-APP-1, or Product approval. A rejection leaves its gate open and creates a bounded residual; completed evidence is never relabelled.

**Fluent UI V5 posture (FLUENT-APP-1 / OI-4 / D-13):**
- The deciders are the Product Owner and the Architect. One approved change proposal named the Release Owner as owner, so state the signer set explicitly in the receipt.
- The dated decision names:
  - the exact package ID and version, plus the Builds catalog revision that selects it;
  - RC or GA status;
  - the supported token/API names, including the exact API name of the active accent role under that pin;
  - the breaking-change rule;
  - the GA re-decision trigger;
  - the accessibility/visual revalidation trigger;
  - the accessibility evidence requirement.
- It either accepts the identity or records a dated alternative with revalidation triggers. A floating label, a legacy token, or an unverified component name cannot stand in for the identity. It must rest on UX-A to UX-F evidence for one exact catalog identity.
- Accepting closes OI-4 only. G-4 also needs OI-10 (DOC-C), OI-16, and OI-19, then a refreshed digest-bound postfix reviewer gate, then D-9 re-approval through PRD-APP-1. Selecting a package is not approval. Documentation may cite the decision instead of copying its evidence.

## Technical Decisions

- **Catalog-owned versions:** the Hexalith.Builds catalog (`Props/Directory.Packages.props`) at the root-selected `references/Hexalith.Builds` gitlink pins all versions. No inline versions and no second version registry. Fluent-pin evidence bound to one Builds revision is never projected onto another.
- **Identity drift (observed at compile time, 2026-10-20):**
  - The planning docs still record `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` (and the contingency doc cites rc.2).
  - The gitlink history moved rc.5 → `5.0.0` GA on 2026-09-30, then → `5.0.1` (Components and Icons) on 2026-10-17. The current gitlink `5d03f1db…` selects `5.0.1`.
  - So the "GA triggers re-decision" condition has fired, and the "RC posture" wording in the planning docs is stale.
  - Epic 13 evidence may span more than one of these identities. Check what each evidence set binds to.
  - Read the identity from the gitlink (`git ls-tree` + `git show <sha>:Props/Directory.Packages.props`), not from a local Builds checkout.
- **D-13 rule:** a breaking Fluent update is a FrontComposer major version and needs conformance plus accessibility/visual evidence.
- **Fluent governance:**
  - Use FrontComposer and Fluent UI Blazor V5 components with Fluent 2 tokens. No raw interactive controls outside documented carve-outs, no legacy v4/FAST tokens, and no theme redefinition.
  - `--fc-color-accent` may only alias the active Fluent V5 accent role; it never owns a seed or a palette. `FcShellOptions.AccentColor` (default `#0097A7`) is a thread, never a chrome fill.
  - Keep the nine `FcTypoToken` mappings and `TypographyMappingVersion` 3.1.0. Keep 32px compact rows, 72px/48px rails, and the 75rem measure. No numeric breakpoints.
- **Name resolution:** documented FrontComposer and Fluent API names must resolve against the selected pin.
  - `FcPageToolbar` is the public contract; its internal Fluent composition is not promised.
  - Inherited Fluent types: `FluentAccordion`, `FluentBadge`, `FluentTooltip`.
  - Surface roles: `--colorNeutralBackground1..3`, `--colorNeutralStroke2`, `--colorNeutralForeground1..3`.
- **FC-NIP:** `IPendingCommandOutcomeResolver` is the single terminal and fresh-row owner. Identity never comes from SignalR nudges, row diffs, aggregate IDs, or untyped payloads. SignalR groups are keyed by projection type, tenant, and scope; a stale tenant blocks its group.

## UX & Interaction Patterns

- `ux-design.md` is the canonical UX authority, and its supplements lose on conflict. AM-row copy is final microcopy, using one channel per event: the polite status region, the focused summary, or the focused heading.
- The visual treatment of Warning, NeedsReview, Degraded, missing tenant, FallbackPolling, SlowQuery, MaxItems, and fresh-row dismissal is unresolved. These states inherit the nearest Fluent semantic treatment, and UX/Product own the disposition.
- The visual contract cannot be marked final until the FLUENT-APP-1 pin and the accent-role API name are confirmed. Until then, do not add a replacement alias or a hard-coded token.

## Cross-Story Dependencies

- 13.9 comes last on purpose, after UX-A to UX-F (13.2–13.7, done) and E9-APP-1 (13.8, closed 2026-10-20). This puts the accessibility evidence before the Fluent decision.
- 13.8 is the A-class precedent: a dated decision receipt in the story's implementation spec, cited from the PRD gate/OI rows and the sprint-status alias tracker.
- Story 15.4 (DOC-C, backlog) consumes 13.9. It names the exact identity, reports the real FLUENT-APP-1 state, cites the decision, and adds a drift check on the existing doc validation path. Stories 15.2/15.3 (DOC-A/DOC-B) close OI-19. Story 17.16 (PRD-APP-1) needs OI-4, OI-10, OI-16, OI-19, and the reviewer gate.
- Tracker drift: the PRD still lists OI-16 as open, and the alias tracker still marks UX-A to UX-F as `proposed`. Neither means OI-16 is closed or open; never infer closure.
- The user-visible auth/scope-loss behavior cross-references MCP-SEC-2 (Story 14.2).
