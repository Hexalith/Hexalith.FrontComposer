# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success. Establish tenant isolation through production seams, deterministic focus and announcements, accessible operator journeys, and reusable adopter assertions. Preserve the delivered projection, lifecycle, and FC-NIP behavior; implementation evidence remains separate from Product and Architecture decisions.

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

- Resolve tenant/user scope before queries, subscriptions, counts, preferences, pending state, or fresh-row operations. Missing, invalid, mismatched, or stale scope blocks explicitly; never show prior-scope data or disguise missing context as an empty result. Prove isolation through production adapters and clear old groups/state before rendering another scope.
- Transport acceptance never establishes confirmed success. Preserve Degraded at 10,000ms, status polling every 1,000ms for at most 120,000ms, and one 250ms transient retry with the same MessageId. Block a second local command with localized accessible feedback; never queue it. Preserve reconnect retry/fallback budgets.
- Fresh cues require explicit immutable pre-dispatch identity and an eligible Material outcome. Publish atomically first-wins per view/entity and expire silently after ten seconds. Unknown, NoOp, delete, Rejected, NeedsReview, and server-allocated-key cases produce no marker. Nudges cannot establish row identity or success.
- Establish WCAG 2.2 AA across every changed shell, Home, navigation, tab, toolbar, projection/detail, form, lifecycle, palette, dialog, and abandonment surface. Cover 320 CSS-pixel reflow, actual 400% browser zoom from a 1280 CSS-pixel reference viewport, combined text spacing, targets, unobscured focus, contrast, forced colors, reduced motion, semantics, keyboard access, and announcement counts.
- Apply line height 1.5 times, paragraph spacing twice, letter spacing 0.12em, and word spacing 0.16em together without clipping, overlap, truncation, or lost operations. Only labelled grid and tab regions may own bounded horizontal scrolling. Entire focused targets and indicators remain visible and unobscured.
- Targets meet 24 by 24 CSS pixels or record one justified Inline, Spacing, Equivalent, User Agent Control, or Essential exception with measurements, rationale, and keyboard path. Normal text contrast is 4.5:1; large text and meaningful non-text/focus boundaries require 3:1. Changed pairs need computed proof; inherited Fluent pairs need inheritance plus visual/conformance evidence.
- Axe uses wcag2a, wcag2aa, wcag21a, wcag21aa, and wcag22aa plus product assertions. Manual AT, heard-speech, actual zoom, and physical-device observations cannot be inferred from automation. Missing manual evidence prevents accessibility-story completion. Disposition every Critical/High finding; run the opt-in UX gate or retain an explicitly authorized skip.

## Technical Decisions

- Package versions belong to the exact root-selected Hexalith.Builds catalog; no inline versions or second registry. Bind results to the identity tested. Product Owner and Architect explicitly did not approve Components plus Icons 5.0.1 GA on 2026-10-21. Re-decision needs hosted Quality on a main commit selecting the exact identity, including Chromium/Firefox/WebKit, accessibility-visual, and Gate 3a bUnit, then a dated owner decision. Human evidence remains under OI-16.
- After acceptance, patches need green Quality on the bump commit and a dated Architect addendum naming version/run; minor or major updates need a new decision. Breaking Fluent updates require a FrontComposer major and conformance/accessibility/visual revalidation.
- Use FrontComposer and Fluent UI Blazor V5 with Fluent 2 tokens. Preserve nine typography mappings/version 3.1.0, default 32px compact rows, 72px/48px rails, and 75rem constrained measure. Rows grow under spacing/reflow; virtualization stays truthful. No invented breakpoints, independent palettes/type ramps, legacy FAST tokens, or theme redefinition. Accent remains a thread; any alias refers only to the active Fluent role.
- The 2026-10-23 owner evidence policy supersedes the earlier storage amendment and Stories 15.6/15.7. Proof is tests plus CI: record commands, pass/fail counts, commit SHA, and CI run link in the owning story/spec. Raw reports, traces, screenshots, logs, and binlogs stay local or in CI artifacts; evidence folders are ignored scratch. Do not create committed packets, manifests, runtime-identity records, replay lanes, or an external evidence store. Source-owned visual/Pact/Verify baselines remain tracked. Preserve privacy and never relabel historical/failed results as current approval.

## UX & Interaction Patterns

Canonical UX matrices govern over supplements. Use one speech owner per event: shared polite status, focused non-live summary, or focused heading. Coalesce eligible intermediate changes for 250ms; terminal outcomes cancel pending progress and speak immediately. Suppress duplicate/restored announcements and retry/poll ticks; expiry stays silent. Status meaning requires visible text and icon/shape without depending on color, motion, or hover. Forced colors preserve system cues; reduced motion removes nonessential transitions, pulse, and smooth scrolling while state and focus changes stay immediate. Route, tab, palette, dialog, validation, and recovery focus follow canonical destinations without stealing subsequent operator focus or input.

## Cross-Story Dependencies

Reuse focused earlier-story verification and keep one candidate-bound accessibility result. E9-APP-1 acceptance closes G-5/OI-1 only for its historical record. Completion of an approval-decision story may record rejection without closing that approval gate. OI-16 needs all applicable deterministic/manual evidence and finding dispositions; G-4 additionally requires OI-4, OI-10, OI-19, the refreshed digest-bound reviewer gate, and Product re-approval. Story 15.4 owns published Fluent documentation correction. Tenant/auth loss also depends on MCP-SEC-2. Storage-policy simplification changes none of the accessibility, manual-evidence, or independent-owner acceptance semantics.
