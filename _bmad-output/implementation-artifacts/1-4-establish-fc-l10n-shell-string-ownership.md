---
baseline_commit: df3731344566ae096c671752b92933a2e6cde9ff
---

# Story 1.4: Establish FC-L10N shell-string ownership

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield reality — read this first.** The localization machinery this story is asked to
> "establish" **already exists and is already tested**. This is **the FC-LYT (1.2) / FC-A11Y (1.3)
> shape again**: a *confirm-and-document* ready-gate story, **NOT a build-from-scratch** one.
> Concretely, at baseline `df37313` the shell already ships:
> - **`FcShellResources.resx` (EN default) + `FcShellResources.fr.resx` (French satellite)** with
>   **157 keys each, at full parity** (`src/Hexalith.FrontComposer.Shell/Resources/`). All shell-chrome
>   text (nav, settings, status, palette, lifecycle, filters, badges, authorization) already resolves
>   through `IStringLocalizer<FcShellResources>`.
> - **`AddHexalithShellLocalization(...)`** — the opt-in extension that chains the shell's
>   request-localization defaults (`SupportedCultures`, `DefaultRequestCulture`) into the adopter's
>   pipeline via `ShellRequestLocalizationOptionsSetup`, *without* double-adding `AddLocalization()`
>   so adopters keep authoritative control (`ServiceCollectionExtensions.cs:452-465,645-662`).
> - **`AddHexalithFrontComposerQuickstart()`** already calls `AddLocalization()` +
>   `AddHexalithShellLocalization()` for one-line setup (`:491-511`).
> - **Culture config surface** — `FcShellOptions.DefaultCulture` (`"en"`) and `SupportedCultures`
>   (`["en","fr"]`), BCP-47-validated, with a cross-property invariant that `SupportedCultures` must
>   include `DefaultCulture` (`FcShellOptions.cs:134-150`).
> - **The resx-convention boundary** — `FcShellResources` (a marker class in
>   `…Shell.Resources`) is the BaseName; satellites resolve by `typeof(T).FullName`. Adopters can
>   `services.Replace` the `IStringLocalizer<FcShellResources>` for a DB-backed/whitelabel source.
> - **Comprehensive parity/round-trip tests** — `FcShellResourcesTests.cs` already pins: EN↔FR key
>   parity, culture round-trip (`ThemeToggleAriaLabelResolvesInFrench`), localizer registration,
>   per-key EN/FR values across 6 surface groups, French-NBSP-before-colon byte guards, and
>   placeholder-count parity across locales.
>
> So Story 1.4 is **(1) author the FC-L10N ownership contract doc** that draws the shell-vs-host/domain
> string-ownership boundary, names the `FcShellResources.resx` + `AddHexalithShellLocalization`
> mechanism, the supported-culture surface, and the `services.Replace` extensibility seam; **(2)
> confirm it with the Tenants author or escalate with a named owner** (and queue the FC-DOC cross-link
> to Story 1.5); **(3) pin the *shell-frame* ownership ready-gate** — "the shell renders chrome in a
> non-default culture and no hard-coded English chrome string remains" — with **one consolidated
> bUnit test** the rest of Epic 1+ can point at. Do **NOT** rebuild the resx files, the localizer
> wiring, or the per-key parity tests — they exist. Do **NOT** scratch-write the CI-gated `docs/` site.

## Story

As an adopter developer,
I want clear ownership of localized strings between the shell (`FcShellResources.resx`) and the Tenants layer,
so that shell text is localizable without colliding with host-owned strings.

## Acceptance Criteria

**AC1 — Shell-chrome strings resolve from `FcShellResources.resx` via `AddHexalithShellLocalization(...)`; host/domain strings stay host-owned. *(AR3)***
**Given** the FC-L10N ownership map,
**When** a string is shell-chrome (nav, settings, status, palette),
**Then** it resolves from `FcShellResources.resx` via `AddHexalithShellLocalization(...)`; host/domain strings stay host-owned. The ownership boundary is **documented** (which string categories the shell owns vs. which the host/Tenants layer owns) and the resolution mechanism (resx BaseName convention + `IStringLocalizer<FcShellResources>` + the `services.Replace` swap seam) is named.

**AC2 — Under a non-default culture the shell renders chrome in that culture, and no hard-coded English chrome string remains. *(AR3)***
**Given** a non-default culture is configured (`fr`),
**When** the shell renders,
**Then** shell-chrome strings display in that culture (assert representative chrome accessible-names/labels equal their **FR** resx values, **not** the EN values) **and** no hard-coded English chrome string remains (a chrome element bound to a literal instead of the localizer would fail this test). Pin this invariant with a consolidated bUnit test so the ready-gate cannot silently regress.

**AC3 — The ownership map is confirmed by the Tenants author, or the boundary question is escalated with a named owner.**
**Given** the ownership map,
**When** the Tenants author reviews it,
**Then** it is marked **confirmed** (boundary agreed) **or** the boundary question is **escalated with a named owner**, **and** queued for the FC-DOC cross-link owned by **Story 1.5** (do **not** scratch-write `docs/`).

## Tasks / Subtasks

- [x] **Task 1 — Author the FC-L10N ownership contract doc (AC: #1, #3) — the DECISION/DOC deliverable**
  - [x] Create `_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md`. **Mirror the structure/tone of the FC-LYT and FC-A11Y contracts** (`_bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md`, `…/fc-a11y-accessibility-primitives-2026-06-03.md`): front-matter (`title`, `date`, `story: '1.4'`, `status`, `owner`, `supersedes`), a "Decision deliverable" intro, the contract body, a **Confirmation** section, and an **FC-DOC linkage** section. **Never** write to the CI-gated `docs/` DocFX site (project-context "docs/ is a PUBLISHED DocFX site" rule); BMAD contracts live under `_bmad-output/contracts/`.
  - [x] **Document the ownership boundary as a table** — for each string category, who owns it and where it resolves:
    | String category | Owner | Source | Resolves via |
    |---|---|---|---|
    | Shell chrome — nav, settings, status, palette, lifecycle, filters, badges, authorization, density, shortcuts, footer, skip-links | **FrontComposer Shell** | `FcShellResources.resx` (+ `.fr.resx`) | `IStringLocalizer<FcShellResources>` |
    | Dev-mode overlay strings (DEBUG/specimen authoring aid) | **FrontComposer Shell** (dev-only) | `Resources/DevMode/DevModeStrings.resx` (+ `.fr.resx`) | `IStringLocalizer<DevModeStrings>` — **separate** resource, not user-facing chrome |
    | Domain field/label text — projection columns, command form labels | **Host/domain** | `[Display(Name=…)]` on the host's annotated types **or** the host's own `IStringLocalizer<T>` (T = the command/projection type) | runtime `IStringLocalizer<TCommand>`; `[Display(Name)]` wins, else falls through to the per-type localizer (see `FormFieldModel.cs:39,62`, `CommandFormEmitter.cs:98`) |
    | Application/tenant text — app name override, host nav items, host pages | **Host / Tenants layer** | host-owned resources | host's own localization registration |
  - [x] **Name the mechanism (AC1):** `FcShellResources` marker class lives in `Hexalith.FrontComposer.Shell.Resources`; ASP.NET Core resolves satellites by `typeof(FcShellResources).FullName` as the resx BaseName (`FcShellResources.cs:9-13`). `AddHexalithShellLocalization(services, configure?)` registers `ShellRequestLocalizationOptionsSetup` (a `TryAddEnumerable` singleton `IConfigureOptions<RequestLocalizationOptions>`) that sets `DefaultRequestCulture`/`SupportedCultures`/`SupportedUICultures` from `FcShellOptions` (`ServiceCollectionExtensions.cs:452-465,645-662`). It **deliberately does not** call `AddLocalization()` so the adopter retains authoritative `LocalizationOptions` control. The **swap seam**: `services.Replace(ServiceDescriptor.Scoped(typeof(IStringLocalizer<FcShellResources>), …))` for a DB-backed/whitelabel localizer (`:432-438`).
  - [x] **Record the supported-culture surface:** `FcShellOptions.DefaultCulture` (default `"en"`) + `SupportedCultures` (default `["en","fr"]`), BCP-47-lite validated (`^[a-z]{2}(-[A-Z]{2})?$`), with the cross-property invariant that `SupportedCultures` **must include** `DefaultCulture` (validated in `FcShellOptionsValidationTests`). Adopters extend coverage by adding cultures here + shipping the matching `FcShellResources.<culture>.resx` satellite (or a custom localizer).
  - [x] **Scope "chrome" precisely (the genuinely open boundary nuance):** the `FcDensityPreviewPanel` density preview uses **illustrative sample-data column titles** (`"Order"/"Customer"/"Status"`, `FcDensityPreviewPanel.razor:26-28`). State explicitly whether these illustrative demo strings are **in scope** (must be localized) or **out of scope** (sample data, like Lorem-ipsum, intentionally not localized). **Recommended:** out-of-scope sample data, but flag it as the one open boundary item for the Tenants author / Product/UX to confirm. (This is the kind of "open question" AC3 expects you to surface, not silently resolve.)
  - [x] **Confirmation section:** mark `confirmed` OR escalate with a **named owner**. Owner per the readiness request (`frontcomposer-readiness-request-2026-06-03.md:23`, 🔴 row): **FrontComposer + Tenants author**. **YOLO mode:** if no live confirmation is available, write it as **escalated with owner = "FrontComposer + Tenants author (pending)"** and proceed — AC3 explicitly permits escalate-with-owner. List the genuinely open items (e.g. the density-preview sample-string scope above; whether the host is expected to register its own `IStringLocalizer` for domain labels or whether the shell should provide a fallback).
  - [x] **FC-DOC linkage:** record this contract's path so **Story 1.5 (FC-DOC)** can cross-link it from the published component docs. Do **not** pre-create a `docs/` page.

- [x] **Task 2 — Pin the shell-frame L10N ownership ready-gate with a consolidated bUnit test (AC: #2) — the CODE deliverable**
  - [x] Add `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs`. **Extend `LayoutComponentTestBase`** (Fluxor store + theme mock + beforeunload/keyboard JS stubs; `JSInterop.Mode = Loose`) and follow `Story13AccessibilityPrimitivesTests.cs` / `FrontComposerShellTests.cs` render-and-query house style.
  - [x] **Critical wiring nuance — register a *localized* `IStringLocalizer<FcShellResources>` in the test DI.** `LayoutComponentTestBase` likely registers a pass-through/stub localizer (echoing keys) for components that only need a non-null localizer. For AC2 you must assert **real FR values**, so the test must build a **genuinely localized** provider — reuse the exact pattern from `FcShellResourcesTests.BuildLocalizedProvider()` (`AddLogging()` + `AddLocalization()` + `AddHexalithShellLocalization(services)`) and register that real `IStringLocalizer<FcShellResources>` into the bUnit `Services` so the rendered shell resolves against the embedded resx. Confirm by reading `LayoutComponentTestBase` first; if it already supplies a real localizer, assert against it — do not double-register.
  - [x] **Non-default-culture chrome render (AC2):** set `CultureInfo.CurrentUICulture = new CultureInfo("fr")` inside a try/finally that restores the previous culture (mirror `FcShellResourcesTests.ThemeToggleAriaLabelResolvesInFrench:43-50`). Render `FrontComposerShell` (with navigation — reuse the registry/viewport setup from `FrontComposerShellTests.cs`). Assert representative chrome accessible-names render in **French** and are **not** the English value:
    - theme toggle `aria-label` ⇒ `"Changer de thème"` (key `ThemeToggleAriaLabel`), **not** the EN `"Change theme"`.
    - palette trigger ⇒ FR value of `PaletteTriggerAriaLabel` (`"Ouvrir la palette de commandes"`).
    - settings button ⇒ FR value of `SettingsTriggerAriaLabel`.
    Resolve the expected FR strings from the **localizer** (`localizer["ThemeToggleAriaLabel"].Value`) rather than hard-coding the French literal in the assertion where practical — that keeps the test resilient to copy edits while still proving "the rendered DOM != the EN literal." At minimum, assert the rendered attribute **equals the FR resx value** and **does not equal** the EN resx value (the "no hard-coded English chrome" guard).
  - [x] **Ownership-boundary guard (AC1, lightweight):** add one assertion that the always-present chrome accessible-names are **non-empty and resource-sourced** (already partially covered by `Story13AccessibilityPrimitivesTests`; here the angle is *localized-source*, not *present*). Avoid duplicating 1.3's accessible-name-presence test — this test's distinct contribution is the **culture-sensitivity** proof.
  - [x] **Method naming:** three-part `Subject_Scenario_Expectation`; file plural `…Tests.cs`; `sealed` class. Use **Shouldly** (`ShouldBe`/`ShouldNotBe`/`ShouldContain` — never raw `Assert.*`).
  - [x] **Do NOT add a `[Parameter]` to `FrontComposerShell`.** This story adds no shell surface — the locked 7-parameter surface (`FrontComposerShellParameterSurfaceTests`) must stay **untouched and green** (verify it).
  - [x] **Shadow-DOM caveat (carry from 1.3):** bUnit cannot DOM-walk FluentUI v5 web-component shadow DOM. Assert against the **markup-level attributes the shell sets** (the `aria-label`/`Title` it binds onto the FluentUI components), exactly as `Story13AccessibilityPrimitivesTests` does — do not try to reach into Fluent internals.

- [x] **Task 3 — Confirm parity/round-trip coverage stays green; confirm boundary or escalate (AC: #2, #3)**
  - [x] **Re-run and confirm the existing L10N suite is green** as the standing parity evidence: `FcShellResourcesTests` (157-key EN↔FR parity, culture round-trip, per-key values, NBSP-before-colon byte guards, placeholder-count parity) and `FcShellOptionsValidationTests` (culture-config invariants). This story **builds on** them — do not modify or duplicate them. Record the result (test count, pass) in the Dev Agent Record.
  - [x] **Confirm the boundary or escalate with owner (AC3):** under YOLO with no live Tenants-author confirmation, mark the contract `status: escalated`, `owner: FrontComposer + Tenants author (pending)`, list the open boundary items (density-preview sample strings; domain-label localizer expectation), and proceed. Queue the FC-DOC cross-link for Story 1.5.

- [x] **Task 4 — Build clean + run the test lanes (DoD)**
  - [x] `dotnet build -c Release Hexalith.FrontComposer.slnx` clean (TWAE — **zero** warnings; this story adds a test file + a markdown contract, no new public API surface, so no XML-doc/`PublicAPI.Shipped.txt` obligation).
  - [x] `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --no-build --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — the new `Story14ShellStringOwnershipTests` green; **re-confirm the Story 1.1–1.3 pre-existing-failure baseline** (the documented 13 full-lane failures: 8 Shell + 3 SourceTools + 2 Cli) is **unchanged** from `df37313` — these are NOT regressions. If a failure looks new, stash to `df37313` and compare (the 1.1/1.2/1.3 documented method).
  - [x] Keep the existing L10N + layout suites green — `FcShellResourcesTests.cs`, `FcShellOptionsValidationTests.cs`, `FrontComposerShellTests.cs`, `FrontComposerShellParameterSurfaceTests.cs`, `Story12PageLayoutTests.cs`, `Story13AccessibilityPrimitivesTests.cs` — this story builds **on** them, not over them.

## Dev Notes

### What already exists vs. what's new

| Concern | State today (baseline `df37313`) | This story |
|---|---|---|
| `FcShellResources.resx` (EN) + `.fr.resx` (FR), 157 keys at parity | **Exists** (`Resources/FcShellResources*.resx`) | **Document ownership + confirm** — do NOT rebuild |
| `IStringLocalizer<FcShellResources>` resolution across all chrome | **Exists** (palette, settings, theme, nav, lifecycle, filters, badges, auth, density) | **Document the boundary**; pin culture-sensitivity in the new test |
| `AddHexalithShellLocalization(...)` + `ShellRequestLocalizationOptionsSetup` | **Exists** (`ServiceCollectionExtensions.cs:452-465,645-662`) | **Name it in the contract** as the resolution mechanism — do NOT modify |
| `AddHexalithFrontComposerQuickstart()` chains localization | **Exists** (`:491-511`) | **Reference** — untouched |
| Culture config (`DefaultCulture`/`SupportedCultures`, `["en","fr"]`) | **Exists** (`FcShellOptions.cs:134-150`) | **Document the supported-culture surface** |
| EN↔FR parity / round-trip / NBSP / placeholder tests | **Exist** (`FcShellResourcesTests.cs`, 30+ tests) | **Confirm green** — do NOT duplicate |
| FC-L10N ownership contract doc | **Does NOT exist** (only FC-LYT + FC-A11Y exist in `_bmad-output/contracts/`) | **Author it** |
| Consolidated shell-frame *culture-render* bUnit test | **Does NOT exist** (round-trip is per-key in `FcShellResourcesTests`, not a whole-shell-frame render under `fr`) | **Add `Story14ShellStringOwnershipTests`** |

### Exact anchors (read these before coding)

- **The marker + resx convention** — `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.cs:9-13` (BaseName = `typeof(FcShellResources).FullName`), `Resources/FcShellResources.resx` (157 EN keys), `Resources/FcShellResources.fr.resx` (157 FR keys).
- **The localization extension** — `src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:452-465` (`AddHexalithShellLocalization`, the deliberate no-`AddLocalization` design + the `services.Replace` swap-seam remark at `:432-438`), `:491-511` (`AddHexalithFrontComposerQuickstart` chains it), `:645-662` (`ShellRequestLocalizationOptionsSetup` reading `FcShellOptions` → `DefaultRequestCulture`/`SupportedCultures`/`SupportedUICultures`).
- **Culture options** — `src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs:134-150` (`DefaultCulture` default `"en"`, BCP-47 regex; `SupportedCultures` default `["en","fr"]`, must-include-default invariant).
- **The existing parity/round-trip suite (the template for "what's already covered")** — `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs`: `CanonicalKeysHaveFrenchCounterparts:18-35` (parity), `ThemeToggleAriaLabelResolvesInFrench:37-51` (the **culture round-trip pattern to copy** — try/finally save-restore `CurrentUICulture`), `BuildLocalizedProvider():365-371` (the **real localized DI provider** to reuse), the per-surface `[Theory]` blocks (nav/settings/palette/badge/filter/auth EN+FR values), and the NBSP/placeholder byte guards.
- **Domain-label ownership (the host side of the boundary)** — `src/Hexalith.FrontComposer.SourceTools/Transforms/FormFieldModel.cs:39,62` (`[Display(Name)]` wins, else runtime `IStringLocalizer` per-type), `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:98,105` (generated forms inject `IStringLocalizer<TCommand>` for domain labels **and** `IStringLocalizer<FcShellResources>` for shell-owned authorization warnings — the two-localizer split is the boundary in code).
- **Dev-mode strings (separate resource)** — `src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx` (+ `.fr.resx`), consumed by `FcDevModeOverlay.razor:10` via `IStringLocalizer<DevModeStrings>`. Dev/specimen authoring aid, not user-facing chrome — document as a separate, shell-owned-but-dev-only resource.
- **The sample-data nuance** — `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor:26-28` (hard-coded `"Order"/"Customer"/"Status"` column titles in the **density preview** — illustrative sample data, the one genuinely open scope question for AC3).
- **bUnit base + house style** — `tests/.../Components/Layout/LayoutComponentTestBase.cs` (Fluxor store + theme mock + JS stubs; **check whether it registers a real or stub localizer**), `FrontComposerShellTests.cs` (render-and-query, registry/viewport setup for the has-navigation case), `Story13AccessibilityPrimitivesTests.cs` (the most recent same-shape story — accessible-name assertions against the localizer; copy its render-and-query + localizer-assertion style).

### The FC-L10N ownership decision (encode in the contract doc)

The story's first job is a **decision/declaration**, not new code. Recommended, carry unless reviewers override:

- **The boundary is a two-localizer split, and that *is* the contract.** (1) **Shell chrome** — every framework-owned UI string (nav, settings, status, palette, lifecycle, filters, badges, authorization warnings, density, shortcuts, footer, skip-links) resolves through `IStringLocalizer<FcShellResources>` against the embedded `FcShellResources.resx`/`.fr.resx`; the shell owns these and ships EN+FR. (2) **Domain/host text** — projection column titles and command-form field labels resolve through the **host's** per-type `IStringLocalizer<T>` (or `[Display(Name)]`), never `FcShellResources`; the host/Tenants layer owns these. The generated command form injecting **both** localizers (`CommandFormEmitter.cs:98,105`) is the boundary made concrete in code. (3) **Application/tenant text** (app name override, host nav, host pages) stays entirely host-owned.
- **`AddHexalithShellLocalization` is the seam, and it is deliberately non-greedy.** It advertises the shell's cultures and default but never calls `AddLocalization()` — the adopter keeps authoritative `LocalizationOptions`. The `services.Replace(IStringLocalizer<FcShellResources>)` swap is the supported extensibility path for whitelabel/DB-backed sources. Naming this contract is the deliverable.
- **Confirm-or-escalate, don't redesign.** Every mechanism already has a working implementation and (mostly) a test. The contract confirms them; the only genuinely open items are the boundary/scope questions (density-preview sample strings; whether the shell provides any domain-label fallback) — escalate those with the named owner.

### Must-not-break (regression surface)

A ready-gate story must leave the shell working end-to-end. Preserve:

- **The 157-key EN/FR parity** — adding or renaming a key without its FR counterpart breaks `CanonicalKeysHaveFrenchCounterparts`. This story adds **no** resx keys; if a review insists on one, add the EN **and** FR entries together, plus its per-key theory row.
- **`FcShellResources` BaseName identity** — the marker class's namespace + name (`Hexalith.FrontComposer.Shell.Resources.FcShellResources`) is the resx resolution key; moving/renaming it silently breaks every satellite lookup. Read-only here.
- **`AddHexalithShellLocalization` / `ShellRequestLocalizationOptionsSetup` / `FcShellOptions` culture surface** — referenced, not modified. The deliberate no-`AddLocalization` design and the must-include-default invariant are contract behaviour.
- **The locked 7-parameter shell surface** (`FrontComposerShellParameterSurfaceTests`) — this story adds **no** shell `[Parameter]`; verify the lock stays green and untouched.
- **French typographic byte guards** (NBSP-before-colon) + placeholder-count parity — referenced as standing evidence; do not disturb the resx values they assert.
- **No `docs/` writes** — `docs/` is the CI-gated DocFX site; the contract goes to `_bmad-output/contracts/`, and the published cross-link is **Story 1.5 (FC-DOC)**'s job.

### Previous story intelligence (Stories 1.2 + 1.3 — both `done`)

- **1.2 and 1.3 are the exact template for 1.4:** confirm a contract, add the minimal consolidated pin test, escalate-with-owner under YOLO, defer the FC-DOC cross-link to Story 1.5. Reuse their fixtures and house style (`Story12PageLayoutTests.cs`, `Story13AccessibilityPrimitivesTests.cs`, the FC-LYT/FC-A11Y contract doc shape).
- **Docs discipline (re-confirmed by 1.1/1.2/1.3):** `docs/` is the published DocFX site; the contract doc goes to `_bmad-output/contracts/`, the cross-link is deferred to 1.5.
- **Pre-existing-failure baseline:** 1.1, 1.2, **and 1.3** all recorded **13 full-lane failures** (8 Shell: `PendingStatusReopenGovernanceTests`×4, `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute…`×1, Generated snapshot×3; 3 SourceTools; 2 Cli) reproduced identically against `f40dece`/`68034f1`/`df37313` — **NOT regressions**. Don't chase them; if a failure looks new, stash to `df37313` and compare (the documented method).
- **YOLO escalate-with-owner is acceptable** — 1.2's FC-LYT and 1.3's FC-A11Y both shipped `status: escalated` with a pending owner and passed review. FC-L10N can do the same with the readiness-request owner (FrontComposer + Tenants author).
- **1.3 already asserts chrome accessible-names against `IStringLocalizer<FcShellResources>` values** (`Story13AccessibilityPrimitivesTests`). 1.4's **distinct** contribution is the **non-default-culture** angle: prove the rendered chrome changes to FR under `CurrentUICulture=fr` (round-trip at the shell-frame level), not just that names are present. Don't re-assert presence; assert culture-sensitivity.
- **The 1.3 test already references `IStringLocalizer<FcShellResources>` as "FC-L10N, Story 1.4"** — this story closes that forward reference. Confirm the 1.3 test's localizer setup so 1.4 uses a consistent (real-localized) DI wiring.

### Git intelligence

- HEAD `df37313` = Story 1.3 (`feat(story-1.3): Establish FC-A11Y accessibility primitives as a ready-gate`). Recent commits (`0db0fb0` spike, `f40dece` bootstrap, `68034f1` FC-LYT, `df37313` FC-A11Y) are all "confirm-and-pin + minimal additive" stories — the shape 1.4 continues. None touched the resx files or the localization wiring, so the L10N surface is exactly as documented.
- Working tree has one unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.
- Branch `feat/<desc>` (continue on `feat/story-1-2-fc-lyt-page-layout` or branch `feat/story-1-4-fc-l10n` — **never** commit to `main`). Conventional Commit: this story adds a contract doc + a test, no shipped behaviour change, so `docs(story-1.4): …` or `test(story-1.4): …` (**NOT** `feat` — a test/doc-only story must not trigger a minor NuGet publish; project-context "Don't use `feat` for refactors/test/docs"). If the design review adds any `src/` behaviour, re-evaluate. Run `/bmad-code-review` before flipping to done.

### Latest tech / FluentUI + .NET localization notes

- **.NET 10 / ASP.NET Core resx convention** — satellites resolve by `typeof(T).FullName` as BaseName; the embedded `FcShellResources.fr.resx` is picked up automatically with `AddLocalization()` registered. No `ResourcesPath` override is set, so the resx must sit at the namespace-matching path (`Resources/FcShellResources*.resx` ↔ `…Shell.Resources.FcShellResources`) — already correct.
- **Culture round-trip in tests** uses `CultureInfo.CurrentUICulture` (UI culture drives `IStringLocalizer`), not `CurrentCulture`. Always restore in a `finally` (tests share the thread) — copy `FcShellResultsTests.ThemeToggleAriaLabelResolvesInFrench`.
- **FluentUI v5 RC** (`Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`, exact pin — ADR-003): FC-L10N sets `aria-label`/`Title` strings the shell binds onto Fluent components from the localizer — **no new FluentUI API surface**, so the RC pin is untouched. The shadow-DOM caveat (bUnit can't reach Fluent web-component internals) means the test asserts the markup-level attributes the shell *sets*, not Fluent's rendered shadow output.
- **RTL forward-compatibility** — FC-LYT (1.2) and FC-A11Y (1.3) already adopted logical CSS properties (`max-inline-size`, `margin-inline`) for RTL-readiness; the FC-L10N contract should note that adding an RTL culture (e.g. `ar`) is a resx-satellite + `SupportedCultures` addition, and the layout/a11y CSS is already logical-property-clean for it. No RTL satellite ships in this story.

### Project-context rules that bite here

- **No copyright/license headers** (0 of 483 files). **File-scoped namespaces, Allman braces, `_camelCase` fields, `Async` suffix, `I`-prefixed interfaces.** `sealed` the test class.
- **Test discipline:** xUnit **v3** + **Shouldly** (`ShouldBe`/`ShouldNotBe`/`ShouldContain` — never raw `Assert.*`) + bUnit; plural `…Tests.cs`; methods three-part `Subject_Scenario_Expectation`; **`DiffEngine_Disabled=true`** when running tests (else Verify hangs); solution-level `dotnet test` + the trait filter (NOT per-project).
- **`TreatWarningsAsErrors=true`** — a test-only + markdown story has no new public API, so no XML-doc/`PublicAPI.Shipped.txt` obligation; still build Release clean (zero warnings).
- **`.slnx` only**; **centralized package versions** (no `Version=` in `.csproj`); **no new third-party analyzer/test framework** (built-in analyzers + the existing xUnit/Shouldly/bUnit stack only).
- **`ConfigureAwait(false)`** on awaits in `src/` (CA2007→error via TWAE) — the new file is a **test**, where this is not required (follow the existing `…Tests.cs` style, which omits it).
- **Generated/BMAD docs → `_bmad-output/`, never `docs/`.** The FC-DOC published cross-link is **Story 1.5**'s.
- **ULIDs/Fluxor/MCP rules** — not in play for this doc+test story; no command/projection/MCP/source-generator surface is touched.

### Project Structure Notes

- New contract doc: `_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md` (beside the FC-LYT + FC-A11Y contracts).
- New test: `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs`.
- **No `src/` changes expected** — the resx files, localizer wiring, culture options, and parity tests all already exist. If a review insists on a tiny `src/` touch (e.g. an unlocalized literal found by the new culture-render test), it must be additive: bind the literal to a **new** `FcShellResources` key, adding the EN **and** FR entries together plus its per-key theory row in `FcShellResourcesTests`. No structural variances expected; the contract-in-`_bmad-output` + test-in-`Shell.Tests` split matches the FC-LYT/FC-A11Y precedent and the dependency-down-to-Contracts rule (no new Contracts/Shell types).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4: Establish FC-L10N shell-string ownership] (story + 3 ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (AR3 FC-L10N; FR9/FR10/FR15)
- [Source: _bmad-output/planning-artifacts/frontcomposer-readiness-request-2026-06-03.md:23] (🔴 FC-L10N ask + owner: FrontComposer + Tenants author; shell-vs-Tenants string ownership, `FcShellResources.resx`)
- [Source: _bmad-output/contracts/fc-lyt-page-layout-2026-06-03.md] (FC-LYT contract — structure/tone to mirror; escalate-with-owner precedent)
- [Source: _bmad-output/contracts/fc-a11y-accessibility-primitives-2026-06-03.md] (FC-A11Y contract — most recent same-shape precedent)
- [Source: _bmad-output/implementation-artifacts/1-3-establish-fc-a11y-accessibility-primitives-as-a-ready-gate.md] (previous story; confirm-and-pin shape, pre-existing-failure baseline, docs discipline, YOLO escalation, the `IStringLocalizer<FcShellResources>` forward-reference to 1.4)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.cs:9-13] (marker class + resx BaseName convention)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx + FcShellResources.fr.resx] (157 EN + 157 FR keys, at parity)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:432-465] (`AddHexalithShellLocalization`, deliberate no-`AddLocalization`, `services.Replace` swap-seam)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:491-511] (`AddHexalithFrontComposerQuickstart` chains localization)
- [Source: src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs:645-662] (`ShellRequestLocalizationOptionsSetup` → DefaultRequestCulture/SupportedCultures from `FcShellOptions`)
- [Source: src/Hexalith.FrontComposer.Contracts/FcShellOptions.cs:134-150] (`DefaultCulture` `"en"`, `SupportedCultures` `["en","fr"]`, BCP-47 + must-include-default invariant)
- [Source: src/Hexalith.FrontComposer.SourceTools/Transforms/FormFieldModel.cs:39,62] (`[Display(Name)]` wins, else runtime per-type `IStringLocalizer` — the host/domain side of the boundary)
- [Source: src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs:98,105] (generated form injects `IStringLocalizer<TCommand>` for domain labels + `IStringLocalizer<FcShellResources>` for shell-owned auth warnings — the boundary in code)
- [Source: src/Hexalith.FrontComposer.Shell/Resources/DevMode/DevModeStrings.resx] (separate dev-only shell resource, consumed by `FcDevModeOverlay.razor:10`)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityPreviewPanel.razor:26-28] (illustrative sample-data column titles — the open scope question for AC3)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs:18-51,365-371] (parity + culture round-trip + `BuildLocalizedProvider` pattern to reuse; standing evidence)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Options/FcShellOptionsValidationTests.cs] (culture-config invariant evidence)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story13AccessibilityPrimitivesTests.cs] (most recent same-shape story tests; localizer-assertion + render-and-query style)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/LayoutComponentTestBase.cs] (Fluxor store + theme mock + JS stubs base; verify its localizer registration before reusing)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs] (registry/viewport setup for the has-navigation render)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules + #Testing Rules] (localization wiring order, IStringLocalizer, test discipline, DiffEngine_Disabled, solution-level test + trait filter)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Claude Opus 4.8, 1M context) — bmad-create-story workflow (story authoring); bmad-dev-story workflow (implementation)

### Debug Log References

- `dotnet build -c Release Hexalith.FrontComposer.slnx` — **clean, 0 warnings, 0 errors** (TWAE). One transient parse error during authoring (`</content>` sentinel accidentally trailing the new test file) was removed before the final build.
- `DiffEngine_Disabled=true dotnet test … --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` — full filtered lane: **13 failures, all matching the documented Story 1.1–1.3 pre-existing baseline** (8 Shell + 3 SourceTools + 2 Cli), reproduced identically at `df37313` — **NOT regressions**. New `Story14ShellStringOwnershipTests` (**4 tests** after the QA-automation pass — FR header chrome, EN converse, FR skip-links + nav landmark, swap-seam) **green**; Shell.Tests passed-count rose to **1681** (re-confirmed `Failed: 8, Passed: 1681` filtered lane — the 8 Shell baseline failures unchanged).
  - 8 Shell baseline: `PendingStatusReopenGovernanceTests`×4, `NavigationEffectsLastActiveRouteTests.HandleAppInitialized_StoredRoute…`×1, Generated snapshot×3 (`CommandRendererFullPageTests`×1, `CounterStoryVerificationTests`×2).
  - 3 SourceTools: `DiagnosticRegistryTests.Story112_…`, `CommandFormEmitterTests.Emit_DoesNotLogModelInstance`, `IdeParityConformanceUtilityTests.EvidencePathNormalization_…`.
  - 2 Cli: `MigrationCommandTests.ProjectSelection_…`×2.
- Must-stay-green suites focused run — `FcShellResourcesTests`, `FcShellOptionsValidationTests`, `FrontComposerShellParameterSurfaceTests` (the locked 7-parameter surface — **untouched + green**), `Story12PageLayoutTests`, `Story13AccessibilityPrimitivesTests`, `Story14ShellStringOwnershipTests`: **136 passed, 0 failed**.

### Completion Notes List

- **Task 1 (DECISION/DOC):** Authored `_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md`, mirroring the FC-LYT/FC-A11Y contract structure (front-matter, decision-deliverable intro, ownership table, mechanism section, Confirmation, FC-DOC linkage, References). Documents the **two-localizer split** as the contract (shell chrome → `IStringLocalizer<FcShellResources>`; domain/host text → host's per-type `IStringLocalizer<T>` / `[Display(Name)]`; app/tenant text → host-owned), names the resx-BaseName convention + `AddHexalithShellLocalization` seam + `services.Replace` swap, records the `FcShellOptions` supported-culture surface (`["en","fr"]`, BCP-47-lite, must-include-default), and scopes the `FcDensityPreviewPanel` sample-column titles as **recommended out-of-scope**. No `docs/` writes (BMAD contract under `_bmad-output/`).
- **Task 2 (CODE):** Added `Story14ShellStringOwnershipTests.cs` extending `LayoutComponentTestBase`. Confirmed the base already supplies a **real** embedded-resx-backed `IStringLocalizer<FcShellResources>` via `AddHexalithFrontComposerQuickstart()` (`AddLocalization()` + `AddHexalithShellLocalization()`) — so **no double-registration**; the test resolves the real localizer and toggles `CurrentUICulture` in try/finally (mirroring `FcShellResourcesTests.ThemeToggleAriaLabelResolvesInFrench`). AC2 test renders `FrontComposerShell` under `fr` and asserts palette/settings `aria-label` + theme `Title` resolve to **FR** resx values and **not** the EN literals (the no-hard-coded-English guard); a converse `en` test proves culture-sensitivity (not literals frozen to one locale). No `[Parameter]` added — the locked 7-parameter surface is untouched. A subsequent QA-automation pass (`bmad-qa-generate-e2e-tests`, summary at `_bmad-output/implementation-artifacts/tests/1-4-test-summary.md`) added **2** more tests to the same file — broadening the AC2 "no hard-coded English chrome" guard onto **skip-links + the nav landmark** (`SkipToContentLabel`/`SkipToNavigationLabel`/`NavMenuAriaLabel`, FR≠EN), and **executably pinning AC1's `services.Replace` swap seam** (a `SentinelLocalizer` returns `SWAP::{key}` and the rendered chrome must source from it) — bringing the file to **4 tests**.
- **Task 3 (parity/confirm):** Re-ran the standing L10N evidence (`FcShellResourcesTests` 157-key EN↔FR parity, round-trip, per-key values, NBSP/placeholder guards; `FcShellOptionsValidationTests` culture invariants) — **green**, unmodified, not duplicated. Per AC3 (YOLO, no live Tenants-author confirmation) the contract is `status: escalated`, `owner: FrontComposer + Tenants author (pending)`, with two open boundary items listed (density-preview sample-string scope; host-owned domain-label localizer with no shell fallback). FC-DOC cross-link queued for Story 1.5.
- **Task 4 (DoD):** Release build clean (0 warnings); filtered lane re-confirms the 13-failure baseline unchanged; new test green. No `src/` changes — resx, localizer wiring, culture options, and parity tests all pre-existed.
- **AC coverage:** AC1 — ownership boundary documented (table) + mechanism named (resx BaseName + `IStringLocalizer<FcShellResources>` + `services.Replace`), and the swap seam **executably pinned** by the QA-added swap-seam test (was doc-only). AC2 — non-default-culture shell-frame render pinned by `Story14ShellStringOwnershipTests` (FR != EN) across **6** chrome categories (palette/settings/theme + skip-to-content/skip-to-nav/nav landmark). AC3 — escalated with named owner, open items listed, FC-DOC link deferred to Story 1.5.

### File List

- `_bmad-output/contracts/fc-l10n-shell-string-ownership-2026-06-03.md` (new — FC-L10N ownership contract doc)
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/Story14ShellStringOwnershipTests.cs` (new — consolidated shell-frame culture-render ready-gate, 4 tests: FR header chrome, EN converse, FR skip-links + nav landmark, swap-seam)
- `_bmad-output/implementation-artifacts/tests/1-4-test-summary.md` (new — QA automation summary; recorded the 2 QA-added tests + the contract-doc sentinel fix)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — story 1-4 ready-for-dev → in-progress → review)
- `_bmad-output/implementation-artifacts/1-4-establish-fc-l10n-shell-string-ownership.md` (modified — task checkboxes, Dev Agent Record, Change Log, Status)

## Change Log

| Date | Change |
|---|---|
| 2026-06-03 | Story 1.4 implemented (dev-story): authored FC-L10N shell-string-ownership contract (`_bmad-output/contracts/`), added consolidated `Story14ShellStringOwnershipTests` culture-render ready-gate (AC2), confirmed standing L10N parity/round-trip evidence green, escalated the ownership boundary with named owner (AC3). No `src/` changes. Status → review. |
| 2026-06-03 | QA-automation pass (`bmad-qa-generate-e2e-tests`): extended `Story14ShellStringOwnershipTests` from 2 → 4 tests (skip-links + nav-landmark FR render; `services.Replace` swap-seam pin) and removed stray authoring sentinels from the contract doc. Summary at `_bmad-output/implementation-artifacts/tests/1-4-test-summary.md`. |
| 2026-06-03 | Review (story-automator-review): verified all ACs against source (build clean, 4/4 new tests green, 138/138 must-stay-green, Shell.Tests 1681 passed / 8 unchanged baseline). Auto-fixed documentation-integrity gaps — File List now lists the QA test-summary, stale "2 tests / 1679" counts corrected to "4 tests / 1681", removed a stray `</content>` sentinel from the test-summary. 0 critical issues → Status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot · **Date:** 2026-06-03 · **Workflow:** bmad-story-automator-review (adversarial, auto-fix) · **Outcome:** ✅ Approved (Status → done)

### What was validated against source (not just claimed)

- **AC1 — ownership boundary + mechanism.** Contract documents the two-localizer split (table) and names the resx BaseName convention, `AddHexalithShellLocalization` (deliberate no-`AddLocalization`), and the `services.Replace` swap seam. The seam is now **executably** pinned (`FrontComposerShell_WhenLocalizerReplaced_ResolvesChromeFromTheSwappedLocalizer`). The localizer is registered **scoped** (`AddLocalization()` default), so the swap genuinely takes effect at render time — verified.
- **AC2 — non-default-culture render.** All 6 chrome keys (`PaletteTriggerAriaLabel`, `SettingsTriggerAriaLabel`, `ThemeToggleAriaLabel`, `SkipToContentLabel`, `SkipToNavigationLabel`, `NavMenuAriaLabel`) exist in both `FcShellResources.resx` and `.fr.resx` with genuinely differing FR values; every DOM selector the test queries is really set by the shell via `IStringLocalizer<FcShellResources>` (`FcPaletteTriggerButton.razor:12-13`, `FcSettingsButton.razor:13-15`, `FrontComposerNavigation.razor:32-33`, `FrontComposerShell.razor:29,32`, `FcThemeToggle.razor:14`). `LayoutComponentTestBase` supplies the real embedded-resx localizer via `AddHexalithFrontComposerQuickstart()` — no stub, no double-registration.
- **AC3 — confirm/escalate.** Contract `status: escalated`, `owner: FrontComposer + Tenants author (pending)`, two open boundary items listed, FC-DOC cross-link deferred to Story 1.5. No `docs/` writes.
- **Task audit.** Every `[x]` task verified done against source/tests; no false-complete claims. No `src/` changes — resx, localizer wiring, culture options, parity tests all pre-existed (confirmed via git). Locked 7-parameter shell surface untouched (`FrontComposerShellParameterSurfaceTests` green).
- **Build & tests.** `dotnet build -c Release` → 0 warnings / 0 errors. `Story14ShellStringOwnershipTests` → 4/4 green. Must-stay-green suites → 138/138. Filtered Shell.Tests lane → 1681 passed / 8 failed (the documented Story 1.1–1.3 baseline, unchanged — NOT regressions).

### Findings (all auto-fixed; 0 critical)

| Sev | Finding | Resolution |
|---|---|---|
| MEDIUM | `_bmad-output/implementation-artifacts/tests/1-4-test-summary.md` (new) absent from the story File List | Added to File List |
| MEDIUM | File List / Debug Log / Completion Notes / Change Log described "2 tests / passed-count 1679" but the file holds **4** tests and the measured count is **1681** (QA pass augmented without writing back) | Counts corrected; Change Log entry added for the QA pass |
| LOW | Stray `</content>` authoring sentinel at `1-4-test-summary.md:73` (same defect class the QA pass claimed to fix in the contract) | Removed |

No security, performance, or correctness issues found in the test or contract. The implementation is faithful to a confirm-and-pin ready-gate story.
