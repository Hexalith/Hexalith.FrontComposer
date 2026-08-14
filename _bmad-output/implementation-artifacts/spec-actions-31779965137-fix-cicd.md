---
title: 'Confirm or reseal CA1707 inventory after Release 31779965137'
type: 'bugfix'
created: '2026-08-14'
status: 'done'
baseline_commit: '4ccd7727b5530a9d92e78fd745e122d1b29b19cd'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run [31779965137](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31779965137/job/94703531817) failed `prepare-candidate` on `main` SHA `d31679c1` because `AnalyzerPolicy_IdentifierInventory_MatchesSeal` reported test-scope CA1707 identifier-inventory drift (`count=6734`, `sha256=a89c894f45bf7a508a20bf964c98eee65d947eebd8c1c2cf98ad43b33badd2f7`). Current `main` (`4ccd7727`) already includes the Story 9.4 reseal (`6820` / `6c099739…`).

**Approach:** Reproduce the Fact on current `main`. If it is already green, leave the ledger unchanged and treat the failed release SHA as superseded. If it still drifts, reseal only the test inventory count and SHA-256 from the live Fact output.

## Boundaries & Constraints

**Always:**
- Treat this as closed-world seal refresh for underscore identifiers already exempted under `.editorconfig` (`CA1707=none` for `tests/**.cs`).
- Prefer no file change when the Fact is already empty on a clean tree.
- If resealing, update only `identifierInventory.testUnderscoreIdentifierTokens` and `identifierInventory.testInventorySha256` unless contracts-scope drift also appears.
- Paste live Fact values; do not invent a hash.

**Ask First:**
- Contracts inventory also drifts (`contractsUnderscoreIdentifierTokens` / `contractsInventorySha256`).
- Failure mode changes from inventory drift to a different governance assertion.
- Dispatching the production `Release` workflow (`workflow_dispatch`).

**Never:**
- Change `.editorconfig` CA1707 scopes, rename `FcDiagnosticIds`, widen suppressions, or lower `TreatWarningsAsErrors`.
- Rename underscore test methods to satisfy CA1707.
- Touch unrelated ledger blocks (`findings`, dispositions, census, activation/rollback).
- Dispatch `release.yml` without an explicit human request.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Already sealed | Clean `main`; Fact returns empty | No ledger edit | N/A |
| Test drift remains | Fact prints `test CA1707 scope identifier inventory drift: count=…, sha256=…` | Reseal those two test keys only | Re-run Fact until empty |
| Contracts still sealed | Contracts live count/hash match ledger | Leave contracts keys unchanged | N/A |
| Contracts drift appears | Second error names FcDiagnosticIds inventory | HALT | Ask before updating contracts keys |

</frozen-after-approval>

## Code Map

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` (`identifierInventory`, L98–104) -- sole writable seal. Current `main` values: test `6820` / `6c099739f35154f98154f37658a6711449a8ce7f92817eaea7967eedf4eebba1`; contracts `90` / `a06a2c10b7bcc9e2abd18b585af55e93d70bd3ad85a20519791590313ffcb3b7`. Failed release SHA `d31679c1` printed live `6734` / `a89c894f…` against a stale SHA at the same count.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
  - `AnalyzerPolicy_IdentifierInventory_MatchesSeal` (L333–338) -- Gate 2b / release `phase_tests` Fact; class-level `[Trait("Category", "Governance")]` is at L18
  - `ValidateIdentifierInventory` (L1634–1659) -- emits `test CA1707 scope identifier inventory drift: count=…, sha256=…`
  - `IdentifierInventory` (L1661–1678) -- Roslyn-scan tracked `.cs` for `_` identifier tokens; SHA-256 over sorted `path:line:token`
  - `LoadLedger` (L1866–1871) / `TrackedFiles` (L1943–1992) -- ledger JSON + `git ls-files`; untracked tests ignored
- `.editorconfig` (CA1707 `tests/**.cs` / `FcDiagnosticIds.cs`) -- read-only exemption scopes
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` -- contracts inventory root; read-only unless Ask First triggers
- Planning baseline: `_bmad-output/implementation-artifacts/spec-fix-ci-ca1707-identifier-inventory-seal.md` -- prior reseal procedure; same two writable keys

## Tasks & Acceptance

**Execution:**
- [x] Re-run `AnalyzerPolicy_IdentifierInventory_MatchesSeal` on a clean `main` tree and capture pass/fail plus any printed `count` / `sha256`
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- if the Fact is already empty, make no edit; if test-scope drift remains, set only the two test inventory keys to the live values
- [x] Re-run the same Fact until green; leave contracts keys untouched unless Ask First triggers

**Acceptance Criteria:**
- Given a clean FrontComposer working tree on current `main`, when `AnalyzerPolicy_IdentifierInventory_MatchesSeal` runs, then validation returns empty.
- Given the ledger after this work, when inspecting `identifierInventory`, then contracts keys are unchanged and no `.editorconfig` / `FcDiagnosticIds` / suppression edits exist.
- Given the failed release SHA `d31679c1` is behind current `main`, when this Fact is green, then no further code change is required for that job's inventory failure.

## Spec Change Log

- 2026-08-14: Reproduced `AnalyzerPolicy_IdentifierInventory_MatchesSeal` on clean `main` at `4ccd7727`. Fact already empty (Passed: 1, Failed: 0); no `identifier inventory drift` message. Left `identifierInventory` unchanged (test `6820` / `6c099739…`, contracts `90` / `a06a2c10…`). Failed release SHA `d31679c1` treated as superseded.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~AnalyzerPolicy_IdentifierInventory_MatchesSeal" --configuration Release` -- expected: Passed, 0 failures; no `identifier inventory drift` message

**Evidence:**
- Current `main` `4ccd7727`: Passed — Failed: 0, Passed: 1, Skipped: 0 (Release). No `identifier inventory drift` message. Ledger not edited.

**Manual checks:**
- Do not dispatch `release.yml` unless the human explicitly asks. The failed run targeted `d31679c1`; current `main` is `4ccd7727`.

## Suggested Review Order

- Local Fact already empty; no ledger edit on current `main`.
  [`AnalyzerPolicyGovernanceTests.cs:334`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L334)

- Test seal remains `6820` / `6c099739…`; contracts keys untouched.
  [`analyzer-policy-exception-ledger-v1.json:98`](../contracts/analyzer-policy-exception-ledger-v1.json#L98)

- Remaining tip Gate 2b and Release re-dispatch deferred.
  [`deferred-work.md:2386`](./deferred-work.md#L2386)
