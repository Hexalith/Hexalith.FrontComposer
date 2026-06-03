---
title: 'FC-SETTINGS — Theme/density settings-persistence contract'
date: '2026-06-03'
story: '1.6'
status: 'confirmed'   # FR15/NFR6 behaviour + AC3 persistence/DOM single-writer fully verified by existing+new tests; ONE sub-item escalated — the literal AC3 wording "one effect owns persistence AND JS interop / each field mutated by exactly one action" (owner: FrontComposer pending). See Confirmation.
owner: 'FrontComposer (pending)'   # only for the AC3 split-interpretation wording; the behaviour itself is confirmed
supersedes: ''
---

# FC-SETTINGS — Theme/density settings-persistence contract

> **Decision deliverable for Story 1.6 — the functional capstone that closes Epic 1.** Unlike the four
> prior confirm-and-document ready-gates (FC-LYT 1.2 / FC-A11Y 1.3 / FC-L10N 1.4 / FC-DOC 1.5), Story
> 1.6 is a **functional** story (FR15 + NFR6 + NFR9) — **but the entire theme/density/settings stack
> already ships, fully wired and already covered by tests** at baseline `c88b362`. The job is therefore
> to **confirm-and-pin** the three ACs against the real implementation, not to build new components.
> This note **(1) names the settings-persistence contract** — *live-no-Apply + tenant/user-scoped
> persistence + restore-on-init*; **(2) records the confirmed facts** for all three ACs; **(3) records
> the one genuinely-open item** (the literal AC3 single-writer wording) escalated with a named owner;
> and **(4) points at the standing enforcement** — the NFR17 tripwire plus the new per-slice
> single-writer pin. Adopting this contract introduces **zero behaviour change and no `src/` change**:
> it documents what the shell already does and pins how the persistence writers must stay owned.

## The contract

The settings contract is **"live-no-Apply UX + tenant/user-scoped persistence + restore-on-init,"**
and naming that *is* the deliverable:

- **Live, no Apply/Cancel.** Changes made in `FcSettingsDialog` take effect immediately (epic AC §126;
  `FcSettingsDialog.razor:10-13`). The footer is **Restore-defaults + Done** (Done only closes; there is
  no commit/rollback step).
- **One dialog, two entry points, one launcher.** The header `FcSettingsButton` and the `Ctrl+,` /
  `meta+,` shortcut both route through the single `FcSettingsDialogLauncher.ShowAsync`
  (`FrontComposerShortcutRegistrar.cs:91-99` register → `OpenSettingsAsync` → `ShowAsync`).
- **Scoped, typed persistence.** Theme and density persist under the scoped keys
  `{tenantId}:{userId}:theme` and `{tenantId}:{userId}:density` (`StorageKeys.BuildKey`) via
  `IStorageService` (`LocalStorageService`). A null/empty/whitespace tenant or user short-circuits the
  write and logs `HFC2105` (fail-closed; theme still applies to the DOM, just isn't persisted).
- **Restore-on-init.** Both slices hydrate on `AppInitializedAction`, and re-hydrate on
  `StorageReadyAction` **iff** their `HydrationState` is still `Idle` (Story 3-6 D19).
- **Density legacy migration is read-path-only.** `DensityEffects.ReadStoredPreferenceAsync`
  (`:233-253`) migrates a legacy string value to the typed enum and re-writes it once via
  `SetAsync((DensityLevel?)migrated)` — the second whitelisted density expression, already counted in
  the NFR17 tripwire. It is **not** a new persistence writer.
- **NFR17 tripwire is the standing guard.** `SetAsyncCallSiteCount_MatchesExpected == 7` and the
  `BlobDoesNotCarryEntityData` whitelist (incl. `action.NewTheme`, `value`, `(DensityLevel?`) stay
  unchanged — proof no new persistence writer was added.

### Single-writer reading (AC3 / NFR9 / ADR-007)

The single-writer guarantee is **per side-effect, per axis** — *not* "one effect does everything":

| Axis | Persistence writer (single) | DOM / JS-interop writer (single) | State field |
|---|---|---|---|
| **Theme** | `ThemeEffects.HandleThemeChanged` — `storage.SetAsync(key, action.NewTheme)` (the only SetAsync under `State/Theme/`) | `IThemeService.SetThemeAsync` — applied from the effect (user change), `FrontComposerShell.ApplyThemeAsync` (re-apply on render), and `FcSystemThemeWatcher` (OS-follow when `CurrentTheme == System`), all reading the single `CurrentTheme` field | `CurrentTheme` assigned by exactly one reducer (`ThemeReducers.ReduceThemeChanged`) |
| **Density** | `DensityEffects` — `HandleUserPreferenceChanged` / `HandleUserPreferenceCleared` → `PersistAsync` (the only SetAsync sites under `State/Density/`, both in one effect class) | `FcDensityApplier` — the only `data-fc-density` DOM writer (ADR-041), pinned by `DensityNoPerComponentLogicLintTest` | `UserPreference` assigned by four reducer methods (changed / cleared / hydrated / legacy-changed) |

The contract reading: **"exactly one persistence writer per slice, and exactly one DOM writer per
side-effect."** Theme's persistence **and** the change-driven interop are co-located in
`HandleThemeChanged`; density deliberately **splits** persistence (effect) from the `data-fc-density`
DOM write (`FcDensityApplier`) because the DOM attribute must also mirror `EffectiveDensity` on the
non-persisting viewport-tier recompute path (ADR-040/ADR-041).

## Confirmation

**ACs 1 & 2 — CONFIRMED.** Verified against source and by existing tests:

- **AC1** — `FcSettingsDialog.razor` renders the density `FluentRadioGroup` (Compact/Comfortable/Roomy),
  the embedded `<FcThemeToggle/>`, and `<FcDensityPreviewPanel/>` under `data-testid="fc-settings-dialog"`;
  both open paths route through `FcSettingsDialogLauncher.ShowAsync`. Covered by `FcSettingsDialogTests`,
  `FcSettingsButtonTests`, `FcSettingsDialogLauncherTests`, `FrontComposerShortcutRegistrarTests`.
- **AC2** — theme/density persist under the scoped keys and restore on app-init/storage-ready hydration;
  density changes announce through `FcDensityAnnouncer` (`role="status"` / `aria-live="polite"`,
  first-render suppressed). Covered by `ThemeEffectsTests` + `ThemeEffectsScopeTests`,
  `DensityEffectsTests` + `DensityEffectsScopeTests` + `DensityPersistenceSnapshotTests`,
  `FcDensityAnnouncerTests`, `FcDensityApplierTests`.

**AC3 (persistence + DOM single-writer) — CONFIRMED, and newly pinned.** The per-slice persistence
ownership and theme-field single assignment are now enforced by
`Architecture/SliceSingleWriterGovernanceTests` (new this story), complementing the global NFR17
call-site count (== 7), the `data-fc-density` single-DOM-writer lint, and the ADR-039 reducer-purity
lint — all green.

**AC3 (literal wording) — ESCALATED (owner: FrontComposer — pending).** Two AC3 phrasings are not
literally true at the source and need a human ratification of the *reading* (not a redesign):

1. *"exactly one effect owns persistence **and** JS interop."* True for **theme** persistence + the
   change-driven `SetThemeAsync`; but theme DOM application is intentionally invoked from **three**
   sites (effect, shell re-apply, system watcher), and **density** deliberately splits persistence
   (effect) from the `data-fc-density` DOM write (`FcDensityApplier`, ADR-041). Recommended reading:
   *"exactly one **persistence** writer per slice + exactly one **DOM** writer per side-effect."*
2. *"`CurrentTheme` / `UserPreference` each mutated by exactly one action type."* True for
   `CurrentTheme` (one reducer); **`UserPreference` is assigned by four reducer methods**
   (changed/cleared/hydrated/legacy). The single-writer discipline holds at the *side-effect* level,
   not the *state-field-per-action* level.

Resolution does **not** block Story 1.6 — the behaviour ships, is tested, and is now pinned. This note
records the recommended reading; a reviewer may ratify or refine the wording.

## Surface confirmed by Story 1.6

- Theme slice — `src/Hexalith.FrontComposer.Shell/State/Theme/` (`ThemeValue`, `FrontComposerThemeState`,
  `ThemeActions`, `ThemeReducers`, `ThemeEffects`, `FrontComposerThemeFeature`).
- Density slice — `src/Hexalith.FrontComposer.Shell/State/Density/` (`FrontComposerDensityState`,
  `DensityActions`, `DensityReducers`, `DensityEffects`, `DensityPrecedence`,
  `FrontComposerDensityFeature`); `DensityLevel` in `Contracts/Rendering/`.
- Settings UI — `src/Hexalith.FrontComposer.Shell/Components/Layout/` (`FcSettingsDialog`,
  `FcThemeToggle`, `FcDensityPreviewPanel`, `FcSettingsButton`, `FcSettingsDialogLauncher`,
  `FcDensityAnnouncer`, `FcDensityApplier`).
- Persistence — `Contracts/Storage/IStorageService.cs` + `Shell/Infrastructure/Storage/LocalStorageService.cs`;
  keys via `StorageKeys.BuildKey`.
- New this story (`tests/` only) —
  `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SliceSingleWriterGovernanceTests.cs`.

## FC-DOC linkage (settings page — tracked gap, optional)

FC-DOC's component status map (`fc-doc-component-documentation-2026-06-03.md`) names **Story 1.6** as
the owner of the *settings* component-doc decision. These ACs do **not** require the published page;
`docs/reference/components/settings.md` remains an **optional, Gate-2d-conformant** close-out and is
left as a tracked gap unless authored deliberately. This note is a BMAD artifact under
`_bmad-output/contracts/`, **not** a published `docs/` page.

## References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.6: Theme, density, and settings persistence] (story + 3 ACs; FR15 / NFR6 / NFR9)
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs:108-130] (single theme persistence writer + IThemeService apply; key `{tenant}:{user}:theme`; hydrate :40-100)
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs:165-203,233-253] (density persist single-writer; ADR-038/040 exclusions; legacy `(DensityLevel?` migration write; key `{tenant}:{user}:density`)
- [Source: src/Hexalith.FrontComposer.Shell/State/Theme/ThemeReducers.cs:19] (CurrentTheme single reducer assignment)
- [Source: src/Hexalith.FrontComposer.Shell/State/Density/DensityReducers.cs:26,45,65,102] (UserPreference assigned by four reducer methods — the AC3-wording nuance)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcSettingsDialog.razor:15-68 (+.razor.cs:58-107)] (radio + FcThemeToggle + FcDensityPreviewPanel + Restore/Done; live-no-Apply; resolver-before-dispatch ADR-039)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityApplier.razor.cs] (single `data-fc-density` DOM writer, ADR-041)
- [Source: src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor (+.razor.cs)] (aria-live="polite" / role="status"; first-render suppression)
- [Source: src/Hexalith.FrontComposer.Shell/Shortcuts/FrontComposerShortcutRegistrar.cs:91-99,210-216] (`ctrl+,`/`meta+,` → OpenSettingsAsync → FcSettingsDialogLauncher.ShowAsync — shared with FcSettingsButton)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs:25-91] (SetAsync count == 7; whitelist — the standing persistence-writer guard)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SliceSingleWriterGovernanceTests.cs] (new per-slice single-writer pin for AC3/NFR9)
- [Source: tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityNoPerComponentLogicLintTest.cs] (data-fc-density single-DOM-writer lint, ADR-041)
- [Source: _bmad-output/contracts/fc-doc-component-documentation-2026-06-03.md] (FC-DOC status map: "Settings … Story 1.6 finalizes settings UX")
