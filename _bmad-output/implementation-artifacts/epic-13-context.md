# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success. Prove tenant isolation, predictable focus and announcements, accessible operator journeys, and reusable adopter assertions while preserving delivered behavior. Implementation evidence and independent owner decisions remain separate.

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

- Resolve tenant/user scope before queries, subscriptions, counts, preferences, pending state, and fresh-row operations. Missing, invalid, mismatched, or stale scope blocks explicitly. Never render prior-scope data or disguise missing context as empty results; clear old state and groups before another scope renders. Prove isolation through production adapters and preserve privacy.
- Transport acceptance never establishes confirmed success. Preserve Degraded at 10,000ms, polling every 1,000ms for at most 120,000ms, and one 250ms transient retry with the same MessageId. Block a second local command with localized accessible feedback; never queue it. Preserve projection recovery budgets.
- Fresh cues require explicit immutable pre-dispatch identity and an eligible Material outcome. Publish atomically first-wins per view/entity and expire silently after ten seconds. Unknown, NoOp, delete, Rejected, NeedsReview, and server-allocated-key cases produce no marker. Nudges establish neither identity nor success.
- Establish WCAG 2.2 AA across changed shell, Home, navigation, tab, toolbar, projection/detail, form, lifecycle, palette, dialog, and abandonment surfaces. Cover 320 CSS-pixel reflow, actual 400% browser zoom from a 1280 CSS-pixel reference viewport, text spacing, targets, unobscured focus, contrast, forced colors, reduced motion, semantics, keyboard access, and announcement counts.
- Apply line height 1.5 times, paragraph spacing twice, letter spacing 0.12em, and word spacing 0.16em together without clipping, overlap, truncation, or lost operations. Only labelled grid and tab regions may own bounded horizontal scrolling. Entire focused targets and indicators remain visible and unobscured.
- Targets meet 24 by 24 CSS pixels or record one justified Inline, Spacing, Equivalent, User Agent Control, or Essential exception with measurements, rationale, and keyboard path. Normal text contrast is 4.5:1; large text and meaningful non-text/focus boundaries require 3:1. Changed pairs require computed proof; inherited Fluent pairs require inheritance plus visual/conformance evidence.
- Axe includes wcag2a, wcag2aa, wcag21a, wcag21aa, and wcag22aa alongside product assertions. Automation does not establish heard speech or physical-device observations; viewport resizing does not establish actual browser zoom. Missing manual evidence prevents accessibility-story completion. Disposition every Critical/High finding and run the opt-in UX gate or retain an explicitly authorized skip.

## Technical Decisions

- Package versions remain owned by the exact root-selected Hexalith.Builds catalog. Bind evidence to the identity tested. Product Owner and Architect recorded Components plus Icons 5.0.1 GA as not approved on 2026-10-21. Re-decision needs hosted Quality on a main commit selecting that exact identity, covering Chromium/Firefox/WebKit, accessibility-visual, and Gate 3a bUnit, followed by a dated owner decision. Human evidence remains separately required.
- After acceptance, patches need green Quality on the bump commit and a dated Architect addendum naming version/run; minor or major updates need a new decision. Breaking Fluent updates require a FrontComposer major and conformance/accessibility/visual revalidation.
- Use FrontComposer and Fluent UI Blazor V5 with Fluent 2 tokens. Preserve nine typography mappings/version 3.1.0, default 32px compact rows, 72px/48px rails, and 75rem constrained measure. Rows expand under spacing/reflow and virtualization stays truthful. No fabricated breakpoints, independent palette/type ramp, legacy FAST tokens, or theme redefinition. Accent remains a thread and any alias follows the active Fluent role.
- The approved bounded consumer-source mechanism preserves raw dependency inventory and binds canonical source hashes, exact owner revision, selected root Builds, and consumer configuration. Inventory-only identities remain untrusted; no whole-owner or nested-source execution is authorized. Candidate-only policy cannot authorize itself, and source projections close no independent governance gate.
- The 2026-10-23 evidence policy supersedes the earlier storage amendment and Stories 15.6/15.7. Record commands, pass/fail counts, commit SHA, and CI run link in the owning story/spec. Raw output stays local or in CI artifacts; evidence folders are ignored scratch. Do not create committed packets, manifests, runtime-identity records, replay lanes, or external evidence storage. Preserve source-owned baselines, privacy, and historical result meanings.

## UX & Interaction Patterns

Canonical matrices govern over supplements. Use one speech owner per event: shared polite status, focused non-live summary, or focused heading. Coalesce eligible intermediate changes for 250ms; terminal outcomes cancel pending progress and speak immediately. Suppress duplicate/restored announcements and retry/poll ticks; expiry stays silent. Visible text and icon/shape carry status without color, hover, or motion dependence. Forced colors preserve system cues; reduced motion removes nonessential transitions, pulse, and smooth scrolling while state/focus changes remain immediate. Route, tab, palette, dialog, validation, and recovery focus follow canonical destinations without stealing later operator focus or input.

## Cross-Story Dependencies

Reuse earlier focused verification and maintain one candidate-bound accessibility result. Historical fresh-row acceptance does not approve a later candidate. Completion of an approval-decision story may record rejection while leaving its approval gate open. OI-16 needs applicable deterministic/manual evidence and finding dispositions; G-4 additionally requires OI-4, OI-10, OI-19, the refreshed digest-bound reviewer gate, and Product re-approval. Story 15.4 owns published Fluent documentation correction. Tenant/auth loss also depends on MCP-SEC-2. Evidence-storage simplification changes no accessibility or owner-acceptance requirement.
