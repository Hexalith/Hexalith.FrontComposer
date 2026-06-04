# Test Automation Summary - Story 2.7 (Command palette discovery and global search)

**Date:** 2026-06-04
**Baseline commit:** `ad6c78e7a23a43a08af2864f6fd452ad8a856360`
**Framework:** xUnit v3 + Shouldly + bUnit + NSubstitute + `FakeTimeProvider`
**Net `src/` change:** ZERO

## Coverage

- **AC1:** `Ctrl+K` / `Meta+K` opens `FcCommandPalette`; rendered palette is pinned as an ARIA combobox/listbox/option surface with `aria-activedescendant` tied to selected rows.
- **AC1 keyboard nav:** rendered `FcCommandPalette` ArrowDown/ArrowUp/Escape path is pinned in the default lane, not only the excluded `e2e-palette` lane.
- **AC2:** `CommandPaletteEffects.HandlePaletteQueryChanged` is pinned through the real scorer/effect path for multi-manifest projection search and empty-state results.
- **AC2 disposition:** option 1. The palette's `PaletteResultCategory.Projection` results are the registry-wide search surface. `FcProjectionGlobalSearch` remains a separate in-grid row search component.

## New or strengthened pins relative to baseline

- `FcCommandPaletteTests.SearchInput_RendersAsAriaCombobox`
- `FcCommandPaletteTests.ArrowKeys_MoveSelection_AndTrackAriaActiveDescendant`
- `FcCommandPaletteTests.Escape_DispatchesPaletteClosed_ClosingThePalette`
- `FcPaletteResultListTests.ResultsContainer_RendersRoleListbox_WithRoleOptionRows`
- `FcPaletteResultListTests.AriaSelected_AndAriaActiveDescendant_TrackSelectedIndex`
- `CommandPaletteEffectsTests.HandlePaletteQueryChanged_MultiManifest_SurfacesMatchingProjectionsRanked`
- `CommandPaletteEffectsTests.HandlePaletteQueryChanged_NonMatchingQuery_SurfacesEmptyState`

## Validation results

- **Build:** `dotnet build Hexalith.FrontComposer.slnx -c Release -m:1 /nr:false` -> 0 warnings, 0 errors.
- **Solution default lane:** `DiffEngine_Disabled=true dotnet test Hexalith.FrontComposer.slnx --filter "Category!=Performance&Category!=e2e-palette&Category!=NightlyProperty&Category!=Quarantined"` could not execute in this sandbox. MSBuild failed before running tests with `System.Net.Sockets.SocketException (13): Permission denied` while opening its named-pipe/socket transport.
- **Story-focused Shell classes:** xUnit v3 in-process runner -> 60 total, 0 failed.
- **Full Shell default lane:** xUnit v3 in-process runner -> 1760 total, 9 failed. Failures are outside palette/search: PendingStatusReopenGovernanceTests x4, NavigationEffectsLastActiveRouteTests x1, CounterStoryVerificationTests x2, EventStorePactContractTests x1 mock-server socket, CommandRendererFullPageTests x1.
- **Palette lane:** xUnit v3 in-process runner with `Category=e2e-palette` -> 4 total, 0 failed.
- **SourceTools default lane:** xUnit v3 in-process runner -> 958 total, 3 failed, matching the untouched standing SourceTools baseline.
- **Snapshot discipline:** `git diff --name-only -- '*.verified.txt'` returned no changes.
- **Sentinel scan:** modified test files are clean. The only story-file match is the pre-existing checklist text naming forbidden examples.
