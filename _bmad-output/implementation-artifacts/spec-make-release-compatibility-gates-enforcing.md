---
title: 'Make the release compatibility gates actually enforce'
type: 'bugfix'
created: '2026-09-07'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c55bfd06fbc06558dd8fda29fe70462a1439050b'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-fix-current-release-compatibility-gates.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The compatibility-lifecycle policy introduced by
`spec-fix-current-release-compatibility-gates.md` is in place but does not enforce what it
claims. Its 2026-09-07 review confirmed four defects, all still live:

1. **The ApiCompat baseline cannot go stale loudly.** `eng/release_compatibility.py` pins
   `PUBLISHED_BASELINE_VERSION = "4.1.1"` and only checks the checked-in files against that
   literal — a constant compared with itself. The retired `eng/pack_release_packages.py` had a
   real advance rule (`MCP_SUPPRESSION_CLEANUP_LINE` / `MCP_POST_REMOVAL_BASELINE`) that was
   lost in the consolidation. Today `currentRelease` is `v4.3` and `v4.2.0`/`v4.3.0` are
   published, while the baseline is still `4.1.1`: candidates are diffed against a two-line-old
   surface, so any API added in 4.2.0 and removed since is invisible. `docs/diagnostics/README.md`
   still calls 4.1.1 "latest published", and the Python test asserts the constant equals itself.
2. **Quality Gate 2a proves nothing.** `.github/workflows/quality.yml` applies the three
   package-validation properties to a `dotnet pack` of `Hexalith.FrontComposer.Cli`, which is
   `PackAsTool=true` — the case the SDK disables package validation for. The `CiGovernanceTests`
   assertions pin that command as source text, so they report the gate as enforced either way.
3. **The policy sees 3 of 8 packages.** `DEFAULT_SUPPRESSION_FILES` covers Contracts, Mcp and
   Shell; `DEFAULT_BASELINE_PATHS` covers two files. A `CompatibilitySuppressions.xml` under any
   other packable project, or a per-project baseline override, passes parity unreviewed.
4. **Two divergent `reason` allowlists.** `DiagnosticRegistryTests` accepts
   `intentional-major-break`, `known-binary-compatibility-gap` and
   `temporary-release-candidate-exception`; `release_compatibility.py` accepts only the first, so
   a ledger row that passes every repository test aborts a live `prepare`.

**Approach:** Replace the self-referential baseline literal with a rule derived from the latest
published release line and advance the checked-in value to `4.3.0`; make Gate 2a pack a library
package so baseline validation actually executes; enumerate suppression and baseline sites from the
release inventory instead of hardcoded tuples; and reconcile the approved-reason set to one source
of truth.

## Boundaries & Constraints

**Always:** Keep pack-once/`--no-build`, the eight-package inventory, the unsigned sealed-candidate
flow, production approval, exact-source/evidence gates, the validation-aware restore that
`scripts/pack-release-packages.py` now performs, and the existing synthetic CI pack contract.
Governance checks must bind to behavior (for example the `--plan` JSON) rather than to Python or
YAML source substrings.

**Ask First:** Any new compatibility suppression or shim, public API change, real
dispatch/publication, or a release-topology change beyond the Gate 2a pack target.

**Never:** Reintroduce a second lifecycle implementation, weaken ApiCompat or expiry checks,
hand-edit `CHANGELOG.md`, rewrite tags/history, modify dependencies/submodules, or change triggers,
permissions, signing, evidence, or publishing commands. `ci.yml`, `release.yml` and
`release-evidence.yml` are pinned by `evaluator_authorizations` and must not be edited here.

## Human decisions (2026-09-07)

- **Baseline:** derive it from the latest published release line and fail closed when the
  checked-in value lags; bump the current value to `4.3.0`.
- **Gate 2a:** keep the CLI pack and add a pack of one library package so the validation
  properties have effect.
- **Baseline rule:** use the preceding-line rule — at pack time the baseline line must be exactly
  the line before the candidate's line (packing `4.4.0` requires `4.3.x`), as the retired MCP rule
  did. The static repo-level check asserts only that the baseline does not lag `currentRelease`.
- **Reason set:** narrow to the single policy reason `intentional-major-break`; the C# tests adopt
  it. The ledger is empty, so no row breaks.
- **`nupkgs/` reset:** dropped. Verified already present at `scripts/pack-release-packages.py:180-184`
  (deletes `*.nupkg`/`*.snupkg` after restore, before the first pack); the retired
  `eng/pack_release_packages.py` never had one. Findings #10/#11 are refuted and DW-1860's
  "already resolved" closure was correct.
- **EventStore 3.103.0:** out of scope here; deferred as separate work owned by `Hexalith.Builds`.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Lagging baseline | `currentRelease` ahead of the checked-in baseline | Policy fails before build with the expected baseline named | Actionable diagnostic and nonzero exit |
| New suppression site | `CompatibilitySuppressions.xml` under a packable project outside the current three | Parity check sees it | Unreviewed XML blocks the candidate |
| Ledger reason | Row using a reason the C# tests accept | One allowlist decides; tests and policy agree | Same diagnostic from both |
| Gate 2a | CI quality run | A library package packs with baseline validation in effect | A real ApiCompat break fails CI |

</frozen-after-approval>

## Carried Review Evidence

From the `## Review Triage Log` of `spec-fix-current-release-compatibility-gates.md`:
findings #2-#5 (baseline), #8/#9 (Gate 2a), #6 (allowlists), #7 (reason set), #10/#11
(`nupkgs/` reset). Note that `deferred-work.md` DW-1860 is closed as "already resolved" for the
`nupkgs/` cleanup; that closure is incorrect — no replacement cleanup exists in
`eng/release_prepublish.py` or `scripts/pack-release-packages.py`.

**Planning correction (2026-09-07):** findings #10/#11 and the DW-1860 note above are refuted by the
tree — the reset exists at `scripts/pack-release-packages.py:180-184` and the retired script never
had one. Human confirmed: no `nupkgs/` work in this spec.


## Code Map

- `eng/release_compatibility.py` -- the policy. `PUBLISHED_BASELINE_VERSION = "4.1.1"` (line 14) is
  the self-referential literal; the check at lines 160-167 compares checked-in XML against that
  constant. `APPROVED_SUPPRESSION_REASON` (line 27) is the single-reason allowlist, enforced at
  lines 236-240. `DEFAULT_BASELINE_PATHS` (29-32) and `DEFAULT_SUPPRESSION_FILES` (33-43, only
  Contracts/Mcp/Shell) are the hardcoded tuples to replace with inventory-driven enumeration.
  `validate_release_policy` (104-180) already accepts `baseline_paths` / `suppression_files`
  overrides — keep that seam for tests.
- `eng/release-package-inventory.json` -- canonical 8-package inventory; the enumeration source.
  Reuse the existing reader `packable_projects()` in `scripts/pack-release-packages.py:36` rather
  than adding a third copy (`eng/release_contract.py:16-25` is already a second hardcoded list).
- `Directory.Build.targets:5` and `src/Hexalith.FrontComposer.Contracts.UI/…csproj:8` -- the two
  checked-in `FrontComposerPackageValidationBaselineVersion` sites, both `4.1.1`.
- `.github/workflows/quality.yml:83-113` -- Gate 2a. Line 87 packs the `PackAsTool=true` CLI, so the
  SDK disables package validation; add a library pack here.
- `tests/…/Governance/CiGovernanceTests.cs` -- `PackageInventory_IsExplicitLockstepAndReviewable`
  (1597) pins `PUBLISHED_BASELINE_VERSION = "4.1.1"` (1627) and `…BaselineVersion=4.1.1` in the pack
  command (1632); `SemanticReleasePack_EvaluatesPublished411PackageValidationBaseline` (1676)
  MSBuild-evaluates the real property and asserts `4.1.1` (1701, 1707) — a behavior-bound check to
  keep and retarget. `ExtractNamedStep` (3329) is the helper for binding Gate 2a structurally.
- `tests/eng/test_pack_release_packages.py` -- line 117 asserts the constant equals itself; line 30
  and 189-195 carry `4.1.1` fixtures.
- `tests/…/Diagnostics/DiagnosticRegistryTests.cs:2219-2223` -- the three-reason `HashSet`.
- `docs/diagnostics/README.md:7` -- still calls `4.1.1` "latest published".
- Do not change: `ci.yml`, `release.yml`, `release-evidence.yml` (pinned by
  `evaluator_authorizations`), signing, evidence and publishing commands.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_compatibility.py` -- replace the self-referential baseline check with the
      preceding-line rule, naming both found and expected baseline in the diagnostic -- makes a
      lagging baseline fail closed.
- [x] `eng/release_compatibility.py` -- derive suppression and baseline sites from the release
      inventory instead of `DEFAULT_SUPPRESSION_FILES` / `DEFAULT_BASELINE_PATHS` -- closes the
      3-of-8 blind spot.
- [x] `eng/release_compatibility.py` + `tests/…/DiagnosticRegistryTests.cs` -- narrow the reason set
      to `intentional-major-break` in both, leaving one source of truth.
- [x] `Directory.Build.targets`, `src/…Contracts.UI/….csproj` -- advance the baseline to `4.3.0`.
- [x] `.github/workflows/quality.yml` -- add a `dotnet pack` of `Hexalith.FrontComposer.Contracts`
      carrying the three validation properties, keeping the existing CLI pack -- gives Gate 2a a
      target the SDK does not disable validation for.
- [x] `tests/eng/test_pack_release_packages.py` -- replace the equals-itself assertion with cases
      that fail a lagging baseline and pass a current one.
- [x] `tests/…/CiGovernanceTests.cs` -- retarget the `4.1.1` pins to `4.3.0`, rename the `411` test,
      and bind Gate 2a to the packed project's validation behavior rather than only its step name.
- [x] `docs/diagnostics/README.md` -- correct the "latest published" baseline text.

**Acceptance Criteria:**
- Given the checked-in baseline lags the rule's expected value, when the policy runs before build,
  then it exits nonzero naming both the found and the expected baseline.
- Given a `CompatibilitySuppressions.xml` is added under a packable project outside
  Contracts/Mcp/Shell, when parity runs, then the unreviewed file blocks the candidate.
- Given a ledger row uses a reason accepted by the C# tests, when the policy validates it, then both
  produce the same verdict.
- Given Gate 2a runs in CI, when a real ApiCompat break exists in a packed library, then the job
  fails.

## Implementation Notes

**Implemented 2026-09-07.**

- **Baseline rule (`eng/release_compatibility.py`).** `_validate_baseline` replaces the
  constant-compared-with-itself check. At pack time (`match_candidate_release=True`) the checked-in
  baseline line must be exactly the line before the candidate's line; the static repository check
  (shared CI / `--plan` without `--release-policy`) accepts the ledger's `currentRelease` line or
  the one before it and fails closed below that. Both diagnostics name the found value and the
  expected line. `preceding_release_line` / `is_preceding_release_line` treat a major bump
  (`M.0`) as "any minor of major `M-1`", because the previous major's last minor cannot be derived
  from the candidate version alone.
- **Inventory-driven enumeration.** `packable_packages` / `packable_projects` moved from
  `scripts/pack-release-packages.py` into `eng/release_compatibility.py`; the packer keeps a thin
  wrapper so its `REPO_ROOT` / `INVENTORY_PATH` test seam still works. `validate_release_policy` now
  derives baseline override sites (every packable `.csproj`) and suppression sites (each packable
  project's `CompatibilitySuppressions.xml`) from `eng/release-package-inventory.json`, requires the
  shared `Directory.Build.targets` default, requires every declaring site to agree, and rejects any
  `CompatibilitySuppressions.xml` under `src/` that belongs to no packable project. A packable
  project with no suppression file contributes no rows, exactly like the checked-in empty files.
- **Reason set.** `DiagnosticRegistryTests` now accepts only `intentional-major-break`, matching
  `APPROVED_SUPPRESSION_REASON`. The ledger is empty, so no row changed verdict.
- **Gate 2a.** A new `Gate 2a: Library Package Validation` step packs
  `Hexalith.FrontComposer.Contracts` with the three validation properties; the CLI smoke pack is
  unchanged apart from its baseline literal. The library pack does **not** use `--no-build`: the
  solution build stamps assembly version `1.0.0.0`, and ApiCompat's CP0003 identity rule rejects
  that against a 4.x baseline, so the step builds with `-p:Version=4.3.0-ci`. Verified locally that
  the `--no-build` form fails with CP0003 and the built form passes, which also proves the baseline
  package is opened and diffed (the old CLI-only gate produced no ApiCompat output at all).
- **Governance binding.** `PublishedPackageValidationBaseline` reads the baseline out of the live
  packer's `--plan` JSON instead of pinning a Python source substring; both the inventory lockstep
  test and the renamed `SemanticReleasePack_EvaluatesThePublishedPackageValidationBaseline` compare
  the MSBuild-evaluated property to that value. `QualityWorkflow_Gate2aPacksALibraryPackageWithValidationInEffect`
  MSBuild-evaluates every project Gate 2a packs and requires at least one to evaluate
  `EnablePackageValidation=true`, while asserting the CLI still evaluates `false`.
- **Collateral baseline sites not named in the Code Map.** `tests/…/PackageBoundaryTests.cs`,
  `tests/…/McpRuntimePackageBoundaryTests.cs`, `tests/…/DiagnosticRegistryTests.cs` and
  `tests/eng/test_release_prepublish.py` also pinned `4.1.1`; they were retargeted (deriving the
  value from `Directory.Build.targets` or the policy constant where practical). The MCP candidate
  fixture moved to `4.4.0-review.compat` because a `4.2.0` candidate now trips CP0003 against the
  `4.3.0` baseline.
- **Ledger left at `v4.3`.** `docs/diagnostics/compatibility-suppressions.json` is unchanged, per
  the frozen decision that the static check "asserts only that the baseline does not lag
  `currentRelease`". Consequence: a live `--release-policy` prepare on the v4.3 line is now blocked
  until the ledger advances to `v4.4` (the same one-line ledger bump every release already needs).
  The production `--plan` test therefore drives an inventory-shaped fixture repository instead of
  the working tree, so it does not go transient as the ledger advances.

## Spec Change Log

## Review Triage Log

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py` -- expected: all pass.
- `python3 scripts/pack-release-packages.py ./nupkgs 4.4.0 --plan` -- expected: exits 0 and the plan
  reports the advanced baseline; a stale checked-in baseline exits nonzero with the expected value named.
- `dotnet msbuild src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj -getProperty:EnablePackageValidation,PackageValidationBaselineVersion -p:EnableFrontComposerPackageValidation=true` -- expected: `true` and `4.3.0`.
- Shell governance tests via the direct xUnit runner (VSTest sockets are blocked):
  `-class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests` -- expected: pass.

**Observed 2026-09-07:**

- `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish` -- 49/49 pass
  (36 in the packer/policy suite, including the new preceding-line, static-lag, site-disagreement,
  inventory-enumeration and stray-suppression-file cases).
- `python3 scripts/pack-release-packages.py ./nupkgs 4.4.0 --plan` -- exit 0, plan reports
  `-p:FrontComposerPackageValidationBaselineVersion=4.3.0` on the restore and all eight packs.
  With `Directory.Build.targets` temporarily reverted to `4.1.1` the same command exits 1 with
  `Directory.Build.targets: package-validation baseline must be on the v4.2 or v4.3 release line
  for currentRelease v4.3; found '4.1.1'`; reverting only one of the two sites exits 1 with
  `package-validation baseline sites disagree`.
- `dotnet msbuild src/Hexalith.FrontComposer.Contracts/… -getProperty:EnablePackageValidation,PackageValidationBaselineVersion -p:EnableFrontComposerPackageValidation=true`
  -- `true` / `4.3.0`. The CLI evaluates `EnablePackageValidation=false` (PackAsTool), confirming
  the pre-change gate was inert.
- Gate 2a command run locally: `dotnet pack …Contracts.csproj -c Release --output … -p:Version=4.3.0-ci
  -p:PackageVersion=4.3.0-ci` plus the three validation properties -- succeeds with no ApiCompat
  findings against published `4.3.0`. The same command with `--no-build` fails with
  `CP0003 … assembly version '1.0.0.0' should be equal to or higher than [Baseline] … '4.3.0.0'`,
  proving baseline validation executes.
- Direct xUnit v3 runner (VSTest sockets blocked): `CiGovernanceTests` 80/81 pass; the single
  failure is the pre-existing `EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`
  gitlink pin (`references/Hexalith.EventStore` moved to `059f6a8` in commit `7c57aee0`), unrelated
  to this change. `DiagnosticRegistryTests` 95/95, `McpRuntimePackageBoundaryTests` 2/2 (real pack
  against the published `4.3.0` MCP baseline), `PackageBoundaryTests.PackageValidation_PinsTheSharedPublishedBaseline` 1/1.
- `actionlint .github/workflows/quality.yml` -- exit 0. `git diff --check` -- clean.

## Documented Unrelated Changes

- `_bmad-output/implementation-artifacts/deferred-work.md` was already modified in the working tree
  by the orchestrator sweep before this change began; it was neither edited nor reverted here.
