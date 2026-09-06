---
title: 'Low logging governance hardening'
type: 'refactor'
created: '2026-09-06'
status: 'in-progress'
baseline_revision: '4d1fa4f9308c2e32a2ef78489296a36d8c9b41cf'
review_loop_iteration: 0
followup_review_recommended: false
context: []
warnings: [multiple-goals, oversized]
deferred: []
---

<intent-contract>

## Intent

**Problem:** MCP helpers still compute formatted logging arguments directly at generated-call sites, Shell diagnostic inventory expectations are duplicated across two observers, and many Shell logger substitutes default `IsEnabled` to false, making log assertions vulnerable to false passes.

**Approach:** Complete the established post-`IsEnabled` local-binding pattern, centralize only the normalized diagnostic inventory expectations, and route every general-purpose Shell logger substitute through one all-level-enabled test factory with focused drift and short-circuit regression coverage.

## Boundaries & Constraints

**Always:** Preserve log levels, EventIds, EventNames, templates, sanitization, exception attachment, and runtime behavior; retain independent Roslyn-source and reflection metadata extraction; keep one C# type per file; validate the MCP and Shell test projects independently under `Recommended` analyzers.

**Never:** Edit `_bmad-output/implementation-artifacts/deferred-work.md` or any `.bmad-loop` ledger; weaken existing governance, security, collision, placeholder, or sanitization assertions; centralize the two inventory extraction mechanisms; enable loggers intended specifically to prove disabled/null behavior; change public, schema, wire, lifecycle, or MCP contracts.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Disabled generated logger | Any wrapper input with its level disabled | Wrapper returns before computed formatting/sanitization reaches the generated call | No log and no exception |
| Diagnostic inventory | Roslyn or reflection view of `FrontComposerDiagnosticLog` | Exactly IDs 6000-6072, 73 unique nonblank names, 56 Information, 17 Debug, 20 exception-bearing methods | Assertion identifies the divergent entry/location |
| Enabled Shell substitute | Any `LogLevel`, including a future wrapper level | `IsEnabled` returns true and generated wrapper calls are observable | Tests fail on missing or wrong EventId/level/name |

</intent-contract>

## Code Map

- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs:20-183` -- six wrappers still pass `ToString`/sanitizer results inline; lines 104-132 and 161-183 are the established guarded-local pattern.
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs:80-85` -- lifecycle read helper passes exception-type formatting inline after its warning guard.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs:35-121` -- Roslyn governance seam for rejecting eager/non-local generated-call arguments, including synthetic negative coverage.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs:324-353,599-629,979-989` -- independent Roslyn extractor and duplicated 73/56/17/20 expectations; retain cross-family collision and placeholder guards locally.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs:25-46,181-203` -- reflection observer and duplicated inventory/wrapper counts; reflection must pair attributed methods with attributes to count exception parameters.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/CapturingLogger.cs` -- existing always-enabled capture semantics and placement precedent for shared telemetry test helpers.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs:38-57` -- concrete Debug/EventId 6070 generated-wrapper regression surface.
- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/ReflectionActionQueueProjectionCatalogTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/DerivedValueProviderChainTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` -- Badge/service/shortcut logger-substitute inventory; the HFC2112/HFC2113 negative assertion at `BadgeCountServiceTests.cs:158-176` is currently vacuous.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsTests.cs` -- State-test logger-substitute inventory, including five duplicated enabled factories.

## Tasks & Acceptance

**Execution:**
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- bind every computed generated-log argument to a descriptive local after the existing `IsEnabled` guard; preserve output contracts.
- `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs` -- add a source guard plus synthetic failing case that rejects inline computed arguments at generated logging calls while allowing guarded locals and approved direct parameters.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogInventoryEntry.cs`, `FrontComposerDiagnosticLogInventoryAssertions.cs` -- add a normalized entry and the single authoritative 6000/73/56/17/20 assertion set, including location-rich failures.
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs` -- project independently extracted metadata into the shared entry/assertion; reuse its event-count constant for wrapper census and retain observer-specific guards.
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/EnabledLoggerSubstitute.cs`, `EnabledLoggerSubstituteTests.cs` -- add and prove one generic NSubstitute factory whose `IsEnabled` is true for every level.
- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/ReflectionActionQueueProjectionCatalogTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/DerivedValueProviderChainTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` -- replace general-purpose logger substitutes with the shared factory; preserve explicit null/disabled test doubles.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsTests.cs` -- replace general-purpose logger substitutes and five duplicated local enabled factories with the shared factory; preserve explicit null/disabled test doubles.
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs` -- assert the enabled path emits Debug HFC event 6070 once and make the existing HFC2112/HFC2113 negative assertions non-vacuous.

**Acceptance Criteria:**
- Given any MCP generated-helper invocation, when governance scans its arguments, then computed projections are bound only after the applicable `IsEnabled` guard and inline computed arguments fail the synthetic guard.
- Given either independent diagnostic metadata observer, when inventory assertions run, then the shared exact ID/name/level/exception contract passes and any one-value drift fails both guard surfaces.
- Given any general-purpose Shell `ILogger<T>` substitute factory, when a generated wrapper checks any level, then logging is enabled; only tests explicitly exercising disabled/null behavior may retain a false logger.
- Given the empty-to-ready scope transition, when `ScopeReadinessGate` dispatches, then exactly one Debug event with EventId 6070 and name `ScopeReadinessStorageReadyDispatched` is observable.
- Given a non-action-queue badge notification, when the no-op completes, then enabled logging records neither HFC2112 nor HFC2113.

## Spec Change Log

## Review Triage Log

## Design Notes

Share expected inventory values, not observation logic. The architecture test continues to parse source with Roslyn; the telemetry test continues to reflect compiled attributed methods. Both normalize into the same entry and call the same exact assertion helper, preventing expectation drift without creating a shared blind spot.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Mcp.Tests.Logging.FrontComposerMcpLogTests -class Hexalith.FrontComposer.Mcp.Tests.Logging.FailClosedLoggingGovernanceTests` -- expected: focused MCP logging tests pass.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry.FrontComposerDiagnosticLogTests -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry.EnabledLoggerSubstituteTests -class Hexalith.FrontComposer.Shell.Tests.State.Navigation.ScopeReadinessGateTests -class Hexalith.FrontComposer.Shell.Tests.Badges.BadgeCountServiceTests` -- expected: focused Shell governance and regression tests pass.
- `rg -n 'Substitute\.For<ILogger(?:<[^>]+>)?>\(\)' tests/Hexalith.FrontComposer.Shell.Tests --glob '*.cs'` -- expected: only the centralized enabled factory (plus explicitly documented disabled/null fixtures, if any).
