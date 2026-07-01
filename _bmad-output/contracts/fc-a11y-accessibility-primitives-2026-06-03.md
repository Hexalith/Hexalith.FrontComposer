---
title: 'FC-A11Y — Accessibility-primitives ready-gate contract'
date: '2026-06-03'
story: '1.3'
status: 'escalated'   # primitive set + HFC1050–HFC1055 enforcement agreed; governance sign-off pending (AC2 escalate-with-owner)
owner: 'FrontComposer + Tenants author / Product/UX (pending)'
supersedes: ''
---

# FC-A11Y — Accessibility-primitives ready-gate contract

> **Decision deliverable for Story 1.3.** Unlike a build-from-scratch story, **every FC-A11Y
> primitive named below already ships and is already tested** in the shell at baseline `68034f1`
> (skip links, zero-override focus visibility, the `aria-live` politeness ladder, `role`/`aria-label`
> on chrome, keyboard reachability, reduced-motion + forced-colors fallbacks). The HFC1050–HFC1055
> override-accessibility diagnostics are implemented, emitting, and documented; the e2e axe-core
> specimen lane runs in CI. This note therefore **(1) names the reusable FC-A11Y primitive set** as
> the single, testable accessibility ready-gate every later story points at, **(2) records the
> HFC1050–HFC1055 enforcement linkage** (override-time build-breakers) as the *agreed enforcement
> mechanism*, and **(3) escalates the one open governance question** with a named owner per AC2.
> Adopting FC-A11Y introduces **zero behaviour change**: the contract confirms and pins what the
> shell already does; it re-implements nothing.

## The contract

The FC-A11Y ready-gate is **three enforcement layers**, and naming those three layers *is* the
contract:

| Layer | Scope | Enforced by | What it catches |
|---|---|---|---|
| **1 — Shell-frame invariants** | FrontComposer's own chrome (header, nav, content, skip links) | New consolidated bUnit test `Story13AccessibilityPrimitivesTests` (Story 1.3) | Skip-link → real content/nav focus target, accessible-name coverage on header chrome, **no suppressed focus** in scoped shell CSS — pinned so the framework's own frame cannot silently regress. |
| **2 — Override-time invariants** | Adopter Level-2/3/4 customizations (templates, field slots, full-view overrides) | **HFC1050–HFC1055** (`CustomizationAccessibilityAnalyzer`, build-time `Warning`, TWAE-promoted to a build-breaker) | A custom override that drops an accessible name, breaks keyboard reachability, suppresses focus, omits `aria-live` parity, animates without a reduced-motion fallback, or recolors without forced-colors evidence. |
| **3 — End-to-end (browser)** | Rendered specimen routes, including FluentUI v5 web-component **shadow DOM** | axe-core specimen lane (`npm run test:a11y`), CI job `accessibility-visual` | Real-DOM WCAG 2.1 AA violations bUnit's non-shadow render cannot reach (FluentUI v5 components live in shadow DOM — see `AxeCoreA11yTests` class doc). Blocking = serious ∪ critical. |

### The FC-A11Y primitive set

Each primitive is a reusable, testable pattern with a canonical example already in the codebase:

1. **Skip links** — visually-hidden-until-focused `.fc-skip-link` anchors render first in the shell
   root: `href="#fc-main-content"` (always) and `href="#fc-nav"` (when `HasNavigation`). Each target
   carries `tabindex="-1"` so the link resolves to a real, focusable region.
   *Canonical:* `FrontComposerShell.razor:29-33` (anchors), `:87` (`<div id="fc-nav" tabindex="-1">`),
   `:107` (`<div id="fc-main-content" tabindex="-1" …>`); CSS `FrontComposerShell.razor.css:21-36`.
   WCAG 2.4.1 Bypass Blocks.

2. **Focus visibility (zero-override invariant)** — focus rings inherit Fluent UI's
   `--colorStrokeFocus2` via the native `:focus-visible` default. The contract is a **negative
   invariant**: **no shell CSS suppresses focus** — no `outline: none` / `box-shadow: none` without
   an adjacent `:focus-visible` restore.
   *Canonical:* the zero-override comment at `FrontComposerShell.razor.css:18-20`. WCAG 2.4.7 Focus
   Visible. Pinned by the "no suppressed focus" guard test (Story 1.3).

3. **`aria-live` politeness ladder** — `role="status"` + `aria-live="polite"` for non-urgent status,
   escalating to `role="alert"` + `aria-live="assertive"` for rejections / errors / action prompts;
   `aria-atomic="true"`; skip-first-render so a page load does not announce stale state.
   *Canonical:* `FcDensityAnnouncer.razor:10-18` (status/polite/atomic, skip-first-render);
   politeness escalation in `FcLifecycleWrapper` (status→alert); the WCAG 4.1.2 hidden-expansion
   live region in `FcExpandedRowHiddenBanner`; `FcProjectionConnectionStatus`,
   `FcPendingCommandSummary`. WCAG 4.1.3 Status Messages.

4. **`role` / `aria-label` on interactive elements** — combobox/listbox/option/group roles for the
   command palette; `aria-label` / `Title` on icon buttons; visually-hidden labels (`.fc-sr-only`
   global, `.fc-visually-hidden` scoped) where an icon button would otherwise be unnamed — chosen to
   avoid double / triple screen-reader announces.
   *Canonical:* `FcPaletteTriggerButton` (`PaletteTriggerAriaLabel`), `FcSettingsButton`
   (`SettingsTriggerAriaLabel`), `FcThemeToggle` (`ThemeToggleAriaLabel`), nav rail
   (`NavMenuAriaLabel`) — all via `IStringLocalizer<FcShellResources>` (FC-L10N, Story 1.4: never
   hard-code copy). WCAG 4.1.2 Name, Role, Value.

5. **Keyboard reachability** — global `Ctrl+,` (settings) and `Ctrl+K` (palette) via
   `IShortcutService`; the per-key `preventDefault` in `fc-keyboard.js` **never swallows Tab**, so no
   focus trap is introduced. Interactive elements are never made unreachable with `tabindex="-1"`
   (that attribute is reserved for skip-link focus *targets*, not controls).
   *Canonical:* shell `@onkeydown` router (`FrontComposerShell.razor:26-27`), `IShortcutService`
   registrations. WCAG 2.1.1 Keyboard.

6. **Reduced-motion + forced-colors fallbacks** — `@media (prefers-reduced-motion: reduce)` disables
   transitions / animations; color styling is **token-driven** (Fluent UI tokens auto-adapt under
   forced-colors / Windows High Contrast); `color-mix()` rules ship a solid-rgba fallback *before*
   the `@supports` block so unsupported engines still get a legible color.
   *Canonical:* `FcLifecycleWrapper.razor.css` (`@media (prefers-reduced-motion: reduce)` +
   focus-ring `outline` preserved), `wwwroot/css/fc-projection.css` (`color-mix()` + rgba fallback).
   WCAG 2.3.3 Animation from Interactions; forced-colors / high-contrast.

> **RTL / FC-L10N forward-compatibility.** Any CSS this contract documents uses **logical
> properties** (`max-inline-size`, `margin-inline`, `inset-inline`) rather than physical
> (`max-width`, `margin-left/right`), consistent with FluentUI v5 and the FC-LYT precedent, so
> FC-A11Y stays correct under FC-L10N (Story 1.4) RTL cultures.

## Enforcement linkage — HFC1050–HFC1055 (AC2)

The **agreed enforcement mechanism** for Layer-2 (override-time) violations is the
`CustomizationAccessibilityAnalyzer` diagnostic family. Each diagnostic is `Warning` severity,
`isEnabledByDefault=true`, and **promoted to a build-breaker by `TreatWarningsAsErrors=true`** — that
promotion is what makes the primitive set a *hard ready-gate* for adopter overrides rather than an
advisory hint. These are **already implemented + emitting + documented** (`docs/diagnostics/HFC1050.md`
… `HFC1055.md`); this contract **references** them and does **not** modify the analyzer, descriptors,
or diagnostics docs.

| Diagnostic | Override violation | FC-A11Y primitive | WCAG criterion |
|---|---|---|---|
| **HFC1050** | Interactive element missing accessible name | `aria-label` / visually-hidden label | 4.1.2 Name, Role, Value |
| **HFC1051** | Keyboard reachability blocked (`tabindex="-1"` on an interactive element) | Keyboard reachability | 2.1.1 Keyboard |
| **HFC1052** | Suppressed focus (`outline` / `box-shadow: none` without `:focus-visible`) | Focus visibility | 2.4.7 Focus Visible |
| **HFC1053** | Lifecycle / status override missing `aria-live` parity | `aria-live` politeness ladder | 4.1.3 Status Messages |
| **HFC1054** | Motion without a reduced-motion fallback | Reduced-motion fallback | 2.3.3 Animation from Interactions |
| **HFC1055** | Custom colors without forced-colors evidence | Forced-colors fallback | (forced-colors / high-contrast) |

*Declaration:* `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:245-273`.
*Descriptors:* `src/Hexalith.FrontComposer.SourceTools/Diagnostics/DiagnosticDescriptors.cs:593-657`.
*Emission:* `src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs:139-210`.

## Confirmation

**Status: ESCALATED (owner: FrontComposer + Tenants author / Product/UX — pending).** No live
confirmation from the named owner was available at implementation time. Per AC2, the contract is
recorded as *escalated with a named owner*; the primitive set and the HFC1050–HFC1055 enforcement
linkage are **agreed and shipped** (they already exist), and the following governance question is the
only genuinely open item:

1. **Is "the bUnit shell-frame gate (Layer 1) + the HFC1050–HFC1055 override gate (Layer 2) + the
   e2e axe lane (Layer 3)" the complete FC-A11Y ready-gate — or is a per-story manual a11y checklist
   also required?** Recommended: the three automated layers *are* the testable ready-gate every
   later story points at; a per-story manual checklist is **not** mandated (it would be advisory at
   best and unenforceable). Confirm or override.
2. **Who signs off visual-design accessibility** (contrast ratios, focus-ring visibility against
   custom themes) that automated tooling cannot fully judge? Recommended owner: Product/UX, with the
   Tenants author for tenant-theme contrast. Confirm.

Owner column per the readiness request (`frontcomposer-readiness-request-2026-06-03.md:23`, 🔴
FC-A11Y row): **FrontComposer + Tenants author** (Product/UX for visual-design input). Resolution
does **not** block Story 1.3 — the AC explicitly permits escalate-with-owner.

### Follow-up tracking (2026-07-01)

Correct-course follow-through from the Epic 1 retro records this as a tracked sprint action rather
than an untracked pending owner: `sprint-status.yaml` now carries **"Drive residual FC-A11Y,
FC-L10N, FC-DOC, and FC-SETTINGS wording decisions to confirmed or dated owned follow-up"** with
owner **FrontComposer + Product/UX + Tenants author**. Until that action is closed, this contract
stays `status: escalated`; the shipped three-layer automated ready-gate remains the recommended
default.

## FC-DOC linkage (deferred to Story 1.5)

The cross-link from the **published component docs** to this contract is owned by **Story 1.5
(FC-DOC)**, which owns the CI-gated `docs/` DocFX site. This story does **not** scratch-write
`docs/`. For 1.5 to link it, this contract lives at:

```
_bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md
```

Story 1.5 should add the published-docs cross-reference from the `FrontComposerShell` accessibility
section (and the HFC1050–HFC1055 diagnostic pages, which already live under `docs/diagnostics/`)
pointing here, so the reusable FC-A11Y ready-gate is discoverable from the component docs.

## Surface confirmed / pinned by Story 1.3

- **No new `src/` surface.** The primitives, announcers, diagnostics, and e2e lane all pre-exist.
- **New pin:** `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs`
  — consolidates the shell-frame invariants (skip-link→target, accessible-name coverage, no
  suppressed focus) into a single Layer-1 ready-gate test.
- **Referenced, not modified:** HFC1050–HFC1055 (`CustomizationAccessibilityAnalyzer` + descriptors +
  `docs/diagnostics/HFC105*.md`); the e2e specimen lane (`specs/specimen-accessibility.spec.ts`,
  `helpers/a11y.ts`, manifest, visual baselines).

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3: Establish FC-A11Y accessibility primitives as a ready-gate] (story + 3 ACs; AR2 / AR4 / NFR6)
- [Source: _bmad-output/planning-artifacts/epics.md:83] (NFR6 — WCAG: aria-label/role/aria-live/data-testid, focus, reduced-motion/forced-colors, HFC1050–HFC1055)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-A11Y ask + owner: FrontComposer + Tenants author)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT contract — structure/tone mirrored; escalate-with-owner precedent)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor:29-33,87,107] (skip links + #fc-main-content / #fc-nav targets)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css:18-36] (skip-link CSS + zero-override focus invariant)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor:10-18] (aria-live announcer pattern)
- [Source: src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs:245-273] (HFC1050–HFC1055 IDs)
- [Source: src/Hexalith.FrontComposer.SourceTools/Diagnostics/CustomizationAccessibilityAnalyzer.cs:139-210] (HFC1050–HFC1055 emission logic)
- [Source: docs/diagnostics/HFC1050.md … HFC1055.md] (published diagnostic docs — reference only, do not modify)
- [Source: tests/e2e/specs/specimen-accessibility.spec.ts + tests/e2e/helpers/a11y.ts:16-18,35-64] (the `npm run test:a11y` lane; blocking = serious/critical, WCAG 2.1 AA)
- [Source: .github/workflows/ci.yml#accessibility-visual] (standing CI evidence for the e2e a11y layer)
