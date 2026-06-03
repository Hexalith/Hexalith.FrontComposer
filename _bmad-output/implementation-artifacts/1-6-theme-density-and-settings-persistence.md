---
baseline_commit: c88b362
---

# Story 1.6: Theme, density, and settings persistence

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

> **🧱 Brownfield + confirm-and-pin reality — read this first.** This is the **final story of Epic 1**
> and it **closes the Shell-Foundation epic**. Unlike the four prior confirm-and-document ready-gates
> (FC-LYT 1.2 / FC-A11Y 1.3 / FC-L10N 1.4 / FC-DOC 1.5), Story 1.6 is a **functional** story
> (FR15 + NFR6 + NFR9) — **but the entire theme/density/settings stack already ships, fully wired and
> already covered by tests** at baseline `c88b362`. The epics file is explicit that *"Most FR/NFR items
> describe capability that is ALREADY BUILT"* — FR15 (theme/density/settings) is one of them. **Your job
> is therefore to CONFIRM-AND-PIN the three ACs against the real, existing implementation, not to build
> new components.** Do **not** reinvent the dialog, the Fluxor slices, the persistence effects, the
> aria-live announcer, or the JS-interop applier — they exist. Author the confirming/pinning artifact
> (a governance pin test in the `Shell.Tests` lane and/or a settings-persistence contract note under
> `_bmad-output/contracts/`), verify the end-to-end behavior holds, and close Epic 1.
>
> **Decisive constraint that will bite if ignored — the NFR17 tripwire.** Project-context: *"a new
> `IStorageService.SetAsync` call site in `Shell/State/` requires updating the tripwire whitelist + the
> story compliance matrix."* `NFR17ComplianceTripwireTests` hard-asserts the **SetAsync call-site count
> == 7** and that every persisted argument expression is allow-listed
> (`tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs:25-91`). The
> theme (`action.NewTheme`) and density (`value` / `(DensityLevel?`) write expressions are **already
> whitelisted**, and the count already includes them. **Because this story adds NO new persistence call
> site, the tripwire must stay green and the count must stay 7.** If you find yourself adding a
> `storage.SetAsync(...)` under `Shell/State/`, STOP — you are rebuilding something that exists.
>
> Concretely, at baseline `c88b362`:
> - **Theme slice** (`src/Hexalith.FrontComposer.Shell/State/Theme/`) — complete: `ThemeValue`
>   (`Light`/`Dark`/`System`), `FrontComposerThemeState`, `ThemeActions` (`ThemeChangedAction` +
>   hydration actions), `ThemeReducers`, `ThemeEffects`, `FrontComposerThemeFeature`. The **single
>   effect that owns BOTH persistence AND JS interop** is `ThemeEffects.HandleThemeChanged`
>   (`State/Theme/ThemeEffects.cs:108-130`) — it applies via `IThemeService.SetThemeAsync(...)` *and*
>   persists via `storage.SetAsync(key, action.NewTheme)` under key `{tenantId}:{userId}:theme`.
> - **Density slice** (`src/Hexalith.FrontComposer.Shell/State/Density/`) — complete: `DensityLevel`
>   lives in **Contracts** (`Contracts/Rendering/DensityLevel.cs`: `Compact`/`Comfortable`/`Roomy`),
>   plus `FrontComposerDensityState`, `DensityActions`, `DensityReducers`, `DensityEffects`,
>   `DensityPrecedence` (pure resolver), `FrontComposerDensityFeature`. **Persistence single-writer:**
>   `DensityEffects.HandleUserPreferenceChanged` / `HandleUserPreferenceCleared` → `PersistAsync` →
>   `storage.SetAsync(key, value)` under key `{tenantId}:{userId}:density`
>   (`State/Density/DensityEffects.cs:165-203`). **DOM/JS-interop single-writer:** the headless
>   `FcDensityApplier` component mirrors `EffectiveDensity` onto `<body data-fc-density>` via
>   `fc-density.js#setDensity` (ADR-041 — interop is deliberately NOT in the effect).
> - **Settings UI** (`src/Hexalith.FrontComposer.Shell/Components/Layout/`) — `FcSettingsDialog`
>   (density `FluentRadioGroup` + embedded `<FcThemeToggle/>` + `<FcDensityPreviewPanel/>` + Restore-
>   defaults/Done footer), `FcThemeToggle`, `FcDensityPreviewPanel`, `FcSettingsButton`,
>   `FcSettingsDialogLauncher`, `FcDensityAnnouncer` (aria-live), `FcDensityApplier`. **Opened two ways
>   that share one launcher:** the header `FcSettingsButton` and the **`Ctrl+,` / `meta+,` shortcut**
>   (`Shortcuts/FrontComposerShortcutRegistrar.cs:91-99,210-216` → `OpenSettingsAsync` →
>   `FcSettingsDialogLauncher.ShowAsync`).
> - **Tests already exist** for every piece (Theme feature/reducers/effects/scope + `FcThemeToggle`;
>   Density feature/reducers/effects/scope/precedence/persistence-snapshot + applier/announcer/preview;
>   `FcSettingsDialog`/`FcSettingsButton`/`FcSettingsDialogLauncher`; the NFR17 tripwire and the shortcut
>   registrar). See **Existing tests** below.
>
> So Story 1.6 is **(1) confirm** the three ACs hold against the existing stack by reading the real
> source; **(2) pin** the cross-cutting AC3 single-writer invariant + the AC1/AC2 wiring with a
> governance/bUnit test *if a gap exists* (prefer extending an existing test over a new file); **(3)
> record** the FR15/NFR9 settings-persistence confirmation (and any genuinely-open item) in a short
> contract note under `_bmad-output/contracts/`, mirroring the 1.2–1.5 escalate-with-owner precedent;
> **(4) close Epic 1** by leaving the build clean and the test baseline unchanged. Do **NOT** add a new
> `SetAsync` call site, **NOT** change `CanonicalSchemaMaterial`/baselines, **NOT** touch the locked
> `FrontComposerShell` 7-parameter surface, and **NOT** `feat:` a non-shipping confirm story.

## Story

As an operator,
I want to set theme and density in a settings dialog and have it persist,
so that the shell remembers my display preferences across sessions.

## Acceptance Criteria

**AC1 — The settings dialog opens via `Ctrl+,` or the settings button and exposes density radio group + theme toggle + density preview. *(FR15)***
**Given** the shell is running,
**When** I press `Ctrl+,` (or `meta+,` on macOS) or activate the header settings button,
**Then** `FcSettingsDialog` opens (both entry points routing through the single `FcSettingsDialogLauncher.ShowAsync`) showing a density `FluentRadioGroup` (Compact / Comfortable / Roomy), the embedded `FcThemeToggle`, and the `FcDensityPreviewPanel` live preview.

**AC2 — Theme and density persist and are restored from `IStorageService` on reload, with density changes announced via the aria-live announcer. *(FR15, NFR6)***
**Given** I change theme or density in the dialog (changes are live — no Apply/Cancel, per the epic AC),
**When** the app reloads,
**Then** the chosen theme and `data-fc-density` are restored from `IStorageService` (`LocalStorageService`) on app-init hydration (keys `{tenantId}:{userId}:theme` and `{tenantId}:{userId}:density`),
**And** density changes are announced through the `aria-live` density announcer (`FcDensityAnnouncer`, `role="status"` + `aria-live="polite"`, first-render announcement suppressed per WCAG).

**AC3 — The Theme and Density Fluxor slices satisfy single-writer discipline (ADR-007). *(NFR9)***
**Given** the Theme and Density slices,
**When** a preference changes,
**Then** exactly one writer owns each side effect: **theme** — `ThemeEffects.HandleThemeChanged` is the single effect owning *both* persistence and JS interop (`IThemeService`); **density** — `DensityEffects` (`HandleUserPreferenceChanged` / `HandleUserPreferenceCleared`) is the single persistence writer, and the headless `FcDensityApplier` is the single `data-fc-density` DOM/JS-interop writer (ADR-041 split), with `CurrentTheme` / `UserPreference` each mutated by exactly one action type. The NFR17 tripwire stays green (SetAsync call-site count == 7; theme/density write expressions already whitelisted).

## Tasks / Subtasks

- [x] **Task 1 — Confirm the three ACs against the real implementation (AC: #1, #2, #3) — read before you write**
  - [x] **AC1 wiring** — verify both open paths land on `FcSettingsDialog` via the one launcher: header `FcSettingsButton` (`Components/Layout/FcSettingsButton.razor[.cs]` → `FcSettingsDialogLauncher.ShowAsync`) and the `ctrl+,`/`meta+,` shortcut (`Shortcuts/FrontComposerShortcutRegistrar.cs:91-99` register → `:210-216` `OpenSettingsAsync` → `ShowAsync`). Confirm `FcSettingsDialog.razor` renders the density `FluentRadioGroup` (`:22-30`), the embedded `<FcThemeToggle/>` (`:41`), and `<FcDensityPreviewPanel/>` (`:44-45`), with `data-testid="fc-settings-dialog"`. **CONFIRMED** — dialog renders all three under `data-testid="fc-settings-dialog"`; both `ctrl+,` and `meta+,` register `OpenSettingsAsync` (registrar :91-99).
  - [x] **AC2 persistence + restore** — verify the write side: density `DensityEffects.PersistAsync` → `storage.SetAsync(key, value)` (`State/Density/DensityEffects.cs:185-203`) on `UserPreferenceChangedAction`/`UserPreferenceClearedAction`; theme `ThemeEffects.HandleThemeChanged` → `storage.SetAsync(key, action.NewTheme)` (`State/Theme/ThemeEffects.cs:122-126`). And the restore side: both effects' `HandleAppInitialized` + `HandleStorageReady`-when-`Idle` hydrate from the same scoped keys (`StorageKeys.BuildKey(tenantId, userId, "{theme|density}")`). Note density's legacy-string→enum migration path (`ReadStoredPreferenceAsync`, `:233-253`) re-writes via `SetAsync((DensityLevel?)migrated)` — this is the **second whitelisted density expression** `(DensityLevel?` and is **already counted** in the tripwire; don't "fix" it. **CONFIRMED** — write/restore paths and keys exactly as described; migration re-write left untouched.
  - [x] **AC2 announcer** — verify `FcDensityAnnouncer` (`Components/Layout/FcDensityAnnouncer.razor[.cs]`) projects `EffectiveDensity` into an `aria-live="polite"` / `role="status"` region and suppresses the first-render announcement. **CONFIRMED** — `role="status"` + `aria-live="polite"` + `aria-atomic="true"`; gated by `_hasAnnouncement` (first render suppressed).
  - [x] **AC3 single-writer** — verify the split documented above: theme effect owns persist+interop together; density splits persist (effect) from `data-fc-density` interop (`FcDensityApplier`, ADR-041); each axis's state field has exactly one mutating action type. Run the existing scope tests (`ThemeEffectsScopeTests`, `DensityEffectsScopeTests`) and the NFR17 tripwire to confirm. **CONFIRMED with one wording caveat** — persistence single-writer holds per slice; theme DOM apply (`SetThemeAsync`) has 3 legitimate invokers (effect/shell-reapply/system-watcher) and density `UserPreference` is written by 4 reducers, so the literal "one effect owns persist+interop / one action per field" wording is escalated (see below).
  - [x] **Save any genuinely-open question for the end** (e.g. whether the AC3 "one effect owns persistence **and** JS interop" wording is satisfied by density's deliberate persist/interop split — see the **AC3 interpretation** note in Dev Notes; surface it, do not silently redesign). **SURFACED** — recorded as ESCALATED (owner: FrontComposer pending) in the confirmation note's Confirmation section; recommended reading = "one persistence writer per slice + one DOM writer per side-effect."

- [x] **Task 2 — Pin the ACs with a governance / bUnit test (AC: #1, #2, #3) — the enforcement deliverable**
  - [x] **First check coverage, then fill only real gaps.** Inventory the existing tests (see **Existing tests**). For each AC, confirm there is an enforcing test; if one already covers it, **reference it — do not duplicate**. Prefer **extending an existing test class** over adding a new file (mirrors 1.2–1.5: a small, targeted pin, not a parallel suite). **DONE** — AC1/AC2 already fully covered (FcSettingsDialog/Button/Launcher/ShortcutRegistrar + Theme/Density effects+scope + Announcer/Applier tests); not duplicated.
  - [x] **AC3 cross-cutting pin (most likely the genuine gap).** If not already pinned, add a governance test asserting the **single-writer invariant** for both slices — e.g. that `CurrentTheme` is only assigned in the `ThemeChangedAction` reducer and `UserPreference` only in the `UserPreferenceChanged`/`Cleared` reducers, and/or that persistence `SetAsync` lives in exactly one effect per slice. The **NFR17 tripwire** (`NFR17ComplianceTripwireTests`) already enforces the "no new persistence writer" half — **keep its count at 7 and the whitelist unchanged.** **DONE** — added `Architecture/SliceSingleWriterGovernanceTests.cs` (3 facts: theme persistence single SetAsync in ThemeEffects; density SetAsync all in DensityEffects; `CurrentTheme` single reducer). Pinned only the literally-true invariants (deliberately NOT "UserPreference one reducer", which is false). NFR17 stays at 7.
  - [x] **Any new/changed test lives in `tests/` (never `src/`).** Test files are plural `{Class}Tests.cs`; methods are three-part `Subject_Scenario_Expectation`; xUnit **v3** + **Shouldly** (`ShouldBe`, never raw `Assert.*`) + **bUnit** for component render (use `GeneratedComponentTestBase`/`AddFrontComposerTestHost` with `JSInterop.Mode = Loose`). If you snapshot, use `Verify.XunitV3` and commit `.verified.txt` intentionally. **DONE** — new file is `tests/`-only, plural-named, three-part methods, Shouldly `ShouldBe`. No snapshot needed (source-scan governance test mirroring NFR17 style).

- [x] **Task 3 — Record the FR15/NFR9 settings-persistence confirmation (AC: #1, #2, #3) — the close-out artifact**
  - [x] Author a short confirmation note under `_bmad-output/contracts/` (e.g. `fc-settings-persistence-2026-06-03.md`) **mirroring the structure/tone of the 1.2–1.5 contracts** (front-matter `title`, `date: '2026-06-03'`, `story: '1.6'`, `status`, `owner`, `supersedes`; a "Decision deliverable" intro; the confirmed behavior; a **Confirmation** section; a **References** section). State the confirmed facts: storage keys + scoping, the live-no-Apply UX, the theme persist+interop unity, the density persist/interop split (ADR-041), the aria-live announcer, and the NFR17 tripwire as the standing guard. This is a BMAD artifact under `_bmad-output/` — **not** a published `docs/` page (FC-DOC's settings page remains the tracked gap owned by this story per 1.5's component status map; if low-friction and Gate 2d stays green you MAY also author/close `docs/reference/components/settings.md`, but it is optional, not required by these ACs). **DONE** — `_bmad-output/contracts/fc-settings-persistence-2026-06-03.md` authored. Settings docs page left as the tracked, optional gap (not required by these ACs).
  - [x] **Confirmation (YOLO):** if no live reviewer is available, mark `status: confirmed` where the behavior is fully verified by existing+new tests (the stack ships and is tested — this is stronger evidence than the 1.2–1.4 escalations had), or `escalated` with owner **"FrontComposer (pending)"** for any item that genuinely needs a human decision (e.g. the AC3 split-interpretation). Surface open items; don't resolve them silently. **DONE** — front-matter `status: confirmed` (behaviour verified); the single AC3-wording sub-item recorded as ESCALATED with owner "FrontComposer (pending)".

- [x] **Task 4 — Build clean + confirm the test baseline is unchanged (DoD)**
  - [x] **Build clean:** `dotnet build -c Release Hexalith.FrontComposer.slnx` — **0 warnings** (TWAE). If you added a `tests/`-only pin, there is no `src/` public-API / `PublicAPI.Shipped.txt` obligation; confirm the build is still clean. **DONE** — `Build succeeded. 0 Warning(s) 0 Error(s)`.
  - [x] **Run the lane:** `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx -c Release --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"`. **Re-measure the pre-existing-failure baseline at HEAD `c88b362` first** (prior Epic-1 stories documented **13** pre-existing full-lane failures at `251a0b5`: 8 Shell — `PendingStatusReopenGovernanceTests`×4, `NavigationEffectsLastActiveRouteTests`×1, Generated snapshot×3 — + 3 SourceTools + 2 Cli; 1.5 was docs/tests-only so the `src` failures should be unchanged, but **measure, don't assume**). Your new pin tests must **pass**, and you must introduce **zero new failures**. If a failure looks new, stash and compare to `c88b362` before chasing it. **DONE** — full lane = exactly 13 pre-existing failures (Shell 8 = `PendingStatusReopenGovernanceTests`×4 + `NavigationEffectsLastActiveRouteTests`×1 + Generated snapshot×3; SourceTools 3; Cli 2). New 3 pin tests PASS. **Zero new failures.**
  - [x] **NFR17 tripwire stays green:** `NFR17ComplianceTripwireTests` (`BlobDoesNotCarryEntityData` + `SetAsyncCallSiteCount_MatchesExpected` == 7) must both pass unchanged — proof you added no new persistence writer. **DONE** — both pass; count == 7 unchanged.
  - [x] Run `/bmad-code-review` before flipping to done (mandatory per story). **DONE** — story-automator-review (adversarial) executed 2026-06-03: ACs 1–3 confirmed against real source; pin tests + NFR17 re-run green (5/5, count==7); e2e typecheck clean and selectors grounded. 1 MED auto-fixed (File List omitted the 3 QA-automation files), 1 LOW (Change Log) folded in. 0 CRITICAL → review → done.

## Dev Notes

### What already exists vs. what's new

| Concern | State today (baseline `c88b362`) | This story |
|---|---|---|
| Theme slice (Value/State/Actions/Reducers/Effects/Feature/Hydration) | **Exists + tested** (`State/Theme/`) | **Confirm + pin** — do NOT rebuild |
| Density slice (State/Actions/Reducers/Effects/Feature/Precedence/Hydration) | **Exists + tested** (`State/Density/`; `DensityLevel` in Contracts) | **Confirm + pin** — do NOT rebuild |
| `FcSettingsDialog` (+ `FcThemeToggle`, `FcDensityPreviewPanel`) | **Exists + tested** (`Components/Layout/`) | **Confirm render surface** — do NOT modify |
| Open paths: `FcSettingsButton` + `Ctrl+,`/`meta+,` shortcut → one launcher | **Exists + tested** (`Shortcuts/FrontComposerShortcutRegistrar.cs`) | **Confirm both route through `FcSettingsDialogLauncher.ShowAsync`** |
| Persistence: `IStorageService`/`LocalStorageService` | **Exists** (`Contracts/Storage/IStorageService.cs`; `Shell/Infrastructure/Storage/LocalStorageService.cs`) | **Confirm keys + restore** — add NO new `SetAsync` call site |
| `data-fc-density` DOM write (`FcDensityApplier` + `fc-density.js`) | **Exists** (ADR-041, headless applier) | **Confirm single DOM writer** |
| aria-live density announcer (`FcDensityAnnouncer`) | **Exists + tested** | **Confirm announce + first-render suppression** |
| NFR17 tripwire (call-site count == 7, whitelist) | **Exists + green** (`tests/.../Architecture/NFR17ComplianceTripwireTests.cs`) | **Keep green — do NOT bump count or add a writer** |
| FR15/NFR9 settings-persistence confirmation note | **Does NOT exist** | **Author it** under `_bmad-output/contracts/` |
| AC3 single-writer governance pin (if gap) | **Partially** (scope tests + tripwire) | **Add only the missing assertion** |

### The story's job (encode in the confirmation note)

The story's first job is a **confirmation/declaration over already-shipping behavior**, plus a thin enforcing pin — not new components. Recommended, carry unless reviewers override:

- **The settings contract is "live-no-Apply + tenant/user-scoped persistence + restore-on-init."** Changes in `FcSettingsDialog` take effect immediately (no Apply/Cancel — `FcSettingsDialog.razor:10-13`, epic AC §126); they persist under `{tenantId}:{userId}:{theme|density}` and rehydrate on `AppInitializedAction` (and re-hydrate on `StorageReadyAction` iff hydration is still `Idle`). **Naming + confirming that *is* the deliverable.**
- **AC3 interpretation (the one genuinely-open nuance — surface it).** AC3 says *"exactly one effect owns persistence + JS interop."* That is literally true for **theme** (`HandleThemeChanged` does both). For **density** it is satisfied by a deliberate **split** (ADR-041): the **effect** owns persistence; a **single headless applier component** (`FcDensityApplier`) owns the `data-fc-density` JS interop, because the DOM write must mirror `EffectiveDensity` (which also changes on viewport-tier recompute, a non-persisting path). Record this as the confirmed reading: "exactly one writer per side effect per axis," not "one effect does everything." Escalate-with-owner only if a reviewer must ratify the wording.
- **Confirm-or-pin, don't redesign.** The dialog, slices, effects, applier, announcer, launcher, shortcut, and tests all exist. This story *confirms and pins*; the only genuinely open item is the AC3 split-interpretation.

### Must-not-break (regression surface)

A ready-gate story must leave the system working end-to-end. Preserve:

- **The NFR17 tripwire stays green** — `SetAsyncCallSiteCount_MatchesExpected` == **7** and `BlobDoesNotCarryEntityData` whitelist unchanged. Adding a `storage.SetAsync(...)` under `Shell/State/` (or persisting a non-whitelisted expression) **breaks CI**. This story adds none.
- **Single-writer discipline (ADR-007)** — do not add a second dispatch source for `CurrentTheme` or `UserPreference`, and do not move density's `data-fc-density` interop out of `FcDensityApplier` or theme's interop out of `HandleThemeChanged`.
- **The locked `FrontComposerShell` 7-parameter surface** and the shared `FcSettingsDialogLauncher` single-entry-point — both open paths must keep routing through it.
- **No `CanonicalSchemaMaterial` / fingerprint / baseline change** — not in play here; don't touch encoder/sentinel/comparer/`PublicAPI.Shipped.txt`/pacts/`.verified.txt` unless an intentional, reviewed snapshot update is part of a pin (then commit it intentionally).
- **`docs/` discipline** — the confirmation note is a BMAD artifact → `_bmad-output/contracts/`, NOT the CI-gated DocFX site. Only author `docs/reference/components/settings.md` if you deliberately close that FC-DOC tracked gap and Gate 2d stays green (optional).
- **No `src/` behavior change unless a pin genuinely requires it** — this is a confirm story; prefer `tests/` + `_bmad-output/`. If confirmation surfaces a real defect (cf. 1.5's stale `HeaderEnd` XML-doc), **file it as a Review Follow-up**, don't fix it inline.

### Single-writer / persistence facts (grounded in source)

- **Theme — one effect, both side effects.** `ThemeEffects.HandleThemeChanged` (`State/Theme/ThemeEffects.cs:108-130`): maps `ThemeValue`→`ThemeMode`, applies via `IThemeService.SetThemeAsync(new ThemeSettings(AccentColor, 0, 0, mode, true))`, then persists `storage.SetAsync(key, action.NewTheme)`. Scope-guarded by `TryResolveScope` (null/empty tenant|user → log `HFC2105`, skip persist but still apply). Hydrate: `HandleAppInitialized` + `HandleStorageReady` (only when `HydrationState == Idle`) → `GetAsync<ThemeValue?>` → re-dispatch `ThemeChangedAction`.
- **Density — persist in effect, interop in applier.** `DensityEffects.HandleUserPreferenceChanged`/`HandleUserPreferenceCleared` → `PersistAsync(value|null)` → `storage.SetAsync(key, value)` (`:165-203`). `DensityHydratedAction` and `ViewportTierChangedAction` are **intentionally excluded** from persist (ADR-038/ADR-040 — hydrate and tier-force are read-only/compute paths). Legacy migration in `ReadStoredPreferenceAsync` (`:233-253`) re-writes once via `SetAsync((DensityLevel?)migrated)` (the `(DensityLevel?` whitelisted expression). `FcDensityApplier` (`Components/Layout/FcDensityApplier.razor.cs`) imports `fc-density.js` on first render and calls `setDensity(EffectiveDensity)` on every change; module-load failure is non-fatal (CSS defaults).
- **Two whitelisted density expressions, one effect file.** The tripwire whitelist has `value` (direct write) and `(DensityLevel?` (migration write) for density, plus `action.NewTheme` for theme — all already counted in the `== 7` total. Confirm you don't perturb this.
- **`SelectedDensity` setter runs the resolver before dispatch (ADR-039 — pure reducers).** `FcSettingsDialog.razor.cs:58-73` resolves `DensityPrecedence.Resolve(value, Options.DefaultDensity, DensitySurface.Default, NavState.CurrentViewport)` then dispatches `UserPreferenceChangedAction(ulid, value, newEffective)`. `RestoreDefaultsAsync` (`:97-107`) dispatches `UserPreferenceClearedAction` + `ThemeChangedAction(System)` together. `IsForcedByViewport` (`:80-90`) drives the inline `FluentMessageBar Info` note + the preview "preview only" badge (ADR-040).

### Existing tests (inventory before adding anything)

- **Theme** — `tests/.../State/Theme/ThemeFeatureTests.cs`, `ThemeReducersTests.cs`, `ThemeEffectsTests.cs`, `ThemeEffectsScopeTests.cs`; `tests/.../Components/Layout/FcThemeToggleTests.cs`.
- **Density** — `tests/.../State/Density/DensityFeatureTests.cs`, `DensityReducersTests.cs`, `DensityEffectsTests.cs`, `DensityEffectsScopeTests.cs`, `DensityPrecedenceTests.cs`, `DensityPersistenceSnapshotTests.cs`.
- **Settings UI** — `tests/.../Components/Layout/FcSettingsDialogTests.cs`, `FcSettingsButtonTests.cs`, `FcSettingsDialogLauncherTests.cs`, `FcDensityPreviewPanelTests.cs`, `FcDensityAnnouncerTests.cs`, `FcDensityApplierTests.cs`.
- **Governance / wiring** — `tests/.../Architecture/NFR17ComplianceTripwireTests.cs`, `tests/.../Shortcuts/FrontComposerShortcutRegistrarTests.cs`.
- **Implication:** AC1 (dialog render + both open paths) and AC2 (persist/hydrate/announce) are very likely **already covered**; the most probable genuine gap is a single **AC3 cross-cutting single-writer governance assertion**. Add only that, and only if missing.

### Previous story intelligence (Stories 1.2–1.5 — all `done`)

- **Shape to mirror:** every Epic-1 story is a brownfield **confirm-and-document/pin** ready-gate — author the contract/note in `_bmad-output/contracts/`, escalate-or-confirm with a named owner, surface (don't silently resolve) the one genuinely open item, keep the change additive (`tests/` + `_bmad-output/`), and leave the build + the documented failure baseline untouched. 1.6 continues that shape — but it is the **functional** capstone, so the enforcement is a **bUnit/governance pin in the `dotnet test` lane**, not a docs validator.
- **1.5 explicitly named Story 1.6 as the owner of the *settings* component-doc decision** (FC-DOC component status map: "Settings … FrontComposer (Story 1.6 finalizes settings UX)"). You MAY close `docs/reference/components/settings.md` here (optional, Gate-2d-conforming) or keep it a tracked gap — these ACs don't require the published page.
- **Pre-existing-failure baseline discipline:** 1.1–1.5 all recorded **13** full-lane failures reproduced identically across `f40dece`→`c88b362`; these are **NOT regressions**. A confirm story with a `tests/`-only pin won't change `src` behavior — re-measure at `c88b362`, then assert "zero new failures."
- **YOLO confirm/escalate is acceptable** — but note 1.6 has *stronger* evidence than 1.2–1.4 had (the behavior ships and is tested), so prefer `status: confirmed` for fully-verified facts and reserve `escalated` for the AC3 wording nuance.

### Git intelligence

- HEAD `c88b362` = Story 1.5 (`feat(story-1.5): Produce the FC-DOC component documentation contract`). The Epic-1 commit chain (`0db0fb0` spike → `f40dece` bootstrap → `68034f1` FC-LYT → `df37313` FC-A11Y → `251a0b5` FC-L10N → `c88b362` FC-DOC) is all additive confirm-and-document ready-gates; none added the theme/density/settings *stack* (it predates Epic 1 — built under the codebase's own internal "Story 3-x/5-2" numbering, visible in the source XML-docs). The BMAD epics here are **reverse-engineered** over that existing code, so 1.6's "Story 3-3/3-6/5-2" source references are the *internal* dev history, not BMAD story numbers — don't be confused by them.
- Working tree has one unrelated modified file (`_bmad-output/story-automator/orchestration-*.md`); leave it alone.
- Branch `feat/<desc>` (continue on `feat/story-1-2-fc-lyt-page-layout` or branch `feat/story-1-6-settings-persistence` — **never** commit to `main`). **Conventional Commit:** this story ships **no new product behavior** — it adds a `tests/` pin + a `_bmad-output/` confirmation note. Per project-context (`docs`/`test`/`chore` → no release; **never `feat` for a non-shipping change** — it would trigger a false minor NuGet publish), prefer **`test(story-1.6): …`** (if the deliverable is the governance pin) or **`docs(story-1.6): …`** (if it is the confirmation note). *Observed nuance:* the repo's prior Epic-1 commits used `feat(story-1.x): …` even for doc/test-leaning stories; the project-context rule still says non-shipping → not `feat`. Recommend `test(story-1.6):`.

### Latest tech / stack notes (no version churn expected)

- **No new packages, no version bumps.** All deps are centralized in `Directory.Packages.props`; this story adds none. FluentUI v5 RC (`5.0.0-rc.3-26138.1`, ADR-003) is referenced as prose only (`FluentDialogBody`, `FluentRadioGroup`, `FluentMessageBar`, `FluentButton`) — no new FluentUI API surface, pin untouched.
- **`ConfigureAwait(false)`** on every await; **ULIDs via `IUlidFactory`** (the dialog already uses `UlidFactory.NewUlid()` for correlation ids — never GUIDs). **Allman braces / file-scoped namespaces / `_camelCase` / `Async` suffix** per `.editorconfig` + TWAE.
- **Test stack:** xUnit v3 + Shouldly + NSubstitute + bUnit (`JSInterop.Mode = Loose`) + Verify (`Verify.XunitV3`); run with `DiffEngine_Disabled=true`. Solution-level `dotnet test` + trait filters (NOT per-project).

### Project-context rules that bite here

- **NFR17 tripwire** — *the* rule for this story: no new `Shell/State/` `SetAsync` call site without bumping the count + whitelist + compliance matrix. You add none → count stays 7.
- **Fluxor single-writer (ADR-007) + scoped-lifetime (ADR-030)** — effects own persistence + JS interop; reducers stay pure; never capture storage/effects/auth/tenant accessors in singletons.
- **`.slnx` only; centralized package versions; built-in analyzers only; no copyright headers; TWAE everywhere.**
- **`docs/` is the published, CI-gated DocFX site** — the confirmation note goes to `_bmad-output/contracts/`; a `settings.md` page (if authored) must pass Gate 2d.
- **Conventional Commits / semantic-release** — non-shipping confirm story → `test(story-1.6):` or `docs(story-1.6):`, **never `feat`**, **never commit to `main`**.
- **ULIDs / Fluxor / storage** in play; **source-generator / MCP / schema-fingerprint** rules are NOT in play (no generator/MCP/canonical-schema surface touched).

### Project Structure Notes

- New (expected): `_bmad-output/contracts/fc-settings-persistence-2026-06-03.md` (the confirmation note, beside FC-LYT/FC-A11Y/FC-L10N/FC-DOC); optionally one extended or new `tests/Hexalith.FrontComposer.Shell.Tests/...` pin (`tests/`, never `src/`); optionally `docs/reference/components/settings.md` (only if closing the FC-DOC gap, Gate-2d-conforming).
- **No `src/` change expected** — the theme/density/settings stack, the launcher, the shortcut, the applier, the announcer, and the locked `FrontComposerShell` surface all pre-exist. No structural variance from the dependency-down-to-`Contracts` rule (no new product types; `DensityLevel` already lives in Contracts). The contract-in-`_bmad-output` + pin-in-`tests/` split matches the 1.2–1.5 precedent.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6: Theme, density, and settings persistence] (story + 3 ACs)
- [Source: _bmad-output/planning-artifacts/epics.md#Epic 1: Shell Foundation & Bootstrap] (FR15 theme/density/settings; NFR6 a11y; NFR9 Fluxor single-writer; "most FR/NFR already built")
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:108-130] (single effect owns theme persist + IThemeService JS interop; key `{tenant}:{user}:theme`; hydrate at :40-100)
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:165-203,233-253] (density persist single-writer; ADR-038/040 exclusions; legacy migration `(DensityLevel?` write; key `{tenant}:{user}:density`)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor:15-68] (density radio + embedded FcThemeToggle + FcDensityPreviewPanel + Restore/Done; live-no-Apply; `data-testid="fc-settings-dialog"`)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor.cs:58-107] (SelectedDensity resolver-before-dispatch ADR-039; RestoreDefaults dispatches cleared+System; IsForcedByViewport ADR-040)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityApplier.razor.cs] (headless single DOM writer for `data-fc-density` via `fc-density.js#setDensity`; ADR-041; non-fatal module-load failure)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor (+.razor.cs)] (aria-live="polite" / role="status" density announcer; first-render suppression)
- [Source: src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:91-99,210-216] (`ctrl+,`/`meta+,` → OpenSettingsAsync → FcSettingsDialogLauncher.ShowAsync — shared with FcSettingsButton)
- [Source: src/Hexalith.FrontComposer.Contracts/Storage/IStorageService.cs + src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs] (GetAsync/SetAsync/RemoveAsync/GetKeysAsync/FlushAsync; localStorage drain + LRU)
- [Source: src/Hexalith.FrontComposer.Contracts/Rendering/DensityLevel.cs] (Compact/Comfortable/Roomy — in Contracts) + [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeValue.cs] (Light/Dark/System)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs:25-91] (whitelist incl. `action.NewTheme`/`value`/`(DensityLevel?`; SetAsync call-site count == 7 — keep green)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/{Theme,Density}/** + Components/Layout/Fc{SettingsDialog,SettingsButton,SettingsDialogLauncher,ThemeToggle,DensityPreviewPanel,DensityAnnouncer,DensityApplier}Tests.cs] (existing AC coverage — inventory before adding)
- [Source: _bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md] (FC-DOC status map: "Settings … Story 1.6 finalizes settings UX" — the tracked gap this story may close)
- [Source: _bmad-output/implementation-artifacts/1-5-produce-the-fc-doc-component-documentation-contract.md] (previous story; confirm-and-pin shape, escalate/confirm-with-owner under YOLO, Dev Agent Record/Change Log house style, the 13-failure pre-existing baseline)
- [Source: _bmad-output/project-context.md#Blazor Shell & Fluxor Rules + #Critical Don't-Miss Rules] (NFR17 tripwire; single-writer ADR-007; scoped-lifetime ADR-030; ULIDs; TWAE; Conventional Commits — never `feat` for non-shipping)

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context) — bmad-dev-story workflow

### Debug Log References

- `dotnet build -c Release Hexalith.FrontComposer.slnx` → `Build succeeded. 0 Warning(s) 0 Error(s)`.
- `DiffEngine_Disabled=true dotnet test … --filter "FullyQualifiedName~SliceSingleWriterGovernanceTests|FullyQualifiedName~NFR17ComplianceTripwireTests"` → **5 passed** (3 new pin facts + 2 NFR17 tripwire).
- Full default lane (`Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined`): Contracts 159✓, Mcp 291✓, Testing 11✓; **failures = Cli 2 + Shell 8 + SourceTools 3 = 13** — identical to the documented pre-existing baseline. New pin tests pass; **zero new failures**.

### Completion Notes List

- **Confirm-and-pin story (no `src/` change).** The theme/density/settings stack already shipped at baseline `c88b362`; all three ACs were confirmed by reading the real source — not rebuilt.
- **AC1 — CONFIRMED:** `FcSettingsDialog` renders the density `FluentRadioGroup` + embedded `FcThemeToggle` + `FcDensityPreviewPanel` under `data-testid="fc-settings-dialog"`; header `FcSettingsButton` and `ctrl+,`/`meta+,` both route through `FcSettingsDialogLauncher.ShowAsync`.
- **AC2 — CONFIRMED:** theme persists `action.NewTheme` and density persists `value`/`(DensityLevel?)` under scoped keys `{tenantId}:{userId}:{theme|density}`; both hydrate on `AppInitialized` + `StorageReady`-when-`Idle`; `FcDensityAnnouncer` is `role="status"`/`aria-live="polite"` with first-render suppression.
- **AC3 — CONFIRMED (persistence/DOM single-writer) + newly pinned; ESCALATED on literal wording.** Added `SliceSingleWriterGovernanceTests` asserting (a) exactly one theme `SetAsync` (in `ThemeEffects`), (b) all density `SetAsync` in `DensityEffects`, (c) `CurrentTheme` assigned by one reducer. Deliberately did **not** pin "`UserPreference` one reducer" (it has **4**) or "theme interop one effect" (`SetThemeAsync` has **3** legitimate invokers) — these are the escalated AC3-wording items (owner: FrontComposer pending), recorded in the contract note's Confirmation section. Recommended reading: *one persistence writer per slice + one DOM writer per side-effect*.
- **NFR17 tripwire stays green** — `SetAsyncCallSiteCount_MatchesExpected` == **7** unchanged, whitelist untouched; no new persistence writer added.
- **Conventional Commit:** non-shipping confirm story (`tests/` pin + `_bmad-output/` note) → use **`test(story-1.6):`** (never `feat`); commit on the feature branch, never `main`.
- ⏭️ **Next (review-phase gate):** run `/bmad-code-review` with a **different** LLM before flipping review → done (DoD Step 10). The dev-story workflow's terminal state is `review`; code-review/`story-automator-review` owns review → done (matches the 1.1–1.5 pattern). This is the one Task-4 subtask intentionally left unchecked.

### File List

- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SliceSingleWriterGovernanceTests.cs` — **new** (AC3/NFR9 per-slice single-writer governance pin).
- `tests/e2e/page-objects/settings.page.ts` — **new** (QA-automation: `SettingsPage` page object — both open paths, density radios, theme menu, live preview, aria-live announcer, scoped-`localStorage` helpers).
- `tests/e2e/specs/settings-persistence.spec.ts` — **new** (QA-automation: AC1 `Ctrl+,` open path + 3 controls; AC2 density/theme persist-across-reload + aria-live announce).
- `_bmad-output/contracts/fc-settings-persistence-2026-06-03.md` — **new** (FR15/NFR9 settings-persistence confirmation note).
- `_bmad-output/implementation-artifacts/tests/test-summary-1-6.md` — **new** (QA-automation E2E test summary for Story 1.6).
- `_bmad-output/implementation-artifacts/1-6-theme-density-and-settings-persistence.md` — **modified** (checkboxes, Dev Agent Record, File List, Change Log, Status).
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — **modified** (1-6 → in-progress → review → done).

## Change Log

| Date | Version | Description | Author |
|---|---|---|---|
| 2026-06-03 | 1.0 | Confirmed FR15/NFR6 theme/density/settings-persistence behaviour against the shipping stack; added `SliceSingleWriterGovernanceTests` AC3/NFR9 pin (NFR17 count stays 7); authored the FC-SETTINGS confirmation note (status: confirmed; AC3-wording sub-item escalated, owner FrontComposer pending); build clean + zero new test failures. Status → review. | Amelia (dev-story) |
| 2026-06-03 | 1.1 | Adversarial story-automator review: ACs 1–3 re-verified against real source; pin tests + NFR17 re-run green (5/5, count==7); e2e `tsc --noEmit` clean, selectors grounded. 1 MED auto-fixed (File List omitted `settings.page.ts`, `settings-persistence.spec.ts`, `test-summary-1-6.md`); Change Log/File List completed (LOW). 0 CRITICAL → Status → done. | story-automator-review |
