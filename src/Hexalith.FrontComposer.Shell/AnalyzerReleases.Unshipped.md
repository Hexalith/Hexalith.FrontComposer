; Unshipped analyzer / logger-message diagnostic releases for Hexalith.FrontComposer.Shell.
; Story 1-8 G2 discipline — every HFC* diagnostic emitted from this project must list a row
; here before release. Story 3-4 P10 (2026-04-21 pass 3) establishes this file in the Shell
; project; rows in this file remain "unshipped" until the next GA release moves them to
; AnalyzerReleases.Shipped.md.

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
HFC1601 | HexalithFrontComposer | Error       | Command manifest registration is invalid at startup (FrontComposerRegistry.ValidateManifests; Story 3-4 D21 placeholder — see `FrontComposerRegistry.HasFullPageRoute` XML doc for the documented-placeholder status)
HFC2108 | HexalithFrontComposer | Information | Duplicate shortcut registration replaced (last-writer-wins semantics per Story 3-4 D3; emitted by `ShortcutService.Register`)
HFC2109 | HexalithFrontComposer | Warning     | Registered shortcut handler threw (Story 3-4 D1; emitted by `ShortcutService.TryInvokeBindingAsync` — exception is caught so it does not bubble to the Blazor error boundary)
HFC2110 | HexalithFrontComposer | Warning     | Palette scoring fault — registry enumeration or per-manifest scoring threw (Story 3-4 ADR-043; emitted by `CommandPaletteEffects.HandlePaletteQueryChanged`)
HFC2111 | HexalithFrontComposer | Information | Palette hydration payload invalid — stored recent-route blob was empty / tampered / corrupt (Story 3-4 D10 open-redirect defence; emitted by `CommandPaletteEffects.HandleAppInitialized` and `HandlePaletteResultActivated` DN5 re-validation)
