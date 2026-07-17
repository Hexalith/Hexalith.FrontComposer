---
created: 2026-07-16
updated: 2026-07-17
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
dependsOn: 11.20
decision_baseline_commit: d9c19a4fb837357af10f6f1aa630232f670557c4
baseline_commit: 6861ca1bb3284f5cb5873daebdf2a7f3febed609
owner: Framework Maintainer + SourceTools Maintainer
due: 2026-08-14
status: ready-for-dev
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
approvalStatus: approved
approvedBy: Administrator
approvedOn: 2026-07-17
implementationEntryGate: story-11.20-done-and-approved-ledger-present
---

# Story 11.21: Recommended Analyzer Product and Generator Burn-down

Status: ready-for-dev.

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-07-17. -->
<!-- Administrator's direct create-story request records the separate Architecture/Product approval. -->
<!-- Approval does not waive the hard 11.20 completion and approved-ledger implementation entry gate. -->

## Story

As a Framework and SourceTools Maintainer,
I want product-source and generator-emission findings fixed by defect class,
so that every shipped package and generated consumer can build cleanly under the approved
`Recommended` policy.

## Acceptance Criteria

1. **The predecessor and approval gates fail closed.** Given Story 11.21 has separate
   Architecture/Product approval but depends on Story 11.20, when implementation starts, then Story
   11.20 is `done`, `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` exists and is
   approved, its analyzer-policy Governance gate passes, and the 11.21 census is rebased against that
   ledger. If any prerequisite is absent, no product, emitter, suppression, baseline, or policy edit
   begins.

2. **All shipped product findings are dispositioned.** Given Story 11.20's approved exception ledger,
   when the seven affected product projects build with command-line `AnalysisMode=Recommended` and
   unchanged warnings-as-errors, then all 367 baseline findings are reconciled to the implementation
   HEAD and are either fixed or covered by a pre-approved narrow compatibility exception. No new
   exception is invented in this story.

3. **Generated output is fixed at its source, including hidden ASP0006 debt.** Given 503 measured
   diagnostics occur in SourceTools output and ASP0006 is currently hidden by emitter pragmas and
   consumer `NoWarn` controls, when generator findings are remediated, then fixes are made in emitters
   or annotated source, never under `obj/`; runtime `RenderTreeBuilder` sequence counters are replaced
   by emitter-assigned literals; generator-owned ASP0006 controls are removed; and unsuppressed
   generated consumers prove both Recommended and ASP0006 cleanliness.

4. **Logging remediation is non-overlapping and support-safe.** Given CA1848 and CA1873 account for
   566 repository baseline findings, including 405 generated findings, when logging work is performed,
   then the 565 product/generated findings owned here use the repository's source-generated
   `LoggerMessage` pattern and the one hand-authored test finding stays with Story 11.22. The work
   consumes, without renumbering or remigrating, the completed Story 11.18 security, hot-path, and
   residual Warning+ event families. The exact 73-site intentional low-severity remainder is the
   direct-call migration denominator, while the refreshed ledger remains the full product logging
   diagnostic denominator; levels, templates, EventIds/EventNames, cardinality, enabled checks, and
   redaction remain stable.

5. **Every non-logging fix is bounded by diagnostic and package.** Given remaining product and
   generated findings span Design, Globalization, Maintainability, Performance, Reliability, and
   Usage, when changes are grouped, then every change maps to a named diagnostic/project/TFM and
   preserves public API, schema fingerprints, JSON/wire formats, command lifecycle, MCP fail-closed
   behavior, generated hint names and artifact inventory, routes, accessibility, and package
   compatibility. A fix requiring an unapproved breaking surface or broad suppression is escalated,
   not improvised.

6. **Compiler-host and dependency boundaries remain load-bearing.** Given Contracts and Schema are
   dual-targeted and SourceTools is a Roslyn component, when candidate validation runs, then their
   existing TFM/analyzer boundaries remain explicit, both netstandard2.0 legs pass, SourceTools still
   references only the Contracts kernel, no `ISymbol` escapes parse, and no net10/Blazor/Fluent
   dependency enters the compiler-host graph.

7. **Completion proves only the owned slice and hands off honestly.** Given the product/generator
   burn-down is complete, when validation runs, then owned product projects, the three non-test
   generated consumers, and a clean packaged generated consumer have zero actionable findings;
   Shell.Tests has zero findings whose location is SourceTools-generated output; normal Release is 0
   warnings/0 errors; required focused, default, Governance, Contract, package/API, schema, Pact,
   snapshot, docs, and artifact gates pass; intentional baseline changes are documented; and
   hand-authored test/sample debt remains explicitly owned by Story 11.22.

## Tasks / Subtasks

- [ ] Satisfy the implementation entry gate and rebase the owned census (AC: 1, 2, 7)
  - [ ] Verify Story 11.20 is `done`, its canonical JSON ledger exists with explicit approval, and
        `AnalyzerPolicyGovernanceTests` passes. Stop without source edits if any condition fails.
  - [ ] Verify that ledger already contains approved exact-symbol dispositions for product findings
        whose correction would break public API, including the three current CA1000 members in
        `QueryResult<T>` and the Testing builders. If any required disposition is absent, stop for a
        scoped Architecture/Product ledger amendment; Story 11.21 cannot manufacture an exception.
  - [ ] Record the implementation commit and exact SDK, MSBuild, Roslyn, UTC date, restore mode, TFM,
        generated-code treatment, and command for the refreshed census. The decision baseline is
        commit `d9c19a4...`, SDK `10.0.302`, MSBuild `18.6.4`, and Roslyn `5.6.0`; current local MSBuild
        has already drifted, so copied counts are not completion evidence.
  - [ ] Reconcile rather than overwrite the approved ledger. Assign every refreshed finding exactly
        once by project/TFM, diagnostic ID, source path or generated hint, source-vs-generated origin,
        owning story, and `fix|approved-exception|later-story` disposition.
  - [ ] Preserve the approved 89-CA1707 exact-file compatibility treatment for
        `FcDiagnosticIds.cs`; do not rename those public constants or count them as 11.21 source edits.
  - [ ] Escalate baseline growth above 5% in an owned scope, any unmatched finding/control, or any
        proposed exception not already approved by Story 11.20.

- [ ] Disposition all 367 shipped-product findings by project and defect class: fix the 278
      actionable findings and retain only the 89 pre-approved CA1707 exceptions (AC: 2, 4, 5)
  - [ ] Use the exact baseline matrix in Dev Notes; refresh it before editing, and keep a
        machine-reconcilable before/after count for every project and diagnostic.
  - [ ] Migrate the exact 73 low-severity direct Shell log calls across the 20-file remainder ledger
        to an internal eponymous source-generated helper or existing matching helper. Allocate a new
        collision-free EventId family; do not renumber Security `5660-5691`, HotPath `5700-5780`, or
        Warning `5800-5853` events.
  - [ ] Resolve product CA1873 sites by deferring expensive computation behind `IsEnabled` or a
        source-generated method. Preserve hashing, bounded identifiers, exception attachment,
        support-safety, and exactly-once behavior; do not broadly suppress the pinned-SDK rule.
  - [ ] Apply semantic fixes for non-logging diagnostics: explicit culture based on data meaning;
        correct throw helpers without changing exception contracts; idempotent disposal and
        unsubscribe/cancellation order; private/internal type narrowing only; cached immutable
        objects only where lifetime is safe; and equivalent overloads without wire/display drift.
  - [ ] Treat public CA1000/design findings and any signature-affecting CA1068/CA1859 proposal as
        compatibility decisions. Preserve public members unless Story 11.20 already contains an
        exact-symbol approved disposition; do not opportunistically update PublicAPI baselines.
  - [ ] Split `Testing/Builders.cs` only if it is touched, preserving the two public type names and
        namespaces while satisfying the repository's one-type-per-file rule.

- [ ] Fix the 503 measured SourceTools-generated findings in their three owning emitters (AC: 3-7)
  - [ ] `CommandFormEmitter` owns 307 findings: CA1507 12, CA1816 18, CA1822 5, CA1848 182, and
        CA1873 90. Add red tests before changing emitted forms; preserve validation, authorization,
        lifecycle dispatch, one-in-flight admission, row identity, disposal, and form rendering.
  - [ ] `CommandRendererEmitter` owns 171 findings: CA1816 5, CA1822 16, CA1848 97, CA1861 17, and
        CA1873 36. Preserve density modes, authorization retry timing, derived-value prefilling,
        destructive confirmation, return-path safety, and route behavior.
  - [ ] `RazorEmitter` owns 25 findings: CA1816 7, CA1822 3, CA1845 7, and CA1859 8. Preserve
        projection customization precedence, query/fallback behavior, accessibility markup,
        generated hint names, and artifact count.
  - [ ] Emit private source-generated logging methods inside the existing partial generated types,
        or use an existing accessible contract-neutral seam. Follow the repository signature rule:
        `ILogger` first, `Exception` second when present, PascalCase placeholders, deterministic
        EventId/EventName, and no new public runtime contract.
  - [ ] Preserve generated method, property, route, JSON, lifecycle, and HFC surfaces. Generated text
        and verified snapshots may change only where the analyzer fix requires it; review every
        accepted diff and keep hint paths/artifact inventory stable.

- [ ] Remove 11.21-owned ASP0006 debt with literal render-tree sequencing (AC: 3, 5, 7)
  - [ ] Inventory `seq++`/computed sequence emission in `CommandFormEmitter`,
        `CommandRendererEmitter`, `RazorEmitter`, `CommandPageEmitter`, and
        `ProjectionRoleBodyEmitter`; inventory all of `ColumnEmitter`, including its direct
        `colSeq++` emission and `SequenceExpression` helper, before adding any numbering abstraction.
        Reuse or extend the existing helper rather than creating parallel sequencing schemes.
  - [ ] Assign literals at generator execution time so generated `RenderTreeBuilder` call sites use
        stable source-location numbers. Reuse the same literal for a runtime loop call site; use
        explicit `OpenRegion`/`CloseRegion` only where a long generated block needs its own sequence
        scope. Do not substitute another runtime counter.
  - [ ] Remove the emitted ASP0006 disable/restore pragmas from command form and renderer output.
  - [ ] Negative-control every ASP0006 entry in Counter.Domain, Counter.Specimens.Domain,
        IdeParityCounter, Counter.Web, Counter.Specimens, Shell.Tests, Testing.Tests, and the packaged
        consumer template. Remove only controls whose violations came from 11.21-owned emission;
        retain and ledger any genuine 11.22 fixture exception rather than absorbing it.
  - [ ] Update the packaged generated-consumer test to set `AnalysisMode=Recommended`, preserve TWAE,
        and remove ASP0006 suppression. `CompilationHelper`/`GeneratorDriverTests` do not load the SDK
        analyzer set and are insufficient by themselves.

- [ ] Add focused regression and governance evidence (AC: 2-7)
  - [ ] Extend emitter syntax, determinism, snapshot, and behavior tests for every changed output;
        update only affected `.verified.txt` files after inspecting semantic diffs.
  - [ ] Add generated-consumer assertions for exact Recommended diagnostic zero and unsuppressed
        ASP0006 zero. Prove the 302 Shell.Tests generated findings disappear without claiming its
        hand-authored test source is Story 11.21-clean.
  - [ ] Update `SecurityLoggingGovernanceTests` from its exact 73-call remainder to zero, retaining
        non-vacuous synthetic negatives, EventId collision checks, placeholder/signature parity,
        support-safety, and disabled-path laziness.
  - [ ] Run focused tests for every changed product package, including public API behavior, schema
        truncation/fingerprint determinism, MCP admission/fail-closed behavior, lifecycle/disposal,
        logging event contracts/cardinality, and generated UI behavior as applicable.
  - [ ] Update the Story 11.20 ledger with fix evidence and final counts; do not create a second
        analyzer ledger or merge this policy with the unrelated package-compatibility suppression
        ledger.

- [ ] Run the scoped candidate, compatibility, and completion gates (AC: 5-7)
  - [ ] Run a normal forced Release `.slnx` build with canonical TWAE and require 0 warnings/0 errors.
  - [ ] Run strict command-line `AnalysisMode=Recommended` builds with TWAE unchanged for CLI,
        Contracts net10.0, Contracts.UI, MCP, Schema net10.0, Shell, and Testing; require zero
        actionable findings after approved exceptions.
  - [ ] Build Contracts and Schema explicitly for netstandard2.0 under their preserved analyzer
        boundary, and build/package SourceTools as netstandard2.0 with Contracts as its only runtime
        dependency.
  - [ ] Build Counter.Domain, Counter.Specimens.Domain, and IdeParityCounter under the candidate gate;
        use the strict clean packaged consumer as independent emitter proof. Run a full candidate
        census with TWAE relaxed only for enumeration and require zero diagnostics whose location is
        an owned product file or SourceTools-generated output.
  - [ ] Run each affected test project/assembly individually with `DiffEngine_Disabled=true`, then
        Governance and Contract lanes using repository trait conventions. Do not use solution-level
        `dotnet test`; `.slnx` is for restore/build.
  - [ ] Run package validation for every changed packable project, PublicAPI/schema/generated-output
        checks, Pact/contract-artifact validation, intentional Verify review, docs validation when a
        published contract changes, story-artifact validation, `git diff --check`, and mechanical
        changed-file/File-List reconciliation.
  - [ ] Confirm no central `AnalysisMode`, weaker TWAE, new analyzer package, broad CA/ASP suppression,
        hand-edited `obj`, unapproved public/schema/wire change, release-workflow edit, UX behavior
        change, or submodule edit entered the story.

## Dev Notes

### Approval, Sequencing, and Fail-Closed Entry Gate

- Administrator's direct Story 11.21 create-story request records this phase's separate
  Architecture/Product approval on 2026-07-17. It does not approve Stories 11.22-11.23.
- The dependency is not yet satisfied at context time: Story 11.20 is `ready-for-dev`, all of its
  implementation tasks are unchecked, and its required JSON ledger/Governance test are absent.
  `ready-for-dev` here means the implementation guide is complete; `dev-story` must halt before edits
  until Story 11.20 is done and the approved ledger is present and green.
- Preserve the sequence `11.19d decision -> 11.20 policy/ledger -> 11.21 product/generator -> 11.22
  tests/samples -> 11.23 central activation`. Do not add central `AnalysisMode`; Story 11.23 owns that
  v1.0 publication gate.
- This is not a UI redesign. Generated markup, DOM behavior, Fluent v5 usage, accessibility, routes,
  lifecycle timing, and visual output must remain invariant. Any intentional UX change is outside
  scope and requires separate approval/evidence.

### Baseline and Exact Owned Matrix

The signed 11.19d baseline is commit `d9c19a4...`, SDK `10.0.302`, MSBuild `18.6.4`, Roslyn `5.6.0`,
Release, 4,070 findings. A create-story census at `6861ca1b...` reproduced every non-Naming count and
reported 4,071 total only because one later underscore-named test increased CA1707 by one. Refresh
again after Story 11.20; toolchain and commit stamps are mandatory.

Product source baseline:

| Project/TFM | Count | Exact diagnostic distribution |
| --- | ---: | --- |
| Shell | 217 | CA1001 1; CA1305 25; CA1510 6; CA1513 5; CA1816 7; CA1834 5; CA1848 73; CA1859 8; CA1865 4; CA1873 83 |
| Contracts net10.0 | 119 | CA1000 2; CA1510 16; CA1707 89; CA1850 1; CA1859 3; CA1861 1; CA1865 2; CA1870 1; CA2249 3; CA2263 1 |
| MCP | 24 | CA1068 1; CA1305 11; CA1513 1; CA1859 6; CA1865 1; CA1873 4 |
| Schema net10.0 | 3 | CA1510 2; CA1845 1 |
| Contracts.UI | 2 | CA1861 2 |
| CLI | 1 | CA1865 1 |
| Testing | 1 | CA1000 1 |
| **Total** | **367** | Includes the 89 Story-11.20-owned CA1707 compatibility findings; 278 are otherwise actionable at the decision baseline. |

Generated SourceTools baseline:

| Emitter/output | Count | Exact diagnostic distribution |
| --- | ---: | --- |
| CommandFormEmitter | 307 | CA1507 12; CA1816 18; CA1822 5; CA1848 182; CA1873 90 |
| CommandRendererEmitter | 171 | CA1816 5; CA1822 16; CA1848 97; CA1861 17; CA1873 36 |
| RazorEmitter | 25 | CA1816 7; CA1822 3; CA1845 7; CA1859 8 |
| **Total** | **503** | CA1507 12; CA1816 30; CA1822 24; CA1845 7; CA1848 279; CA1859 8; CA1861 17; CA1873 126 |

Consumer distribution is Counter.Domain 79, Counter.Specimens.Domain 94, IdeParityCounter 28, and
Shell.Tests generated specimens 302. Story 11.21 owns the emitter corrections and consumer proof;
Story 11.22 owns remaining hand-authored test/sample findings.

### Current UPDATE Surfaces and Preservation Rules

Known direct generator UPDATE files:

- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs` currently emits the form,
  disposal, direct logs, runtime sequence counters, and an ASP0006 pragma. Change only the emitted
  mechanisms; preserve form parameters, validation, admission, authorization, lifecycle, and row
  identity.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs` currently emits renderer
  modes, authorization retry, direct logs, runtime sequence counters, and an ASP0006 pragma. Preserve
  density, route, confirmation, retry, and derived-value behavior.
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs` emits projection bodies and
  teardown. Preserve customization `L4 -> L2 -> default`, delegated Level-3 slots, query composition,
  fallback polling, accessibility, artifact inventory, and hint names.
- ASP0006 inspection also covers `CommandPageEmitter.cs`, `ProjectionRoleBodyEmitter.cs`, and the
  complete `ColumnEmitter.cs` surface, including direct `colSeq++` emission and the existing
  `SequenceExpression` helper. Do not invent a second sequence allocator.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
  already provides the best end-to-end packaged consumer seam, but currently suppresses ASP0006 and
  does not enable Recommended. Strengthen this test instead of building a parallel harness.

The refreshed ledger is authoritative for the full product UPDATE list. At the decision baseline it
contains 73 actionable source files after excluding preserved `FcDiagnosticIds.cs`. Before changing
each file, read it completely and record current behavior, the exact diagnostic fix, and preserved
contracts. High-risk surfaces include public generic static members, MCP parameter order, schema
surrogate-safe truncation, component/lifecycle disposal, private type narrowing, and the exact 73-call
Shell logging remainder pinned by `SecurityLoggingGovernanceTests`.

### Architecture and Anti-Disaster Guardrails

- Built-in SDK analyzers only. Do not add Sonar, StyleCop, Roslynator, or another analyzer package;
  do not add package versions to project files.
- Keep root `TreatWarningsAsErrors=true`. No root/category CA disable, blanket `NoWarn`, wildcard
  production scope, generated-code exclusion, new pragma, or central `AnalysisMode` is allowed.
- Do not hide the 503 findings by changing generated headers/suffixes or generated-code classification.
  The measured candidate output is the authority.
- No `obj/**` edit. Generator fixes belong in SourceTools emitters or annotated source. Generated
  output path `obj/{Config}/{TFM}/generated/HexalithFrontComposer/` remains a public contract.
- Keep SourceTools pure incremental IR: no `ISymbol` after parse, `EquatableArray<T>` for collections,
  full equality/hash participation, and no new `CompilationProvider` dependency.
- Do not change `CanonicalSchemaMaterial`, encoders, sentinel, ordinal comparison, source-gen context,
  fingerprints, wire JSON, MCP opaque failure shapes, server-controlled fields, or mandatory security
  gates.
- Keep one C# type per file. Use file-scoped namespaces, Allman braces, nullable validation, CRLF,
  `ConfigureAwait(false)` where required by repository policy, and XML docs on public/internal API
  surfaces.
- Preserve Story 11.18 event identity and support safety. The 73 remaining low-severity calls are an
  intentional handoff, not permission to remigrate or renumber earlier events.
- Do not touch release workflows or `references/**`; the currently dirty EventStore gitlink and MCP
  test files are unrelated user work and must remain untouched.

### Testing and Validation Requirements

- `CompilationHelper` and ordinary `GeneratorDriverTests` prove syntax/compiler correctness but do
  not load the SDK built-in analyzer set. They cannot be the only Recommended evidence.
- Red-green tests must cover emitter strings/IR, parseability, deterministic output, generated
  behavior, strict packaged-consumer analysis, and actual sample/test generated trees. Inspect every
  Verify diff; do not mass-accept snapshots.
- The strict product candidate is per project because Story 11.22 still owns test/sample source debt.
  A full Recommended build with TWAE relaxed is census instrumentation only; it is not a green gate.
- Run test projects individually. For xUnit v3 focused lanes, build the project and invoke the built
  assembly with single-dash `-class`/`-method` filters. Set `DiffEngine_Disabled=true` for every test
  invocation.
- Package/API/schema/Pact/Verify/docs lanes are conditional on touched surfaces but must be run when
  triggered. PublicAPI baselines should remain unchanged unless a separately approved compatibility
  decision says otherwise.

Required command spine (record every exit code and resulting diagnostic/test count):

```bash
dotnet restore Hexalith.FrontComposer.slnx \
  -p:Configuration=Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

product_projects=(
  src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj
  src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj
  src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj
  src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj
  src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj
  src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj
  src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj
)
for product_project in "${product_projects[@]}"; do
  dotnet build "$product_project" -c Release -f net10.0 --no-restore --no-incremental -m:1 \
    -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended
done

for compatibility_project in \
  src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj \
  src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj; do
  dotnet build "$compatibility_project" -c Release -f netstandard2.0 \
    --no-restore --no-incremental -m:1 \
    -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
done

dotnet build src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj \
  -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

generated_consumers=(
  samples/Counter/Counter.Domain/Counter.Domain.csproj
  samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj
  samples/IdeParityCounter/IdeParityCounter.csproj
)
for generated_consumer in "${generated_consumers[@]}"; do
  dotnet build "$generated_consumer" -c Release -f net10.0 --no-restore \
    --no-incremental -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
    -p:AnalysisMode=Recommended
done

dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj \
  -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

DiffEngine_Disabled=true \
  tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests \
  -noLogo -noColor -parallel none \
  -class Hexalith.FrontComposer.SourceTools.Tests.Integration.PackagedAnalyzerConsumerTests

dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
  -p:AnalysisMode=Recommended -p:TreatWarningsAsErrors=false \
  -bl:/tmp/story-11-21-shell-tests-census.binlog

DiffEngine_Disabled=true \
  tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -noLogo -noColor -parallel none \
  -class Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests \
  -class Hexalith.FrontComposer.Shell.Tests.Architecture.SecurityLoggingGovernanceTests

DiffEngine_Disabled=true \
  tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests \
  -noLogo -noColor -parallel none -trait Category=Contract

pwsh ./eng/validate-contract-artifacts.ps1

python3 eng/validate-story-artifacts.py --story \
  _bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md
```

The Shell.Tests command is census instrumentation, so its nonzero hand-authored Story 11.22 findings
do not fail this story. Reuse Story 11.20's ledger extractor against the binary log and require zero
diagnostics located in SourceTools-generated output. Run the same direct xUnit-v3 executable pattern
for every affected test project and append the complete default, Governance, and Contract evidence to
`_bmad-output/implementation-artifacts/tests/test-summary.md`; append paired
`--unrelated PATH --reason TEXT` arguments to artifact
validation for every pre-existing dirty path.

### Previous Story and Git Intelligence

- Story 11.20 is the immediate predecessor and sole policy/exception authority. Its current guide
  defines the canonical JSON ledger, exact-scope Governance, approved CA1707 policy, and warning-control
  ownership, but none of that implementation exists yet. Consume its delivered artifacts; do not copy
  its tasks into this story.
- Story 11.19d established the four-phase target and raw binlogs without changing policy. Its counts
  are the decision baseline, not permission to skip an implementation-HEAD refresh.
- Stories 11.18a-c established exclusive source-generated logging patterns, event families, redaction,
  and the exact 73-call low-severity remainder. Extend that pattern without reopening their scopes.
- The latest five commits contain submodule-pointer, dependency, and MCP/benchmark test-governance
  work; no shipped FrontComposer product or emitter change. Commit `335061df...` nevertheless proved
  that census counts drift with ordinary repository evolution, so every result stays commit-stamped.
- Existing dirty changes to Story 11.17, Story 11.20, `deferred-work.md`, the pre-existing 11.20 sprint
  transition, the EventStore gitlink, MCP split tests/helpers, and concurrent CI/release/docs tooling
  belong to other work. Preserve them and reconcile Story 11.21's File List from its own diff only.

### Current Official Technical Guidance

- `Recommended` enables a toolchain-dependent SDK rule set. Keep SDK `10.0.302`/Roslyn `5.6.0`
  unchanged in this story and stamp the effective toolchain on every census.
- Microsoft ASP0006 guidance says not to suppress non-literal `RenderTreeBuilder` sequence numbers;
  literals identify source locations, not execution order. Long manual blocks may use regions.
- CA1848 directs callers to `LoggerMessageAttribute` and says not to suppress it. CA1873 requires
  deferred expensive arguments through `IsEnabled` or source-generated logging. Validate against the
  pinned SDK if the known .NET 10 guarded-call false-positive is encountered; route an exact site to
  the ledger rather than adding a broad suppression.
- Compile-time logging requires partial methods and partial containing types. Static methods take an
  `ILogger`; repository convention places it first and an `Exception` second.
- Do not change generated-code classification to evade findings. Roslyn analyzers decide whether to
  analyze/report generated trees, and the measured 503 diagnostics are the accepted behavior.

### Project Structure Notes

Expected story-owned update families include:

- the three direct SourceTools emitters and ASP0006 helper emitters listed above;
- focused SourceTools emitter/integration tests and only affected Verify baselines;
- the strengthened packaged consumer test;
- the refreshed Story 11.20 ledger and AnalyzerPolicy Governance evidence;
- the exact Shell low-severity logging remainder and its Governance/event tests;
- product files named by the refreshed ledger across CLI, Contracts, Contracts.UI, MCP, Schema,
  Shell, and Testing;
- `_bmad-output/implementation-artifacts/tests/test-summary.md` for implementation evidence.

Read/govern but do not edit unless a separately approved exact need is proven:

- `Directory.Build.props`, central package versions, `.slnx` structure, and central analyzer policy;
- `FcDiagnosticIds.cs` names/values and owned PublicAPI baselines;
- `CanonicalSchemaMaterial`, schema fingerprints, pact/CLI JSON contracts, and generated artifact
  inventory/hint paths;
- release workflows and all `references/**` submodules.

## References

- [Source: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md]
- [Source: _bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md]
- [Source: _bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md]
- [Source: _bmad-output/implementation-artifacts/11-18-hot-path-log-sites.md]
- [Source: _bmad-output/implementation-artifacts/11-18-warning-and-above-log-sites.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.21-Recommended-analyzer-product-and-generator-burn-down]
- [Source: _bmad-output/planning-artifacts/prd.md#FR-25]
- [Source: _bmad-output/planning-artifacts/prd.md#FR-29]
- [Source: _bmad-output/planning-artifacts/architecture.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-16-post-correction.md]
- [Source: _bmad-output/project-context.md]
- [Microsoft: MSBuild AnalysisMode properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#analysismode)
- [Microsoft: Analyzer configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#analysis-mode)
- [Microsoft: ASP0006](https://learn.microsoft.com/en-us/aspnet/core/diagnostics/asp0006?view=aspnetcore-10.0)
- [Microsoft: Blazor render-tree sequence guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/advanced-scenarios?view=aspnetcore-10.0#sequence-numbers-relate-to-code-line-numbers-and-not-execution-order)
- [Microsoft: CA1848](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1848)
- [Microsoft: CA1873](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1873)
- [Microsoft: Compile-time logging generation](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/source-generation)
- [Microsoft: Generated-code analysis configuration](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#exclude-generated-code)
- [dotnet/roslyn-analyzers: .NET 10 guarded-call CA1873 issue](https://github.com/dotnet/roslyn-analyzers/issues/7690)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-17: Create-story analysis loaded the repository instructions, BMAD workflow/config/template/
  checklist, project context, full sprint tracker, PRD/Epic/architecture/UX inputs, Stories 11.18-11.20,
  analyzer decision/census evidence, current emitters/product files, last five commits, and current
  official Microsoft analyzer/Blazor/logging guidance.
- 2026-07-17: A forced no-incremental Release census at context HEAD with command-line
  `AnalysisMode=Recommended` and TWAE relaxed only for enumeration reproduced 4,071 findings: the
  decision's 1,112 non-Naming findings were unchanged; Naming increased by the one already-reconciled
  underscore test.
- 2026-07-17: Source-to-diagnostic reconciliation identified the exact seven-product and three-emitter
  matrices, the 73-site low-severity logging handoff, the generated consumer distribution, and hidden
  ASP0006 controls not represented by the 4,070 decision census.
- 2026-07-17: Story 11.20's ledger and Governance test are absent. Story 11.21 is context-ready but its
  implementation entry gate remains fail-closed until 11.20 is done.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Administrator's direct request records this phase's separate approval without waiving Story 11.20.
- Added exact product and generated diagnostic matrices, 11.18 logging handoff, hidden ASP0006 scope,
  current UPDATE surfaces, semantic preservation rules, and a scoped validation strategy that does
  not falsely claim Story 11.22's test/sample debt.
- Story status is ready-for-dev for context tracking; implementation is explicitly blocked until the
  Story 11.20 approved ledger exists and passes Governance.

### File List

- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 2 from Story 11.19d as a separately gated
  backlog specification.
- 2026-07-17: Administrator supplied separate phase approval; create-story enriched the complete
  product/generator implementation guide, retained the hard 11.20 dependency gate, and promoted the
  story context from backlog to ready-for-dev.
