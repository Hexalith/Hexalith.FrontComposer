# Test Automation Summary — Story 1.2 (FC-LYT page-layout contract)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-03
**Story:** `1-2-confirm-and-apply-the-fc-lyt-page-layout-contract.md`
**Contract:** `_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`

## Scope

Story 1.2 confirms + applies the **FC-LYT** contract: `FullWidth` (default, edge-to-edge) vs.
`Constrained` (opt-in readable max-measure) on `FrontComposerShell`'s `#fc-main-content`, driven by a
cascaded `FcPageLayoutCoordinator` + a page-dropped `FcPageLayout` component.

**Framework detected:** xUnit v3 + **bUnit** 2.7.2 + Shouldly (the project's established Blazor
component-test stack). This is a signed-NuGet **library** with no HTTP API surface, so "E2E" here =
bUnit render-and-query component tests (house style), not Playwright. The separate Playwright
`tests/e2e` a11y/visual workspace is out of scope for this feature's logic coverage.

## Generated Tests

All tests live in
`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story12PageLayoutTests.cs`
(extended in place — the story's designated test file).

### Pre-existing (3 — happy path, retained)
- [x] `FrontComposerShell_WithNoPageLayout_RendersFullWidthContentContainer`
- [x] `FrontComposerShell_WithConstrainedPageLayout_RendersConstrainedContentContainer`
- [x] `FcPageLayout_WhenDeclaredAndDisposed_FlipsThenResetsCoordinatorMode`

### Added by this QA sweep (10 — gap coverage)
- [x] `FcPageLayoutMode_Default_IsFullWidthZeroValue` — the zero-regression keystone (`default == FullWidth`, value `0`).
- [x] `FcPageLayoutCoordinator_SetSameMode_DoesNotRaiseChanged` — the render-re-entry guard (no-op on unchanged mode).
- [x] `FcPageLayoutCoordinator_SetDifferentMode_RaisesChangedOnce` — single `Changed` event on a real transition.
- [x] `FcPageLayoutCoordinator_ResetFromConstrained_RaisesChangedAndRestoresFullWidth` — reset path fires + restores default.
- [x] `FcPageLayoutCoordinator_ResetWhenAlreadyFullWidth_IsNoOp` — idempotent reset, no spurious event.
- [x] `FcPageLayout_RendersChildContentVerbatim_WithNoWrapperMarkup` — component adds no DOM of its own.
- [x] `FcPageLayout_WithNoCascadedCoordinator_IsInertAndDisposeDoesNotThrow` — shell-less edge case, safe dispose.
- [x] `FrontComposerShell_WithDefaultPageLayout_KeepsFullWidthContentContainer` — bare `<FcPageLayout>` is a no-op.
- [x] `FrontComposerShell_WhenConstrainedPageRemoved_RestoresFullWidth` — **round-trip**: leaving a constrained page restores edge-to-edge.
- [x] `FrontComposerShell_WhenLayoutAnnotated_PreservesMainContentSkipLinkTarget` — `#fc-main-content` `id` + `tabindex="-1"` regression guard.

## Coverage

| Surface | Before | After |
|---|---|---|
| `FcPageLayoutMode` enum (default/zero invariant) | 0 | 1 |
| `FcPageLayoutCoordinator` (event / no-op contract) | 0 | 4 |
| `FcPageLayout` component (verbatim render, shell-less, default no-op) | partial (lifecycle only) | 3 + lifecycle |
| `FrontComposerShell` `#fc-main-content` annotation (both modes + round-trip + skip-link) | 2 | 5 |
| **Total Story 1.2 tests** | **3** | **13** |

- **Acceptance criteria:** AC1 (applied measure — both modes assert container attr/class) and AC3
  (both modes expose `data-fc-page-layout` + class, plus the round-trip) are fully pinned. AC2 is a
  documentation/confirmation criterion (contract doc + Product/UX escalation + FC-DOC linkage) — not
  automatable; verified by inspection of the contract doc.
- **Not coverable in bUnit (documented limits):** the actual `max-inline-size` / `margin-inline`
  *computed* centering is a CSS-render concern (no layout engine in bUnit) — the constrained **class
  presence** is the proxy; `Padding.All3` on the `FluentLayoutItem` is FluentUI-internal.

## Results

- `dotnet test … --filter Story12PageLayoutTests` → **13 passed, 0 failed**.
- Adjacent regression lane (`Components.Layout` + Story 1.0 spike — incl.
  `FrontComposerShellTests`, `FrontComposerShellParameterSurfaceTests`, `FcHamburgerToggleTests`) →
  **119 passed, 0 failed**. Locked 7-parameter surface stays green (no shell `[Parameter]` added).
- Release build clean under `TreatWarningsAsErrors=true` (0 warnings).
- Run with `DiffEngine_Disabled=true` per project testing rules.

## Next Steps

- Run in CI via the standard solution lane
  (`--filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`).
- Optional future: a Playwright `tests/e2e` visual check that a `Constrained` page is actually
  centred at the themeable `--fc-page-max-inline-size` cap (covers the CSS-render gap bUnit can't).
