---
title: 'Reseal CA1707 test identifier inventory after Gate 2b drift'
type: 'bugfix'
created: '2026-08-13'
status: 'draft'
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

- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` (`identifierInventory`, ~L98–104) -- sole writable seal; current test seal `6610` / `b7da0f795de7cef7d98cbee270554eb7f55aa9c1e6b8ea4c8e6caecddbbf4df1`; CI target `6734` / `6e3d396608344c5911259b0584a16a55fd1f34115bdb4e8773495dd5f8ade073`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/AnalyzerPolicyGovernanceTests.cs`
  - `AnalyzerPolicy_IdentifierInventory_MatchesSeal` (~L334–338) -- failing Fact
  - `ValidateIdentifierInventory` (~L1634–1659) -- emits `test CA1707 scope identifier inventory drift: count=…, sha256=…`
  - `IdentifierInventory` (~L1661–1678) -- Roslyn-scan tracked `.cs` for `_` identifier tokens; SHA-256 over sorted `path:line:token`
  - `TrackedFiles` / `LoadLedger` (~L1866+, ~L1943+) -- `git ls-files` + ledger JSON load; untracked test files ignored
- `.editorconfig` (~CA1707 `tests/**.cs` / `FcDiagnosticIds.cs`) -- read-only exemption scopes; do not edit
- `src/Hexalith.FrontComposer.Contracts/Diagnostics/FcDiagnosticIds.cs` -- contracts inventory root; read-only unless contracts drift appears

## Tasks & Acceptance

**Execution:**
- [ ] Confirm red locally with `AnalyzerPolicy_IdentifierInventory_MatchesSeal` and capture printed `count` / `sha256`
- [ ] `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- set `testUnderscoreIdentifierTokens` and `testInventorySha256` to the confirmed live values (expected CI values above) -- reseal stale test inventory only
- [ ] Re-run the same Fact until green; leave contracts seal keys untouched unless Ask First triggers

**Acceptance Criteria:**
- Given a clean FrontComposer working tree at the failing main tip, when `AnalyzerPolicy_IdentifierInventory_MatchesSeal` runs, then validation returns empty (no inventory drift strings).
- Given the resealed ledger, when inspecting `identifierInventory`, then only the test count/hash keys changed (unless human approved a contracts reseal), and no `.editorconfig` / `FcDiagnosticIds` / suppression changes exist.

## Spec Change Log

## Verification

**Commands:**
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --filter "FullyQualifiedName~AnalyzerPolicy_IdentifierInventory_MatchesSeal" --configuration Release` -- expected: Passed, 0 failures; after reseal, no `identifier inventory drift` message
