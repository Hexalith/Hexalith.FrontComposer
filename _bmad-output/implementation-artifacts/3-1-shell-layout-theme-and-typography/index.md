# Story 3.1: Shell Layout, Theme & Typography

Status: done

## Table of Contents

- [Dev Agent Cheat Sheet (Read First — 2 pages)](./dev-agent-cheat-sheet-read-first-2-pages.md)
- [Story](./story.md)
- [Critical Decisions (READ FIRST — Do NOT Revisit)](./critical-decisions-read-first-do-not-revisit.md)
- [Architecture Decision Records](./architecture-decision-records.md)
    - [ADR-027: FrontComposerShell owns the top-level FluentLayout — MainLayout becomes a 3-line wrapper](./architecture-decision-records.md#adr-027-frontcomposershell-owns-the-top-level-fluentlayout-mainlayout-becomes-a-3-line-wrapper)
    - [ADR-028: IThemeService (Fluent UI) is the theme applier; Fluxor ThemeState is the tenant-scoped cache](./architecture-decision-records.md#adr-028-ithemeservice-fluent-ui-is-the-theme-applier-fluxor-themestate-is-the-tenant-scoped-cache)
    - [ADR-029: LocalStorageService replaces StorageKeys static defaults with IUserContextAccessor fail-closed reads](./architecture-decision-records.md#adr-029-localstorageservice-replaces-storagekeys-static-defaults-with-iusercontextaccessor-fail-closed-reads)
    - [ADR-030: IStorageService lifetime migration — Singleton → Scoped](./architecture-decision-records.md#adr-030-istorageservice-lifetime-migration-singleton-scoped)
    - [ADR-031: Six semantic color slots declared in FrontComposerShell.razor.css with CI-locked lifecycle-state binding table](./architecture-decision-records.md#adr-031-six-semantic-color-slots-declared-in-frontcomposershellrazorcss-with-ci-locked-lifecycle-state-binding-table)
    - [ADR-032: Header placeholder triggers are hidden via compile-away guard, not rendered as aria-disabled buttons](./architecture-decision-records.md#adr-032-header-placeholder-triggers-are-hidden-via-compile-away-guard-not-rendered-as-aria-disabled-buttons)
    - [ADR-033: IStringLocalizer<FcShellResources> replaces IFluentLocalizer in the framework resource-resolution path](./architecture-decision-records.md#adr-033-istringlocalizerfcshellresources-replaces-ifluentlocalizer-in-the-framework-resource-resolution-path)
    - [ADR-034: Scoped CSS bundle filename contract for the FrontComposerShell RCL](./architecture-decision-records.md#adr-034-scoped-css-bundle-filename-contract-for-the-frontcomposershell-rcl)
- [Acceptance Criteria](./acceptance-criteria.md)
    - [AC1: FrontComposerShell renders three regions with header pinned to 48 px and form max 720 px](./acceptance-criteria.md#ac1-frontcomposershell-renders-three-regions-with-header-pinned-to-48-px-and-form-max-720-px)
    - [AC2: Default accent is #0097A7, overridable via FcShellOptions.AccentColor with hex-format validation](./acceptance-criteria.md#ac2-default-accent-is-0097a7-overridable-via-fcshelloptionsaccentcolor-with-hex-format-validation)
    - [AC3: Theme toggle applies Light/Dark/System instantly via IThemeService and persists via LocalStorageService](./acceptance-criteria.md#ac3-theme-toggle-applies-lightdarksystem-instantly-via-ithemeservice-and-persists-via-localstorageservice)
    - [AC4: Six semantic color slots expose CSS custom properties with lifecycle-state mapping](./acceptance-criteria.md#ac4-six-semantic-color-slots-expose-css-custom-properties-with-lifecycle-state-mapping)
    - [AC5: Typography static class exposes 9 constants with version-pinned mappings](./acceptance-criteria.md#ac5-typography-static-class-exposes-9-constants-with-version-pinned-mappings)
    - [AC6: LocalStorageService implements IStorageService with LRU eviction and beforeunload FlushAsync](./acceptance-criteria.md#ac6-localstorageservice-implements-istorageservice-with-lru-eviction-and-beforeunload-flushasync)
    - [AC7: EN + FR resource files resolve framework-generated UI strings via IStringLocalizer](./acceptance-criteria.md#ac7-en-fr-resource-files-resolve-framework-generated-ui-strings-via-istringlocalizer)
- [Tasks / Subtasks](./tasks-subtasks.md)
    - [Task 0 — Prereq verification + JS interop spike + IStorageService audit (≤ 75 min)](./tasks-subtasks.md#task-0-prereq-verification-js-interop-spike-60-min)
    - [Task 1 — Contracts additions (accent/locale options + Typography)](./tasks-subtasks.md#task-1-contracts-additions)
    - [Task 2 — LocalStorageService (WASM impl + LRU + FlushAsync drain)](./tasks-subtasks.md#task-2-localstorageservice)
    - [Task 3 — beforeunload.js + JS module loader](./tasks-subtasks.md#task-3-beforeunloadjs)
    - [Task 4 — IUserContextAccessor wiring for ThemeEffects + DensityEffects](./tasks-subtasks.md#task-4-iusercontextaccessor-wiring)
    - [Task 5 — FrontComposerShell.razor component](./tasks-subtasks.md#task-5-frontcomposershellrazor-component)
    - [Task 6 — FcThemeToggle.razor + FcSystemThemeWatcher](./tasks-subtasks.md#task-6-fcthemetoggle-fcsystemthemewatcher)
    - [Task 7 — Semantic color slots CSS + lifecycle mapping regression lock](./tasks-subtasks.md#task-7-semantic-color-slots-css)
    - [Task 8 — EN/FR resource files + AddHexalithShellLocalization extension](./tasks-subtasks.md#task-8-enfr-resource-files)
    - [Task 9 — Counter sample rewire (MainLayout → FrontComposerShell)](./tasks-subtasks.md#task-9-counter-sample-rewire)
    - [Task 10 — Tests (bUnit + snapshot + options validation + resource round-trip)](./tasks-subtasks.md#task-10-tests)
    - [Task 11 — Regression + zero-warning gate](./tasks-subtasks.md#task-11-regression-zero-warning-gate)
- [Known Gaps (Explicit, Not Bugs)](./known-gaps-explicit-not-bugs.md)
- [Dev Notes](./dev-notes.md)
    - [Service Binding Reference](./dev-notes.md#service-binding-reference)
    - [FrontComposerShell Composition Diagram](./dev-notes.md#frontcomposershell-composition-diagram)
    - [Theme Application Flow](./dev-notes.md#theme-application-flow)
    - [LocalStorageService Contract Notes](./dev-notes.md#localstorageservice-contract-notes)
    - [Tenant/User Scope Migration (L03 fail-closed)](./dev-notes.md#tenantuser-scope-migration-l03-fail-closed)
    - [Typography API Surface](./dev-notes.md#typography-api-surface)
    - [Fluent UI v5 Component Reference](./dev-notes.md#fluent-ui-v5-component-reference)
    - [Files Touched Summary](./dev-notes.md#files-touched-summary)
    - [Naming Convention Reference](./dev-notes.md#naming-convention-reference)
    - [Testing Standards](./dev-notes.md#testing-standards)
    - [Build & CI](./dev-notes.md#build-ci)
    - [Previous Story Intelligence](./dev-notes.md#previous-story-intelligence)
    - [Lessons Ledger Citations](./dev-notes.md#lessons-ledger-citations)
    - [References](./dev-notes.md#references)
    - [Project Structure Notes](./dev-notes.md#project-structure-notes)
- [Dev Agent Record](./dev-agent-record.md)
    - [Agent Model Used](./dev-agent-record.md#agent-model-used)
    - [Debug Log References](./dev-agent-record.md#debug-log-references)
    - [Completion Notes List](./dev-agent-record.md#completion-notes-list)
    - [File List](./dev-agent-record.md#file-list)
    - [Change Log](./dev-agent-record.md#change-log)
    - [Review Findings](./dev-agent-record.md#review-findings)

### Review Findings

- [x] \[Review\]\[Patch\] `AddHexalithShellLocalization` now configures `RequestLocalizationOptions`, so `DefaultCulture` / `SupportedCultures` flow into request-localization wiring [`src/Hexalith.FrontComposer.Shell/Extensions/ServiceCollectionExtensions.cs`]
- [x] \[Review\]\[Patch\] Theme application moved back behind `ThemeEffects`, restoring the AC3/D6 effect path for hydrated + user-driven changes [`src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs`]
- [x] \[Review\]\[Patch\] `FrontComposerShell` now scopes semantic CSS variables through `.fc-shell-root`, avoiding the generated `:host[b-…]` mismatch [`src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerShell.razor.css`]
- [x] \[Review\]\[Patch\] `LocalStorageService.GetAsync` only updates LRU timestamps after a real hit, preventing phantom eviction entries [`src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs`]
- [x] \[Review\]\[Patch\] `LocalStorageService.RemoveAsync` now uses the queued drain path, preserving set/delete ordering [`src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs`]
- [x] \[Review\]\[Patch\] `FlushAsync` now surfaces earlier drain failures instead of reporting false success [`src/Hexalith.FrontComposer.Shell/Infrastructure/Storage/LocalStorageService.cs`]
- [x] \[Review\]\[Patch\] `FcThemeToggle` now emits a durable accessible name via screen-reader-only text because `FluentMenuButton` does not surface the raw `aria-label` attribute [`src/Hexalith.FrontComposer.Shell/Components/Layout/FcThemeToggle.razor`, `src/Hexalith.FrontComposer.Shell/Components/Layout/FcThemeToggle.razor.css`]
- [x] \[Review\]\[Patch\] Required shell/theme regression tests, parameter-surface coverage, and slot-mapping verification are now implemented and passing [`tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/**`, `tests/Hexalith.FrontComposer.Shell.Tests/SlotMappingRegressionTests.cs`]
