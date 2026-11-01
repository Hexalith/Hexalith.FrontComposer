---
title: 'Story 15.5: Qualify the Governance Transition'
type: 'chore'
created: '2026-10-30'
status: 'done'
route: 'oneshot'
story_id: '15.5'
story_key: '15-5-i-rel-base-1-reconcile-the-published-compatibility-baseline'
baseline_commit: 'e7e892b6ed584501f6dd18e11acd299948c0113f'
review_loop_iteration: 0
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-15-5-repair-verification-blockers.md'
---

<frozen-after-approval reason="user-authorized continuation of the existing governed transition and clean-environment qualification; no new policy authority inferred">

## Intent

Continue the completed bounded repair by establishing the exact authorized source tuple under AD-12/AD-12a and qualifying current governance in a clean environment. Reconcile only through the existing prior-base policy workflow; preserve catalog authority, dependency selections, validation guards, Git history, original baselines, unrelated changes and the prohibition on nested-submodule initialization. Run focused regressions, ordinary committed-source graph validation, actual-base dependency diff, infrastructure/CI governance, clean-consumer checks and the unchanged seven-library ApiCompat lane. Record exact source coordinates, commands, results and blockers in this continuation and append to the repair, activation handoff, release ledger and deferred ledger. Keep concurrent a2ed597bfbf3e178928144963ad8c9cb63077b3c's strict attribution failure explicit. Create a PR for hosted verification only after the relevant local gates pass; hosted success never supplies independent approval or closes Story 15.5 prematurely.

</frozen-after-approval>

## Implementation Notes

- Initial HEAD and remote main were f3fef9f2dad49638521d7bc2fd3bf43c8056b516. Read-only hosted inspection preceded all edits. Quality 41019771520 attempt 1 failed catalog preparation because its actual base selected unavailable Platform a1965486cbcb23aa1693f4e7dc9bd4f21bbbc574; Gate 2b and Gate 4 were skipped. CI 41019771901 attempt 1 passed the reusable build/test job but failed dependency-governance exact-object acquisition for the same unavailable revision.
- The original workspace had an unrelated dirty Platform checkout at a8e6276b512186b1e9e35160b8c150a50a17f789. During read-only investigation an external commit e7e892b6ed584501f6dd18e11acd299948c0113f landed locally and remotely, recording that pointer. This task did not create or push that commit. Qualification is pinned to it and uses its actual parent f3fef9f2dad49638521d7bc2fd3bf43c8056b516. Original story baselines remain unchanged.
- Scratch directory: `/tmp/fc-15-5-governance-20261030-zqwjdxk5/`. A detached clean worktree at `checkout/` has independent local clones of only root-declared dependencies, checked out at committed gitlinks. No ignored npm folders, original build products or nested initialized repositories were copied. Commands and results are logged by `run.py` to `commands.jsonl` and named logs.
- AD-12 requires one event-base policy for both graphs. AD-12a seals the owner, every root selector, consumer configuration and actual root Builds source. The active policy is unchanged. An inactive correction cannot authorize its own event, and a scratch descendant cannot substitute for a landed remote base. Current source authority is investigated as exact committed data, not established by rehashing it.
- Intent gaps: none for evidence gathering and preservation. The blocked policy activation requires owner action before the requested passing transition can exist. No authority to bypass that prerequisite is inferred. Footprint: five documentation files; no source, policy, catalog, dependency, guard, workflow, API or runtime changes.
- Sprint Story 15.5 and REL-BASE-1 remain at review; monotonic status synchronization makes no tracking edit.

## File List

- `_bmad-output/implementation-artifacts/spec-15-5-qualify-the-governance-transition.md`
- `_bmad-output/implementation-artifacts/spec-15-5-repair-verification-blockers.md`
- `_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`


## Source authority and transition disposition — 2026-10-30

The exact authorized tuple comes from the committed active policy, not from a
newly calculated candidate seal. Both e7e892b6ed584501f6dd18e11acd299948c0113f
and its actual parent f3fef9f2dad49638521d7bc2fd3bf43c8056b516 contain identical
policy bytes: SHA-256 `e9ad463b10e144c9117378c92f518306e914680e6ac9a2cc2542a9e8a07b5bd6`.
The [approved root-fallback record](spec-13-6-repair-root-catalog-fallback.md)
records that tuple's implementation f54ce542db8675d685c50408b390158152283106
and later activation e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e. Recomputing the
latter's exact committed root selectors reproduces the active seal. That history
authorizes its recorded context; it does not authorize later selections.

| Coordinate | Authorized by the active policy | Selected at qualification source e7e892b6ed584501f6dd18e11acd299948c0113f |
| --- | --- | --- |
| Platform owner | `b742b1b7a496ac72d6961ca6db5213d3df42d234` | `a8e6276b512186b1e9e35160b8c150a50a17f789` |
| Root Builds | `81225bcb6151c38ae891abd9110af17385e1b3b9` | `0b17f8022cdc23b8b242b000f5b2aa15580184ac` |
| Root selector SHA-256 | `ddf83f55971c78a6d265f46b3bc9d6364c1acf85cfc88ee5c3e60818c13f81ef` | `b6534b18c7f4462366def97b1a0e5e79d1224e17c3543833de3042895dc4a40d` |
| Platform raw inventory SHA-256 | `7f4753f5bd787b2948eafb6da1b28b103246f3022b57710129cc61b714a58a29` | `2145b99e0580abdb05a812dc81ea009b4364d12990fe176a6d0dc5988efccedb` |
| Consumer configuration SHA-256 | `f5186d1b483c33e065ac542374a4b90c0da8257782ebc871d29aaf0ef3613762` | Identical |
| Platform .gitmodules SHA-256 | `6da4f5297d79bfa54afcb8daacda70670af11868ba1738cc185f584db8eb6ab7` | Identical |
| DaprSelfHostedMtls.cs SHA-256 | `703072feb8b609c2661a1c7eaa955fbd2c9d838402520b4fc425bda2000ba1a8` | Identical |
| DaprSelfHostedMtlsSubscriber.cs SHA-256 | `55c280284d0a305b1d6b6667f31362b7c4e4a3cf39890b6175d913cd762bb9c6` | Identical |

The consumer remains `github.com/hexalith/hexalith.frontcomposer`, project
`src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj`.
`HexalithPlatformRoot` binds exactly the two source files above, linked as
`Platform/DaprSelfHostedMtls.cs` and `Platform/DaprSelfHostedMtlsSubscriber.cs`.
Root Builds supplies `src/libraries/Hexalith.Builds.Tooling/Hexalith.Builds.Tooling.csproj`.
Both Platform inventories have 17 mappings and the same seven inventory-only
identities: Agents, Chatbot, Conversations, Folders, Projects, Timesheets and
Works. They remain untrusted and uninitialized. Unchanged source-file hashes do
not authorize a different owner inventory or tooling revision.

Every root selector is part of the seal, including evidence-only dependencies:

| Root path | Authorized tuple (actual activation e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e) | Current committed tuple |
| --- | --- | --- |
| `references/Hexalith.AI.Tools` | `3f194e17174994d308ec84af9ee2b5aa68674d0d` | `70dbf73e80787299c832d30d911b16b105ab194d` |
| `references/Hexalith.Builds` | `81225bcb6151c38ae891abd9110af17385e1b3b9` | `0b17f8022cdc23b8b242b000f5b2aa15580184ac` |
| `references/Hexalith.Commons` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` | `3edfbdafc7d4fa915286dce8a37dc4cc7c06085a` |
| `references/Hexalith.EventStore` | `36c78f3435d2d492a73f1b0707cf865c8735d740` | `7b1df62102efb19da03553a890c62eb5ad6aeafe` |
| `references/Hexalith.Memories` | `72c36c7113c750c8b809f9d72b945a2a8c2d4555` | `9ea24458f949589bb8e59f9334161009487509d1` |
| `references/Hexalith.Parties` | `492a8cc1417c625cdfb7fd4c946686ef88adab9d` | `e2528a5d5853641f2b38e5e2e34bb02c0cc4be23` |
| `references/Hexalith.Platform` | `b742b1b7a496ac72d6961ca6db5213d3df42d234` | `a8e6276b512186b1e9e35160b8c150a50a17f789` |
| `references/Hexalith.PolymorphicSerializations` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` | `221ac4592ba908ae7661ba1fa9e2d1fcc23baee0` |
| `references/Hexalith.Tenants` | `7ddb864cda5e942925b056391617eae4895c1253` | `0d469d7c8fedc383f6258cbf2798e62cf9847304` |

Current raw root catalog SHA-256 is `fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10`;
the sealed historical Builds revision's raw catalog SHA-256 is
`6833662f8b6e49d6001a6b89c7ee6b9065a972554ab72c3c41fc935411f70966`. These are identity observations,
not a new catalog authority or a version change. The exact command
`git diff --name-only 81225bcb6151c38ae891abd9110af17385e1b3b9 0b17f8022cdc23b8b242b000f5b2aa15580184ac -- src/libraries/Hexalith.Builds.Tooling`
from the root Builds repository identifies 25 changed paths, including runtime
composition, durable state, attachment admission and native-test execution.
Adopting that source is substantive authorization; matching the two helper files
cannot establish tooling equivalence or replace its review.

`python3 /tmp/fc-15-5-governance-20261030-zqwjdxk5/inspect-source-tuple.py`
ran from the clean checkout, exit 0. It uses committed Git blobs, the existing
canonicalization and raw inventory parsing helpers, and asserts the authorized
historical seals. Its current inventory output is explicitly non-authorizing
metadata. No modified policy is loaded into collection, validation, acquisition,
build scheduling or release acceptance. Full raw coordinates are retained in
`source-tuple-reproducible.json` and `source-tuple-inspection.log` in scratch.

The actual event base is a third, separately refused source tuple:
`f3fef9f2dad49638521d7bc2fd3bf43c8056b516` selects Platform `2cbb8a68d277bdd55e321972b8aa11ef015fc54e`;
its root selector SHA-256 is `f33ed81f6900248864406f0fbd063b3c9835b4dc0fd82a5a732cc4dd29a61748` and
Platform raw inventory SHA-256 is `5af47f04148037a80eeadeb47a7dfbee53466e8ff40c4338a25410b583345084`.
All eight other root selectors equal the current column above. Actual-base
refusal occurs while validating this tuple; standalone candidate refusal occurs
at a8e6276b512186b1e9e35160b8c150a50a17f789. Neither refusal grants the other
context authority.

Raw nested Builds catalog metadata remains an independent obligation, even though
the helper consumes root Builds:

| Platform context | Exact nested Builds selector | Catalog availability in the clean root Builds object store | Raw catalog SHA-256 |
| --- | --- | --- | --- |
| Authorized owner b742b1b7a496ac72d6961ca6db5213d3df42d234 | `51a71cb1cfe860d5fc135eb3b8521fd80c413810` | Available | `95622edbc970beea37bf22c2fbd5bfe43cf6e69f2087a8a0f6fac0e0b3097ef6` |
| Actual base owner 2cbb8a68d277bdd55e321972b8aa11ef015fc54e | `0b17f8022cdc23b8b242b000f5b2aa15580184ac` | Available | `fa3df877f1304db565456dd6c79da7d13ad2cff600fea94331ea093523568a10` |
| Current owner a8e6276b512186b1e9e35160b8c150a50a17f789 | `1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9` | Missing locally: `fatal: not a tree object` | Unavailable; no digest invented |

`python3 /tmp/fc-15-5-governance-20261030-zqwjdxk5/inspect-source-tuple.py`
was rerun after review, exit 0, with each catalog lookup's own availability/error
record preserved in `source-tuple-inspection-reviewed.log`. The diagnostic's
successful exit means its report was produced, not that all metadata exists.
No networking was performed and remote absence of 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9
is not claimed. Ordinary commands still fail the owner seal before reaching
this lookup. After an authorized policy activation, existing bounded preparation
must acquire/verify this exact metadata from the canonical Builds identity;
full raw graph acceptance must authenticate every in-boundary catalog.

AD-12/AD-12a in the [architecture spine](../planning-artifacts/architecture/architecture-gov-1-2026-07-19/ARCHITECTURE-SPINE.md)
and [FC-DEP-1](../contracts/shared-catalog-dependency-governance-2026-07-19.md)
requires both graphs to use the immutable event-base policy. Current main's base
already fails its owner-pin seal. Consequently a candidate-only policy refresh
cannot yield a passing actual-base diff. No policy was changed or hash refreshed.

The existing governed sequence remains the required handoff:

1. Dependency-governance maintainers obtain substantive review of the exact
   proposed owner/inventory/root/tooling tuple above and prepare a bounded inactive
   policy precursor. Catalog authority, trust, expectations, limits, consumer
   bindings and independent approvals must retain their existing boundaries.
2. That reviewed precursor must land through an independently authorized process;
   preserve its old-base refusal. This task neither merges it nor bypasses any
   protection. A scratch descendant or a self-to-self diff is not a landed base.
3. A later real event can then use the landed precursor as its prior policy.
   Recheck the live base, ordinary committed-source validation, exact event diff,
   infrastructure governance and hosted CI/Quality at that later source.

The requested successful transition and PR are therefore **blocked**, not complete.
The user's prerequisite to create a PR after relevant local gates pass is unmet.
No PR or remote write was made, and no owner acceptance of a new tuple is inferred.

## Verification — clean checkout at e7e892b6ed584501f6dd18e11acd299948c0113f

All commands below ran from
`/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout`, unless explicitly stated.
The checkout was initially clean and had no npm directories. Root-only dependency
clones use local object-store alternates; this is a clean working-directory and
npm qualification, not a cold-object availability proof. Global NuGet caches are
shared. SDK 10.0.403 satisfies global.json's 10.0.402/latestPatch policy; runtime
is 10.0.14, Node is 26.4.0, Python is 3.14.4. Release builds reproduce the
package-based verification lanes; no source-mode runtime/topology acceptance is
claimed. Exact argv, CWD, UTC start, exit, duration and logs are in `commands.jsonl`.

`python3 eng/dependency_graph.py --root . validate --commit e7e892b6ed584501f6dd18e11acd299948c0113f`

Exit 1: current Platform a8e6276b512186b1e9e35160b8c150a50a17f789: consumer source projection owner pin changed. This is ordinary committed-policy validation. Log: `graph-validate.log`.

`python3 eng/dependency_graph.py --root . diff --event push --event-base f3fef9f2dad49638521d7bc2fd3bf43c8056b516 --candidate e7e892b6ed584501f6dd18e11acd299948c0113f`

Exit 1: actual base Platform 2cbb8a68d277bdd55e321972b8aa11ef015fc54e: consumer source projection owner pin changed. The supplied base is the real direct parent, not a self-comparison. Log: `dependency-diff-push.log`.

`python3 eng/dependency_graph.py --root . acquire-catalogs --event-base f3fef9f2dad49638521d7bc2fd3bf43c8056b516 --candidate e7e892b6ed584501f6dd18e11acd299948c0113f`

Exit 1: the same actual-base owner-pin guard. No candidate policy is substituted to authorize acquisition. Log: `catalog-preparation.log`.

`npm ci --ignore-scripts --no-audit --no-fund`

Exit 0: 352 packages installed from the existing lockfile in the clean root only. No original npm folder, lockfile, dependency selection or guard was changed. Log: `npm-ci.log`.

`python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_workflow_source_closure.py tests/eng/test_dependency_handoff.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py tests/eng/test_validate_contract_artifacts.py eng.tests.test_validate_story_artifacts`

Exit 0: 427 tests run, 425 passed and 2 skipped; 125.522s runner time. Includes projection, forged evidence, candidate self-authorization, zero base, bounded acquisition, actual workflow preparation, handoff and release-consumer refusal regressions. Log: `dependency-regressions-qualified.log`.

`python3 -m unittest tests/eng/test_pack_release_packages.py tests/eng/test_release_prepublish.py`

Exit 0: 60 passed. Log: `release-regressions.log`.

`dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`

Exit 0: zero warnings/errors. Log: `shell-build.log`.

`dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests -class Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests`

Exit 1: 106 total, 105 passed, 1 failed; zero errors/skips/unrun. Only CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds fails, on the preserved a8e6276b512186b1e9e35160b8c150a50a17f789 owner-pin guard. GovernanceBuildTrait_IsDeclaredOnlyInTheProjectTheHeavyLaneRuns passes without changing its scanner. The clean-environment self-link obstruction is avoided, while the governance gate remains failed. Log: `clean-governance-qualified.log`.

`dotnet build tests/Hexalith.FrontComposer.Testing.Tests/Hexalith.FrontComposer.Testing.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`

Exit 0: zero warnings/errors. Log: `testing-build.log`.

`dotnet tests/Hexalith.FrontComposer.Testing.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Testing.Tests.dll -class Hexalith.FrontComposer.Testing.Tests.PackageBoundaryTests`

Exit 0: 5 passed, zero errors/failures/skips/unrun. Existing clean temporary consumer actually packs, restores and compiles; its generated smoke test is compiled, not executed. Log: `testing-boundaries.log`.

`dotnet build tests/Hexalith.FrontComposer.Contracts.UI.Tests/Hexalith.FrontComposer.Contracts.UI.Tests.csproj --configuration Release --nologo -m:1 -p:NuGetAudit=false`

Exit 0: zero warnings/errors. Log: `contracts-ui-build.log`.

`dotnet tests/Hexalith.FrontComposer.Contracts.UI.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Contracts.UI.Tests.dll -class Hexalith.FrontComposer.Contracts.UI.Tests.PackageBoundaryTests`

Exit 0: 4 passed, zero errors/failures/skips/unrun, including its temporary consumer build/executable run. Log: `contracts-ui-boundaries.log`.

`env GITHUB_WORKSPACE=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout bash -x /tmp/fc-15-5-governance-20261030-zqwjdxk5/quality-gate4.sh`

Exit 0: all seven inventory-derived library packs pass against 4.7.1, with baseline skipping false and no new suppression. The existing Quality Gate 4 run block was extracted verbatim and run with bash tracing; 19.767s wall time. Its full seven expanded pack invocations are retained in api-compat-seven.log. Log: `api-compat-seven.log`.

`dotnet pack src/Hexalith.FrontComposer.Cli/Hexalith.FrontComposer.Cli.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Cli/bin/`

Exit 0: additional inventory tool package/symbols; the SDK tool layout is separate from the seven-library ApiCompat lane. Log: `cli-pack.log`.

`python3 scripts/validate-nuget-packages.py /tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/nupkgs-validation`

Exit 0: all eight fresh inventory packages and kernel boundaries validate. The isolated output has no old package versions. Log: `package-validation.log`.

`python3 scripts/validate-consumer-package-references.py /tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/nupkgs-validation --work-directory /tmp/fc-15-5-governance-20261030-zqwjdxk5/clean-consumers`

Exit 0: both additional fresh PackageReference-only consumers restore/build with zero warnings/errors. Contracts-only has no Blazor/Fluent/Fluxor runtime output. This script compiles the programs; execution is not claimed. Log: `clean-consumers.log`.

`python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md --candidate a2ed597bfbf3e178928144963ad8c9cb63077b3c`

Exit 1: story-id=no-match, disposition=unmapped and the same four listed-unowned delivery paths. This remains an independent strict failure, not repaired by this continuation’s scope accounting. Log: `strict-a2-attribution.log`.

The two skipped Python cases are:

- `eng.tests.test_validate_story_artifacts.ReviewVerifierTests.test_artifact_validation_failure_prevents_done_review_completion` — `bmad-story-automator skill is not installed`.
- `eng.tests.test_validate_story_artifacts.ReviewVerifierTests.test_incomplete_review_reports_workflow_not_complete` — `bmad-story-automator skill is not installed`.

These are optional Story Automator integration checks; their skip is retained,
not a claim that those integration paths passed.

The first broad regression run preceded npm installation and failed six planner
assertions because `semantic-release` was absent. The first governance run was
104/106 with the missing `@semantic-release/commit-analyzer` failure plus the owner
seal. Both first results remain in their original logs. The same broad suite and
governance classes were rerun after pinned `npm ci`; their final results above
supersede only those environment failures. No assertion, package selection or
validation guard was weakened.

Each of the seven ApiCompat invocations is recorded below exactly as expanded by
the existing shell block:

```text
dotnet pack src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Contracts/bin/
dotnet pack src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Contracts.UI/bin/
dotnet pack src/Hexalith.FrontComposer.Mcp/Hexalith.FrontComposer.Mcp.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Mcp/bin/
dotnet pack src/Hexalith.FrontComposer.Schema/Hexalith.FrontComposer.Schema.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Schema/bin/
dotnet pack src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Shell/bin/
dotnet pack src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.SourceTools/bin/
dotnet pack src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj --configuration Release --output ./nupkgs-validation -p:Version=4.7.1-ci -p:PackageVersion=4.7.1-ci -p:EnableFrontComposerPackageValidation=true -p:FrontComposerPackageValidationBaselineVersion=4.7.1 -p:FrontComposerPackageValidationSkipBaseline=false -p:BaseOutputPath=/tmp/fc-15-5-governance-20261030-zqwjdxk5/checkout/artifacts/library-package-validation/Hexalith.FrontComposer.Testing/bin/
```

## Hosted observations and remaining gates — 2026-10-30

Read-only GitHub connector calls used `github_fetch` for the exact main ref and
`actions/runs?head_sha=e7e892b6ed584501f6dd18e11acd299948c0113f&per_page=15`,
`github_fetch_workflow_run_jobs` for the two run IDs below, and
`github_fetch_workflow_job_logs` for jobs 123957050415 and 123959451108.
These are existing push runs of the concurrent source, not PR verification of
this documentation continuation. Raw observations/logs are local scratch in
`hosted-observation.json` and `hosted-e7-jobs-and-logs.json`.

- [Quality 41023802429, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023802429/attempts/1),
  build-and-test job 123957050415: failed catalog preparation on actual-base
  Platform 2cbb8a68d277bdd55e321972b8aa11ef015fc54e, `consumer source projection
  owner pin changed`. Infrastructure Gate 2b and ApiCompat Gate 4 were skipped.
- [CI 41023803195, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41023803195/attempts/1):
  reusable build-and-test job 123957052919 succeeded, including the package-consumer
  and unit-test steps. Dependency-governance job 123959451108 failed exact-object
  acquisition on the same base Platform owner-pin guard; affected builds and
  release-proof creation were skipped. A successful build/test job is not a
  passing overall CI or dependency-governance result.

The earlier f3fef9f2dad49638521d7bc2fd3bf43c8056b516 failures on unavailable
base Platform a1965486cbcb23aa1693f4e7dc9bd4f21bbbc574 remain historical failures;
the later source reaches the existing projection guard and does not repair the
earlier attempt. Local ApiCompat and clean consumers do not establish skipped
hosted gates or authorize publication.

Concurrent commit a2ed597bfbf3e178928144963ad8c9cb63077b3c retains its strict
failure. No shared/process disposition, exemption or owner acceptance was added.
The original published-baseline spec retains
2c8e204c9f1ca5c2c44d70005d481dd70f89f3fb, the activation handoff retains
7319f44bcf02b67d7ee09a315ba8aa9c7251b1f6, and the repair retains
2fcb1e9b5802ed32fc387680c6e42369130e0384. This qualification uses its own starting
source; none of those baselines moved. Story 15.5 and REL-BASE-1 stay at review.
Release Owner, publication, Fluent, Product and EventStore approvals remain open
or at their previously recorded dispositions. No approval is inferred from tests,
review, hashes, Git history or hosted workflow success.

### Timestamped hosted status distinction

At **2026-10-30 08:20:37 UTC**, read-only `github_fetch` of
`https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/41023802429`
reports Quality attempt 1 still **in_progress**, with no overall conclusion.
Its build-and-test job 123957050415 is **completed/failure**; infrastructure and
ApiCompat are skipped in that job. CI 41023803195 attempt 1 is
**completed/failure**. References above to Quality's failure describe the failed
job/step, not a completed overall-run result. No result is inferred for jobs
still running. These observations belong to e7e892b6ed584501f6dd18e11acd299948c0113f,
not a new PR.

### Reproduce the raw tuple inspection

The full diagnostic implementation below runs from the checkout containing exact
Git objects. It calls the tracked engine's raw object helpers and creates reports
only; it neither edits nor substitutes policy. The same script is retained in the
receipt archive. Missing catalogs are reported explicitly.

```python
from pathlib import Path
import hashlib,json,subprocess,sys
sys.path.insert(0,'eng')
import dependency_graph as dg
root=Path.cwd();scratch=root.parent;commit='e7e892b6ed584501f6dd18e11acd299948c0113f'
policy,raw,projection=dg.load_policy_at_commit(root,commit)
identity='github.com/hexalith/hexalith.frontcomposer';owner='github.com/hexalith/hexalith.platform'
def selectors(rev):
 return [{k:e[k] for k in ('path','repository','commit')} for e in dg._owner_edges(root,rev,identity,1,policy,root)]
def inventory(rev):
 owner_path=root/'references/Hexalith.Platform';limits=policy['resource_limits']
 modules=dg.read_gitmodules(owner_path,rev,limits['max_gitmodules_blob_bytes']);links=dg.list_gitlinks(owner_path,rev,limits['max_ls_tree_bytes_per_owner_commit'])
 mappings={row['path']:dg.normalize_identity(row['url'],'raw diagnostic') for row in modules.values()}
 assert set(mappings)==set(links)
 return [{'owner_repository':owner,'owner_commit':rev,'path':path,'repository':mappings[path],'commit':sha,'depth':2} for path,sha in sorted(links.items())]
current=selectors(commit);authorized=selectors('e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e');row=policy['consumer_source_projections'][owner]
assert dg.canonical_digest(authorized)==row['root_selectors_sha256']
actual_owner=next(r['commit'] for r in current if r['repository']==owner)
assert dg.canonical_digest(inventory(row['owner_commit']))==row['inventory_sha256']
assert dg.canonical_digest(dg.consumer_configuration(root,commit,policy))==row['consumer_configuration_sha256']
result={'authority':'non-authorizing raw metadata comparison; no candidate policy used for admission', 'source':commit,'policy':projection,'authorized_projection':row,'authorized_root_example':'e04652d7f3fef2d3c2c6a77504a73b0c2f363a6e','authorized_root_selectors':authorized,'current_root_selectors':current,'current_root_selectors_sha256':dg.canonical_digest(current),'current_raw_platform_inventory_sha256':dg.canonical_digest(inventory(actual_owner)),'current_platform_inventory':inventory(actual_owner),'current_source_hashes':{file:hashlib.sha256(dg.read_blob(root/'references/Hexalith.Platform',actual_owner,file,policy['resource_limits']['max_contract_tree_blob_bytes'],'raw diagnostic source')).hexdigest() for file in row['source_files']}}
(scratch/'source-tuple-reproducible.json').write_text(json.dumps(result,indent=2)+'\n')
base='f3fef9f2dad49638521d7bc2fd3bf43c8056b516'
base_selectors=selectors(base);base_owner=next(r['commit'] for r in base_selectors if r['repository']==owner)
result['actual_base']={'commit':base,'root_selectors':base_selectors,'root_selectors_sha256':dg.canonical_digest(base_selectors),'platform_inventory_sha256':dg.canonical_digest(inventory(base_owner)),'platform_owner':base_owner}
result['catalog_metadata']={}
builds_root=root/'references/Hexalith.Builds'
for name,revision in [('authorized',row['owner_commit']),('actual_base',base_owner),('current',actual_owner)]:
 selected_builds=[r for r in inventory(revision) if r['repository']==policy['builds_identity']]
 reports=[]
 for edge in selected_builds:
  try:
   blob=dg.read_blob(builds_root,edge['commit'],'Props/Directory.Packages.props',policy['resource_limits']['max_catalog_blob_bytes'],'non-authorizing nested catalog diagnostic')
   reports.append({'commit':edge['commit'],'path':edge['path'],'catalog_sha256':hashlib.sha256(blob).hexdigest() if blob is not None else None,'available':blob is not None})
  except dg.GraphError as error:
   reports.append({'commit':edge['commit'],'path':edge['path'],'catalog_sha256':None,'available':False,'error':str(error)})
 result['catalog_metadata'][name]=reports
(scratch/'source-tuple-reproducible.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
```

### Receipt retention and documentation checks

Raw receipts are retained outside temporary storage in the machine-local archive
`/home/administrator/.local/share/hexalith/frontcomposer/story-15-5/2026-10-30-e7e892b6/verification-receipts.tar.gz`.
SHA-256: `eb803d6eac893f15055b951c346b264c4cfba475c32f531072d1c7b647128920`.
The 53,525-byte archive has 42 members, including commands/results, first and final
gate logs, exact diagnostic implementation, source tuple reports, hosted job/log
snapshots and per-member SHA-256 manifest. Every member hash was verified after
archive creation. Retrieve it from that absolute local path and verify with
`sha256sum -c verification-receipts.tar.gz.sha256` from its containing directory.
This local archive is not a hosted or governed release-evidence artifact; no
remote retention or release compliance is claimed. The inline commands, source
coordinates and results remain in Git even if local receipts become unavailable.

From the primary repository, the following all exit 0:

- `python3 eng/validate-story-artifacts.py`.
- `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-qualify-the-governance-transition.md`.
- `pwsh -NoProfile -File eng/validate-docs.ps1 -SkipDocFx -SkipSnippetBuild`; no fresh DocFX/snippet build is claimed.
- `git diff --check`.
- `node node_modules/@commitlint/cli/cli.js --edit /tmp/fc-15-5-governance-20261030-zqwjdxk5/qualification-commit-message.txt --verbose`; installed/pinned CLI 21.2.2, zero problems/warnings before commit.

Preservation checks confirm complete historical prefixes of the repair, activation
handoff, release ledger and deferred ledger after repository newline normalization;
unchanged policy blob; unchanged production/configuration/guard/selector paths;
unchanged original npm self-link; empty initialized nested Git-entry lists; and a
clean verification checkout. All five new/updated documents use CRLF. The external
concurrent commit is preserved. Original Story 15.5 and REL-BASE-1 remain review.
This continuation remains in-review with a blocked governance transition and PR;
it is not marked done.

## Review Triage Log

The independent Blind Hunter reviewed all five files and their receipts at the
parent model level: 37.969 kB, finding floor 7. The oneshot route skips Edge Case
Hunter and Verification Gap. Every finding was checked against actual artifacts.

- BH-1 — medium / patch: receipts existed only in scratch. Added a verified local archive, exact SHA-256, per-member manifest and retrieval path; distinguish local retention from hosted release evidence.
- BH-2 — medium / patch: reproduction used a scratch-only script. Included the complete raw diagnostic implementation and retained it in the receipt archive.
- BH-3 — medium / patch plus existing-prerequisite handoff: documented all three Platform contexts' nested Builds selectors and catalog availability/digests. Current nested 1884a88ab80d3d0ae80c09a5ed9dfe23867d9fb9 is absent locally. Its acquisition remains behind the existing authorized-base workflow; no guard is bypassed to obtain a digest.
- BH-4 — medium / patch: added actual-base f3fef9f2dad49638521d7bc2fd3bf43c8056b516's root/inventory digests and distinguished base and candidate owner-pin refusals.
- BH-5 — low / patch: named both skipped integration tests and their optional missing-skill reason.
- BH-6 — medium / patch: recorded timestamped overall-run versus failed-job status; Quality remained in progress at observation, while CI and the relevant Quality job had failed.
- BH-7 — low / patch: corrected deferred evidence wording from source bytes to source hashes and exact Git coordinates.

These patches improve the evidence record. They do not approve a new source tuple
or remedy failed graph/governance gates. The existing policy/metadata prerequisites
are handed off explicitly; no new behavior or independent approval is inferred.


## Code review — 2026-10-31

Follow-up review of the three Story 15.5 commits that had no four-layer review:
b1d89648ae6feed1c89e48c265de013e1507fa97 (this record),
188d8b733090a83108bd006aaa9fa71cd9a5a2d8 (the 4.7.2 review patches, governed by
[the 4.7.2 record](spec-15-5-advance-the-published-baseline-to-4-7-2.md#code-review--2026-10-31))
and 6811bfd6ca47e3272c692288503c68552040df4c (the precursor round-3 resolution,
governed by [the precursor record](spec-15-5-prepare-the-inactive-policy-precursor.md#code-review-round-3--2026-10-30)).
Each commit was diffed against its own parent (147.235 kB; Blind Hunter floor
min(floor(sqrt(147.235) + 1), 10) = 10). Four independent reviewers ran at the
parent model capability: Blind Hunter, Edge Case Hunter, Verification Gap and
Acceptance Auditor. Every finding was verified against HEAD
e442544c, Git history, hosted runs and local probes before triage.

### Review Findings

- [x] [Review][Decision] Precursor promoted to `done` without the human amendment its own record requires — 6811bfd6 sets `spec-15-5-prepare-the-inactive-policy-precursor.md` to `status: 'done'` on Blind Hunter BH-31-13 alone. The same record's 2026-10-30 resumed section (line 490) says "this spec must not be promoted to done. Completing the successor requires a human amendment permitting renewed current-tuple qualification and the necessary Parties content/dependency-selection repair". Its frozen intent (a draft owner-review PR, no merge) was never met: the precursor landed by direct merge 92eff2e7 with no PR, its policy is recorded as unreviewed and owner acceptance is open. No human amendment is recorded. Options: (1) keep `done` and record a dated human amendment that the bounded record work is closed, with the open items carried by the three round-3 deferrals; (2) set the precursor to a non-done state (for example `blocked`) until owner acceptance is settled. Resolved 2026-10-31: human decision, option 1. The precursor stays `done`; a dated human amendment is recorded in the precursor record (patch below).
- [x] [Review][Patch] Record the human amendment that keeps the precursor `done` [_bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md:721] — append a dated `Human amendment — 2026-10-31` section. It states that the 2026-10-31 human decision closes the bounded record work and supersedes the line-490 condition. It also states that `done` does not accept or review the f75d774d policy, close owner acceptance or grant activation, and that the open items stay with the three round-3 deferrals.
- [x] [Review][Patch] Round-3 records say main is red on a55960f5's unsealed Builds move, but bf87ab62 had already resealed it [_bmad-output/implementation-artifacts/deferred-work.md:11089] — 6811bfd6 (authored 17:19+01:00, committed 19:22+01:00 on parent 51121f24) adds a high deferral saying main's dependency governance "has been red since" a55960f5 and "every later push stays red until a separately authorized seal transition renews all three fields"; its ledger entry calls the reseal "deferred here" and the sprint-status comment (sprint-status.yaml:232) says "main stays red on a55960f5's unsealed Builds move (deferred)". Ancestor bf87ab6224979d7ba8a297b23365ec99db861cbf (`spec-requalify-selected-hosting-helper`) had already renewed `builds_commit` (a22b55f0 → bb1b71eb), `root_selectors_sha256` and `consumer_configuration_sha256`. `python3 eng/dependency_graph.py --root . validate --commit <sha>` exits 0 at bf87ab62, 86e07bab, 51121f24 and 6811bfd6, and exits 1 only at 188d8b73. Hosted CI and Quality are success at 86e07bab (41223391369/41223391036), 51121f24 (41225916404/41225916043) and 6811bfd6 (41229072774/41229072509); bf87ab62's own CI 41223229013 and Quality 41223228512 were cancelled, the cancel-in-progress pattern already deferred for decbb195. Correct the high deferral in place as resolved by bf87ab62, append a dated ledger correction and rewrite the sprint-status reason. HEAD e442544c (`fix: submodules`, outside this review) is red again for a different reason: validate exits 1 with `github.com/hexalith/hexalith.platform@03a099bba258c37a8f16c8b2e842ae842b1ad6d6: consumer source projection owner pin changed`.
- [x] [Review][Patch] Gate 4 collector misses quoted, space-delimited and `=`-delimited override switches [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2096] — 188d8b73's comment and commit claim the collector accepts every MSBuild property-switch spelling, but `propertySwitch` requires a colon and `(?<!\S)` fails after a quote. A local probe shows `dotnet pack` applies `-p Foo=X`, `-p=Foo=X` and `--property Foo=X`, and bash passes a quoted `"-p:FrontComposerPackageValidationBaselineVersion=4.7.1"` to the pack unquoted. None is collected, so such a later Gate 4 override would reach ApiCompat while the live row still evaluates 4.7.2. Accept `:`, `=` or whitespace after the switch, allow a leading quote in the lookbehind and add a row for each new form.
- [x] [Review][Patch] No theory row exercises the `property:` or double-dash spellings [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2004] — the rows cover only `-p:` and `/p:`; narrowing `propertySwitch` to `(?:-|/)p:` still passes all three, so the `property:` hardening the 4.7.2 review asked for can be lost silently. Add `[InlineData("--property:", "4.7.1")]`; each row costs eight `dotnet msbuild -getProperty` evaluations.
- [x] [Review][Patch] Hotfix comment says real-tree candidates "track every advance", but only patch advances are derived [tests/eng/test_pack_release_packages.py:40] — the `v4.7` release line stays literal at line 302 and in the `current_release="v4.7"` fixtures, so a minor advance still needs manual edits. Say "every patch advance".
- [x] [Review][Patch] Checklist deferral cites a line its own commit fixed as still lagging [_bmad-output/implementation-artifacts/deferred-work.md:11084] — 188d8b73 adds "`tests/eng/test_pack_release_packages.py:299` has lagged since the 4.7.1 advance" in the commit that fixes line 299. A sweep that re-verifies the cited line can close the still-valid checklist deferral as already resolved. Reword it as past drift fixed by 188d8b73.
- [x] [Review][Patch] Release ledger never records Story 15.5's return to review [_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:1340] — the ledger's last Story 15.5 status statement (line 1323) is 188d8b73's "returns Story 15.5 to in-progress"; 6811bfd6 moves sprint-status to `review`, but its ledger and handoff entries do not record the transition, although earlier transitions were recorded there. Append it with the ledger correction above.

#### Rejected

Findings whose fix would edit one of the three specs under review are rejected under the workflow rule; where the record defect behind one is real, the line says so.

- Qualification record says "No PR or remote write was made", yet b1d89648 is on origin/main — false: the statement covers this record's own run; when the precursor started, b1d89648 was local and remote main was e7e892b6 (precursor Implementation Notes), and the later push came through user-performed merges, as in the 4.7.2 review's rejected "pushed and merged twice" row.
- This record has no superseded note or final state for Quality 41023802429, and its three deferrals are stale — rejected: the fix edits the spec under review; the deferral part is carried from round-3 BH-31-3 (true when observed; the sweep re-verifies).
- b1d89648's three deferrals sit under the 2026-10-29 section and lack severity markers — false: they sit under their own dated `### … — 2026-10-30` heading and each carries `source_spec:` naming this record; round 3's defect was a block with no heading. The flat format has no severity field (round-3 BH-31-2).
- Copied continuation text says "this repair" and points at /tmp — false: in the ledger and handoff it follows the repair's own entry, and the ledger's review-qualified paragraph points to this record's archive and checksum.
- Receipt archives use different directories, and "Verify with `sha256sum <archive>`" checks nothing — low: both paths are outside temporary storage and recorded exactly, the expected hash is on the line above, and the fix edits the precursor spec under review.
- The override is injected after the first physical `dotnet pack` line — low: the live command is one line; a multi-line reformat degrades only the self-test override rows (the live row joins correctly), and the fix restructures the injection.
- Response-file (`@args.rsp`) overrides are not collected — low: no Gate 4 step uses response files, and modelling them is more than a direct correction.
- `PUBLISHED_BASELINE_VERSION.split(".")` fails at import for a prerelease or four-part baseline — false: the constant is a published stable baseline, and a malformed value fails loudly at import.
- The hotfix version is derived from the constant under test, so the test cannot catch a wrong constant — false: the requested 4.7.3 is exactly what is computed, and a literal would not catch a wrong constant either, because the policy accepts candidates at or below the baseline (the 4.7.2 review's rejected `eng/release_compatibility.py` row).
- 188d8b73's verification omitted ordinary validation and InfrastructureGovernance, and "No hosted run has observed these patches yet" was never revisited — rejected: the fix edits the 4.7.2 spec under review; the red base was external (a55960f5), 6811bfd6 recorded the 188d8b73 lanes, and hosted CI/Quality at 6811bfd6 ran the patched tests green.
- The decbb195 replay lacks context on whether refusal is expected and on its base — low: nothing it states is false; replay base a9c66bfe is decbb195's direct parent.
- Sprint-status keeps the old "seven open patches" comment instead of replacing it — low: stacked dated comments are this file's convention (Story 15.1), and the newer comment supersedes it.
- `acquire-catalogs` passes at 188d8b73 but failed at e7e892b6 without explanation, so hosted preparation lets a drifted-seal base through — false: preparation is not admission; at 188d8b73 Quality 41215250572 still failed Gate 2b and CI 41215250922 failed dependency governance on the same seal. An explanation would edit the precursor spec under review.
- DW-1988 has no pointer to its new decision entry — false, carried from round-3 BH-31-2: the new entry names DW-1988, and the adjacent in-place correction was required by round-3 Patch 3.
- Hosted checks name connector tools instead of `gh` commands — rejected: the fix edits the spec under review; the exact REST paths are recorded and reproducible with `gh api`.
- The inline reproduction script uses bare `assert` (stripped by `-O`), a private helper, an unused import and a double write, and raises `StopIteration`/`TypeError` or writes an empty or partial report on missing data — rejected: the fix edits the spec under review; each case either fails loudly or needs input never shown reachable for this diagnostic.
- Round-3 diagnostics exist only in a machine-local archive — false: round-3 Patch 4 required a persistent archive with checksum, member manifest and exact invocations, and all four were delivered.
- This spec is `in-review` while its text says the transition is blocked — false: `in-review` is the correct state while this review is pending; the blocked transition is an external prerequisite recorded in the body.

### Review resolution — 2026-10-31

The decision resolved to option 1, and all seven patches were applied to the
working tree on HEAD e442544ca2369a43c3e5244eb8005a551a713a77:

- `deferred-work.md`: the round-3 a55960f5 entry is corrected in place as resolved
  by bf87ab62, with the validation and hosted evidence above, and the checklist
  entry now reads its line-299 lag as past drift fixed by 188d8b73.
- `rel-ai-1-release-evidence-ledger.md`: a dated entry records the reseal
  correction, e442544c's new owner-pin refusal, Story 15.5's return to review
  and the human amendment's limits.
- `sprint-status.yaml`: the false clause is removed from the round-3 reason, and
  a dated comment records the correction.
- `spec-15-5-prepare-the-inactive-policy-precursor.md`: a dated `Human amendment
  — 2026-10-31` section keeps the spec `done` and states what that does not grant.
- `CiGovernanceTests.cs`: the collector accepts `:`, `=` or whitespace after the
  switch and a fully quoted switch argument; the theory now passes a template
  per row and adds `--property:`, `-p `, `-p=` and quoted `"-p:…"` rows.
- `test_pack_release_packages.py`: the comment says "every patch advance".

Local verification:

- `python3 -m unittest tests/eng/test_pack_release_packages.py`: 47 passed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --nologo -nodeReuse:false --disable-build-servers -p:NuGetAudit=false`:
  0 warnings, 0 errors.
- Shell.Tests `-method …CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary`:
  7 passed. Mutation checks: the previous collector regex fails exactly the
  `-p `, `-p=` and quoted rows; narrowing the switch to `(?:-|/)p` fails exactly
  the `--property:` row. The file was restored byte-identically and rebuilt.
- Shell.Tests `-class …CiGovernanceTests`: 93 passed.
- `python3 eng/validate-story-artifacts.py`: exit 0. `git diff --check`: exit 0.
  All seven edited files remain CRLF-only.

No hosted run has observed these patches yet.

By human decision on 2026-10-31, this spec moves to `done`. That supersedes the
2026-10-30 statement that it is not marked done while its governance transition
and PR are blocked. `done` closes only the reviewed record work. The blocked
transition remains an external prerequisite, Story 15.5 and REL-BASE-1 stay at
review, and no publication, owner or independent approval is granted.


## Code review round 2 — 2026-11-01

Follow-up review of the fix delta c46c1fb540604f76ce6754437ba68c894007ca8f,
which applied the [2026-10-31 code review](#code-review--2026-10-31) patches. The
diff is `git diff e442544c c46c1fb5` (34.402 kB; Blind Hunter floor
min(floor(sqrt(34.402) + 1), 10) = 6). e442544ca2369a43c3e5244eb8005a551a713a77
(`fix: submodules`) is outside Story 15.5 and is excluded. Four independent
reviewers ran at the parent model capability: Blind Hunter, Edge Case Hunter,
Verification Gap and Acceptance Auditor. Every finding was verified against HEAD
c46c1fb5, Git history, hosted runs and local probes before triage.

### Review Findings

- [x] [Review][Decision] Nothing tracks what closes Story 15.5 now that all seven of its specs are `done` — c46c1fb5 moves this spec, the last open Story 15.5 spec, to `done`. Story 15.5 and REL-BASE-1 stay at `review` only through a sprint-status comment ("stays at review while precursor owner acceptance remains open"). The deferral that holds owner acceptance says it "has no tracker" (deferred-work.md:11095), and no artifact names an owner or closing condition. A spec-to-story status sync, including this workflow's own status step, would set the story to `done` while owner acceptance is still open. Options: (1) keep Story 15.5 at `review` and add an open tracker for precursor owner acceptance, for example an `approved_change_backlog` approval item next to REL-BASE-1 that names the owner and the closing condition; (2) close Story 15.5 and REL-BASE-1 as `done` and carry owner acceptance only as the existing deferral, outside the story; (3) open a new non-done Story 15.5 spec (for example `blocked`) that owns owner acceptance. Resolved 2026-11-01: human decision, option 2. Story 15.5 and REL-BASE-1 close as `done`; precursor owner acceptance is carried only by the existing deferral (deferred-work.md:11095), outside the story (patch below).
- [x] [Review][Patch] Close Story 15.5 and REL-BASE-1 as `done` [_bmad-output/implementation-artifacts/sprint-status.yaml:238] — apply the option-2 decision: set REL-BASE-1 in `approved_change_backlog` to `done` with its closing date and a reference to this decision, and add a dated sprint-status comment saying owner acceptance stays open in the deferred ledger, outside the story. The development-status entry moves to `done` in the review's status step. Closing the story grants no publication, activation, owner, Fluent, Product, EventStore or GOV-1 approval, and it does not accept the f75d774d281aa6d4764d9576597ff98ecfbaa3a7 policy.
- [x] [Review][Patch] Theory rows do not pin every spelling the collector accepts [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2010] — c46c1fb5 adds a single-quoted branch `'{propertySwitch}[^']*'` and `Trim('\'')`, but no row passes a single-quoted argument. A probe that deletes the branch keeps all seven rows green. Bash still applies a single-quoted later override, so it would reach ApiCompat while the live row evaluates 4.7.2. The 2026-10-31 probe also named `--property Foo=X`, which has no row. Add `[InlineData("'-p:{property}'", "4.7.1")]` and `[InlineData("--property {property}", "4.7.1")]`; each row costs about 3 s (the seven rows take 21.2 s).
- [x] [Review][Patch] Collector comment still claims every spelling `dotnet pack` applies [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2085] — a probe of the exact expression shows that `"-p" FrontComposerPackageValidationBaselineVersion=4.7.1` is not collected, although bash gives it the same argv as `-p Foo=X`. `"-p:Foo=4.7.2"-rc` is collected as `4.7.2`, and `"-p Foo=X"` is collected although pack rejects it (MSB1001). c46c1fb5's subject and body repeat the claim. Reword the comment to name the forms it models, and state that a switch quoted apart from its value, quoted text joined to an unquoted suffix and response files are not modelled.
- [x] [Review][Patch] Hotfix comment still overstates what tracks an advance [tests/eng/test_pack_release_packages.py:40] — `test_real_script_packs_the_checked_in_release_line_under_release_policy` packs the literal `"4.7.2"` (line 119). The at-or-below policy keeps it green after the next advance, so it lags silently, as line 299 did. A major advance also moves the `v4.7` literals, and the checklist deferral's "about 16" sites (deferred-work.md:11084) omit those release-line literals. Derive line 119 from `PUBLISHED_BASELINE_VERSION`, say "a minor or major advance", and add the release-line literals to the deferral.
- [x] [Review][Patch] Release ledger never records this spec's move to `done` [_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md:1426] — c46c1fb5's body says it records "the qualification spec's move to done", but its ledger entry records only the precursor amendment. The move appears only in this spec and a sprint-status comment. Append a dated ledger line that records it and its limits.
- [x] [Review][Patch] Resolved a55960f5 deferral still parses as an open high item [_bmad-output/implementation-artifacts/deferred-work.md:11089] — the entry now says "Resolved before this entry landed … filed as high" but keeps an open flat summary with no closure marker. Use the ledger's closure form (deferred-work.md:9760-9762): prefix the summary with `RESOLVED 2026-10-31 —` and add `status: done 2026-10-31`, so a sweep or `--migrate` does not import it as an open high deferral.
- [x] [Review][Defer] Main is red again on the Platform owner pin, and no deferral tracks it [_bmad-output/implementation-artifacts/deferred-work.md:11090] — deferred: pre-existing, caused by e442544c outside Story 15.5. `python3 eng/dependency_graph.py --root . validate --commit <sha>` exits 1 at e442544c and c46c1fb5 with `github.com/hexalith/hexalith.platform@03a099bba258c37a8f16c8b2e842ae842b1ad6d6: consumer source projection owner pin changed`. CI 41246730936 for e442544c failed, and c46c1fb5's CI 41250039661 and Quality 41250039403 inherit it. The corrected entry says only "this entry does not track that".
- [x] [Review][Defer] Activation handoff still says the a55960f5 drift is deferred and not repaired [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md:542] — deferred: the fix edits another spec. The 2026-10-31 release-ledger patch named the missing "ledger and handoff entries", but c46c1fb5 corrected only the deferred ledger, the release ledger and sprint-status. The handoff also never records Story 15.5's return to review, and this spec's intent requires appending to the handoff.
- [x] [Review][Defer] Precursor amendment does not say it leaves the successor amendment unmet [_bmad-output/implementation-artifacts/spec-15-5-prepare-the-inactive-policy-precursor.md:731] — deferred: the fix edits another spec. The 2026-10-30 section sets two conditions: the spec "must not be promoted to done", and "Completing the successor requires a human amendment permitting renewed current-tuple qualification and the necessary Parties content/dependency-selection repair". The `Human amendment — 2026-10-31` section quotes both and "supersedes the 2026-10-30 condition" (singular). Its "does not" list omits renewed qualification and the Parties repair, so it can be read as that successor amendment. The human decision (option 1) granted only the bounded record closure.
- [x] [Review][Defer] 4.7.2 record still describes 188d8b73's collector as current [_bmad-output/implementation-artifacts/spec-15-5-advance-the-published-baseline-to-4-7-2.md:504] — deferred: the fix edits another spec. Its resolution says the collector "accepts every `-`/`--`/`/`" spelling, and that record owns `CiGovernanceTests.cs` in its File List, but nothing points it to c46c1fb5's change.

#### Rejected

- Blind Hunter: c46c1fb5's local verification omits ordinary validation and InfrastructureGovernance — low: the fix edits this spec, and the red base it would show (e442544c) is already recorded in the resolution and deferred above.
- Blind Hunter: seven rows of eight `dotnet msbuild` evaluations make the theory costly, so extract a string-only collector — low: all seven rows take 21.2 s, and the refactor is more than a direct correction.
- Blind Hunter and Acceptance Auditor: the rejection rule contradicts the accepted patches — false: the precursor amendment was the human's option-1 decision recorded in the Decision row, and appending a review record to this spec is the workflow's own output, not a finding fix. The stale Quality 41023802429 `in_progress` state the auditor cites is real but is carried from the 2026-10-31 review, because its fix edits this spec.
- Blind Hunter: rejected findings that are real defects are not tracked anywhere — false: each one is recorded with its reason in the 2026-10-31 Rejected list, and leaving them out of the deferred ledger is the workflow rule.
- Blind Hunter and Acceptance Auditor: this spec moved to `done` without a Decision row or amendment section — low: the fix edits this spec, and the human decision is recorded and dated at the end of the 2026-10-31 resolution.
- Blind Hunter and Acceptance Auditor: the File List and Footprint were not updated, and focused strict validation fails — medium, rejected because the fix edits this spec. `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-qualify-the-governance-transition.md --base e442544c` exits 1 (four changed paths unlisted, two listed paths untouched), so the line-383 exit-0 claim does not cover the review delivery. The global validator exits 0.
- Blind Hunter and Acceptance Auditor: the sprint-status round-3 comment and the deferral were corrected in place instead of stacked — false: the 2026-10-31 "Round-3 records" patch prescribed both in-place corrections. The stacked convention covers superseded but true comments, the removed clause was false, and the new dated comment and the deferral's "corrected 2026-10-31 by code review" say what changed.
- Edge Case Hunter: a single-quoted value (`-p:Foo='4.7.1'`, `-p 'Foo=4.7.1'`) is mis-collected — false: the probe yields `'4.7.1` or an invalid `'Foo` property, so the live row fails loudly on the baseline assertion or the msbuild exit code. It never passes silently.
- Edge Case Hunter: a quoted switch followed by an unquoted suffix is cut at the closing quote — low: it passes silently only when the quoted prefix equals the published baseline, no Gate 4 line does this, and modelling bash concatenation is more than a direct correction. The comment patch names it as unmodelled.
- Acceptance Auditor: the verification record elides the Shell.Tests commands and records no commitlint run — low: the fix edits this spec, and the message passes local commitlint and hosted Commitlint 41250039616.

### Review resolution — 2026-11-01

The decision resolved to option 2, and all six patches were applied to the
working tree on HEAD c46c1fb540604f76ce6754437ba68c894007ca8f:

- `CiGovernanceTests.cs`: the theory adds `'-p:{property}'` and
  `--property {property}` rows. The collector comment now lists the modelled
  spellings and names the unmodelled ones.
- `test_pack_release_packages.py`: the real-tree release-line test packs
  `PUBLISHED_BASELINE_VERSION` instead of `"4.7.2"`. The comment says that a minor
  or major advance still moves the `v4.7` literals.
- `deferred-work.md`: the checklist entry names the eleven release-line literals,
  and the a55960f5 entry carries `RESOLVED 2026-10-31 —` and
  `status: done 2026-10-31`. Four round-2 deferrals are appended.
- `rel-ai-1-release-evidence-ledger.md`: a dated entry records this spec's move to
  `done`, the Story 15.5 and REL-BASE-1 closure and its limits, and the open
  owner-pin refusal.
- `sprint-status.yaml`: REL-BASE-1 is `done` (closed 2026-11-01) with a reference
  to this review, and a dated comment records the decision.

Local verification:

- `python3 -m unittest tests/eng/test_pack_release_packages.py`: 47 passed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --nologo -nodeReuse:false --disable-build-servers -p:NuGetAudit=false`:
  0 warnings, 0 errors.
- `tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -method "Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary"`:
  9 passed (20.2 s).
- Mutation checks, each rebuilt and then run with the same `-method`: deleting the
  `'{propertySwitch}[^']*'` alternative fails exactly the `'-p:{property}'` row,
  and limiting whitespace to the short `p` form fails exactly the
  `--property {property}` row. The file was restored byte-identically (SHA-256
  compared), rebuilt and re-run green.
- The same executable with `-class "Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests"`:
  95 passed.
- `python3 eng/validate-story-artifacts.py`: exit 0. `git diff --check`: exit 0.
  All six edited files remain CRLF-only.

`python3 eng/dependency_graph.py --root . validate --commit c46c1fb540604f76ce6754437ba68c894007ca8f`
still exits 1 on the Platform 03a099bba258c37a8f16c8b2e842ae842b1ad6d6 owner pin
(deferred above). No hosted run has observed these patches yet.

By human decision on 2026-11-01, Story 15.5 and REL-BASE-1 close as `done`.
Precursor owner acceptance stays open in the deferred ledger, outside the story.
No publication, activation, owner or independent approval is granted.


## Code review round 3 — 2026-11-01

Follow-up review of the fix delta 589d63aa1a91c9c602f4caa15deef48bcf035b0c,
which applied the [round-2 code review](#code-review-round-2--2026-11-01) patches
and closed Story 15.5. The diff is `git diff c46c1fb5 589d63aa` (34.321 kB; Blind
Hunter floor min(floor(sqrt(34.321) + 1), 10) = 6). The later commits 5c8a89a0,
44453a45 and 1e1f3fde (gitlink syncs) and c092e8f9 (`fix(deps)`) are outside
Story 15.5 and are excluded. Four independent reviewers ran at the parent model
capability: Blind Hunter, Edge Case Hunter, Verification Gap and Acceptance
Auditor. Every finding was verified against HEAD c092e8f9, whose reviewed paths
are unchanged since 589d63aa, plus Git history, hosted runs and local probes
before triage.

### Review Findings

- [x] [Review][Decision] Epic 15 stays `in-progress` although all seven of its stories are `done` — 589d63aa closed Story 15.5, the last open Epic 15 story: 15-1 through 15-7 are `done`, and `epics.md` lists no Story 15.8. The status file's rule is "in-progress → done: Manually when all stories reach 'done' status" (sprint-status.yaml:17), and `epic-15: in-progress` (line 224) has no dated comment explaining the hold. The file has precedents for both choices: epic-14 is `done` while its retrospective stays `optional`, and epic-12 moved to `done` after its retrospective was accepted. Options: (1) set `epic-15: done` now with a dated comment, leaving `epic-15-retrospective: optional`; (2) keep `epic-15: in-progress` until the retrospective runs, with a dated comment saying so. Resolved 2026-11-01: human decision, option 2. Epic 15 stays `in-progress` until its retrospective runs; a dated sprint-status comment records the hold (patch below).
- [x] [Review][Patch] Record the Epic 15 hold [_bmad-output/implementation-artifacts/sprint-status.yaml:224] — apply the option-2 decision: above `epic-15: in-progress`, add a dated comment saying that all seven Epic 15 stories are `done` and that, by human decision, the epic stays `in-progress` until `epic-15-retrospective` runs. No story status changes.
- [x] [Review][Patch] Theory rows still do not pin the case-insensitive switch match [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2012] — `propertySwitch` matches `(?i:p|property)` (line 2109), but all nine rows are lowercase, so narrowing it to `(?:p|property)` keeps every row green. Two independent probes on SDK 10.0.403 show that `dotnet pack` applies `-P:Foo=X`, so an uppercase later override would reach ApiCompat while the live row still evaluates 4.7.2. 589d63aa's subject says "pin every Gate 4 collector branch". Add `[InlineData("-P:{property}", "4.7.1")]`. The tab delimiter and the embedded-quote value segment also have no row. They stay unpinned: no Gate 4 line uses a tab, and a mis-collected quoted value evaluates `PackageValidationBaselineVersion` as empty (probed), which fails the live row loudly.
- [x] [Review][Patch] Collector comment omits case-insensitivity and lists a quoted whitespace form that pack rejects [tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs:2088] — it says the collector accepts "the property-switch spellings `dotnet pack` applies" and names "`p` or `property`" without case. The quoted branch also accepts whitespace, so it collects `"-p Name=Value"`, which pack rejects (MSB1001, round 2); that form fails loudly. Say "`p` or `property` in any case", and state that a quoted switch with whitespace before its value is collected although pack rejects it.
- [x] [Review][Patch] Release-line checklist omits the ledger's own `currentRelease` sites [_bmad-output/implementation-artifacts/deferred-work.md:11084] — the amended entry names only the eleven `v4.7` literals in `tests/eng/test_pack_release_packages.py`. A minor or major advance also moves `docs/diagnostics/compatibility-suppressions.json:3` (`"currentRelease": "v4.7"`), `tests/Hexalith.FrontComposer.SourceTools.Tests/Diagnostics/DiagnosticRegistryTests.cs:701` (`ShouldBe("v4.7")`) and the diagnostics README's "active `v4.7` ledger" (`docs/diagnostics/README.md:7`). Add them to the entry's added sentence.
- [x] [Review][Patch] Hotfix comment does not say that a ledger release-line advance also breaks the derived real-tree candidates [tests/eng/test_pack_release_packages.py:40] — the candidates derive from the baseline only. When `currentRelease` opens the next line while the baseline stays one line behind, which the policy permits, `run_plan(PUBLISHED_BASELINE_VERSION, release_policy=True)` (line 120) and `validate_release_policy(ROOT, HOTFIX_VERSION)` (line 304) fail with "does not match currentRelease" (`eng/release_compatibility.py:401-404`), even after the `v4.7` literals move. Say so in the comment.
- [x] [Review][Patch] Sprint-status header date was not advanced [_bmad-output/implementation-artifacts/sprint-status.yaml:2] — 589d63aa moved `last_updated:` to `11-01-2026 07:46` but left `# last_updated: 2026-10-31`. The eight previous status commits kept the two in step. Set the header to `# last_updated: 2026-11-01`.
- [x] [Review][Patch] An open deferral still names unblocking Story 15.5 as its purpose [_bmad-output/implementation-artifacts/deferred-work.md:11071] — the resumed-review entry asks maintainers to finish its work "before an authorized successor can unblock Story 15.5", and 589d63aa closed that story. A sweep can read the entry as moot and close work that is still open. Append a dated note under this review's deferred heading: Story 15.5 closed on 2026-11-01 by human decision, the line-11071 entry and the owner-acceptance entry (line 11096) stay open outside the story, and only line 11071's "unblock Story 15.5" purpose is obsolete.
- [x] [Review][Patch] The 4.7.2 forward-pointer deferral misses 589d63aa [_bmad-output/implementation-artifacts/deferred-work.md:11113] — it names only c46c1fb5, but 589d63aa changed the same theory and collector comment again. Name both commits.
- [x] [Review][Defer] No hosted run has executed the c46c1fb5 or 589d63aa test changes, or Gate 4, since e442544c [.github/workflows/quality.yml:162] — deferred: pre-existing, caused by the Platform owner-pin refusal already deferred as high. Quality fails "Prepare exact governance catalog objects" and skips every Gate 2b–4a step: 41250039403 at c46c1fb5 (failure), 41282260584 at 589d63aa (that step failed, run cancelled) and 41289068705 at HEAD c092e8f9 (failure). Push CI does not run Shell.Tests. The last executed Gate 4 is Quality 41229072509 at 6811bfd6 (success). Neither the high deferral nor the release-ledger closure entry says that these gates are skipped. Ordinary validation at HEAD now refuses Platform 55592ec3a57e26d11928971d5e04d9e50235a959, moved by 1e1f3fde, instead of 03a099bb.
- [x] [Review][Defer] The activation handoff and the repair record neither Story 15.5's return to review nor its closure [_bmad-output/implementation-artifacts/spec-15-5-reconcile-the-activation-handoff.md:508] — deferred: the fix edits other specs. This spec's frozen intent requires appending to "the repair, activation handoff, release ledger and deferred ledger". The handoff's last section is the 2026-10-31 precursor round-3 resolution, and the repair's last section is dated 2026-10-30; neither has a 2026-11-01 entry. The round-2 deferral (deferred-work.md:11107) covers only the handoff and only the return to review.

#### Rejected

- Blind Hunter, Edge Case Hunter and Acceptance Auditor: the File List and Footprint omit this delivery's paths, and focused strict attribution now fails for good — medium, rejected because the fix edits this spec (carried from round 2). `python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-15-5-qualify-the-governance-transition.md --base c46c1fb5 --candidate 589d63aa` exits 1: 589d63aa is `interleaved` with three unowned paths (`sprint-status.yaml`, `CiGovernanceTests.cs`, `test_pack_release_packages.py`). With Story 15.5 `done`, no later delivery will reconcile it.
- Blind Hunter and Acceptance Auditor: the verification record is inconsistent (nine rows in 20.2 s against seven in 21.2 s at "about 3 s" each), records the mutations only in prose and has no commitlint evidence — rejected: the fix edits this spec. The timings are run-to-run variance, and the pinned commitlint CLI accepts 589d63aa's message with 0 problems and 0 warnings.
- Acceptance Auditor: the ledger says "the same human decision of 2026-10-31" moved this spec to `done`, but the 2026-10-31 Decision row offered only precursor options — maybe-false, low: the 2026-10-31 sprint-status comment records both moves in one human decision ("the precursor stays done (amendment) and the qualification spec is done"). Settling it needs the 2026-10-31 decision itself.
- Blind Hunter and Acceptance Auditor: REL-BASE-1 closes on the superseded 4.7.1 receipt — false: `reconciled` and `evidence` are the dated reconciliation receipt, the same shape as UX-A through UX-F. The `done` receipt is the new `ref:` to the round-2 review, which leads to the 4.7.2 record.
- Verification Gap: the high owner-pin deferral names 03a099bb, but main now refuses on 55592ec3 — false as a defect of this delta: validate still prints 03a099bb at 589d63aa, and the deferral asks for a renewed "Platform owner pin", not one SHA. 55592ec3 came from 1e1f3fde after this delta; the hosted-gates deferral above records it.
- Blind Hunter: the owner-acceptance deferral (deferred-work.md:11096) still says it "has no tracker" — false: the round-2 Decision named exactly that, and option 2 chose to carry owner acceptance by that entry alone.
- Acceptance Auditor: deferred-work.md:11026 still targets the closed story — false: it names no Story 15.5 target, and "the current request's passing-local-gates PR prerequisite remains unmet" is still true.
- Edge Case Hunter: derive the real-tree candidates from `currentRelease` so that a ledger line advance keeps them green — low: the failure is loud, it occurs only at a ledger line advance, and the derivation is more than a direct correction. The comment patch above names the case.

### Review resolution — 2026-11-01

The decision resolved to option 2, and all eight patches were applied to the
working tree on HEAD c092e8f91ebb670fad4ab959f2d3c8e13d809e0f, whose reviewed
paths are unchanged since 589d63aa:

- `sprint-status.yaml`: a dated comment above `epic-15: in-progress` records the
  hold until `epic-15-retrospective` runs. The header reads
  `# last_updated: 2026-11-01`, and `last_updated:` reads `11-01-2026 15:17`. No
  story status changes.
- `CiGovernanceTests.cs`: the theory adds a `-P:{property}` row. The collector
  comment says that `p` or `property` matches in any case, and that a quoted switch
  with whitespace before its value is collected although pack rejects it.
- `test_pack_release_packages.py`: the comment says that the real-tree candidates
  do not follow the ledger, so a `currentRelease` line advance ahead of the
  baseline fails the real-tree release-policy plan test and the hotfix-candidate
  test.
- `deferred-work.md`: the checklist entry names the three ledger release-line
  sites, and the 4.7.2 forward-pointer entry names 589d63aa as well as c46c1fb5. A
  dated note under this review's deferred heading says that only line 11071's
  "unblock Story 15.5" purpose is obsolete, and that it and the owner-acceptance
  entry stay open outside the story.

Local verification:

- `python3 -m unittest tests/eng/test_pack_release_packages.py`: 47 passed.
- `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -c Release --nologo -nodeReuse:false --disable-build-servers -p:NuGetAudit=false`:
  0 warnings, 0 errors.
- `tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -method "Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary"`:
  10 passed (31.0 s).
- Mutation check, rebuilt and run with the same `-method`: replacing
  `(?i:p|property)` with `(?:p|property)` fails exactly the `-P:{property}` row (9
  passed, 1 failed). The line was restored, `git diff` shows only the patch lines,
  and the file was rebuilt.
- The same executable with `-class "Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests"`
  on the restored file: 96 passed.
- `python3 eng/validate-story-artifacts.py`: exit 0. The focused `--story` form
  still exits 1 on the File List gap rejected above (carried). `git diff --check`:
  exit 0. All five edited files remain CRLF-only, and `sprint-status.yaml` parses.

`python3 eng/dependency_graph.py --root . validate --commit` still exits 1 at
c092e8f9 on the Platform 55592ec3a57e26d11928971d5e04d9e50235a959 owner pin
(deferred above). No hosted run has observed these patches yet.

By human decision on 2026-11-01, Epic 15 stays `in-progress` until its
retrospective runs, and Story 15.5 stays `done`. No publication, activation, owner
or independent approval is granted.

## Hosted governance qualification — 2026-11-01 (E15R-AI-1)

Epic 15 retrospective action E15R-AI-1 closes the two deferrals that kept this
record's hosted gates unexecuted. The seal transitions were human-authorized. No
policy rule, guard, selector or workflow changed.

Seal transitions:

- fdb61860e6eea6ce8f8bfb0bc3d6235406f7f222 resealed Platform
  55592ec3a57e26d11928971d5e04d9e50235a959. That commit pinned Builds
  74c1bc5ca68bc9856a5fa7d06323f60a0b48b604 and EventStore
  9a962a1be8cdc0df814a63254eed0fca77328e54, two local sync commits that never
  reached GitHub. CI
  [41328585991](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41328585991)
  at dbc846fa603fc861e312e00499b3a267b73fd524 passed build-and-test but failed
  dependency-governance with `failed to acquire exact approved commit 74c1bc5c…:
  not our ref`.
- 1a4761d831bf51edb8c55ca7eba1fa66932f96f0 moved all nine root submodules to the
  head of their main branches and resealed the Platform row: owner
  4c43913231b54214b40574d02ce5ed5ed30c470d, Builds
  17616d439b36957a7c5da078e293e598bb339790, and the inventory and root-selector
  digests. The `.gitmodules` digest, canonical sources, inventory-only identities
  and consumer configuration are unchanged. Every nested pin of the selected
  commits is on its own repository's main branch.
- 4436dd53643e3b9a8de121f231303744a51cb0f1 advanced EventStore to
  69c0276a21bf0a9204593d69ea7f2f9e058aa109 and Tenants to
  01803dc080d8c397450acc5dc7da3cc094d5ef00, and
  78ba5c24cdad39bb3cf7b6b7926dadfaebd5670b renewed `root_selectors_sha256`.
- Each later push cancelled the runs of the one before it (cancel-in-progress):
  the seal pushes and the docs commit edb541bcd143c927964d068dc7361662489ac40e.

Hosted proof on the non-seal descendant 0c9012a61a53519db160c7127a0c677c7e03b772
(event base 78ba5c24):

- CI
  [41330569875](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41330569875):
  success. dependency-governance passed "Collect exact graph diff and
  affected-module proof", "Create exact-source release proof", "Create AD-13
  dependency-release-handoff" and "Enforce dependency-governance result".
- Quality
  [41330569491](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/41330569491):
  build-and-test success. Every step from Gate 1 to Gate 4a executed and
  succeeded, including "Prepare exact governance catalog objects", every Gate 2b
  step ("Infrastructure governance and telemetry contracts", "Release suppression
  lifecycle tests" and the rest), Gate 2c, Gates 3a–3d, Gate 4 "Library Package
  Validation (ApiCompat baseline)" and Gate 4a "Adopter proof kit smoke". The
  `test-results` artifact records all 10
  `CiGovernanceTests.QualityWorkflow_Gate4ValidatesEveryPackableLibrary` rows as
  Passed: the null row, the c46c1fb5 and 589d63aa spellings, and the bd189a4e
  `-P:{property}` row.
- The Quality run's conclusion is still failure, because the separate
  `epic9-live-acceptance` job failed "AppHost rejects invalid bearer tokens":
  every advertised EventStore endpoint timed out. That job passed at 589d63aa
  (Quality 41282260584) and has failed since 1e1f3fde (Quality 41283830089),
  which is before both reseals. It is deferred in `deferred-work.md`.

Local verification before the pushes:

- `python3 eng/dependency_graph.py --root . acquire-catalogs --event-base <push base> --candidate <sha>`
  and `validate --commit <sha>`: exit 0 for edb541bc over 1a4761d8, and for
  0c9012a6 over 78ba5c24. With base dbc846fa, 1a4761d8 is refused with
  `consumer source projection owner pin changed`, as designed.
- The CI pair `acquire` then `diff --event push` for edb541bc over 1a4761d8: ok,
  0 changes.
- `python3 -m pytest tests/eng/test_dependency_graph.py`: 143 passed.
- `dotnet restore Hexalith.FrontComposer.slnx -p:Configuration=Release`: success.

E15R-AI-1 stays `open` in sprint-status until the human decision. No
publication, activation, owner or independent approval is granted.
