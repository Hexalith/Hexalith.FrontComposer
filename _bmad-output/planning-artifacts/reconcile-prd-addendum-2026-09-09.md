# UX Input Reconciliation — PRD Addendum (2026-09-09)

## Source

Exact source reconciled: `_bmad-output/planning-artifacts/prd-addendum-2026-09-08.md` (`updated: 2026-09-09`), especially §4, “Qualitative UX Rules.” The addendum supports the canonical PRD; it does not replace `ux-design.md` as UX authority.

## Adopted Into The UX Authority Chain

- Preserve the settled module/tab routes, plural-label default with `Overview` fallback, in-module projection flyout, exact compact `32px` grid metric, always-visible hamburger, optional `/` search shortcut, Aspire-derived chrome language, UX-DR2 status treatment, urgency ordering, and Tenants/Parties adopter disposition.
- Adopt the §4.1 announcement families and observable outcomes: polite, deduplicated, coalesced projection and command progress; one terminal command outcome; focused client-validation/submit-blocking errors; once-per-attempt blocked-submit feedback; once-per-tenant/user/entity fresh-row appearance; silent retry ticks and fresh-row expiry.
- Adopt §4.2's deterministic focus and recovery outcomes: route/palette navigation to the route `h1`, active-tab focus retention, palette/dialog return to invoker, linked and focused validation summary, first-invalid navigation, useful-input preservation, lifecycle treatment for unsafe-to-map server rejection, and keyboard-only recovery.
- Adopt §4.3's complete state-by-surface matrix. Every FR-11 state, every FR-15 state, blocked submit, no-tenant, and no-access row records entry evidence, visible meaning, permitted actions, recovery/timeout, announcement, and terminal/non-terminal classification.
- Adopt per-changed-surface evidence matrices for 320 CSS-pixel reflow, 400% zoom, resilient text spacing, WCAG 2.2 AA target size with standard exceptions, unobscured focus, light/dark/forced-colors, and reduced motion. Navigation-current, focus, status, stale/reconnect, lifecycle, and fresh-row meaning never rely on animation or color alone.
- Adopt §4.4 source integrity: exact public names, the `FrontComposerNavigation` contract, `FcPageToolbar` as public contract, `--fc-color-accent` only as an active-Fluent-role alias, and full contrast/forced-colors fallback coverage in the UX source.

## Delivery And Evidence Status

- The quoted §4 baseline behavior is preserved as delivered product intent; FC-NIP runtime composition and the named 2026-08-27 live proof remain delivered, with only DW-679 open.
- Sections 4.1 through 4.4 are the 2026-09-09 validation delta. Complete matrices, deterministic assertions, and Governance/e2e/bUnit evidence are new OI-16/SM-6 work and are not represented as already delivered.
- Exact announcement wording, live-region implementation, deduplication keys, and coalescing windows remain UX/implementation-owned, but the observable outcomes and support-safe evidence are mandatory.

## Conflicts And Dispositions

- The dead 2026-07-05 PRD path is replaced in both supplements by `_bmad-output/planning-artifacts/prd.md`; both supplements carry a 2026-09-09 reconciliation marker.
- Unavailable, vague, or implementation-only component names are not retained as public promises. Use exact selected-pin APIs, `FrontComposerNavigation`, and public `FcPageToolbar`; keep the toolbar's current Fluent composition implementation-owned.
- The settled FC-IA-1 chronology is removed from the active behavior contract and retained only as reconciliation/decision history.
- Tooltip/color status language is reconciled with FR-11/NFR-3 by ensuring meaning remains available without hover or color. Transport acceptance never becomes Confirmed; asynchronous server rejection is not recast as field validation without a support-safe mapping.
- No addendum idea was silently dropped: each applicable qualitative rule is adopted, retained as history, or explicitly rejected/re-scoped above.

## Open Gate

OI-16 and SM-6 evidence work remain open. This artifact does not close G-4, record Product approval, amend D-9, close OI-19, or claim handoff/readiness approval. The digest-bound postfix reviewer gate must still report no unresolved Critical/High finding before Product may re-approve the exact PRD/addendum pair.
