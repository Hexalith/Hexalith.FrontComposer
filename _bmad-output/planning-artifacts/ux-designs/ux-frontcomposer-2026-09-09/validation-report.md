# Validation Report — frontcomposer

- **DESIGN.md:** `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/DESIGN.md`
- **EXPERIENCE.md:** `_bmad-output/planning-artifacts/ux-designs/ux-frontcomposer-2026-09-09/EXPERIENCE.md`
- **Run at:** 2026-09-09T13:58:07+02:00

> **Source-change notice:** `_bmad-output/planning-artifacts/ux-design.md` was rewritten concurrently after these reviews completed. This report remains the validation snapshot of the peer-spine draft at the stated time; source reconciliation and a post-update validation pass are required before final status.

## Overall verdict

The migrated pair is substantially stronger than the legacy three-file contract: every canonical UJ has a complete flow, component names join across both spines, token references resolve, and the required spine shapes are present. It is not yet handoff-ready because consequential command and accessibility states remain under-specified, the declared sources retain the old authority model, and the Fluent V5 inheritance boundary is not approved.

The specialist lenses found no critical issue, but they sharpened the handoff blockers: deterministic dialog/navigation/form focus, blocked-submit and polling-ceiling outcomes, caller/tenant isolation for cross-request MCP lifecycle state, equivalent user/auth scope-loss behavior, and explicit security evidence. The current repository pin resolves the named Fluent APIs, but Product/Architecture has not accepted that RC pin or the runtime accent-role mapping.

## Category verdicts

- Flow coverage — strong
- Token completeness — adequate
- Component coverage — strong
- State coverage — thin
- Visual reference coverage — strong
- Bloat & overspecification — strong
- Inheritance discipline — broken
- Shape fit — strong

## Findings by severity

### Critical (0)

None.

### High (13)

1. **[Rubric · State coverage] Command state matrix is not closed** (`EXPERIENCE.md:113,144,175-187,233-234`). Blocked second submit lacks a state row; Warning terminality is delegated; post-`120,000ms` Degraded behavior is undefined. **Fix:** define entry evidence, visible meaning, focus/announcement, recovery, and classification for each.
2. **[Rubric · Inheritance] Source-of-record conflict** (`DESIGN.md:63`; `EXPERIENCE.md:18`; `prd.md:16-18,675-683`; `prd-addendum-2026-09-08.md:8-10,102-108`). The pair says it supersedes the legacy chain while its canonical sources retain that chain. **Fix:** propagate the approved peer-spine authority into the PRD/addendum or preserve the old authority model.
3. **[Rubric · Inheritance] Fluent boundary remains unapproved** (`DESIGN.md:75-84,154-158`; `EXPERIENCE.md:353-358`). Exact package/catalog identity, accent role, and inherited state treatments are unresolved. **Fix:** approve the catalog identity and exact roles/components, then replace nearest-role placeholders.
4. **[Accessibility] Dialog and palette focus entry/naming are non-deterministic** (`EXPERIENCE.md:99-100,112,225-245`; `DESIGN.md:124-125,137`). **Fix:** define invoker, initial focus, containment, accessible name/description source, submit/error focus, and return focus for each dialog/palette.
5. **[Accessibility] Navigation and flyout semantics omit confirmed requirements** (`DESIGN.md:121`; `EXPERIENCE.md:96,139,223-229`). **Fix:** require programmatic current-route state, named icon-only entries with keyboard-accessible tooltip, and the exact menu/popover keyboard/focus model.
6. **[Accessibility] Generated-form relationships are incomplete** (`EXPERIENCE.md:110,144,169-173,242-245`; `DESIGN.md:135`). **Fix:** require visible labels, named/described groups, per-control invalid/error associations, stable summary targets, declared order, and deterministic summary/first-invalid focus.
7. **[Accessibility] Blocked second submit lacks acceptance behavior** (`EXPERIENCE.md:113,134-148,169-187,233-245`). **Fix:** add trigger, retained original lifecycle, stable focus, localized explanation, per-attempt deduplication, recovery, and classification.
8. **[Fluent V5] Package contract is uncommitted** (`DESIGN.md:69,158`; `EXPERIENCE.md:24,357`). Repository state resolves `Microsoft.FluentUI.AspNetCore.Components` at `5.0.0-rc.5-26219.1`, but D-13/OI-4 has not accepted it. **Fix:** record the approved package/catalog identity and rerun API/token checks.
9. **[Fluent V5] `accent-thread` conflates a default seed with a runtime semantic role** (`DESIGN.md:15-16,36-42,56-58,71-84`; `EXPERIENCE.md:24`). **Fix:** separate the default-only hex from the approved theme-derived runtime role; prevent mechanical consumers from rendering the default directly.
10. **[Security/Tenancy] Known MCP disclosure/enforcement gaps are not finalization gates** (`EXPERIENCE.md:197-208,253-260,320-329,353-358`). **Fix:** gate final status on the independent disclosure/oracle disposition plus endpoint-authentication and non-Development allow-all enforcement evidence; separate accepted residuals from open gaps.
11. **[Security/Tenancy] Cross-request lifecycle state lacks caller/tenant binding** (`EXPERIENCE.md:210,253-260,325-329`). **Fix:** bind entries to authenticated caller, tenant, and command identity; re-admit every poll; define expiry/scope-change behavior; add two-caller/two-tenant negatives.
12. **[Security/Tenancy] User/auth scope loss is weaker than tenant loss** (`EXPERIENCE.md:26-28,97,127-142,253-260,295-318`). **Fix:** define one fail-closed transition sequence for stale/missing tenant, user, or authentication context, including clearing rendered/cached state before replacement and validated re-entry.
13. **[Security/Tenancy] UJ-6 does not prove UJ-2/UJ-3/UJ-4 security promises** (`EXPERIENCE.md:342-351`). **Fix:** require named shell and MCP/governance evidence lanes covering cross-scope transitions, authorization revocation, controlled-field tampering, disclosure equality/oracles, lifecycle isolation, endpoint auth, and production allow-all bans.

### Medium (15)

1. **[Rubric · Token completeness] `rounded.fluent-default` is not a scalar CSS dimension** (`DESIGN.md:26-29,108-112`). **Fix:** remove it and state Fluent radius inheritance in prose, or use an approved scalar.
2. **[Rubric · State coverage] Filtered no-results is missing from the projection matrix** (`EXPERIENCE.md:80-82,106,150-165`). **Fix:** add distinct entry evidence, reset/recovery, announcement, and classification.
3. **[Accessibility] Shortcut policy omits editable controls, IME, conflicts, and discoverability** (`EXPERIENCE.md:103,223-230`). **Fix:** scope handlers outside editing/composition, preserve platform/AT bindings, expose accessible help and normal-control alternatives.
4. **[Accessibility] Grid focus recovery is unspecified across filtering, sorting, refresh, paging, and virtualization** (`EXPERIENCE.md:105,143,150-167,229,246-268`). **Fix:** define focus anchors/fallbacks and accessible access to omitted lower-priority data.
5. **[Accessibility] Tenant/auth replacement lacks executable focus transfer** (`EXPERIENCE.md:127-145,244,253-260,295-318`). **Fix:** provide a persistent labelled heading/alert target and deterministic focus for removed versus still-valid nodes.
6. **[Accessibility] Exact 32px rows conflict with text spacing/reflow unless treated as a minimum metric** (`DESIGN.md:31-34,94-100,130,150-152`; `EXPERIENCE.md:246-250`). **Fix:** make 32px the normal compact minimum or prove an equally operable overflow/detail strategy.
7. **[Accessibility] Warning and the polling ceiling lack deterministic terminal/live-region behavior** (`EXPERIENCE.md:175-187,234-245`). **Fix:** name the Warning discriminator and define post-ceiling state, action, focus, and one-time announcement.
8. **[Accessibility] Forced-colors and contrast fallbacks remain open for FrontComposer-owned deltas** (`DESIGN.md:75-84,139,154-158`; `EXPERIENCE.md:250,353-358`). **Fix:** approve a matrix for current navigation, focus, status, health, lifecycle, and fresh-row cues against the chosen Fluent catalog.
9. **[Fluent V5] `rounded.fluent-default` violates the DESIGN.md schema** (`DESIGN.md:26-29,108-110`). **Fix:** remove the token and retain prose/component inheritance only.
10. **[Fluent V5] Typography aliases do not name governed FrontComposer mappings** (`DESIGN.md:17-25,86-90,126`). **Fix:** map aliases to exact `Typography.*` roles or exact component-owned Fluent parameters.
11. **[Security/Tenancy] UJ-3 lacks denial/revocation failure paths** (`EXPERIENCE.md:308-318`). **Fix:** cover initial/direct-route denial and policy loss or mutation before dispatch, with no protected disclosure or side effect.
12. **[Security/Tenancy] Controlled-field hostile input behavior is ambiguous** (`EXPERIENCE.md:110,253-258,320-326`). **Fix:** omit controlled fields from public schemas, reject supplied copies before side effects, and test each controlled field.
13. **[Security/Tenancy] Storage scope lacks canonicalization/collision rules** (`EXPERIENCE.md:100,132,141,255-256`). **Fix:** require one canonical scope-key builder and cross-user/tenant/feature normalization tests.
14. **[Security/Tenancy] MCP endpoint-auth and skill-resource gate boundaries are ambiguous** (`EXPERIENCE.md:28,201,208,253-260,320-324`). **Fix:** distinguish endpoint authentication from defense-in-depth handler behavior and document the narrow skill-resource exception.
15. **[Security/Tenancy] Fresh-row adversarial scope transitions are missing from journeys/tests** (`EXPERIENCE.md:189-195,295-306,342-348`). **Fix:** add active-marker tenant/user switching plus already-rendered DOM invalidation, concurrent transitions, old-scope reads, dismissal/expiry, and first-wins provenance tests.

### Low (3)

1. **[Accessibility] Busy semantics lack an owner and clearing rule** (`EXPERIENCE.md:106,123-126,154-158`). **Fix:** place `aria-busy` on the stable labelled results region, hide decorative skeleton descendants, and clear atomically on Empty/Data/Error.
2. **[Security/Tenancy] MCP timing side channels are not declared out of scope** (`EXPERIENCE.md:197-210,329`). **Fix:** state the timing non-goal beside the response-shape guarantees.
3. **[Security/Tenancy] Security-consequential microcopy remains unresolved** (`DESIGN.md:154-158`; `EXPERIENCE.md:87,353-358`). **Fix:** approve localized message keys, safe fields, codes, and recovery actions, then test interpolation redaction.

## Reviewer files

- `review-rubric.md`
- `review-accessibility.md`
- `review-fluent-ui-v5.md`
- `review-security-tenancy.md`
