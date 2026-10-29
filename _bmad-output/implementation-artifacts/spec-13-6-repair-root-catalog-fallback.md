---
title: 'Story 13.6: Repair the Root Catalog Package Fallback'
type: 'bugfix'
created: '2026-10-29'
status: 'in-progress'
route: 'dispatch'
artifact_role: 'supporting-implementation-record'
primary_story: '_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md'
baseline_commit: 'cde40df71ebcef6c6c74c25a5a5644bebef3fc17'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
---

<frozen-after-approval reason="human-owned intent - Owner approved concrete amendment 2026-10-29">

## Intent

**Problem:** Parties' corrected nested catalog contains Contracts.UI, but its normal uninitialized-nested checkout imports root Builds 81ab, which omits it. Actual standalone package restore fails with three NU1010 errors.

**Approach:** Apply the prepared root-catalog entry and owner guard, refresh only the normal derived FrontComposer audit, select the immutable direct successor locally, and update exact provenance bindings through delayed activation. The Owner explicitly approved this amendment and necessary local commits on 2026-10-29.

## Boundaries & Constraints

**Always:** Start an isolated owning Builds worktree at `81ab049208678bd6616828824729c0ddac02f7d1`. Add exactly one Contracts.UI row using existing `$(HexalithFrontComposerVersion)` 4.7.1 plus the three prepared guard lines. Preserve every actual package version/SDK and unrelated catalog/audit evidence, selected Parties `492a8cc1417c625cdfb7fd4c946686ef88adab9d`, other root selections, Platform source/raw inventory/global trust and all configuration bytes. Use the documented revision-bound FrontComposer-only incremental audit. Reuse existing graph/materialization/seal helpers, exact Git objects and AD-12; retain old-base refusal separately from a later activation pass. Validate every full commit message with its owning lock-pinned CLI/config before use. Preserve the three pre-existing modified context/parent/result files unstaged. Root commits require a feature branch. Parent owns the final local activation record after implementation audit.

**Never:** Push/publish, initialize nested submodules, acquire unknown sources, change actual package versions, relax validation, broaden trust or claim old runtime/hosted results ran the new candidate. No product/runtime or engine primitive changes are intended. Separate WebKit/UX/manual/owner decisions remain unchanged; primary Story 13.6 stays in-progress.

## I/O & Edge-Case Matrix

| Scenario | Input | Expected behavior | Failure handling |
|---|---|---|---|
| Normal fallback | Parties with nested Builds uninitialized | Standalone Release/NuGet restore/build imports adopted root catalog and resolves all three Contracts.UI consumers at 4.7.1 | Retain first failure and use focused environment fallback |
| Selected catalog | Exact Parties and its own selected catalog | Same standalone build still passes at existing versions | Diagnose drift; no unrelated changes |
| Owner guard | Correct catalog; missing UI; wrong family | Positive passes; both malformed controls fail for intended reason | No false successful guard |
| Provenance | Exact candidate and actual committed activation base | 69 raw edges/eight selectors/all 17 Platform mappings retained; current graph and later-base transition pass | Earlier old-base refusal is retained; no candidate-policy substitution |
| Drift | Existing projection/engine/consumer fixtures | Changed inputs, unknown sources and forged evidence still refuse | Fix introduced failure without weakening gates |

</frozen-after-approval>

## Code Map

- `/tmp/fc-story13-6-current-20261029-95mkw08q/root-catalog-proposal/builds-root-catalog.patch`: approved delta and README/full files. Restore red/green and guard negative controls already demonstrated.
- Builds `Props/Directory.Packages.props`, `Tools/test-authoritative-package-catalog.ps1`: one family row plus evaluated property, required identity and alignment row. Read owning guidance/config and `Tools/README.md` first.
- Builds `Tools/package-version-audit.json` and documented audit/authority/consumer/exception validators: preserve prior audit externally; source commit precedes `-PriorAuditPath ... -ChangedFamily hexalith-frontcomposer` refresh and metadata commit.
- `eng/dependency-graph-policy.json` `consumer_source_projections`: update only required root Builds binding/selector seal. Reuse `eng/dependency_graph.py` canonical digest, exact-object/configuration and bounded materialization helpers. No primitive change.
- `tests/eng/test_dependency_graph.py`, `test_dependency_handoff.py`, `test_release_evidence_v2.py`, `test_apphost_dependency_modes.py`; Shell `FrontComposerUiAppHostTests`/`InfrastructureGovernanceTests`: existing refusal/consumer checks. Local builds use Debug; isolated standalone checks use registry Release/NuGet commands.

## Tasks & Acceptance

- [x] `references/Hexalith.Builds` catalog/guard: inspect owning guidance, preserve initial edits and create isolated direct successor with pinned commitlint evidence.
- [x] `references/Hexalith.Builds` audit: refresh only FrontComposer, preserve versions/unrelated evidence, run documented validators/fixtures and commit metadata.
- [x] `references/Hexalith.Builds`, `eng/dependency-graph-policy.json`: advance only Builds and necessary bindings; verify graph invariants and both isolated standalone package paths.
- [x] `_bmad-output/implementation-artifacts/spec-13-6-repair-root-catalog-fallback.md` records existing Python/Shell suites: run relevant consumers and Debug builds; commit only root implementation. Record exact commands/counts/revisions/first failures here; parent owns audit, later activation and story/result reconciliation.

**Acceptance Criteria:**
- Given each reachable package import path, when the exact standalone restore/build runs, then Contracts.UI resolves centrally at unchanged 4.7.1 with clean build.
- Given immutable source/audit/root successors, when parent audits scope and actual later-base activation, then exact provenance passes without changing other selections or weakening refusal behavior.
- Given independent review, when this bounded correction closes, then E1/B3 are dispositioned while primary acceptance/manual history and independent gates retain their meanings.

## Implementation Notes

Owner approval received after the concrete red/green proposal. Parent owns original dirty documentation and final activation/review.

## Spec Change Log

## Review Triage Log

## Verification

Run the Code Map owner validators/fixtures and Python suites, both registry standalone restore/build paths, exact-commit graph validate/diff, focused Debug builds and host/governance tests, `python3 eng/validate-story-artifacts.py` and `git diff --check`. Record exact commands/counts/limits; old runtime evidence keeps its original inputs.


### Implementation verification — 2026-10-29

Implementation HEAD is `f54ce542db8675d685c50408b390158152283106` on `fix/story-13-6-root-catalog-fallback`. Its only committed paths are `references/Hexalith.Builds` and `eng/dependency-graph-policy.json`. Parent audit, independent review, actual later-base activation and final result reconciliation remain pending at this handoff; this supporting record and primary Story 13.6 remain in-progress.

Owning Builds worktree: `/tmp/fc-root-catalog-implementation-20261029-403g00hg/builds`, started exactly at `81ab049208678bd6616828824729c0ddac02f7d1` on `fix/story13-6-root-catalog-fallback`. Source commit `11ef2cab441433c5fe25a2fb3ddcfc282e3a7f0e` adds exactly one existing-family `Hexalith.FrontComposer.Contracts.UI` row and the three approved guard lines. Audit-only successor `81225bcb6151c38ae891abd9110af17385e1b3b9` refreshes only `hexalith-frontcomposer`. All 307 existing selected versions, all 302 unrelated package rows and all 150 unrelated family decisions remain unchanged. Consumer declarations, source scope and consumer evidence hash are preserved; their owning revision advances normally to the committed source. The BOM and Git EOL normalization are preserved; candidate raw Git catalog SHA-256 is `6833662f8b6e49d6001a6b89c7ee6b9065a972554ab72c3c41fc935411f70966`.

Every command, CWD, exit status, duration and log path is retained in `/tmp/fc-root-catalog-implementation-20261029-403g00hg/results.jsonl`. The prior audit is retained externally as `package-version-audit.before.json` in that directory. `catalog-preservation.json`, `audit-preservation.json`, `standalone-paths.json` and `final-invariants.json` retain machine-readable scope proofs. The three exact full messages are retained as `builds-source-message.txt`, `builds-audit-message.txt` and `root-implementation-message.txt`; owning lock-pinned commitlint 21.2.4/21.2.2 validated each before use with zero problems/warnings. No push or publication occurred.

The deliberate refresh command, from the isolated owning worktree, was:

```text
pwsh -NoProfile -File Tools/audit-central-package-versions.ps1 -PriorAuditPath /tmp/fc-root-catalog-implementation-20261029-403g00hg/package-version-audit.before.json -ChangedFamily hexalith-frontcomposer -OutputPath Tools/package-version-audit.json
```

It passed for 308 packages: one refreshed family and 150 preserved. Owning commands below ran in the same worktree; the audit validator also passed again after the metadata commit.

| Exact command | Result |
| --- | --- |
| `pwsh -NoProfile -File Tools/test-authoritative-package-catalog.ps1` | 51 required identities and four family bindings passed. The same new script in isolated missing-UI and wrong-family controls rejected the absent row and `4.7.0` versus `4.7.1` for the intended reasons. |
| `pwsh -NoProfile -File Tools/validate-central-package-versions.ps1` | 308 package rows passed. |
| `pwsh -NoProfile -File Tools/test-central-package-version-validator.ps1` | 17 scenarios passed. |
| `pwsh -NoProfile -File Tools/validate-package-version-audit.ps1` | 308 packages, 151 families, one source passed, including committed-result validation. |
| `pwsh -NoProfile -File Tools/test-package-version-audit-generator.ps1` | 115 scenarios passed. |
| `pwsh -NoProfile -File Tools/test-package-version-audit-validator.ps1` | 103 scenarios passed. |
| `pwsh -NoProfile -File Tools/validate-consumer-package-authority.ps1 -RepositoryRoot /tmp/fc-root-catalog-implementation-20261029-403g00hg/builds -CatalogPath /tmp/fc-root-catalog-implementation-20261029-403g00hg/builds/Props/Directory.Packages.props` | 22 projects passed. |
| `pwsh -NoProfile -File Tools/test-consumer-package-authority-validator.ps1` | 32 scenarios passed. |
| `pwsh -NoProfile -File Tools/validate-package-version-exceptions.ps1 -InventoryPath Tools/package-version-exceptions.json -CatalogPath Props/Directory.Packages.props` | 16 allowlisted exceptions passed. |
| `pwsh -NoProfile -File Tools/test-package-version-exception-validator.ps1` | 30 scenarios passed. |

First invocation corrections are retained, not catalog failures: the exception validator without mandatory `-InventoryPath`/`-CatalogPath` failed before evaluation, then the documented command above passed; graph validation with `--commit HEAD` refused its noncanonical argument, then the full exact SHA below passed. A preliminary commitlint invocation preceded creation of its temporary message file and refused the missing file; the full candidate subsequently passed before its commit. The owner suite itself reported that first missing-arguments invocation as failed; the corrected standalone result closes it without changing validation.

Both isolated package paths use exact Parties `492a8cc1417c625cdfb7fd4c946686ef88adab9d` and the policy registry's literal commands:

```text
dotnet restore Hexalith.Parties.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true
dotnet build Hexalith.Parties.Standalone.slnx --configuration Release --no-restore -p:UseNuGetDeps=true
```

CWDs are `/tmp/fc-root-catalog-implementation-20261029-403g00hg/fallback/references/Hexalith.Parties` and `/tmp/fc-root-catalog-implementation-20261029-403g00hg/selected/references/Hexalith.Parties`. The normal fallback leaves nested Builds absent/empty and imports root sibling `81225bcb6151c38ae891abd9110af17385e1b3b9`; the selected path uses its own `326af3d248974b5f3849ff667e97a5d61e3e3f30`. Existing exact-object clone/bounded contract materialization helpers produced each contract tree (2,931 regular files, under 59 MB), rehashed its catalog against the graph and created no nested repository. Both restore/build pairs passed with zero warnings/errors without environment fallback. Each path's three direct consumers (UI, AdminPortal, ConsumerPortal) restore `Hexalith.FrontComposer.Contracts.UI/4.7.1` in actual assets. The pre-amendment root-81ab normal-fallback NU1010 failure remains separately retained in `/tmp/fc-story13-6-current-20261029-95mkw08q/fallback-current.log` and its original `results.jsonl`; it is not overwritten by these green runs.

Root commands/results:

| Exact command | Result |
| --- | --- |
| `python3 eng/dependency_graph.py --root . validate --commit f54ce542db8675d685c50408b390158152283106` | Passed: 69 raw edges, eight selectors; graph digest `c1f3b2156bfcc08a1b499c0e0f781ee3c17afc04167a04945de0d47078154e47`. |
| `python3 eng/dependency_graph.py --root . diff --event push --event-base cde40df71ebcef6c6c74c25a5a5644bebef3fc17 --candidate f54ce542db8675d685c50408b390158152283106` | Expected exit 1: `consumer projection root source selectors changed`. Old-base refusal is retained separately; no later-base activation is claimed by this implementation. |
| `python3 -m unittest discover -s tests/eng -p test_dependency_graph.py` | 130 passed. |
| `python3 -m unittest discover -s tests/eng -p test_dependency_handoff.py` | 11 passed. |
| `python3 -m unittest discover -s tests/eng -p test_release_evidence_v2.py` | 15 passed. |
| `python3 -m unittest discover -s tests/eng -p test_apphost_dependency_modes.py` | 13 passed. |
| `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug` | Passed, with one MSB3026 XML-file copy retry warning retained in `apphost-debug-build.log`. |
| `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -m:1` | Focused serialized rerun passed, zero warnings/errors. |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug -m:1` | Passed, zero warnings/errors. |
| `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Integration.FrontComposerUiAppHostTests -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests` | 76 passed, zero failed/skipped/not-run. |

The Platform owner/source/raw inventory and all 17 raw mappings remain identical. The only policy-field changes are `builds_commit` and canonical `root_selectors_sha256` (`ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef`); all other root selections, global trust and policy values are identical. Exact committed consumer configuration still hashes to `f5186d1b483c33e065ac542374a4b90c0da8257782ebc871d29aaf0ef3613762`. All four pre-existing modified documents (including the parent's approved Parties amendment) retain their starting bytes and remain unstaged. No engine primitive, product/runtime source, actual package version, SDK, nested checkout or hosted/runtime/manual acceptance was changed or claimed.

Aspire baseline commands were `aspire start --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --isolated --non-interactive`, `aspire describe --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive --format Json`, then `aspire stop --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive`. All passed before implementation. Resource state included Running and Waiting entries; this snapshot is not full-topology readiness or acceptance evidence. The started AppHost was stopped before builds.

Final `python3 eng/validate-story-artifacts.py`, `git diff --check`, `git diff --check cde40df71ebcef6c6c74c25a5a5644bebef3fc17 f54ce542db8675d685c50408b390158152283106`, and owning `git diff --check 81ab049208678bd6616828824729c0ddac02f7d1 81225bcb6151c38ae891abd9110af17385e1b3b9` passed. The untracked supporting spec was also checked using `git diff --no-index --check /dev/null _bmad-output/implementation-artifacts/spec-13-6-repair-root-catalog-fallback.md`: no whitespace diagnostics; its exit 1 indicates the expected new-file diff. The supporting record remains unstaged for the parent-owned final activation record.

## Parent audit and local activation preparation - 2026-10-29

The parent read the full root/untracked/owning-repository diff and independently checked exact source ancestry, approved one-row/three-line delta, all 307 previous selected versions, 302 unrelated package rows and 150 unrelated family decisions. Consumer entries/hash/scope are identical except their normal source revision. Root commit f54 changes only the Builds gitlink and exactly two policy fields; independently recomputed selector and configuration hashes match, and all other policy values remain unchanged. All four initially modified parent documents retain their full starting bytes and remain unstaged. Receipts and full reviewed diff are under `/tmp/fc-story13-6-approved-parent-wfbpz9j_/`.

Matrix audit: both actual registry restore/build paths passed, all three direct UI consumers resolve 4.7.1, owner positive/negative guard controls ran, 297 owner fixtures and 169 Python checks passed, and 76 built host/governance tests passed without skips. Debug Shell and the serialized AppHost rerun are warning/error free. No broader runtime result is inferred. The activation record now supplies a later local revision after implementation f54; its actual canonical identifier and committed-base transition result will be recorded after creation, before independent review.

A separate existing browser capability probe passes 3/3 on implementation f54: Chromium/Firefox replace authored red/green with black/white; Linux WebKit leaves red/green while reporting forced-colors active. This records the current platform limitation, not a passing WebKit system-palette gate. Exact corrected invocation from `tests/e2e`: `PLAYWRIGHT_SKIP_WEBSERVER=1 FC_E2E_OUTPUT_DIR=/tmp/fc-story13-6-approved-parent-wfbpz9j_/palette FC_E2E_HTML_REPORT_DIR=/tmp/fc-story13-6-approved-parent-wfbpz9j_/palette-html npx playwright test specs/responsive-accessibility.spec.ts --grep 'forced-color replacement capability is recorded for its engine and run' --workers=1 --retries=0`. The first anchored title selector matched no tests (exit 1, before browser assertions); corrected selection executed all three engines without retries. Reports and both selector outcomes remain in parent scratch. Primary manual waiver, untouched checkboxes/status and independent owner gates are preserved.

## File List

- `references/Hexalith.Builds` - Adopt the exact approved catalog/guard/audit successor; owning three-file delta is independently reviewed.
- `eng/dependency-graph-policy.json` - Rebind only exact root Builds source and selector seal.
- `_bmad-output/implementation-artifacts/spec-13-6-repair-root-catalog-fallback.md` - Approved bounded plan, implementation, activation and review record.
- `_bmad-output/implementation-artifacts/spec-13-6-correct-parties-shared-catalog-inheritance.md` - Explicitly record the Owner's frozen-scope amendment and eventual E1/B3 disposition.
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` - Pre-existing reconciliation preserved unstaged; parent will append current final disposition.
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md` - Pre-existing parent story preserved unstaged; parent will append final disposition without changing acceptance/status.
- `_bmad-output/implementation-artifacts/epic-13-context.md` - Pre-existing regenerated planning context preserved unstaged.
