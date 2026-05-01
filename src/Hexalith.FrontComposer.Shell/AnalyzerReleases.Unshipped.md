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
HFC2112 | HexalithFrontComposer | Warning     | Badge initial-fetch / re-fetch / capability-seen persistence fault — per-type reader threw, async-void notifier handler threw, or seen-capability storage write failed (Story 3-5 D12, D13; emitted by `BadgeCountService.FetchOneAsync`, `BadgeCountService.OnProjectionChanged`, and `CapabilityDiscoveryEffects.HandleCapabilityVisited`)
HFC2113 | HexalithFrontComposer | Information | Projection type-name string from `IProjectionChangeNotifier.ProjectionChanged` failed `Type.GetType` resolution (Story 3-5 D7; emitted by `BadgeCountService.OnProjectionChanged` — de-duplicated per Scoped service instance via `ConcurrentDictionary<string, byte>` guard)
HFC2114 | HexalithFrontComposer | Information | DataGrid hydrate encountered an `Empty`/`Corrupt`/`OutOfScope`/`RegistryFailure` key during `{tenantId}:{userId}:datagrid:*` enumeration (Story 3-6 D11 / D14 / A9; emitted by `DataGridNavigationEffects.HandleAppInitialized` and `HandleStorageReady`; `OutOfScope` dedup is per-distinct-viewKey, `RegistryFailure` dedup is once-per-hydrate-pass)
HFC1047 | HexalithFrontComposer | Information | Dev-mode annotation site lacks stable descriptor metadata; generated annotation is omitted or marked stale (Story 6-5)
HFC1048 | HexalithFrontComposer | Information | Dev-mode starter emission requested an unsupported customization level or mismatched node level (Story 6-5)
HFC1049 | HexalithFrontComposer | Information | Dev-mode starter metadata is stale relative to contract version, descriptor hash, or source component identity (Story 6-5)
HFC2010 | HexalithFrontComposer | Information | Defensive runtime log for a dev-mode activation attempt outside Development; normal DEBUG/runtime gates prevent user-visible behavior (Story 6-5)
HFC2011 | HexalithFrontComposer | Error       | FrontComposer authentication bridge configuration is invalid at startup (Story 7-1)
HFC2012 | HexalithFrontComposer | Warning     | Authenticated claim extraction failed closed without exposing raw claim values (Story 7-1)
HFC2013 | HexalithFrontComposer | Warning     | EventStore access-token relay failed without exposing token material (Story 7-1)
HFC2014 | HexalithFrontComposer | Error       | GitHub OAuth sign-in requires an adopter broker/custom provider before EventStore bearer relay (Story 7-1)
