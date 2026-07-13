# Story 11.16 hydration-state compatibility evidence

## Breaking-change disposition

Story 11.16 intentionally replaces five public, source- and binary-distinct enum types with the single public `Hexalith.FrontComposer.Shell.State.HydrationState` type. The repository's approved `2.0.0` major-release posture is the compatibility boundary for this change; no alias or obsolete compatibility enum is retained.

Exact consumer migration mapping:

| Removed type | Replacement |
|---|---|
| `Hexalith.FrontComposer.Shell.State.CommandPalette.CommandPaletteHydrationState` | `Hexalith.FrontComposer.Shell.State.HydrationState` |
| `Hexalith.FrontComposer.Shell.State.DataGridNavigation.DataGridNavigationHydrationState` | `Hexalith.FrontComposer.Shell.State.HydrationState` |
| `Hexalith.FrontComposer.Shell.State.Density.DensityHydrationState` | `Hexalith.FrontComposer.Shell.State.HydrationState` |
| `Hexalith.FrontComposer.Shell.State.Navigation.NavigationHydrationState` | `Hexalith.FrontComposer.Shell.State.HydrationState` |
| `Hexalith.FrontComposer.Shell.State.Theme.ThemeHydrationState` | `Hexalith.FrontComposer.Shell.State.HydrationState` |

The member names and values remain `Idle = 0`, `Hydrating = 1`, and `Hydrated = 2`. Consumers must replace the former feature-specific enum import/type with `Hexalith.FrontComposer.Shell.State.HydrationState` and recompile. Serialized numeric meaning and runtime state transitions are unchanged. `CapabilityDiscoveryHydrationState` remains separate because its lifecycle is `Idle/Seeding/Seeded`.

## Consumer evidence

`HydrationStateConsolidationTests.RepresentativeConsumer_CompilesAgainstSharedStateAndHydrationActions` compiles an independent Roslyn consumer against the built Shell assembly. The consumer reads the new type from representative public Theme and Navigation state signatures and constructs all five public hydration-start action types. The same suite pins all five state property types, the shared enum values, removal of the five former runtime types, and preservation of the capability-discovery enum.

The FcTbl-only Shell public API baseline remains byte-unchanged because these state types are outside that focused surface. `PublicAPI.FcTbl.Shipped.txt` is not modified.

## Required implementation metadata

The implementation PR or eventual commit must make the break visible, for example:

```text
refactor(shell)!: consolidate hydration state

BREAKING CHANGE: replace the five feature-specific hydration enums with
Hexalith.FrontComposer.Shell.State.HydrationState. Consumers must update type
references and recompile for FrontComposer 2.0.0.
```

This evidence does not authorize a direct commit to `main`; repository branch, commitlint, review, and PR rules still apply.
