# Accessibility Review — Hexalith Common Application UX

## Overall verdict

**Needs revision before accessibility handoff.** The spine pair is substantially stronger than the legacy contract: it commits to WCAG 2.2 AA, measurable reflow/zoom/text-spacing/target-size outcomes, non-color state cues, fail-closed tenant behavior, linked validation, focus return, and restrained announcements. Four high-severity gaps still prevent independent implementers and test authors from deriving deterministic behavior for dialog entry, primary/flyout navigation, generated-form relationships, and a blocked second submit.

Finding counts: **Critical 0 · High 4 · Medium 6 · Low 1**.

## Strengths

- The accessibility floor is broad and measurable: landmarks and roles, accessible names, reading-order focus, live-status/error paths, 320 CSS-pixel reflow, 400% zoom, WCAG text-spacing overrides, 24×24 CSS-pixel targets or documented exceptions, unobscured focus, reduced motion, and forced colors are explicit (`EXPERIENCE.md:237-251`).
- Projection and lifecycle states mostly expose entry evidence, visible meaning, recovery, terminality, and announcement behavior instead of relying on generic “accessible” language (`EXPERIENCE.md:150-187`).
- Validation and rejection are separated: failed client validation focuses a linked summary and preserves input, while asynchronous rejection remains lifecycle feedback unless a safe field mapping exists (`EXPERIENCE.md:110, 169-185`).
- Status, stale/reconnect, lifecycle, and fresh-row meaning remain available without color, hover, or animation; the fresh marker expires silently (`DESIGN.md:77-82, 132-139`; `EXPERIENCE.md:107-114, 234-250`).
- Missing/stale tenant and authorization failures are explicit, support-safe, and fail closed rather than masquerading as empty data (`EXPERIENCE.md:127-145, 253-260`).
- All six source journeys have named protagonists, numbered actions, a climax, and a failure path. UJ-6 names the intended accessibility evidence across focus, keyboard recovery, announcements, reflow, zoom, text spacing, target size, forced colors, and reduced motion (`EXPERIENCE.md:282-351`).

## Findings

### Critical

None.

### High

1. **Dialog and palette focus entry and naming are not deterministic.** The palette, settings dialog, destructive confirmation, and abandonment guard define close/return behavior, while the global floor merely says their behavior is deterministic and that dialogs have semantics (`EXPERIENCE.md:99-100, 112, 225-245`; `DESIGN.md:124-125, 137`). No rule identifies the element focused on open, the accessible name/description source for each dialog, or the safe initial focus for a destructive confirmation. “Inherited Fluent behavior” cannot supply product-specific labels or decide whether the destructive action receives initial focus. *Fix:* add a dialog/palette focus table: invoker → initial focus → focus containment → accessible name/description source → submit/error focus → close/cancel return. Require palette focus on its search/combobox and state the deliberately safe initial focus for destructive confirmation.

2. **Navigation and projection-flyout semantics lose confirmed accessibility requirements.** The current rail contract specifies visual active treatment and the flyout only as “keyboard navigable” (`DESIGN.md:121`; `EXPERIENCE.md:96, 139, 223-229`). It does not commit the active route to a programmatic current-state property, require the icon-only tile's accessible label/tooltip, or define the flyout's role and Enter/Space/arrow/Escape/focus-return route. These were explicit in the confirmed `epics.md` source and are needed to test WCAG 1.3.1, 2.1.1, and 4.1.2. *Fix:* restore the exact component-level contract: `aria-current` (or the selected Fluent equivalent) for the one current route, accessible label plus keyboard-accessible tooltip in icon-only mode, and the inherited Fluent menu/popover keyboard model with declared role, open focus, activation keys, Escape, and invoker focus return.

3. **Generated forms do not define programmatic field, instruction, error, and group relationships.** The form rows require linked summary navigation and an accessible name for every interactive element, but do not state how an invalid control exposes its error, how helper/constraint text is described, or how declared field groups preserve their accessible group identity and order (`EXPERIENCE.md:110, 144, 169-173, 242-245`; `DESIGN.md:135`). A summary link alone does not satisfy the per-control relationship, and the confirmed source requires accessible field-group identity. *Fix:* require visible labels, programmatic group name/description, `aria-invalid` and Fluent-supported error-description association, stable summary-to-control targets, preserved declared field order, and focusable summary/first-invalid behavior. Add corresponding accessible-name/relationship assertions to the form acceptance contract.

4. **Blocked second submit is absent from the state-by-surface acceptance matrix.** The component and interaction prose say a later local submit is blocked and announced, but the generated-command state row omits this state and the lifecycle matrix has no entry evidence, focus outcome, permitted action, recovery, or per-attempt deduplication rule (`EXPERIENCE.md:113, 134-148, 169-187, 233-245`). This leaves implementers unable to prove that the attempted command did not run while keeping the original operation and focus usable. *Fix:* add a `Blocked second submit` row with trigger evidence, disabled/blocked action behavior, retained in-flight lifecycle, stable focus destination, localized explanation, one announcement per blocked attempt, recovery when the original operation leaves flight, and non-terminal classification for the original operation.

### Medium

1. **Global shortcuts have no editable-control, IME, conflict, or discoverability policy.** `Ctrl+K`, `Ctrl+,`, and `/` are assigned, but only `/` is conditional (`EXPERIENCE.md:103, 223-230`). The contract does not prevent `/` or other shortcuts from hijacking text entry/composition, say how users discover them, or define a visible fallback when browser/OS/assistive-technology bindings win. *Fix:* scope handlers outside editable controls and active composition, avoid overriding platform/AT commands, expose shortcuts through accessible help/tooltips and `aria-keyshortcuts` where appropriate, and keep every destination reachable through ordinary controls.

2. **Grid focus recovery during filtering, sorting, refresh, and virtualization is unspecified.** Keyboard operations are promised, and the special filter-hidden expanded-row path is defined, but ordinary focused rows/cells may disappear or be recycled during filtering, sorting, paging, server virtualization, or live refresh (`EXPERIENCE.md:105, 143, 150-167, 229`). Column priority is named without explicitly preserving keyboard access to omitted lower-priority data at narrow widths (`EXPERIENCE.md:105, 246-268`). *Fix:* define the focus anchor and fallback for every grid mutation, prevent virtualized focus from moving to detached content, and require all hidden/omitted cell data and actions to remain available through an accessible detail/column mechanism.

3. **Tenant/auth scope changes announce blocking state but do not define focus transfer.** Missing/stale tenant clears prior content and announces once; no-access has a heading/alert target, and sign-out returns to a stable route (`EXPERIENCE.md:127-145, 244, 253-260, 295-318`). The tenant-change case does not say where focus moves when its previously focused grid/form node is removed, and the generic rule against removed focus is not an executable outcome. *Fix:* give every tenant/auth replacement state a persistent labelled heading/alert target, move focus there when the active node is removed, preserve focus when it remains valid, and define focus after sign-in/sign-out failure and retry.

4. **The exact 32px compact row metric is not reconciled with text-spacing and reflow.** Compact rows are required to be exactly 32px while the contract also requires no loss under text-spacing overrides, 320 CSS-pixel reflow, 400% zoom, and 24×24 targets (`DESIGN.md:31-34, 94-100, 130, 150-152`; `EXPERIENCE.md:246-250`). A fixed-height row can clip or overlap wrapped/expanded text and target content. *Fix:* state that 32px is the normal compact minimum/metric, not a clipping ceiling under accessibility overrides, or define an equally operable overflow/detail strategy with tests proving no content or action is lost.

5. **Live-region terminal behavior remains ambiguous for Warning and the polling ceiling.** Most transitions have useful announcement rules, but `Warning` may be terminal or non-terminal “as supplied,” and `Degraded` defines behavior only until the 120-second ceiling without naming the post-ceiling state/message (`EXPERIENCE.md:175-187, 234-245`). This prevents deterministic deduplication and leaves screen-reader users without a committed final status. *Fix:* name the evidence discriminator that classifies Warning, and define the state, action, focus, and one-time announcement when Degraded polling reaches its ceiling.

6. **Custom forced-colors/contrast fallbacks remain an open visual decision.** Global behavior is strong, but the configurable accent and custom fresh-row marker still lack an approved exact forced-colors treatment, and the Fluent pin/token API is unresolved (`DESIGN.md:75-84, 139, 154-158`; `EXPERIENCE.md:250, 353-358`). This is especially relevant where FrontComposer—not Fluent—owns the visual delta. *Fix:* approve a compact contrast/forced-colors matrix for navigation current state, focus, status, projection health, lifecycle, and fresh-row indicators; name which cues survive (`current`, border, icon, text, shape) and bind them to the selected Fluent V5 catalog.

### Low

1. **Busy semantics do not identify their owner or clearing rule.** The skeleton “is marked busy,” but the contract does not say whether `aria-busy` belongs on the replaced content region/grid, whether skeleton descendants are hidden from the accessibility tree, or exactly when busy clears (`EXPERIENCE.md:106, 123-126, 154-158`). *Fix:* place busy state on the stable labelled result region, keep decorative skeleton shapes hidden, and clear it atomically when Empty, Data, or Error becomes available.

## Journey accessibility and testability

- **UJ-1:** testable bootstrap, shell, home, and failure outcomes; its human-UI accessibility relies on the global shell contract rather than a journey-level keyboard/landmark assertion (`EXPERIENCE.md:284-293`).
- **UJ-2:** testable navigation, flyout, grid, detail, projection-health, freshness, and tenant-failure behavior; the navigation/flyout and grid-focus findings above must be resolved (`EXPERIENCE.md:295-306`).
- **UJ-3:** testable authorization, validation, dialog, submit, lifecycle, and rejection behavior; the dialog, form-relationship, and blocked-submit findings above must be resolved (`EXPERIENCE.md:308-318`).
- **UJ-4:** testable as a nonvisual protocol journey. WCAG web criteria do not apply to the agent, but host-authentication and disclosure failures remain deterministic and support-safe (`EXPERIENCE.md:320-329`).
- **UJ-5:** testable as CLI/generator behavior. The contract supplies deterministic outputs and failure paths; terminal/screen-reader compatibility is outside the declared web-UI surface (`EXPERIENCE.md:331-340`).
- **UJ-6:** explicitly testable and names the broadest accessibility evidence set, but it cannot close the four high findings until the expected outcomes above are committed (`EXPERIENCE.md:342-351`).

## Verification gaps

- This review validates the downstream contract only. No rendered DOM, computed styles, screen-reader runs, browser zoom/reflow captures, forced-colors screenshots, keyboard traces, or bUnit/e2e results are present in this UX workspace.
- The draft itself requires the selected Fluent UI V5 catalog pin and exact token/API names to be reconciled; inherited dialog, tab, grid, tooltip, and forced-colors behavior therefore cannot yet be verified (`EXPERIENCE.md:357-358`; `DESIGN.md:84, 158`).
- Final microcopy is still open for several blocking or consequential states, so announcement clarity and error-identification quality cannot be content-tested yet (`EXPERIENCE.md:87, 353-358`; `DESIGN.md:154-158`).
- The six journeys expose testable product behavior, but only UJ-2, UJ-3, and UJ-6 directly exercise the principal human accessibility paths. Add UJ-1 shell/skip-link/heading evidence to the acceptance set; treat UJ-4 and UJ-5 under protocol/CLI testing rather than claiming WCAG coverage.
