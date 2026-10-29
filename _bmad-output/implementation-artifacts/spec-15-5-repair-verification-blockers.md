---
title: 'Story 15.5: Repair Verification Blockers'
type: 'bugfix'
created: '2026-10-29'
status: 'done'
route: 'oneshot'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: '2fcb1e9b5802ed32fc387680c6e42369130e0384'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="bounded repair explicitly requested by the user; prior completed specifications remain historical context">

## Intent

**Problem:** Current HEAD's Quality governance cannot read EventStore's exact selected catalog object from the root Builds object store. Testing's package-boundary checks expect localization 10.0.13 although the selected catalog supplies 10.0.14.

**Approach:** Extend the existing dependency-graph engine with explicit, bounded catalog-object acquisition from the active base policy's canonical Builds identity before Quality governance. Reuse committed depth-one/depth-two edge collection and exact-object verification; ordinary validation remains read-only and fails closed. Update only Testing's expectation to 10.0.14. Run focused regressions, dependency governance, actual clean-consumer checks and the existing seven-library ApiCompat lane. Append exact outcomes to the existing handoff and ledgers. Preserve dependency selections, catalogs, gitlinks, guards, Git history, original story baseline and all independent approvals. Never initialize nested submodules. Keep the concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c strict attribution failure explicit.

</frozen-after-approval>

## Implementation Notes

- This new bounded continuation follows the completed activation-handoff spec; its original baseline 7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6 and frozen intent remain unchanged. The new baseline records this repair's starting source, not a replacement for prior attribution.
- Intent gaps: none. Irreversibles: none; no history rewrite, dependency update, remote write or nested checkout. Footprint: existing Python graph engine/tests, Quality preparation, CI governance assertion, one Testing expectation, append-only handoff/ledgers and this continuation. No runtime or public library API changes.
- Reuse `_owner_edges`, canonical identity URLs, bounded network retry and exact commit/blob checks. Gather and validate both base/candidate inventories before any fetch, deduplicate only Builds selectors, use the root Builds store, and refuse zero-base or untrusted mappings. No automatic networking in graph collection, semantic validation or C# tests.
- Initial root/hosted main: 2fcb1e9b5802ed32fc387680c6e42369130e0384; clean main. CI 40921392978 attempt 1 fails Testing's two recorded cases; Quality 40921392023 attempt 1 job 123615929938 fails the catalog lookup and skips ApiCompat Gate 4. These hosted observations do not establish results for the repair.
- Root Builds e38700b3a8de8ef57342688e9dfbbe3f573201ff stays selected. Root EventStore 71829366dd34406119f80d7a37a30ac48e9ed48c selects missing Builds a9455fb3b57a65233020f4b3fc61426514521d7c. Ordinary graph validation reproduced exit 1 before acquisition.
- The original strict command against concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c reproduced exit 1: story-id=no-match, disposition=unmapped, four listed-unowned delivery paths. No exemption or acceptance is granted.
- Scratch logs: `/tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/`. Sprint Story 15.5 and REL-BASE-1 already remain at review; monotonic synchronization preserves them.

## File List

- `eng/dependency_graph.py`
- `tests/eng/test_dependency_graph.py`
- `.github/workflows/quality.yml`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs`
- `_bmad-output/implementation-artifacts/spec-15-5-repair-verification-blockers.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`

## Verification

All runs use the local repair worktree rooted at
`2fcb1e9b5802ed32fc387680c6e42369130e0384`, with unchanged production sources,
dependency selections and policy. SDK 10.0.403 satisfies global.json's 10.0.402
latestPatch policy; runtime is 10.0.14. Logs are in the scratch directory above.
Release builds below reproduce the package-based CI lane required for the
package/ApiCompat checks; no source-mode topology acceptance is claimed.

- `dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`
  — exit 0, zero warnings/errors. Its initial built boundary class reproduces
  5 total / 2 failed (localization diagnostic and consumer precondition).
  After the one-line correction,
  `dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release --no-restore --nologo -m:1`
  exits 0, zero warnings/errors.
- `dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests`
  — final exit 0: 5 passed, zero failures/errors/skips/unrun. The real clean
  consumer packs four packages, restores and compiles with local package/fallback
  sources and 10.0.14, and checks package-only assets. Its generated smoke test
  is compiled, not executed by this existing test.
- `dotnet build tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`
  — exit 0, zero warnings/errors.
  `dotnet tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests.dll -class Hexalith.FrontComposer.Contracts.UI.Tests.PackageBoundaryTests`
  — exit 0: 4 passed, zero failures/errors/skips/unrun, including its actual
  temporary consumer build/executable run.
- `python3 -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests -v`
  — exit 0: 26 passed in 28.704s, including six new acquisition regressions.
  They retain missing-object refusal before acquisition, exact commit/catalog
  verification, canonical remote use despite an untrusted origin, idempotence,
  unchanged checkout/catalog bytes and absent nested initialization; candidate
  self-authorization, zero base, unavailable refs, false fetch success and edge
  limit overflow are rejected.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py tests/eng/test_validate_contract_artifacts.py eng.tests.test_validate_story_artifacts`
  — exit 0: 421 run, 419 passed, 2 skipped in 115.015s. These include the focused
  graph cases above. Expected negative-fixture diagnostics are retained in the log.
- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`
  — exit 0: 60 passed in 4.125s.
- `python3 eng/dependency_graph.py --root . acquire-catalogs --event-base a2ed597bfbf3e178928144963ad8c9cb63077b3c --candidate 2fcb1e9b5802ed32fc387680c6e42369130e0384`
  — exit 0, seven distinct exact catalog revisions prepared; only the missing
  a9455fb3b57a65233020f4b3fc61426514521d7c required network acquisition. The actual
  Quality preparation shell block, extracted verbatim into
  `quality-catalog-preparation.sh`, also exits 0 with EVENT_NAME=push,
  PUSH_BEFORE=a2ed597bfbf3e178928144963ad8c9cb63077b3c,
  PULL_REQUEST_BASE empty and CANDIDATE=2fcb1e9b5802ed32fc387680c6e42369130e0384.
  Builds HEAD remains e38700b3a8de8ef57342688e9dfbbe3f573201ff, worktree clean,
  catalog SHA-256 03632e3d36f63806e757a618f72ecc887aeacaefc1725750e706ebf88c4526cf;
  nested initialized Git entries remain empty.
- `python3 eng/dependency_graph.py --root . validate --commit 2fcb1e9b5802ed32fc387680c6e42369130e0384`
  — after acquisition exit 1: `consumer projection root source selectors changed`.
  `python3 eng/dependency_graph.py --root . diff --event push --event-base a2ed597bfbf3e178928144963ad8c9cb63077b3c --candidate 2fcb1e9b5802ed32fc387680c6e42369130e0384`
  — exit 1, same guard. This supersedes the missing-object diagnostic without
  claiming graph acceptance. The committed projection seals selectors hash
  ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef and
  Builds 81225bcb6151c38ae891abd9110af17385e1b3b9; current committed selectors hash
  fabdab5dcd3946836a4fbd519875f58bf5ba374b96f1c071f15201cf9c428a38 and selected
  Builds e38700b3a8de8ef57342688e9dfbbe3f573201ff disagree. No seal is refreshed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`
  — exit 0, zero warnings/errors.
  `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests`
  — exit 1: 106 total, 104 passed, 2 failed, zero errors/skips/unrun in 40.681s.
  CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds
  reports the selector-seal guard above. GovernanceBuildTrait_IsDeclaredOnlyInTheProjectTheHeavyLaneRuns
  encounters the pre-existing self-link `tests/e2e/node_modules/node_modules`
  and reports `Too many levels of symbolic links`. The changed Gate2b workflow
  assertion passed. No local dependency folder or unrelated guard is changed.
- Executed the existing Quality Gate 4 run block verbatim with
  `GITHUB_WORKSPACE=/home/administrator/projects/hexalith/frontcomposer bash /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/quality-gate4.sh`
  — exit 0, all seven inventory-derived library packs passed in 22.391s. Each
  exact command is `dotnet pack <inventory-project> --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/home/administrator/projects/hexalith/frontcomposer/artifacts/library-package-validation/<project-name>/bin/`.
  No bypass or new suppression is used. This is a local execution of the existing
  lane; hosted Gate 4 remains skipped at the observed source.
- `dotnet pack src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/home/administrator/projects/hexalith/frontcomposer/artifacts/library-package-validation/Hexalith.FrontComposer.Cli/bin/`
  — exit 0. Tool-layout packaging is additional to the seven ApiCompat libraries.
- Initial `python3 scripts/validate-nuget-packages.py ./nupkgs-validation` exits 1:
  eight packages expected, 15 found because seven old 4.4.0-ci packages already
  exist. Preserve those unrelated artifacts. Copy the eight inventory-selected
  4.7.1-ci nupkgs byte-for-byte into the fresh scratch `packages/` directory;
  SHA-256 comparisons confirm unchanged copies.
- `python3 scripts/validate-nuget-packages.py /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/packages`
  — exit 0: all eight packages and kernel boundary validated.
- `python3 scripts/validate-consumer-package-references.py /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/packages --work-directory /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/clean-consumers`
  — exit 0 in 4.077s: both fresh PackageReference-only consumers restored/built,
  zero warnings/errors; Contracts-only has no Blazor/Fluent/Fluxor runtime output.
  The shared consumer script compiles these programs; execution is not claimed.

### Hosted scope and preserved attribution

The original published-baseline spec retains baseline
2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb; the activation handoff retains
7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6. Neither is moved by this new repair spec.

Read-only rechecks use the GitHub connector's `github_fetch` with
`https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/git/ref/heads/main`
and
`https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs?head_sha=2fcb1e9b5802ed32fc387680c6e42369130e0384&per_page=15`.
Hosted main remains 2fcb1e9b5802ed32fc387680c6e42369130e0384. Initial CLI queries
before any changes agreed. The connector's `github_fetch_workflow_run_jobs`
used run IDs 40921392978 and 40921392023; `github_fetch_workflow_job_logs` used
jobs 123615256106 and 123615929938, all in Hexalith/Hexalith.FrontComposer.

- [CI 40921392978, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392978/attempts/1)
  remains failed on the two old Testing cases.
- [Quality 40921392023, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40921392023/attempts/1)
  remains failed on the missing catalog object; hosted ApiCompat Gate 4 is skipped.

No new hosted run is claimed. Local package/ApiCompat success does not pass the
live dependency guard, repair independent approvals, or close the original
hosted closure criteria. Story 15.5 and REL-BASE-1 remain at review, with
Release Owner, publication, Fluent, Product and EventStore approvals unchanged.

`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md --candidate a2ed597bfbf3e178928144963ad8c9cb63077b3c`
— exit 1, reproduced before implementation: `story-id=no-match`,
`disposition=unmapped`, and all four delivered documents `listed-unowned`.
This repair's separate scope accounting must never be reported as a passing
strict attribution of that concurrent commit. No human disposition is inferred.

### Validation and preservation checks

- The exact committed pre-repair engine was exported with
  `git show 2fcb1e9b5802ed32fc387680c6e42369130e0384:eng/dependency_graph.py`
  to scratch `dependency_graph-head.py`. Running
  `python3 /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/dependency_graph-head.py --root /home/administrator/projects/hexalith/frontcomposer validate --commit 2fcb1e9b5802ed32fc387680c6e42369130e0384`
  after acquisition also exits 1 with `consumer projection root source selectors changed`.
  This confirms the newly reachable guard failure predates the repair.
- `python3 eng/validate-story-artifacts.py` and
  `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-repair-verification-blockers.md`
  — both exit 0. These checks validate the new repair's scope; they do not
  reclassify the original concurrent delivery.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
  — exit 0, documentation validation passed. No fresh DocFX/snippet compilation
  is claimed.
- `git diff --check` — exit 0. All nine owned files use CRLF; the original
  activation handoff, release ledger and deferred ledger retain complete
  historical prefixes after repository newline normalization. No dependency,
  policy, gitlink, baseline configuration or independent gate status is changed;
  initialized nested Git entries remain empty.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/fc-15-5-ci-repair-20261029-Bfm7Wx6a/commit-message.txt --verbose`
  — exit 0, zero problems/warnings. Installed CLI 21.2.2 matches package-lock.
  The validated candidate is retained in local scratch before any commit.

## Review Triage Log

Blind Hunter ran independently at the parent model level. The oneshot route
skipped Edge Case Hunter and Verification Gap. All seven findings were checked
against the actual code; all were patched, with no additional review work deferred.

- BH-1 — medium / patch: a missing base-only root Builds object prevented reading
  its depth-two inventory. Preparation now validates both root mappings against
  the active-base policy, acquires only approved exact root Builds prerequisites,
  then validates both full bounded inventories before acquiring other selectors.
  This refines the initial Implementation Notes plan: full inventories cannot
  precede fetching a missing root object that contains the inventory. Existing
  offline validation, active-policy, identity and resource guards remain intact.
- BH-2 — medium / patch: manual dispatch could select an unmerged branch's policy.
  Require SELECTED_REF=refs/heads/main before treating CANDIDATE as a landed base;
  unsupported/manual branch or tag refs fail before acquisition.
- BH-3 — low / patch: the success fixture previously used identical revisions.
  It now uses distinct base/candidate root Builds selectors with a missing
  base-only object shared by a nested selector, and asserts exactly one fetch.
- BH-4 — low / patch: candidate self-authorization refusal previously had warm
  catalog objects. The fixture now withholds the base's nested catalog object,
  proving rejection of untrusted candidate mappings before any network call.
- BH-5 — medium / patch: actual committed absent, directory and oversized catalog
  fixtures now exercise the content/type/size guards after commit verification.
- BH-6 — medium / patch: uninitialized root Builds and mismatched root gitlink
  fixtures now assert refusal before networking.
- BH-7 — medium / patch: execute the actual extracted Quality shell block with a
  stub acquisition command for PR, push and manual-main events; verify full argv
  and exact GitHub env bindings. Manual unmerged/tag refs and unknown events are
  refused before invoking the stub. Shell substring checks remain supplemental.

### Review-patch validation

`python3 -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests tests.eng.test_dependency_graph.QualityCatalogPreparationWorkflowTests -v`
— exit 0: 32 passed in 11.033s (29 projection/catalog and 3 workflow cases).

`dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*Gate2bGovernanceStep_IsNotMarkedAdvisory'`
— exit 0: 1 passed, zero failures/errors/skips/unrun.

`actionlint -oneline .github/workflows/quality.yml` — exit 0.

Final explicit acquisition again exits 0. Ordinary graph validate and the
actual-parent push diff again exit 1 with the unchanged selector-seal guard.
The final Python suite and final scope/documentation checks are recorded below.

Final run of the same seven-module Python dependency/guard command above exits
0: 427 run, 425 passed and 2 skipped in 108.401s. `dependency-governance-final.log`
retains expected negative-fixture diagnostics separately from the successful
runner result. Final acquisition/validate/diff results are retained in the
corresponding `*-final.json` logs. Production/package inputs were unchanged by
review; the seven-library ApiCompat, eight package validations and actual clean
consumer results above remain applicable, without another pack run.

This bounded repair and its independent review are complete. The original Story
15.5 and REL-BASE-1 remain at review; live graph/governance, hosted CI/Quality,
original concurrent strict attribution and independent approval gates retain
their explicit failed, skipped or open dispositions. No new review fix is deferred;
the existing selector-policy and workspace/hosted follow-ups remain handed off.
