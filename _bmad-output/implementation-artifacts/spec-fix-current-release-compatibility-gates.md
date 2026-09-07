---
title: 'Restore compatibility gates on the production release path'
type: 'bugfix'
created: '2026-08-22'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'fd04bdd97fbdd4976a0f213e46a316be199fd8a9'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-align-production-release-with-tenants.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The production candidate path replaced the compatibility-aware packer but left package validation disabled on live `dotnet pack` commands and left release-line suppression checks attached only to dead code. It also builds assemblies before applying the semantic-release version, so a correctly named package can contain binaries with default version metadata.

**Approach:** Put one compatibility-lifecycle policy on the live prepare/pack path, advance the next-release baseline to published `4.1.1`, retire absorbed v4 suppressions, and build/pack the sealed candidate with identical semantic-release version properties.

## Boundaries & Constraints

**Always:** Keep pack-once/`--no-build`, the eight-package inventory, unsigned sealed-candidate flow, production approval, exact-source/evidence gates, and the existing synthetic CI pack contract. Apply package validation to every live pack command; the SDK's `PackAsTool` exception remains authoritative for CLI. Fail release-policy errors before build and recheck before package-output mutation.

**Ask First:** Any new compatibility suppression or shim, public API change, real dispatch/publication, package baseline other than verified published `4.1.1`, or workflow/release-topology change.

**Never:** Reconnect semantic-release to the retired build-plus-pack entrypoint, keep two independent lifecycle implementations, weaken ApiCompat or expiry checks, hand-edit `CHANGELOG.md`, rewrite tags/history, modify dependencies/submodules, or change triggers, permissions, signing, evidence, or publishing commands.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Next release | semantic-release `4.2.0`; published `4.1.1`; empty reviewed ledger | Build metadata and eight package/symbol pairs use the requested version; live packs validate against `4.1.1` | Any untracked break or version mismatch blocks the candidate |
| Stale policy | wrong `currentRelease`, pre-target/expired row, stale MCP XML, or unadvanced baseline | No build or package-output cleanup begins | Actionable lifecycle diagnostic and nonzero exit |
| Shared CI pack | synthetic `0.0.0-ci-test` positional invocation | Existing shared contract remains usable; live pack commands still opt into package validation | Release-line matching is not applied outside explicit release-policy mode |

</frozen-after-approval>

## Code Map

- `eng/release_prepublish.py::phase_build`, `phase_pack`, `cmd_prepare` -- production ordering; forward one version to restore/build/pack and run policy before build.
- `scripts/pack-release-packages.py::main` -- only live eight-package packer; preserve positional CI contract, add aligned pack properties and explicit release-policy recheck.
- `eng/release_compatibility.py` -- new pure release-line/schema/expiry/baseline/XML policy shared by the pre-build guard and live packer.
- `eng/pack_release_packages.py` -- retired duplicate; migrate pure lifecycle behavior/tests, then remove it.
- `docs/diagnostics/compatibility-suppressions.json`, `src/Hexalith.FrontComposer.Mcp/CompatibilitySuppressions.xml` -- move to `v4.2` with no rows after the `4.1.1` baseline absorbs the 26 v4 removals; Contracts/Shell XML stay empty.
- `Directory.Build.targets`, `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`, `docs/diagnostics/README.md` -- apply and document the verified published `4.1.1` baseline.
- `tests/eng/test_pack_release_packages.py`, `tests/eng/test_release_prepublish.py` -- exercise live policy/packer ordering, synthetic CI mode, version propagation, and negative lifecycle cases.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` -- ledger/XML parity, evaluated baseline, active-packer governance, and package/binary version evidence.

## Tasks & Acceptance

**Execution:**
- [x] `eng/release_prepublish.py`, `scripts/pack-release-packages.py`, `eng/release_compatibility.py`, and `eng/pack_release_packages.py` -- consolidate live policy enforcement, align build/pack version properties, preserve pack-once, and remove dead-code certification.
- [x] Baseline, ledger, XML, and diagnostics documentation files -- advance to published `4.1.1` / planned `v4.2` and remove all 26 absorbed MCP suppression rows without adding replacements.
- [x] Python and .NET governance/package tests -- bind checks to production code and cover every matrix row, package validation, and compiled metadata alignment.

**Acceptance Criteria:**
- Given the current source and published `4.1.1` packages, when a non-publishing `4.2.0` candidate is prepared, then exactly eight `.nupkg` and eight `.snupkg` artifacts pass ApiCompat with no suppression and carry matching package/assembly/file/informational versions.
- Given workflow and release-configuration diffs, when scope is inspected, then triggers, permissions, reusable workflow pins, evidence/signing gates, and publishing commands are unchanged.

## Spec Change Log

## Design Notes

Use one pure lifecycle-policy implementation from both the pre-build guard and live packer. Production enables it explicitly; shared CI retains the positional invocation and skips release-line matching only, not package validation. Version, `PackageVersion`, continuous-integration, and validation properties must be consistent across build and pack rather than patched directly into assembly attributes.

## Verification

**Commands:**
- `node eng/semantic-release-plan.mjs` -- expected: release required at `4.2.0`.
- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py -v` -- expected: live packer/policy and orchestration cases pass.
- Build and directly run the focused SourceTools, Contracts.UI, Shell Governance, and MCP package-boundary tests -- expected: ledger/XML parity, evaluated `4.1.1` baseline, active-packer binding, and package metadata checks pass.
- `python3 eng/release_prepublish.py prepare --version 4.2.0-review.compat --non-publishing` -- expected: sealed non-publishing eight-package candidate succeeds; no publication command runs.
- `git diff --check` -- expected: clean.

**Observed 2026-08-22:**

- Semantic-release selected `4.2.0`; Python release-policy/orchestration tests passed 37/37; `actionlint` and `git diff --check` passed.
- The production `policy -> build -> pack` sequence completed with zero build warnings/errors, produced eight `.nupkg` plus eight `.snupkg` files, passed package/consumer validation, and verified the requested nuspec, assembly, file, and informational versions across eight packages and ten primary assembly copies.
- Focused SourceTools diagnostics, Contracts.UI baseline, Shell CI governance, and MCP package-boundary tests passed 95/95, 1/1, 66/66, and 2/2 respectively.
- The complete non-publishing prepare remained fail-closed after those candidate gates: the unchanged Contracts.UI clean-consumer test expects Fluent UI `5.0.0-rc.4-26180.1`, while the checked-in Builds catalog already supplies `5.0.0-rc.5-26219.1`. This pre-existing dependency/test drift is recorded in `deferred-work.md`; no publication command ran.

## Review Triage Log

Reviewed 2026-09-07 against the implementation commit `2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06`
(parent = `baseline_commit`). Claims verified against the current tree at `62ffa6f6`, which is
378 commits downstream of the change.

| # | Finding | Verdict | Evidence |
|---|---------|---------|----------|
| 1 | Release-line equality blocks every minor/major bump | false | Fail-closed is the spec's own "Stale policy" matrix row; the diagnostic is actionable and the bump is one ledger line. The ledger reads `v4.3` at HEAD, advanced as designed. |
| 2 | Nothing forces the ApiCompat baseline forward | high | The retired `eng/pack_release_packages.py:118,140` tied the baseline to the release line (`MCP_SUPPRESSION_CLEANUP_LINE` / `MCP_POST_REMOVAL_BASELINE`). `eng/release_compatibility.py:160-167` only compares the checked-in files to its own literal. At HEAD `currentRelease` is `v4.3` while `PUBLISHED_BASELINE_VERSION` is `4.1.1` and `v4.2.0`/`v4.3.0` are tagged and published, so candidates are diffed against a two-line-old surface and anything added in 4.2.0 and removed since is invisible. |
| 3 | `4.1.1` duplicated across five sites | medium | `Directory.Build.targets:5`, `Contracts.UI.csproj:8`, `release_compatibility.py:14`, `quality.yml:87`, plus test literals; the policy reconciles only the first three. Same root cause as #2. |
| 4 | Baseline test restates the constant instead of comparing to the feed | medium | `tests/eng/test_pack_release_packages.py` asserts `PUBLISHED_BASELINE_VERSION == "4.1.1"`, which cannot fail on drift. Same root cause as #2. |
| 5 | `docs/diagnostics/README.md` claim "latest published 4.1.1" is now untrue | medium | `v4.2.0` and `v4.3.0` are tagged and on the feed. Same root cause as #2. |
| 6 | Suppression-file and baseline-path allowlists hardcoded to 3 of 8 packages | medium | `DEFAULT_SUPPRESSION_FILES` covers Contracts/Mcp/Shell only and `DEFAULT_BASELINE_PATHS` two files; a `CompatibilitySuppressions.xml` under any of the other five packable projects, or a per-project baseline override, passes parity unreviewed. |
| 7 | Ledger `reason` allowlist diverges between policy and tests | medium | `DiagnosticRegistryTests.cs:2220-2222` accepts `intentional-major-break`, `known-binary-compatibility-gap`, `temporary-release-candidate-exception`; `release_compatibility.py:236` accepts only the first. A row that passes every repository test aborts `prepare`. |
| 8 | Gate 2a validation properties are inert | medium | `quality.yml:87` packs only `Hexalith.FrontComposer.Cli`, which is `PackAsTool=true` (`Cli.csproj:6`) — the exact case `docs/diagnostics/README.md` says the SDK disables package validation for. The gate proves nothing. |
| 9 | Governance tests assert Python source substrings | medium | `CiGovernanceTests.cs` pins `release_properties(version)`, `PUBLISHED_BASELINE_VERSION = "4.1.1"` and the Gate 2a command text; these pass on code that never runs and break on reformatting. Grouped with #8. |
| 10 | `phase_pack` lost its directory reset | medium | `eng/release_prepublish.py:213-231` dropped `shutil.rmtree(REPO_ROOT / NUPKGS_DIR)`; the packer unlinks only top-level `*.nupkg`/`*.snupkg` (`scripts/pack-release-packages.py:181-184`), while `_candidate_files()` seals every file under `nupkgs/` recursively. Stale nested output can enter the sealed candidate. |
| 11 | No test asserts `nupkgs/` is clean after a successful pack | medium | The new prepublish tests assert the opposite direction (a pre-seeded file is *preserved* on the fail-closed path). `deferred-work.md` DW-1860 is closed as "already resolved", but no replacement cleanup exists in either file. Grouped with #10. |
| 12 | Shared-CI pack requires a baseline no restore acquires | false | Real when shipped and it did break CI, but remediated downstream: `scripts/pack-release-packages.py:124,158` now runs a validation-aware `restore_command` before the `--no-build` packs. The bad outcome no longer occurs at HEAD. |
| 13 | No live (non-`--plan`) execution test for the packer | low | Every packer test appends `--plan` and returns before any `subprocess.run`; the only live-path guard is the plan/restore equality check at `tests/eng/test_pack_release_packages.py:420`. This is the coverage hole that let #12 ship. |
| 14 | Emptying the ledger made the C# row-level assertions dead code | low | `suppressions.ShouldBeEmpty` precedes the `foreach`, so the id/rationale/expiry/uniqueness checks are unreachable until a suppression returns. |
| 15 | Python fixtures hardcode `v4.2`/`v4.3` literals instead of deriving from the ledger | low | The retired suite derived the candidate via `current_release_version(payload)`; the 4.3 bump required a manual sweep of the fixtures. |
| 16 | `verify-candidate-packages.cs` ignores nuspec `<dependencies>` and `.snupkg` contents | low | A candidate whose `Shell` package still depended on `Contracts` at the previous version would pass; symbol packages are never opened. |
| 17 | `VerifyAssembly` `StartsWith` arms are never executed | low | The fixture builds outside a git repo, so the informational version equals the candidate exactly and only the `string.Equals` arm runs; the `4.2.0+<sha>` shape a real release produces is untested. |
| 18 | Gate 2b became SDK-dependent without a skip guard | low | Adding `tests/eng/test_release_prepublish.py` pulls `CandidatePackageVersionVerifierTests`, which runs a real `dotnet build` and `dotnet run --file` inside a step still named "Release suppression lifecycle tests". |
| 19 | Two submodule gitlinks bumped, named in no task | low | The commit advances `references/Hexalith.Builds` and `references/Hexalith.Tenants`, which the frozen Never list excludes ("modify dependencies/submodules"). Consistent with the known workspace auto-commit behavior rather than deliberate scope creep. |
| 20 | Non-advancing candidate version accepted by policy | maybe-false | Nothing in `validate_release_policy` compares the candidate triple to the published baseline; whether this is reachable depends on the separate semantic-release contract and evidence gates, which were not traced. Would be medium if reachable; settled by tracing whether `cmd_prepare` can be driven with a version at or below the last published release. |
| 21 | `ci.yml` pin and the `Hexalith.Builds` gitlink point at different commits | false | Deliberate and previously confirmed: the gitlink selects catalog content, the `uses:@<sha>` pin selects the release tool; only the pin and `builds-execution-sha` move in lockstep. |
| 22 | `ExpectedMcpBenchmarkRemovalTargets` is a dead 26-entry fixture | false | Still read at `DiagnosticRegistryTests.cs:784`. |
| 23 | Informational-version alignment demands a `.` separator SourceLink never emits | false | `eng/verify-candidate-packages.cs:193-198` carries both arms: `candidate + "+"` when the candidate has no build metadata, `candidate + "."` when it does. |
| 24 | Candidate build metadata (`4.3.1+build.7`) survives policy but breaks the pack-name check | low | `CANDIDATE_VERSION` accepts `+`, and NuGet strips it from the file name, so `phase_pack` would report the candidates missing. Not reachable from `semantic-release-plan.mjs` output, and it fails loudly rather than silently. |

**Grouping and routing**

- **intent_gap — baseline advance rule (#2, #3, #4, #5).** The frozen Boundaries make any "package baseline other than verified published `4.1.1`" an Ask First item, so the fix (a rule tying the baseline to `currentRelease`, or to the newest published line) cannot be derived from the captured intent. The intent covered a one-time advance and is silent on what keeps the baseline current.
- **intent_gap — Gate 2a proves nothing (#8, #9).** Making the gate real means packing a library package in `quality.yml`; the frozen Boundaries make a workflow/release-topology change an Ask First item.
- **bad_spec — policy allowlists (#6).** The spec fixed "every live pack command" but never said which projects the lifecycle policy enumerates, so the implementation pinned three of eight packages.
- **bad_spec — approved `reason` set (#7).** The spec never named the allowed suppression reasons, leaving two divergent sources of truth.
- **patch (moot under loopback) — `nupkgs/` reset (#10, #11).**
- **defer (moot under loopback) — #13, #14, #15, #16, #17, #18, #19, #20.**

Because intent_gap entries exist, the cascade calls for reverting the code and looping back to the human.
That revert was **not** performed: the change merged 378 commits ago, `v4.2.0` and `v4.3.0` shipped on
top of it, and three peer sessions are live in this working tree. Halting for a human decision instead.

**Disposition 2026-09-07 (human decision):** accepted as shipped. The change merged as
`2dcc43fea9aa39c42d15b1028fa5ef774b5d8b06`, and `v4.2.0` and `v4.3.0` were released on top of it,
so the intent_gap cascade's revert-and-re-derive was deliberately not performed. The two intent
gaps and the confirmed bad_spec/patch entries were filed forward against the current tree as
`spec-make-release-compatibility-gates-enforcing.md`, with the human decisions recorded there:
derive the ApiCompat baseline from the latest published release line (advancing it to `4.3.0`),
and give Quality Gate 2a a library package to pack. The eight remaining low and unverified
findings were appended to `deferred-work.md`. Finding #24 (SemVer build metadata in a candidate
version) was rejected: unreachable from `semantic-release-plan.mjs` output and it fails loudly.

