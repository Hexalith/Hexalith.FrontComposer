---
title: 'Story 11.23 Recommended analyzer repository activation'
type: 'refactor'
created: '2026-08-08'
status: 'done'
review_loop_iteration: 0
baseline_commit: '490447e3be5142c3793019fa5491c19b8910f899'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/11-23-recommended-analyzer-repository-activation.md'
  - '{project-root}/_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Stories 11.20–11.22 cleared Recommended debt to zero, but central `AnalysisMode` is still absent, so Recommended is only a CLI posture and cannot gate v1.0.

**Approach:** Stamp a fresh zero-finding census, declare `AnalysisMode=Recommended` in root `Directory.Build.props`, reconcile the Bench TWAE exception with a true zero-warning Release gate, flip Governance to prove central Recommended, and record Release Owner evidence.

## Boundaries & Constraints

**Always:** Spec approval is Story 11.23's Architecture/Product approval. Keep root `TreatWarningsAsErrors=true` and built-in SDK analyzers only. Preserve Contracts/Schema multi-TFM and SourceTools `netstandard2.0`/Roslyn-host boundaries. Prefer removing Bench `TreatWarningsAsErrors=false` after Recommended Release shows 0W/0E including Bench; update ledger + Governance to match. Count MSBuild warning/error totals — exit code alone is insufficient. Record activation census, toolchain stamps, and AnalysisMode-only rollback posture.

**Ask First:** Pre-activation census not zero; residuals when removing Bench TWAE=false; third-party analyzers; broad CA/`NoWarn` suppressions; API/schema/generated/Verify/Pact baseline changes; dependency/submodule bumps; lowering TWAE.

**Never:** CLI-only or project-local activation instead of root props; analyzer packages; weaken TWAE; repo/category/project-wide CA suppressions; `obj/**` edits; mass-accept baselines; hide diagnostics; start while 11.20–11.22 are incomplete or ledger fields are missing.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Pre-activation census | Forced Recommended before props edit | Zero actionable findings; ledger stamp | Halt on any finding |
| Central activation | Root `AnalysisMode=Recommended` | Owned TFMs evaluate Recommended; TWAE true | Halt if ns2.0/SourceTools breaks |
| Bench reconciliation | Bench under Recommended Release | Solution 0W/0E; override removed or retained with updated disposition | Ask First on residuals |
| Drift / rollback | Config drift or emergency revert | Governance fails closed; rollback may drop AnalysisMode only via separate approval | Never lower TWAE or blanket-suppress |

</frozen-after-approval>

## Code Map

- `Directory.Build.props` -- add `<AnalysisMode>Recommended</AnalysisMode>` beside `TreatWarningsAsErrors` (line 32).
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj` -- reconcile lines 8–11 TWAE=false with zero-warning Release.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs` -- flip absence checks (~1415–1418, ~1555) to central Recommended; update Bench expectations (~1447–1454, ~1561–1569); keep built-in/TWAE/control seals; add activated build-parity proof.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- append 11.23 activation evidence; reconcile `benchmark-twae-exception` / `msbuild-benchmark-twae`; preserve 11.20–11.22 history.
- `src/Hexalith.FrontComposer.{Contracts,Schema,SourceTools}/*.csproj`, `src/Directory.Build.props`, `.editorconfig` -- read-only TFM/host, doc-NoWarn, and CA1707/policy seals.
- `_bmad-output/implementation-artifacts/11-23-recommended-analyzer-repository-activation.md`, `sprint-status.yaml`, `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md` -- status/approval/v1.0 trail; refresh current-facing “no AnalysisMode” notes only.

## Tasks & Acceptance

**Execution:**
- [x] Forced Recommended census at implementation commit; append zero-finding stamp before editing root props.
- [x] `Directory.Build.props` -- central `AnalysisMode=Recommended` beside unchanged TWAE=true.
- [x] Bench csproj + ledger -- reconcile TWAE exception so forced Release reports 0W/0E.
- [x] `AnalyzerPolicyGovernanceTests.cs` -- prove Recommended, built-in-only, TWAE, suppression bans, ledger parity, TFM boundaries, activated build parity.
- [x] Run default (`DiffEngine_Disabled=true`), Governance, Contract, package/PublicAPI/schema/generated-output, docs, and story-artifact lanes for touched surfaces.
- [x] Update story/sprint/Release Owner evidence; rollback remains AnalysisMode-only under separate approval.

**Acceptance Criteria:**
- Given 11.20–11.22 done and pre-activation census zero, when root props activate Recommended, then no analyzer packages, TWAE change, or global/category CA suppression is added.
- Given Contracts/Schema/SourceTools boundaries, when properties evaluate, then TFM/host rules hold with no net10-only analyzer dependency on SourceTools or netstandard2.0.
- Given Bench's former TWAE exception, when Release finalizes, then the forced solution summary shows 0W/0E.
- Given Governance after activation, when checks run, then Recommended, built-in-only, TWAE, suppression bans, ledger parity, and build parity fail closed on drift.
- Given touched surfaces, when validation runs, then lanes pass without unapproved baseline drift and sprint/release links v1.0 evidence.

## Spec Change Log

- 2026-08-08: Code-review patches — discrete activation evidence errors; exact census/Release command seals; Schema/SourceTools property probes; activated-vs-forced warning/error summary parity; AnalysisMode rollback comment; Bench fixed-disposition pointer; elevation-decision current posture; docs CA1707 snippet note.
- 2026-08-08: Activated the approved root `AnalysisMode=Recommended` posture after a fresh zero-finding
  census; removed the clean Bench warning-as-error exception and recorded the activation evidence.

## Verification

**Commands:**
- Pre-activation census: `dotnet build Hexalith.FrontComposer.slnx -c Release --no-restore --no-incremental -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=4.0.0 -p:AnalysisMode=Recommended -p:TreatWarningsAsErrors=false` -- 0 findings.
- Post-activation Release: same flags without AnalysisMode/TWAE overrides -- MSBuild `0 Warning(s)` / `0 Error(s)`.
- `dotnet msbuild <Contracts|Schema|SourceTools|Bench> -p:Configuration=Release -getProperty:AnalysisMode,TreatWarningsAsErrors,NoWarn -nologo` -- Recommended + reconciled TWAE; ns2.0 NoWarn unchanged.
- `DiffEngine_Disabled=true` default; Shell Governance/Contract traits; `pwsh ./eng/validate-contract-artifacts.ps1`; story-artifact validation; `git diff --check` -- green; no unapproved baseline drift.

**Results (2026-08-08):**

- Pre-activation forced Recommended census: 0 warnings / 0 errors.
- Activated Release solution build: 0 warnings / 0 errors.
- Contracts (net10.0 and netstandard2.0), Schema (netstandard2.0), SourceTools (netstandard2.0), and
  Bench effective MSBuild properties report `AnalysisMode=Recommended` and
  `TreatWarningsAsErrors=true`; the source TFM documentation `NoWarn` boundary remains unchanged.
- The Bench override was removed because the pre-activation census and activated Release build both report
  0 warnings / 0 errors; no Ask First residual appeared.
- Docs how-to snippet renamed for CA1707 inventory coherence under Recommended (`CommandServiceDispatchCapturesRedactedEvidence`).

## Dev Agent Record

### Completion Notes

- The root activation remains built-in SDK analyzers only. No analyzer package, broad CA suppression,
  root warning-policy weakening, dependency change, generated-output edit, or public/schema baseline
  update was made.
- Release Owner evidence is recorded in the canonical analyzer-policy ledger. Any emergency rollback
  requires separate approval and may remove only the root `AnalysisMode` declaration.
- Review patches hardened activation evidence (exact commands, typed counts, Schema/SourceTools probes,
  summary parity) and refreshed the elevation-decision current-configuration notes.

### File List

- `Directory.Build.props`
- `tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
- `docs/how-to/test-generated-components.md`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json`
- `_bmad-output/contracts/analyzer-elevation-decision-2026-07-16.md`
- `_bmad-output/implementation-artifacts/spec-11-23-recommended-analyzer-repository-activation.md`
- `_bmad-output/implementation-artifacts/11-23-recommended-analyzer-repository-activation.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Suggested Review Order

**Activation**

- Central Recommended declaration with AnalysisMode-only rollback comment
  [`Directory.Build.props:34`](../../Directory.Build.props#L34)

- Bench TWAE exception removed; pointer to fixed ledger disposition
  [`Hexalith.FrontComposer.Shell.Tests.Bench.csproj:8`](../../tests/Hexalith.FrontComposer.Shell.Tests.Bench/Hexalith.FrontComposer.Shell.Tests.Bench.csproj#L8)

**Governance seals**

- Activated Release vs forced Recommended summary parity
  [`AnalyzerPolicyGovernanceTests.cs:387`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L387)

- Discrete activation evidence with exact census/Release commands
  [`AnalyzerPolicyGovernanceTests.cs:918`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L918)

- Root AnalysisMode must be exactly Recommended
  [`AnalyzerPolicyGovernanceTests.cs:1555`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L1555)

- Schema/SourceTools/Bench effective Recommended + TWAE probes
  [`AnalyzerPolicyGovernanceTests.cs:1683`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L1683)

**Evidence & docs**

- Story 11.23 activation census and Release Owner rollback posture
  [`analyzer-policy-exception-ledger-v1.json:77`](../contracts/analyzer-policy-exception-ledger-v1.json#L77)

- Current-facing elevation decision updated for activation
  [`analyzer-elevation-decision-2026-07-16.md:79`](../contracts/analyzer-elevation-decision-2026-07-16.md#L79)

- How-to snippet renamed under Recommended naming analysis
  [`test-generated-components.md:75`](../../docs/how-to/test-generated-components.md#L75)
