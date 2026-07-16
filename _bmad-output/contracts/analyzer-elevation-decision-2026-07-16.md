---
contract: FC-ANALYZERS-RECOMMENDED
version: 1.0
status: approved-staged-activation
decision_date: 2026-07-16
target_mode: Recommended
activation_strategy: staged
owners:
  - Architect
  - Product Owner
implementation_owner: Framework Maintainer
release_gate: v1.0
source_story: _bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md
---

# FC-ANALYZERS-RECOMMENDED Decision Contract

## Decision and sign-off

FrontComposer adopts the .NET SDK built-in analyzer mode `Recommended` as its target posture through
a staged activation. Immediate repository-wide activation is rejected: the clean current Release
baseline becomes 4,070 diagnostics when the candidate mode is fully enumerated. Staging keeps the
canonical `TreatWarningsAsErrors=true` policy intact while each bounded scope reaches zero candidate
warnings.

**Approved by:** Administrator, acting as Architecture and Product authority for Story 11.19d.

**Signed-off:** 2026-07-16.

The decision adds no analyzer package. FrontComposer continues to use only the analyzers shipped by
the pinned .NET SDK/Roslyn toolchain. It does not enable `AnalysisMode`, change severity, suppress a
new diagnostic, or clean up source in Story 11.19d.

## Evidence boundary

| Item | Value |
| --- | --- |
| Repository baseline | `d9c19a4fb837357af10f6f1aa630232f670557c4` |
| SDK | .NET SDK `10.0.302` |
| MSBuild | `18.6.4` |
| Compiler/analyzer package | Roslyn `5.6.0`; SDK built-in .NET analyzers |
| Configuration | `Release`, normal centralized NuGet dependency mode |
| Current build | 0 warnings, 0 errors |
| Candidate strict build | Failed after 120 promoted diagnostics when the Contracts dependency stopped downstream enumeration |
| Candidate census build | 4,070 unique diagnostics with command-line-only `TreatWarningsAsErrors=false` for enumeration |
| Candidate affected legs | 19 project/TFM legs; 4 Release legs remain at zero |
| Generated-code findings | 503; included because the effective candidate build reports diagnostics from SourceTools output under `obj/` |
| Regression baseline | 4,150/4,150 default-lane tests pass after restoring the current analyzer posture |

The candidate census temporarily lowers warnings-as-errors only on the command line to allow complete
enumeration. This is diagnostic instrumentation, not a policy change. The strict candidate binary log
captures the actual fail-fast behavior under unchanged NFR-1.

Raw machine-readable evidence:

- `_bmad-output/contracts/analyzer-elevation-current-2026-07-16.binlog.gz`
- `_bmad-output/contracts/analyzer-elevation-recommended-strict-2026-07-16.binlog.gz`
- `_bmad-output/contracts/analyzer-elevation-recommended-census-2026-07-16.binlog.gz`

To inspect an evidence file, decompress it and open the `.binlog` with an MSBuild binary-log reader.

## Effective current configuration

The repository declares no `AnalysisMode`. Normal net10.0 projects resolve `AnalysisLevel=latest`,
`EnableNETAnalyzers=true`, `EnforceCodeStyleInBuild=false`, and `TreatWarningsAsErrors=true`.
`Contracts` and `Schema` disable .NET analyzers on their netstandard2.0 legs; SourceTools also disables
them on netstandard2.0 to preserve compiler-host compatibility. The benchmark project is the existing
project-local exception with `TreatWarningsAsErrors=false`; this decision does not change it.

The root `.editorconfig` explicitly sets CA1062, CA1822, and CA2007 to warning and CA1014 to none.
CS1591 is none outside the four approved Contracts API-freeze globs, where it is warning. Individual
rule settings and the Story 11.19a documentation boundary remain in force; no bulk category severity
is declared.

Effective warning controls include:

| Scope | IDs | Target disposition |
| --- | --- | --- |
| All source projects | 0419, 1570, 1572, 1573, 1574, 1734 | Preserve during analyzer burn-down; documentation/compiler policy is owned separately by Story 11.19a. Revisit only through a scoped policy story. |
| All projects | NU1605 as warning-as-error; net10.0 also SYSLIB0011 | Preserve. These are dependency/runtime compatibility gates, not Recommended-mode debt. |
| Contracts.UI and Shell | NU5104 | Preserve pending package-validation policy; not a code-analysis suppression. |
| Testing | NU5104, NU5128 | Preserve pending package-validation policy; not a code-analysis suppression. |
| Samples and selected tests | ASP0006 | Audit in the policy phase; retain only for intentional render-sequence fixtures. |
| Counter.Domain, Counter.Specimens.Domain, IdeParityCounter | HFC1002 | Audit in the policy phase; retain only for intentional generator specimens. |
| Shell.Tests and SourceTools.Tests | CA2255 | Retain narrowly for intentional module-initializer test infrastructure. |
| Testing.Tests | CA2007 | Audit against asynchronous test semantics; no repository-wide suppression is permitted. |
| Shell.Tests | IDE1006, IDE0058, CS1656 | Retain only on negative/fixture code that intentionally violates the corresponding rule. |
| Root `.editorconfig` | CA1014 | Preserve as the current explicit assembly-policy decision until a dedicated compatibility review changes it. |
| Root `.editorconfig` | CS1591 outside API-freeze globs | Preserve the approved Story 11.19a scope. |

SDK-inherited 1701/1702 entries appear in effective sample/test `NoWarn` values; they are not
repository-authored Recommended-mode suppressions and are not expanded by this decision.

## Candidate census

### By category

| Category | Count | Share |
| --- | ---: | ---: |
| Naming | 2,958 | 72.7% |
| Performance | 772 | 19.0% |
| Globalization | 228 | 5.6% |
| Usage | 49 | 1.2% |
| Maintainability | 46 | 1.1% |
| Reliability | 12 | 0.3% |
| Design | 5 | 0.1% |
| **Total** | **4,070** | **100.0%** |

### By diagnostic ID

| ID | Category | Count |
| --- | --- | ---: |
| CA1000 | Design | 3 |
| CA1001 | Design | 1 |
| CA1068 | Design | 1 |
| CA1305 | Globalization | 226 |
| CA1310 | Globalization | 2 |
| CA1507 | Maintainability | 13 |
| CA1510 | Maintainability | 24 |
| CA1512 | Maintainability | 2 |
| CA1513 | Maintainability | 7 |
| CA1707 | Naming | 2,956 |
| CA1711 | Naming | 2 |
| CA1816 | Usage | 37 |
| CA1822 | Performance | 24 |
| CA1826 | Performance | 7 |
| CA1827 | Performance | 1 |
| CA1834 | Performance | 5 |
| CA1845 | Performance | 8 |
| CA1848 | Performance | 353 |
| CA1850 | Performance | 1 |
| CA1854 | Performance | 1 |
| CA1859 | Performance | 88 |
| CA1861 | Performance | 41 |
| CA1863 | Performance | 4 |
| CA1865 | Performance | 11 |
| CA1869 | Performance | 6 |
| CA1870 | Performance | 1 |
| CA1873 | Performance | 213 |
| CA1875 | Performance | 8 |
| CA2012 | Reliability | 12 |
| CA2201 | Usage | 4 |
| CA2249 | Usage | 5 |
| CA2263 | Usage | 3 |
| **Total** |  | **4,070** |

### By Release project/TFM

| Project/TFM | Count |
| --- | ---: |
| Hexalith.FrontComposer.Shell.Tests | 2,022 |
| Hexalith.FrontComposer.SourceTools.Tests | 892 |
| Hexalith.FrontComposer.Mcp.Tests | 305 |
| Hexalith.FrontComposer.Shell | 217 |
| Hexalith.FrontComposer.Contracts (net10.0) | 119 |
| Hexalith.FrontComposer.Contracts.Tests | 116 |
| Counter.Specimens.Domain | 94 |
| Counter.Domain | 79 |
| Hexalith.FrontComposer.Cli.Tests | 66 |
| Hexalith.FrontComposer.Testing.Tests | 64 |
| IdeParityCounter | 28 |
| Hexalith.FrontComposer.Shell.Tests.Bench | 25 |
| Hexalith.FrontComposer.Mcp | 24 |
| Hexalith.FrontComposer.Contracts.UI.Tests | 10 |
| Hexalith.FrontComposer.Schema (net10.0) | 3 |
| Counter.Web | 2 |
| Hexalith.FrontComposer.Contracts.UI | 2 |
| Hexalith.FrontComposer.Cli | 1 |
| Hexalith.FrontComposer.Testing | 1 |
| Counter.Specimens | 0 |
| Hexalith.FrontComposer.Contracts (netstandard2.0) | 0 |
| Hexalith.FrontComposer.Schema (netstandard2.0) | 0 |
| Hexalith.FrontComposer.SourceTools (netstandard2.0) | 0 |
| **Total** | **4,070** |

Tests account for 3,500 findings, samples for 203, and product source for 367. Generated SourceTools
output accounts for 503 findings: Performance 461, Usage 30, and Maintainability 12. The generated
subset is concentrated in Counter.Domain (79), Counter.Specimens.Domain (94), IdeParityCounter (28),
and Shell.Tests generated specimens (302).

No new compiler, FrontComposer source-generator, package-audit, or executable Governance-test
diagnostic is counted in the 4,070 delta. The current build is clean; the delta consists of SDK
built-in CA diagnostics enabled by the candidate mode. Governance remains a separately executed test
lane, and package audit was disabled exactly as prescribed for the local comparison.

## Burn-down and staged activation

The 4,070 findings are not one defect class. In particular, CA1707 conflicts with the repository's
required underscore-separated three-part test names and with existing public `FcDiagnosticIds`
constant names. Mechanical renaming would violate established test and public-compatibility contracts.
The policy phase must therefore distinguish narrow justified exceptions from genuine fixes.

| Phase | Follow-up | Scope and baseline | Exit criterion | Owner | Due date | Release gate |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Story 11.20 | Naming/policy audit: 2,958 Naming findings plus every existing suppression. Freeze an approved per-path/per-symbol exception ledger; no global CA disable. Estimate 3-5 engineer-days. | Every exception is narrow, documented, owner-bound, and test-backed; actionable Naming findings are zero in the scoped candidate lane. | Architect + Framework Maintainer | 2026-07-24 | Blocks later activation phases. |
| 2 | Story 11.21 | Product and generator-emission defects: 367 product-source findings, generated templates, and the non-test share of logging/performance/globalization/usage debt. Estimate 8-12 engineer-days. | Product projects and generated sample consumers build with Recommended, unchanged TWAE, and zero warnings under the approved Phase-1 ledger. | Framework Maintainer + SourceTools Maintainer | 2026-08-14 | Blocks v1.0 release candidate. |
| 3 | Story 11.22 | Tests and samples: 3,500 test findings and 203 sample findings, excluding only Phase-1-approved narrow exceptions. Estimate 8-12 engineer-days. | Default/Governance/Contract lanes pass; all test/sample projects have zero actionable Recommended findings. | Test Architect + Framework Maintainer | 2026-09-04 | Blocks v1.0 release candidate. |
| 4 | Story 11.23 | Repository-wide activation and governance. Estimate 2-3 engineer-days. | Commit `AnalysisMode=Recommended`; full forced Release build is 0 warnings/0 errors with TWAE unchanged; regression, Governance, Contract, docs, and artifact lanes pass. | Architect + Framework Maintainer + Release Owner | 2026-09-11 | Required before v1.0 publication authorization. |

Total estimated effort is 21-32 engineer-days before contingency. Each follow-up is independently
reviewed and approved; no phase is an authorization to fix unrelated code.

## Validation lanes

Every phase must run:

1. The normal forced Release solution build with 0 warnings and 0 errors.
2. A forced candidate build for the owned project/category scope with `AnalysisMode=Recommended` and
   unchanged `TreatWarningsAsErrors=true`.
3. The focused project tests for changed packages.
4. The default solution test lane with `DiffEngine_Disabled=true`.
5. Governance/Contract lanes when configuration, public API, analyzer output, or generated output is
   touched.
6. Story/document artifact validation and changed-file/File-List reconciliation.

Generated-output fixes must be made in SourceTools emitters or annotated source, never in `obj/`.
Public API, Verify, Pact, and generated-output baselines may change only when the owning follow-up
acceptance criteria explicitly authorize them.

## Release impact

The current Release build remains authoritative until Phase 4. Story 11.19d therefore has no product
or package release impact by itself. However, `Recommended` activation is a v1.0 readiness gate: the
Release Owner must not classify the v1.0 candidate as publishable until Phase 4 is complete, or until
Architecture and Product record a dated replacement decision with equivalent diagnostic ownership.

The decision does not alter semantic-release behavior and does not authorize release-workflow edits.

## Rollback and escalation

Before Phase 4, rollback means removing only a follow-up's command-line/scoped candidate gate; the
normal build posture remains unchanged. After Phase 4, an emergency rollback requires a separately
approved build-policy change. It must never lower `TreatWarningsAsErrors`, add a third-party analyzer,
globally disable a CA category, or conceal new findings through blanket `NoWarn`.

Escalate to Architecture, Product, and the Release Owner when any of these occurs:

- a phase misses its due date;
- the candidate baseline grows by more than 5% within an owned scope;
- a proposed fix changes public API, wire/schema identity, generated-output contracts, or package
  compatibility outside that follow-up's acceptance criteria;
- a diagnostic cannot be fixed without a broad or global suppression;
- a narrow exception cannot name an owner, rationale, review date, and removal/revalidation trigger.

The escalation outcome must split or re-scope the affected phase and update sprint status. “Too many
warnings” is not an accepted reason to defer the target without a replacement schedule.

## References

- `_bmad-output/implementation-artifacts/11-19-analyzer-elevation-decision.md`
- `_bmad-output/planning-artifacts/prd.md` — NFR-1 and v1.0 release gates.
- `_bmad-output/project-docs/architecture-quality-review-2026-07-04.md` — finding M17.
- https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#analysis-mode
