# Test Automation Summary — Story 1.3 (FC-A11Y accessibility primitives)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-03 · **Author:** Administrator (QA automation)
**Scope:** the shell's accessibility primitives (skip links, focus visibility, `aria-label`/`role`/`aria-live` patterns, keyboard reachability) as a reusable, testable ready-gate.

> Role discipline: this run generates/augments **tests only** — no story validation or code review
> (that's `/bmad-code-review`). No `src/` was modified; no `[Parameter]` was added to
> `FrontComposerShell`; the locked 7-parameter surface and the frozen e2e specimen lane (AC3) were
> left untouched.

## Test Framework (detected — used the project's existing stack)

| Layer | Framework | Where |
|---|---|---|
| Component / shell-frame (Layer 1) | **xUnit v3 + bUnit + Shouldly** (`DiffEngine_Disabled=true`) | `tests/Hexalith.FrontComposer.Shell.Tests` |
| End-to-end / axe (Layer 3) | **Playwright + `@axe-core/playwright`** (WCAG 2.1 AA, serious∪critical gate) | `tests/e2e` |

bUnit cannot DOM-walk FluentUI v5 shadow DOM, so the markup-level ARIA contract is asserted in
bUnit and real `axe.run()` lives in the Playwright lane — by design (see `AxeCoreA11yTests`).

## Generated / Augmented Tests

### Component (bUnit) — `Story13AccessibilityPrimitivesTests.cs`

Pre-existing (dev-story, 4 tests) — left intact:

- [x] `…_SkipLinkResolvesToMainContentFocusTarget` — `a.fc-skip-link[href="#fc-main-content"]` → `#fc-main-content[tabindex="-1"]`
- [x] `…_SkipToNavLinkResolvesToNavFocusTarget` — `href="#fc-nav"` → `#fc-nav[tabindex="-1"]` (has-navigation)
- [x] `…_HeaderChromeControlsExposeAccessibleNames` — palette / settings / theme localized accessible names
- [x] `…_ScopedCss_SuppressesNoFocusIndicator` — zero-override focus invariant (no `outline:none`/`box-shadow:none` without `:focus-visible`)

**Gaps discovered and auto-applied this run (+2 tests):**

- [x] **`…_NavigationLandmarkExposesAccessibleName`** — *Gap:* AC1 requires **every** interactive
  shell-chrome element to carry an accessible name, but the suite stopped at palette/settings/theme.
  The auto-populated navigation landmark (`fc-navigation-full`, `NavMenuAriaLabel`) — the content of
  the `#fc-nav` skip target — was unasserted. Now pinned against the **localized** value (FC-L10N-safe).
- [x] **`…_DensityAnnouncerExposesPoliteLiveRegion`** — *Gap:* primitive #3 (the `aria-live`
  politeness ladder) is named in the FC-A11Y contract but was pinned only in the **isolated**
  `FcDensityAnnouncerTests`; the "single testable ready-gate" never confirmed the **shell frame
  itself** mounts the polite live region. Now asserts the frame renders `role="status"` /
  `aria-live="polite"` / `aria-atomic="true"` / `.fc-sr-only`.

### E2E (Playwright axe lane) — no changes

AC3 is a *confirm-green*, **not** a *change* — specs/helpers/manifest/baselines were not touched.
The lane (`specs/specimen-accessibility.spec.ts` + `helpers/a11y.ts`) already covers, per route:
blocking axe gate (WCAG 2.1 AA), keyboard flow without traps, focus-visible over lifecycle visuals,
forced-colors + reduced-motion, zoom/reflow at 100/200/400%, and fail-closed route exposure.

### API tests — not applicable

Story 1.3 ships no API/endpoint surface; the MCP/EventStore boundaries are out of scope.

## Test Run Results

- `dotnet build -c Release …Shell.Tests.csproj` → **0 Warning(s), 0 Error(s)** (TWAE clean).
- `DiffEngine_Disabled=true dotnet test … --filter FullyQualifiedName~Story13AccessibilityPrimitivesTests`
  → **Passed! Failed: 0, Passed: 6** (4 pre-existing + 2 new).
- Must-not-break suites (regression guard) →
  **Passed! Failed: 0, Passed: 51** — `FrontComposerShellParameterSurfaceTests` (locked 7-param
  surface), `FrontComposerShellTests`, `Story11BootstrapShellRenderTests`, `Story12PageLayoutTests`,
  `FcDensityAnnouncerTests`, `FcLifecycleWrapperA11yTests`, `FcExpandedRowHiddenBannerTests`.
- **E2E a11y lane — NOT executed locally.** Environment is Node v22.22.1 (lane requires ≥24) on
  Ubuntu 26.04 (Playwright chromium unsupported). No local pass is claimed. **Standing evidence:** CI
  job `accessibility-visual` (`.github/workflows/ci.yml:420-466`, `runs-on: windows-latest`) runs this
  exact lane (`npm run test:a11y`) as a blocking gate.

## Coverage

| FC-A11Y primitive | bUnit (Layer 1) | E2E axe (Layer 3) |
|---|---|---|
| Skip links → focus target | ✅ pinned | ✅ keyboard-flow |
| Focus visibility (zero-override) | ✅ scoped-CSS guard | ✅ focus-visible test |
| `aria-live` politeness ladder | ✅ **now pinned at shell frame** | ✅ specimen routes |
| `role`/`aria-label` on chrome | ✅ palette/settings/theme **+ nav landmark (new)** | ✅ axe names |
| Keyboard reachability | n/a (E2E layer) | ✅ no-traps flow |
| Reduced-motion + forced-colors | (CSS guard scope) | ✅ dedicated test |

- Shell-frame interactive ARIA contract: **6/6** bUnit ready-gate tests green.
- Override-time enforcement (HFC1050–HFC1055): unchanged — referenced, not modified.

## Next Steps

- Run the e2e a11y lane in CI (`accessibility-visual`, windows-latest) — the standing AC3 evidence.
- Run `/bmad-code-review` before flipping the story to Done (mandatory per story).
- Commit as `test(story-1.3): …` (test-only; **not** `feat` — avoids a minor NuGet publish).
