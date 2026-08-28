---
title: 'Bump EventStore Package Family to 3.98.0'
type: 'refactor'
created: '2026-08-28'
status: 'done'
baseline_commit: '7b11ca8978c707d3b639ee57c14343c8a3287c8e'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer Debug already consumes the latest EventStore `main` commit, `1c20429750bb2492bbf10c2a486239aedfe3022b` (`v3.98.0-7-g1c204297`), but Release still resolves the shared 13-package EventStore family at `3.97.0`. Source and package modes therefore do not use the user-requested latest source/package combination.

**Approach:** Preserve the current EventStore gitlink and advance the Builds-owned package selector and deterministic audit to `3.98.0`. Create the required local Builds commit so FrontComposer can select an immutable catalog identity, repairing the catalog's required UTF-8 BOM in the same scoped change.

## Boundaries & Constraints

**Always:** Keep all 13 EventStore package rows on the single conditional `HexalithEventStoreVersion`; preserve every unrelated catalog selection and durable audit decision; retain CRLF and restore the required UTF-8 BOM; verify NuGet.org listing evidence; validate the exact local Builds commit before selecting its parent gitlink; keep EventStore gitlink and checkout exactly `1c20429750bb2492bbf10c2a486239aedfe3022b`.

**Ask First:** Pushing either repository, committing FrontComposer, accepting any unrelated live-audit package upgrade, changing another gitlink or package family, or expanding this bump into provider/Pact reconciliation or Story 11.24 closure.

**Never:** Retarget EventStore to exact tag `v3.98.0`; edit EventStore contents or nested submodules; add a FrontComposer-local/inline version override; change dependency wiring, workflow execution SHAs, or policy values; weaken validation; overwrite, stage, or commit unrelated work; claim exact source/package commit parity or provider success.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Release packages | Package mode with isolated cache | Only `Hexalith.EventStore.Aspire/3.98.0`; no EventStore project edge | Fail without falling back to source |
| Debug source | Project-reference mode | Four root EventStore project edges at unchanged `1c204297...`; no package edge | Stop if gitlink or checkout moves |
| Catalog governance | Catalog has stale audit and missing BOM | Audit is reconciled, BOM/CRLF restored, unrelated selections unchanged | Fail on unresolved/missing family rows or unintended upgrades |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore` -- read-only root gitlink/checkout at latest `main` `1c204297...`; exact tag `v3.98.0` is seven commits behind at `b36a39b8...`.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52` -- change the shared selector from `3.97.0` to `3.98.0`; preserve 13 aligned rows, CRLF, BOM, and all other pins.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- refresh catalog provenance and NuGet evidence; select/audit all 13 EventStore rows at listed latest stable `3.98.0` while preserving the `retained` family decision. Reconcile the four already-merged unrelated selections -- `Roslynator.Analyzers`, `Roslynator.Formatting.Analyzers`, `SonarAnalyzer.CSharp`, and `xunit.v3` -- without accepting them.
- `references/Hexalith.Builds/Tools/{audit-central-package-versions,validate-package-version-audit}.ps1` -- reuse the live generator and deterministic validator; do not edit.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-97-0.md:75-159` -- reuse its proven committed-byte, prospective-parent, and isolated AppHost identity recipes with `3.98.0` expectations.
- `Directory.Packages.props`, `deps.local.props`, `deps.nuget.props` -- read-only central import and Debug/Release mode selection; no local override.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:15-38` -- Release Aspire package edge and four Debug source edges used for identity validation.
- `eng/dependency_graph.py`, `eng/dependency-graph-policy.json` -- validate the prospective parent commit; policy remains unchanged.
- `tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md` -- committed provider handoff: EventStore owns deterministic provider states and real-loopback TCP verification for all 19 committed interactions; FrontComposer consumer tests do not complete this gate.
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md` -- read-only broader runtime/provider story; this scoped bump does not close it.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds` -- create one pinned-commitlint-validated local commit that selects and lists all 13 EventStore rows at `3.98.0`, restores catalog BOM/CRLF, and reconciles current audit facts without accepting EventStore or unrelated upgrades. The EventStore family/rows remain `retained` because Builds has zero owned representative EventStore consumers; completion here is governed selection/listing, not owner or compatibility acceptance.
- [x] `references/Hexalith.Builds` parent gitlink -- select the validated local commit while leaving `references/Hexalith.EventStore` unchanged and making no parent commit or push.
- [x] `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-98-0.md` -- record passing isolated Release, Debug source, catalog/audit, prospective dependency-graph, focused governance, and consumer Pact evidence.

**Acceptance Criteria:**
- Given the finalized Builds commit, when catalog and audit validators run, then all 13 EventStore rows resolve to listed stable `3.98.0`, BOM/CRLF policy passes, and unrelated catalog selections remain byte-semantically unchanged.
- Given Release mode, when the AppHost restores and evaluates from an isolated cache, then it resolves only `Hexalith.EventStore.Aspire/3.98.0` and zero EventStore project references.
- Given Debug mode, when the AppHost restores and evaluates, then it uses four root EventStore project references at `1c204297...` and no EventStore package reference.
- Given the prospective FrontComposer tree, when dependency-graph validation runs, then it passes with the new Builds gitlink, unchanged EventStore gitlink, and unchanged workflow/policy identities.

## Spec Change Log

- 2026-08-28 delivery authorization -- the human authorized pushing the validated Builds commit and creating the scoped local FrontComposer commit. Builds `origin/main` now resolves to `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade`; FrontComposer push remains unauthorized. KEEP: exact three-path parent scope and all prior validation/review evidence.
- 2026-08-28 review deferrals -- recorded the pre-existing parent-revision provenance ambiguity and catalog-wide generated-history growth in `deferred-work.md`; neither changes the validated `3.98.0` selection or expands this bump into audit-generator redesign. KEEP: exact Builds/EventStore identities and all green implementation evidence.
- 2026-08-28 review patch -- replaced prose-only verification with exact reproducible commands; documented anonymous-candidate construction, retained audit semantics, generated-history/provenance limitations, Pact/provider ownership, remote-reachability delivery safety, atomic rollback, and observed broad blockers. Avoid the known-bad states of treating `retained` as compatibility acceptance, treating consumer Pact success as provider/package-family proof, relying on an anonymous commit as a durable ref, or committing a parent pointer to a remote-unreachable Builds object. KEEP: immutable successful Builds commit `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade`, unchanged EventStore `1c20429750bb2492bbf10c2a486239aedfe3022b`, all green catalog/audit/build/identity/governance/consumer-Pact evidence, and the no-parent-commit/no-push boundary.
- 2026-08-28 artifact-scope gate -- added the root-owned File List and rewrote checked-task evidence paths to the actual parent changes; this avoids claiming unchanged validation targets or submodule-internal paths as parent-repository changes while preserving all implementation and verification evidence.

## Design Notes

`3.98.0` identifies the published package family, not the source commit. The approved source identity is latest EventStore `main`, seven commits after the release tag, so validation must record that distinction instead of asserting exact source/package parity.

The generated EventStore family and all 13 rows intentionally remain `retained`. The current Builds audit requires an accepted family to name owned representative consumers, but Builds contains zero owned EventStore `PackageReference` consumers. This bump proves catalog selection and current NuGet listing evidence at `3.98.0`; broader binary compatibility, owner acceptance, other-package consumer coverage, and provider compatibility remain open.

The live generator preserves typed historical decisions whenever catalog bytes or package metadata change. Rebinding the complete 285-row catalog therefore expanded generated history across the file (`13,241` insertions and `1,447` deletions) even though the only newly requested catalog selector was EventStore and the four pre-existing stale selections stayed `retained`. The audit's `generatedFromRevision` is `8f255570b2df14603a943e8d7ee0c5d3f0b025fc`, the parent Builds HEAD from which the dirty candidate was generated; `catalogSha256=ff1d2f16218a3edab2ae2cc8e48fad3a633df2b294e359cee2c02dae12cf6c5a` binds the actual candidate catalog bytes. This is a known pre-existing provenance convention/limitation, not a claim that `8f255570...` contains the new catalog.

Consumer Pact success validates the unchanged FrontComposer consumer contract only. The 3/3 test-class result reproduces 19 interaction fixtures; it does not execute all 13 package binaries or prove provider success. The committed [provider verification handoff](../../tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md) keeps completion with EventStore: deterministic state setup/cleanup and all 19 interactions must run against the pinned provider through real loopback TCP. Until that provider-owned report exists and is accepted by the required contract-artifact lane, the status remains `BLOCKED_HANDOFF`.

Delivery remains fail-closed: human-authorized push completed, and Builds `origin/main` now resolves to `c883721...`. The parent commit may therefore select that reachable identity. Atomic rollback changes only the Builds gitlink back to `8f255570b2df14603a943e8d7ee0c5d3f0b025fc`; EventStore must remain `1c20429750bb2492bbf10c2a486239aedfe3022b`.

Known broad blockers remain separate. The exact full-transitive AppHost command recorded below exited `1`; representative diagnostics are `src/Hexalith.FrontComposer.UI/Program.cs(2,16): error CS0234` for `Hexalith.Parties`, `Program.cs(7,16): error CS0234` for `Hexalith.Tenants`, and `Components/Pages/AdminLanding.razor(2,32): error CS0103` for `PartiesUiAuthorization`. The inherited exact-solution FsCheck downgrade was not rerun during this bump, so there is no fresh log or exit status to attribute to this implementation. Do not alter dependency wiring, package families, or provider artifacts to hide either inherited issue.

## Verification

Run from the FrontComposer root. Validate the exact committed Builds identity and its complete catalog/audit suite:

```bash
set -euo pipefail
builds_sha=c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade
base_builds_sha=8f255570b2df14603a943e8d7ee0c5d3f0b025fc
(
  cd references/Hexalith.Builds
  test "$(git rev-parse HEAD)" = "$builds_sha"
  test -z "$(git status --porcelain)"
  ./node_modules/.bin/commitlint --last --verbose
  pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1
  pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1
  pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1
  pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1
  pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1
  dotnet build Hexalith.Builds.slnx --configuration Release
)
```

Validate committed scope, BOM, the `eol=crlf` policy, CRLF-only checkout materialization, audit semantics, and generator provenance:

```bash
set -euo pipefail
builds_sha=c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade
base_builds_sha=8f255570b2df14603a943e8d7ee0c5d3f0b025fc
test "$(git -C references/Hexalith.Builds diff-tree --no-commit-id --name-only -r "$base_builds_sha" "$builds_sha")" = $'Props/Directory.Packages.props\nTools/package-version-audit.json'
test "$(git -C references/Hexalith.Builds cat-file blob "$builds_sha:Props/Directory.Packages.props" | od -An -tx1 -N3 | tr -d ' \n')" = efbbbf
cmp --silent \
  <(git -C references/Hexalith.Builds cat-file blob "$base_builds_sha:Props/Directory.Packages.props") \
  <(git -C references/Hexalith.Builds cat-file blob "$builds_sha:Props/Directory.Packages.props" | tail -c +4 | perl -pe 's/3\.98\.0/3.97.0/')
test "$(git -C references/Hexalith.Builds check-attr --source="$builds_sha" eol -- Props/Directory.Packages.props)" = 'Props/Directory.Packages.props: eol: crlf'
byte_tmp=$(mktemp -d)
mkdir "$byte_tmp/checkout"
GIT_INDEX_FILE="$byte_tmp/index" git -C references/Hexalith.Builds read-tree "$builds_sha"
GIT_INDEX_FILE="$byte_tmp/index" git -C references/Hexalith.Builds checkout-index --prefix="$byte_tmp/checkout/" -- Props/Directory.Packages.props
materialized="$byte_tmp/checkout/Props/Directory.Packages.props"
test "$(od -An -tx1 -N3 "$materialized" | tr -d ' \n')" = efbbbf
perl -0777 -ne 'exit 1 if /(?<!\r)\n/; exit 1 unless /\r\n/; exit 0' "$materialized"
test "$(sha256sum "$materialized" | awk '{print $1}')" = ff1d2f16218a3edab2ae2cc8e48fad3a633df2b294e359cee2c02dae12cf6c5a
git -C references/Hexalith.Builds show "$builds_sha:Tools/package-version-audit.json" |
  jq -e '
    .generatedFromRevision == "8f255570b2df14603a943e8d7ee0c5d3f0b025fc" and
    .catalogSha256 == "ff1d2f16218a3edab2ae2cc8e48fad3a633df2b294e359cee2c02dae12cf6c5a" and
    ([.packages[] | select(.family == "hexalith-eventstore" and .auditedVersion == "3.98.0" and .selectedVersion == "3.98.0" and .latestStable == "3.98.0" and .listingState == "listed" and .disposition == "retained")] | length) == 13 and
    ([.packages[] | select(.id == "Roslynator.Analyzers" or .id == "Roslynator.Formatting.Analyzers" or .id == "SonarAnalyzer.CSharp" or .id == "xunit.v3") | .disposition] == ["retained", "retained", "retained", "retained"])'
```

Construct and validate the anonymous prospective parent. The fixed identities, message, author/committer data, and timestamp reproduce `9e8441022c5b51b65cb4d4704d9e00f7d253341c`. It is an anonymous validation object, not a branch/ref or durable delivery identity; if garbage-collected, regenerate it with this block.

```bash
set -euo pipefail
root_sha=7b11ca8978c707d3b639ee57c14343c8a3287c8e
builds_sha=c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade
eventstore_sha=1c20429750bb2492bbf10c2a486239aedfe3022b
real_index_tree=$(git write-tree)
candidate_tmp=$(mktemp -d)
printf '%s\n' 'build(deps): validate EventStore 3.98.0 candidate' > "$candidate_tmp/message.txt"
./node_modules/.bin/commitlint --edit "$candidate_tmp/message.txt" --verbose
GIT_INDEX_FILE="$candidate_tmp/index" git read-tree "$root_sha"
GIT_INDEX_FILE="$candidate_tmp/index" git update-index --add --cacheinfo "160000,$builds_sha,references/Hexalith.Builds"
candidate_tree=$(GIT_INDEX_FILE="$candidate_tmp/index" git write-tree)
candidate_sha=$(
  GIT_AUTHOR_NAME='Hexalith Validation' \
  GIT_AUTHOR_EMAIL='validation@hexalith.invalid' \
  GIT_AUTHOR_DATE='2026-08-28T07:30:00Z' \
  GIT_COMMITTER_NAME='Hexalith Validation' \
  GIT_COMMITTER_EMAIL='validation@hexalith.invalid' \
  GIT_COMMITTER_DATE='2026-08-28T07:30:00Z' \
  git commit-tree "$candidate_tree" -p "$root_sha" < "$candidate_tmp/message.txt"
)
test "$candidate_sha" = 9e8441022c5b51b65cb4d4704d9e00f7d253341c
test "$(git diff-tree --no-commit-id --name-only -r "$root_sha" "$candidate_sha")" = references/Hexalith.Builds
test "$(git ls-tree "$candidate_sha" references/Hexalith.Builds | awk '{print $3}')" = "$builds_sha"
test "$(git ls-tree "$candidate_sha" references/Hexalith.EventStore | awk '{print $3}')" = "$eventstore_sha"
python3 eng/dependency_graph.py --root . validate --commit "$candidate_sha" > "$candidate_tmp/dependency-graph.json"
jq -e '.ok == true and .envelope.edge_count == 43 and .semantics.selectors_validated == 7' "$candidate_tmp/dependency-graph.json"
test "$(git rev-parse HEAD)" = "$root_sha"
test "$(git write-tree)" = "$real_index_tree"
```

Validate isolated Release package consumption and the focused AppHost build:

```bash
set -euo pipefail
apphost=src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
release_tmp=$(mktemp -d)
release_packages="$release_tmp/packages"
release_evaluation="$release_tmp/evaluation.json"
dotnet restore "$apphost" --packages "$release_packages" --force --no-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
dotnet msbuild "$apphost" -nologo -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -getProperty:HexalithEventStoreVersion,HexalithEventStoreFromSource,UseHexalithProjectReferences,UseNuGetDeps -getItem:PackageReference,ProjectReference > "$release_evaluation"
jq -e '.Properties.HexalithEventStoreVersion == "3.98.0" and .Properties.HexalithEventStoreFromSource == "false" and .Properties.UseHexalithProjectReferences == "false" and .Properties.UseNuGetDeps == "true" and ([.Items.PackageReference[] | select(.Identity | startswith("Hexalith.EventStore")) | .Identity] == ["Hexalith.EventStore.Aspire"]) and ([.Items.ProjectReference[] | select(.FullPath | contains("/references/Hexalith.EventStore/"))] | length == 0)' "$release_evaluation"
jq -e '([.libraries | keys[] | select(startswith("Hexalith.EventStore"))] == ["Hexalith.EventStore.Aspire/3.98.0"]) and ([.project.frameworks[].projectReferences // {} | keys[] | select(contains("/references/Hexalith.EventStore/"))] | length == 0)' src/Hexalith.FrontComposer.AppHost/obj/project.assets.json
dotnet build "$apphost" --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:BuildProjectReferences=false -p:RestorePackagesPath="$release_packages"
```

Record the known full-transitive Release blocker without weakening or hiding it:

```bash
set -euo pipefail
apphost=src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
release_tmp=$(mktemp -d)
release_packages="$release_tmp/packages"
dotnet restore "$apphost" --packages "$release_packages" --force --no-cache -p:Configuration=Release -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true
set +e
dotnet build "$apphost" --configuration Release --no-restore -p:UseHexalithProjectReferences=false -p:UseNuGetDeps=true -p:RestorePackagesPath="$release_packages" > "$release_tmp/full-transitive.log" 2>&1
full_release_rc=$?
set -e
test "$full_release_rc" = 1
rg -q "Program.cs\(2,16\): error CS0234.*Parties" "$release_tmp/full-transitive.log"
rg -q "Program.cs\(7,16\): error CS0234.*Tenants" "$release_tmp/full-transitive.log"
rg -q "AdminLanding.razor\(2,32\): error CS0103.*PartiesUiAuthorization" "$release_tmp/full-transitive.log"
```

Validate isolated Debug source consumption, exact source identity, and the serialized source build:

```bash
set -euo pipefail
apphost=src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj
debug_tmp=$(mktemp -d)
debug_packages="$debug_tmp/packages"
debug_evaluation="$debug_tmp/evaluation.json"
dotnet restore "$apphost" --packages "$debug_packages" --force --no-cache -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false
dotnet msbuild "$apphost" -nologo -p:Configuration=Debug -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -getProperty:HexalithEventStoreVersion,HexalithEventStoreFromSource,UseHexalithProjectReferences,UseNuGetDeps,EventStorePath -getItem:PackageReference,ProjectReference > "$debug_evaluation"
jq -e '.Properties.HexalithEventStoreVersion == "3.98.0" and .Properties.HexalithEventStoreFromSource == "true" and .Properties.UseHexalithProjectReferences == "true" and .Properties.UseNuGetDeps == "false" and ([.Items.PackageReference[] | select(.Identity | startswith("Hexalith.EventStore"))] | length == 0) and ([.Items.ProjectReference[] | select(.FullPath | contains("/references/Hexalith.EventStore/"))] | length == 4)' "$debug_evaluation"
jq -e '[.libraries | to_entries[] | select(.key | startswith("Hexalith.EventStore")) | {key, type: .value.type}] == [{"key":"Hexalith.EventStore.Aspire/3.98.0","type":"project"}]' src/Hexalith.FrontComposer.AppHost/obj/project.assets.json
test "$(git -C references/Hexalith.EventStore rev-parse HEAD)" = 1c20429750bb2492bbf10c2a486239aedfe3022b
test "$(git -C references/Hexalith.EventStore describe --tags --long --always HEAD)" = v3.98.0-7-g1c204297
dotnet build "$apphost" --configuration Debug --no-restore --no-incremental --maxcpucount:1 -p:UseHexalithProjectReferences=true -p:UseNuGetDeps=false -p:RestorePackagesPath="$debug_packages"
```

Run focused governance, unchanged consumer Pact, contract-artifact, and handoff checks:

```bash
set -euo pipefail
test_bin=tests/Hexalith.FrontComposer.Shell.Tests/bin/Release/net10.0/Hexalith.FrontComposer.Shell.Tests
test -x "$test_bin"
DiffEngine_Disabled=true "$test_bin" -method 'Hexalith.FrontComposer.Shell.Tests.Governance.InfrastructureGovernanceTests.CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds' -method 'Hexalith.FrontComposer.Shell.Tests.Governance.CiGovernanceTests.HexalithDependencyMode_DefaultsToProjectReferencesForDebugAndPackagesForRelease'
DiffEngine_Disabled=true "$test_bin" -class 'Hexalith.FrontComposer.Shell.Tests.Pact.EventStorePactContractTests'
test "$(jq -r .interactionCount tests/Hexalith.FrontComposer.Shell.Tests/Pact/interaction-manifest.json)" = 19
contract_artifacts=$(mktemp -d)
pwsh -NoProfile -File ./eng/validate-contract-artifacts.ps1 -ArtifactDir "$contract_artifacts"
git diff --exit-code -- tests/Hexalith.FrontComposer.Shell.Tests/Pact
rg -F 'Provider verification result: BLOCKED_HANDOFF' "$contract_artifacts/provider-verification-blocked.txt"
rg -F 'Release status: blocked until provider verification runs against the pinned EventStore provider version.' tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md
rg -F 'real loopback TCP endpoint' tests/Hexalith.FrontComposer.Shell.Tests/Pact/provider-verification-handoff.md
```

Before the authorized FrontComposer commit, prove the Builds commit is reachable from the intended remote. This gate now passes after the separately authorized Builds push.

```bash
set -euo pipefail
builds_sha=c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade
git -C references/Hexalith.Builds fetch origin main --quiet
git -C references/Hexalith.Builds merge-base --is-ancestor "$builds_sha" origin/main
test "$(git -C references/Hexalith.Builds rev-parse HEAD)" = "$builds_sha"
test "$(git -C references/Hexalith.EventStore rev-parse HEAD)" = 1c20429750bb2492bbf10c2a486239aedfe3022b
```

Atomic rollback restores only the prior Builds gitlink and rechecks EventStore. Any parent rollback commit remains separately authorization- and commitlint-gated.

```bash
set -euo pipefail
prior_builds_sha=8f255570b2df14603a943e8d7ee0c5d3f0b025fc
eventstore_sha=1c20429750bb2492bbf10c2a486239aedfe3022b
git -C references/Hexalith.Builds switch --detach "$prior_builds_sha"
test "$(git -C references/Hexalith.Builds rev-parse HEAD)" = "$prior_builds_sha"
test "$(git -C references/Hexalith.EventStore rev-parse HEAD)" = "$eventstore_sha"
test "$(git diff --name-only)" = references/Hexalith.Builds
```

**Observed results (2026-08-28):**
- Builds commit `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade` has parent `8f255570b2df14603a943e8d7ee0c5d3f0b025fc`, changes only the catalog and audit, and is now reachable as Builds `origin/main` after explicit human authorization. Repository-pinned commitlint passed before and after the commit and across the pushed one-commit range.
- The committed catalog blob starts with `efbbbf`; checkout materialization is CRLF-only. Removing the BOM and reverting only `3.98.0` to `3.97.0` reproduces the parent catalog bytes. All 13 EventStore rows remain on `HexalithEventStoreVersion`.
- Live NuGet V3 discovery recorded 285 packages from one source. All 13 EventStore rows are `retained` and listed with `auditedVersion`, `selectedVersion`, and `latestStable` equal to `3.98.0`; `Roslynator.Analyzers`, `Roslynator.Formatting.Analyzers`, `SonarAnalyzer.CSharp`, and `xunit.v3` also remain `retained`. The catalog-wide history expansion came from the generator rebinding preserved typed history after catalog/metadata changes, not from accepting unrelated upgrades. Catalog, authoritative-catalog, audit validation, 55 generator scenarios, 60 validator scenarios, and the Builds Release build passed with zero warnings/errors.
- Anonymous parent candidate `9e8441022c5b51b65cb4d4704d9e00f7d253341c` selects the new Builds commit only and passed dependency-graph validation with 43 edges and 7 selectors. It has no durable ref and may be regenerated from the recorded inputs. FrontComposer HEAD/index did not move, and EventStore stayed at `1c20429750bb2492bbf10c2a486239aedfe3022b` (`v3.98.0-7-g1c204297`).
- Isolated Release evaluation restored only `Hexalith.EventStore.Aspire/3.98.0` with no EventStore project edge; its focused AppHost build passed with zero warnings/errors. The full transitive command exited `1` with the recorded Parties/Tenants diagnostics. The inherited FsCheck downgrade was not rerun.
- Debug evaluation used four root EventStore project edges, no EventStore package edge, and the unchanged source checkout; the serialized Debug AppHost build passed with zero warnings/errors.
- Focused governance/dependency-mode tests passed 2/2; the unchanged consumer Pact class passed 3/3 and reproduced all 19 interactions. This does not exercise all 13 binaries or the provider. Contract-artifact validation passed while retaining `Provider verification result: BLOCKED_HANDOFF`; completion remains with the EventStore-owned real-loopback provider handoff.

## File List

- `references/Hexalith.Builds` -- parent gitlink now selects remote-reachable Builds commit `c8837217e6c07f7e12ccf3e3b5e86c5bc83ceade`; that commit changes only `Props/Directory.Packages.props` and `Tools/package-version-audit.json`.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-98-0.md` -- approved scope, workflow status, completed tasks, verification results, and review evidence for this freeform dependency bump.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- appended two review-confirmed, out-of-scope audit-generator design issues without changing existing entries.

## Suggested Review Order

**Dependency intent and selection**

- Start with the approved source/package identity split and immutable catalog approach.
  [`spec-bump-eventstore-package-to-3-98-0.md:14`](spec-bump-eventstore-package-to-3-98-0.md#L14)

- The shared selector moves all thirteen EventStore package rows atomically.
  [`Directory.Packages.props:8`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L8)

**Audit truth**

- Retained family semantics separate governed selection from compatibility acceptance.
  [`package-version-audit.json:2368`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L2368)

- The consumed Aspire package records listed stable `3.98.0` evidence.
  [`package-version-audit.json:39372`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L39372)

- Parent-revision provenance is independently bound to exact candidate catalog bytes.
  [`package-version-audit.json:4`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L4)

**Delivery and verification**

- Remote reachability and atomic rollback keep parent delivery fail-closed.
  [`spec-bump-eventstore-package-to-3-98-0.md:81`](spec-bump-eventstore-package-to-3-98-0.md#L81)

- Reproducible commands cover catalog, graph, Release, Debug, and contracts.
  [`spec-bump-eventstore-package-to-3-98-0.md:85`](spec-bump-eventstore-package-to-3-98-0.md#L85)

- Observed results distinguish proven selection from open provider compatibility.
  [`spec-bump-eventstore-package-to-3-98-0.md:258`](spec-bump-eventstore-package-to-3-98-0.md#L258)

**Deferred generator design debt**

- Provenance and history-growth concerns remain visible without widening this bump.
  [`deferred-work.md:9393`](deferred-work.md#L9393)
