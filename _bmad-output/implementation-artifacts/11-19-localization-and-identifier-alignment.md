---
created: 2026-07-15
updated: 2026-07-16
epic: 11
childStory: 11.19c
parentStory: 11.19
owner: Developer + Product/UX
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: review
implementationGate: post-correction-readiness-pass
baseline_commit: 84273bac14c00e0051872d91ee9be8761317b2af
---

# Story 11.19c: Localization And Identifier Alignment

Status: review.

## Story

As an operator and maintainer,
I want shell accessibility copy, document language, and diagnostic identifiers to describe their real
scope,
so that localized users and support tooling receive accurate, stable signals.

## Acceptance Criteria

1. Given `FcHomeCard` has a positive aggregate count, when it renders in English or French, then its
   accessible label is produced from a whole-string `FcShellResources` template with bounded-context
   display name and count placeholders. No hard-coded `"items pending"` remains, and the zero-count
   label remains the manifest display name without awkward punctuation.

2. Given the UI host culture is selected, when `App.razor` renders, then the root `<html lang>` is a
   valid BCP-47 tag derived from the effective UI culture rather than hard-coded `en`. Server prerender
   and interactive rendering agree, and tests cover at least `en` and `fr` without adding JS as a
   second language authority unless the host lifecycle proves it necessary.

3. Given the Shell and UI host source sets, when localization Governance runs, then the corrected
   home-card accessibility string and root language have focused positive/negative tests and no other
   user-visible hard-coded English introduced by this story. EN/FR resource key parity remains green.

4. Given HFC2106 is used by both Theme and Density hydration fallback/deferred paths, when the public
   constant is aligned, then `HFC2106_PreferenceHydrationFallback` is the preferred name and retains the
   exact string value `"HFC2106"`. The old public `HFC2106_ThemeHydrationEmpty` constant remains as an
   `[Obsolete]` alias to the new constant for source/binary compatibility during the documented
   deprecation window.

5. Given the rename, when Theme and Density call sites, XML docs, diagnostic docs/catalog, tests, and
   public API baselines are checked, then current code uses the neutral name, the diagnostic value and
   runtime phase do not change, and catalog/help-link parity remains exact.

6. Given validation runs, when Release build, Shell/UI localization tests, accessibility/render tests,
   diagnostic/public-API Governance, docs, artifact, and file-integrity lanes execute, then they pass
   without route, behavior, package version, analyzer policy, schema, generated output, release
   workflow, or submodule changes.

## Tasks / Subtasks

- [x] Add EN/FR whole-string home-card pending-label resources and use the existing localizer.
- [x] Bind the UI host root `lang` to the effective BCP-47 UI culture and add prerender/render tests.
- [x] Add focused hard-coded-string, resource-parity, and accessibility-name governance.
- [x] Add `HFC2106_PreferenceHydrationFallback`, obsolete-alias the old public name, and migrate call sites.
- [x] Reconcile diagnostic docs/catalog and intentional public API baseline changes.
- [x] Run Release/localization/a11y/diagnostic/docs/artifact validation and reconcile the File List.

## Dev Notes

### Exact Current State

- `FcHomeCard.razor` currently builds
  `$"{Card.Manifest.Name}, {Card.AggregateCount} items pending"` even though it already injects
  `IStringLocalizer<FcShellResources>`.
- `FcShellResources.resx` and `.fr.resx` have home-card/home-directory keys but no whole-string pending
  card aria-label template.
- `src/Hexalith.FrontComposer.UI/Components/App.razor` contains `<html lang="en">` and no other
  visible English body copy.
- Public `FcDiagnosticIds.HFC2106_ThemeHydrationEmpty = "HFC2106"` is used once by Theme and three
  times by Density for Empty, BrowserStorageUnavailable, and Corrupt/fallback paths.

### Files To Read Before Editing

- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx` and `.fr.resx`
- `src/Hexalith.FrontComposer.UI/Components/App.razor` plus host localization setup
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `State/Theme/ThemeEffects.cs`, `State/Density/DensityEffects.cs`, diagnostic docs/catalog, and
  public API baselines

### Architecture And Anti-Patterns

- Use a whole-string resource such as `HomeCardPendingAriaLabelTemplate`; do not concatenate localized
  fragments. Preserve placeholder order explicitly in EN and FR.
- Use the culture already established by ASP.NET Core/Blazor localization; do not create a second
  persisted culture setting or hard-code an EN fallback in markup.
- Do not change the HFC2106 string, severity, runtime-only phase, or support semantics. Because the old
  constant is public, removing it is not authorized.
- Keep WCAG 2.2 AA behavior and the existing role/link/keyboard semantics of the card unchanged.

### Technical References

- Blazor globalization/localization and the root `lang` accessibility role:
  https://learn.microsoft.com/en-us/aspnet/core/blazor/globalization-localization?view=aspnetcore-10.0
- .NET resource-file and `IStringLocalizer` guidance:
  https://learn.microsoft.com/en-us/dotnet/core/extensions/localization

### Validation Commands

```bash
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-19-localization-and-identifier-alignment.md
```

## References

- `_bmad-output/planning-artifacts/ux-design.md` — canonical WCAG 2.2 AA and localization behavior.
- `_bmad-output/planning-artifacts/epics.md` — 11.19c child scope.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — localization/name mismatch.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Task 1 RED: `HomeCard_PositiveCount_LocalizesAccessibleName` failed for `fr` because the rendered label retained the hard-coded English `items pending` suffix.
- Task 1 GREEN: focused home-card lane passed 3/3; the full filtered solution lane passed 4,140/4,140 with `DOTNET_TieredCompilation=0` to make the pre-existing zero-allocation telemetry assertion deterministic.
- Task 2 RED: the UI-host language contract test failed while `App.razor` retained `<html lang="en">`.
- Task 2 GREEN: EN/FR BCP-47 and prerender/interactive source-contract lane passed 3/3; full filtered solution lane passed 4,143/4,143.
- Task 3 RED: a deliberate incorrect home-card resource-key mutation was rejected by the localization governance test.
- Task 3 GREEN: hard-coded-string, placeholder-parity, and accessible-name governance passed 6/6; full filtered solution lane passed 4,146/4,146.
- Task 4 RED: the compatibility contract failed to compile before `HFC2106_PreferenceHydrationFallback` existed; the first full lane then exposed uniqueness and deprecation-policy assumptions that did not recognize obsolete constant aliases.
- Task 4 GREEN: preferred-name/alias and obsolete-policy governance passed 6/6, Theme/Density behavior passed 28/28, and the full filtered solution lane passed 4,148/4,148.
- Task 5 RED: HFC2106 catalog governance rejected the old Theme-only registry title; docs validation then rejected the intentionally changed registry until its normalized producer fingerprint was reconciled.
- Task 5 GREEN: focused HFC2106 parity passed, diagnostic registry/catalog governance passed 106/106, docs validation passed, and the full filtered solution lane passed 4,149/4,149.
- Task 6 GREEN: final Release solution and Shell-test builds completed with 0 warnings/errors; the focused story slice passed 40/40, Contracts package validation passed against the published 3.0.0 baseline at version 4.0.0, the full filtered solution lane passed 4,149/4,149, docs and story-artifact validation passed, and diff/submodule integrity checks were clean.

### Completion Notes List

- Added the EN/FR `HomeCardPendingAriaLabelTemplate` whole-string resource and formatted it with the effective UI culture; positive-count labels now include the manifest display name and count, while zero-count labels remain the display name alone.
- Bound the UI host root `lang` attribute directly to `CultureInfo.CurrentUICulture.Name`; the same C# authority runs during server prerender and interactive rendering, with no JavaScript language writer.
- Added source governance for the home-card whole-string label and culture-derived root language, plus exact EN/FR placeholder-parity coverage.
- Added the neutral HFC2106 preferred constant, retained the former public name as a policy-compliant obsolete alias, migrated Theme/Density consumers, and kept active diagnostic allocation uniqueness strict.
- Re-authored HFC2106 registry/docs copy around Theme/Density preference fallback, preserved runtime-only Information semantics and the canonical help link, and pinned the additive public API through reflection-based compatibility governance (Contracts has no checked-in PublicAPI baseline file).
- Reconciled every changed source, test, documentation, governance, and tracking artifact in the File List; no generated output, route, schema, package-version policy, analyzer release row, workflow, or submodule pointer changed.

### File List

- `_bmad-output/implementation-artifacts/11-19-localization-and-identifier-alignment.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/diagnostics/HFC2106.md`
- `docs/diagnostics/diagnostic-registry.json`
- `docs/validation/producer-fingerprints.json`
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.fr.resx`
- `src/Hexalith.FrontComposer.Shell/Resources/FcShellResources.resx`
- `src/Hexalith.FrontComposer.UI/Components/App.razor`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Diagnostics/FcDiagnosticIdsCompatibilityTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Home/FcHomeDirectoryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/LocalizationGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Resources/FcShellResourcesTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`

## Change Log

- 2026-07-16: Implemented EN/FR accessible-name localization, culture-derived document language, and HFC2106 neutral identifier/API/docs alignment with focused governance and full validation.
- 2026-07-15: Materialized approved 11.19c child with exact live copy, culture, and HFC2106 compatibility boundaries.
