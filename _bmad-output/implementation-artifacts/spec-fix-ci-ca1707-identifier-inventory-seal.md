---
title: 'Reseal CA1707 test identifier inventory after Gate 2b drift'
type: 'bugfix'
created: '2026-08-13'
status: 'done'
baseline_commit: '1df79479fcd6cbadefd75767c3a68058a17aa75d'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI run [31715693323](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31715693323) fails Gate 2b because `AnalyzerPolicy_IdentifierInventory_MatchesSeal` reports test-scope CA1707 identifier-inventory drift (`count=6734`, `sha256=6e3d396608344c5911259b0584a16a55fd1f34115bdb4e8773495dd5f8ade073`) against a stale ledger seal (`6610` / `b7da0f79…`).

**Approach:** Confirm live inventory on a clean tree, then reseal only the test inventory count and SHA-256 in the analyzer-policy exception ledger so the governance Fact matches the tracked `tests/**/*.cs` inventory. Leave the Windows Playwright FC-NIP failure deferred.

## Boundaries & Constraints

**Always:**
- Treat this as closed-world seal refresh for intentional test-side underscore identifiers already exempted via `.editorconfig` (`CA1707=none` under `tests/**.cs`).
- Update only `identifierInventory.testUnderscoreIdentifierTokens` and `identifierInventory.testInventorySha256` unless a contracts-scope drift error also appears when re-running the Fact.
- Re-run `AnalyzerPolicy_IdentifierInventory_MatchesSeal` on a clean working tree and require an empty validation result before marking done.
- Keep Conventional Commits / commitlint policy if committing later (human-requested).

**Ask First:**
- Live inventory differs from CI-printed `6734` / `6e3d3966…` after a clean checkout (unexpected extra local drift).
- Contracts inventory also drifts (`contractsUnderscoreIdentifierTokens` / `contractsInventorySha256`).
- Failure mode changes from inventory drift to a different governance assertion.

**Never:**
- Change `.editorconfig` CA1707 scopes, rename `FcDiagnosticIds`, or widen suppressions / lower `TreatWarningsAsErrors`.
- “Fix” by renaming underscore test methods to satisfy CA1707.
- Touch unrelated ledger blocks (`findings`, dispositions, census, activation/rollback).
- Implement the deferred Windows `PLAYWRIGHT_SKIP_WEBSERVER` npm/script fix in this change.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy path | Clean tree; live test inventory matches CI `6734` / `6e3d3966…` | Ledger test seal updated; Fact returns empty | N/A |
| Contracts still sealed | Contracts live hash/count match ledger | Leave contracts keys unchanged | N/A |
| Local inventory mismatch | Live count/hash ≠ CI values | HALT; do not invent a seal | Ask human before resealing |
| Contracts drift appears | ValidateIdentifierInventory also reports contracts drift | HALT | Ask before updating contracts keys |

</frozen-after-approval>

## Code Map

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` (`identifierInventory`, ~L98–104) -- sole writable seal; resealed test inventory to `6734` / `e19f9e81f2ddd869e773e5551244f634e192eece09b1b60ef5aa15e2621ff802` (was `6610` / `b7da0f795de7cef7d98cbee270554eb7f55aa9c1e6b8ea4c8e6caecddbbf4df1`); contracts seal unchanged (`90` / `a06a2c10b7bcc9e2abd18b585af55e93d70bd3ad85a20519791590313ffcb3b7`); CI tip `d355482` had printed `6734` / `6e3d396608344c5911259b0584a16a55fd1f34115bdb4e8773495dd5f8ade073`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
  - `AnalyzerPolicy_IdentifierInventory_MatchesSeal` (~L334–338) -- Gate 2b Fact used for red/green proof
  - `ValidateIdentifierInventory` (~L1634–1659) -- emits `test CA1707 scope identifier inventory drift: count=…, sha256=…`
  - `IdentifierInventory` (~L1661–1678) -- Roslyn-scan tracked `.cs` for `_` identifier tokens; SHA-256 over sorted `path:line:token`
  - `TrackedFiles` / `LoadLedger` (~L1866+, ~L1943+) -- `git ls-files` + ledger JSON load; untracked test files ignored
- `.editorconfig` (~CA1707 `tests/**.cs` / `FcDiagnosticIds.cs`) -- read-only exemption scopes; do not edit
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` -- contracts inventory root; read-only unless contracts drift appears

## Tasks & Acceptance

**Execution:**
- [x] Confirm red locally with `AnalyzerPolicy_IdentifierInventory_MatchesSeal` and capture printed `count` / `sha256`
- [x] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- set `testUnderscoreIdentifierTokens` and `testInventorySha256` to the confirmed live values at tip `1df79479` (`6734` / `e19f9e81…`; human approved Keep despite CI tip SHA `6e3d3966…`) -- reseal stale test inventory only
- [x] Re-run the same Fact until green; leave contracts seal keys untouched unless Ask First triggers

**Acceptance Criteria:**
- Given a clean FrontComposer working tree at the failing main tip, when `AnalyzerPolicy_IdentifierInventory_MatchesSeal` runs, then validation returns empty (no inventory drift strings).
- Given the resealed ledger, when inspecting `identifierInventory`, then only the test count/hash keys changed (unless human approved a contracts reseal), and no `.editorconfig` / `FcDiagnosticIds` / suppression changes exist.

## Spec Change Log

- 2026-08-13: Resealed live test inventory at baseline tip `1df79479` to `testUnderscoreIdentifierTokens=6734` / `testInventorySha256=e19f9e81f2ddd869e773e5551244f634e192eece09b1b60ef5aa15e2621ff802`. Count matches CI run 31715693323; CI tip `d355482` printed SHA `6e3d396608344c5911259b0584a16a55fd1f34115bdb4e8773495dd5f8ade073`. Human chose Keep for the tip-advanced live SHA. Contracts seal left untouched. Review patches refreshed Code Map / task wording / verification evidence and restored the deferred FrontComposer Windows Playwright FC-NIP entry that had been lost from `deferred-work.md`.

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~AnalyzerPolicy_IdentifierInventory_MatchesSeal" --configuration Release` -- expected: Passed, 0 failures; after reseal, no `identifier inventory drift` message

**Evidence:**
- Before reseal (tip `1df79479`): Failed — `count=6734, sha256=e19f9e81f2ddd869e773e5551244f634e192eece09b1b60ef5aa15e2621ff802` (contracts clean)
- After reseal: Passed — Failed: 0, Passed: 1, Skipped: 0 (Release)

## Suggested Review Order

- Entry point: resealed test inventory count and SHA only; contracts keys untouched.
  [`analyzer-policy-exception-ledger-v1.json:100`](../contracts/analyzer-policy-exception-ledger-v1.json#L100)

- Governance Fact that proves the seal; no production code path changed.
  [`AnalyzerPolicyGovernanceTests.cs:334`](../../tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs#L334)

- Deferred Windows Playwright FC-NIP and Memories CI items kept out of this reseal.
  [`deferred-work.md:2379`](./deferred-work.md#L2379)
