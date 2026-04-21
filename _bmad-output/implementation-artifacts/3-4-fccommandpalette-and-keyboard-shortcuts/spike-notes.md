# Spike Notes — Story 3-4 Task 0 prereq verification

> Captured 2026-04-20 on a clean main checkout (post-Story 3-3 done state).

## Baseline test count (Task 0.1)

`Grep "[Fact]|[Theory]"` across the entire `tests/` tree returned **594 occurrences across 100 files** — this is the `test_baseline_pre_3_4` value to compare delta against post-3-4. (The story spec estimated ~649; the lower number reflects exact-file post-3-3 closure rather than the original estimate.)

## FluentSearch surface (Task 0.2)

Verified against the in-source Fluent UI v5 references already used by Story 3-3 (`FluentSearch` is mentioned in Counter.Web's data-grid samples and in the Fluent UI v5 RC2 package referenced by the solution). For 3-4 the contract we depend on is:

- `Value` (string) + `ValueChanged` (`EventCallback<string>`) two-way binding.
- `Placeholder` parameter.
- `FocusAsync()` programmatic focus API on the `FluentInputBase`-rooted component.
- `aria-controls` HTML attribute is forwarded via splat (per the `@attributes` capture in `FluentSearch.razor`).

If `FluentSearch.FocusAsync()` proves unavailable in v5 RC2, fall back to `_searchRef.Element?.FocusAsync()` (Blazor `ElementReference` extension) — both paths land on the same `<input>` element. Decision: proceed with `FluentSearch` per spec; if a follow-up bug surfaces, swap to `FluentTextField` with `Type="Search"`.

## FluentBadge surface (Task 0.3)

Confirmed `FluentBadge` exposes `Appearance` + `Fill` + child content. The `Intent` property is named `Appearance` of `BadgeAppearance` enum in v5 RC2 — the spec's `Intent="MessageIntent.Info"` referenced an older API. The v5 RC2 `BadgeAppearance` enum has four members: `Filled` / `Ghost` / `Outline` / `Tint` (verified against `Microsoft.FluentUI.AspNetCore.Components.xml` at `5.0.0-rc.2-26098.1`). The earlier spike-note draft named `Appearance.Accent` — that value exists on the general `Appearance` enum but NOT on `BadgeAppearance`. The implementation uses `Appearance="@BadgeAppearance.Tint"` (lightest emphasis, closest to the "Info" semantic the spec intended). DN5 2026-04-21 ratified `Tint` after the code-review workflow surfaced the spike-note/API mismatch. This does not alter ADR-044 contract — badge rendering remains identical.

## IDialogService keyDown routing (Task 0.4)

`FluentDialog` content components implement `IDialogContentComponent<TData>` (or the non-generic variant). The dialog hosts the content's render fragment inside its own panel; `@onkeydown` on the content's root `<div>` receives the event before Fluent's internal Escape handler. To keep our explicit `PaletteClosedAction` dispatch authoritative, the component still manages Escape itself (D11 dismiss-path coherence requires it). Confirmed Escape-bubbling is non-blocking in v5 RC2.

## MetaKey availability (Task 0.5)

`KeyboardEventArgs.MetaKey` is populated on Blazor Server `@onkeydown` events as part of the standard `KeyboardEventArgs` payload — verified in `Microsoft.AspNetCore.Components.Web.KeyboardEventArgs` (always populated since .NET 7). No D1 grammar reduction necessary. The `meta` modifier is preserved in `ShortcutBinding.Normalize`.

## Search icon name (Task 0.6)

Confirmed `Icons.Regular.Size20.Search` exists in `Microsoft.FluentUI.AspNetCore.Components.Icons` (v5 RC2 default icon set). No name divergence; Task 7.1 uses the exact name.

## dotnet build clean (Task 0.7)

`dotnet build Hexalith.FrontComposer.sln --nologo` reports `0 Warning(s) / 0 Error(s)`. Baseline confirmed.

## FakeTimeProvider package reference (Task 0.9)

`tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` ALREADY references `Microsoft.Extensions.TimeProvider.Testing` (line 28). No `<PackageReference>` addition required.

## D21 registry contract check (Task 0.10) — TRIPWIRE + RATIFIED PATTERN (DN1 2026-04-21)

`Grep "HasFullPageRoute" src/Hexalith.FrontComposer.Contracts` returned **ZERO matches** at spike time. Story 2-2's `IFrontComposerRegistry` does NOT expose `bool HasFullPageRoute(string commandTypeName)`.

**Ratified design (DN1 resolution 2026-04-21 — code-review Decision-Needed option 1):**

After the bmad-code-review workflow flagged the original spec-append path as a HIGH-severity D21 deviation (extension-with-fallback-to-true silently surfaces every command as reachable for adopters who don't opt in), the reviewer-resolved design is a **companion-interface opt-in** rather than a direct `IFrontComposerRegistry` append:

1. `IFrontComposerFullPageRouteRegistry` — new companion interface in `Contracts/Registration/` declaring `bool HasFullPageRoute(string commandTypeName)`.
2. `FrontComposerRegistryExtensions.HasFullPageRoute(this IFrontComposerRegistry registry, string commandTypeName)` — extension delegating to the companion when the concrete registry implements it, else returning `true` (pre-3-4 default).
3. Shell's `FrontComposerRegistry` implements BOTH `IFrontComposerRegistry` AND `IFrontComposerFullPageRouteRegistry`; its `HasFullPageRoute` returns `true` only when the command is in a registered manifest.
4. Startup guard `FrontComposerRegistry.ValidateManifests()` throws `HFC1601_ManifestInvalid` when a registered command has no FullPage route — prevents the palette from booting into a state where it can 404 the user.
5. Story 9-4 layers a build-time analyzer that requires any registry type which registers commands to also implement `IFrontComposerFullPageRouteRegistry`.

Rationale for NOT modifying `IFrontComposerRegistry` directly: adding a member to a stable-contract interface forces every external adopter to recompile + provide an implementation. The companion-interface pattern keeps the 2-2 contract binary-stable while still delivering D21's "filter unreachable commands" invariant for adopters who opt in. The permissive fallback is documented in the interface's XML docs (DN1 doc-update patch).

## Diagnostic ID renumber (post-spike correction)

The story spec reserves `HFC2107_ShortcutConflict` / `HFC2108_ShortcutHandlerFault` / `HFC2109_PaletteScoringFault` / reuse-of-`HFC2106` for palette tampering. **`HFC2107` is already taken** by `FcDiagnosticIds.HFC2107_NavigationHydrationEmpty` (Story 3-2 D15). Likewise `HFC2106` is theme-scoped (`HFC2106_ThemeHydrationEmpty`).

Renumber for 3-4 (additive to `FcDiagnosticIds`):

| Spec ID | Actual ID (this story) | Severity | Use |
|---|---|---|---|
| HFC2107_ShortcutConflict | **HFC2108_ShortcutConflict** | Information | Duplicate `Register` last-writer-wins log |
| HFC2108_ShortcutHandlerFault | **HFC2109_ShortcutHandlerFault** | Warning | Registered handler threw |
| HFC2109_PaletteScoringFault | **HFC2110_PaletteScoringFault** | Warning | Registry enumeration threw during scoring |
| HFC2106 (reuse) `Reason=Tampered` | **HFC2111_PaletteHydrationEmpty** with `Reason=Empty/Corrupt/Tampered` | Information | Palette recent-route hydrate outcomes (per-feature precedent — theme owns 2106, navigation owns 2107) |

This deviation is recorded here so reviewers see why the diagnostic IDs in code do not match the prose in the critical-decisions doc.
