---
title: 'Story 15.5: Reconcile the Published Compatibility Baseline'
type: 'chore'
created: '2026-10-26'
status: 'done'
route: 'dispatch'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: '2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '{project-root}/.editorconfig'
  - '{project-root}/.gitattributes'
  - '{project-root}/Directory.Build.props'
  - '{project-root}/Directory.Build.targets'
  - '{project-root}/tests/Directory.Build.props'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-15-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 15.5 expects advancement from 4.4.0 to an evidence-backed release. Subsequent work already selected 4.7.0 consistently, but the formal release ledger ends at 4.1.1 and does not connect that baseline to the published packages and independent verification.

**Approach:** Retain the verified 4.7.0 baseline, append a dated compatibility reconciliation to the existing release ledger, and link it from diagnostic governance documentation. Record the exact published source, Release and Release Evidence runs, inventory, evidence limits and existing compatibility checks. Preserve historical dispositions and independent owner approvals. Reuse existing ApiCompat, policy, public-surface and documentation lanes; add no scanner, reconstructed published packages or evidence bundle.

## Approved amendment — 2026-10-29

The user approved advancing to the newly evidence-backed 4.7.1 and retaining
this spec's historical evidence. This supersedes the original instruction to
retain 4.7.0 for current implementation; the original dated observations above
remain historical. Align the existing policy constant, shared default,
Contracts.UI override, Quality pack/consumer coordinates, focused baseline and
package-boundary tests, and current diagnostic guidance to 4.7.1. Append the
exact v4.7.1 publication evidence and reuse existing verification lanes. Keep
the empty v4.7 suppression ledger, historical release dispositions, original
baseline_commit, independent approvals and public contracts. No dependency
selection, AppHost/runtime change, new scanner or reconstructed historical
package is authorized. The current Tasks & Acceptance below define the bounded
implementation. Keep older Implementation Notes/Verification/Review Findings
as dated history; they do not override this amendment.

</frozen-after-approval>

## Code Map

- `eng/release_compatibility.py` — `PUBLISHED_BASELINE_VERSION`, pack arguments
  and same-release-line policy; change only the selected patch version.
- `Directory.Build.targets` and
  `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
  — shared baseline and reviewed explicit override; align both to 4.7.1.
- `.github/workflows/quality.yml` — CLI and seven-library package-validation
  commands plus validated synthetic package version; align their baseline and
  4.7.x-ci coordinates together. Preserve all gates.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
  and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
  — existing baseline expectations; update alongside configuration.
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
  and `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs`
  — package-boundary baseline assertions and MCP candidate/binary versions;
  advance together and run their existing focused checks. Python pack tests
  derive the selected version from the existing policy constant.
- `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt` — add the
  existing `FcOptimisticReconcileRequest.WaitForSubscriptionAsync` signature
  exposed by the required public-surface check; no runtime member is changed.
- `docs/diagnostics/README.md` and
  `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
  — current baseline guidance and append-only dated publication observations.
- `eng/dependency-graph-policy.json` — recompute only the existing consumer
  configuration seal after the approved XML baseline updates; preserve raw
  graph/source/selector/catalog identities and all validation semantics.
- `docs/diagnostics/compatibility-suppressions.json` — remains v4.7, empty;
  patch advancement does not change the release line or authorize suppressions.
- Existing seven-project inventory-driven ApiCompat command above — reuse with
  the policy constant after advancement. No new scanner, reconstructed historical
  package or evidence bundle. AppHost/Keycloak changes remain owned by Story 13.6.

## Tasks & Acceptance

The user approved this bounded plan and retained historical evidence on
2026-10-29. The frozen block now records that amendment; older statements
that approval is pending describe the preceding investigation.

- [x] `eng/release_compatibility.py`, `Directory.Build.targets`,
  `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
  and `.github/workflows/quality.yml` — advance the selected baseline coherently
  to 4.7.1 and align Quality's candidate/consumer coordinates. Update only the
  derived `consumer_configuration_sha256` in `eng/dependency-graph-policy.json`
  to match those approved XML bytes; preserve every dependency selection and
  validator guard. Verify exact candidate graph semantics and retain the separate
  prior-active-policy transition limitation if it still refuses.
- [x] `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`
  and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
  plus `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
  and `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs`
  — align existing expectations and MCP candidate/binary versions; run Python
  policy, diagnostic, governance and package-boundary checks and all seven real
  ApiCompat packs against published 4.7.1. Record the independently added optional
  callback in `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt`
  using the existing exact-member inventory mechanism; preserve runtime APIs.
- [x] `docs/diagnostics/README.md` and
  `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
  — describe 4.7.1, exact source and attempt identities, original published
  metadata hashes and independent-verification limits; retain dated history.
- [x] `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md`
  and `_bmad-output/implementation-artifacts/sprint-status.yaml` — reconcile
  review ownership, record fresh checks and preserve independent approvals.

Acceptance: Given approval and published-package evidence, when the baseline
advances, then all existing selected-baseline sites agree on 4.7.1, all seven
ApiCompat packs pass, and affected public-surface/governance checks pass through
existing mechanisms. Given no approval or a failed gate, when work stops, then
story completion is not claimed. Historical source, evidence and accepted
commit-scope defects remain attributed to their original revisions.


## Implementation Notes

### Current implementation handoff (2026-10-29)

The baseline advancement plan is approved. Implement only its tasks. Original
baseline `2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb` stays unchanged; current
continuation begins at `612d23485d7e2845d22102621a38d6c904e1ea90` with only the
two known draft documentation edits. Preserve historical notes and results;
append fresh results with exact commands/counts and external scratch locations.
Raw reports/packages/logs belong in temporary or ignored output, never tracked
evidence packets. The historical strict story check has known accounting
failures; do not move the baseline, delete old ownership or weaken validation.
Ordinary development builds use Debug/source mode. Real ApiCompat and package
boundary checks necessarily exercise Release/NuGet package mode. Run test
projects individually; prefer built xUnit assemblies for focused filtering.
No staging/commit/push by the implementation subagent; the parent handles the
final diff audit, independent review and pinned commit-message validation.
Record pass-3 stale/superseded/false findings with evidence. AppHost model-test
coverage and the standalone bearer helper's merged stderr remain independently
owned Story 13.6 follow-ups; append explicit deferred-work handoffs. Do not
change those runtime/helper paths or claim to have fixed them. Historical
"No runtime/configuration files changed" statements apply to their dated
iterations, not this approved package-baseline amendment.

### Historical implementation notes (through 2026-10-26)

- No intent gaps, irreversible actions or new APIs. Small documentation change: existing release ledger, diagnostic README, refreshed Epic 15 context, this spec and sprint status. The already correct baseline/configuration and all public contracts remain unchanged.
- Verified latest stable release: v4.7.0, immutable release 416759469, published 2026-10-18T22:57:16Z; exact source/tag commit cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4. Release 39173030800 and independent Release Evidence 39174645465 succeeded for that source. All eight NuGet version indexes select 4.7.0 as latest stable and their package endpoints return HTTP 200 on 2026-10-26. Metadata queries download no package bytes.
- Reuse eng/release_compatibility.py, Directory.Build.targets, Contracts.UI's explicit baseline, quality.yml Gate 4 and existing governance tests, all already pinned to 4.7.0. docs/diagnostics/compatibility-suppressions.json remains v4.7 with no suppressions. Do not derive publication from that planned-release ledger.
- Previous Story 15.4 is done: preserve catalog-owned identity, publication validation, complete File Lists and separately owned approval gates. Cache was stale and regenerated from current planning artifacts. Concurrent commit 2c8e204c arrived during investigation; no initial user edits existed and its accessibility changes are outside this story.
- Baseline policy command: python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py — exit 0, 60 tests passed in 5.642s.

- Implemented the append-only ledger reconciliation and repository-safe diagnostic README link. The record distinguishes fresh metadata/availability checks from the existing independent downloaded-byte verification and grants no owner sign-off or publication permission. No runtime/configuration files changed.
- All seven actual Release ApiCompat packs passed against published 4.7.0 with package validation enabled, baseline skipping disabled and isolated output per project; no suppression changes were needed. Packages are temporary validation output, not reconstructed historical evidence.
- The documentation-only footprint does not require an AppHost startup; no runtime code or AppHost model changed. Focused publication validation skips unchanged snippet compilation and DocFX generation. Normal CI keeps its existing gates and performs no new live feed query.

- Resumed on 2026-10-26 to close the seven remaining review patches. Corrected the ledger's three-commit baseline chronology and update date, separated technical publication evidence from FR24 disposition, and added reproducible inventory/hash queries and delivery/CI provenance below. Runtime code, package-validation configuration and historical owner decisions are unchanged.
- The existing 2026-10-26 review decisions accept the two historical commit-scope defects without rewriting history or declaring delivery work shared. Their strict-validator failures remain explicit below; declaring concurrent Story 13.6 artifacts only completes unrelated-path accounting.
- Concurrent edits to `spec-13-6-repair-hosted-verification-blockers.md` appeared after the initially clean worktree check. They are preserved and excluded from this follow-up's commit.

### Replanning trigger and fresh checks (2026-10-29)

Resumed from initially clean `main` at
`612d23485d7e2845d22102621a38d6c904e1ea90`. The original baseline and frozen
intent are preserved. Stop/replan was triggered by the newly published 4.7.1,
which the original "retain 4.7.0" intent does not authorize selecting. No
runtime, workflow, baseline configuration or dependencies have been changed.
The ledger's inventory citation is completed as an authorized documentation
patch. Implementation of the proposed advancement awaits the human decision.

Fresh published JSON reads confirm v4.7.1 inventory `683094182` is valid with
eight packable rows (raw hash `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a`),
consumer validation `683094079` is valid, and sealed manifest `683094417` hashes
to `f5ebb7d831c1a0c5b06a8f879109ab102b19c690c55f2e4ffa7af4ca64b782f8`.
The manifest binds source `5688c3eba98567133285b0f51843d9daa8f24f0b`, tag v4.7.1
and release run 40593684166. Exact attempt-1 API queries confirm the Release
and Release Evidence successes. All eight NuGet indexes select 4.7.1, while
HEAD requests for the eight 4.7.0 endpoints still return 200. No historical
package bytes were rebuilt or downloaded. Published-JSON scratch and exact
attempt/job API records: `/tmp/fc-story15-5-20261029-gyd2sf55/`.

`python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`
passed 60/60 in 4.843s against the unchanged 4.7.0 configuration.
`python3 eng/validate-story-artifacts.py` passed. The legacy story-scoped
command now exits 1 because later independently owned commits added paths
since the original baseline; its older pass is historical. No baseline or
validator is changed to hide that result. The known strict-history failures
remain separate. The follow-up-only command
`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md --base 612d23485d7e2845d22102621a38d6c904e1ea90`
also exits 1: the four historical File List entries unchanged in this two-file
follow-up have no matching change in that narrower range. These are accounting
limitations, not passing gates; no original ownership list is deleted to obtain
a pass. `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
passed; `git diff --check` passed. A focused comparison confirms the frozen
block is identical to the starting revision after repository newline
normalization. No ApiCompat rerun or new .NET result is claimed.

A separate investigation agent checked the concrete draft, original frozen
intent, exact publication metadata and impacted baseline sites. Its two
missing-test-site findings were incorporated: Contracts.UI package-boundary
assertions and MCP package-boundary baseline/candidate/binary identities. The
active amendment proposal is about 1,100 tokens by character estimate; the
existing full spec and historical review evidence remain about 12,400 tokens.
Keeping that history avoids deleting prior evidence; the user can instead
request a separate historical archive before approving implementation.

Review pass 3 recheck: the consumed CI pin is now
`6952b59c3486ee50d32056c3fee07a07e8120bd5`, whose exact workflow source has
heredoc-isolated TRX verification; its adoption is documented by Story 13.6,
so the old request to select 09af is superseded, not applied by this story.
`LocalWorkloadRealm` and its regression now preserve `CreateDefaultClientScopes`;
Story 13.6 records actual workload/user token exchanges and four 401s, so its
former "unwired" result is historical. The claimed Boolean whitespace defect
is false: `pwsh -NoProfile -Command` exercised `Boolean.TryParse` with `true`,
space-padded true and tab/CRLF-padded true, all successful. The remaining
AppHost source-text/model-test gap and standalone bearer's merged stderr are
still present, independently owned Story 13.6 issues; no fix or new runtime
verification is claimed here.

## Verification

### Approved advancement verification (2026-10-29)

The approved baseline amendment is implemented from continuation source
`612d23485d7e2845d22102621a38d6c904e1ea90`; the original `baseline_commit`
remains unchanged. The selected configuration and focused expectations now
agree on `4.7.1`. Quality builds/consumes `4.7.1-ci`; MCP package-boundary
validation builds `4.7.1-review.compat` with binary identity `4.7.1.0`.
The `v4.7` suppression ledger and public contracts are unchanged. No AppHost,
runtime, dependency selection, publication or independent approval was changed.

Fresh local logs, original published JSON, exact attempt/job API responses,
and expanded pack commands are external scratch at
`/tmp/fc-story15-5-implementation-20261029-e4k5a67r/`. No tracked evidence
bundle or reconstructed published package was created.

- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`
  — exit 0, 60/60 passed in 2.956s (`python-policy.log`).
- The existing Repeatable ApiCompat command above, after the policy constant
  advanced to `4.7.1`, ran all seven projects with candidate `4.7.1-ci`,
  `EnableFrontComposerPackageValidation=true`, baseline `4.7.1`,
  `FrontComposerPackageValidationSkipBaseline=false`, and a distinct temporary
  `BaseOutputPath` per project. All seven `dotnet pack` processes exited 0.
  Exact arguments/results are in `apicompat-results.json`; per-project
  `*-apicompat.log` files retain the expanded commands. Inventory: Contracts
  (net10.0/netstandard2.0), Contracts.UI, Mcp, Schema, Shell, SourceTools
  (netstandard2.0), Testing.
- Individually built SourceTools.Tests, Shell.Tests, Contracts.UI.Tests and
  Mcp.Tests with `dotnet build tests/<project>/<project>.csproj --configuration Debug --nologo -v:minimal`.
  Each exited 0 with zero warnings/errors. These ordinary development builds
  use the repository's Debug/source mode; the existing real package checks
  necessarily invoke Release/NuGet mode themselves.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests -class '*SchemaFingerprint*' -class '*Drift*'`
  — exit 0, 289 passed; zero failed/errors/skipped/unrun (`source-tests.log`).
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -method '*CiGovernanceTests.PackageInventory_IsExplicitLockstepAndReviewable' -method '*CiGovernanceTests.SemanticReleasePack_EvaluatesThePublishedPackageValidationBaseline' -method '*CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary'`
  — exit 0, 3 passed; zero failed/errors/skipped/unrun (`governance-tests.log`).
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests.dll -class Hexalith.FrontComposer.Contracts.UI.Tests.PackageBoundaryTests`
  — exit 1, 4 executed / 3 passed / 1 failed; zero errors/skipped/unrun
  (`contracts-ui-tests.log`). Baseline policy, ownership, and real packed
  clean-consumer checks pass. `PublicApi_ExportedMembers_MatchIntentionalBaseline`
  fails on exactly the independently added
  `Hexalith.FrontComposer.Contracts.Freshness.FcOptimisticReconcileRequest.WaitForSubscriptionAsync:System.Func<System.IDisposable,System.Threading.CancellationToken,System.Threading.Tasks.Task>:get/set`.
  Commit `5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d` added the member;
  unchanged HEAD source contains it and unchanged HEAD `PublicAPI.Shipped.txt`
  omits it. The comparison has no missing members and that one added member
  (`contracts-ui-public-api-diff.json`). The implementation subagent preserves
  the failed gate and public contracts; the parent owns the inventory correction
  and rerun during its final audit. Completion is not claimed at this handoff.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Mcp.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Mcp.Tests.dll -class Hexalith.FrontComposer.Mcp.Tests.Skills.McpRuntimePackageBoundaryTests`
  — exit 0, 2 passed; zero failed/errors/skipped/unrun (`mcp-tests.log`). This
  includes cold/warm baseline validation and actionable missing-baseline refusal.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`
  — exit 0; documentation validation passed (`docs-validation.log`). Publication
  inputs/snippets are unchanged; DocFX and snippet compilation were not rerun.

Final local assertions verify the README URL against the actual heading-derived
ledger anchor, all three raw published hashes against original asset digests,
the empty `v4.7` suppression ledger, and the original story baseline (exit 0;
`final-focused-assertions.log`). `python3 eng/validate-story-artifacts.py` passes.
The exact legacy `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md`
exits 1 because later independently owned changes since the original baseline
are absent from this story's ownership list (`artifacts-legacy-story.log`).
Adding the approved baseline sites to File List also makes strict
`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md --candidate HEAD`
report later unrelated commits touching those sites alongside the accepted
original `unmapped` / `interleaved` defects (`artifacts-strict-story.log`).
These are current accounting failures, not passing gates. No baseline, history,
validator or unrelated-path inventory was rewritten to force a pass.

Original publication observations are appended to the existing
[release ledger](rel-ai-1-release-evidence-ledger.md#story-155-approved-compatibility-baseline-advancement-2026-10-29).
Reproduction used `gh api repos/Hexalith/Hexalith.FrontComposer/releases/tags/v4.7.1`,
`gh api repos/Hexalith/Hexalith.FrontComposer/git/ref/tags/v4.7.1`, exact
`gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/<run-id>/attempts/1`
and `/attempts/1/jobs?per_page=100` for runs `40593684166` / `40596573358`,
and `gh api repos/Hexalith/Hexalith.FrontComposer/releases/assets/<asset-id> -H 'Accept: application/octet-stream'`
for original inventory `683094182`, consumer validation `683094079`, and sealed
manifest `683094417`. SHA-256 was computed over raw response bytes before JSON
parsing. Their hashes match the published digests and the ledger. Eight existing
inventory-derived NuGet index reads select `4.7.1`; HEAD requests to their `4.7.1`
package endpoints all return 200 (`nuget-availability.json`). No new byte/signature
independent audit or original independent artifact download is claimed. These
published-release runs do not test this later worktree amendment; parent review,
delivery SHA/CI provenance, and all separately owned approvals remain outstanding.

### Historical verification (2026-10-26)

- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py` — exit 0; 60 passed. Includes production/synthetic pack plans, baseline agreement, suppression lifecycle and failure paths.
- For each of the seven projects returned by `eng/release_compatibility.py:packable_library_projects`, ran `dotnet pack <project> --configuration Release --output /tmp/frontcomposer-15-5-apicompat-_uwokzr9/packages -p:Version=4.7.0-ci -p:PackageVersion=4.7.0-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.0 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/frontcomposer-15-5-apicompat-_uwokzr9/<project-name>/bin/ --nologo -v:minimal` — all seven exited 0. Exact expanded commands and logs remain local at that scratch path. Inventory: Contracts (net10.0/netstandard2.0), Contracts.UI, Mcp, Schema, Shell, SourceTools (netstandard2.0), Testing. This reuses Quality Gate 4's actual package-validation mechanism.
- `dotnet build tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` — exit 0; zero warnings/errors. Local log `/tmp/frontcomposer-15-5-source-build.log`.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.SourceTools.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.SourceTools.Tests.dll -class Hexalith.FrontComposer.SourceTools.Tests.Diagnostics.DiagnosticRegistryTests -class '*SchemaFingerprint*' -class '*Drift*'` — exit 0; 289 passed, zero failed/skipped/errors/unrun. Includes real baseline/public-surface parity, diagnostic bands, canonical fingerprints and drift. Local log `/tmp/frontcomposer-15-5-contract-tests.log`.
- `dotnet build tests/Hexalith.FrontComposer.Contracts.Tests/Hexalith.FrontComposer.Contracts.Tests.csproj --configuration Debug --no-restore --nologo -v:minimal` initially failed NETSDK1004 before compilation because the test assets file was absent. Repeating without `--no-restore` restored the project and exited 0 with zero warnings/errors. Logs `/tmp/frontcomposer-15-5-contracts-build.log` and `/tmp/frontcomposer-15-5-contracts-build-restored.log`.
- `DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Contracts.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Contracts.Tests.dll -class Hexalith.FrontComposer.Contracts.Tests.Architecture.ContractsPackageBoundaryTests -class Hexalith.FrontComposer.Contracts.Tests.Schema.CanonicalSchemaMaterialFingerprintVectorTests -class Hexalith.FrontComposer.Contracts.Tests.Schema.SchemaFingerprintContractsTests` — exit 0; 11 passed, zero failed/skipped/errors/unrun. Local log `/tmp/frontcomposer-15-5-public-contracts.log`.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` — exit 0; documentation validation passed, no blocking failures. Local log `/tmp/frontcomposer-15-5-docs.log`. No publication input/configuration or snippet changed, so full site regeneration is unnecessary here.
- `python3 eng/validate-story-artifacts.py` and `git diff --check` — exit 0 before review; final story-scoped ownership check follows review.
- Reuse [Story 15.4's verification](spec-15-4-correct-the-fluent-ui-v5-contingency-identity.md#verification), including 127 passing MigrationCommandTests and full DocFX/snippet publication proof, plus [its follow-up's verification](spec-15-4-correct-the-fluent-ui-v5-contingency-identity-2.md#verification), covering 59 passing documentation cases, for unchanged migration/publication behavior. Those are historical focused results, not claims of fresh execution in this story. All public API, generated-output and migration authorities remain unchanged by the five owned documentation/tracking files.

### Parent task audit (2026-10-29)

The required Contracts.UI public-surface gate exposed one pre-existing inventory
omission from `5af87f9e5cc8db16896fc97d19b6a828b2ed0d0d`. Added exactly its
existing optional `WaitForSubscriptionAsync` signature to the existing
`PublicAPI.Shipped.txt`; no runtime member, visibility, behavior or test assertion
changed. This uses the story's existing public-surface baseline mechanism.
The same full four-test class reran against the corrected inventory:
`DiffEngine_Disabled=true dotnet tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests.dll -class Hexalith.FrontComposer.Contracts.UI.Tests.PackageBoundaryTests`
— exit 0, 4 passed, zero failures/errors/skips/unrun, 3.030s; exact output
`/tmp/fc-story15-5-implementation-20261029-e4k5a67r/contracts-ui-after-inventory.log`.
This supersedes the earlier failed first attempt above. The correction changes
only a text inventory, so the previously passed ApiCompat and compiled-assembly
checks remain applicable. All four current implementation tasks and their
acceptance criteria now pass; no I/O matrix was introduced. Independent review
and final status reconciliation follow. Hosted CI for this continuation has not
run: the approved workflow does not push or dispatch remote work, and no overall
hosted-green claim is made for the new local commit.


### Delivery and CI provenance (review follow-up 2026-10-26)

The original five-file delivery is commit `2f1dd0e128273706634374d7d66a0878662bd62f`.
A fresh Actions API query for that exact SHA returned zero runs. The following
commit `62e3664c51c7cb7b9475728f3bf02a266d098fa7` has cancelled
[CI 40287030893](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287030893/attempts/1)
and [Quality 40287030293](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287030293/attempts/1),
so neither proves a passing delivery gate.

The first completed, non-cancelled CI/Quality runs containing the delivery are push attempt 1 at descendant
commit `29dcbfdeff32b964c581aa96a1b18d7dab20a71b`, queried again on 2026-10-26:

| Lane | Durable run | Observed result |
| --- | --- | --- |
| CI | [40287567822, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287567822/attempts/1) | Completed, success. |
| Quality | [40287567326, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287567326/attempts/1) | Completed, failure overall. The [build-and-test job](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287567326/job/121455214432) succeeded, including `Gate 4: Library Package Validation (ApiCompat baseline)`. The separate [epic9-live-acceptance job](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/40287567326/job/121455214625) failed at `AppHost rejects invalid bearer tokens`. |

These runs corroborate the delivered compatibility checks. They do not establish
an overall green Quality workflow, owner acceptance or publication eligibility,
and they do not test this later documentation follow-up. The bearer-verification
failure remains separately owned by [Story 13.6's repair](spec-13-6-repair-hosted-verification-blockers.md).
Existing local test counts above are historical results from the original delivery.
Fresh follow-up checks are recorded separately below.

Re-query attempt 1 with `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/<id>/attempts/1`
for the exact `run_attempt`, `head_sha`, `event` and `conclusion`, and
`gh run view <id> --attempt 1 --repo Hexalith/Hexalith.FrontComposer --json conclusion,status,headSha,url,jobs`
for the job/step results. The absence/cancellation checks used
`gh api 'repos/Hexalith/Hexalith.FrontComposer/actions/runs?head_sha=<full-SHA>&per_page=100'`
for each delivery/following SHA above.

### Fresh follow-up verification (2026-10-26)

Executed from review baseline `3668b603bf9cd176da86a2f3e020fd8fab2b58af` with
this documentation follow-up in the worktree:

- `python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py` — exit 0; 60 tests passed in 2.569s. No package-validation code or configuration changed.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild` — exit 0; docs validation passed. DocFX and snippet compilation were skipped because neither publication configuration nor snippets changed.
- `python3 eng/validate-story-artifacts.py` and `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md` — exit 0; artifact hygiene and unrelated-path accounting passed.
- The strict `--candidate HEAD` command in Commit-Scope Reconciliation — exit 1, reporting exactly the historical `unmapped` delivery and `interleaved` Story 13.6 commit. No File List accounting failure remains. This is an accepted history limitation, not a passing gate.
- The published-asset example below — exit 0; inventory `valid`, eight packable rows, both raw-byte hashes matching the recorded values. Exact-attempt CI/Quality API queries and `gh run view 40287567326 --attempt 1 --repo Hexalith/Hexalith.FrontComposer --json conclusion,status,headSha,url,jobs` confirmed the run/step results above.
- Focused Python assertions passed: frozen intent equals the original tracked block; the README URL matches the actual ledger heading-derived anchor; all eight patch findings are checked; release-ledger observations before Story 15.5 are unchanged except the frontmatter update date.
- `git diff --check` — exit 0.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/frontcomposer-15-5-followup-jph4ekwt/commit-message.txt --verbose` — exit 0; zero problems/warnings. Successful local validation evidence: `/tmp/frontcomposer-15-5-followup-jph4ekwt/commitlint.log`.

The original seven ApiCompat packs and focused .NET tests were not repeated for
this documentation follow-up; their recorded results and the successful descendant
Gate 4 remain the compatibility evidence. No overall green Quality run is claimed.

### Repeatable ApiCompat command

The following reruns the actual seven-library gate using the existing inventory authority and a fresh disposable output directory. It intentionally builds current packages for ApiCompat; it does not reconstruct previously published bytes.

```bash
python3 - <<'PYCOMPAT'
import pathlib, subprocess, sys, tempfile
root = pathlib.Path.cwd()
sys.path.insert(0, str(root / 'eng'))
from release_compatibility import PUBLISHED_BASELINE_VERSION, packable_library_projects
output = pathlib.Path(tempfile.mkdtemp(prefix='frontcomposer-apicompat-'))
projects = packable_library_projects(root)
assert len(projects) == 7
for project in projects:
    subprocess.run([
        'dotnet', 'pack', str(project), '--configuration', 'Release',
        '--output', str(output / 'packages'),
        '-p:Version=' + PUBLISHED_BASELINE_VERSION + '-ci',
        '-p:PackageVersion=' + PUBLISHED_BASELINE_VERSION + '-ci',
        '-p:EnableFrontComposerPackageValidation=true',
        '-p:FrontComposerPackageValidationBaselineVersion=' + PUBLISHED_BASELINE_VERSION,
        '-p:FrontComposerPackageValidationSkipBaseline=false',
        '-p:BaseOutputPath=' + str(output / project.stem / 'bin') + '/',
        '--nologo', '-v:minimal',
    ], check=True)
PYCOMPAT
```

### Read-only publication queries

- `gh release list --repo Hexalith/Hexalith.FrontComposer --limit 8` — latest stable v4.7.0.
- `gh api repos/Hexalith/Hexalith.FrontComposer/git/ref/tags/v4.7.0 --jq .object` — exact commit cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4.
- `gh api repos/Hexalith/Hexalith.FrontComposer/releases/tags/v4.7.0` — immutable release 416759469; small JSON asset IDs: manifest 654681307, readiness 654681225, consumer validation 654681092. Read their original published JSON with `gh api repos/Hexalith/Hexalith.FrontComposer/releases/assets/<id> -H 'Accept: application/octet-stream'`.
- `gh run view 39173030800 --repo Hexalith/Hexalith.FrontComposer --json conclusion,headSha,url` and `gh run view 39174645465 --repo Hexalith/Hexalith.FrontComposer --json conclusion,headSha,url,jobs` — both success for the exact source; independent verification steps succeeded.
- `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/39174645465/artifacts` — unexpired original artifact 12154181217, `release-verification-39174645465-1`; not downloaded. `gh run view --log` returned empty stdout with exit 0. Therefore exact historical independent ledger booleans are attributed to the tracked October 18 record, corroborated by live verification-step results.
- For each packable package ID, queried `https://api.nuget.org/v3-flatcontainer/<lowercase-id>/index.json`, selected the last version without a prerelease suffix, and sent HTTP HEAD to `https://api.nuget.org/v3-flatcontainer/<lowercase-id>/4.7.0/<lowercase-id>.4.7.0.nupkg`. All eight selected 4.7.0 and returned 200 on 2026-10-26. Live queries were investigation only; normal tests remain offline.

### Published inventory and raw manifest hash

Read the original JSON assets through the GitHub release-assets API; hash the raw
response bytes before JSON parsing or any reserialization:

```bash
python3 - <<'PYASSETS'
import hashlib, json, subprocess
for asset_id in (654681115, 654681307):
    data = subprocess.check_output([
        'gh', 'api', '-H', 'Accept: application/octet-stream',
        f'repos/Hexalith/Hexalith.FrontComposer/releases/assets/{asset_id}',
    ])
    document = json.loads(data)
    print(asset_id, hashlib.sha256(data).hexdigest())
    if asset_id == 654681115:
        print(document['status'], sum(row['packable'] for row in document['rows']))
PYASSETS
```

Fresh execution on 2026-10-26 returned inventory asset `654681115` with
`status=valid`, eight packable rows and raw SHA-256
`c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a`.
Manifest asset `654681307` returned raw SHA-256
`2aa2d01e8cc6b8b489b016c2828af478192dcf68850ea9c1978d14b7d19f24ba`,
matching both the ledger's value and the release API's `sha256:` digest.
Only published JSON metadata was read; package bytes were not downloaded or rebuilt.

## Commit-Scope Reconciliation

The 2026-10-26 review decisions (option 1 for both findings) accept these history
defects as recorded. They do not grant a strict-validation exemption:

| Canonical commit | Actual contents | Strict classification and accepted defect |
| --- | --- | --- |
| `2f1dd0e128273706634374d7d66a0878662bd62f` | The original five-file Story 15.5 delivery. | `unmapped`: the subject lacks the story identity; its five paths are `listed-unowned` in this commit. This is delivery work, not `shared` work. |
| `62e3664c51c7cb7b9475728f3bf02a266d098fa7` | Eleven concurrent Story 13.6 paths; no Story 15.5 delivery files. | `interleaved`: its subject incorrectly names Story 15.5 and all eleven paths are unowned by this story. Unrelated declarations below complete path accounting but cannot change that classification. |
| `29dcbfdeff32b964c581aa96a1b18d7dab20a71b` | The `references/Hexalith.Builds` gitlink sync. | `unrelated`; outside Story 15.5. |
| `3668b603bf9cd176da86a2f3e020fd8fab2b58af` | Story 15.5 review findings, deferred work and status reopening. | `owned`; its subject names Story 15.5 and its paths are in the File List. |

`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md --candidate HEAD`
continues to exit 1 for the two immutable history defects above. No history is
rewritten, baseline moved, validator weakened or non-owning disposition declared.
The legacy `--story` check validates File List/unrelated-path accounting separately;
its success must not be presented as a strict commit-scope pass. This follow-up must
use a validated commit message with Story 15.5 identity; verify its full SHA and
`owned` classification after committing. The historical strict failures remain.

## Documented Unrelated Changes

- `tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs` — concurrent Story 13.6 AppHost regression coverage; appeared during final checks and is excluded from this commit.
- `src/Hexalith.FrontComposer.AppHost/Program.cs` — concurrent Story 13.6 local workload-identity amendment; appeared during follow-up review and is excluded from this commit.
- `src/Hexalith.FrontComposer.AppHost/LocalWorkloadRealm.cs` — concurrent Story 13.6 helper under construction; preserved and excluded from this commit.
- `tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj` — concurrent Story 13.6 test wiring; preserved and excluded from this commit.
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — concurrent Story 13.6 result artifact committed in the mislabelled `62e3664c51c7cb7b9475728f3bf02a266d098fa7`; outside Story 15.5.
- `_bmad-output/implementation-artifacts/spec-13-6-repair-hosted-verification-blockers.md` — concurrent Story 13.6 repair artifact committed in the same mislabelled commit; ongoing worktree edits are preserved and excluded from this follow-up.
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md` — concurrent Story 13.6 verification artifact committed in the same mislabelled commit; outside Story 15.5.
- `references/Hexalith.Builds` — concurrent dirty submodule content appeared during verification; not edited or staged by Story 15.5.
- `eng/verify-apphost-invalid-bearer.sh` — concurrent bearer-verification work appeared during final checks; preserved and outside Story 15.5.
- `tests/e2e/package.json` — concurrent browser test wiring; preserved and outside Story 15.5.
- `tests/e2e/scripts/verify-apphost-invalid-bearer.test.mjs` — concurrent bearer-verification tests; preserved and outside Story 15.5.
- `.github/workflows/quality.yml` — earlier Story 13.6 CI/browser work remains unrelated historical content; the approved 2026-10-29 baseline-only changes to this file are Story 15.5-owned.
- `eng/validate-docs.ps1` — concurrent documentation validator hardening; preserved and outside Story 15.5.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — earlier concurrent CI/browser governance remains unrelated historical content; the approved 2026-10-29 selected-baseline expectation is Story 15.5-owned.
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Docs/DocsSiteValidationTests.cs` — concurrent documentation validator tests; preserved and outside Story 15.5.
- `tests/e2e/specs/adopter-historical-label-cleanup.spec.ts` — concurrent browser/adopter work; preserved and outside Story 15.5.

## File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/epic-15-context.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `docs/diagnostics/README.md`


- `.github/workflows/quality.yml`
- `Directory.Build.targets`
- `eng/release_compatibility.py`
- `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`
- `tests/Hexalith.FrontComposer.Contracts.UI.Tests/PackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Mcp.Tests/Skills/McpRuntimePackageBoundaryTests.cs`
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`
- `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs`

- `src/Hexalith.FrontComposer.Contracts.UI/PublicAPI.Shipped.txt`

- `eng/dependency-graph-policy.json`

## Review Triage Log

Blind Hunter reviewed the five owned files at the parent model capability. Edge Case Hunter and Verification Gap were skipped by the oneshot route. All six findings were verified and patched; nothing was deferred.

- BH-1 — low / patch: the source/run IDs were correct, but attempt identity was implicit. Fresh `gh api repos/Hexalith/Hexalith.FrontComposer/actions/runs/<id> --jq '{id,run_attempt,head_sha,conclusion}'` checks confirm attempt 1 and success for both runs at cf5fe8da100bd5224a0d7f597fc92e1ebb98a5f4. The ledger now names and links each exact attempt.
- BH-2 — low / patch: parameterized commands plus temporary exact-command logs were harder to reproduce after cleanup. Added the runnable inventory-driven command above, reusing the existing policy and the same arguments that passed all seven projects.
- BH-3 — low / patch: historical results were correctly attributed but lacked direct navigation. Added explicit links to the original Story 15.4 and separate follow-up verification sections; those results remain clearly distinguished from fresh execution.
- BH-4 — medium / patch: the tracked REL-BASE-1 alias still said proposed. It now agrees with the numbered story at review and points to this spec. Epic 15 and unrelated approval rows remain unchanged.
- BH-5 — low / patch: the fallback expiry was stated accurately without explaining its temporal scope. Clarified that expiry affects publication authorization and does not retroactively invalidate the published release or its use as an ApiCompat baseline.
- BH-6 — low / patch: confirmed that Get-ContentFiles excludes docs/diagnostics/README.md; its generic docs pass cannot prove the new link. Added a focused local check of the exact repository URL, existing ledger path and heading-derived anchor, recorded below. No new validator or test lane was added.


### Final checks after review

- The focused README link check derived the GitHub anchor from the actual ledger heading, constructed the exact repository URL from its tracked path, and asserted the README link matches. Exit 0; anchor `story-155-compatibility-baseline-reconciliation-2026-10-26`. Local log `/tmp/frontcomposer-15-5-ledger-link.log`. The newly added target is verified locally before publication; no remote-main availability is claimed before push.
- Both original successful workflow attempts were queried directly: `run_attempt=1`, `conclusion=success`, and the same exact published source SHA for each. The ledger links `/attempts/1` explicitly.
- Review changed documentation and tracking only; the previously green seven ApiCompat packs and 360 focused tests remain applicable. The complete spec is done; the numbered sprint row and REL-BASE-1 alias are review, while Epic 15 remains in-progress for maintainer acceptance.
- Pinned commit-message validation: `node node_modules/@commitlint/cli/cli.js --edit /tmp/frontcomposer-15-5-commit-message.txt --verbose` — exit 0, zero problems/warnings; evidence `/tmp/frontcomposer-15-5-commitlint.log`.
- Final `python3 eng/validate-story-artifacts.py` and story-scoped `--story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md` checks passed, as did `git diff --check`. Concurrent dirty paths are explicitly classified above and excluded from the five-file commit.

### Review Findings

Code review pass 2 (`bmad-code-review`, 2026-10-26) covered `2c8e204c..62e3664c` over the five File List paths, which is byte-identical to `2f1dd0e1`. Blind Hunter (BH), Edge Case Hunter (EC), Verification Gap (VG) and Acceptance Auditor (AA) all completed. Result: 2 decision-needed (both resolved 2026-10-26 to patch, option 1), 6 patch, 5 defer, 15 rejected.

- [x] [Review][Patch] Record the unmapped delivery commit as an accepted history defect [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md:Commit-Scope Reconciliation] — resolved from decision 2026-10-26, option 1 (record and accept; no history rewrite, no `shared` declaration): `2f1dd0e128273706634374d7d66a0878662bd62f` is the delivery commit but its subject carries no story ID, so strict mode reports it `unmapped` and its five paths `listed-unowned`. Review patches commit as `fix(15.5): …` so they classify `owned`. (VG, AA; medium)
- [x] [Review][Patch] Record the mislabelled 13.6 auto-commit and declare its committed 13-6 artifacts [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md:Documented Unrelated Changes] — resolved from decision 2026-10-26, option 1: `62e3664c51c7cb7b9475728f3bf02a266d098fa7` (`fix: story 15.5`) holds only concurrent Story 13.6 work, so strict mode's `interleaved` failure is recorded as unrepairable without a history rewrite, and the three committed 13-6 artifacts are declared so legacy `--story` passes. (VG, EC, AA; medium)
- [x] [Review][Patch] Record the delivery SHA and CI run link that the PRD §7 evidence policy requires [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md:Verification] — Verification cites only `/tmp` logs. `2f1dd0e1` received no workflow runs, CI/Quality for `62e3664c` were cancelled, and the first runs that include it are on `29dcbfde` (CI 40287567822, Quality 40287567326; in progress at review). Record their conclusions and whether Gate 4 ran. (BH, EC, AA; medium)
- [x] [Review][Patch] Ledger attributes the whole 4.7.0 advance to `ee9762e6` [_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:609] — `ee9762e6` (2026-10-19) moved the policy constant, the shared default and Contracts.UI; Quality's package-validation gates moved from 4.6.0 to 4.7.0 in `a258b992` (2026-10-21), and the `v4.7` suppression ledger came from `15577f42`. (AA; medium)
- [x] [Review][Patch] State that v4.7.0 has no FR24 disposition in this ledger [_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:613] — a "Publication authority" heading over `publish_authorized=true`, "succeeded" and "valid" inside a compliance ledger whose Summary ends at v4.1.1 reads as a disposition. Name the independent verifier's `compliant-candidate` classification and that v4.2.0+ rows remain REL-LEDGER-2 (Story 17.14) backfill. (BH, AA; low)
- [x] [Review][Patch] Ledger front matter still says `updated: 2026-08-14` [_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:5] (BH; low)
- [x] [Review][Patch] Record how package inventory and the manifest hash were read [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-published-compatibility-baseline.md:Read-only publication queries] — the ledger states package-inventory `valid` and the sealed-manifest SHA-256, but asset `654681115` and the hash computation are not listed. (AA; low)
- [x] [Review][Patch] Restore the canonical sprint-status `last_updated` format [_bmad-output/implementation-artifacts/sprint-status.yaml:51] — `2026-10-26` replaced the `MM-DD-YYYY HH:MM` form (`DATE_FORMAT` in `sprint_plan.py`); the fallback parser accepts it, so this is cosmetic. (BH, AA; low) Resolved 2026-10-26 by the review status sync, which wrote `10-26-2026 15:59`.
- [x] [Review][Defer] HFC2122 still targets removal in 4.7.0 although 4.7.0 shipped the member [docs/diagnostics/diagnostic-registry.json:2512] — deferred: pre-existing (Story 9-4 lifecycle; baseline advanced in `ee9762e6`), medium. `removedIn`, the `[Obsolete]` text at `FrontComposerShortcutRegistrar.cs:259`, `HFC2122.md` and the message template all promise v4.7 removal, and any removal is now a CP0002 break against the 4.7.0 baseline.
- [x] [Review][Defer] PRD OI-11/G-1 and the epics.md 15.5 AC still read "4.4.0 → currently v4.5.0" with OI-11 Open [_bmad-output/planning-artifacts/prd.md:731] — deferred: the fix edits PM/Release Owner planning artifacts, medium; also `prd.md:490`, `prd.md:621`, `epics.md:3990`.
- [x] [Review][Defer] `phase_build` docstring says the baseline is "currently 4.5.0" [eng/release_prepublish.py:193] — deferred: pre-existing, low; check `eng/workflow_source_closure.py` coverage before editing a release-chain script.
- [x] [Review][Defer] PLAN-INT-2 and DOC-A/B/C aliases stay `proposed` although Stories 15.1–15.4 are done [_bmad-output/implementation-artifacts/sprint-status.yaml:1044] — deferred: pre-existing since 2026-09-23, low. The 15.6/15.7 part is false: sprint-status records them as superseded and closed as done.
- [x] [Review][Defer] Regenerated Epic 15 context dropped still-applicable details [_bmad-output/implementation-artifacts/epic-15-context.md:40] — deferred: agent-context file, low. It lost "tenant/user values" (`epics.md:3912`, `epics.md:3917`), the Story 13.9 decision reference and the PRD §7 "Components + Icons 5.0.1 GA not approved" posture.

#### Rejected

- BH/EC NuGet check ignores `listed` and `.snupkg` — false: on 2026-10-26 the registration reports `listed=true` for all eight 4.7.0 entries and 4.7.0 is the highest stable version; symbol packages are served by the symbol server, not the flat container.
- BH nothing prevents future baseline lag — false: the release policy fails closed two or more lines behind, including the static check against the ledger's `currentRelease`; the fallback expiry is recorded and owned by publication governance.
- BH Epic 15 context removed retention rules and added the 15.6/15.7 supersession — false: both follow the PRD §7 owner decision of 2026-10-23, which supersedes those stories and their retention protocol.
- EC Epic 15 context dropped EventStore packet ownership and the cross-repository cleanup ban — false: PRD §7 scopes `evidence/` scratch to this repository, and its repository policy separately forbids modifying submodule files.
- EC repeatable command checks a different baseline after advancement — false: rerunning against the current policy constant is intended.
- BH repeatable command skips Gate 4's restore — false: `dotnet pack` restores implicitly with the same global properties.
- EC repeatable command assumes the repository root as working directory — low: every command in this spec is repository-root relative.
- EC `assert` is stripped under `python3 -O` — low: not an everyday invocation, and a guard adds code.
- EC no `-nodeReuse:false` or timeout — low: a workspace-specific MSBuild wedge; Gate 4 runs the same flags.
- EC `mkdtemp` output is never removed — low: a deliberately disposable directory.
- AA repeatable command copies Gate 4 flags by hand — low: a dated record of the exact passing command, not a scanner.
- BH README link uses a mutable `blob/main` anchor — low: verified locally; append-only ledger headings are not renamed.
- EC same `blob/main` anchor concern — low: duplicate of the BH entry above.
- BH spec front matter has `context: []` — low: the fix edits the spec front matter, and Implementation Notes already name the regenerated context.
- EC duplicate of the NuGet flat-container ordering concern — false: see the first rejected entry.


### Follow-up Blind Hunter triage (2026-10-26)

The oneshot route ran Blind Hunter and skipped Edge Case Hunter and Verification
Gap. All five findings were checked. Four were patched; one question about the
concurrent Story 13.6 amendment was deferred as unverified. The seven outstanding
pass-2 patches above are complete; the eighth was already resolved by status sync.

- BH-F1 — low / patch: the record promised fresh checks but only retained original-delivery results. Added Fresh follow-up verification with executed commands, counts and the strict failure limitation, separate from historical evidence.
- BH-F2 — medium / patch: run-level queries select the latest attempt after a rerun. Changed reproduction to `/attempts/1` and `--attempt 1`; fresh exact-attempt queries confirm the recorded source, CI success, Quality failure and Gate 4 success.
- BH-F3 — low / patch: cancelled preceding runs also contained the delivery. Corrected the description to first completed, non-cancelled CI/Quality runs.
- BH-F4 — low / patch: wording asserted a committed follow-up before a commit existed. Changed it to a commit-message requirement and post-commit classification check; the exact candidate has passed pinned commitlint.
- BH-F5 — maybe-false / defer, medium if confirmed: the concurrent Story 13.6 amendment requires existing realms and clients to be preserved, but its matrix currently names only fresh-realm verification. The implementation is still being authored, so missing preservation behavior is not established. Inspect its completed helper/tests and verify an existing-realm restart with differing generated credentials before judging the behavior. Recorded in deferred-work; no Story 13.6 intent or implementation was edited by this follow-up.

### Review Findings

Code review pass 3 (`bmad-code-review`, 2026-10-26) covered `2c8e204c...5b8a8607` (22 files, +1359/−103). Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance Auditor all completed. Acceptance Auditor reported no story-owned acceptance deviations. Result: 0 decision-needed, 7 patch, 5 defer, 8 rejected. The CI-pin decision was resolved on 2026-10-27: advance the hosted workflow pin to the gitlink.

- [ ] [Review][Patch] Point hosted CI at the Builds gitlink that contains the stdin/TRX repair [.github/workflows/ci.yml:25] — resolved 2026-10-27, option 1: `references/Hexalith.Builds` is `09af6ef8`, whose `domain-ci.yml` keeps the TRX verifier off the project-list stdin. `ci.yml` still calls `domain-ci.yml@4eb33928a1d8c7775f97221cf9edc171db0cb5f8`. Advance that pin to `09af6ef818beb27490c87c00ed83dd581a7e6d8d`. (blind-hunter; medium)
- [ ] [Review][Patch] Rendered realm adds `clientScopes` without `CreateDefaultClientScopes` [src/Hexalith.FrontComposer.AppHost/LocalWorkloadRealm.cs:64] — the checked-in realm has neither `clientScopes` nor `attributes`. The renderer always writes `clientScopes` and never sets `attributes.CreateDefaultClientScopes` to `"true"`. Keycloak 26 then skips built-in scopes (`openid`, `profile`, `roles`) for the existing user clients. `LocalWorkloadRealmPreservesUserRealmAndGeneratesPrivateMatchingCredentials` ignores added properties, and `LocalWorkloadRealmGrantsOnlyExplicitPlatformAudienceAndOperationScopes` requires the nine workload names only, so both stay green. (blind-hunter+edge-case-hunter+verification-gap; high)
- [ ] [Review][Patch] AppHost workload wiring is asserted as source text [tests/Hexalith.FrontComposer.Shell.Tests/Integration/FrontComposerUiAppHostTests.cs:300] — `AppHostWorkloadWiringIsLocalAndUsesTheRenderedSecretParameter` reads `Program.cs` and checks substrings. It does not build the Aspire model, so a wrong `RealmImportPath` or secret callback still passes, and the invalid-bearer 401/403 script does not check secret agreement or the persistent refusal. (verification-gap+blind-hunter; medium)
- [ ] [Review][Patch] `KeycloakPersistent` is parsed without the platform trim [src/Hexalith.FrontComposer.AppHost/Program.cs:29] — `bool.TryParse` does not trim, while `AddHexalithEventStoreSecurity` trims the same key before parsing. A padded `true` skips `LocalWorkloadRealm`'s refusal and still starts the persistent container, which does not re-import the generated secret. (blind-hunter; medium)
- [ ] [Review][Patch] Invalid-bearer start merges stderr into the ownership JSON [eng/verify-apphost-invalid-bearer.sh:82] — `aspire start --format Json` is redirected with `2>&1` and then sliced from the first `{` line. A log line that begins with `{` makes the single-object check fail closed, and there is no `--log-file` child log. `eng/run-epic9-live-proof.sh` already separates that log. (blind-hunter; medium)
- [ ] [Review][Patch] Accessibility result still says the workload client is unwired [\_bmad-output/implementation-artifacts/13-6-accessibility-result.md:28] — the same commit's `Program.cs` calls `WithEventStoreWorkloadClientCredentials`, and the repair spec's frozen block approves that amendment. The result still says the host never makes that call and that correction requires a separate amendment. (blind-hunter; medium)
- [ ] [Review][Patch] Ledger omits the v4.7.0 inventory asset id and raw hash [\_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:643] — the section stores the sealed-manifest SHA-256 and says the package inventory is `valid`, but it does not record asset `654681115` or raw SHA-256 `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a`. Those values are in this spec's publication queries. (blind-hunter; low)
- [x] [Review][Defer] Publish mode still sets a JWT authority without workload client credentials [src/Hexalith.FrontComposer.AppHost/Program.cs:24] — deferred: pre-existing. `WithJwtBearerSecurity` already set `Authentication__JwtBearer__Authority` before this change. The new call is limited to run mode. EventStore's publish path supplies external parameters; this AppHost did not. A published EventStore still fails `ValidateWorkloadAssertionIssuerOptions` when authority is set and client credentials are absent.
- [x] [Review][Defer] HFC2122 still promises removal in 4.7.0 [docs/diagnostics/diagnostic-registry.json:2512] — deferred: pre-existing. `removedIn` is `4.7.0` and `FocusFirstColumnFilterAsync` remains in `FrontComposerShortcutRegistrar.cs:259`. This diff does not edit that lifecycle. Reconfirmed from the earlier deferral.
- [x] [Review][Defer] Regenerated Epic 15 context dropped tenant/user and Fluent 5.0.1 posture [\_bmad-output/implementation-artifacts/epic-15-context.md] — deferred: agent-context file. The diff removes the mismatch/render-fault ban on tenant and user values and the "Components + Icons 5.0.1 GA not approved" sentence. Reconfirmed from the earlier deferral.
- [x] [Review][Defer] Recorded invalid-bearer run is still exit 1 [\_bmad-output/implementation-artifacts/13-6-accessibility-result.md:24] — deferred, medium if confirmed: the committed result is the pre-wiring run (curl exit 28, task unchecked). Whether `5b8a8607`'s wiring returns four 401/403 responses was not run. Settle by running `eng/verify-apphost-invalid-bearer.sh` against that AppHost.
- [x] [Review][Defer] Commit `62e3664c` is labelled Story 15.5 and contains only Story 13.6 paths — deferred: already accepted on 2026-10-26. Repair requires a history rewrite, which that decision declined.

#### Rejected

- Persistent Keycloak now throws — false: `LocalWorkloadRealm.Create` refuses `KeycloakPersistent=true` because a reused container does not re-import, and the amendment forbids resetting an existing user realm. The throw is the guard.
- Audience list omits platform services — false: `sample`, `tenants`, `parties` and `counter-scope-fixture` are this AppHost's domain-service app ids. EventStore's smaller realm belongs to a different host.
- Dispose leaves the secret behind — low: the unowned-file throw is the tested refusal in `LocalWorkloadRealmCleanupPreservesUnownedFiles`. A SIGKILL sweeper would add lease machinery for a path normal shutdown already cleans.
- Describe fails when `eventstore-admin` exists — false: the script calls `aspire describe eventstore`, which selects that resource. Admin resources are a different describe argument.
- DocFX setup failures all say "run dotnet tool restore" — low: the catch is intentional path redaction. `dotnet nuget locals` is not an everyday hang, and a timeout would be new process machinery. Already deferred as unverified from Story 15.4.
- Spec `done` vs sprint `review`, and strict validator exit 1 — false: the spec records both. The sprint row stays `review` for maintainer acceptance, and the validator failure is the accepted `unmapped`/`interleaved` history.
- README `blob/main` anchor is unresolved — false: the ledger heading is `Story 15.5 Compatibility Baseline Reconciliation (2026-10-26)`, the README uses that anchor, and `5b8a8607` is `main`.
- Missing lock-directory preflight — low: the default lock root is `/tmp`, which is present for normal local and CI runs. A pre-check is an extra branch for a custom `FC_EPIC9_LOCK_ROOT`.


### Current review ownership reconciliation (2026-10-29)

The pass-3 checkboxes above preserve that dated review snapshot. Their current
ownership/dispositions are below; they are not seven new baseline-amendment tasks.

| Historical pass-3 finding | Current disposition and evidence |
| --- | --- |
| CI pin requests `09af6ef8` | Superseded by independently owned Story 13.6 adoption. `.github/workflows/ci.yml:25` consumes `6952b59c3486ee50d32056c3fee07a07e8120bd5`. A fresh read of that exact GitHub workflow source confirms four heredoc-isolated `PYTRX` validators; no workflow pin was changed here. |
| Missing `CreateDefaultClientScopes` | Resolved by Story 13.6. `LocalWorkloadRealm.cs:69` preserves/sets the attribute and its existing regression checks it. No realm code changed here. |
| AppHost wiring asserted as source text | Still a verification gap at `FrontComposerUiAppHostTests.cs:326`; the existing case reads `Program.cs`. Explicit Story 13.6 deferred-work handoff appended; no Aspire model test or runtime fix claimed. |
| `Boolean.TryParse` whitespace rejection | False. Fresh `pwsh -NoProfile -Command` checked `true`, space-padded true, and tab/CRLF-padded true; all parsed successfully as true (exit 0, `boolean-whitespace.log`). |
| Invalid-bearer stderr merged into JSON | Still present at `eng/verify-apphost-invalid-bearer.sh:94`. Explicit Story 13.6 deferred-work handoff appended; no helper change claimed. |
| Accessibility result says workload client unwired | Superseded historical observation. The current Story 13.6 result records token exchange and all four actual 401 responses, including attributed hosted results. No fresh bearer execution or whole-runtime pass claimed here. |
| Missing original 4.7.0 inventory citation | Patched by the preserved October 29 draft observation: asset `654681115` and raw SHA-256 `c1e6662f6042c6ec72165a666a51935a626867a5c2e259382a822b2530d30f0a` are now explicit. The new approved observation separately cites the 4.7.1 assets. |

At the implementation handoff, the numbered story and REL-BASE-1 alias remain
`in-progress` pending the parent inventory correction and independent review;
the Parent task audit above records the subsequent green gate. Epic 15 and all
Fluent/Product, migration, publication and Release Owner approval states retain
their existing independent ownership. Accepted original commit-scope defects,
original baseline and historical unrelated-path declarations remain unchanged.


### Independent review of approved advancement (2026-10-29)

All three current layers completed before classification. They reviewed the
full original-baseline diff at `/tmp/fc-story15-5-full-history-411supss.diff`
(1,219,068 bytes), including independently owned commits since the original
baseline. Parent separately audited the complete 14-file current continuation
and scoped runtime exclusions. Blind Hunter's ten findings all concern those
inherited runtime/test additions; Edge Case Hunter found the affected derived
configuration seal; Verification Gap found inherited ordinary Tenants wiring
coverage. No public/runtime/dependency selection change is added by triage.

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| B1 typed input mounts after first render | medium: `FcDestructiveConfirmationDialog.OnAfterRenderAsync` returns on later renders, while `RequiredName` is mutable and renders the input conditionally; its accessibility observer is not installed for a later mount. | defer to Story 13.6; inherited runtime excluded by approved intent. |
| B2 reconciler UI callbacks resume off dispatcher | medium: `Reconcile`/`IssueCommand` await with `ConfigureAwait(false)` and then invoke Action callbacks without renderer dispatch; ordinary asynchronous Blazor adapters can update UI off context. | defer to owning feedback work; runtime/API change excluded. |
| B3 one connected confirmation followed by failed/stale read stalls | medium: the failed/stale read leaves completion pending, while the fallback wrapper suppresses polls whenever `IsConnected()` is true; absent a new notification only cancellation completes it. | defer to owning feedback work; inherited behavior. |
| B4 per-read cancellation token ignored | medium: `TryReconcileAsync(CancellationToken ignored)` uses only operation `token` for gate/read, so a caller fallback's bounded read token cannot cancel that read. | defer to owning feedback work; inherited behavior. |
| B5 Current may describe a pre-command record | maybe-false, medium if confirmed: core accepts Current and its public freshness enum describes the current projection, but the request explicitly leaves domain mapping caller-owned. A real adapter returning Current for pre-command state plus a disconnected fallback reproduction would establish false confirmation; no such adapter failure was demonstrated here. | defer unverified to owning feedback/adapter work. |
| B6 typed confirmation help lacks description association | medium: markup renders `ConfirmationHelp` in an unlabelled paragraph; `aria-describedby` names only the body, and the projection cannot discover that help. | defer to Story 13.6 accessibility work; inherited markup. |
| B7 /tenants route assertion can pass a pending/error surface | medium: after the second navigation the live case checks only focused heading and three absent selectors; it does not reuse the stronger terminal data/empty checks above. | defer to Story 13.6 full-topology verification. |
| B8 real .NET-to-browser download path is uncovered | medium: the browser suite fabricates `arrayBuffer()` and C# tests mock interop, leaving `DotNetStreamReference` transport unexercised. | defer to owning download work; no new failure claimed. |
| B9 feedback manifest omits contract modifiers | medium: `FormatParameters` records only types; property formatting treats accessor existence as visibility and omits init/nullability/defaults, allowing some future source-contract changes to evade that snapshot. | defer to owning public-surface verification; existing ApiCompat remains required. |
| B10 new service adopter documentation missing | medium: repository `docs/` has no registration/service/reconciler symbol matches, leaving adapters without stream/cancellation/dispatch/lifetime examples. | defer to owning feedback/download documentation work. |
| V1 ordinary Tenants receiver binding lacks executable test | medium, pre-verified: the source-string AppHost case passes with the receiver-policy variable substituted; disposable verification overwrites that binding, and the live suite is opt-in. | defer to Story 13.6; approved intent excludes AppHost/runtime changes. |
| E1 changed XML invalidates consumer configuration seal | medium: `evaluate_consumer_projections` hashes all committed build configuration and compares it to the enrolled seal, so changing the shared targets and Contracts.UI project rejects the candidate even with unchanged source selectors. | patch: recompute only that derived metadata; preserve guards and separately validate the actual prior-policy transition. |


### Review correction and final local disposition (2026-10-29)

E1 was patched by changing only the existing derived consumer configuration
seal in `eng/dependency-graph-policy.json` from
`68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca` to
`f5186d1b483c33e065ac542374a4b90c0da8257782ebc871d29aaf0ef3613762`.
A deep policy comparison confirms every other policy value is unchanged.
The 39-file committed configuration map differs from the continuation base
only in `Directory.Build.targets` and Contracts.UI's project baseline literal.
No source, selector, catalog pin, trust identity, or validator guard changed.

`python3 -m unittest tests.eng.test_dependency_graph.ConsumerProjectionTests tests.eng.test_dependency_graph.ApprovedProjectionExpectationTests`
— exit 0, 21 passed in 7.947s; log
`/tmp/fc-story15-5-implementation-20261029-e4k5a67r/consumer-seal-focused-tests.log`.
All previously green package/test lanes remain applicable to this derived JSON
metadata correction. Current total: 379 focused tests passed and seven real
ApiCompat packs passed; the failed first UI inventory attempt remains recorded.

Parent created isolated scratch commit
`8ac611e0ac8eba0386cb63bac47148fa9fb014eb` after pinned-message validation,
without moving HEAD or a branch. Its exact source/configuration includes the
completed amendment and corrected seal; later changes are documentation/status
only. `python3 eng/dependency_graph.py --root . validate --commit 8ac611e0ac8eba0386cb63bac47148fa9fb014eb`
— exit 0, `ok=true`, 69 edges, eight unchanged selectors and the corrected
consumer projection. Original Platform source/inventory and root Builds pins
are preserved. Command/output records are in
`/tmp/fc-story15-5-implementation-20261029-e4k5a67r/candidate-governance-results.json`
and `candidate-graph.log`; these are disposable local verification artifacts.

**Separate prior-policy push gate remains blocked:**
`python3 eng/dependency_graph.py --root . diff --event push --event-base 612d23485d7e2845d22102621a38d6c904e1ea90 --candidate 8ac611e0ac8eba0386cb63bac47148fa9fb014eb`
— exit 1, `consumer Compile/ProjectReference configuration changed from its sealed source projection`.
This uses the actual prior active policy and is not relabelled as a pass or
repaired by substituting candidate policy. The baseline configuration change
requires a separately governed integration/activation path before a push from
that prior base can qualify. No new transition mechanism, validation exception,
publication authorization, push, or hosted CI run is implied by local completion.
The accepted historical story-accounting failures remain separate limitations.

All three review layers completed: one current-amendment metadata correction,
eleven inherited follow-ups (including one unverified adapter concern) recorded
individually in deferred-work, and no runtime/API changes made from that review.
The spec records local implementation completion; sprint and REL-BASE-1 move to
review for maintainer acceptance. Epic 15 and independent approval gates remain
unchanged. This does not claim hosted/integration readiness.

Pinned commit-message check:
`node node_modules/@commitlint/cli/cli.js --edit /tmp/fc-story15-5-implementation-20261029-e4k5a67r/commit-message.txt --verbose`
— exit 0, zero problems/warnings. Installed CLI 21.2.2 equals the repository's
package-lock pin; successful evidence is `commitlint.log` in that scratch path.


Final post-review documentation and artifact hygiene checks passed. The final
scratch story-accounting run still exits 1 for preserved historical/later
ownership failures, while scratch commit
`8ac611e0ac8eba0386cb63bac47148fa9fb014eb` itself is `story-id=match` and
`disposition=owned`, with all its paths owned. Its exact output is
`/tmp/fc-story15-5-implementation-20261029-e4k5a67r/final-scratch-story-accounting.log`.
The final diff check detected one trailing blank line in the appended deferred
entries; removing that blank line is the only formatting correction.
