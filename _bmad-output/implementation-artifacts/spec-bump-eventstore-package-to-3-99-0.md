---
title: 'Bump EventStore Package Family to 3.99.0'
type: 'refactor'
created: '2026-08-28'
status: 'done'
baseline_commit: '08c2ddb5cd914b23fef88794cb7f9a1ff908fca7'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer Debug is pinned to the exact EventStore `v3.99.0` source commit, but Release still inherits the 13-package EventStore family at `3.98.0` from the Builds catalog. The two dependency modes therefore select different EventStore releases.

**Approach:** Advance the single Builds-owned EventStore package selector and its governed audit evidence to `3.99.0`, then prove FrontComposer Release restores that package version while Debug retains the existing source checkout.

## Boundaries & Constraints

**Always:** Keep all 13 EventStore package rows on the one conditional `HexalithEventStoreVersion`; preserve unrelated catalog selections and audit dispositions; retain the catalog's UTF-8 BOM and CRLF policy; use official NuGet V3 listing evidence; keep EventStore at `f18fbf113e1ccfb41d330a3e4aecb913c16bc6de` (`v3.99.0`).

**Ask First:** Committing, staging, or pushing either repository; changing another package family or gitlink; accepting an unrelated audit upgrade; editing EventStore source, dependency wiring, application code, policy, or CI.

**Never:** Add a FrontComposer-local or inline package override; update or initialize submodules; split the EventStore family across versions; weaken validation; overwrite unrelated work; claim provider/Pact compatibility from package selection alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Release package mode | `UseHexalithProjectReferences=false` | Only `Hexalith.EventStore.Aspire/3.99.0`; no EventStore project edge | Fail without falling back to source |
| Debug source mode | Existing root EventStore checkout | Project edges resolve to exact `v3.99.0`; no EventStore package edge | Stop if the source identity moves |
| Catalog audit | 13 aligned rows at `3.99.0` | Every row is listed/latest stable and remains `retained` | Stop on missing rows or unrelated selection/disposition drift |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52` -- shared selector currently `3.98.0`; its 13 rows already consume that one property. Preserve BOM and CRLF.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- generated 285-package governance evidence; refresh the 13 `hexalith-eventstore` rows without accepting unrelated upgrades.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- canonical live NuGet V3 generator; reuse without editing.
- `references/Hexalith.Builds/Tools/{validate-central-package-versions,test-authoritative-package-catalog,validate-package-version-audit,test-package-version-audit-generator,test-package-version-audit-validator}.ps1` -- catalog and audit gates; reuse without editing.
- `Directory.Packages.props`, `deps.local.props`, `deps.nuget.props` -- read-only central import and Debug/Release mode switches; no local version belongs here.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:15-38` -- Release consumes `Hexalith.EventStore.Aspire`; Debug exposes the four EventStore project edges used for identity checks.
- `references/Hexalith.EventStore` -- read-only exact `v3.99.0` source identity.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-98-0.md` -- prior bump's validated catalog/audit and isolated restore pattern; do not copy its obsolete identities.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds` -- change only `Props/Directory.Packages.props`'s `HexalithEventStoreVersion` from `3.98.0` to `3.99.0`, regenerate `Tools/package-version-audit.json`, and inspect both so the aligned rows and byte policy are preserved while unrelated selections/dispositions remain unchanged.
- [x] FrontComposer dependency evaluation -- validate isolated Release package consumption and unchanged Debug source consumption without modifying dependency wiring.

**Acceptance Criteria:**
- Given the Builds catalog, when it is evaluated and validated, then exactly 13 EventStore rows resolve through `HexalithEventStoreVersion=3.99.0` and all catalog/audit gates pass.
- Given official NuGet metadata, when the audit is regenerated, then all 13 EventStore entries record `auditedVersion`, `selectedVersion`, and `latestStable` as `3.99.0`, `listingState=listed`, and `disposition=retained`.
- Given FrontComposer Release mode, when the AppHost is restored and evaluated with an isolated package cache, then it contains only `Hexalith.EventStore.Aspire/3.99.0` and no EventStore project reference.
- Given Debug source mode, when the AppHost is evaluated, then it retains four root EventStore project references, no EventStore package reference, and exact source identity `v3.99.0`.

## Spec Change Log

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1` from `references/Hexalith.Builds` -- expected: refreshes audit evidence from the configured official source.
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1; pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1; pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` from Builds -- expected: all catalog and production-audit gates pass.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1; pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1; dotnet build Hexalith.Builds.slnx --configuration Release` from Builds -- expected: generator/validator scenarios and Release build pass.
- Isolated `dotnet restore`, `dotnet msbuild -getProperty/-getItem`, and focused AppHost Release build with package mode -- expected: only Aspire `3.99.0`, zero EventStore project edges, zero warnings/errors.
- Debug `dotnet msbuild -getProperty/-getItem` plus exact EventStore tag checks -- expected: four EventStore project edges, zero package edges, checkout `v3.99.0`.

**Observed results (2026-08-28):**
- The live audit generated 285 package records from one source. Exactly the 13 EventStore selections moved to `3.99.0`; all remain `retained`, and zero unrelated selected-version or disposition changes occurred. Audit provenance is Builds `569a6e9554b69a5c5e042affb837649e205b5ef8` with catalog SHA-256 `1a1535739095c27ad45ade0c595c68a6a1077b6500d38f5122172fc0f19c411e`.
- Central catalog validation passed 285 entries; authoritative-catalog tests passed 49 identities and three shared selectors; production audit validation passed 285 packages, 140 families, and one source; generator tests passed 55 scenarios; validator tests passed 60 scenarios. `dotnet build Hexalith.Builds.slnx --configuration Release` passed with zero warnings and errors.
- Isolated Release restore/evaluation resolved only `Hexalith.EventStore.Aspire/3.99.0`, no EventStore project edge, and the focused AppHost build passed with zero warnings and errors. Isolated Debug evaluation resolved four EventStore project edges, no EventStore package edge, exact source tag `v3.99.0`, and its serialized AppHost build passed with zero warnings and errors.
- `python3 eng/dependency_graph.py --root . validate --commit 45967719c59d5adbcd8360167d591c71b66b36cd` remains blocked by `FsCheck.Xunit.v3 expected version '3.3.4', found '3.4.0'`. The same command against baseline `08c2ddb5cd914b23fef88794cb7f9a1ff908fca7` returns the identical pre-existing mismatch; it is unrelated to EventStore and was not widened into this change.
- While implementation was running, external commits advanced Builds first to selector commit `569a6e9554b69a5c5e042affb837649e205b5ef8`, then to audit commit `9aca670aa9d4605bb147f641ef23d30d37813e92`; FrontComposer advanced to `45967719c59d5adbcd8360167d591c71b66b36cd`, which commits the spec and selector gitlink. This session performed no commit or push. Builds is clean; FrontComposer's unstaged Builds gitlink now reflects the later audit commit.

## File List

- `references/Hexalith.Builds` -- gitlink advances from `59d6992c6fbe8355f96f3ef5ff50a003ac0a3a94` to `9aca670aa9d4605bb147f641ef23d30d37813e92`; its two commits update `Props/Directory.Packages.props` and `Tools/package-version-audit.json` for EventStore `3.99.0`.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-99-0.md` -- approved scope, completed tasks, and validation evidence.

## Review Triage Log

Three layers ran (blind-hunter, edge-case-hunter, verification-gap) against a diff of superproject commit `45967719` plus Builds `59d6992c..9aca670a`. Verdicts below were rendered after independent verification; reviewer-assigned severities were discarded.

| # | Finding (layer) | Verdict | Evidence |
|---|---|---|---|
| 1 | Gitlink advances to `569a6e95`, not the `9aca670a` the File List claims (blind, edge×3, gap-other) | medium | `git show --stat 45967719` moves `references/Hexalith.Builds` to `569a6e95`, which touches only `Props/Directory.Packages.props`. The File List statement is untrue for the commit this change delivered. |
| 2 | At that pinned commit the audit gate fails closed (blind, edge×2, gap) | medium | At `569a6e95` the catalog canonically hashes to `1a153573…` while the committed audit records `catalogSha256 3659212b…` and 13 rows at `3.98.0`. `validate-package-version-audit.ps1:1267,1432` compares both. Main carried this for 13 commits until `61c05256` advanced the gitlink to `9aca670a`. Already remediated: `pwsh ./Tools/validate-package-version-audit.ps1` on HEAD passes — 286 packages, 141 families, exit 0. |
| 3 | Reviewed artifact mixes committed and worktree state (blind) | low | Accurate description of the review input, not a defect in the change; the spec's own Observed-results paragraph records it. |
| 4 | Builds commit `9aca670a` message fails Conventional Commits (blind) | low | Message is `Implement code changes to enhance functionality and improve performance` — no type, no scope. Committed externally; this session made no commits. Fix requires rewriting shared submodule history. |
| 5 | Whole-catalog `catalogSha256` makes a 1-line selector edit rewrite 702 KB of evidence (blind, edge) | low | `Get-CatalogSha256` hashes the entire catalog, so all 140 families restamp with `reason: tracked catalog declaration bytes changed`. Pre-existing generator design; this change only exercised it. |
| 6 | ~65% of appended `historicalContext` is byte-identical filler (blind) | low | Pre-existing generator design; not caused by this change. |
| 7 | `Tools/package-version-audit.json` has no line-ending policy (blind) | low | `references/Hexalith.Builds/.gitattributes` declares rules for `Props/Directory.Packages.props` (`eol=crlf`) and `test/fixtures/**/*.json` (`eol=lf`), none for the audit artifact. Pre-existing. |
| 8 | `consumerEvidence.repositoryRevision` restamps though no evidence changed (blind) | low | Pre-existing generator behavior; cannot signal stale consumer evidence. |
| 9 | Family `consumerEvidenceSha256` is the SHA-256 of the empty string while asserting consumer discovery (blind) | medium | `hexalith-eventstore` carries `representativeConsumers: []` and `e3b0c442…`, as do 130 of 140 families; discovery is scoped to Builds' own repo so no EventStore consumer can ever appear. Pre-existing design. |
| 10 | Acceptance proves the AppHost's single direct edge and never the transitive closure (blind) | medium | `Directory.Packages.props:4` enables `CentralPackageTransitivePinningEnabled`; `Hexalith.FrontComposer.UI` resolves `EventStore.Client`, `.Contracts`, `.SignalR` with no direct reference. The spec's ACs evaluate only the AppHost. |
| 11 | Nothing binds the selector to the EventStore gitlink identity (blind, edge) | false | `tests/…/Governance/CiGovernanceTests.cs:3732` asserts `HexalithEventStoreVersion` equals the current runtime identity, and `eng/eventstore_runtime_evidence.py` exists. The claim held at this change's baseline; the guard landed afterwards (see DW-1909). |
| 12 | The blocked `dependency_graph.py validate` run was narrated but never filed (blind) | low | No `deferred-work.md` row references this spec. The blocker itself is now resolved — `eng/dependency-graph-policy.json:70` pins `FsCheck.Xunit.v3` at `3.4.0`, matching the catalog. |
| 13 | Verification and structure regress against the 3.98.0 sibling (blind) | low | Accurate: prose bullets replace the sibling's reproducible `set -euo pipefail` assertions, `## Spec Change Log` is empty though the task list was restructured after approval. Fix is an edit to this spec. |
| 14 | Tenants.Aspire 5.5.0 built against EventStore 3.97.0 is unified to 3.99.0 unverified (edge) | false | No Tenants or Memories package in the AppHost graph declares any EventStore dependency; the named consumer does not pull EventStore. The general empty-consumer-evidence point is row 9. |
| 15 | All 140 families restamp so unrelated drift is indistinguishable from the EventStore edit (edge) | low | Same mechanism as row 5. |
| 16 | No FrontComposer verification reads the Builds audit it pins (gap) | medium | `grep -rn "validate-package-version-audit\|validate-central-package-versions" .github/workflows/ eng/` returns nothing. This is precisely what let row 2 land green here while Builds' own CI would have caught it. |
| 17 | No CI lane restores or builds anything in package mode against the selected version (gap) | medium | `Hexalith.FrontComposer.slnx:36-38` sets the AppHost `Build Solution="Release|*" Project="false"`, and it holds the repository's only `Hexalith.EventStore.*` PackageReference. All CI builds Release; the one Debug lane resolves the EventStore source project. An unpublishable selector ships green — the NU1102 class this repo has already hit. |
| 18 | The one catalog-aware gate was red for an unrelated reason and is value-blind anyway (gap) | medium | `HexalithEventStoreVersion` appears only in `selected_catalog_required_property_names`, which checks shape, not value; no EventStore package id appears in the policy's package lists. The FsCheck half is resolved (row 12). |
| 19 | `test-authoritative-package-catalog.ps1` binds 2 of the 13 rows (gap-other) | medium | Lines 71-72 and 91-92 bind only `Hexalith.EventStore.Contracts` and `.Gateway`; the other eleven — including `Hexalith.EventStore.Aspire`, the only one FrontComposer consumes — are unbound, so that test does not establish the 13-row alignment the spec cites it for. |
| 20 | Audit diff carries no unrelated drift (gap-other, confirmation) | false | Not a defect. Independently reproduced: 285/285 package ids preserved, exactly 39 field changes across the 13 EventStore rows, zero disposition changes, zero unrelated `selectedVersion` changes. |

**Routing:** no `intent_gap` and no `bad_spec` entries, so no loopback. No `patch` entries — row 2's code fix is already in main (`61c05256`), and every other surviving entry's fix edits CI, policy, or the Builds submodule, which this spec's frozen Boundaries place under **Ask First**. Rows 11, 14 and 20 are rejected on their refutations; row 13's fix would edit this spec and is rejected on that rule. The remaining entries are deferred below.
