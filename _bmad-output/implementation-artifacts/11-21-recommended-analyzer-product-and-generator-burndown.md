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
status: done
implementation_baseline_commit: 4a8cfa4926b8fc52850da70f811103a91df22dfc
storyType: implementation-phase
approvalGate: separate-architecture-product-approval
approvalStatus: approved
approvedBy: Administrator
approvedOn: 2026-07-17
implementationEntryGate: story-11.20-done-and-approved-ledger-present
---

# Story 11.21: Recommended Analyzer Product and Generator Burn-down

Status: done.

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

- [x] Satisfy the implementation entry gate and rebase the owned census (AC: 1, 2, 7)
  - [x] Verify Story 11.20 is `done`, its canonical JSON ledger exists with explicit approval, and
        `AnalyzerPolicyGovernanceTests` passes. Stop without source edits if any condition fails.
        **Deviation, approved by Administrator 2026-08-07:** 11.20 is `review`, not `done`. Substance
        re-verified at this HEAD (40/40 tasks, approved ledger present, Governance 4/4) and accepted.
  - [x] Verify that ledger already contains approved exact-symbol dispositions for product findings
        whose correction would break public API, including the three current CA1000 members in
        `QueryResult<T>` and the Testing builders. If any required disposition is absent, stop for a
        scoped Architecture/Product ledger amendment; Story 11.21 cannot manufacture an exception.
        **Absent as predicted — escalated, not manufactured.** Administrator approved the scoped
        amendment; disposition `design-ca1000-generic-static-compatibility` added.
  - [x] Record the implementation commit and exact SDK, MSBuild, Roslyn, UTC date, restore mode, TFM,
        generated-code treatment, and command for the refreshed census. The decision baseline is
        commit `d9c19a4...`, SDK `10.0.302`, MSBuild `18.6.4`, and Roslyn `5.6.0`; current local MSBuild
        has already drifted, so copied counts are not completion evidence.
        Recorded in ledger block `story1121Census`.
  - [x] Reconcile rather than overwrite the approved ledger. Assign every refreshed finding exactly
        once by project/TFM, diagnostic ID, source path or generated hint, source-vs-generated origin,
        owning story, and `fix|approved-exception|later-story` disposition.
        (Aggregate reconciliation recorded; per-finding fix evidence lands with the burn-down.)
        Complete: every refreshed finding is assigned by project/TFM, diagnostic ID and
        source-vs-generated origin in `story1121Census`, and the final 275 -> 0 / 503 -> 0 outcome
        is sealed in `story1121Completion`. The approved ledger was extended, never overwritten.
  - [x] Preserve the approved 89-CA1707 exact-file compatibility treatment for
        `FcDiagnosticIds.cs`; do not rename those public constants or count them as 11.21 source edits.
        Untouched; the scope is confirmed working (Contracts 119 -> 30 with CA1707 no longer counted).
  - [x] Escalate baseline growth above 5% in an owned scope, any unmatched finding/control, or any
        proposed exception not already approved by Story 11.20.
        Drift -1.1% (no escalation required); the unapproved CA1000 exception was escalated.

- [x] Disposition all 367 shipped-product findings by project and defect class: fix the 278
      actionable findings and retain only the 89 pre-approved CA1707 exceptions (AC: 2, 4, 5)
  - [x] Use the exact baseline matrix in Dev Notes; refresh it before editing, and keep a
        machine-reconcilable before/after count for every project and diagnostic.
        Refreshed at HEAD `6388d5a5` before editing; machine-reconcilable before/after counts per
        project and per diagnostic are recorded in the Debug Log and the ledger.
  - [x] Migrate the exact 73 low-severity direct Shell log calls across the 20-file remainder ledger
        to an internal eponymous source-generated helper or existing matching helper. Allocate a new
        collision-free EventId family; do not renumber Security `5660-5691`, HotPath `5700-5780`, or
        Warning `5800-5853` events.
        New `Shell/Infrastructure/Telemetry/FrontComposerDiagnosticLog.cs` carries all 73 events at
        **EventIds 6000-6072** — above every occupied Shell band and above the 5900-5926 band the
        SourceTools slice took. Level, template, placeholder names, argument order, cardinality and
        exception attachment are unchanged for all 73. The three Story 11.18 range assertions are
        verbatim untouched (orchestrator confirmed the diff adds only the new `Enumerable.Range(6000, 73)`
        assertion and removes no `5660`/`5700`/`5800` line).
  - [x] Resolve product CA1873 sites by deferring expensive computation behind `IsEnabled` or a
        source-generated method. Preserve hashing, bounded identifiers, exception attachment,
        support-safety, and exactly-once behavior; do not broadly suppress the pinned-SDK rule.
        Shell 83 -> 0 and Mcp 4 -> 0. A 5-variant probe confirmed the known .NET 10 limitation: the
        analyzer ignores `IsEnabled` guards entirely and keys off the argument being an invocation.
        Existing early-return guards were kept untouched and the projections bound to locals *after*
        the guard, so laziness is preserved. No pragma and no broad suppression of the pinned rule.
  - [x] Apply semantic fixes for non-logging diagnostics: explicit culture based on data meaning;
        correct throw helpers without changing exception contracts; idempotent disposal and
        unsubscribe/cancellation order; private/internal type narrowing only; cached immutable
        objects only where lifetime is safe; and equivalent overloads without wire/display drift.
        Culture chosen by data meaning: `CurrentCulture` for user-facing formatting (the analyzer
        explicitly flags `CurrentUICulture` as "inappropriate for formatting methods") and
        `InvariantCulture` for emitted source. Disposal made idempotent; only private/internal types
        narrowed; equivalent overloads introduced with no wire or display drift.
  - [x] Treat public CA1000/design findings and any signature-affecting CA1068/CA1859 proposal as
        compatibility decisions. Preserve public members unless Story 11.20 already contains an
        exact-symbol approved disposition; do not opportunistically update PublicAPI baselines.
        The one CA1068 site is on `internal sealed record FrontComposerMcpProjectionReadSnapshot`,
        constructed via named arguments, so reordering is source- and behaviour-inert and needed no
        escalation. No public signature moved; both `PublicAPI.Shipped.txt` files are untouched and
        package validation reports zero ApiCompat codes.
  - [x] Split `Testing/Builders.cs` only if it is touched, preserving the two public type names and
        namespaces while satisfying the repository's one-type-per-file rule.
        Done under the approved entry-gate CA1000 amendment: `Testing/Builders.cs` was split into
        `ProjectionTestDataBuilder.cs` and `CommandTestDataBuilder.cs`, preserving both public type
        names and namespaces while satisfying the one-type-per-file rule.

- [x] Fix the 503 measured SourceTools-generated findings in their three owning emitters (AC: 3-7)
      **Complete: generated findings 503 -> 0.** Orchestrator-verified, not accepted on the
      implementing agent's report.
  - [x] `CommandFormEmitter` owns 307 findings: CA1507 12, CA1816 18, CA1822 5, CA1848 182, and
        CA1873 90. Add red tests before changing emitted forms; preserve validation, authorization,
        lifecycle dispatch, one-in-flight admission, row identity, disposal, and form rendering.
        CA1816 `GC.SuppressFinalize(this)` in emitted `Dispose()`; CA1822 emits `HasClientParseErrors`
        `static` when no parse-error backing fields exist; CA1507 helper parameters renamed to
        `commandPropertyName`.
  - [x] `CommandRendererEmitter` owns 171 findings: CA1816 5, CA1822 16, CA1848 97, CA1861 17, and
        CA1873 36. Preserve density modes, authorization retry timing, derived-value prefilling,
        destructive confirmation, return-path safety, and route behavior.
        CA1816 `GC.SuppressFinalize(this)`; CA1822 emits `ResolveIcon` `static` when no `[Icon]` name;
        CA1861 hoists the popover show-fields array to a `private static readonly string[]` emitted
        only on the popover path.
  - [x] `RazorEmitter` owns 25 findings: CA1816 7, CA1822 3, CA1845 7, and CA1859 8. Preserve
        projection customization precedence, query/fallback behavior, accessibility markup,
        generated hint names, and artifact count.
        CA1816 in both `DisposeAsync()` and the non-grid `Dispose()`; CA1845 `Truncate` via
        `string.Concat(value.AsSpan(...), "…")`; CA1859 concrete `string[]`/`HashSet<BadgeSlot>`;
        CA1822 `RenderTemplateDefaultField` emitted `static` only when its body reaches none of the
        four instance members, decided from the scratch-buffered body so the choice is valid in both
        Debug and Release.
  - [x] Emit private source-generated logging methods inside the existing partial generated types,
        or use an existing accessible contract-neutral seam. Follow the repository signature rule:
        `ILogger` first, `Exception` second when present, PascalCase placeholders, deterministic
        EventId/EventName, and no new public runtime contract.
        **Took the second clause (contract-neutral seam) — the first is impossible.** `[LoggerMessage]`
        cannot be emitted by a source generator: Roslyn does not feed one generator's output into
        another, so the compile-time logging generator never observes an emitted
        `static partial void` declaration and the consumer build fails CS8795 (proved empirically on
        SDK 10.0.302 / Roslyn 5.6.0). Dropping `private` would compile but silently delete every
        logging call. Used cached `LoggerMessage.Define` delegates plus private static wrappers —
        the same construct the compile-time generator emits internally, keeping the `IsEnabled`
        short-circuit inside the delegate. New shared emitter
        `SourceTools/Emitters/GeneratedLogMethodEmitter.cs`; EventId band 5900+ (form 5900-5911,
        renderer 5920-5926), disjoint from Shell's Security 5660-5691 / HotPath 5700-5780 /
        Warning 5800-5853 families.
  - [x] Preserve generated method, property, route, JSON, lifecycle, and HFC surfaces. Generated text
        and verified snapshots may change only where the analyzer fix requires it; review every
        accepted diff and keep hint paths/artifact inventory stable.
        24 `.verified.txt` snapshots re-approved after per-file semantic review; every diff reduces to
        pragma removal, `commandPropertyName`, `GC.SuppressFinalize`, a `static` modifier,
        `string[]`/`HashSet`, `string.Concat`, the hoisted popover array, counter-declaration removal,
        or `seq++` -> literal. Hint names and artifact inventory unchanged.

- [x] Remove 11.21-owned ASP0006 debt with literal render-tree sequencing (AC: 3, 5, 7)
  - [x] Inventory `seq++`/computed sequence emission in `CommandFormEmitter`,
        `CommandRendererEmitter`, `RazorEmitter`, `CommandPageEmitter`, and
        `ProjectionRoleBodyEmitter`; inventory all of `ColumnEmitter`, including its direct
        `colSeq++` emission and `SequenceExpression` helper, before adding any numbering abstraction.
        Reuse or extend the existing helper rather than creating parallel sequencing schemes.
  - [x] Assign literals at generator execution time so generated `RenderTreeBuilder` call sites use
        stable source-location numbers. Reuse the same literal for a runtime loop call site; use
        explicit `OpenRegion`/`CloseRegion` only where a long generated block needs its own sequence
        scope. Do not substitute another runtime counter.
        One central allocator, `SourceTools/Emitters/RenderTreeSequenceRewriter.cs`, applied at emit
        time from all four `Emit` entry points (which transitively covers `ProjectionRoleBodyEmitter`
        and `ColumnEmitter` output). It is Roslyn-syntax based and **fails safe**: a counter with any
        reference that is not a postfix `++` in call-argument position or a constant reset is left
        completely untouched. Parsed with `DEBUG` defined so `#if DEBUG` dev-mode blocks are numbered
        and the result is valid in both consumer configurations. `RenderNewItemIndicators` lost its
        `ref int seq` parameter and is wrapped by the caller in `OpenRegion`/`CloseRegion`.
  - [x] Remove the emitted ASP0006 disable/restore pragmas from command form and renderer output.
  - [x] Negative-control every ASP0006 entry in Counter.Domain, Counter.Specimens.Domain,
        IdeParityCounter, Counter.Web, Counter.Specimens, Shell.Tests, Testing.Tests, and the packaged
        consumer template. Remove only controls whose violations came from 11.21-owned emission;
        retain and ledger any genuine 11.22 fixture exception rather than absorbing it.
        Orchestrator re-ran all seven with `-p:NoWarn= -p:TreatWarningsAsErrors=false`: **0 generated
        ASP0006 everywhere.** `NoWarn` removed from the six consumers and the packaged-consumer
        template. Shell.Tests retains its control for exactly 17 hand-authored fixture sites
        (`Generated/ActionQueueProjectionContextIsolationTests.cs` 13,
        `Components/Rendering/FcProjectionViewOverrideHostTests.cs` 2,
        `Components/DataGrid/FcNewItemIndicatorLaneIntegrationTests.cs` 2) — retained and ledgered as
        Story 11.22 debt under `asp0006-hand-authored-fixture-debt`, not absorbed.
  - [x] Update the packaged generated-consumer test to set `AnalysisMode=Recommended`, preserve TWAE,
        and remove ASP0006 suppression. `CompilationHelper`/`GeneratorDriverTests` do not load the SDK
        analyzer set and are insufficient by themselves.
        Proved non-vacuous: a throwaway CA1822 violation injected into the generated temp consumer
        warns under the template's `AnalysisMode=Recommended` and is silent under
        `-p:AnalysisMode=Default`, so the packaged consumer really is analysed at Recommended.

- [x] Add focused regression and governance evidence (AC: 2-7)
  - [x] Extend emitter syntax, determinism, snapshot, and behavior tests for every changed output;
        update only affected `.verified.txt` files after inspecting semantic diffs.
        9 rewriter tests plus 12 emitter behaviour tests added; 24 affected `.verified.txt` files
        re-approved after per-file semantic review.
  - [x] Add generated-consumer assertions for exact Recommended diagnostic zero and unsuppressed
        ASP0006 zero. Prove the 302 Shell.Tests generated findings disappear without claiming its
        hand-authored test source is Story 11.21-clean.
        The packaged consumer test now asserts zero CA/ASP diagnostics at Recommended with TWAE on
        and no ASP0006 control, proven non-vacuous by an injected CA1822 probe. Shell.Tests generated
        findings are 0 while its 72 hand-authored findings remain openly owned by Story 11.22.
  - [x] Update `SecurityLoggingGovernanceTests` from its exact 73-call remainder to zero, retaining
        non-vacuous synthetic negatives, EventId collision checks, placeholder/signature parity,
        support-safety, and disabled-path laziness.
        73 -> 0. Synthetic negatives retained and the exception-parameter guard hardened against a
        bind failure that had made the support-safety assertions fragile. 7/7 green.
  - [x] Run focused tests for every changed product package, including public API behavior, schema
        truncation/fingerprint determinism, MCP admission/fail-closed behavior, lifecycle/disposal,
        logging event contracts/cardinality, and generated UI behavior as applicable.
        Default lane across all seven test projects: 4274 total, 0 failed.
  - [x] Update the Story 11.20 ledger with fix evidence and final counts; do not create a second
        analyzer ledger or merge this policy with the unrelated package-compatibility suppression
        ledger.
        `story1121Completion` added and the drifted identifier seal recomputed to
        `count=6307`. One ledger only; the package-compatibility suppression ledger is untouched.

- [x] Run the scoped candidate, compatibility, and completion gates (AC: 5-7)
  - [x] Run a normal forced Release `.slnx` build with canonical TWAE and require 0 warnings/0 errors.
        Exit 0, 0 Warning(s) / 0 Error(s).
  - [x] Run strict command-line `AnalysisMode=Recommended` builds with TWAE unchanged for CLI,
        Contracts net10.0, Contracts.UI, MCP, Schema net10.0, Shell, and Testing; require zero
        actionable findings after approved exceptions.
        All seven exit 0 with 0 warnings / 0 errors.
  - [x] Build Contracts and Schema explicitly for netstandard2.0 under their preserved analyzer
        boundary, and build/package SourceTools as netstandard2.0 with Contracts as its only runtime
        dependency.
        Both legs 0 warnings; SourceTools packs netstandard2.0 with Contracts as its only runtime
        dependency. Fixes lacking netstandard2.0 APIs are guarded by `#if NET10_0_OR_GREATER`.
  - [x] Build Counter.Domain, Counter.Specimens.Domain, and IdeParityCounter under the candidate gate;
        use the strict clean packaged consumer as independent emitter proof. Run a full candidate
        census with TWAE relaxed only for enumeration and require zero diagnostics whose location is
        an owned product file or SourceTools-generated output.
        All three exit 0 at 0/0 under strict Recommended, and the full candidate census shows zero
        diagnostics located in an owned product file or in SourceTools-generated output.
  - [x] Run each affected test project/assembly individually with `DiffEngine_Disabled=true`, then
        Governance and Contract lanes using repository trait conventions. Do not use solution-level
        `dotnet test`; `.slnx` is for restore/build.
        Every project run via its built xUnit v3 executable with `DiffEngine_Disabled=true`;
        Governance 218/0 and Contract 3/0. Solution-level `dotnet test` was not used.
  - [x] Run package validation for every changed packable project, PublicAPI/schema/generated-output
        checks, Pact/contract-artifact validation, intentional Verify review, docs validation when a
        published contract changes, story-artifact validation, `git diff --check`, and mechanical
        changed-file/File-List reconciliation.
        All eight packable projects validate with zero ApiCompat codes; contract-artifact validation
        exits 0; `git diff --check` clean; `docs/` untouched so the DocFX gate is not triggered.
  - [x] Confirm no central `AnalysisMode`, weaker TWAE, new analyzer package, broad CA/ASP suppression,
        hand-edited `obj`, unapproved public/schema/wire change, release-workflow edit, UX behavior
        change, or submodule edit entered the story.
        Confirmed: no central `AnalysisMode`, no TWAE weakening, no new analyzer package, no broad
        CA/ASP suppression, no hand-edited `obj`, no unapproved public/schema/wire change, no
        release-workflow edit, no UX behaviour change and no submodule edit entered this story.

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

- 2026-08-07: Implementation session opened at HEAD `4a8cfa4926b8fc52850da70f811103a91df22dfc` on branch
  `fix/11-21-recommended-analyzer-burndown`. Toolchain stamped: SDK `10.0.302`, MSBuild `18.6.11.33009`,
  Roslyn `5.6.0`, UTC `2026-08-07T17:50:01Z`. Restore mode
  `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0`.
- 2026-08-07: **Entry gate adjudicated with Administrator (two deviations, both explicitly approved).**
  (a) Story 11.20 is `review`, not `done`. Its substance was re-verified at this HEAD before proceeding:
  40/40 tasks checked, approved ledger present, `AnalyzerPolicyGovernanceTests` 4/4 green. Administrator
  accepted the review-state as satisfying the predecessor gate; Story 11.20 still owes its own code
  review independently of this story.
  (b) The ledger contained **zero** CA1000 dispositions — Story 11.20's scope was Naming findings plus
  warning controls, so the Design category was never classified. Per Task 1 this story cannot manufacture
  an exception, so it was escalated. Administrator approved a scoped ledger amendment: a narrow
  exact-symbol compatibility exception for the three public-API-breaking CA1000 members.
- 2026-08-07: Refreshed census reconciled against the create-story baseline. Generated findings match
  **exactly** at 503 with an identical per-diagnostic distribution. Product actionable findings moved
  278 -> 275: Contracts fell 119 -> 30 because Story 11.20's exact-file CA1707 scope now suppresses its
  89 compatibility findings as designed, and Shell CA1873 fell 83 -> 80 through unrelated repository
  evolution. Drift is -1.1%, below the 5% escalation threshold. Owned total 778; 342 hand-authored
  test/sample findings remain explicitly owned by Story 11.22.
- 2026-08-07: Ledger schema note — its `findings` array is the Naming-census reconciliation set whose
  counts must sum to the sealed Naming totals, and `warningControls` is a closed-world inventory that
  must match the controls actually discovered in tracked sources. The CA1000 exception is therefore
  recorded as a disposition plus a `source-suppression-attributes` inventory update (entryCount 40 -> 43,
  CA1000 added), not as findings rows. `AnalyzerPolicyGovernanceTests` proved non-vacuous by failing
  closed on the first (incorrect) shape and passing 4/4 after correction.

- 2026-08-07: **Generated logging slice complete and independently re-verified by the orchestrator**
  (not accepted on the implementing agent's report alone). Generated CA1848 279 -> 0 and CA1873 126 -> 0
  across all four consumers; owned generated findings 503 -> 98.

  | Consumer | generated before | after | remaining distribution |
  | --- | ---: | ---: | --- |
  | Shell.Tests specimens | 302 | 55 | CA1507 8; CA1816 17; CA1822 14; CA1845 3; CA1859 3; CA1861 10 |
  | Counter.Specimens.Domain | 94 | 21 | CA1816 7; CA1822 6; CA1845 2; CA1859 3; CA1861 3 |
  | Counter.Domain | 79 | 15 | CA1507 4; CA1816 4; CA1822 2; CA1845 1; CA1859 1; CA1861 3 |
  | IdeParityCounter | 28 | 7 | CA1816 2; CA1822 2; CA1845 1; CA1859 1; CA1861 1 |

  Every remaining count equals its baseline minus that consumer's CA1848+CA1873 exactly, proving no
  non-logging diagnostic moved. SourceTools Release 0 warnings / 0 errors; SourceTools.Tests via the
  direct xUnit v3 executable with `DiffEngine_Disabled=true`: 1098 total, 0 failed, 0 errors, 0 skipped.
- 2026-08-07: CA1873 adjudication — the diagnostic stops firing because the call is no longer an
  `ILogger.Log*` invocation, so the orchestrator audited whether this was a real fix or a silencing.
  All 20 emitted call sites pass only locals, compile-time literals, or trivial property reads; the sole
  method-call argument is `ResolveLoggingCorrelationId()` (`=> _lifecycleState.Value.CorrelationId`),
  on a rare rejected-return-path branch at Error level. Nothing expensive is evaluated eagerly, so the
  resolution is genuine. Call-site null guards were kept as `if (Logger is not null) { ... }` rather than
  moved inside the helpers, preserving the exact argument-evaluation short-circuit of `Logger?.Log*`.
- 2026-08-07: Two SourceTools tests required real changes beyond snapshot re-approval.
  `Emit_CommandExecutionAdmissionReleasesInFinally` anchored on literal text that moved into the
  delegate block and threw; it was re-anchored on the call site. `Emit_DoesNotLogModelInstance` filtered
  on `"Logger?"` and would have become **vacuous** (empty `ShouldAllBe`); it now filters `"(Logger,"`,
  asserts `ShouldNotBeEmpty()`, and checks `ShouldNotContain("{Model}")`. Nine `.verified.txt` snapshots
  were re-approved after per-file semantic review confirming the multiset of (level, template) pairs is
  unchanged.

- 2026-08-07 (session 2): Re-opened on branch `fix/11-21-analyzer-burndown-2` at HEAD
  `6388d5a5c311988d2cd29b0aa9755ac1e13bc693` (session 1's work merged as PR #82). Toolchain re-stamped:
  SDK `10.0.302`, MSBuild `18.6.11.33009`, Roslyn `5.6.0`, UTC `2026-08-07T19:40:34Z`, restore
  `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0`.
  Census re-measured with `-p:AnalysisMode=Recommended -p:TreatWarningsAsErrors=false --no-incremental -m:1`
  (TWAE relaxed for enumeration only), deduplicated on `(file, line, col, id)` because MSBuild echoes each
  diagnostic in its summary, and attributed by owning project path.

  **Remaining owned product source — 275 actionable:**

  | Project/TFM | Count | Exact diagnostic distribution |
  | --- | ---: | --- |
  | Shell | 217 | CA1001 1; CA1305 25; CA1510 6; CA1513 5; CA1816 7; CA1834 5; CA1848 73; CA1859 8; CA1865 4; CA1873 83 |
  | Contracts net10.0 | 28 | CA1510 16; CA1850 1; CA1859 3; CA1861 1; CA1865 2; CA1870 1; CA2249 3; CA2263 1 |
  | MCP | 24 | CA1068 1; CA1305 11; CA1513 1; CA1859 6; CA1865 1; CA1873 4 |
  | Schema net10.0 | 3 | CA1510 2; CA1845 1 |
  | Contracts.UI | 2 | CA1861 2 |
  | CLI | 1 | CA1865 1 |
  | Testing | 0 | Cleared: its single CA1000 was resolved by the approved entry-gate builder split |
  | **Total** | **275** | The 89 CA1707 compatibility findings are already suppressed by Story 11.20's exact-file scope and are no longer counted |

  Reconciles to session 1's recorded 275 in total, but the per-project split moved: Contracts fell
  30 -> 28 and Testing 1 -> 0 (the entry-gate CA1000 work), while Shell CA1873 returned 80 -> 83. This
  HEAD measurement supersedes session 1's per-project figures.

  **Remaining owned generated output — 98:**

  | Consumer | Count | Exact diagnostic distribution |
  | --- | ---: | --- |
  | Shell.Tests specimens | 55 | CA1507 8; CA1816 17; CA1822 14; CA1845 3; CA1859 3; CA1861 10 |
  | Counter.Specimens.Domain | 21 | CA1816 7; CA1822 6; CA1845 2; CA1859 3; CA1861 3 |
  | Counter.Domain | 15 | CA1507 4; CA1816 4; CA1822 2; CA1845 1; CA1859 1; CA1861 3 |
  | IdeParityCounter | 7 | CA1816 2; CA1822 2; CA1845 1; CA1859 1; CA1861 1 |
  | **Total** | **98** | CA1507 12; CA1816 30; CA1822 24; CA1845 7; CA1859 8; CA1861 17 |

  This equals the sealed 503 generated baseline minus exactly CA1848 279 + CA1873 126, independently
  re-confirming that session 1's logging slice moved no non-logging diagnostic. Attribution note for any
  re-measurement: a raw Shell.Tests build reports 91 generated findings because it also compiles the
  referenced Counter.Domain (15) and Counter.Specimens.Domain (21) generated trees; Shell.Tests' own
  specimens are 55.

  **ASP0006 control inventory in tracked sources (all still present):** emitted pragma pairs in
  `CommandFormEmitter.cs` (lines 32/416) and `CommandRendererEmitter.cs` (lines 30/612); `NoWarn` entries
  in `Counter.Domain.csproj`, `Counter.Specimens.Domain.csproj`, `Counter.Specimens.csproj`,
  `Counter.Web.csproj`, `IdeParityCounter.csproj`, `Shell.Tests.csproj`, `Testing.Tests.csproj`, and the
  inline consumer template in `PackagedAnalyzerConsumerTests.cs` (line 71). Runtime `seq++`/`colSeq++`
  emission additionally spans `ProjectionRoleBodyEmitter.cs`, `RazorEmitter.cs`, `CommandPageEmitter.cs`,
  and `ColumnEmitter.cs`.

- 2026-08-07 (session 2): **Generated non-logging + ASP0006 slice complete and independently
  re-verified by the orchestrator**, not accepted on the implementing agent's report.

  | Consumer | generated before | after |
  | --- | ---: | ---: |
  | Shell.Tests specimens | 55 | **0** |
  | Counter.Specimens.Domain | 21 | **0** |
  | Counter.Domain | 15 | **0** |
  | IdeParityCounter | 7 | **0** |
  | **Total** | **98** | **0** |

  Per diagnostic CA1507 12->0, CA1816 30->0, CA1822 24->0, CA1845 7->0, CA1859 8->0, CA1861 17->0, so
  the full sealed 503 generated baseline is now zero. Each consumer's **non-generated** count was
  unchanged across the change (247/247/247 for the samples, 346 for Shell.Tests), proving no product
  finding moved into or out of scope. Ground truth was additionally taken by emitting the trees
  (`-p:EmitCompilerGeneratedFiles=true` to a scratch path): `seq++` 0 occurrences, `ASP0006` 0
  occurrences, `GC.SuppressFinalize` present. Note for re-measurement: Roslyn reports generated-tree
  diagnostics against a synthesized `obj/<GeneratorAssembly>/<GeneratorType>/<HintName>` path that need
  not exist on disk, so attribute by that path pattern rather than by file existence.

  Verification commands and exit codes (orchestrator-run):
  `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1` -> 0,
  **0 Warning(s) / 0 Error(s)**; 4x consumer census at `AnalysisMode=Recommended` -> 0; 7x ASP0006
  negative control with `-p:NoWarn=` -> 0 with 0 generated ASP0006; SourceTools.Tests default lane
  (direct xUnit v3 executable, `DiffEngine_Disabled=true`) -> **1117 total, 0 failed**; Shell.Tests
  default lane -> **2409 total, 1 failed** (only `AnalyzerPolicy_IdentifierInventory_MatchesSeal`);
  `SecurityLoggingGovernanceTests` -> 7/7, so the pinned 73-call remainder and the 11.18 EventId ranges
  are provably untouched by this slice.

  Two findings from the re-verification worth recording:
  (a) An unfiltered SourceTools.Tests run reports 1 failure,
  `ParseStagePerformanceTests.ParseStage_20PlusTypes_CompletesUnder500ms` (672ms vs a 500ms budget).
  It carries `[Trait("Category","Performance")]`, so it is advisory-only and outside the default lane;
  it is untouched by this story's diff and passes 3/3 in isolation. It is a load-sensitive wall-clock
  flake, not a regression — the rewriter runs at emit time, not in the parse stage it measures.
  (b) `AnalyzerPolicy_IdentifierInventory_MatchesSeal` is deliberately left RED. New test methods
  shifted the CA1707-scope identifier seal from `count=6247` to `count=6259`
  (`sha256=caed487c0c5ea1d335f877d8635a076811e2f2bd5b42179d0c97b545cd0c74ba` at the time of measuring).
  The seal is computed over `git ls-files`, so untracked new test files are not yet counted and every
  later slice shifts it again; it is re-sealed once, after all slices land and are staged.

  Behavioural note for review: literal sequencing changes render-tree *diff* behaviour where a runtime
  counter previously produced unique ascending numbers — distinct `case` arms now carry distinct fixed
  numbers, so switching between them replaces the subtree instead of patching it. This is the
  documented, intended Razor semantics (sequence numbers identify source location, not execution
  order); the rendered DOM is unchanged and all 2409 default-lane Shell.Tests bUnit tests pass.

- 2026-08-07 (session 2): **Shell product slice complete and independently re-verified by the
  orchestrator.** `src/Hexalith.FrontComposer.Shell` went **217 -> 0** actionable findings; the refreshed
  census now lists no Shell row at all, leaving exactly the 58 findings owned by the remaining product
  slice.

  | ID | Before | After | Resolution |
  | --- | ---: | ---: | --- |
  | CA1873 | 83 | 0 | 55 vanished with the CA1848 migration; 23 in the 11.18 telemetry wrappers were the known guarded-call false-positive shape, fixed by hoisting sanitizer results into locals inside the existing `IsEnabled` guard; 5 in `LifecycleStateService`/`ProjectionConnectionStateService` were genuinely eager (two sha256 `DigestIdentifier` calls and enum `ToString()`s) and gained a real `IsEnabled(Information)` guard |
  | CA1848 | 73 | 0 | migrated to the new 6000-6072 source-generated family |
  | CA1305 | 25 | 0 | 14 UI sites passed `CurrentUICulture` as an `IFormatProvider`, which is exactly what the analyzer flags — the diagnostic text reads "this property returns a culture that is inappropriate for formatting methods" — so they became `CurrentCulture`; 11 `StringBuilder.AppendLine($"…")` sites in `Services/DevMode/RazorEmitter` became `InvariantCulture` because emitted source must be culture-invariant |
  | CA1859 | 8 | 0 | private/private-static returns and one local narrowed to concrete types; 3 cascaded findings also fixed; no public signature moved |
  | CA1816 | 7 | 0 | `GC.SuppressFinalize(this)` in component `Dispose`/`DisposeAsync` |
  | CA1510 | 6 | 0 | `ArgumentNullException.ThrowIfNull` |
  | CA1513 | 5 | 0 | `ObjectDisposedException.ThrowIf` |
  | CA1834 | 5 | 0 | `Append(char)` via a new private `SeparatorChar`, leaving the public `FrontComposerStorageKey.Separator` const untouched |
  | CA1865 | 4 | 0 | `StartsWith`/`EndsWith(char)`; every site already passed `StringComparison.Ordinal`, so exactly equivalent |
  | CA1001 | 1 | 0 | `ETagCacheService` implements `IDisposable` and disposes `_lruSeedGate` |
  | **Total** | **217** | **0** | |

  Orchestrator verification: refreshed product census -> Shell absent, total 58 (all other projects).
  Strict `AnalysisMode=Recommended` **with TWAE unchanged** on Shell exits 1 with 56 errors, and every
  one is located in `src/Hexalith.FrontComposer.Contracts` — **zero errors in Shell source** — so Shell
  passes its own strict gate and the failure is only dependency propagation that the remaining product
  slice clears. Canonical Release `.slnx` -> **0 Warning(s) / 0 Error(s)**. Shell.Tests default lane
  **2417 total, 1 failed**; Governance lane **218 total, 1 failed**; both failures are the single expected
  `AnalyzerPolicy_IdentifierInventory_MatchesSeal` drift. Contract lane 3/3.

  Two governance points worth a reviewer's attention:
  (a) The slice **hardened** a guard rather than relaxing one. `IsExceptionParameterType` bound types in
  a single-file compilation without implicit usings, so an `Exception` parameter written without
  `using System;` was silently misread as a message placeholder — meaning the `HasExceptionParameter`
  support-safety assertions were fragile. An exact-spelling fallback (`Exception`/`System.Exception`
  only, so `FakeException` still fails to qualify) now applies when the symbol fails to bind.
  (b) Two pre-existing tests needed fixture updates, not behaviour changes: `ShortcutServiceTests` and
  `BadgeCountServiceTests` use `Substitute.For<ILogger<T>>()`, whose `IsEnabled` defaults to `false`;
  they only ever passed because `LoggerExtensions.LogInformation` performs no enabled check. Any move to
  `[LoggerMessage]` breaks them, so `IsEnabled(Information)` is now stubbed alongside the existing
  `IsEnabled(Warning)` stub.

  Open item carried to review: `ETagCacheService` is `public sealed` and now also implements
  `IDisposable`. This is additive (source- and binary-compatible), the type appears in **no**
  `PublicAPI*.Shipped.txt` baseline — Shell's baseline is the focused FC-TBL one covering
  `Components.DataGrid` — and the package-boundary guards pass, so no baseline was updated. The real
  behavioural consequence is that the DI container, where the service is registered `Scoped`, now
  disposes it at circuit teardown; `ObjectDisposedException` guards were added on the seed-gate
  acquire/release so a best-effort LRU seed racing teardown degrades to "unseeded" rather than throwing.

  Deferred inconsistency (not a finding, flagged for review): four `.razor` **markup** sites still pass
  `CurrentUICulture` as an `IFormatProvider` (`FcColumnPrioritizer.razor:35`, `FcHomeCard.razor:21`,
  `FcHomeDirectory.razor:59`, `FcCustomizationDiagnosticPanel.razor:77`). They are not analyzer findings
  at Recommended, so fixing them is outside this story's diagnostic-bounded scope, but they now differ
  from their `.razor.cs` counterparts.

- 2026-08-08: **Adversarial review round complete.** Three independent context-free review layers (blind
  hunter, edge-case hunter, verification-gap) ran against the full story diff. Triage found **no
  `intent_gap` and no `bad_spec`**, so no loopback was required: every finding was an implementation-level
  gap rather than a defect in the captured intent. **12 findings were applied as patches; 7 were deferred**
  to `deferred-work.md`; the remainder were rejected as noise.

  Applied patches of substance:
  - The render-tree rewriter now **fails closed** rather than merely failing safe. It bails out on
    `#else`/`#elif`/non-`DEBUG` `#if` regions (a counter referenced only in a disabled branch could
    otherwise have had its declaration deleted), returns the source unchanged when the parse carries error
    diagnostics, and asserts non-overlapping edit spans.
  - All four emitters now call `AssignLiteralsOrFail`, which re-scans rewritten output for a surviving
    runtime sequence argument and fails generation naming the exact call site. This closes a real
    interaction the implementation had opened: the ASP0006 pragma was removed **unconditionally**, so had
    the rewriter ever taken its fail-safe path, a `TreatWarningsAsErrors` consumer would have broken with
    no control covering it. Re-emitting the pragma was attempted first and rejected — the governance
    control-parity scanner counts an emitted pragma as an `emitter-pragma` control and the approved ledger
    records zero, so it turned `AnalyzerPolicy_GovernanceContract_FailsClosed` red.
  - Role-specific projection bodies (`ProjectionRoleBodyEmitter`, ~51 sequence emissions) had been covered
    only by whole-text Verify snapshots that an author re-approves. They now carry literal-sequencing
    assertions. Verified beforehand that role output **is** already literal, so this closed a coverage gap
    rather than a defect.
  - `GeneratedLogMethodEmitter` gained validation for the `LoggerMessage.Define` six-argument ceiling and
    template/parameter placeholder parity; it previously had no guard and no direct test.
  - The disabled-path allocation proof was moved to `[Trait("Category","Performance")]` and given a bounded
    budget instead of an exact-zero assertion — `domain-ci` runs unit-test projects **unfiltered**, so an
    exact-zero delta over 10,000 iterations would have been flaky on shared CI hardware.
  - Log bounding now digests U+2028, U+2029 and Unicode format characters, which `char.IsControl` misses,
    closing a line-forging vector on adopter-supplied values.
  - The `.razor` markup culture sweep was finished, so no `CurrentUICulture` remains as an `IFormatProvider`
    in the Shell; `ETagCacheService` gained the dispose tests its new `IDisposable` contract lacked; and the
    culture-mutating MCP test was serialised into a non-parallel collection.

  Post-review verification: canonical Release `.slnx` **0 warnings / 0 errors**; all seven product projects
  still **0/0** under strict `AnalysisMode=Recommended` with TWAE unchanged; both netstandard2.0 legs,
  SourceTools, and the three generated consumers still clean; default lane **4304 tests, 0 failed** (up from
  4274); Governance 218/0; Contract 3/0; story-artifact validation passes.

  Two corrections to this session's own record, stated rather than smoothed over:
  (a) An intermediate duplicate-sequence-literal scan reported 71 builders with repeated literals, which
  would have been a serious Blazor diffing defect. **It was a false alarm** — the scan grouped by builder
  *identifier name* across whole files, but each generated `RenderFragment` lambda declares its own counter,
  so literals correctly restart per fragment and within-scope uniqueness holds by construction.
  (b) Repairing this session's own whole-file JSON reflow of the ledger initially reverted it to `HEAD`,
  which silently discarded the session-2 ASP0006 control amendments and turned
  `AnalyzerPolicy_GovernanceContract_FailsClosed` red. The exact prior blob was recovered from the object
  store and the seal re-applied surgically; the ledger diff is now 17 insertions / 2 deletions instead of
  68 / 86, so the sealed-contract amendment is auditable.

### Completion Notes List

- Ultimate context engine analysis completed - comprehensive developer guide created.
- Administrator's direct request records this phase's separate approval without waiving Story 11.20.
- Added exact product and generated diagnostic matrices, 11.18 logging handoff, hidden ASP0006 scope,
  current UPDATE surfaces, semantic preservation rules, and a scoped validation strategy that does
  not falsely claim Story 11.22's test/sample debt.
- Story status is ready-for-dev for context tracking; implementation is explicitly blocked until the
  Story 11.20 approved ledger exists and passes Governance.
- 2026-08-08: **Implementation complete.** Owned product findings 275 -> 0 and owned generated findings
  503 -> 0. All seven product projects build Release net10.0 at `AnalysisMode=Recommended` with
  `TreatWarningsAsErrors` unchanged at 0 warnings / 0 errors; both netstandard2.0 compatibility legs and
  the SourceTools Roslyn component are clean; the three generated consumers pass the strict candidate
  gate; and the canonical Release `.slnx` build is 0 warnings / 0 errors.
- Every implementing slice was dispatched to a subagent and then **independently re-verified by the
  orchestrator** before acceptance — census re-measured, emitted trees inspected directly, negative
  controls re-run, and every claimed count reproduced. One agent-reported figure did not survive
  re-verification unchanged and was corrected in place: session 1's per-project product split had
  drifted (Contracts 30 -> 28, Testing 1 -> 0, Shell CA1873 80 -> 83), so the HEAD measurement recorded
  here supersedes it.
- The story is honest about what it does not own: 72 hand-authored findings remain in Shell.Tests source
  plus 17 hand-authored ASP0006 fixture sites, all explicitly owned by Story 11.22 and ledgered rather
  than absorbed. No central `AnalysisMode` was introduced; Story 11.23 still owns that activation gate.
- Two items are carried into review rather than decided unilaterally: the additive `IDisposable` on the
  `public sealed ETagCacheService` (non-breaking, confirmed by zero ApiCompat codes, but newly disposed
  by the DI container at circuit teardown), and four `.razor` markup sites that still pass
  `CurrentUICulture` as an `IFormatProvider` and are not analyzer findings at Recommended.

### File List

Complete enumeration of every path this story changed, measured as the staged diff against the story
implementation baseline `4a8cfa4926b8fc52850da70f811103a91df22dfc` (which spans both the session-1 work merged as PR #82 and the
session-2 implementation and review-hardening work on `fix/11-21-analyzer-burndown-2`).

Total: **142 paths**.

**Shell product source** (55)

- `src/Hexalith.FrontComposer.Shell/Badges/BadgeCountService.cs`
- `src/Hexalith.FrontComposer.Shell/Badges/ReflectionActionQueueProjectionCatalog.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Badges/FcDesaturatedBadge.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcColumnPrioritizer.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterEmptyState.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterResetButton.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcFilterSummary.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcMaxItemsCapNotice.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/DataGrid/FcSlowQueryNotice.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Diagnostics/FcCustomizationDiagnosticPanel.razor`
- `src/Hexalith.FrontComposer.Shell/Components/EventStore/FcPendingCommandSummary.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Forms/FcFormAbandonmentGuard.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeCard.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor`
- `src/Hexalith.FrontComposer.Shell/Components/Home/FcHomeDirectory.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcCommandPalette.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcDensityAnnouncer.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FrontComposerNavigation.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Lifecycle/FcLifecycleWrapper.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcAuthorizedCommandRegion.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionEmptyPlaceholder.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Components/Rendering/FcProjectionSubtitle.razor.cs`
- `src/Hexalith.FrontComposer.Shell/Extensions/AddFrontComposerDevModeExtensions.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreOptionsValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/EventStoreQueryClient.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/EventStore/ProjectionSubscriptionService.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerDiagnosticLog.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerHotPathLog.cs`
- `src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerSecurityLog.cs`
- `src/Hexalith.FrontComposer.Shell/Registration/FrontComposerRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Auth/FrontComposerAuthenticationOptionsValidator.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Customization/CustomizationContractValidationGate.cs`
- `src/Hexalith.FrontComposer.Shell/Services/DevMode/ClipboardJSModule.cs`
- `src/Hexalith.FrontComposer.Shell/Services/DevMode/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.Shell/Services/FrontComposerStorageKey.cs`
- `src/Hexalith.FrontComposer.Shell/Services/InlinePopoverRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/Lifecycle/LifecycleStateService.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionSlots/ProjectionSlotRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionTemplates/ProjectionTemplateRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Services/ProjectionViewOverrides/ProjectionViewOverrideRegistry.cs`
- `src/Hexalith.FrontComposer.Shell/Shortcuts/ShortcutService.cs`
- `src/Hexalith.FrontComposer.Shell/State/CapabilityDiscovery/CapabilityDiscoveryEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/CommandPalette/CommandPaletteEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/DataGridNavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/DataGridNavigation/LoadedPageReducers.cs`
- `src/Hexalith.FrontComposer.Shell/State/Density/DensityEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/NavigationEffects.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/ScopeReadinessGate.cs`
- `src/Hexalith.FrontComposer.Shell/State/Navigation/SessionRouteHelper.cs`
- `src/Hexalith.FrontComposer.Shell/State/PendingCommands/NewItemIndicatorStateService.cs`
- `src/Hexalith.FrontComposer.Shell/State/ProjectionConnection/ProjectionConnectionStateService.cs`
- `src/Hexalith.FrontComposer.Shell/State/ReconnectionReconciliation/ReconnectionReconciliationCoordinator.cs`
- `src/Hexalith.FrontComposer.Shell/State/Theme/ThemeEffects.cs`

**Other product source (Contracts, Contracts.UI, Mcp, Schema, Cli, Testing)** (24)

- `src/Hexalith.FrontComposer.Cli/SubmoduleBoundaryReader.cs`
- `src/Hexalith.FrontComposer.Contracts.UI/Shortcuts/ShortcutBinding.cs`
- `src/Hexalith.FrontComposer.Contracts/Attributes/ProjectionTemplateAttribute.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/CommandServiceExtensions.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequest.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryRequestJsonConverter.cs`
- `src/Hexalith.FrontComposer.Contracts/Communication/QueryResult.cs`
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/CustomizationDiagnosticFormatter.cs`
- `src/Hexalith.FrontComposer.Contracts/Registration/FrontComposerRegistryExtensions.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ProjectionSlotSelector.cs`
- `src/Hexalith.FrontComposer.Contracts/Rendering/ReturnPathValidator.cs`
- `src/Hexalith.FrontComposer.Contracts/Schema/SchemaFingerprintContracts.cs`
- `src/Hexalith.FrontComposer.Mcp/Extensions/FrontComposerMcpServiceCollectionExtensions.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpDescriptorRegistry.cs`
- `src/Hexalith.FrontComposer.Mcp/FrontComposerMcpLog.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpLifecycleStore.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReadSnapshot.cs`
- `src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReader.cs`
- `src/Hexalith.FrontComposer.Mcp/Rendering/McpMarkdownProjectionRenderer.cs`
- `src/Hexalith.FrontComposer.Mcp/Schema/InMemorySchemaBaselineProvider.cs`
- `src/Hexalith.FrontComposer.Mcp/Skills/SkillCorpusAggregateManifestBuilder.cs`
- `src/Hexalith.FrontComposer.Schema/Diagnostics/SchemaMigrationDeltaAnalyzer.cs`
- `src/Hexalith.FrontComposer.Testing/CommandTestDataBuilder.cs`
- `src/Hexalith.FrontComposer.Testing/ProjectionTestDataBuilder.cs`

**SourceTools emitters** (6)

- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandPageEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs`
- `src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs`

**Test projects** (23)

- `tests/Hexalith.FrontComposer.Contracts.Tests/Schema/CanonicalSchemaMaterialFingerprintVectorTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Invocation/McpLifecycleStoreDisposalTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpCultureTestGroup.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/SkillCorpusAggregateManifestRenderTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Badges/BadgeCountServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Infrastructure/Telemetry/FrontComposerDiagnosticLogTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Shortcuts/ShortcutServiceTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/State/ETagCache/ETagCacheServiceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/SchemaMigrationDeltaPathTruncationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/GeneratedLogMethodEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterBadgeColumnTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterExpandInRowTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterVirtualizationTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RenderTreeSequenceRewriterTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/GeneratedRenderTreeText.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj`

**Re-approved Verify snapshots** (24)

- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_DerivableFieldsHidden_OmitsHiddenFieldsOnly.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandFormEmitterTests.CommandForm_ShowFieldsOnly_RendersOnlyNamedFields.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Page_FiveFields_FullPageBoundarySnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FiveFields_FullPageBoundarySnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_FourFields_CompactInlineBoundarySnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_InlinePopoverSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithIconAttributeSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_OneField_WithoutIconUsesDefaultSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_TwoFields_CompactInlineSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/CommandRendererEmitterTests.Renderer_ZeroFields_InlineSnapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.BasicProjection_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DescriptionWithEscapeEdgeCases_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.DisplayNameOverrides_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.EnumAndBadgeMappings_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.GuidTruncation_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RazorEmitterTests.NullableProperties_Snapshot.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueNoEnumProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.ActionQueueProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DashboardWrongShapeProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.DetailRecordProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.StatusOverviewProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.TimelineProjection_Approval.verified.txt`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.WhenStateTypoProjection_Approval.verified.txt`

**Sample consumer projects (ASP0006 control removal)** (5)

- `samples/Counter/Counter.Domain/Counter.Domain.csproj`
- `samples/Counter/Counter.Specimens.Domain/Counter.Specimens.Domain.csproj`
- `samples/Counter/Counter.Specimens/Counter.Specimens.csproj`
- `samples/Counter/Counter.Web/Counter.Web.csproj`
- `samples/IdeParityCounter/IdeParityCounter.csproj`

**Story artifacts, ledger and evidence** (5)

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/implementation-artifacts/11-21-recommended-analyzer-product-and-generator-burndown.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`

Named exception — a path this story deleted, so it has no extant changed-file entry:

- `Testing/Builders.cs` — named exception: story shorthand for the deleted
  `src/Hexalith.FrontComposer.Testing/Builders.cs`, which the approved entry-gate CA1000 amendment
  split into `ProjectionTestDataBuilder.cs` and `CommandTestDataBuilder.cs` (git records it as a
  rename with 58% similarity).

Documented unrelated changes, not owned by this story:

- `references/Hexalith.Builds` (gitlink `3ac63338` -> `345e0cec`), `references/Hexalith.EventStore`,
  `references/Hexalith.Parties` and `references/Hexalith.Tenants` — submodule gitlinks advanced by the
  already-merged PR #82. This session never touched `references/`; `git diff` against the branch base
  `6388d5a5` is empty for that path. They fall inside the validation range only because it spans that
  merge. The Governance lane, including the shared-catalog `RunDependencyGraphValidate` compatibility
  checks, passes at this HEAD.

## Change Log

- 2026-07-16: Materialized approved staged-activation Phase 2 from Story 11.19d as a separately gated
  backlog specification.
- 2026-07-17: Administrator supplied separate phase approval; create-story enriched the complete
  product/generator implementation guide, retained the hard 11.20 dependency gate, and promoted the
  story context from backlog to ready-for-dev.
- 2026-08-08: Implementation session 2 completed the product and generator burn-down on branch
  `fix/11-21-analyzer-burndown-2` from baseline `6388d5a5`: generated non-logging findings and
  11.21-owned ASP0006 debt cleared via emit-time literal render-tree sequencing, Shell 217 -> 0,
  the remaining five product packages 58 -> 0, the analyzer ledger sealed with completion evidence,
  and the identifier inventory re-sealed. Status moved in-progress -> review.

## Suggested Review Order

**Generated render-tree sequencing — the highest-risk change**

- Entry point: emit-time literal allocation replacing every runtime seq++ counter.
  [`RenderTreeSequenceRewriter.cs:84`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs#L84)
- Fails closed: generation aborts if any runtime counter survives rewriting.
  [`RenderTreeSequenceRewriter.cs:153`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RenderTreeSequenceRewriter.cs#L153)
- Emitted ASP0006 pragma removed; guarded call is what makes that safe.
  [`CommandFormEmitter.cs:425`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandFormEmitter.cs#L425)
- Same guard on the renderer, whose consumers also lost their NoWarn.
  [`CommandRendererEmitter.cs:630`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/CommandRendererEmitter.cs#L630)
- Conservative static-emission check; wrong guess breaks adopter builds with CS0120.
  [`RazorEmitter.cs:505`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/RazorEmitter.cs#L505)

**Shell logging migration — the 73-site remainder**

- New source-generated family at EventIds 6000-6072, disjoint from every 11.18 band.
  [`FrontComposerDiagnosticLog.cs:46`](../../src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerDiagnosticLog.cs#L46)
- Line-forging guard: digests U+2028/U+2029 and format chars, not just control chars.
  [`FrontComposerDiagnosticLog.cs:1684`](../../src/Hexalith.FrontComposer.Shell/Infrastructure/Telemetry/FrontComposerDiagnosticLog.cs#L1684)
- Remainder assertion 73 -> 0; the three 11.18 range pins are untouched.
  [`SecurityLoggingGovernanceTests.cs:327`](../../tests/Hexalith.FrontComposer.Shell.Tests/Architecture/SecurityLoggingGovernanceTests.cs#L327)

**Disposal and lifetime changes**

- CA1001 fix makes a public sealed type IDisposable — now disposed at circuit teardown.
  [`ETagCacheService.cs:73`](../../src/Hexalith.FrontComposer.Shell/State/ETagCache/ETagCacheService.cs#L73)

**Emitter safety guards added during review**

- Enforces the LoggerMessage.Define six-argument ceiling and placeholder parity.
  [`GeneratedLogMethodEmitter.cs:55`](../../src/Hexalith.FrontComposer.SourceTools/Emitters/GeneratedLogMethodEmitter.cs#L55)
- CA1068 reorder is safe only because this record is internal, not public API.
  [`FrontComposerMcpProjectionReadSnapshot.cs:5`](../../src/Hexalith.FrontComposer.Mcp/Invocation/FrontComposerMcpProjectionReadSnapshot.cs#L5)

**Verification that the burn-down cannot silently regress**

- The only end-to-end proof: packaged consumer at Recommended, TWAE on, no ASP0006 control.
  [`PackagedAnalyzerConsumerTests.cs:72`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Integration/PackagedAnalyzerConsumerTests.cs#L72)
- Closes the role-body gap that only re-approved snapshots were watching.
  [`RoleSpecificProjectionApprovalTests.cs:257`](../../tests/Hexalith.FrontComposer.SourceTools.Tests/Emitters/RoleSpecificProjections/RoleSpecificProjectionApprovalTests.cs#L257)

**Peripherals**

- Identifier seal and completion evidence; diff is deliberately surgical.
  [`analyzer-policy-exception-ledger-v1.json:76`](../../_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json#L76)
- Seven review findings deferred with evidence, including the digest join-key question.
  [`deferred-work.md:1`](../../_bmad-output/implementation-artifacts/deferred-work.md#L1)
