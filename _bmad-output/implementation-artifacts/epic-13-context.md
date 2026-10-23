# Epic 13 Context: Operators Trust Tenant-Scoped Data and Command Outcomes

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Operators can browse projections, execute commands, recover from transport failures, and understand lifecycle and fresh-row state without stale-tenant leakage or false success. Establish tenant isolation through production seams, deterministic focus and announcement behavior, accessible operator journeys, and reusable adopter assertions while preserving the delivered projection, lifecycle, and FC-NIP runtime. Keep implementation evidence separate from Product and Architecture acceptance decisions.

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

- Resolve tenant/user scope before queries, subscriptions, counts, preferences, pending state, and fresh-row operations. Missing, invalid, mismatched, or stale scope fails closed to an explicit blocking state, never an empty-looking result or default tenant. Clear prior-scope state before rendering another scope; skip persistence with the governed diagnostic when scope is unavailable. Prove isolation through real production adapters.
- Transport acceptance never means confirmed success. Preserve the 10,000ms Degraded threshold, 1,000ms polling for at most 120,000ms, and one 250ms transient retry. Reconnect retries use jittered exponential backoff capped at 30,000ms without an attempt limit; closed connections restart within 10s; disconnected fallback polls every 15s on at most eight lanes; Reconnected remains visible for 3,000ms. A second local command is blocked with localized feedback, never queued.
- Fresh cues require immutable pre-dispatch identity and an eligible Material outcome. Publish atomically first-wins per view/entity; expire silently after 10s. Unknown, NoOp, delete, Rejected, NeedsReview, and server-allocated-key cases produce no marker. SignalR nudges cannot supply identity or establish success.
- Establish WCAG 2.2 AA across every changed shell, Home, navigation, tab, toolbar, projection/detail, form, lifecycle, palette, dialog, and abandonment surface. Cover 320 CSS-pixel reflow, actual 400% browser zoom from a 1280 CSS-pixel reference viewport, combined text spacing, targets, unobscured focus, contrast, forced colors, reduced motion, semantics, keyboard access, and announcement counts.
- Apply spacing together: line height 1.5×, paragraph spacing 2×, letter spacing 0.12em, word spacing 0.16em. No loss, clipping, overlap, or control truncation. Only labelled grid and tab regions may own bounded horizontal scrolling. Entire focused targets and indicators remain visible and unobscured.
- Targets meet 24×24 CSS pixels or record one justified Inline, Spacing, Equivalent, User Agent Control, or Essential exception with measurements, rationale, and keyboard path. Text contrast is 4.5:1, large text 3:1, meaningful non-text/focus boundaries 3:1. Changed pairs require computed evidence; inherited Fluent pairs require inheritance plus visual/conformance evidence.
- Use axe tags wcag2a, wcag2aa, wcag21a, wcag21aa, and wcag22aa plus product assertions. Required manual AT, heard-speech, actual zoom, and physical-device observations cannot be inferred from automation. Missing human evidence prevents accessibility-story completion.
- Reuse focused evidence and retain one candidate-bound result with verified references. Sanitize before storage: no tokens, JWTs, raw EventStore metadata/payloads, stack traces, or unrestricted PII. Never relabel failed or historical evidence as current approval.

## Technical Decisions

- Versions belong to the exact root-selected Hexalith.Builds catalog; no inline versions or second registry. Bind evidence to the package/catalog identity actually tested. Components and Icons 5.0.1 GA were explicitly **not approved** by Product Owner and Architect on 2026-10-21. The GA re-decision trigger is consumed; D-13, OI-4, FLUENT-APP-1, and G-4 remain open. Acceptance requires hosted Quality on a main commit selecting the exact identity, covering Chromium/Firefox/WebKit, accessibility-visual, and Gate 3a bUnit, followed by a dated owner re-decision. Human evidence remains under OI-16.
- After acceptance, a patch needs green Quality on the bump commit and a dated Architect addendum naming version/run; minor or major updates need a new decision. A breaking Fluent update requires a FrontComposer major and conformance/accessibility/visual revalidation.
- Use FrontComposer and Fluent UI Blazor V5 components with Fluent 2 tokens. Preserve nine typography mappings and version 3.1.0, default compact rows of 32px, 72px/48px rails, and 75rem constrained measure. Rows grow under spacing/reflow; virtualization stays truthful. No invented breakpoints, independent palettes/type ramps, legacy FAST tokens, or theme redefinition. Any accent alias refers only to the active Fluent accent role; accent remains a thread.
- Generate full qualification output outside the checkout or in ignored CI staging. New attempt metadata is bounded to 10 MiB and command excerpts to 2,048 UTF-8 bytes; accepted proof needs durable immutable, verified retrieval. Preserve tracked source baselines and existing bound evidence until enumerated consumer migration. Missing, expired, corrupt, or substituted proof fails the consuming gate.

## UX & Interaction Patterns

Canonical UX matrices govern over supplements. Preserve one speech owner per event: shared polite status, focused non-live summary, or focused heading. Coalesce progress, suppress duplicate/restored announcements and retry/poll ticks, and keep expiry silent. Status meaning uses visible text and icon/shape without depending on color, motion, or hover. Forced colors retain system cues; reduced motion removes nonessential transitions, pulse, and smooth scrolling while state and focus changes remain immediate. Route, tab, palette, dialog, validation, and recovery focus follow their canonical destinations without stealing subsequent operator focus or input.

## Cross-Story Dependencies

Accessibility evidence precedes the exact Fluent posture decision; completion of an A-class decision story can record rejection without closing its approval gate. E9-APP-1 Product acceptance is recorded separately. OI-16 requires every required implementation/evidence row and finding disposition; G-4 additionally needs OI-4, OI-10, OI-19, a refreshed digest-bound reviewer gate, and Product re-approval. Story 15.4 owns published Fluent documentation correction. Stories 15.6/15.7 own browser-output migration and dependency-safe retirement under the approved storage amendment; neither storage work nor historical completion wording supplies missing manual evidence or owner approval. Auth/scope-loss behavior also depends on MCP-SEC-2.
