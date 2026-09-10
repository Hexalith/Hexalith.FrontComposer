# Fluent UI Blazor V5 And Source-Integrity Review — OI-16 Pass 2

## Overall verdict

**Changes required.** The D-8 chain now has strong Fluent UI Blazor V5 inheritance discipline,
complete supplement component parity, coherent route/IA rules, and testable forced-colors and
reduced-motion contracts. It is not yet clean because three High-severity source-integrity defects
remain: current public component ownership/delivery is misstated, the settings-state contract promises
actions the public component intentionally does not have, and the OI-16 trail names a review artifact
that does not exist. One Medium delivery-status ambiguity also remains.

This review does not close OI-16, SM-6, OI-19, G-4, or D-9 and is not Product approval. Explicitly
open implementation and evidence work was treated as status, not as a defect, unless the chain marked
the behavior delivered, contradicted the public source, or left ownership ambiguous.

## Severity counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 1 |
| Low | 0 |

**Unresolved Critical/High count: 3** (0 Critical, 3 High).

## Evidence basis

- Exact reviewed revisions:
  - `ux-design.md`: SHA-256 `1167a6927d2fba38ee931e55ee0a996a18e490f42a46ce0ffa088fda9226f97c`
  - `ux-design-detailed-2026-07-05.md`: SHA-256 `4bb720364b69370d001a6ecdc151428813ea51bd05a47d1fcb06e538c7bc115c`
  - `ux-experience-2026-07-05.md`: SHA-256 `da74b2a1cb22e9ea3854a86e90208116f4e467315789e01deacafd8079ec0471`
- Product authority: `prd.md`, especially FR-8, Table A/Table B, D-8/D-9, OI-16/OI-19, and
  SM-6; and `prd-addendum-2026-09-08.md` §4.
- FrontComposer public source and published reference pages under
  `src/Hexalith.FrontComposer.Shell/Components`, `src/Hexalith.FrontComposer.Shell/Shortcuts`, and
  `docs/reference/components`.
- Selected Fluent pin `Microsoft.FluentUI.AspNetCore.Components` version
  `5.0.0-rc.5-26219.1` from `references/Hexalith.Builds/Props/Directory.Packages.props`; the installed
  assembly documentation and assets resolve `FluentAccordion`, `FluentBadge`, `FluentTooltip`, and
  the inherited Fluent 2 semantic tokens used by the visual supplement.
- All frontmatter source paths in the three UX files resolve. All 15 canonical FrontComposer
  component identifiers resolve to public repository types. The two supplement component registries
  match exactly, including the three explicitly inherited Fluent types. No `FluentToolbar`,
  `FluentSearch`, dead 2026-07-05 PRD path, product-owned hex palette, or legacy Fluent v4/FAST token
  is promised by the chain.

## Strengths

- D-8 authority order is explicit and FC-IA-1 is retained only as reconciliation history
  (`ux-design.md:20-39`; `ux-experience-2026-07-05.md:322-327`). Routes, default Module Tab behavior,
  projection flyouts, and command routes agree with the PRD.
- `FcPageToolbar` is correctly named as the public FrontComposer contract, while its current Fluent
  composition remains implementation-owned. The chain does not invent `FluentToolbar` or
  `FluentSearch` as public dependencies (`ux-design.md:74-83`;
  `ux-design-detailed-2026-07-05.md:133-145,184`).
- `FcPageTabs` and `FcPageTab` are the correct public body-tab types, and the visual contract correctly
  leaves roles, roving focus, selection visuals, and tab/panel mechanics to inherited Fluent behavior
  (`ux-design-detailed-2026-07-05.md:182-184`; `docs/reference/components/page-tabs.md:13-18`).
- The visual supplement inherits Fluent V5 semantic colors and component radii instead of defining a
  competing theme. `--fc-color-accent` is constrained as an alias rather than a separate product
  palette; any implementation/CSS convergence and proof is correctly left in the open NFR-3/SM-6
  implementation/evidence lane (`ux-design-detailed-2026-07-05.md:21-57,93-115`;
  `ux-design.md:305`).
- UX-VC-1 and UX-RM-1 retain meaning through text, icon/shape, system colors, DOM state, and focus when
  authored color or motion is unavailable. The five WCAG 2.2 SC 2.5.8 exceptions are now separately
  named and measurable (`ux-design-detailed-2026-07-05.md:100-115,147-163,201-210`;
  `ux-design.md:287`).
- Product approval remains `pending-reapproval`; OI-16, OI-19, SM-6, and G-4 remain explicitly open
  (`ux-design.md:1-28,309-321`; both supplement frontmatters and introductions).

## Findings by severity

### Critical

None.

### High

#### H-01 — Public component ownership and delivered-baseline wording do not match current source

**Evidence/location.** The behavior registry says `FrontComposerShell` always renders account access
and navigation, assigns the always-visible hamburger to `FrontComposerNavigation`, says `FcPageTabs`
owns the active ID, and assigns `/` page-search focus to `FcPageToolbar`
(`ux-experience-2026-07-05.md:82-86`). Current public source differs:

- `FrontComposerShell.ShowAccountMenu` is a public opt-out and the account menu is conditional
  (`FrontComposerShell.razor.cs:206`; `FrontComposerShell.razor:108`).
- The default `FcHamburgerToggle` is rendered by `FrontComposerShell` through its replaceable
  `HeaderStart` slot, while the navigation region is conditional
  (`FrontComposerShell.razor:66,118`; `docs/reference/components/front-composer-shell.md:63-76`).
- `ActiveTabId`/`ActiveTabIdChanged` are caller-owned
  (`docs/reference/components/page-tabs.md:67`; `FcPageTabs.razor.cs:19-23`).
- The registered `/` shortcut currently calls `FocusFirstColumnFilterAsync` for the active DataGrid;
  `FcPageToolbar` exposes search value/input parameters but no shortcut registration or focus contract
  (`FrontComposerShortcutRegistrar.cs:103-109,214-225`; `FcPageToolbar.razor.cs:24-38`).

The PRD target may validly require always-on account/hamburger access and optional page-search focus,
but the canonical ledger retains shell shortcuts/account/navigation as delivered and lists only
route/reflow fixes as the FR-8 implementation delta (`ux-design.md:296`). The chain therefore neither
describes the delivered public baseline accurately nor records the required convergence as open work.

**Remediation.** Keep the FR-8 target, but state the current public baseline precisely and add the
always-on account/hamburger behavior plus enabled page-search registration/focus to FR-8's open
implementation column. Attribute the hamburger/default slot to `FrontComposerShell`, describe
`FrontComposerNavigation` as consuming shell navigation state, and change the tabs row to say it
forwards caller-owned active state to Fluent tabs. Do not expose the toolbar's internal Fluent
composition as public API.

#### H-02 — SS-43 promises Apply/Cancel actions that `FcSettingsDialog` intentionally omits

**Evidence/location.** Canonical SS-43 lists `Edit/apply/cancel/close` and `Apply or close`, labels the
state a delivered baseline, and leaves only entry/return evidence open (`ux-design.md:266`). The public
settings contract applies changes live and explicitly has no Apply or Cancel step; it provides Restore
defaults and Done (`docs/reference/components/settings.md:13-20`;
`FcSettingsDialog.razor.cs:17-24`). The experience component row no longer promises Apply/Cancel, so
the canonical row also conflicts with its supplement (`ux-experience-2026-07-05.md:88`).

**Remediation.** Replace SS-43's actions with live preference editing, Restore defaults, Done/close,
and any actual error recovery. Preserve UX-OF-1 entry/containment and FM-05 return behavior without
inventing Apply/Cancel controls.

#### H-03 — The OI-16 evidence trail contains an unresolved review path

**Evidence/location.** UX-OI16-1 names
`review-fluent-ui-v5-oi-16-2026-09-09.md` as one of three required clean follow-up reports
(`ux-design.md:316`), but that file does not exist. The rubric and accessibility siblings exist; this
review is deliberately written to the separately requested `-pass2.md` path. Thus the canonical trail
still contains a non-resolving artifact reference after this pass.

**Remediation.** Reconcile UX-OI16-1 to the actual accepted Fluent/source-integrity review filename,
or create the exact named artifact and ensure the validation report records the same file and digest.
Do not describe the report as clean while any Critical/High finding remains.

### Medium

#### M-01 — “All six are delivered baseline journeys” is broader than the file's own gate status

**Evidence/location.** The Key Flows introduction calls all six journeys delivered baseline
(`ux-experience-2026-07-05.md:231-232`), but UJ-4 explicitly leaves G-7 security acceptance incomplete,
UJ-5 leaves OI-19 open, and UJ-6 leaves SM-6 evidence open
(`ux-experience-2026-07-05.md:289,302,316`). The later caveats preserve gate integrity, but the blanket
opening sentence makes delivery status ambiguous.

**Remediation.** Say that all six stable journey definitions and their previously delivered runtime
baselines are retained, then name the open UJ-4/G-7, UJ-5/OI-19, and UJ-6/OI-16/SM-6 work. Do not imply
that those gates or the full end-to-end journeys are delivered.

### Low

None.

## Summary

The Fluent V5/component-first, semantic-token, no-theme-redefinition, route/IA, forced-colors,
reduced-motion, component-name, and supplement-parity checks pass. Source ownership/status and the
OI-16 evidence trail do not. The pass-2 result is **0 Critical, 3 High, 1 Medium, 0 Low**, with
**3 unresolved Critical/High findings**. OI-16, SM-6, OI-19, G-4, D-9, and Product approval remain
open.
