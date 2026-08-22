---
title: 'Restore compatibility gates on the production release path'
type: 'bugfix'
created: '2026-08-22'
status: 'in-review'
review_loop_iteration: 0
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
