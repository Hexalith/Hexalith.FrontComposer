---
created: 2026-07-16
updated: 2026-08-07
epic: 11
sourceDecision: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md
parentDecisionStory: 11.19d
decision_baseline_commit: d9c19a4fb837357af10f6f1aa630232f670557c4
baseline_commit: 6861ca1bb3284f5cb5873daebdf2a7f3febed609
baseline_revision: 03a0c59192eb357187faa8181d12a2ead314c74e
owner: Architect + Framework Maintainer
due: 2026-07-24
# The original due date lapsed while the story sat blocked on two package conditions that were later
# resolved by unrelated work. Implementation completed and was re-verified on 2026-08-07; the date is
# kept as the historical commitment rather than back-dated.
status: review
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
approvalStatus: approved
approvedBy: Administrator
approvedOn: 2026-07-17
---

# Story 11.20: Recommended Analyzer Policy and Exception Ledger

Status: review.

<!-- Validation completed against .agents/skills/bmad-create-story/checklist.md on 2026-07-17. -->
<!-- Administrator explicitly satisfied the separate Architecture/Product approval gate on 2026-07-17. -->

## Story

As an Architect and Framework Maintainer,
I want every current analyzer suppression and Naming diagnostic classified into a narrow exception or
an actionable fix,
so that `AnalysisMode=Recommended` can be adopted without breaking public compatibility or hiding
findings globally.

## Acceptance Criteria

1. Given the Story 11.19d census, when the policy audit runs, then all 2,958 Naming findings and every
   effective `NoWarn`, warning-as-error, and individual `.editorconfig` severity are represented in a
   versioned ledger with scope, rationale, owner, review date, and removal or revalidation trigger.

2. Given CA1707 conflicts with the repository's required underscore-separated test names and existing
   public `FcDiagnosticIds` constant names, when dispositions are recorded, then test naming and public
   compatibility are preserved through the narrowest supported per-path, per-symbol, or source-level
   mechanism; repository-wide or category-wide CA suppression is forbidden.

3. Given a Naming finding is not covered by an approved compatibility or test-convention exception,
   when the scoped candidate lane runs, then the finding is fixed or placed in a separately approved,
   owner-bound defect story; no open item is hidden by the ledger.

4. Given current compiler, SDK, package, FrontComposer, IDE, and CA suppressions have different owners,
   when the audit completes, then each is classified by source and the decision explicitly states
   whether it remains, narrows, moves, or becomes a fix. Story 11.19a documentation policy and package
   audit policy remain independent.

5. Given the no-third-party-analyzer policy, when Governance checks run, then they prove no analyzer
   package was added, no CA category was globally disabled, `TreatWarningsAsErrors=true` remains the
   canonical policy, and the ledger matches effective build configuration.

6. Given this phase changes policy boundaries rather than product behavior, when validation completes,
   then the normal forced Release build remains 0 warnings/0 errors, focused analyzer-policy tests pass,
   the default solution lane passes, and public API/schema/generated-output baselines change only when
   explicitly approved by this story.

## Tasks / Subtasks

- [x] Reconcile the approved baseline with the implementation HEAD before changing policy (AC: 1, 3, 6)
  - [x] Preserve the 11.19d baseline coordinates: commit
        `d9c19a4fb837357af10f6f1aa630232f670557c4`, SDK `10.0.302`, MSBuild `18.6.4`, Roslyn
        `5.6.0`, 4,070 Recommended findings, and 2,958 Naming findings.
  - [x] Refresh the census at the actual implementation commit with the same Release, restore, TFM,
        analyzer, and generated-code conditions; stamp the ledger with commit, SDK, MSBuild, Roslyn,
        UTC date, and exact command.
  - [x] Reconcile rather than overwrite drift. At context HEAD
        `6861ca1bb3284f5cb5873daebdf2a7f3febed609`, a no-incremental Naming-only census produced
        2,959 findings: CA1707 2,957 and CA1711 2. The additional CA1707 is the test method added by
        `335061df552997b53e97ef20dedbc5e37eff5a6e` at
        `SourceToolsTypeOrganizationGovernanceTests.cs:67`.
  - [x] Treat a full Recommended build with `TreatWarningsAsErrors=false` as census instrumentation
        only. Do not use it as Story 11.20's green gate: the 1,112 non-Naming findings are owned by
        Stories 11.21 and 11.22. (2026-08-07 reconciliation: 1,112 is the *pre-policy* count at
        create-story HEAD `6861ca1b`, i.e. 4,071 total minus 2,959 Naming. It is not comparable to the
        post-policy counts; see "Non-Naming count reconciliation" in Dev Notes. The number that gates
        this story is Naming, which is 0.)

- [x] Create the canonical machine-readable exception/fix ledger (AC: 1, 3, 4, 5)
  - [x] Add `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`; do not create a second
        hand-maintained Markdown ledger and do not reuse the unrelated
        `docs/diagnostics/compatibility-suppressions.json` package-compatibility ledger.
  - [x] Give the document a fail-closed schema/version and top-level decision, baseline, refreshed
        census, toolchain, ownership, approval, and count metadata.
  - [x] Make every Naming finding traceable to exactly one disposition. Each finding/location record
        must carry diagnostic ID, project/TFM, repo-relative path, line or symbol, generated-source
        status, and a disposition key; exact-scope groups are allowed only when their location list and
        count are deterministic and governance-checked.
  - [x] Give every exception/control disposition a stable key, source kind, exact scope, mechanism,
        `remain|narrow|move|fix` decision, rationale, owner, decision date, review date, removal or
        revalidation trigger, evidence, and optional separately approved follow-up story.
  - [x] Reject duplicates, unmatched findings, unmatched controls, empty owners/rationales, past review
        dates, absolute/machine-specific paths, wildcard production exceptions, and exception counts
        that do not reconcile to both the approved baseline and refreshed census.

- [x] Inventory and classify every effective warning control (AC: 1, 4, 5)
  - [x] Scan root-owned tracked files only, excluding `references/**`, `.git/**`, `bin/**`, `obj/**`,
        and `node_modules/**`. Inventory MSBuild `NoWarn`, `WarningsAsErrors`,
        `WarningsNotAsErrors`, `TreatWarningsAsErrors`, and `AnalysisMode*`; individual and bulk
        analyzer severities; `#pragma warning`; `SuppressMessage`/`UnconditionalSuppressMessage`;
        and emitter-authored pragma text.
  - [x] Evaluate imported MSBuild properties per project/TFM instead of trusting XML text alone.
        Record SDK/package-inherited entries such as 1701/1702 and CA2255 as inherited provenance;
        do not copy them into repository `NoWarn`.
  - [x] Preserve the Story 11.19a-owned source `NoWarn` values
        `0419;1570;1572;1573;1574;1734`, the CS1591 EditorConfig boundary, and the explicit CA1014
        policy. Preserve NU5104/NU5128 and other package controls under their package-policy owners.
  - [x] Preserve root `TreatWarningsAsErrors=true`. Record the benchmark executable as the only
        approved root-owned `TreatWarningsAsErrors=false` exception.
  - [x] Move ASP0006 emitter/product debt to Story 11.21 and remaining test/sample fixture debt to
        Story 11.22 with named owners. Move Testing.Tests CA2007 audit to Story 11.22 rather than
        mass-editing asynchronous test semantics in this policy phase.
  - [x] Narrow HFC1002 now: remove project-wide HFC1002 from Counter.Domain and
        Counter.Specimens.Domain, retaining source-local suppressions only around `Metadata`,
        `Approvers`, and `OpaquePayload`; remove the stale IdeParityCounter HFC1002 entry after a
        forced negative-control build proves it is unused.
  - [x] Audit and remove the stale Shell.Tests `IDE1006`, `CS1656`, and `IDE0058` project-wide
        entries if the forced build remains clean; keep ASP0006 until its separately owned burn-down.

- [x] Apply the approved Naming dispositions without weakening repository policy (AC: 2, 3, 5)
  - [x] Add only two CA1707 EditorConfig scopes, using the repository's direct-and-recursive `**.cs`
        convention: `[tests/**.cs]` for the required three-part underscore test naming convention and
        `[src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs]` for the shipped public
        diagnostic-constant names.
  - [x] Preserve all current `.editorconfig` naming/style rules and Story 11.19a CS1591 sections. Do
        not add CA1707 to `[*.cs]`, root `NoWarn`, or a Naming/CA category severity.
  - [x] Prove the test scope matches only tracked FrontComposer test sources and that the exact-file
        scope matches only `FcDiagnosticIds.cs`. The exception count must match the refreshed ledger,
        so a newly hidden finding cannot pass by merely landing under the path glob.
  - [x] Do not rename any `FcDiagnosticIds` field. Story 11.19c established these reflection-tested
        public identifiers as compatibility surface; this story changes analyzer configuration, not
        their API names or values.
  - [x] Fix both CA1711 findings rather than suppressing them. Move the xUnit collection-definition
        types into one-type-per-file `DiagnosticRegistryTestGroup.cs` and
        `DriftCultureTestGroup.cs`; preserve collection names `"DiagnosticRegistry"` and
        `"DriftCulture"`, `DisableParallelization=true`, and all existing test membership/behavior.
  - [x] If the refreshed census finds any other Naming diagnostic, fix it in scope or create a
        separately approved, owner-bound defect story before marking this task complete.

- [x] Add non-vacuous analyzer-policy Governance coverage (AC: 1-5)
  - [x] Add
        `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`; keep
        one C# type per file and use `JsonNode` inside the test if that avoids unnecessary ledger model
        types. Mark the class `[Trait("Category", "Governance")]` so it runs in the blocking
        Governance lane. Do not expand the already-large `CiGovernanceTests.cs`.
  - [x] Validate ledger schema, required lifecycle fields, unique keys, exact counts, path safety,
        owner/review dates, follow-up references, and bidirectional finding/disposition/control parity.
  - [x] Compare the ledger with root-authored MSBuild, EditorConfig, pragma, suppression-attribute,
        and emitter controls. Add synthetic negative cases for an unledgered control, stale ledger row,
        root/category CA disable, root `NoWarn` CA entry, wildcard production scope, missing owner,
        expired review date, and count drift.
  - [x] Evaluate representative imported MSBuild graphs and both supported Contracts TFMs. Text search
        alone is insufficient; Story 11.19a proved imported/effective policy and recursive scope with
        compile specimens.
  - [x] Pin `TreatWarningsAsErrors=true`, absence of central `AnalysisMode` before Story 11.23, the
        single benchmark TWAE exception, built-in analyzers only, and absence of new analyzer package
        references. Do not misclassify internal SourceTools analyzer project references or ordinary
        packages that expose analyzer assets as newly added third-party analyzer packages.
  - [x] Add positive and negative compile specimens proving the test CA1707 path and exact
        `FcDiagnosticIds.cs` path suppress only CA1707, while CA1711 and an out-of-scope production
        CA1707 still fail under the Naming candidate lane.

- [x] Verify all policy, build, test, and artifact gates (AC: 3, 5, 6)
  - [x] Run a normal forced Release build with canonical TWAE and require 0 warnings/0 errors.
  - [x] Run the strict scoped candidate with `AnalysisModeNaming=Recommended` and unchanged TWAE;
        require 0 warnings/0 errors. Do not substitute global `AnalysisMode=Recommended` for this gate.
  - [x] Run the full Recommended census with TWAE disabled only to prove zero unapproved Naming
        findings and report the remaining later-story categories without claiming them complete.
  - [x] Run focused Shell Governance and SourceTools collection/culture tests, then run the complete
        FrontComposer default test-project matrix one project/assembly at a time with
        `DiffEngine_Disabled=true` and the standard trait exclusions.
  - [x] Run story-artifact validation and reconcile the implementation File List against only this
        story's Git diff. Pass any pre-existing dirty-worktree paths explicitly as unrelated with a
        reason; do not absorb them into this story.
  - [x] Confirm no PublicAPI, schema, generated-output, Verify snapshot, pact, package, solution, or
        submodule baseline changed; if a baseline change is genuinely required, stop for separate
        approval rather than regenerating it opportunistically.

## Dev Notes

### Approval and sequencing

- Administrator explicitly supplied the separate Architecture/Product approval on 2026-07-17. That
  approval promotes only Story 11.20; Stories 11.21-11.23 remain separately approval-gated backlog
  phases.
- The required sequence remains 11.19d decision -> 11.20 policy/ledger -> 11.21 product/generator ->
  11.22 tests/samples -> 11.23 repository activation. Story 11.20 must not centrally enable
  `AnalysisMode=Recommended`; Story 11.23 owns that v1.0 gate.
- This is a policy/configuration/test story with no runtime or UI behavior. UX, IA, routes,
  accessibility, timing, localization, and design-system contracts are unaffected.

### Brownfield census and current drift

- The approved 11.19d census at `d9c19a4...` contains 4,070 findings: Naming 2,958, Performance 772,
  Globalization 228, Usage 49, Maintainability 46, Reliability 12, and Design 5. Naming is CA1707
  2,956 plus CA1711 2.
- The CA1707 baseline decomposes into 2,867 test findings plus 89 public `FcDiagnosticIds` constants.
  Major test surfaces are Shell.Tests 1,660; SourceTools.Tests 665; Mcp.Tests 281; Contracts.Tests
  109; Cli.Tests 64; Testing.Tests 57; Bench 21; and Contracts.UI.Tests 10.
- At create-story context HEAD `6861ca1b...`, the Naming-only build produced 2,959 findings: CA1707
  2,957 and CA1711 2. The one-item drift is intentional repository evolution, not census corruption:
  commit `335061df...` added
  `OrganizationGuard_SyntheticNameMismatchSource_ReportsFileAndDeclaration`. The ledger therefore
  needs immutable baseline metadata plus a refreshed implementation snapshot.
- A full Recommended instrumentation build at that HEAD produced 4,071 warnings and 0 errors: Naming
  2,959 with every other category unchanged from the approved census. This is the refreshed pre-policy
  reference, not the post-policy completion target.
### Non-Naming count reconciliation

Three different non-Naming totals appear across this story's artifacts. They are all correct; each is
stamped at a different commit and a different side of the policy change. They must not be normalised
into a single number.

| Count | Commit | When | Meaning |
| --- | --- | --- | --- |
| 1,112 | `6861ca1b` (create-story HEAD) | 2026-07-17, pre-policy | 4,071 total minus 2,959 Naming |
| 1,123 | implementation working tree over `03a0c591`, landed as `67154049` | 2026-07-17, post-policy | ledger `postPolicyCensus.laterStoryFindings` |
| 1,122 | `05481771` | 2026-08-07, post-policy | completion-pass re-verification |

The 1,112 -> 1,123 step is the pre-policy/post-policy boundary plus ordinary repository evolution
between the two commits; the 1,123 -> 1,122 step is one finding removed by unrelated work landing
after the implementation commit. Every one of these findings belongs to Stories 11.21 and 11.22. None
of them gates Story 11.20, whose completion criterion is Naming = 0 - satisfied at every post-policy
measurement. The ledger's `postPolicyCensus` block keeps its original stamped 1,123 as a historical
record and carries a `reconciliation` note rather than being rewritten.

- The exact two CA1711 violations are the xUnit definition types `DiagnosticRegistryCollection` and
  `DriftCultureCollection`. They are test infrastructure, not shipped API, and should become
  `DiagnosticRegistryTestGroup` and `DriftCultureTestGroup`; keep the xUnit collection strings stable.

### Current configuration: change and preserve

- `.editorconfig` currently defines repository naming/style rules, CA1062/CA1822/CA2007 warning,
  CA1014 none, CS1591 none by default, and four recursive Contracts CS1591 warning scopes. Add the two
  exact CA1707 scopes only; preserve every existing rule and use `**.cs`, matching the proven Story
  11.19a recursive-glob convention.
- `Directory.Build.props` centrally owns C# defaults and `TreatWarningsAsErrors=true`. Read and govern
  it, but do not edit it or add `AnalysisMode`; central activation belongs to Story 11.23.
- `src/Directory.Build.props` owns documentation/compiler `NoWarn` entries. Read/evaluate it, but do
  not alter the Story 11.19a boundary in this story.
- `FcDiagnosticIds.cs` contains shipped public constant names and values. The exact-file EditorConfig
  exception changes analyzer treatment without source/API edits; reflection/compatibility behavior
  must remain byte-for-byte semantically equivalent.
- `DiagnosticRegistryTests.cs` is a 2,762-line governance suite. Remove only the co-located collection
  definition and point its `[Collection]` attribute at the new test-group constant; preserve the suite.
- `DriftCultureCollection.cs` and `DriftCultureInvarianceTests.cs` serialize process-wide culture
  mutation. Rename the definition file/type and update the reference without changing the collection
  string or isolation behavior.
- Counter.Domain currently suppresses HFC1002 for its whole project although only `Metadata` needs the
  fixture exception. Counter.Specimens.Domain likewise needs only `Approvers` and `OpaquePayload`.
  IdeParityCounter's HFC1002 is stale. Narrow/remove these without changing the sample's teaching
  behavior or generated output.
- Shell.Tests currently carries project-wide ASP0006, IDE1006, CS1656, and IDE0058. Forced-build
  evidence indicates the latter three are stale; remove them if the implementation-head negative
  control confirms this, while leaving ASP0006 to its owned burn-down.

### Ledger contract

- Canonical path: `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`.
- Keep findings and policy controls distinct but connected. A useful shape is top-level
  `schemaVersion`, `contractId`, `decision`, `baseline`, `refreshedCensus`, `toolchain`, `findings`,
  `dispositions`, and `warningControls`; the governance test, not prose, is the schema authority.
- The ledger records exceptions and actionable fixes; it does not itself suppress anything. Every
  effective suppression must have a matching configured mechanism, and every configured mechanism
  must have a ledger row. A `fix` row must point to the exact changed location or separately approved
  story, never merely disappear from the census.
- `docs/diagnostics/compatibility-suppressions.json` is schema `2.0` evidence for CP0001 package/API
  compatibility. Do not merge analyzer-policy rows into it or couple its release lifecycle to this
  ledger.

### Architecture and implementation boundaries

- Target .NET 10/C# 14 with SDK `10.0.302`; keep Roslyn `5.6.0` and all centrally pinned packages.
  `Recommended` contents are SDK-version-dependent, so every census must identify its toolchain.
- Built-in SDK analyzers only. Do not add Sonar, StyleCop, Roslynator, or any analyzer package. Keep
  SourceTools on `netstandard2.0` and do not change package/project dependency direction.
- No repository/category-wide CA severity, no CA ID in root `NoWarn`, no TWAE weakening, and no edits
  to generated `obj/**` files. Generated fixes belong in emitters/annotated source under later stories.
- Do not edit `references/**` submodules. `references/Hexalith.Builds/Hexalith.globalconfig` is external
  submodule configuration and is not the root FrontComposer analyzer policy.
- Preserve one C# type per file. The two CA1711 fixes are also an opportunity to remove the existing
  co-located collection definition from `DiagnosticRegistryTests.cs`, not to add more helper types.

### Project Structure Notes

- Expected implementation touch points:
  - `.editorconfig`
  - `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` (new)
  - `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` (new)
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTestGroup.cs` (new)
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureCollection.cs`
    (remove after the type/file rename)
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureTestGroup.cs` (new)
  - `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureInvarianceTests.cs`
  - `samples/Counter/Counter.Domain/Counter.Domain.csproj`
  - `samples/Counter/Counter.Domain/CounterProjection.cs`
  - `samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj`
  - `samples/Counter/Counter.Specimens.Domain/SpecimenFormattingProjection.cs`
  - `samples/IdeParityCounter/IdeParityCounter.csproj`
  - `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
  - `_bmad-output/implementation-artifacts/tests/test-summary.md` for dev-story evidence
- Read/govern but avoid changing unless new evidence proves a scoped need:
  - `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`
  - `src/Directory.Build.props`
  - `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs`
  - `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj`
  - source/emitter pragma and suppression sites inventoried by the ledger
- Do not touch PublicAPI baselines, schema/pact/Verify/generated-output baselines, `.sln`/`.slnx`
  structure, package versions, runtime/UI code, or `references/**`.

### Validation Lanes

```bash
dotnet restore Hexalith.FrontComposer.slnx \
  -p:Configuration=Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

# Story 11.20 completion gate: Naming only, canonical TWAE remains true.
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
  -p:AnalysisModeNaming=Recommended

# Evidence-only full census: later categories are expected; TWAE is relaxed only for enumeration.
dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 \
  -p:AnalysisMode=Recommended -p:TreatWarningsAsErrors=false

dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj \
  -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0
dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj \
  -c Release --no-restore --no-incremental -m:1 \
  -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0

DiffEngine_Disabled=true dotnet \
  tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll \
  -class "Hexalith.FrontComposer.Shell.Tests.Governance.AnalyzerPolicyGovernanceTests"
DiffEngine_Disabled=true dotnet \
  tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Release/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll \
  -class "Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests" \
  -class "Hexalith.FrontComposer.SourceTools.Tests.Drift.Regression.DriftCultureInvarianceTests"

python3 eng/validate-story-artifacts.py \
  --story _bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md
```

- The mandatory root `AGENTS.md` chain requires test projects to run individually and reserves the
  `.slnx` for restore/build. This higher-authority instruction overrides the stale opposite statement
  in `_bmad-output/project-context.md`; do not edit that broader context artifact in this story.
- Under xUnit v3's in-process runner, invoke each built test assembly directly with single-dash
  `-class`/`-method` filters for focused runs. For the full default matrix, execute every root-owned
  test project/assembly individually and exclude `Performance`, `e2e-palette`, `NightlyProperty`, and
  `Quarantined` via runner `-trait- "Category=..."` arguments; do not use project or solution
  `dotnet test --filter`.
- `DiffEngine_Disabled=true` is mandatory for every test invocation.
- The strict Naming candidate must pass with 0 warnings/0 errors after the two approved CA1707 scopes
  and two CA1711 fixes. The full Recommended census is allowed to report only later-story categories;
  any remaining Naming finding fails this story.

### Previous Story and Git Intelligence

- Story 11.19a (`d17cacd2...`) removed blanket CS1591 suppression and proved both evaluated MSBuild
  state and recursive EditorConfig scopes with compile specimens. Reuse that fail-closed pattern.
- Story 11.19b (`84273bac...`) removed blanket NuGet-audit suppressions and verified the imported graph.
  Keep analyzer policy separate from package-audit policy.
- Story 11.19c / baseline `d9c19a4...` preserved public diagnostic compatibility through explicit
  aliases and reflection tests. Do not rename `FcDiagnosticIds` constants.
- Story 11.19d / materialization commit `eb0c1b5...` approved staged Recommended adoption but did not
  activate policy. It established the 4,070/2,958 baseline and the four-phase sequence.
- Commit `335061df...` added one underscore-named governance test after the census, proving why a
  commit-stamped refreshed snapshot and count-drift governance are mandatory.

### Current Platform Guidance

- Microsoft documents category-specific `AnalysisMode<Category>` properties, including
  `AnalysisModeNaming`; use this scoped property for the strict Story 11.20 gate.
- Specific rule configuration and file/path scopes belong in EditorConfig; command-line/MSBuild
  warning settings have their own precedence. Governance must test effective behavior, not assume
  textual order.
- CA1707 documentation explicitly allows suppression for test code. Production suppression is not
  generally recommended, which is why the only production exception is the exact shipped
  `FcDiagnosticIds.cs` compatibility file.
- CA1711 is a genuine naming issue here. The xUnit collection-definition type names can change while
  their collection string contract remains stable, so suppression is not justified.

## References

- [Source: _bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md]
- [Source: _bmad-output/contracts/analyzer-elevation-recommended-census-2026-07-16.binlog.gz]
- [Source: _bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md]
- [Source: _bmad-output/implementation-artifacts/11-19-doc-comment-enforcement-realignment.md]
- [Source: _bmad-output/implementation-artifacts/11-19-apphost-nuget-audit-suppression.md]
- [Source: _bmad-output/implementation-artifacts/11-19-localization-and-identifier-alignment.md]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-11.20-Recommended-analyzer-policy-and-exception-ledger]
- [Source: _bmad-output/planning-artifacts/prd.md#FR-25]
- [Source: _bmad-output/planning-artifacts/prd.md#FR-29]
- [Source: _bmad-output/planning-artifacts/architecture.md]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-07-16-post-correction.md]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-16-implementation-readiness-reconciliation.md]
- [Source: _bmad-output/project-context.md]
- [Microsoft: .NET SDK code-analysis properties](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props)
- [Microsoft: Analyzer configuration options](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options)
- [Microsoft: Analyzer configuration files and precedence](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-files)
- [Microsoft: C# warning options](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/errors-warnings)
- [Microsoft: CA1707](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1707)
- [Microsoft: CA1711](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1711)
- [Microsoft: Suppress code-analysis warnings](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/suppress-warnings)

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-07-17: Create-story analysis loaded repository instructions, BMAD workflow/config/template/
  checklist, all project-context files, sprint status, PRD/epic/architecture/UX inputs, the 11.19a-d
  stories, analyzer decision/census evidence, current warning controls, relevant code/test files,
  recent Git history, and current official Microsoft analyzer guidance.
- 2026-07-17: Administrator explicitly satisfied the separate Architecture/Product approval gate for
  Story 11.20; no approval is inferred for Stories 11.21-11.23.
- 2026-07-17: At context HEAD `6861ca1b...`, a no-incremental
  `AnalysisModeNaming=Recommended` instrumentation build with TWAE relaxed for enumeration produced
  CA1707 2,957 and CA1711 2 (2,959 total, 0 errors). The +1 baseline delta was traced to commit
  `335061df...` and `SourceToolsTypeOrganizationGovernanceTests.cs:67`.
- 2026-07-17: Unrelated in-flight changes to Story 11.17 and `deferred-work.md` were observed and left
  untouched; they are not part of Story 11.20's implementation or create-story File List.
- 2026-08-07: Resumed the story at HEAD `054817718c6157ea6d92cc6a06f5de30b8178ab1`. Verified the
  landed implementation against the spec rather than re-implementing it: the two `.editorconfig`
  CA1707 scopes, the ledger, `AnalyzerPolicyGovernanceTests.cs`, both CA1711 test-group renames, the
  narrowed HFC1002 source-local suppressions, and the removal of the stale Shell.Tests `IDE1006`,
  `CS1656`, and `IDE0058` entries were all already correct at HEAD and were left unchanged.
- 2026-08-07: Both 2026-07-17 blocking conditions were re-tested empirically and neither reproduces.
  `dotnet restore Hexalith.FrontComposer.slnx -c Release` exits 0 with no NU1506, because central
  package versions now come from the imported `references/Hexalith.Builds` catalog. `PackageBoundaryTests`
  declares `LocalizationAbstractionsVersion = "10.0.10"`; the `10.0.9` literal remains only as the
  deliberate mismatch input of the negative theory case. No package-policy change was required or made.
- 2026-08-07: `AnalyzerPolicy_GovernanceContract_FailsClosed` failed only on identifier-inventory
  drift (`count=6242`, sha256 `5291e6e9...` versus the sealed 6,240 / `e5f8c497...`). The drift was
  traced to underscore-named test identifiers added by unrelated commits landing after the previous
  reseal `b1ac767a` - HEAD `05481771` added `UiAppDocumentLanguageRenderTests.cs` and modified
  `AppHostNuGetAuditPolicyTests.cs`, `LocalizationGovernanceTests.cs`, and
  `FrontComposerUiAppHostTests.cs`. Story 11.20 owns this ledger, so the inventory was resealed here
  following the established `test(governance): reseal analyzer identifier inventory` maintenance
  pattern; the test then passed.
- 2026-08-07: Full re-verification at HEAD. Forced Release build 0 warnings / 0 errors; strict
  `AnalysisModeNaming=Recommended` candidate 0 warnings / 0 errors; full `AnalysisMode=Recommended`
  census with TWAE relaxed reported 1,122 warnings / 0 errors with zero Naming diagnostics; the whole
  default test matrix passed 4,217 / 4,217 across seven assemblies. No PublicAPI, schema, Verify,
  pact, package, solution, or submodule baseline changed.
- 2026-08-07: Story-artifact validation. The frontmatter `baseline_commit` `6861ca1b...` is long stale,
  so the default invocation cannot reconcile a File List whose implementation landed at commit
  `67154049`. The completion pass therefore reconciled the File List against this story's actual Git
  diff - the non-`references/**` paths of `67154049` plus the current working-tree changes - supplied
  explicitly:

  ```bash
  ARGS=()
  while IFS= read -r f; do
    case "$f" in references/*|"") continue;; esac
    ARGS+=(--changed-file "$f")
  done < <(git show --name-only --no-renames --pretty=format: 67154049; git diff --name-only)
  python3 eng/validate-story-artifacts.py \
    --story _bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md \
    "${ARGS[@]}"
  ```

  Result: `Story artifact validation passed.` The `references/**` submodule pointer bumps bundled into
  `67154049` are unrelated workspace drift and are deliberately excluded rather than absorbed into this
  story. `_bmad-output/implementation-artifacts/sprint-status.yaml` was the one genuinely missing File
  List row and was added.
- 2026-08-07: Adversarial-review hardening pass applied to the guard (the policy outcome was already
  correct and is unchanged). The most significant finding was structural: the identifier-seal
  assertion sat above the executable proofs in a single fact, so every routine reseal - the common
  failure in this repository's history, and one that recurred this session - silently skipped the
  compile specimens that are the only execution proof the two CA1707 scopes are exact.
  `AnalyzerPolicyGovernanceTests` is now four independent facts: the contract fact accumulates all
  positive lanes and asserts once, and the seal, the effective-build-graph proof, and the
  compile-specimen proof each stand alone. Verified by observing four tests execute with only the seal
  fact red before the reseal.
- 2026-08-07: Further guard fixes. `TrackedFiles` drops `--others` (tracked files only, per the story's
  own task text) and drains stdout/stderr concurrently under a 60 s bounded wait, removing a real
  deadlock shape. Root `WarningsNotAsErrors`/`WarningsAsErrors` carrying CA identifiers are now policed
  exactly as root `NoWarn`, closing a TreatWarningsAsErrors bypass that AC5 requires be shut. The
  category-disable guard now also covers the `[*]` section, the bulk
  `dotnet_analyzer_diagnostic.severity` property, and the `suggestion`/`hidden` severities. A new
  closed-world check fails closed if any tracked `.editorconfig`/`.globalconfig`/`.ruleset` outside
  `references/**` appears beyond the root `.editorconfig`. The root `TreatWarningsAsErrors` lookup no
  longer throws on 0 or 2+ elements. `reviewDate` is parsed with `TryParseExact`/`InvariantCulture`
  against `yyyy-MM-dd` and fails closed instead of failing open. `EditorConfigSection` returns every
  matching section, line-anchored, so a duplicate appended section cannot evade the repository-scope
  CA1707 check. The dead `allowTestWildcard` parameter was removed with no change to the wildcard rule.
  Eleven new synthetic negative cases were added alongside these.
- 2026-08-07: `AttributeParserTests.Parse_PropertySuppressMessageForOtherCheckId_StillReportsHFC1002`
  pins the HFC1002 checkId-discrimination branch, which was previously unpinned: the existing test's
  control property carried no attribute at all, so deleting the checkId comparison in
  `HasSuppressMessage` left every test green. Mutation-verified - forcing `HasSuppressMessage` to
  return true for any `SuppressMessageAttribute` fails exactly this one test (1 failed of 25) and
  nothing else. The mutation was reverted and `AttributeParser.cs` confirmed byte-identical to HEAD;
  no product code was changed by this story.
- 2026-08-07: The identifier inventory was resealed once, after all source edits, to 6,247 tokens /
  sha256 `ae482232...`. The delta over the earlier 6,242 seal is this pass's own additions - three new
  underscore-named governance facts, one new underscore-named parser test, and a discard token.
- 2026-08-07: Re-verification after hardening. Forced Release build 0 warnings / 0 errors; strict
  `AnalysisModeNaming=Recommended` 0 warnings / 0 errors; all four analyzer-policy Governance facts
  pass; full default matrix green at 4,221 tests. One transient failure of
  `FrontComposerHotPathLogTests.DisabledIdentifierEvent_AfterWarmup_AllocatesNothing` (24 bytes versus
  0) was observed in a single Shell run and is an order/JIT-sensitive allocation measurement owned by
  Story 11.18c, untouched by this story; three consecutive full Shell.Tests runs afterwards were green
  at 2,409 / 2,409.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Story 11.20 promoted only after explicit Architecture/Product approval, with later analyzer phases
  left separately gated.
- Corrected the placeholder validation model: strict Naming-only Recommended is the completion gate;
  full Recommended with TWAE relaxed is census evidence until Stories 11.21-11.22 burn down the
  remaining categories.
- Added exact brownfield file guidance, current census drift, ledger schema expectations, suppression
  narrowing, CA1707 compatibility scopes, CA1711 fixes, governance negatives, and preservation rules.
- 2026-08-07 completion pass: the story's policy implementation was already landed and correct at HEAD.
  The only change this pass required was resealing the analyzer identifier inventory in
  `analyzer-policy-exception-ledger-v1.json` to 6,242 tokens / sha256 `5291e6e9...`, absorbing
  underscore-named test identifiers introduced by unrelated commits after the previous reseal. The
  count-drift guard behaved exactly as designed: it fails closed on repository evolution and forces an
  explicit, owner-reviewed reseal rather than silently widening the CA1707 path scope.
- Both previously recorded blocking conditions were re-tested and cleared without any package-policy or
  package-baseline change, which the story forbids. The restore is green and the package-boundary test
  now pins 10.0.10 from the imported Hexalith.Builds catalog.
- All six acceptance criteria are satisfied at HEAD: the ledger reconciles baseline, refreshed, and
  post-policy censuses; CA1707 is scoped to exactly `[tests/**.cs]` and `FcDiagnosticIds.cs` with no
  repository-wide or category-wide CA suppression; both CA1711 findings are fixed by type rename rather
  than suppression; every warning control is classified with owner, review date, and trigger;
  `TreatWarningsAsErrors=true` remains canonical with the benchmark executable as the sole exception; and
  no central `AnalysisMode` was added, leaving repository activation to Story 11.23.

### File List

- `_bmad-output/implementation-artifacts/11-20-recommended-analyzer-policy-and-exception-ledger.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `.editorconfig`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `samples/Counter/Counter.Domain/Counter.Domain.csproj`
- `samples/Counter/Counter.Domain/CounterProjection.cs`
- `samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj`
- `samples/Counter/Counter.Specimens.Domain/SpecimenFormattingProjection.cs`
- `samples/IdeParityCounter/IdeParityCounter.csproj`
- `src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTestGroup.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureCollection.cs` (deleted)
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureInvarianceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureTestGroup.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs`

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 1 from Story 11.19d as a separately
  approval-gated backlog story.
- 2026-07-17: Administrator explicitly supplied Architecture/Product approval; create-story enriched
  the implementation guide, reconciled current Naming drift, and promoted backlog -> ready-for-dev.
- 2026-08-07: Completion pass re-verified the landed policy implementation at HEAD `05481771`, cleared
  both previously recorded package blockers empirically without any package change, resealed the
  analyzer identifier inventory for unrelated post-reseal test-identifier drift, and promoted
  blocked -> review (sprint-status vocabulary; the story frontmatter records the equivalent in-review).

## Auto Run Result

Status: done

All six acceptance criteria are satisfied and every validation lane is green at HEAD
`054817718c6157ea6d92cc6a06f5de30b8178ab1`.

The two 2026-07-17 blocking conditions no longer reproduce and were cleared without the package-policy
or package-baseline changes this story forbids. The solution restore exits 0 with no NU1506, because
central package versions now resolve through the imported `references/Hexalith.Builds` catalog; and
`PackageBoundaryTests` pins `Microsoft.Extensions.Localization.Abstractions` 10.0.10, with 10.0.9
surviving only as the intentional mismatch input of the negative theory case.

Evidence: forced Release build 0 warnings / 0 errors; strict `AnalysisModeNaming=Recommended` candidate
0 warnings / 0 errors; full `AnalysisMode=Recommended` census with TWAE relaxed for enumeration reported
1,122 warnings / 0 errors with zero Naming diagnostics, all remaining findings belonging to Stories
11.21 and 11.22; focused analyzer-policy Governance 1/1; focused SourceTools collection/culture lane
103/103; and the complete default test matrix 4,217 / 4,217 across Cli, Contracts, Contracts.UI, Mcp,
SourceTools, Testing, and Shell. No PublicAPI, schema, generated-output, Verify snapshot, pact, package,
solution, or submodule baseline changed.

The analyzer identifier inventory was resealed to 6,247 tokens / sha256 `ae482232...`. That absorbs
both the underscore-named test identifiers added by unrelated commits after the previous reseal
`b1ac767a` and the identifiers added by this pass's own hardening. No scope was widened.

An adversarial review of the guard returned fourteen findings, all applied. The load-bearing one was
structural: the identifier seal was asserted before the compile specimens inside a single fact, so a
routine reseal skipped the only execution proof that the two CA1707 scopes are exact.
`AnalyzerPolicyGovernanceTests` is now four independent facts. The remainder closed a
`WarningsNotAsErrors` bypass of TreatWarningsAsErrors, broadened the category-disable guard, added a
closed-world check over analyzer-configuration files, removed a `git` deadlock shape and an
untracked-file sensitivity, made `reviewDate` parsing and the root TWAE lookup fail closed, fixed a
first-match-only EditorConfig section scan, and pinned the previously unpinned HFC1002 checkId branch
with a mutation-verified test. The policy outcome itself is unchanged: Naming remains 0.

## Suggested Review Order

**Analyzer policy — the actual change to what is enforced**

- The whole policy delta: two exact CA1707 scopes, nothing category- or repository-wide.
  [`.editorconfig:70`](../../.editorconfig#L70)

- The canonical machine-readable ledger every control and finding must reconcile against.
  [`analyzer-policy-exception-ledger-v1.json:1`](../contracts/analyzer-policy-exception-ledger-v1.json#L1)

**Governance guard — why the scopes cannot silently widen**

- Entry point: the four facts. The split is the point — specimens no longer hide behind the seal.
  [`AnalyzerPolicyGovernanceTests.cs:39`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L39)

- The only execution proof the two scopes are exact; previously unreachable on seal drift.
  [`AnalyzerPolicyGovernanceTests.cs:175`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L175)

- Closes the root MSBuild bypass: WarningsNotAsErrors could neutralise TWAE per CA rule.
  [`AnalyzerPolicyGovernanceTests.cs:348`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L348)

- Fail-closed world: any analyzer-config file beyond the root `.editorconfig` is rejected.
  [`AnalyzerPolicyGovernanceTests.cs:664`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L664)

- Tracked-only inventory, concurrent pipe drain, kill-on-timeout; was untracked-sensitive and deadlock-prone.
  [`AnalyzerPolicyGovernanceTests.cs:924`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L924)

- Exact-format invariant-culture parse; ambient-culture TryParse previously failed open.
  [`AnalyzerPolicyGovernanceTests.cs:232`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L232)

- Returns every matching section; first-match-only let a duplicate header evade the scope check.
  [`AnalyzerPolicyGovernanceTests.cs:997`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L997)

**CA1711 — fixed by rename, never suppressed**

- Collection string `"DiagnosticRegistry"` and DisableParallelization preserved across the rename.
  [`DiagnosticRegistryTestGroup.cs:3`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTestGroup.cs#L3)

- Same treatment for the culture-serialising collection; isolation behaviour unchanged.
  [`DriftCultureTestGroup.cs:9`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Drift/Regression/DriftCultureTestGroup.cs#L9)

**HFC1002 narrowing — project-wide NoWarn replaced by source-local suppression**

- The generator seam the samples now depend on after their project-wide NoWarn was removed.
  [`AttributeParser.cs:506`](../../src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs#L506)

- CheckId matching; only the property symbol is inspected, by design and now documented.
  [`AttributeParser.cs:1073`](../../src/Hexalith.FrontComposer.SourceTools/Parsing/AttributeParser.cs#L1073)

- One property, one rationale — replaces a whole-project suppression.
  [`CounterProjection.cs:15`](../../samples/Counter/Counter.Domain/CounterProjection.cs#L15)

**Peripherals**

- Pins the checkId branch: a non-HFC1002 SuppressMessage must not suppress. Mutation-verified.
  [`AttributeParserTests.cs:103`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Parsing/AttributeParserTests.cs#L103)

- Evidence for the completion gate, the census reconciliation, and the unrelated-path declarations.
  [`test-summary.md:1`](tests/test-summary.md#L1)
