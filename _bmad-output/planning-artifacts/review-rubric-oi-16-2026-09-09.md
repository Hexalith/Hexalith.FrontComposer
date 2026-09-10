# Spine Pair Review — Hexalith.FrontComposer OI-16

## Overall verdict

The repaired D-8 chain is substantially stronger than the prior snapshot: all six PRD journeys are source-extractable, the visual and behavior supplements share an exact body-level component registry, source paths and token references resolve, and the requested accessibility dimensions now have explicit matrices. It is **not yet handoff-ready**, because several matrices still leave load-bearing behavior indeterminate, the state-by-surface claim is broader than its rows, and the evidence ledger still collapses implementation work into evidence work in ways that could let downstream planning skip required changes.

## 1. Flow coverage — strong

PRD UJ-1 through UJ-6 were compared with the Key Flows and visual journey-coverage table. All six source IDs and names are preserved verbatim; each behavior flow has a named protagonist, numbered steps, a climax, and an applicable failure path (`prd.md:78-92`; `ux-experience-2026-07-05.md:214-301`; `ux-design-detailed-2026-07-05.md:222-231`). Nonvisual UJ-4 and UJ-5 are retained without inventing human-facing surfaces.

### Findings

No misses.

## 2. Token completeness — adequate

All five dotted token references resolve, load-bearing light/dark and forced-colors combinations have contrast targets, and the lack of product-owned hex colors is justified by the repository's mandatory Fluent V5 inheritance/no-theme-redefinition policy (`ux-design-detailed-2026-07-05.md:21-73,93-115`). This is the UI-system-inheritance exception described by the DESIGN.md spec, not a missing-color defect.

### Findings

- **[medium] Custom radius and spacing entries are not all machine-usable DESIGN.md values.** `rounded.framed-surface-max` is an object with a prose `note` even though it is a FrontComposer-owned numeric delta and is dereferenced as `{rounded.framed-surface-max}`; `spacing.chrome-padding-x` says “normally 12px” and `spacing.toolbar-gap` is prose rather than a CSS dimension or exact inherited token (`ux-design-detailed-2026-07-05.md:42-52,169-170`; `design-md-spec.md`, “Frontmatter tokens”). A downstream token consumer cannot render these values deterministically. *Fix:* make the owned maximum a scalar `8px`; omit purely inherited spacing entries or bind them to an exact supported Fluent token/value, keeping rationale in prose.

## 3. Component coverage — strong

The canonical registry's 15 FrontComposer identifiers are present verbatim in both supplements, and the visual and behavioral tables also match on the three explicitly inherited Fluent components (`ux-design.md:74-83`; `ux-design-detailed-2026-07-05.md:173-197`; `ux-experience-2026-07-05.md:76-99`). Every row carries substantive visual and behavioral rules. `FcPageToolbar` is correctly treated as public contract while its Fluent composition remains implementation-owned.

### Findings

No misses.

## 4. State coverage — broken

The canonical matrices were checked against every Information Architecture surface, every FR-11 projection state, every FR-15 lifecycle state, no-tenant/no-access, blocked submit, announcements, validation/rejection, focus, and the responsive/accessibility dimensions required by Addendum §4 (`ux-experience-2026-07-05.md:40-55`; `ux-design.md:128-236`; `prd-addendum-2026-09-08.md:76-108`). FR-11 states, the nine named FR-15 states, blocked submit, and the requested reflow/zoom/text-spacing/target/focus/forced-colors/reduced-motion dimensions are all represented, but the matrices are not yet closed.

### Findings

- **[high] The state-by-surface matrix does not cover every state family declared by its own IA.** The behavior supplement says the Application shell requires bootstrap and route-failure states; Module workspace requires Loading, default/selected tab, and invalid-route fallback; Module tabs require selected, disabled, and invalid/default fallback; and palette/dialog overlays require open, no-results, denied/failure, close/cancel, and removed-invoker fallback. UX-SS-1 has no rows for those states, even though the State Patterns introduction says every IA surface lands on a row (`ux-experience-2026-07-05.md:42-51,101-122`; `ux-design.md:187-219`). Focus or component prose supplies fragments, but not the required entry evidence, visible meaning, permitted actions, recovery/timeout, announcement, terminality, and delivery/evidence columns. Projection query failure/offline behavior is likewise not distinguished from realtime reconnection. *Fix:* add the missing shell/workspace/tab/overlay/query rows to UX-SS-1, or narrow each IA “Required state family” cell and explicitly mark genuinely inapplicable states.

- **[high] `Degraded` has a contradictory terminal classification and no deterministic post-polling-ceiling outcome.** UX-SS-1 defines terminal as unable to advance automatically, then labels `Degraded` a “Terminal landing” while saying polling may continue to 120,000 ms and later evidence updates it (`ux-design.md:189-190,217`). FR-15 requires `Degraded` at 10,000 ms while polling continues for at most 120,000 ms (`prd.md:338-346`). The contract never states what visible state, actions, announcement, or terminality apply when the polling ceiling is reached. *Fix:* classify Degraded as non-terminal while polling can advance it, then define the deterministic ceiling transition/state; alternatively define a separately named post-ceiling terminal phase without changing the stable FR-15 state vocabulary.

- **[high] Announcement coalescing and several state announcements remain non-deterministic.** AM-03 and AM-12 say to coalesce “rapid” results/progress but supply neither a time window nor an event-boundary rule, although Addendum §4.1 assigns that decision to UX (`ux-design.md:139,148`; `prd-addendum-2026-09-08.md:76-86`). No-registration, no-tenant, no-access, authorization denial, and palette no-results outcomes sit outside UX-AM-1 or say only “once,” without a canonical channel, complete dedupe key, or announcement-count evidence (`ux-design.md:194,198-199`; `ux-design.md:168`; `ux-experience-2026-07-05.md:87`). *Fix:* define one measurable coalescing rule and add/cross-reference complete AM rows for every announced state; mark intentionally silent settled states explicitly.

- **[high] The focus matrix covers dialog exit but not deterministic dialog entry.** FM-05 specifies close/cancel return only; `FcSettingsDialog` says it traps focus, while destructive confirmation and abandonment refer to an undefined “safe default” (`ux-design.md:180,170`; `ux-experience-2026-07-05.md:88-90,159-170`). NFR-3 requires deterministic palette/dialog focus, so implementers still cannot derive the initial target, name/description source, containment behavior, or safe destructive-confirmation default. *Fix:* add an overlay-focus matrix with invoker, initial focus, accessible name/description owner, containment, submit/error destination, Escape behavior, and close fallback for palette, settings, destructive confirmation, and abandonment.

- **[high] Addendum §4.5's generated field-group relationship is absent from the validation contract.** VR-01 covers each invalid input and linked summary, but neither supplement requires declared field groups to preserve group identity, description, and order (`ux-design.md:161-170`; `ux-experience-2026-07-05.md:133-142`; `prd-addendum-2026-09-08.md:117-121`). A generated form can satisfy the current matrix while losing the required group semantics. *Fix:* extend VR-01 or add a generated-form row requiring visible labels, programmatic group name/description, stable error targets, declared group/field order, and corresponding bUnit relationship assertions.

## 5. Visual reference coverage — strong

No `imports/`, `mockups/`, or `wireframes/` directories or files exist beside the D-8 artifacts, so there are no orphaned or unspecific references. The chain-over-visual-artifact conflict rule is stated once in each supplement (`ux-design-detailed-2026-07-05.md:76-80`; `ux-experience-2026-07-05.md:22-26`).

### Findings

No misses.

## 6. Bloat & overspecification — adequate

The matrices are lengthy but warranted by OI-16's deterministic handoff requirement. The experience supplement generally indexes the canonical matrices rather than silently redefining them, and UJ-4/UJ-5 explain nonvisual applicability instead of adding fictional UI. Some duplication is deliberate join material for a legacy three-file chain and does not yet impede extraction.

### Findings

No material overspecification found.

## 7. Inheritance discipline — adequate

All supplement `sources:` paths resolve, every dotted token reference resolves, UJ names are verbatim, component names match between body registries, FC-IA-1 is correctly retained as non-authoritative history, and the documents explicitly preserve pending Product approval and open G-4/OI-16/OI-19 (`ux-design.md:20-27,253-267`; `ux-experience-2026-07-05.md:303-314`). No rejected alternate two-spine workspace participates in the authority chain.

### Findings

- **[high] Delivery, implementation, and evidence state are still collapsed rather than distinguished.** Frontmatter labels OI-16 `open-evidence-work`, while the ledger combines “New implementation / evidence obligation” in one column and several rows describe only evidence even where changed rendering or behavior may be required (`ux-design.md:4-9,238-251`). PRD Table A explicitly says the 2026-09-09 UX deltas include implementation and evidence work and preserves only the earlier runtime baseline as delivered (`prd.md:142-160`). In particular, icon-plus-visible-text status, dialog-entry focus, generated relationship semantics, and forced-colors/reduced-motion fallbacks cannot be assumed delivered merely because the prior icon/tooltip, dialog, form, or fresh-row runtime exists. *Fix:* change the frontmatter state to implementation-and-evidence work, and split the ledger into separate “delivered baseline,” “open implementation delta,” and “open evidence” columns, using `none identified` only where source/repository evidence supports it.

- **[medium] The OI-16 trail describes the two already-updated supplements as still requiring reconciliation.** The canonical trail says the visual and behavior supplements require reconciliation even though both carry the same `oi-16-2026-09-09` revision and updated contract (`ux-design.md:257-259`; `ux-design-detailed-2026-07-05.md:4-7`; `ux-experience-2026-07-05.md:3-6`). This makes the handoff state internally stale. *Fix:* mark both “reconciled; pending clean document review and implementation evidence,” without closing OI-16, SM-6, G-4, or Product approval.

## 8. Shape fit — strong

The visual supplement follows the canonical DESIGN.md body order: Brand & Style, Colors, Typography, Layout & Spacing, Elevation & Depth, Shapes, Components, and Do's and Don'ts. The behavior supplement contains every required EXPERIENCE spine section plus applicable Responsive & Platform and Inspiration & Anti-patterns; its additional matrix indexes and Brownfield Reconciliation earn their place. The user's legacy canonical-plus-two-supplement form is explicitly governed by D-8 and was assessed as such rather than as the rejected alternate folder.

### Findings

No misses.

## Mechanical notes

- Source paths: all resolve in both supplements.
- Key Flows: 6/6 preserve canonical UJ identifiers/names and include protagonist, numbered steps, climax, and failure handling.
- Body-level component registry: 18/18 exact matches between visual and behavior supplements; the canonical 15 FrontComposer identifiers are retained.
- Dotted token references: 5/5 resolve; one referenced custom radius is structurally non-scalar.
- Requested matrix families are present: announcements, validation/rejection, focus, state-by-surface, reflow/zoom, text spacing, target size, unobscured focus, forced colors, and reduced motion. Completeness defects are listed above.
- Visual artifacts: none; no orphan check required.
- Mermaid: none.
- Approval integrity: `product_approval: pending-reapproval`; G-4, OI-16, OI-19, and SM-6 remain open. The documents do not claim current Product approval.
- Severity totals: Critical **0**, High **6**, Medium **2**, Low **0**.
