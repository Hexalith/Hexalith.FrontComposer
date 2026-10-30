---
title: 'Story 15.5: Qualify the Governance Transition'
type: 'chore'
created: '2026-10-30'
status: 'in-review'
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
