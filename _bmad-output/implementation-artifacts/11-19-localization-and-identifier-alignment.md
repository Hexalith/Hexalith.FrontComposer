---
created: 2026-07-15
updated: 2026-07-15
epic: 11
childStory: 11.19c
parentStory: 11.19
owner: Developer + Product/UX
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15.md
status: ready-for-dev
implementationGate: post-correction-readiness-pass
---

# Story 11.19c: Localization And Identifier Alignment

Status: ready-for-dev.

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

- [ ] Add EN/FR whole-string home-card pending-label resources and use the existing localizer.
- [ ] Bind the UI host root `lang` to the effective BCP-47 UI culture and add prerender/render tests.
- [ ] Add focused hard-coded-string, resource-parity, and accessibility-name governance.
- [ ] Add `HFC2106_PreferenceHydrationFallback`, obsolete-alias the old public name, and migrate call sites.
- [ ] Reconcile diagnostic docs/catalog and intentional public API baseline changes.
- [ ] Run Release/localization/a11y/diagnostic/docs/artifact validation and reconcile the File List.

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

### Debug Log References

### Completion Notes List

### File List

## Change Log

- 2026-07-15: Materialized approved 11.19c child with exact live copy, culture, and HFC2106 compatibility boundaries.
