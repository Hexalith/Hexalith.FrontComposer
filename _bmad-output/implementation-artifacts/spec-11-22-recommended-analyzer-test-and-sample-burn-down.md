---
title: 'Story 11.22 Recommended analyzer test and sample burn-down'
type: 'refactor'
created: '2026-08-08'
status: 'done'
review_loop_iteration: 0
baseline_commit: 'c3154b9b7c2cadf3bb42a8cc83ea7ede278f58a5'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/11-22-recommended-analyzer-test-and-sample-burndown.md'
  - '{project-root}/_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** After Story 11.21 removed product and generated-output debt, Recommended analysis still reports 343 hand-authored test/benchmark findings and two Counter.Web findings; project controls additionally hide 17 ASP0006 and six CA2007 sites. This blocks the repository-wide activation owned by Story 11.23.

**Approach:** Rebase the census at the implementation commit, fix deterministic test/sample code by diagnostic class, replace broad project controls with semantic fixes or exact approved fixture exceptions, and seal the result in the canonical ledger and Governance tests without changing product behavior.

## Boundaries & Constraints

**Always:** Treat approval of this spec as Story 11.22's separate Architecture/Product approval and explicit acceptance that the substantively complete Stories 11.20 and 11.21 remain `review` in sprint status. Preserve underscore-separated three-part test names and the exact `[tests/**.cs]` CA1707 policy. Keep root `TreatWarningsAsErrors=true`, built-in analyzers only, and central `AnalysisMode` absent. Preserve test intent, xUnit continuation semantics, generated artifacts, package/public/schema/wire contracts, Fluent v5 UI, and Counter.Web's Development/Test-only fake-auth and MCP fail-closed boundaries. Record every retained exception at exact method/site scope with rationale, owner, review date, trigger, and evidence.

**Ask First:** Census growth above 5%; a new exception class beyond the approved exact CA2012 NSubstitute `ValueTask` fixtures and CA2201 fatal-exception fixtures; any product, emitter, public API, snapshot, Pact, generated-output, UX, release-workflow, dependency, or submodule change.

**Never:** Rename tests to satisfy CA1707; add repository/category/project-wide analyzer suppression; weaken warnings-as-errors; add analyzer packages; activate Recommended centrally; edit `obj/**`; mass-accept baselines; alter invalid-code fixture strings merely to silence analyzers.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Actionable finding | Recommended diagnostic in test/sample source | Semantic, behavior-preserving fix; strict project build reaches zero | Escalate if the fix changes a protected contract |
| Intentional fixture | CA2012 substitute or CA2201 fatal-exception specimen | Exact-site suppression plus governed ledger disposition | Reject file/project/global suppression |
| Hidden control | ASP0006 or CA2007 project `NoWarn` | Stable literal sequences or context-preserving async disposal; remove `NoWarn` | Negative-control build must expose zero owned sites |
| Drift | Census differs from predecessor | Append commit/toolchain-stamped reconciliation | Halt above 5% or on unmatched product/generated findings |

</frozen-after-approval>

## Code Map

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- append `story1122Census`/completion evidence and replace the two moved-control dispositions; do not rewrite historical blocks.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` -- closed-world ledger/control parity, CA1707 seal, effective-build checks, and new 11.22 census/completion validation.
- `.editorconfig` -- read-only CA1707 test convention and root analyzer policy.
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` -- remove ASP0006 `NoWarn` after fixing 17 literal-sequence sites in `Generated/ActionQueueProjectionContextIsolationTests.cs`, `Components/Rendering/FcProjectionViewOverrideHostTests.cs`, and `Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs`.
- `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj` -- remove CA2007 `NoWarn`; repair five `FrontComposerTestHostTests.cs` and one `TestingFailureModeTests.cs` async-disposal sites without losing test context.
- `tests/Hexalith.FrontComposer.{SourceTools,Shell,Mcp,Testing,Contracts,Contracts.UI,Cli}.Tests/**` and `tests/Hexalith.FrontComposer.Shell.Tests.Bench/**` -- semantic fixes by diagnostic/project; keep raw Roslyn-invalid fixtures intact and use exact approved CA2012/CA2201 exceptions only.
- `samples/Counter/Counter.Web/CounterProjectionEffects.cs`, `CounterFakeAuthLogging.cs`, and `Program.cs` plus `CounterFakeAuthIntegrationTests.cs` -- resolve CA1826/CA1848 while preserving sample behavior and directly proving the Critical anti-deployment event contract.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs` and `src/Hexalith.FrontComposer.Contracts/Conformance/GeneratedOutputPathContract.cs` -- read-only package/generated-output proof.

## Tasks & Acceptance

**Execution:**
- [x] Re-run and deduplicate the full Recommended census; append commit, SDK/MSBuild/Roslyn, UTC, command, per-project/ID/location/origin, +3 drift reconciliation, and hidden-control probes to the ledger.
- [x] Fix 345 visible findings by project and diagnostic class, using invariant culture for deterministic diagnostics/source text and concrete/cached collections or regexes only where lifetime and fixture semantics remain unchanged.
- [x] Replace all 17 runtime render-tree counters with stable call-site literals, repair six CA2007 disposal sites, remove both project `NoWarn` entries, and prove their negative controls clean.
- [x] Add exact CA2012/CA2201 fixture dispositions and strengthen Governance so unmatched findings, stale rows, broadened controls, or count drift fail closed.
- [x] Run strict Recommended builds for all eight test/benchmark and five sample projects, each changed test assembly, default/Governance/Contract lanes, artifact/baseline gates, and the normal Release solution build.

**Acceptance Criteria:**
- Given the approved CA1707 convention, when test sources change, then test naming and the exact path-scoped policy remain intact and the identifier seal is intentionally refreshed.
- Given generated consumers were clean after 11.21, when 11.22 completes, then no product/generated finding returns and Counter.Web retains its Fluent/MCP/auth boundaries.
- Given intentional invalid, fatal, async, and render-tree fixtures, when diagnostics are remediated, then behavior is preserved and every remaining exception is exact, governed, dated, and trigger-bound.
- Given candidate validation, when every owned project builds, then actionable Recommended findings are zero, both removed-control probes are zero, all tests have zero failures/skips, protected baselines are unchanged unless explicitly approved, and normal Release is 0 warnings/0 errors.

### Review Findings

_Chunk 1 — Governance & contracts (`c3154b9b...HEAD`, 2026-08-08)._

- [x] [Review][Decision] Predecessor closeouts vs frozen Always — Accepted: separate 11.20/11.21 code reviews supersede the frozen “remain `review`” clause; keep status/`sourceCommit`/story closeouts as landed (no revert).
- [x] [Review][Patch] Repo-wide CA2012/CA2201 fixture pragma seal [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:807`]
- [x] [Review][Patch] Re-execute ASP0006/CA2007 negative-control probes (not JSON-only) [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:690`]
- [x] [Review][Patch] Match CA2012/CA2201 restore pragmas that carry trailing comments [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:841`]
- [x] [Review][Patch] Clear `followUpStory` on fixed dispositions `testing-ca2007-audit` and `asp0006-hand-authored-fixture-debt` [`_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json:273`]
- [x] [Review][Patch] Resolve obsolete Shell.Tests ASP0006 deferred-work bullet after NoWarn removal [`_bmad-output/implementation-artifacts/deferred-work.md:5`]
- [x] [Review][Patch] Align traditional story artifact `11-22-...burndown.md` (still `backlog`/open ACs) with spec/sprint completion bookkeeping [`_bmad-output/implementation-artifacts/11-22-recommended-analyzer-test-and-sample-burndown.md:10`]
- [x] [Review][Patch] `FindDisposition` should fail-closed into `errors` instead of throwing on missing/duplicate keys [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:970`]
- [x] [Review][Patch] Close `hiddenControlProbes` to exactly ASP0006 and CA2007 [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:689`]
- [x] [Review][Patch] Fail-closed on non-integer census count JSON in `SumCounts` / `byLocation` [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:900`]
- [x] [Review][Defer] Thirteen-project Recommended Governance rebuild gate is expensive [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:316`] — deferred, pre-existing
- [x] [Review][Defer] `RunDotnetResultAsync` timeout path can orphan output awaits [`tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs:1714`] — deferred, pre-existing

_Chunk 2 — Samples + product/src (+ Tenants gitlink) (`c3154b9b...HEAD`, 2026-08-08)._

- [x] [Review][Decision] Predecessor product/emitter/gitlink vs 11.22 Ask First — Accepted carve-out: 11.20/11.21 review closeouts + Spec Verification’s Tenants-unrelated note cover those hunks; leave tree as-is; 11.22 evidence stays sample/test-owned.
- [x] [Review][Patch] Restore null-safe first-item Count read after CA1826 rewrite [`samples/Counter/Counter.Web/CounterProjectionEffects.cs:68`]
- [x] [Review][Defer] ETag LRU seed still races Dispose after the post-gate re-check [`src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs:340`] — deferred, pre-existing
- [x] [Review][Defer] FrontComposerMcpLog CA1873 local-binding remains incomplete on sibling helpers [`src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs:127`] — deferred, pre-existing
- [x] [Review][Defer] StartsArgumentList still ignores comments/trivia between `(` and the operator [`src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs:248`] — deferred, pre-existing
- [x] [Review][Defer] GeneratedLogMethodEmitter validates whitespace but not C# identifier shape [`src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs:217`] — deferred, pre-existing

_Chunk 3 — Shell / Testing / Mcp / Contracts / Cli tests (`c3154b9b...HEAD`, 2026-08-08)._

- [x] [Review][Patch] Add `SetKey` on ActionQueue multi-row CascadingValue loop after ASP0006 literal rewrite [`tests/Hexalith.FrontComposer.Shell.Tests/Generated/ActionQueueProjectionContextIsolationTests.cs:118`]
- [x] [Review][Patch] Append seven chunk-3 owned paths missing from Spec File List [`_bmad-output/implementation-artifacts/spec-11-22-recommended-analyzer-test-and-sample-burn-down.md:123`]
- [x] [Review][Patch] Fail fast in `CreateSampleArgument` for unknown NullLogger wrapper parameter types [`tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs:252`]
- [x] [Review][Patch] Align new CA1513 dispose pins to assert `ObjectName` like FaultInjecting tests [`tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateServiceTests.cs:83`]
- [x] [Review][Defer] `ServiceProvider` helpers from CA1859 narrowing still leak undisposed providers at call sites [`tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs:48`] — deferred, pre-existing

## Spec Change Log

- 2026-08-08: Chunk-3 code-review — ActionQueue `SetKey` after ASP0006 rewrite; File List honesty for seven owned paths; NullLogger sample-arg fail-closed; ObjectName dispose-pin alignment; deferred undisposed `ServiceProvider` helpers.
- 2026-08-08: Chunk-2 code-review — accepted predecessor product/emitter/Tenants carve-out; restored null-safe `items[0]?.Count` in Counter.Web projection effect after CA1826 rewrite.
- 2026-08-08: Chunk-1 code-review patches — repo-wide CA2012/CA2201 fixture seal, executable ASP0006/CA2007 negative-control probes, restore-comment matching, fail-closed disposition/count/probe guards, cleared `followUpStory` on fixed dispositions, resealed identifier inventory (6,359 / `6dd0420b...`), aligned traditional story artifact, and resolved the obsolete ASP0006 deferred-work bullet.
- 2026-08-08: Implemented the approved 345-finding test/sample burn-down, removed the ASP0006 and CA2007 project controls, sealed exact retained fixture exceptions, refreshed governance evidence, and completed the required verification matrix.

## Design Notes

The implementation baseline is `85fb8865e96ad2cef9aec3ac67f1e805386b5347`: 345 visible findings (SourceTools.Tests 229, Shell.Tests 70, Mcp.Tests 25, Testing.Tests 7, Contracts.Tests 6, Bench 4, Cli.Tests 2, Counter.Web 2) plus 17 ASP0006 and six CA2007 negative-control sites. The visible distribution is led by CA1305 (195), CA1859 (65), CA1861 (23), CA1875/CA2012 (12 each). Reconcile rather than overwrite Story 11.21's historical 342 handoff.

- The implementation census was reproduced at frontmatter baseline `c3154b9b7c2cadf3bb42a8cc83ea7ede278f58a5`; the ledger preserves the earlier `85fb8865e96ad2cef9aec3ac67f1e805386b5347` implementation-baseline coordinate and the Story 11.21 historical blocks.
- Deterministic diagnostic/source-text assertions now use invariant culture. Performance remediations use concrete collection types, cached arrays/options/composite formats, direct collection access, and ordinal string APIs without changing fixture behavior.
- The twelve CA2012 findings remain only within eleven directive-adjacent NSubstitute `ValueTask` statement spans, and the four CA2201 findings remain only within four directive-adjacent fatal-exception construction/throw spans. Governance seals each normalized syntax span so a method-level broadening fails closed; the ledger records each exact site group with owner, review date, trigger, rationale, and evidence.
- Counter.Web keeps the Development/Test-only fake-auth boundary and its single Critical anti-deployment warning. A dedicated source-generated `LoggerMessage` type owns the exact EventId/name/message contract, and a direct capture test proves the emission; the projection effect keeps empty-list behavior while avoiding LINQ indexing.

## Verification

**Commands:**
- `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended -p:TreatWarningsAsErrors=false` -- census only; zero product/generated locations and reconciled owned counts.
- Strict matrix pattern: `dotnet build <project> -c Release --no-restore --no-incremental -m:1 /nr:false -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended`, with no TWAE override. The exact 13-project matrix is `tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj`, `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`, `tests/Hexalith.FrontComposer.Mcp.Tests/Hexalith.FrontComposer.Mcp.Tests.csproj`, `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj`, `tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj`, `tests/Hexalith.FrontComposer.Cli.Tests/Hexalith.FrontComposer.Cli.Tests.csproj`, `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj`, `samples/Counter/Counter.Domain/Counter.Domain.csproj`, `samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj`, `samples/Counter/Counter.Specimens/Counter.Specimens.csproj`, `samples/Counter/Counter.Web/Counter.Web.csproj`, and `samples/IdeParityCounter/IdeParityCounter.csproj`.
- Direct xUnit v3 pattern: `DiffEngine_Disabled=true <assembly> -noLogo -noColor -parallel none`. The exact eight-assembly matrix is the Release/net10.0 executable under each of `Hexalith.FrontComposer.SourceTools.Tests`, `Hexalith.FrontComposer.Shell.Tests`, `Hexalith.FrontComposer.Mcp.Tests`, `Hexalith.FrontComposer.Testing.Tests`, `Hexalith.FrontComposer.Contracts.Tests`, `Hexalith.FrontComposer.Contracts.UI.Tests`, `Hexalith.FrontComposer.Cli.Tests`, and `Hexalith.FrontComposer.Shell.Tests.Bench`; Shell additionally runs `-trait Category=Governance` and `-trait Category=Contract`.
- `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1`, `pwsh ./eng/validate-contract-artifacts.ps1`, story-artifact validation, and `git diff --check` -- all green; no unintended Verify/PublicAPI/Pact/generated-output diff.

**Results:**

- Full Recommended census: 345 -> 0 warnings, 0 errors; zero product/generated locations.
- Strict Recommended builds: all 13 owned test/benchmark/sample projects passed with 0 warnings and 0 errors. Shell.Tests and Testing.Tests negative-control builds also passed with the former ASP0006/CA2007 project additions absent.
- Direct xUnit v3 default lanes: 4,339 tests across eight assemblies, 0 errors, 0 failed, 0 skipped, 0 not run. Shell Governance: 219 passed; Shell Contract: 3 passed.
- Normal Release solution build: 0 warnings, 0 errors. Contract artifact validation passed. `git diff --check` passed. No Verify/PublicAPI/Pact artifact or generated-output baseline was accepted or modified.
- `references/Hexalith.Tenants` moved concurrently in the shared root workspace and is unrelated to Story 11.22; story-artifact validation records it through the validator's explicit unrelated-path mechanism.

## File List

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/11-22-recommended-analyzer-test-and-sample-burndown.md`
- `_bmad-output/implementation-artifacts/spec-11-22-recommended-analyzer-test-and-sample-burn-down.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `samples/Counter/Counter.Web/CounterProjectionEffects.cs`
- `samples/Counter/Counter.Web/Counter.Web.csproj`
- `samples/Counter/Counter.Web/CounterFakeAuthLogging.cs`
- `samples/Counter/Counter.Web/Program.cs`
- `tests/Hexalith.FrontComposer.Cli.Tests/Architecture/CliTypeOrganizationGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Attributes/ProjectionRoleEnumTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/EventStoreContractTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Communication/Story52ResponseSurfaceTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/CanonicalSchemaMaterialFingerprintVectorTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/AuthContextAccessorTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandInvokerTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/CommandLifecycleTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpCommandToolAdapterTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/ProjectionReaderTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Rendering/McpMarkdownProjectionRendererTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Schema/Story11_5ResolutionTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusAggregateManifestRenderTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillTypeOrganizationGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Skills/BenchmarkHarnessTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/NFR17ComplianceTripwireTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/ShellLayeringTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SliceSingleWriterGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcColumnPrioritizerTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FrontComposerShellTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Rendering/FcProjectionViewOverrideHostTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ActionQueueProjectionContextIsolationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererCompactInlineTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandRendererInlineTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/ExpandInRowGeneratedGridTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AppHostNuGetAuditPolicyTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/FluentConformanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/EventStoreQueryCacheIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/FaultInjectingProjectionHubConnection.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/FaultInjectingProjectionHubConnectionTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/ProjectionSubscriptionServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Tenancy/TenantContextValidationMatrixTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/EventStorePactContractTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Authorization/CommandDispatchAuthorizationGateTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/CounterFakeAuthIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/DerivedValueProviderChainTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/EmptyStateCtaResolverTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Lifecycle/LifecycleStateServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Services/Validation/ServerValidationApplicatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/DataGridNavigation/LoadedPageStateCacheBoundTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/Navigation/NavigationEffectsLastActiveRouteTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/PendingCommands/PendingCommandPollingCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinatorTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Architecture/SourceToolsTypeOrganizationGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Benchmarks/IncrementalRebuildBenchmarkTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/CustomizationAccessibilityAnalyzerTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticCatalogTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticDescriptorTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1008DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1025DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1027DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1028DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1029DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1030DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1031DiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/Hfc1047To1049DevModeReservationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/QueryRequestDeprecationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaTruncationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/FcDocComponentDocumentationContractTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftAnalyzerConfigOptionsTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineMissingDiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Baseline/DriftBaselineTrustFailureTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierBoundedContextTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierMetadataTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierProjectionPropertyTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierRenameTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierTypeAndNullabilityTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticContractTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticOrderingAndTruncationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticPrecedenceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Diagnostics/DriftDiagnosticRedactionTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Incremental/DriftIncrementalCacheTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftByteStabilityRegressionTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureInvarianceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/TrimAot/TrimAotReflectionCatalogDiagnosticTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CounterProjectionApprovalTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityConformanceHelpers.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/IdeParity/IdeParityConformanceUtilityTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CommandLifecycleBridgeIntegrationTest.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/CounterDomainIntegrationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/GeneratorDriverTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/CommandParserTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Performance/ParseStagePerformanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Schema/SchemaFixtureCatalogTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Transforms/CommandFormTransformTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj`
- `tests/Hexalith.FrontComposer.Testing.Tests/TestingFailureModeTests.cs`

## Suggested Review Order

**Analyzer policy closure**

- This executable gate rebuilds the exact thirteen-project Recommended matrix.
  [`AnalyzerPolicyGovernanceTests.cs:294`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L294)

- The census records the reconciled baseline before remediation.
  [`analyzer-policy-exception-ledger-v1.json:849`](../contracts/analyzer-policy-exception-ledger-v1.json#L849)

- Completion evidence seals projects, assemblies, outcomes, and hidden-control probes.
  [`analyzer-policy-exception-ledger-v1.json:1013`](../contracts/analyzer-policy-exception-ledger-v1.json#L1013)

- Syntax digests prevent intentional fixture exceptions from silently broadening.
  [`AnalyzerPolicyGovernanceTests.cs:70`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L70)

**Hidden-control removal**

- Stable call-site literals replace runtime render-tree sequence counters.
  [`ActionQueueProjectionContextIsolationTests.cs:106`](../../tests/Hexalith.FrontComposer.Shell.Tests/Generated/ActionQueueProjectionContextIsolationTests.cs#L106)

- Context-preserving async disposal removes the Testing project CA2007 control.
  [`FrontComposerTestHostTests.cs:73`](../../tests/Hexalith.FrontComposer.Testing.Tests/FrontComposerTestHostTests.cs#L73)

**Intentional fixture exceptions**

- Directive-adjacent CA2012 spans preserve NSubstitute ValueTask fixture semantics.
  [`DataGridFocusScopeTests.cs:18`](../../tests/Hexalith.FrontComposer.Shell.Tests/Services/DataGridFocusScopeTests.cs#L18)

- Directive-adjacent CA2201 spans preserve fatal-exception classification specimens.
  [`ExceptionGuardTests.cs:13`](../../tests/Hexalith.FrontComposer.Shell.Tests/Services/ExceptionGuardTests.cs#L13)

**Counter sample safety**

- Source-generated logging owns the Critical anti-deployment event contract.
  [`CounterFakeAuthLogging.cs:8`](../../samples/Counter/Counter.Web/CounterFakeAuthLogging.cs#L8)

- The existing environment guard still gates fake authentication before registration.
  [`Program.cs:149`](../../samples/Counter/Counter.Web/Program.cs#L149)

- Direct capture proves the exact Critical event identity and message.
  [`CounterFakeAuthIntegrationTests.cs:55`](../../tests/Hexalith.FrontComposer.Shell.Tests/Services/Auth/CounterFakeAuthIntegrationTests.cs#L55)

**Representative semantic fixes**

- Exhaustive option mapping keeps JSON omission behavior explicit.
  [`QueryRequestTests.cs:296`](../../tests/Hexalith.FrontComposer.Contracts.Tests/Communication/QueryRequestTests.cs#L296)

- Unified disposal checks preserve one ObjectName across synchronous and queued paths.
  [`FaultInjectingProjectionHubConnection.cs:748`](../../tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/EventStore/FaultInjection/FaultInjectingProjectionHubConnection.cs#L748)

- Invariant diagnostic formatting demonstrates deterministic analyzer-test remediation.
  [`DriftClassifierTypeAndNullabilityTests.cs:45`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Comparison/DriftClassifierTypeAndNullabilityTests.cs#L45)
