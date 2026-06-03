# Test Automation Summary — Story 1.4 (FC-L10N shell-string ownership)

**Date:** 2026-06-03
**Workflow:** bmad-qa-generate-e2e-tests
**Role:** QA automation engineer (tests only — no code review / story validation)
**Feature under test:** FC-L10N shell-string-ownership ready-gate (Story 1.4, ACs 1–3)

## Framework detected

xUnit **v3** + **Shouldly** + **bUnit** + **NSubstitute** (the project's existing Blazor
component-test stack). No new framework introduced. Tests run solution-level with
`DiffEngine_Disabled=true` and the standard trait filter, per project-context testing rules.

## Coverage analysis (existing vs. gaps)

The story shipped a consolidated `Story14ShellStringOwnershipTests` with **2** tests:
- FR culture-render of header chrome (palette / settings / theme) resolves to FR resx values, not EN.
- EN culture-render converse (proves culture-sensitivity, not literals frozen to one locale).

QA pass found **3 gaps** and auto-applied them:

| # | Gap | AC | Resolution |
|---|---|---|---|
| 1 | **Swap-seam untested.** The contract names `services.Replace(IStringLocalizer<FcShellResources>)` as *the* whitelabel/DB-backed extensibility seam, but no test proved the shell actually renders chrome from a replaced localizer. | AC1 | Added `FrontComposerShell_WhenLocalizerReplaced_ResolvesChromeFromTheSwappedLocalizer` — swaps in a `SentinelLocalizer` (returns `SWAP::{key}`) and asserts palette / settings / nav accessible-names source from it. |
| 2 | **AC2 chrome breadth.** The "no hard-coded English chrome remains" guard only covered header controls. Skip-links (`SkipToContentLabel`, `SkipToNavigationLabel`) and the nav landmark (`NavMenuAriaLabel`) are FR≠EN chrome rendered at the shell frame but unasserted. | AC2 | Added `FrontComposerShell_UnderFrenchCulture_ResolvesSkipLinkAndNavLandmarkChromeToFrenchResxValues` — asserts both skip-link texts + the nav landmark aria-label resolve FR, not EN. (Footer copyright is identical EN/FR by design, correctly excluded.) |
| 3 | **Contract doc defect.** Stray `</content>` / `</invoke>` authoring-sentinel lines had leaked into the end of `fc-l10n-shell-string-ownership-2026-06-03.md`. | — | Removed the two trailing sentinel lines. |

## Generated tests

### E2E / component (bUnit) tests
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs`
  - `FrontComposerShell_UnderFrenchCulture_ResolvesChromeAccessibleNamesToFrenchResxValues` (pre-existing)
  - `FrontComposerShell_UnderEnglishCulture_ResolvesChromeAccessibleNamesToEnglishResxValues` (pre-existing)
  - `FrontComposerShell_UnderFrenchCulture_ResolvesSkipLinkAndNavLandmarkChromeToFrenchResxValues` **(new — gap #2)**
  - `FrontComposerShell_WhenLocalizerReplaced_ResolvesChromeFromTheSwappedLocalizer` **(new — gap #1)**

No API tests applicable — Story 1.4 is a Blazor shell-frame localization ready-gate with no HTTP/service
endpoints. The standing EN↔FR parity / round-trip / NBSP / placeholder evidence (`FcShellResourcesTests`,
`FcShellOptionsValidationTests`) is referenced, not duplicated.

## Coverage

- AC1 (ownership boundary documented + mechanism named, incl. swap seam): mechanism now **executably
  pinned** by the swap-seam test (was doc-only).
- AC2 (non-default-culture chrome render, no hard-coded English): chrome categories asserted under `fr`
  grew from 3 (palette/settings/theme) to **6** (+ skip-to-content, skip-to-nav, nav landmark).
- AC3 (confirm/escalate): doc-level; contract `status: escalated` with named owner — unchanged.

## Validation

- `dotnet build -c Release Hexalith.FrontComposer.slnx` → **0 warnings, 0 errors** (TWAE).
- `Story14ShellStringOwnershipTests` → **4 passed, 0 failed**.
- Must-stay-green suites (`FcShellResourcesTests`, `FcShellOptionsValidationTests`,
  `FrontComposerShellParameterSurfaceTests` [locked 7-parameter surface, untouched],
  `Story12PageLayoutTests`, `Story13AccessibilityPrimitivesTests`) → **138 passed, 0 failed** combined.

## Checklist (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] E2E tests generated (UI exists) · [x] standard framework APIs · [x] happy path · [x] critical
  cases (FR render + EN converse + swap-seam)
- [x] All generated tests run successfully · [x] semantic/accessible locators (`data-testid`, ARIA,
  semantic CSS) · [x] clear three-part names · [x] no hardcoded waits (`WaitForAssertion`) · [x]
  independent (culture restored in `finally`)
- [x] Summary created · [x] tests in the standard test dir · [x] coverage metrics included
- N/A: API tests (no endpoints in this feature)

## Next steps

- Run the full filtered lane in CI; the documented Story 1.1–1.3 pre-existing-failure baseline
  (13 failures) is unaffected — this change adds only test + doc artifacts, no `src/` change.
- Add more cultures (e.g. an RTL `ar` satellite) → the swap-seam + culture-render gates already
  generalize to them.
