---
title: 'Low logging governance hardening'
type: 'refactor'
created: '2026-09-06'
status: 'done'
baseline_commit: '4d1fa4f9308c2e32a2ef78489296a36d8c9b41cf'
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
- [x] `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`, `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleTracker.cs` -- bind every computed generated-log argument to a descriptive local after the existing `IsEnabled` guard; preserve output contracts.
- [x] `tests/Hexalith.FrontComposer.Mcp.Tests/Logging/FailClosedLoggingGovernanceTests.cs` -- add a source guard plus synthetic failing case that rejects inline computed arguments at generated logging calls while allowing guarded locals and approved direct parameters.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogInventoryEntry.cs`, `FrontComposerDiagnosticLogInventoryAssertions.cs` -- add a normalized entry and the single authoritative 6000/73/56/17/20 assertion set, including location-rich failures.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs` -- project independently extracted metadata into the shared entry/assertion; reuse its event-count constant for wrapper census and retain observer-specific guards.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/EnabledLoggerSubstitute.cs`, `EnabledLoggerSubstituteTests.cs` -- add and prove one generic NSubstitute factory whose `IsEnabled` is true for every level.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/ReflectionActionQueueProjectionCatalogTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/CommandPaletteE2ETests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/DerivedValueProviderChainTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs` -- replace general-purpose logger substitutes with the shared factory; preserve explicit null/disabled test doubles.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/CapabilityDiscovery/CapabilityDiscoveryEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/CommandPalette/CommandPaletteEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/DataGridNavigationEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Density/DensityEffectsTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsScopeTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/State/Theme/ThemeEffectsTests.cs` -- replace general-purpose logger substitutes and five duplicated local enabled factories with the shared factory; preserve explicit null/disabled test doubles.
- [x] `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/ScopeReadinessGateTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs` -- assert the enabled path emits Debug HFC event 6070 once and make the existing HFC2112/HFC2113 negative assertions non-vacuous.

**Acceptance Criteria:**
- Given any MCP generated-helper invocation, when governance scans its arguments, then computed projections are bound only after the applicable `IsEnabled` guard and inline computed arguments fail the synthetic guard.
- Given either independent diagnostic metadata observer, when inventory assertions run, then the shared exact ID/name/level/exception contract passes and any one-value drift fails both guard surfaces.
- Given any general-purpose Shell `ILogger<T>` substitute factory, when a generated wrapper checks any level, then logging is enabled; only tests explicitly exercising disabled/null behavior may retain a false logger.
- Given the empty-to-ready scope transition, when `ScopeReadinessGate` dispatches, then exactly one Debug event with EventId 6070 and name `ScopeReadinessStorageReadyDispatched` is observable.
- Given a non-action-queue badge notification, when the no-op completes, then enabled logging records neither HFC2112 nor HFC2113.

## Spec Change Log

## Review Triage Log

Three layers ran (blind-hunter, edge-case-hunter, verification-gap). The staged diff initially
spanned nine unrelated commits that share `tests/Hexalith.FrontComposer.Shell.Tests/`; findings
against those commits are recorded below as out-of-change and were **not** written to
`deferred-work.md`, which this spec's Never-list forbids editing. They are surfaced to the human
in the completion report instead.

### This change — actioned

| Finding | Verdict | Evidence | Route |
|---|---|---|---|
| `DisabledLogger_AllWrappersReturnWithoutEmitting` cannot detect a wrapper whose guard checks the wrong `LogLevel`: the `[LoggerMessage]` partial performs its own `IsEnabled` check, so the test passes even with the hand guard deleted or mis-levelled. | high | Confirmed by mutation: changing `ToolsListFailedClosed`'s guard to `LogLevel.Trace` left all pre-existing tests green. In production that silently suppresses every 8310 fail-closed warning. | patch |
| No completeness pin — a new public wrapper is silently uncovered by the disabled-logger test. | low | `FrontComposerMcpLogTests` enumerated nine wrappers by hand with nothing binding the list to the type. | patch |
| The five newly converted wrappers and `FrontComposerMcpLifecycleTracker.LogReadFailure` lack the `// CA1873:` rationale their three siblings carry, so a later reader has no reason not to inline the locals back. | low | `FrontComposerMcpLog.cs:117,135,175` carry the comment; the converted six did not. | patch |
| `using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;` sorted before `...Shell.State.Navigation` in three State test files; new usings appended after the `Microsoft.CodeAnalysis.*` group in `SecurityLoggingGovernanceTests.cs`. | low | Confirmed by reading the files. No analyzer or CI gate enforces it (Release build is warning-clean), so cosmetic; fix is a direct reorder. | patch |

### This change — rejected on refutation

| Finding | Verdict | Evidence |
|---|---|---|
| `DisabledLogger_…` "passes vacuously on an empty call list". | false | NSubstitute records `IsEnabled` calls, so `ReceivedCalls()` is non-empty; the assertion does discriminate `Log` calls. The real weakness was the level blindness, actioned above. |
| `EnabledLoggerSubstitute` needs a `BeginScope` stub or callers NRE. | false | No production type under test calls `ILogger.BeginScope`; every repository hit is a test double implementing it. |
| Enabling every level erases level-specific coverage at converted call sites. | false | The converted tests assert the level explicitly via `CountLoggedAtLevel(logger, LogLevel.X, ...)`; enabling all levels removes a false-pass risk without dropping the level contract. |
| `EnabledLoggerSubstitute` lacks a non-generic `ILogger` overload for the MCP tests. | false | It lives in `Shell.Tests`; MCP is a separate project and cannot reference it. Sharing was never in scope. |
| Governance guard does not flag a generated call that has no `IsEnabled` guard at all. | false | When no guard is found, `enabledGuard` is null and any non-parameter local argument yields a violation. A call passing only direct parameters computes nothing, so it correctly needs no guard. |
| Removing the `Trace` allowance could reject a future Trace-level diagnostic event. | false | The frozen matrix pins the inventory at 56 Information + 17 Debug = 73; permitting only those two levels is the contract, not a regression. |
| `IsEnabled(LogLevel.None)` returns true on the shared substitute. | low → rejected | No code under test uses `LogLevel.None` as an off sentinel; the fix adds an `Arg.Is` matcher, i.e. complexity for an unreachable case. |
| The diff is not scoped to the spec's file list / "the patch is not self-contained". | false | An artifact of diff staging, not of the change: nine unrelated commits touched the same directories between the baseline and HEAD. Re-scoping to commit `8cabbf54` yields exactly the spec's 28 files. |

### This change — governance guard precision, rejected as low

Root cause: `FindGeneratedLogArgumentViolations` analyzes only the invocation's own `BlockSyntax`
and only bare-identifier argument forms.

| Finding | Verdict | Evidence |
|---|---|---|
| A correctly guarded generated call nested in `if`/`try`/`using` is reported as a violation (guard and locals in an enclosing block are not found). | low | Confirmed by reading the code. Unreachable today — no call site nests. |
| A positive-form guard `if (logger.IsEnabled(x)) { Log(...); }` is rejected; only early-return is accepted. | low | `IsDisabledLoggerEarlyReturnGuard` requires an unconditional return. Unreachable today. |
| Non-identifier arguments — string literals, `nameof(...)` — are flagged even though they are free to evaluate. | low | `argument.Expression is not IdentifierNameSyntax` yields a violation. No current call site passes a literal. |
| Out-vars, pattern variables and `foreach` variables are not recognised as guarded declarations. | low | Only `LocalDeclarationStatementSyntax` is scanned. Unreachable today. |
| `loggerParameterName` resolves only when the first argument is a bare identifier, so `this.logger` or a named argument defeats guard detection. | low | Confirmed; MCP wrappers are static and take `ILogger logger`, so unreachable today. |
| `IsDirectParameter` exempts any name matching a parameter, so a parameter reassigned before the guard escapes detection. | low | Confirmed; contrived, and C# forbids a local shadowing a parameter. |
| `generatedMethodNames` is collected per source file, so a `[LoggerMessage]` partial split across files would be skipped; a qualified call expression is not inspected. | low | Confirmed. `[LoggerMessage]` appears only in two self-contained files today. |

All seven are developer-only, unreachable in the current tree, and each fix adds branches to the
rule. Rejected per the low-finding rule (unlikely to be met in everyday use **and** the fix adds
complexity rather than being a direct correction). Recorded here so the next author who nests a
generated call knows where to look.

### Not this change — out-of-change commits sharing the directory

Findings against the nine unrelated commits between baseline `4d1fa4f9` and HEAD. Not written to
the deferred-work ledger (Never-list); reported to the human.

| Finding | Owning commit area |
|---|---|
| `CiGovernanceTests.ExtractJobBlock` has two compensating off-by-one errors and returns a truncated header for a body-less job; it has no direct unit test. | CI governance (Gate 4) |
| New Dapr assertions are unanchored `ShouldContain` and pass when only one of the two installs in `quality.yml` is bumped, unlike the Aspire check beside them. | CI governance |
| The 4.3.0 published baseline is pinned in two unlinked places (`CiGovernanceTests` literal and `McpRuntimePackageBoundaryTests.PublishedBaselineVersion`) with nothing binding either to `Directory.Build.targets`. | package validation |
| `PublishedPackageValidationBaseline` reads the value via `--plan` then asserts it equals the same hardcoded literal. | package validation |
| Aspire catalog falls back to a path outside the repository with no assertion recording which path was used. | CI governance |
| Plan JSON parsing can throw `KeyNotFoundException` instead of failing diagnosably. | CI governance |
| `AnalyzerPolicyGovernanceTests` restates `timeout-minutes: 45` in prose instead of asserting it, and the 420,000 ms deadline rationale was not revisited after the job ceiling doubled. | analyzer policy |
| EventStore runtime-identity test compares two hand-edited literals without checking the actual `references/Hexalith.Builds` gitlink. | EventStore identity |
| `Subscribe_DuringAutomaticReconnect_…` was relaxed with `ignoreOrder: true` while a sibling still asserts ordered rejoin; the new tests duplicate construction and skip `ConfigureAwait`. | projection realtime |
| SignalR factory test uses `HttpConnectionOptions` after the `ServiceProvider` is disposed; empty-string scope is untested. | projection realtime |
| `CounterStoryVerificationTests` can hang in teardown if an assertion fails before `SetVoidResult`; overlapping `DisposeAsync` reports completion before JS cleanup lands; three unrelated contracts ride on an indicator-cleanup test. | Counter disposal |
| `ToolsList`/`LifecyclePrecheck`/`ProjectionReader` log `category.ToString()` (raw integer for an undefined enum) where sibling wrappers use `BoundedCategory` (`"Unknown"`). Pre-existing; this change only moved it into a local. | pre-existing, `FrontComposerMcpLog` |


## Design Notes

Share expected inventory values, not observation logic. The architecture test continues to parse source with Roslyn; the telemetry test continues to reflect compiled attributed methods. Both normalize into the same entry and call the same exact assertion helper, preventing expectation drift without creating a shared blind spot.

## Verification

**Commands:**
- `dotnet build tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj --configuration Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Mcp.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Mcp.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Mcp.Tests.Logging.FrontComposerMcpLogTests -class Hexalith.FrontComposer.Mcp.Tests.Logging.FailClosedLoggingGovernanceTests` -- expected: focused MCP logging tests pass.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0` -- expected: zero warnings/errors.
- `DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -noLogo -noColor -parallel none -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry.FrontComposerDiagnosticLogTests -class Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry.EnabledLoggerSubstituteTests -class Hexalith.FrontComposer.Shell.Tests.State.Navigation.ScopeReadinessGateTests -class Hexalith.FrontComposer.Shell.Tests.Badges.BadgeCountServiceTests` -- expected: focused Shell governance and regression tests pass.
- `rg -n 'Substitute\.For<ILogger(?:<[^>]+>)?>\(\)' tests/Hexalith.FrontComposer.Shell.Tests --glob '*.cs'` -- expected: only the centralized enabled factory (plus explicitly documented disabled/null fixtures, if any).
