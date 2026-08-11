---
title: 'Bump EventStore Catalog Policy to 3.92.0'
type: 'refactor'
created: '2026-08-11'
status: 'done'
baseline_commit: '680c789e97bad67558327e35b4ab7c90e4126abe'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer's pushed EventStore gitlink is exact tag `v3.92.0`, and its pushed Hexalith.Builds catalog selects EventStore `3.92.0`, but `eng/dependency-graph-policy.json` still requires `3.91.1`. The canonical dependency validator therefore rejects the repository's selected catalog.

**Approach:** Synchronize the single FrontComposer-owned governed catalog expectation to `3.92.0`, then prove the dependency graph and both Debug/source and Release/package AppHost modes use the already-pushed identities without changing dependency wiring.

## Boundaries & Constraints

**Always:** Change only the stale EventStore policy value needed for this bump; preserve the policy schema and formatting; treat EventStore commit `52200827070f1588e313e843cd80320b0a4f6fd2` and Builds commit `b4e361672293f6462160e4ee666d24bd49befec8` as read-only pushed inputs; validate the actual Release package path as well as Debug source compilation.

**Ask First:** Any need to change a submodule pointer or submodule content, dependency-mode wiring, package/project references, pacts, provider verification, or Story 11.24 artifacts; any resolution other than EventStore `3.92.0`.

**Never:** Initialize nested submodules, edit files under `references/Hexalith.*`, weaken or remove semantic policy checks, reformat the policy file wholesale, or represent this mechanical catalog sync as completing the broader owner-approved runtime adoption in Story 11.24.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Aligned pushed selection | Builds selects `3.92.0`; policy expects `3.92.0` | Canonical graph validation accepts all governed selectors | Fail if any other semantic catalog requirement drifts |
| Unexpected catalog value | Selected EventStore version differs from `3.92.0` | Validation remains fail-closed | Report the exact expected/found values; do not relax policy |
| Release package unavailable | NuGet cannot restore `Hexalith.EventStore.Aspire` `3.92.0` | Release-mode validation does not pass | Report the exact restore blocker; do not substitute source mode |

</frozen-after-approval>

## Code Map

- `eng/dependency-graph-policy.json:54-66` -- `frontcomposer-catalog-v1.selected_catalog_required_properties`; line 61 is the only stale FrontComposer value.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52` -- read-only selected catalog evidence: EventStore `3.92.0` feeds all 13 package rows.
- `Directory.Packages.props:3-13` -- imports the root Builds catalog; no local version override belongs here.
- `Directory.Build.props:12-26`, `deps.local.props:2-16`, `deps.nuget.props:2-9` -- read-only Debug/source versus Release/package mode selection.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:11-19` -- conditional EventStore.Aspire project/package consumer; the solution excludes this AppHost from Release.
- `eng/dependency_graph.py:1021-1057,1211-1324` -- exact selected-property and semantic-profile validation reused unchanged.
- `tests/eng/test_dependency_graph.py:1488-1498` -- focused real-catalog policy test; no hard-coded test version change is needed.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:35-51,408-452` -- C# Governance consumer of the canonical Python validator.
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md` -- read-only backlog boundary; this bump does not satisfy its approval, hash-inventory, Pact, or provider-verification gates.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` -- change only `HexalithEventStoreVersion` from `3.91.1` to `3.92.0` so policy matches the selected Builds catalog.
- [x] Validation surface -- prove the graph, exact source gitlink/tag, deterministic dependency modes, exact restored EventStore package version, absence of an EventStore project-reference fallback, fail-closed unavailable-package behavior, and unchanged Story 11.24 artifacts.

**Acceptance Criteria:**
- Given the pushed Builds catalog selects EventStore `3.92.0`, when canonical graph validation runs, then it succeeds without weakening any other profile requirement.
- Given the root EventStore gitlink is exact tag `v3.92.0`, when the AppHost builds in Debug/source mode, then it consumes that root checkout successfully.
- Given Release/package mode, when the AppHost restores and builds, then `Hexalith.EventStore.Aspire` `3.92.0` resolves without an EventStore project-reference fallback.
- Given Story 11.24 remains independently blocked, when this focused bump completes, then its spec and sprint status remain unchanged.

## Spec Change Log

- Iteration 1 -- Verification-gap review found that restore/build success alone could consume a stale `Hexalith.EventStore.Aspire` package row or an EventStore project reference. Verification now evaluates the Release project, inspects restored assets for exact `3.92.0`, forces both dependency-mode switches, checks source identities, exercises unavailable-package failure, and confirms Story 11.24 remains unchanged. This avoids a false-positive build while keeping the one-line policy edit, canonical graph validation, Debug/source build, Release/package restore, and Story 11.24 boundary intact.

## Design Notes

The already-pushed gitlinks performed the source and catalog selection. FrontComposer's policy is an executable semantic contract, so updating its expected value is required alignment rather than a version override. Package availability and broader runtime-identity authorization are distinct: Release restore proves package consumption, while Story 11.24 continues to own approval and provider evidence. The Release AppHost compile may set `BuildProjectReferences=false` after a successful full restore to isolate its package edge from unrelated downstream UI compilation; evaluated items and restored assets remain the authority for the EventStore dependency mode and version.

## Verification

**Commands:**
- `python3 -m unittest tests.eng.test_dependency_graph.PolicyShapeTests.test_all_governed_selected_catalog_properties_match_and_mutations_fail -v` -- expected: the focused real-catalog policy test passes.
- `python3 eng/dependency_graph.py --root . validate --commit "$(git rev-parse HEAD)"` -- expected: `ok: true` with every selector validated.
- `git ls-tree HEAD references/Hexalith.EventStore references/Hexalith.Builds`, `git -C references/Hexalith.EventStore rev-parse HEAD`, `git -C references/Hexalith.EventStore describe --tags --exact-match HEAD`, and `git -C references/Hexalith.Builds rev-parse HEAD` -- expected: checkouts equal the approved spec inputs and EventStore is exact tag `v3.92.0`.
- `dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Debug -p:UseNuGetDeps=false -p:UseHexalithProjectReferences=true` then `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -p:UseNuGetDeps=false -p:UseHexalithProjectReferences=true` -- expected: source-mode AppHost compiles from the root EventStore checkout.
- `eventstore_packages="$(mktemp -d)"`; restore the AppHost in Release mode with `--packages "$eventstore_packages" --no-cache --force -p:Configuration=Release -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false`, then build it with `--configuration Release --no-restore -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false -p:BuildProjectReferences=false -p:RestorePackagesPath="$eventstore_packages"` -- expected: the isolated package-mode AppHost compiles with zero warnings and errors.
- `dotnet msbuild src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -property:Configuration=Release -property:UseNuGetDeps=true -property:UseHexalithProjectReferences=false -getProperty:HexalithEventStoreVersion -getProperty:HexalithEventStoreFromSource -getItem:PackageReference -getItem:ProjectReference` -- expected: EventStore version `3.92.0`, source mode `false`, an EventStore.Aspire package reference, and no EventStore project reference.
- `rg -n 'Hexalith.EventStore.Aspire/3\\.92\\.0' src/Hexalith.FrontComposer.AppHost/obj/project.assets.json` -- expected: the restored assets contain the exact `3.92.0` package.
- Restore Release/package mode once with `-p:HexalithEventStoreVersion=0.0.0-matrix-unavailable` -- expected: nonzero `NU1603`/`NU1101` naming `Hexalith.EventStore.Aspire` and no source fallback; rerun the valid Release restore afterward.
- `git diff --exit-code 680c789e97bad67558327e35b4ab7c90e4126abe -- _bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md _bmad-output/implementation-artifacts/sprint-status.yaml references/Hexalith.Builds references/Hexalith.EventStore` -- expected: no changes.
- `git diff --check` and `git status --short` -- expected: no whitespace errors; only the policy file and this lifecycle spec are changed.

## Suggested Review Order

**Catalog alignment**

- Match FrontComposer's semantic contract to the already-selected Builds catalog.
  [`dependency-graph-policy.json:61`](../../eng/dependency-graph-policy.json#L61)

**Verification boundary**

- Lock source and package acceptance while leaving Story 11.24 independently blocked.
  [`spec-bump-eventstore-to-3-92-0.md:56`](spec-bump-eventstore-to-3-92-0.md#L56)

- Prove exact package resolution and reject project-reference fallback.
  [`spec-bump-eventstore-to-3-92-0.md:70`](spec-bump-eventstore-to-3-92-0.md#L70)
