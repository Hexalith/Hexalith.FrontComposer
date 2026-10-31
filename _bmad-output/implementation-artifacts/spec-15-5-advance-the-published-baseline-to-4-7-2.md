---
title: 'Story 15.5: Advance the Published Baseline to 4.7.2'
type: 'chore'
created: '2026-10-31'
status: 'done'
route: 'dispatch'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: '7e25d2b52a0becf265b7904cc157b42c7bff5b1d'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Advance existing compatibility baseline sites and focused assertions from 4.7.1 to exactly 4.7.2. Immutable v4.7.2 publishes eight packages from 085026328a6ae6f9818b9529e7b79db374e1f055; Release 41111778787 and independent Release Evidence 41115667970 succeeded at attempt 1. Preserve the completed 4.7.1 spec and frozen amendment.

## Boundaries & Constraints

Reuse existing policy, inventories, package-boundary tests and ApiCompat. Only the consumer configuration seal may change with the two baseline XML inputs. Preserve selections, other seals, guards, contracts, the empty suppression ledger and history. No dependency update, new scanner, evidence bundle, publication, push, merge or approval.

Candidate validation cannot authorize its prior-policy event. Retain actual-base failures, including the Platform refusal. The merged precursor remains unreviewed; owner, publication, Fluent, Product and EventStore approvals remain separate. Keep Story 15.5 open while required transition gates remain unresolved.

</frozen-after-approval>

## Code Map

- `eng/release-package-inventory.json` defines eight packages and seven library ApiCompat targets; `.github/workflows/quality.yml` supplies the existing Gate 4 commands.
- `eng/dependency_graph.py` supplies `consumer_configuration`, `canonical_digest`, committed validation and event-base diff; reuse these without changing admission behavior.

## Tasks & Acceptance

**Execution:**

- [x] `eng/release_compatibility.py`, `Directory.Build.targets`, `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj` — advance active baseline sites to 4.7.2; preserve unrelated historical/synthetic literals.
- [x] `.github/workflows/quality.yml` — align consumer/pack coordinates to 4.7.2 without changing blocking gates.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`, `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`, `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`, `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs` — align active baseline, candidate and binary expectations; retain missing-baseline failures.
- [x] `eng/dependency-graph-policy.json` — derive only the configuration seal from exact committed candidate inputs; compare every other policy value and root selector with the preserved baseline.
- [x] `docs/diagnostics/README.md` — explain the current 4.7.2 technical baseline and link its new ledger observation.
- [x] `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`, `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md` — append dated source, package and attempt-specific results; distinguish local/hosted proof and approvals.
- [x] `_bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md` — record verification, revisions, review disposition and actual changed File List.

Update `_bmad-output/implementation-artifacts/sprint-status.yaml` only when actual evidence supports a workflow transition.

**Acceptance Criteria:**

- Given the immutable release and eight NuGet packages, when the baseline is selected, then all active sites agree on 4.7.2 and retain the published source/run identities.
- Given unchanged contracts and suppression policy, when existing compatibility and package-boundary checks run, then seven library packs and focused assertions pass without a new exception or skipped baseline.
- Given prior-policy drift, when an actual-base transition is checked, then refusal remains a failure; candidate diagnostics and later evidence cannot relabel it or approve the unreviewed precursor.

## Implementation Notes

- User approved and continued on 2026-10-31; the frozen block is unchanged. The preserved starting HEAD remains 7e25d2b52a0becf265b7904cc157b42c7bff5b1d. Sprint and Epic 15 were already in-progress, so synchronization made no change.
- Before implementation, `aspire start --isolated --non-interactive --apphost src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj` exited 0 after 82.4 seconds. `aspire describe --apphost /home/administrator/projects/hexalith/frontcomposer/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --format Json --non-interactive` exited 0 and captured 32 resources during startup: security was Unhealthy and dependent services were Waiting; this is not full runtime acceptance. `aspire stop --apphost /home/administrator/projects/hexalith/frontcomposer/src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --non-interactive` exited 0 before test builds. Other running AppHosts were preserved. Exact command results and logs are local at `/tmp/fc-15-5-472-baseline-cpku162k/`.

## Verification

Run `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish tests.eng.test_dependency_graph` and the existing DiagnosticRegistryTests, InfrastructureGovernanceTests, CiGovernanceTests, Contracts.UI/Testing PackageBoundaryTests and McpRuntimePackageBoundaryTests in their individual projects. Use Debug for local checks; use the existing Release Gate 4 pack lane for package-only compatibility. Run documentation and global/focused story-artifact validators plus `git diff --check`. Record exact commands/results and use the repository fallback ladder for blocked builds.

Run ordinary dependency validation and the actual event-base diff with full canonical commit IDs. Keep historical/current refusals distinct from candidate validation. Bind hosted observations to exact source/attempt.

## File List

- `eng/release_compatibility.py`
- `Directory.Build.targets`
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
- `.github/workflows/quality.yml`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
- `tests/eng/test_pack_release_packages.py`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs`
- `eng/dependency-graph-policy.json`
- `docs/diagnostics/README.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
- `_bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/deferred-work.md`


## Implementation Verification — 2026-10-31

The preserved root baseline is `7e25d2b52a0becf265b7904cc157b42c7bff5b1d`.
SDK 10.0.403 and runtime 10.0.14 were used. The active policy constant, two XML
baseline sites, Quality pack/consumer coordinates and four focused assertion
files now agree on 4.7.2. MCP candidate/binary identities are
`4.7.2-review.compat` / `4.7.2.0`; its missing-baseline negative assertion is
unchanged. Historical and synthetic 4.7.1 literals elsewhere are preserved.

Read-only API and inventory-derived NuGet observations establish immutable
release `429411190` / v4.7.2, published `2026-10-30T19:28:01Z`, source
`085026328a6ae6f9818b9529e7b79db374e1f055`, eight latest-stable 4.7.2 packages
with HTTP 200 and that exact nuspec repository commit, successful
[Release 41111778787 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41111778787/attempts/1)
and successful independent
[Release Evidence 41115667970 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41115667970/attempts/1).
The ledger records the exact API/flat-container input patterns and package rows.
This is fresh metadata/nuspec proof, not a repeated signature/member audit.

### Committed configuration and admission checks

`consumer_configuration(root, commit, policy)` and `canonical_digest` from the
unchanged existing graph engine enumerate 39 exact committed configuration
blobs. A temporary unreferenced XML-input object
`7355c600cbef4e7893f52d9562ce91e261b8f478` contains precisely the two baseline XML
changes. Its derived seal is
`c05817271c10b796ad652a868ca59a5bf076f36584acfb2190a821f24070a9be`, replacing
`576787166037b08cb25c3c21ae06c1135d7573ba8a47520c4b06208349841484`.
A deep JSON comparison against baseline policy proves this is its only changed
value. All nine root gitlinks, root-selector digest
`33d945a7f93d45f238dd749d0412c9823b85e6bae4d0266b8151fdf5a6f180bf`, Platform
owner/inventory/source seals, catalog selections, trust/expectations and guards
are unchanged. The empty suppression ledger, contracts, completed 4.7.1 spec
and frozen amendment are preserved.

A second unreferenced technical candidate
`67b483831d2104203379c19ae1f1559c4064e2ae` contains all nine code/policy paths
and those unchanged baseline gitlinks. It deliberately excludes documentation
and concurrent worktree changes; its configuration seal matches the final XML
bytes. Git objects live only in scratch `objects/` with read-only alternates for
the root and nine root-declared submodule object stores. The real index, branch,
HEAD and repository history are untouched. The diagnostic object's full message
was accepted by `node_modules/.bin/commitlint --edit <scratch>/candidate-message.txt --verbose`
(exit 0; successful output retained in `candidate-message-commitlint.log`).
No commit or proposed commit message is presented as delivery.

- `python3 eng/dependency_graph.py --root . validate --commit 7e25d2b52a0becf265b7904cc157b42c7bff5b1d`
  — exit 0, eight selectors validated under its own committed policy.
- With `GIT_OBJECT_DIRECTORY=<scratch>/objects` and
  `GIT_ALTERNATE_OBJECT_DIRECTORIES` containing the absolute root plus nine
  root-declared submodule object directories,
  `python3 eng/dependency_graph.py --root . validate --commit 67b483831d2104203379c19ae1f1559c4064e2ae`
  — exit 0, eight selectors. This is candidate diagnostics only.
- With the same read-only object lookup environment,
  `python3 eng/dependency_graph.py --root . diff --event push --event-base 7e25d2b52a0becf265b7904cc157b42c7bff5b1d --candidate 67b483831d2104203379c19ae1f1559c4064e2ae`
  — exit 1, `consumer Compile/ProjectReference configuration changed from its sealed source projection`.
  No candidate object is used as an event base.
- `python3 eng/dependency_graph.py --root . diff --event push --event-base 085026328a6ae6f9818b9529e7b79db374e1f055 --candidate 7e25d2b52a0becf265b7904cc157b42c7bff5b1d`
  — exit 1, `github.com/hexalith/hexalith.platform@097d663f79f409f723fc1d2bd0609d46be4a417a: consumer source projection owner pin changed`.
  This is the real previous parent event; its failure and the precursor's absent
  owner review are not repaired by ordinary candidate validation.

Initial scratch candidate invocations omitted submodule object stores from the
Git alternate environment and failed before semantic validation with `fatal: not a tree object`.
Correcting that local lookup setup produced the results above without fetching,
changing selectors, checking out dependencies or altering policy admission.
Exact environment paths remain in `configuration-seal.json` under scratch.

### Local tests and package compatibility

- `python3 -m unittest tests.eng.test_pack_release_packages tests.eng.test_release_prepublish tests.eng.test_dependency_graph`
  — exit 0, 202 tests passed in 21.454s; no skips.
- Each of the following projects was built individually with
  `dotnet build tests/<project>/<project>.csproj --configuration Debug --nologo -m:1 -p:NuGetAudit=false`:
  `Hexalith.FrontComposer.SourceTools.Tests`, `Hexalith.FrontComposer.Shell.Tests`,
  `Hexalith.FrontComposer.Contracts.UI.Tests`, `Hexalith.FrontComposer.Testing.Tests`
  and `Hexalith.FrontComposer.Mcp.Tests`. All exit 0 with zero warnings/errors.
  These are local Debug project-reference builds; no blocked build/fallback is
  relabeled as passing.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests`
  — exit 0, 98 passed, zero failures/skips.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests`
  — exit 0, 107 passed, zero failures/skips.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests.dll -class Hexalith.FrontComposer.Contracts.UI.Tests.PackageBoundaryTests`
  — exit 0, four passed, including the clean package consumer build/run.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests`
  — exit 0, five passed, including the clean package-only consumer compilation.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -class Hexalith.FrontComposer.Mcp.Tests.Skills.McpRuntimePackageBoundaryTests`
  — exit 0, two passed; cold/warm 4.7.2 baseline packs and the required missing
  baseline refusal ran. Candidate package and assembly expectations are checked.
- The exact existing `.github/workflows/quality.yml` Gate 4 `run` block was
  extracted unchanged to `<scratch>/gate4.sh` and run with
  `GITHUB_WORKSPACE=/home/administrator/projects/hexalith/frontcomposer bash <scratch>/gate4.sh`.
  It exits 0 and produces exactly seven `4.7.2-ci` library nupkgs and their
  symbols under the existing inventory-driven lane, with
  `EnableFrontComposerPackageValidation=true`, baseline `4.7.2` and
  `FrontComposerPackageValidationSkipBaseline=false`. Each pack has its own
  existing `BaseOutputPath`; the CLI's documented SDK tool exception is unchanged.
  Release pack validation is package-reference based and remains local evidence.

The concurrent source checkout observations below limit attribution of Debug
project-reference runs: they are local worktree evidence, not a claim that a
clean checkout of the technical candidate was built. They do not affect the
committed-object seal/root-selector comparison or authorize different selections.

### Hosted source-specific posture and open gates

At `2026-10-31T06:47:22Z`, starting source
`7e25d2b52a0becf265b7904cc157b42c7bff5b1d` has
[CI 41176684664 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41176684664/attempts/1)
completed/failure. Build/test job `124465587169` passed; dependency-governance
job `124466918079` failed the exact Platform owner-pin refusal. Hosted
[Quality 41176684504 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41176684504/attempts/1)
is still in_progress overall with no conclusion. Build job `124465587020` is
completed/failure: catalog preparation failed the same owner pin and skipped
infrastructure and ApiCompat Gate 4. The successful publication/verifier runs
belong to 085026 source; local passing packs are not hosted candidate CI proof.

The merged precursor remains unreviewed. Actual transition gates remain failed;
owner, publication, Fluent, Product and EventStore approvals stay separate.
Story 15.5 and Epic 15 were already in-progress and remain so; REL-BASE-1's
separate review action is preserved. No sprint-status edit is justified or made.
No dependency update, evidence bundle, publication, push, merge, activation or
approval was performed.

### Documentation, artifacts and review disposition

`pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
exits 0. Full documentation, final global/focused story-artifact validation and
hygiene results are recorded after final execution below. Independent review is
pending the calling build workflow; this implementation record grants no owner
review or approval and leaves status in-progress.

Raw output, read-only API responses, downloaded package scratch and exact Git
object/environment receipts remain at
`/tmp/fc-15-5-472-implementation-9c0s47dg/`; no evidence files are committed.

## Documented Unrelated Changes

Concurrent external checkout movement was observed and preserved. During initial
Debug verification, root worktree gitlinks temporarily differed as follows:

| Path | Preserved baseline selection | Observed external worktree selection |
| --- | --- | --- |
| references/Hexalith.Builds | 849b0be6849f14c097ab05ff9bafbc115bdabe7e | ff1c2952fa6aec113975bbe78b4d2487d6ec0b3d |
| references/Hexalith.EventStore | e014af6c3f1c67732485f24012cdbe528a12b0f6 | 211fe67676a89c969eb8821798b52c7be124dc6b |
| references/Hexalith.Memories | 71a721a87ebbb34c557c08517b5c39884a7417da | 2537cad34e173d61d6bffad9e5c20cbe47cdd9d2 |
| references/Hexalith.Parties | 6b8f0f8637c9a3bc7596794e2f819e5015e40e8e | a8b2549cfacc43d75373a6e16ad372de525ab309 |
| references/Hexalith.Tenants | e45ed9d87de7800ebb02b1cedc9c64b720763e9e | 28cc8f159a8109de519abe0b2fc2ee4d5a6c7979 |

A subsequent read-only snapshot found all nine worktree coordinates equal to the
baseline tuple, with no checkout/update by this task. Later Platform independently
moved again and had staged changes; these belong to the external workspace work:

- `references/Hexalith.Platform` — Concurrent external checkout and staged changes are outside this task and preserved.
  The initial observed checkout was `eb6e478cd8969c22619499e89b726d147d71a3a4` with staged `README.md`,
  `eng/timesheets-deployment/README.md` and `global.json` changes. Preserved without
  staging, restoration or edits; excluded from the technical candidate and owned
  File List. Its baseline selection remains
  `097d663f79f409f723fc1d2bd0609d46be4a417a`.

A still-later read-only snapshot observed Platform worktree
`a8922b46ae272ef317d6b867f4fae68ec268b9d9` with independently staged changes.
That evolving external checkout is preserved and remains unrelated; its full
status and all nine root dependency coordinates are retained locally in
`final-worktree-dependency-coordinates.json`. No result above selects that
worktree revision as the committed candidate or changes its sealed gitlink.


These externally committed paths are preserved and are not owned by this story:

- `package.json` — External 4dfc5390 npm/tooling update; preserved.
- `package-lock.json` — External 4dfc5390 lock update; preserved.
- `src/Hexalith.FrontComposer.Shell/Components/Layout/FcThemeToggle.razor` — External 4dfc5390 accessibility fix; preserved.
- `tests/Hexalith.FrontComposer.Shell.Tests/Components/Layout/FcThemeToggleTests.cs` — External 4dfc5390 accessibility tests; preserved.
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package.json` — External 4dfc5390 tooling update; preserved.
- `tests/Hexalith.FrontComposer.Shell.Tests/EndToEnd/package-lock.json` — External 4dfc5390 tooling update; preserved.
- `tests/e2e/package.json` — External 4dfc5390 tooling update; preserved.
- `tests/e2e/package-lock.json` — External 4dfc5390 tooling update; preserved.


## Final Verification and Implementation Handoff

- Initial `pwsh -NoProfile -File eng/validate-docs.ps1` exited 1 at
  `dotnet docfx metadata docs/docfx.json`: `Run "dotnet tool restore" to make the "docfx" command available.`
  This was a local tool-registration prerequisite, not a documentation pass.
- `dotnet tool restore` exited 0 and restored exactly the existing manifest's
  DocFX 2.81.0 and dotnet-stryker 5.0.0. No manifest/version/dependency change.
- Repeating `pwsh -NoProfile -File eng/validate-docs.ps1` exited 0. Its manifest
  records DocFX 2.81.0, all eight compile snippets passed and no blocking failures;
  full solution build, API metadata generation and DocFX site generation ran.
- `python3 eng/validate-story-artifacts.py` — exit 0.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md`
  — exit 0; external Platform worktree changes are explicitly unrelated.
- `git diff --check` — exit 0. Exact historical-prefix SHA-256 comparisons prove
  both append-only documents preserve their original bytes. Approved frozen
  block SHA-256 remains
  `10d786d8fbc7afb90346ebb54c3fdc99ea3594409332fa0f37cf8d27855d15a4`.
  The old completed 4.7.1 spec, suppression ledger and sprint-status file have
  no diff; policy comparison still changes only the configuration seal.

The focused artifact check initially rejected the checked record task because
its same line also named the optional unchanged sprint-status file. The record
task and conditional sprint instruction were separated outside the frozen block;
no fictitious sprint modification or File List entry was added. Final artifact
checks use the actual 13-path owned File List. Independent implementation review
remains for the calling workflow; no protected owner review is claimed.

Implementation is handed back with all requested local technical checks passing.
The actual-base configuration and historical Platform refusals remain exit 1,
current-source hosted gates remain as dated above, and Story 15.5 stays open.


## Review Triage Log

### Independent implementation review — 2026-10-31

Three context-free reviewers ran at the parent model capability: Blind Hunter,
Edge Case Hunter and Verification Gap. No layer was skipped. The complete review
diff was 51,228 bytes; Blind Hunter's finding floor was eight. Every finding was
classified individually before grouping. Edge Case Hunter returned no findings.
This implementation spec is now in-review; the encompassing Story 15.5 stays
in-progress, and its unresolved transitions/independent decisions remain open.

- BH-1 — medium / patch: the real-tree production-plan case still supplies 4.7.1 at `tests/eng/test_pack_release_packages.py:115`. Its intent is a packable current production candidate, unlike the surrounding synthetic fixtures; align only this input with the active baseline and retain synthetic history.
- BH-2 — medium / patch: Quality Gate 4a's package coordinate changed, but only the seven preceding library packs were executed. Run the existing adopter smoke block against those 4.7.2-ci packages and record its result or exact blocker; no substitute fixture or new lane.
- BH-3 — medium / patch: Quality Gate 2a's baseline argument changed and the CLI is deliberately outside the seven-library lane. Execute the existing CLI packaging/local-tool smoke with CI shell failure semantics and record its result or exact blocker.
- BH-4 — false / reject: the finding would require clean-candidate Debug attribution which the record expressly disclaims. The approved verification requires local Debug checks plus separate committed-object validation; both ran, and every changed production input is covered by package-only Release packs. Moving external worktrees were preserved and their attribution limit remains explicit; no claim that they prove a clean candidate is made.
- BH-5 — low / reject: this requests a construction recipe inside this build's spec, so the workflow rejects that fix. The scratch identities are explicitly diagnostic, their exact parent and changed paths are recorded, the existing collector/digest implementation remains checked in, and no durable evidence packet is claimed. A real committed delivery, if reached, must receive its own actual-SHA validation rather than inherit scratch acceptance.
- BH-6 — medium / patch: the release ledger gives endpoint patterns rather than a directly runnable package-version/nuspec check and exact observation time. Append a bounded reproduction command with a timestamp, preserving its existing historical prefix and the distinction between availability/source checks and signature verification.
- BH-7 — low / reject: the requested change edits this build's spec. The implementation-phase in-progress observation is retained as history; this dated review entry records the later in-review state, independently of the still-open encompassing story.
- BH-8 — false / reject: these are exact recorded invocations from the actual checkout, including an exact-target Aspire start/stop amid other running AppHosts. They are not advertised as location-independent shell instructions; replacing their historical paths would reduce execution fidelity. The new ledger reproduction may use the current repository root without rewriting these receipts.
- VG-1 — medium / patch: pre-verified broken-verification gap. `QualityWorkflow_Gate4ValidatesEveryPackableLibrary` asserts YAML substrings but calls `EvaluatePackageValidation` with defaults, so a later duplicate override can select 4.7.1 without failing those assertions. Evaluate the real Gate 4 property arguments and assert the policy baseline, including a regression for the demonstrated later-override case. Reuse the existing governance test and MSBuild probe; no production API or alternate validator.

The five patch entries have distinct outcomes and remain separate. No intent
change, loopback, approval inference or deferred-work entry is required. The
implementation agent is re-engaged for the smallest corrections; final checks
follow after it returns.


### Review correction handoff and concurrent root advance — 2026-10-31

The implementation agent corrected BH-1 and VG-1 in the two existing test files.
The real-tree Python plan uses 4.7.2; the existing Gate 4 check evaluates ordered
actual Release properties and rejects the demonstrated later 4.7.1 override.
One focused Python case, three affected .NET cases and the Shell Debug build pass.
The existing Gate 2a block passes with CI bash failure semantics, including local
tool installation, help, inspect and both migration modes against its temporary
fixture. The ledger now retains exact smoke commands, timestamps and a runnable
eight-package observation. No production behavior or workflow guard was changed.

During that check, an external commit advanced root main from the preserved
baseline to `4dfc5390e117b601b88254304985f60d74981acd`. It changes eight unrelated
accessibility/npm paths; our staged files were preserved. The baseline frontmatter
is not rebased. Gate 4a correctly exited 1 with `package-candidate-mismatch`:
the seven earlier packages identify 7e25d2b5, while its command read the new HEAD.
That failed first smoke remains evidence. Final verification will use an isolated
local checkout with pinned root dependencies and newly source-aligned packages,
without weakening identity checks or moving the shared dependency worktrees.

The external lock now pins commitlint 21.2.4; the existing root installation is
21.2.2. New diagnostic/delivery messages must be validated with the new pinned
CLI in isolation, using the byte-identical tracked commitlint configuration.
The earlier 21.2.2 validation remains attributed to its then-effective baseline.


### Isolated verification prerequisites — 2026-10-31

A detached local clone froze external source 4dfc5390 and all nine approved root
dependencies, with no nested submodule initialization. Its 14 owned paths were
overlaid and committed only inside the disposable clone after pinned commitlint
21.2.4 validation. Initial diagnostic candidate
`e5ee2a807229c409636bb21f7461c35cd7aa4ac1` passed 202 Python and 98 diagnostic
cases. Its first 108-case governance run had two failures, preserved in local
logs: an unavailable ff1c2952 exact Builds catalog object, and the external npm
update's stale conventional-changelog assertions.

The exact ff1c2952 object already exists in the canonical local Builds repository.
A read-only alternate was added only to the isolated Builds object lookup; no
network fetch, selector, working dependency source or admission rule changed.
The existing analyzer-matrix test's two manifest/lock expectations were aligned
with the externally committed ^10.4.1 preset. Package manifests, lockfiles,
behavior matrix and all package selections remain untouched by this correction.
The affected Debug build and analyzer test pass. A fresh isolated candidate will
receive the full checks; the first failure is not relabeled as a passing run.


### Independent review closure and stable-source final verification — 2026-10-31

All five accepted review patches are resolved: BH-1 uses the current production
candidate; VG-1 evaluates actual ordered pack arguments and rejects a later old
baseline; BH-2/BH-3 now have passing adopter/CLI smoke executions; BH-6 has the
runnable timestamped package observation in the ledger. Rejected findings retain
their individual verdicts above. No review item is deferred or approval inferred.

Final technical candidate `ccf8250a5404aea3fca835de74d0a30858610364` is a detached,
clean, disposable local commit parented by external 4dfc5390. It contains the
14 owned changes and every approved root dependency at its preserved gitlink,
with no initialized nested submodules. The isolated npm install honors 4dfc5390's
lockfile and commitlint 21.2.4; shared local Git/NuGet caches mean this is not a
cold-network availability claim. Its full source/package tests are independent
of the moving shared worktrees. All 14 owning-workspace input hashes match this
tested snapshot before these result-only appendices.

The first isolated governance run's two failures remain recorded. The corrected
full run passes 202 Python cases plus 98 diagnostic, 108 infrastructure/CI,
four Contracts.UI, five Testing and two MCP cases: 419 total, zero failures/skips.
A fresh package-boundary run initially lacked the conventional Release outputs
its existing --no-build packs require. Normal Release builds of Contracts and
Testing supplied those package-fixture prerequisites; no assertion or gate was
weakened. The successful four/five boundary reruns follow that failed first run.

From the isolated checkout, the final commands are the same Python suite,
five individual Debug project builds and direct -class test invocations listed
above. The additional package prerequisites were:

- `dotnet build src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false` — exit 0.
- `dotnet build src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false` — exit 0.

The exact unchanged Gate 4, Gate 4a and Gate 2a run blocks were extracted to local
scripts and executed with `bash --noprofile --norc -e -o pipefail`, with
GITHUB_WORKSPACE set to the isolated checkout and RUNNER_TEMP to its private
runner directory. All seven fresh library packs pass ApiCompat with baseline
4.7.2 and skipping false. Gate 4a exits 0: package identity verifies against
ccf8250a, and projection_rendered, command_rendered, bootstrap_rejected and
empty_registry_rendered are all true. The earlier shared-worktree mismatch is
retained as a distinct failure. Gate 2a also exits 0 with a fresh local tool
manifest and a private empty NUGET_PACKAGES directory; the CLI package identifies
ccf8250a, and help/inspect/dry-run/apply complete against temporary fixtures.

- `python3 eng/dependency_graph.py --root . validate --commit ccf8250a5404aea3fca835de74d0a30858610364` — exit 0.
- `python3 eng/dependency_graph.py --root . diff --event push --event-base 4dfc5390e117b601b88254304985f60d74981acd --candidate ccf8250a5404aea3fca835de74d0a30858610364` — exit 1, unchanged sealed-configuration refusal. This is a local direct-parent simulation, not a hosted activation event.
- The historical 0850263 -> 7e25d2b5 command above still exits 1 on the Platform owner pin.
- `pwsh -NoProfile -File eng/validate-docs.ps1` — exit 0; full documentation, eight compiled snippets, DocFX 2.81.0 metadata/site generation.
- Global and focused `python3 eng/validate-story-artifacts.py` commands — exit 0.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md --candidate ccf8250a5404aea3fca835de74d0a30858610364` — exit 0; external 4dfc5390 is unrelated, the technical candidate owns exactly 14 paths, and the isolated workspace is clean.
- `git diff --check` — exit 0.

Exact expanded argv, timestamps, expected failure results, source coordinates and
logs are local at `/tmp/fc-15-5-472-final-0yc9auae/`, particularly commands.jsonl
and results-v2.json. The local orchestration script only runs the existing lanes;
no product scanner, evidence bundle or hosted proof is introduced. This proves
the bounded implementation locally; it does not approve the policy transition,
precursor landing or independent release/runtime decisions. Story 15.5 remains
in-progress. The original baseline, frozen blocks, old ledger/handoff prefixes,
suppression ledger, dependency selections and user changes are preserved.


### Workflow handoff — 2026-10-31

The bounded successor is done after independent review and successful local
verification. The workflow now advances the encompassing Story 15.5 from
in-progress to review, which remains an open state; it does not mark that story
or Epic 15 done. This supersedes only the earlier phase's in-progress tracking
observations. The required policy transition and separate approvals remain
unresolved exactly as recorded. The sprint synchronization is the fifteenth
owned path; the tested candidate's fourteen implementation/documentation paths
are unchanged except these result/tracking appendices. No compiled input,
configuration seal or guard changed after the clean candidate verification.

No findings were deferred. New source/selector work and policy activation still
require their separately authorized event/review path; no push or merge is part
of this delivery. A local delivery commit will be validated against the actual
parent and receive ordinary/strict checks at its actual canonical SHA. Its final
SHA cannot be embedded in its own contents; post-commit receipts remain local.


## Code review — 2026-10-31

Post-delivery review of the story commit
4dfc5390e117b601b88254304985f60d74981acd..eb1f0f7158b254dfacf460b2e30199ac8912944f
(82.007 kB; Blind Hunter floor min(floor(sqrt(82.007) + 1), 10) = 10). External
4dfc5390 is excluded. Four independent reviewers ran at the parent model
capability: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance
Auditor. At review time main was eacc218d431e198e1d48d83f3e4260c4dd53991b; the
rebased copy decbb195f1c010460530fde6b662a6118a83ee7c and merge
5cd01bd8b501a4f64c0997e966c964dccaa2eb71 carry the same content, and every listed
path at HEAD equals eb1f0f71 except the later re-sealed
`eng/dependency-graph-policy.json`. Every finding was verified against HEAD, the
hosted runs and the story validators before triage.

### Review Findings

- [x] [Review][Decision] Story 15.5 `review` transition is not supported by current evidence — sprint-status moved the story in-progress → review, but `spec-15-5-prepare-the-inactive-policy-precursor.md` is still `in-progress` with seven unchecked round-3 `[Review][Patch]` items, and the frozen intent keeps Story 15.5 open while transition gates remain unresolved. Hosted: CI 41182262969 for decbb195 (the seal change) was cancelled by `cancel-in-progress` when 05f01d72 was pushed; CI 41182438087 then used decbb195 as its event base (failing on its own `consumer projection root source selectors changed`), and CI 41183043376 (5cd01bd8) and 41184768444 (eacc218d) are green. The seal transition's actual-base check never completed — neither refused nor passed — and the new seal is now the active event-base policy. Options: return Story 15.5 to in-progress until the precursor patches and transition record are resolved, or keep `review` (an open state) and record the cancelled actual-base check as an open item. (Acceptance Auditor; Blind Hunter's companion claim that no admission route is recorded is refuted — the route lives in the precursor and policy-activation specs.) Resolved 2026-10-31: Story 15.5 returns to in-progress; this bounded 4.7.2 spec stays done. A local read-only replay of the cancelled event (`--event-base a9c66bfe… --candidate decbb195…`) exits 1 on the configuration seal, recorded in the ledger's `Story 15.5 code review: cancelled seal-transition check — 2026-10-31` entry; no hosted re-run was requested.
- [x] [Review][Patch] Gate 4 pack-argument collector drops overrides it cannot parse [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2081] — `(?<!\S)-p:` (line 2090) matches only the `-p:` spelling and `Single()` reads one physical line, so a `/p:`, `-property:`, `--property:` or `\`-continued override of `FrontComposerPackageValidationBaselineVersion` reaches ApiCompat while the test still evaluates 4.7.2 (pre-verified by Verification Gap). Join continuations, collect every property-switch spelling normalized to `-p:`, and add a `/p:` regression case beside `[InlineData("4.7.1")]`.
- [x] [Review][Patch] Real-tree hotfix test and production fixture still model a 4.7.1 candidate below the 4.7.2 baseline [tests/eng/test_pack_release_packages.py:299] — `validate_release_policy(ROOT, "4.7.1")` and its comment "`4.7.1` against published `4.7.0`" no longer describe the tree; `PRODUCTION_VERSION = "4.7.1-review.compat"` (line 40) is paired with the 4.7.2 `PUBLISHED_BASELINE_VERSION` while the MCP lane moved to `4.7.2-review.compat`. Use a `4.7.3` hotfix candidate against published `4.7.2` and align the fixture candidate.
- [x] [Review][Patch] VG-1 theory asserts indirectly and duplicates its property substitution [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2106] — `Should.Throw<ShouldAssertException>(() => baseline.ShouldBe(...))` is `ShouldNotBe`, and the `$GITHUB_WORKSPACE`/`$name` substitution block is copied into the library and tool loops, so a new placeholder can be added to one loop and not the other.
- [x] [Review][Patch] Published diagnostics page exposes undefined internal process terms [docs/diagnostics/README.md:9] — "prior-policy refusals, precursor review" name internal BMAD records adopters cannot resolve; keep the technical-baseline statement and the existing separately-owned disclaimer.
- [x] [Review][Defer] CI cancel-in-progress can erase a policy-changing push's dependency-governance verdict [.github/workflows/ci.yml:16] — deferred: pre-existing concurrency setting; observed on decbb195 (run 41182262969 cancelled, the next event used it as its base). The fix touches a release-chain-pinned workflow and needs its own authorized change.
- [x] [Review][Defer] Focused strict validation of this spec exits 1 at HEAD [_bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md:65] — deferred: pre-existing; six later unrelated commits (5aeae9a9, d13fa361, 05f01d72, e9a0edd5, e08f0ef8, 126c43f6) touch listed paths and are reported as unmapped delivery commits; the global validator passes. Reconcile with `--base` on the next delivery that touches this spec.
- [x] [Review][Defer] Baseline advancement has no maintained site checklist [docs/diagnostics/README.md:9] — deferred: pre-existing; one patch advance touches about 16 hand-maintained literals across 13 files plus a seal recompute, and the line-299 drift shows sites can lag silently.

#### Rejected

- Spec self-contradictions (status, sprint-status "no diff", 13 vs 15 paths) — rejected: the fix edits the spec under review.
- Ledger and handoff state both in-progress and review — false: the closing paragraph explicitly records the transition to review as an open state that resolves no gate.
- Orphaned, duplicated handoff paragraph — false: it sits under the dated `### Story 15.5 isolated final verification — 2026-10-31` heading; repeating it in the self-contained handoff record is append-only convention.
- npm preset `^10.4.1` expectation is out of scope — low: documented in the isolated-verification record, the identical edit landed independently in 5aeae9a9, and HEAD is consistent; no correction remains without rewriting pushed history.
- Collector misses `env:` overrides, `${VAR}` forms, quoted values or leftover `$` — false: `-p:` global properties override environment variables, and unexpanded or quoted values either touch only `BaseOutputPath` or fail the evaluation loudly.
- CLI evaluated with Gate 4 arguments; Gate 2a baseline literal unasserted — low: the CLI branch only asserts validation is off, which PackAsTool guarantees for any arguments; the Gate 2a literal is inert by SDK design.
- Gate 4 and Gate 4a version literals are not tied — low: a mismatch fails the blocking Gate 4a with `package-missing` (`eng/adopter_proof.py:73-75`); a static guard adds complexity for an unlikely drift.
- Local-only proof, unresolvable candidate SHAs, no CI link for the delivered commit — rejected: recording post-delivery SHA/CI edits the spec under review; the hosted facts are captured in the decision and defer entries above.
- Hosted observations left stale — rejected: the fix edits the spec under review or the append-only ledger.
- Ledger reproduction script prints without asserting and lacks None guards — low: an eyeballed, recorded recipe in an append-only ledger; adding guards is more than a direct correction.
- Candidates sort below the published baseline, so ApiCompat compares the package with itself — false: candidates are built from current source, AssemblyVersion 4.7.2.0 equals the baseline, and `<baseline>-ci` is the pre-existing enforced convention.
- Counts and Tasks list disagree across the record — rejected: the fix edits the spec under review.
- `review_loop_iteration` not advanced — rejected: the fix edits the spec under review.
- Policy accepts a candidate at or below the baseline — low: pre-existing `eng/release_compatibility.py` behavior; semantic-release always computes a newer version and ApiCompat rejects a lower AssemblyVersion; the fix adds a guard.
- Removed default evaluation masks per-project overrides — false: every live pack passes the baseline as a global property, which wins over project or imported values, and `_validate_baseline` still scans every packable project through the real-tree release-policy test.
- Sprint-status `last_updated` key not advanced — low: superseded by this review's sprint-status synchronization.
- Story pushed and merged twice despite "no push or merge" — false as a code defect: user-performed delivery; listed paths at HEAD equal eb1f0f71 apart from the later re-sealed policy file.

### Review resolution — 2026-10-31

The decision resolved to returning Story 15.5 to in-progress (sprint-status and
the ledger entry above); this spec stays `done`, because its bounded 4.7.2 scope
has no open patch. All four patches were applied on main after
`34d0afde14c43b6079208c47f95297d0cf99b51d`:

- Gate 4's collector joins `\` continuations and accepts every `-`/`--`/`/`
  `p:`/`property:` switch, normalized to `-p:`. The override case is injected as
  a continued line and runs with both `-p:` and `/p:`; the redundant
  `Should.Throw` became `ShouldNotBe`, and one local function owns the
  `$GITHUB_WORKSPACE`/`$name` substitution.
- The real-tree hotfix test and `PRODUCTION_VERSION` derive the next patch from
  `PUBLISHED_BASELINE_VERSION` (now `4.7.3` and `4.7.3-review.compat`),
  following the fixture's history of sitting one release above the baseline.
- `docs/diagnostics/README.md` drops "prior-policy refusals, precursor review".

Local verification:

- `python3 -m unittest tests/eng/test_pack_release_packages.py`: 47 passed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release -nodeReuse:false --disable-build-servers`:
  0 warnings, 0 errors.
- Shell.Tests `-method …CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary`:
  3 passed. Mutation checks: restricting the switch to `-p:` fails only the
  `/p:` case; removing the continuation join fails both override cases.
- Shell.Tests `-class …CiGovernanceTests -class …AnalyzerPolicyGovernanceTests`:
  97 total, 94 passed, 3 failed. All three failures were the canonical Release
  build's MSB3277 AngleSharp 1.8.6/1.9.0 conflict from a stale local
  `Testing.Tests` restore, not this change. After
  `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release`,
  `-class …AnalyzerPolicyGovernanceTests` passed 8 of 8, including the
  identifier-inventory seal.
- `pwsh -NoProfile -File eng/validate-docs.ps1`: exit 0.

No hosted run has observed these patches yet.
