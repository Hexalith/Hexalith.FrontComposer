---
title: 'Bump EventStore Package Family to 3.97.0'
type: 'refactor'
created: '2026-08-22'
status: 'done'
baseline_commit: '36efb0c3f774744cd9556f256c0b47f9b0b6bcad'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer's root EventStore checkout is already exact tag `v3.97.0`, but the recorded Builds gitlink still selects EventStore packages `3.96.2`. Builds `main` now contains pushed catalog-only commit `761dc0187ef60599f12310fef2411dbaf0206742`, which selects `3.97.0` while its deterministic package audit remains stale and fails 13 family checks.

**Approach:** Treat this request and checkpoint approval as a one-time waiver solely for aligning the EventStore package family to `3.97.0`. Reconcile the Builds audit, repair the catalog's missing required UTF-8 BOM without changing its XML content, and commit those two files locally; then leave the FrontComposer gitlink ready to select that commit. Do not push, commit the parent repository, or claim Story 11.24 runtime/provider closure.

## Boundaries & Constraints

**Always:** Keep all 13 catalog rows on the single conditional `HexalithEventStoreVersion`; use live NuGet V3 evidence generated from `761dc018...`; preserve all non-EventStore catalog pins and durable audit decisions; retain CRLF/BOM policy; validate the exact local Builds commit before exposing it through the parent gitlink; keep the EventStore checkout at `94591f3539ce30372db58e5fdd3ba017ea8c07b8` / `v3.97.0`.

**Ask First:** Pushing either repository, committing FrontComposer, changing any package family other than EventStore, changing EventStore source, or expanding this package-only waiver into provider/pact reconciliation or Story 11.24 closure.

**Never:** Add a FrontComposer-local version override; edit workflow execution SHAs or point-version governance mirrors; accept unrelated live-audit upgrades; reference the catalog-only Builds commit as the final durable dependency; initialize nested submodules; weaken validation; stage, commit, or overwrite unrelated work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Aligned family | NuGet lists `3.97.0` for all 13 catalog packages | Catalog and accepted audit selection resolve every row to `3.97.0` | Fail on a missing, unlisted, split, or unresolved package |
| Audit drift | Catalog `3.97.0`, audit `3.96.2` | Audit is refreshed from `761dc018...` and deterministic validation passes | Do not select `761dc018...` as the final parent gitlink |
| Release consumption | Package mode with isolated cache | AppHost restores/builds exact `Hexalith.EventStore.Aspire/3.97.0` with no EventStore project edge | Fail; never fall back to source |
| Debug consumption | Source mode | AppHost builds against unchanged exact `v3.97.0` checkout | Stop if source identity moves |
| Authorization boundary | Package-only waiver | Story 11.24 stays backlog and provider verification remains open | Reject broader compatibility/closure claims |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:1,8,40-52` -- pushed base `761dc018...` changes only the shared EventStore selector to `3.97.0` but lacks the policy-required UTF-8 BOM; restore only that encoding marker while preserving its CRLF XML bytes, 13 aligned rows, and all other pins.
- `references/Hexalith.Builds/Tools/package-version-audit.json:1-12,237-260,4269-4566` -- stale provenance, family decision, and 13 accepted selections; the current validator reports exactly 13 `3.97.0`/`3.96.2` mismatches.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` and `Tools/README.md:67-105` -- generate live evidence to a temporary output, then merge only the authorized EventStore/provenance facts while preserving unrelated decisions.
- `Directory.Packages.props:1-13`, `Directory.Build.props:12-26`, `deps.local.props`, `deps.nuget.props` -- version-free import and existing Debug/source versus Release/package selection; read-only.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:11-39` -- explicit AppHost validation target; Release consumes only EventStore.Aspire and the solution omits AppHost.
- `eng/dependency-graph-policy.json:54-83` and `.github/workflows/{ci,release,release-evidence}.yml` -- shape-only catalog policy and independent immutable execution identities; read-only.
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md` -- read-only broader identity/provider gate; remains backlog.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Tools/package-version-audit.json` -- refresh to temporary output and reconcile only provenance plus the EventStore family decision and 13 rows to listed/accepted `3.97.0` evidence -- restore deterministic catalog/audit consistency without unrelated upgrades.
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` and `Tools/package-version-audit.json` -- restore the catalog's required UTF-8 BOM without XML-content changes, run all catalog/audit validators and Release build, then amend the scoped local commit to contain exactly the BOM repair and audit correction on top of pushed `761dc018...` -- provide a governance-valid dependency identity without pushing.
- [x] `references/Hexalith.Builds` parent gitlink -- leave it at the audit-corrected final commit and explicitly report its remote reachability before any FrontComposer parent commit; preserve the EventStore gitlink and execution SHAs.
- [x] Consumer validation -- prove isolated Release package consumption, Debug source consumption, focused governance/dependency-mode tests, EventStore adapter/Pact checks, and every matrix row.

**Acceptance Criteria:**
- Given the pushed catalog base and official NuGet evidence, when Builds validation runs, then all 13 EventStore rows select listed stable `3.97.0`, all other catalog pins remain unchanged, and unrelated audit decisions retain their prior dispositions.
- Given Release and Debug modes, when the AppHost restores and builds, then Release uses exact package `3.97.0` without source fallback and Debug uses unchanged exact tag `v3.97.0`.
- Given this package-only waiver, when work is handed off, then Story 11.24 remains backlog, no provider success is claimed, and no parent commit or push occurs while the Builds commit is local-only.

## Spec Change Log

- 2026-08-22: Human authorized expanding the audit-only local commit to repair the missing required UTF-8 BOM in `Props/Directory.Packages.props` after exact candidate governance failed; XML content, CRLF, version scope, and no-push boundaries remain unchanged.

## Design Notes

The catalog gitlink and immutable CI/Release execution SHA are intentionally independent after `spec-split-builds-catalog-gitlink-from-ci-cd-execution-sha.md`; this bump must not reintroduce their former lockstep or any local point-version mirror.

## Verification

**Commands:**

Run from the FrontComposer root. The Builds gates and exact committed catalog-byte check are:

```bash
set -euo pipefail
(
  cd references/Hexalith.Builds
  pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1
  pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1
  pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1
  pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1
  pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1
  dotnet build Hexalith.Builds.slnx --configuration Release
)
builds_sha=4eb33928a1d8c7775f97221cf9edc171db0cb5f8
base_sha=761dc0187ef60599f12310fef2411dbaf0206742
byte_tmp=$(mktemp -d)
mkdir "$byte_tmp/checkout"
test "$(git -C references/Hexalith.Builds cat-file blob "$builds_sha:Props/Directory.Packages.props" | od -An -tx1 -N3 | tr -d ' \n')" = efbbbf
cmp --silent \
  <(git -C references/Hexalith.Builds cat-file blob "$base_sha:Props/Directory.Packages.props") \
  <(git -C references/Hexalith.Builds cat-file blob "$builds_sha:Props/Directory.Packages.props" | tail -c +4)
test "$(git -C references/Hexalith.Builds check-attr --source="$builds_sha" eol -- Props/Directory.Packages.props)" = "Props/Directory.Packages.props: eol: crlf"
GIT_INDEX_FILE="$byte_tmp/index" git -C references/Hexalith.Builds read-tree "$builds_sha"
GIT_INDEX_FILE="$byte_tmp/index" git -C references/Hexalith.Builds checkout-index --prefix="$byte_tmp/checkout/" -- Props/Directory.Packages.props
materialized="$byte_tmp/checkout/Props/Directory.Packages.props"
test "$(od -An -tx1 -N3 "$materialized" | tr -d ' \n')" = efbbbf
perl -0777 -ne 'exit 1 if /(?<!\r)\n/; exit 1 unless /\r\n/; exit 0' "$materialized"
```

Validate the exact prospective parent gitlink through a temporary index and anonymous deterministic commit object; this does not move the real index or any ref:

```bash
set -euo pipefail
root_sha=36efb0c3f774744cd9556f256c0b47f9b0b6bcad
builds_sha=4eb33928a1d8c7775f97221cf9edc171db0cb5f8
real_index_tree=$(git write-tree)
candidate_tmp=$(mktemp -d)
printf '%s\n' 'build(deps): validate eventstore 3.97.0 candidate' > "$candidate_tmp/message.txt"
./node_modules/.bin/commitlint --edit "$candidate_tmp/message.txt" --verbose
GIT_INDEX_FILE="$candidate_tmp/index" git read-tree "$root_sha"
GIT_INDEX_FILE="$candidate_tmp/index" git update-index --add --cacheinfo "160000,$builds_sha,references/Hexalith.Builds"
candidate_tree=$(GIT_INDEX_FILE="$candidate_tmp/index" git write-tree)
candidate_sha=$(GIT_AUTHOR_NAME='Hexalith Validation' GIT_AUTHOR_EMAIL='validation@hexalith.invalid' GIT_AUTHOR_DATE='2026-08-22T09:30:00Z' GIT_COMMITTER_NAME='Hexalith Validation' GIT_COMMITTER_EMAIL='validation@hexalith.invalid' GIT_COMMITTER_DATE='2026-08-22T09:30:00Z' git commit-tree "$candidate_tree" -p "$root_sha" < "$candidate_tmp/message.txt")
test "$(git diff-tree --no-commit-id --name-only -r "$root_sha" "$candidate_sha")" = references/Hexalith.Builds
test "$(git ls-tree "$candidate_sha" references/Hexalith.Builds | awk '{print $3}')" = "$builds_sha"
python3 eng/dependency_graph.py --root . validate --commit "$candidate_sha" > "$candidate_tmp/dependency-graph.json"
test "$(jq -r .ok "$candidate_tmp/dependency-graph.json")" = true
test "$(jq -r .envelope.edge_count "$candidate_tmp/dependency-graph.json")" = 43
test "$(jq -r .semantics.selectors_validated "$candidate_tmp/dependency-graph.json")" = 7
test "$(git rev-parse HEAD)" = "$root_sha"
test "$(git write-tree)" = "$real_index_tree"
```

The isolated package/source identity recipe, including the known unfocused Release blocker, is:

```bash
set -euo pipefail
apphost=src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
release_tmp=$(mktemp -d)
release_packages="$release_tmp/packages"
release_evaluation="$release_tmp/evaluation.json"
dotnet restore "$apphost" --packages "$release_packages" --force --no-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
dotnet msbuild "$apphost" -nologo -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -getProperty:HexalithEventStoreVersion,HexalithEventStoreFromSource,UseHexalithProjectReferences,UseNuGetDeps -getItem:PackageReference,ProjectReference > "$release_evaluation"
jq -e '.Properties.HexalithEventStoreVersion == "3.97.0" and .Properties.HexalithEventStoreFromSource == "false" and .Properties.UseHexalithProjectReferences == "false" and .Properties.UseNuGetDeps == "true" and ([.Items.PackageReference[] | select(.Identity | startswith("Hexalith.EventStore")) | .Identity] == ["Hexalith.EventStore.Aspire"]) and ([.Items.ProjectReference[] | select(.FullPath | contains("/references/Hexalith.EventStore/"))] | length == 0)' "$release_evaluation"
jq -e '([.libraries | keys[] | select(startswith("Hexalith.EventStore"))] == ["Hexalith.EventStore.Aspire/3.97.0"]) and ([.project.frameworks[].projectReferences // {} | keys[] | select(contains("/references/Hexalith.EventStore/"))] | length == 0)' src/Hexalith.FrontComposer.AppHost/obj/project.assets.json
dotnet build "$apphost" --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:BuildProjectReferences=false -p:RestorePackagesPath="$release_packages"
set +e
dotnet build "$apphost" --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:RestorePackagesPath="$release_packages" > "$release_tmp/full-transitive.log" 2>&1
full_release_rc=$?
set -e
test "$full_release_rc" = 1
rg -q "Program.cs.*namespace name 'Parties'" "$release_tmp/full-transitive.log"
rg -q "Program.cs.*namespace name 'Tenants'" "$release_tmp/full-transitive.log"

debug_tmp=$(mktemp -d)
debug_packages="$debug_tmp/packages"
debug_evaluation="$debug_tmp/evaluation.json"
dotnet restore "$apphost" --packages "$debug_packages" --force --no-cache -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false
dotnet msbuild "$apphost" -nologo -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -getProperty:HexalithEventStoreVersion,HexalithEventStoreFromSource,UseHexalithProjectReferences,UseNuGetDeps,EventStorePath -getItem:PackageReference,ProjectReference > "$debug_evaluation"
jq -e '.Properties.HexalithEventStoreVersion == "3.97.0" and .Properties.HexalithEventStoreFromSource == "true" and .Properties.UseHexalithProjectReferences == "true" and .Properties.UseNuGetDeps == "false" and ([.Items.PackageReference[] | select(.Identity | startswith("Hexalith.EventStore"))] | length == 0) and ([.Items.ProjectReference[] | select(.FullPath | contains("/references/Hexalith.EventStore/"))] | length == 4)' "$debug_evaluation"
jq -e '[.libraries | to_entries[] | select(.key | startswith("Hexalith.EventStore")) | {key, type: .value.type}] == [{"key":"Hexalith.EventStore.Aspire/3.97.0","type":"project"}]' src/Hexalith.FrontComposer.AppHost/obj/project.assets.json
test "$(git -C references/Hexalith.EventStore rev-parse HEAD)" = 94591f3539ce30372db58e5fdd3ba017ea8c07b8
test "$(git -C references/Hexalith.EventStore describe --tags --exact-match HEAD)" = v3.97.0
dotnet build "$apphost" --configuration Debug --no-restore --no-incremental --maxcpucount:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:RestorePackagesPath="$debug_packages"
```

Focused governance, adapter, and consumer-contract checks are:

```bash
set -euo pipefail
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -method 'Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds' -method 'Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease'
(cd references/Hexalith.EventStore && DiffEngine_Disabled=true dotnet test Hexalith.EventStore.slnx --configuration Release --no-restore --no-build --filter 'FullyQualifiedName~Adapter|FullyQualifiedName~Pact')
DiffEngine_Disabled=true ./tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests -class 'Hexalith.FrontComposer.Shell.Tests.Pact.EventStorePactContractTests'
test "$(jq -r .interactionCount tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json)" = 19
contract_artifacts=$(mktemp -d)
pwsh -NoProfile -File ./eng/validate-contract-artifacts.ps1 -ArtifactDir "$contract_artifacts"
git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact
git -C references/Hexalith.EventStore diff --exit-code -- Hexalith.EventStore.slnx src/Hexalith.EventStore.Server/Pact
rg -F 'Provider verification result: BLOCKED_HANDOFF' "$contract_artifacts/provider-verification-blocked.txt"
```

**Observed results (2026-08-22):**
- The live audit generated from `761dc0187ef60599f12310fef2411dbaf0206742` enumerated 284 packages from NuGet V3; all 13 EventStore rows were listed stable `3.97.0`. Structural comparison proved every unrelated family decision and package row was preserved.
- The EventStore family decision now separates NuGet listing evidence for all 13 packages from the narrower compatibility evidence: FrontComposer exercised only its Release Aspire package edge, Debug source graph, adapter checks, and consumer-contract checks. Broader provider verification and other-package consumer coverage remain open; every non-EventStore field remains unchanged.
- Builds commit `4eb33928a1d8c7775f97221cf9edc171db0cb5f8` has exact parent `761dc018...` and changes only `Props/Directory.Packages.props` plus `Tools/package-version-audit.json`. Its committed blob begins with the three-byte UTF-8 BOM and, after removing that marker, exactly matches the base XML blob; temporary-index materialization under the committed `eol=crlf` attribute contains CRLF only. Pinned commitlint passed before and after amend. This task did not run a push; during the final shared-workspace snapshot, `origin/main` advanced externally to this same SHA.
- Central catalog validation passed for 284 entries; authoritative catalog tests passed for 49 identities and 3 shared versions; deterministic audit validation passed for 284 packages, 139 families, and 1 source; generator and validator fixtures passed 14 and 29 scenarios. `dotnet build Hexalith.Builds.slnx --configuration Release` passed with 0 warnings and 0 errors.
- Isolated Release restore used `/tmp/frontcomposer-eventstore-release.K2Ug7S`: MSBuild selected `HexalithEventStoreVersion=3.97.0`, source mode was false, assets contained only `Hexalith.EventStore.Aspire/3.97.0` as a package, and no EventStore project edge existed. The focused AppHost compile passed with 0 warnings and 0 errors using `BuildProjectReferences=false`. The full transitive build without that property remains blocked by the pre-existing FrontComposer UI package gate: it returned 1 with 23 `CS0234`/`CS0103` errors for unavailable Parties/Tenants namespaces and symbols.
- Debug restore used `/tmp/frontcomposer-eventstore-debug.jpBbBK`: source mode was true with no EventStore package edge, the AppHost had four direct EventStore project edges, and its serialized graph build passed with 0 warnings and 0 errors. The clean EventStore checkout remained `94591f3539ce30372db58e5fdd3ba017ea8c07b8`, exact tag `v3.97.0`.
- Deterministic anonymous parent candidate `0be2ff014eac5f86bd4ede4d753809891c318cd9` selected `4eb3392...` without moving the real index or a ref and passed dependency-graph validation for 43 edges and 7 selectors. The committed root HEAD was not used as evidence for the new gitlink because it still selects the old Builds commit.
- Focused governance/dependency-mode tests passed 2/2. The EventStore adapter/Pact filter passed 190/190 across the matching projects; FrontComposer's consumer Pact class passed 3/3 and reproduced the 19-interaction manifest. Pact stale-diff and contract-artifact validation passed while explicitly emitting `BLOCKED_HANDOFF`, not provider success.
- FrontComposer remained at uncommitted baseline `36efb0c3f774744cd9556f256c0b47f9b0b6bcad`; workflow execution identities and the EventStore gitlink were unchanged. Story 11.24 remained backlog and provider verification remained an explicit blocked handoff. The final Builds SHA is now present on `origin/main`, while its FrontComposer parent gitlink remains uncommitted.

## Suggested Review Order

**Audit evidence scope**

- Separate family-wide listing evidence from FrontComposer's narrow compatibility coverage.
  [`package-version-audit.json:255`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L255)

- Give every EventStore row a truthful listing rationale and repository-neutral rollback trigger.
  [`package-version-audit.json:4279`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L4279)

**Durable identity and verification**

- Preserve the catalog XML while restoring its required materialized UTF-8 BOM and CRLF.
  [`Directory.Packages.props:1`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L1)

- Reproduce candidate governance, consumer identities, and the explicit transitive-build blocker.
  [`spec-bump-eventstore-to-3-97-0.md:73`](spec-bump-eventstore-to-3-97-0.md#L73)

**Deferred follow-ups**

- Keep pre-existing governance and coverage gaps visible without widening this dependency bump.
  [`deferred-work.md:2547`](deferred-work.md#L2547)
