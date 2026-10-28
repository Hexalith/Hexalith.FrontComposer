---
title: 'Story 13.6: Correct Parties Shared Catalog Inheritance'
type: 'bugfix'
created: '2026-10-28'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '29f865e9a663d05179e0f23cb5f87f0feec858b3'
context:
  - '{project-root}/AGENTS.md'
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Current committed graph validation fails because Parties declares `Hexalith.FrontComposer.Contracts.UI` outside its selected Builds catalog. Removing the declaration alone breaks its three NuGet UI consumers.

**Approach:** Add the missing shared entry at the existing family version, consume an immutable catalog successor in Parties, remove its local shim, and update the root selection/seals through delayed activation. The user approved the prepared correction, required local dependency commits and pin/seal updates on 2026-10-28.

## Boundaries & Constraints

**Always:** Preserve current package versions, SDKs, source/runtime behavior, all other root selections, the exact Platform source/inventory/trust boundary and AD-12. Start Builds from exact Parties-selected `abc0e1be8069d1c7a567515a13bb015adc407536` in an isolated owning worktree so unrelated newer catalog changes are not imported. Keep root Builds `81ab049208678bd6616828824729c0ddac02f7d1`. Use the existing `$(HexalithFrontComposerVersion)` (4.7.1 at that catalog). Validate every exact full commit message with the owning pinned CLI/config before use; retain evidence outside the checkout. The approved scope includes necessary isolated worktrees/branches and local commits for the catalog, Parties successor, root pointer/seal update and later activation record. Preserve both existing modified parent story documents and their frozen baseline/manual waiver/status.

**Never:** Push, publish, acquire unknown sources, initialize nested submodules, change package versions/other pins, loosen policy or invent approval. Update the Parties nested Builds gitlink as committed metadata only. Do not claim the historical runtime/hosted proofs executed the new candidate. Parent Story 13.6 remains in-progress for its separate accessibility requirements.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Shared package | New catalog plus Parties NuGet consumers | Exactly one inherited Contracts.UI entry at unchanged family version; standalone Release restore/build succeeds | Preserve first failure and use focused fallback without weakening gates |
| Immutable graph | Updated Parties and root selector seal | Current committed graph validates with full raw inventory and unchanged trust | Diagnose every further refusal; do not silently amend unrelated contracts |
| Delayed activation | Pointer/policy amendment followed by later base | Earlier transition remains separately attributed; later activation uses committed base policy and passes | Never substitute candidate policy for event-base authority |
| Drift | Existing projection/semantic negative fixtures | Changed selectors, extra consumption, unknown identities and forged evidence still refuse | Run full relevant engine/consumer suites |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props`: catalog owner; exact one-row patch is `/tmp/fc-story13-6-catalog-proposal-9mzn3fr6/builds.patch`. The isolated worktree remains backed by the root-declared Builds repository/object store.
- `references/Hexalith.Parties/Directory.Packages.props`: remove the exact local ItemGroup/comment using sibling `parties.patch`; selected revision is `9239c531f3b0cbb4a998c419e537463b597ff14b`. Its `.gitmodules`/tree select the old Builds commit. No nested checkout is necessary.
- `eng/dependency_graph.py`: reuse exact-object collection, canonical digest, `materialize_build_checkout`, semantic validation and CLI transition logic. No new validator primitive is proposed.
- `eng/dependency-graph-policy.json`: only the necessary root selector seal may change; keep source/configuration seals unchanged unless measured committed bytes establish an existing normalization discrepancy, which must be reported before any scope expansion.
- `tests/eng/test_dependency_graph.py`, `test_dependency_handoff.py`, `test_release_evidence.py`: existing projection/semantic/refusal consumers.
- `Hexalith.Parties.Standalone.slnx`, closed `module_build_registry`: authoritative isolated Release/NuGet commands and edge-bound Builds materialization.

## Tasks & Acceptance

**Execution:**

- [x] Inspect owning guidance/configuration and preserve initial dirty documents; apply only the two reviewed catalog/shim edits and required gitlinks/seal.
- [x] Create and retain exact Builds and Parties successors with pinned commitlint evidence; verify all pre-existing catalog values and product source bytes remain unchanged.
- [x] Commit root Parties pointer plus necessary selector seal separately from submodule content; keep original parent documents unstaged. Record expected transition failures truthfully.
- [x] Validate current committed objects, affected standalone contract, dependency engine/handoff/release consumers, source dependency-mode suite and focused Debug AppHost/Shell governance. Use baseline fallback ladder for environmental blockers.
- [x] Record exact commands/counts/revisions and limits in this bounded spec; leave its final activation commit to the parent after implementation audit and before formal review. Update parent story/result with the final disposition while preserving their existing edits.
- [x] Complete the owning Builds README-required audit validation: preserve the prior audit JSON from the isolated owning Builds worktree, use its documented revision-bound incremental refresh for only `hexalith-frontcomposer`, preserve all actual package selections and unrelated family evidence, validate owner catalog/consumer/exception/audit gates, commit the audit metadata successor, and advance only the already approved dependent Parties/root selectors and seal. Retain earlier failures and exact revision attribution; no source/version/policy relaxation.

**Acceptance Criteria:**

- Given exact new catalog/Parties revisions, when package-mode restore/build runs, then Contracts.UI resolves centrally and all existing versions are preserved.
- Given the root pointer/seal amendment and a later activation revision, when current-object and committed-base transition gates run, then both pass without policy substitution or broadened trust.
- Given independent parent audit/review, when this bounded task closes, then original parent acceptance, manual waiver, historical failures and unperformed platform/owner gates remain intact.

## Implementation Notes

Implementation completed locally on 2026-10-28; final activation and independent acceptance remain parent-owned.

| Owner | Exact parent | Exact successor | Committed change |
|---|---|---|---|
| Builds | `abc0e1be8069d1c7a567515a13bb015adc407536` | `94d38129932f750a46adb7a0f6bb0b25b3415b23` | One Contracts.UI catalog row using the unchanged family property. |
| Parties | `9239c531f3b0cbb4a998c419e537463b597ff14b` | `c4fae6120af10b9f139b2e0a564988ff331aebd4` | Remove the five shim lines; update nested Builds gitlink as metadata only. |
| FrontComposer | `29f865e9a663d05179e0f23cb5f87f0feec858b3` | `c3f217ee7216f998875b23a4d6b8175280e8a86a` | Root Parties pointer and root selector seal only; original dirty parent documents remained unstaged. |

Retained owning worktrees/branches are `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv/builds` (`fix/parties-contracts-ui-catalog`) and sibling `parties` (`fix/shared-contracts-ui-catalog`). Root Builds remains `81ab049208678bd6616828824729c0ddac02f7d1` on its original main checkout. The root Parties checkout selects the retained successor; its nested Builds checkout was never initialized.

The sole policy edit is the root selector seal, `3bda30ee17afc853c07e416498ab1cde0b05bc5ad73d640680f6524415dc8ec6` → `d06ccbc8efb2ac9b0a60a3ad4ada705c907f4c3c931ebd53582a1b1796588077`. The existing committed configuration seal already matches `68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca`; no normalization discrepancy or extra seal edit was needed. All other policy fields, root selections, Platform source/inventory/trust and AD-12 remain unchanged.

Byte assertions prove the Builds successor differs by exactly the reviewed added line; every pre-existing catalog byte and version is preserved. Parties differs only by the exact five-line removal and nested gitlink. Its three package consumers (UI, AdminPortal, ConsumerPortal) each resolve Contracts.UI **4.7.1** in restored assets. The exact new catalog SHA-256 is `367c7b8864e4bc4361e1f3f370307b0200806f1dc18eb7a065485c0347df022b`. Engine helpers materialized the isolated exact Parties checkout and 2,931 regular Builds files / 58,804,619 bytes, with no nested repository initialization.

All full commit messages passed the owning committed configuration and lock-pinned CLI before use: Builds CLI/config-conventional 21.2.4, Parties/root 21.2.2. Logs and exact message files are outside the checkout at `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv`; lock/installed-version and configuration-hash evidence is `final-scope-and-resolution.json`.

No product source, runtime behavior, SDK or package version changed. No topology was started and no hosted, accessibility, platform-owner or release gate was executed. Earlier runtime/hosted results remain evidence only for their original inputs. Both parent documents retain their full initial byte prefixes, frozen acceptance/baseline, manual waiver and in-progress status; this correction adds a separately attributed disposition.

### Mandatory owning audit completion discovered by parent audit — 2026-10-28

The owning Builds README requires catalog/consumer/exception validators before integration. At catalog successor `94d38129932f750a46adb7a0f6bb0b25b3415b23`, central catalog (308 entries), authoritative catalog (50 identities/three family versions), Builds consumer authority (22 projects) and exception alignment (16 exceptions) pass. Its existing audit correctly refuses the changed catalog hash and missing Contracts.UI evidence. The approved catalog correction therefore requires its normal derived metadata refresh; this preserves the approved functional patches and every actual package version. The parent directs the documented incremental refresh of only the existing FrontComposer family and the dependent local revision/seal advances already authorized by the user. This is an implementation-completeness correction under the existing approval, not additional package/version/trust scope. The frozen intent is unchanged.

## Spec Change Log

- 2026-10-28: Implemented the approved exact catalog/Parties successors and root selector amendment. Frozen intent is unchanged; independent audit/review and later activation remain parent-owned.

## Review Triage Log

## Verification

Evidence root: `/tmp/fc-story13-6-catalog-implementation-ipz1i_kv`. `results.jsonl` preserves full argv, cwd, exit code, elapsed time and log path for each recorded command. All commands below run from the root unless an owning directory is named.

| Exact command | Result |
|---|---|
| `python3 eng/dependency_graph.py --root . validate --commit 29f865e9a663d05179e0f23cb5f87f0feec858b3` | Exit 1 retained: Parties local shim violates catalog inheritance. |
| `python3 eng/dependency_graph.py --root . validate --commit c3f217ee7216f998875b23a4d6b8175280e8a86a` | Exit 0: eight selectors; all 69 raw edges / all 17 Platform edges preserved. Graph digest `075e81104cf0a9b06b06f0a40697a89083963ff6438ca7c57bc6164e1bbde2d2`. |
| `python3 eng/dependency_graph.py --root . diff --event push --event-base 29f865e9a663d05179e0f23cb5f87f0feec858b3 --candidate c3f217ee7216f998875b23a4d6b8175280e8a86a` | Exit 1 retained: old-base root-selector seal refuses the amendment. No candidate-policy substitution. |
| `dotnet restore Hexalith.Parties.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true` in isolated `standalone` | Exit 0 using the exact registry argv and new edge-bound catalog. |
| `dotnet build Hexalith.Parties.Standalone.slnx --configuration Release --no-restore -p:UseNuGetDeps=true` in isolated `standalone` | Exit 0; zero warnings/errors. |
| `python3 tests/eng/test_dependency_graph.py` | 130/130 pass. |
| `python3 tests/eng/test_dependency_handoff.py` | 11/11 pass. |
| `python3 tests/eng/test_release_evidence_v2.py` | 15/15 pass; this is the existing release consumer's actual filename. |
| `python3 tests/eng/test_apphost_dependency_modes.py` | 13/13 pass. |
| `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore` | First exit 1 retained: NETSDK1004 for unchanged Builds.Tooling assets (21 warnings). |
| `dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj -p:Configuration=Debug` | Exit 0, including the missing Tooling prerequisite. |
| Same AppHost build after restore | Exit 1 retained: MSB4018 file-sharing failure reading Shell output (five warnings). |
| `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -m:1` | Exit 0; zero warnings/errors. |
| `dotnet build tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Debug --no-restore -m:1` | Initial exit 0 with four assembly-version conflict warnings; retained separately. |
| `dotnet restore tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj -p:Configuration=Debug` then the same Shell build | Both exit 0; final build has zero warnings/errors. |
| `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*FrontComposerUiAppHostTests'` | 56/56 pass; zero skipped/not run. |
| `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*InfrastructureGovernanceTests'` | 20/20 pass; zero skipped/not run, including committed-object central-catalog governance. |

`materialize-and-check.py` and `final-scope.py` retain executable invariant/asset checks; their successful receipts are `invariants.json` and `final-scope-and-resolution.json`. An initial scope comparison used the current seal for the historical root and refused; the corrected comparison independently loads each root's committed policy and asserts unchanged raw Platform rows, rather than treating that refusal as permission to substitute policy.

The prepared LF Parties patch initially failed against its committed CRLF file; the identical five reviewed lines were removed by asserted byte replacement preserving all other bytes. An intervening empty commit attempt refused and created no commit (`parties-commit.log`). The successful Parties commit is separately recorded. No gates or warnings were suppressed and no source/configuration/package change was used to repair environmental validation failures.

Matrix coverage: shared inheritance is covered by the exact standalone restore/build and three restored-asset assertions; immutable inventory/trust by current-object validation and exact scope comparisons; old-base refusal plus the engine's delayed-policy tests cover AD-12 until the parent's later activation; drift/forgery refusals by the full engine/handoff/release consumer suites. The later activation revision, committed-base transition pass, independent audit/review and final disposition remain to be recorded by the parent before bounded closure. Parent Story 13.6 stays in-progress for its independent accessibility requirements.

Use existing `eng/dependency_graph.py validate/diff` on full canonical revisions; run individual affected test/build commands from their owning repositories. Preserve full argv, exit codes and logs in external scratch. Run `python3 eng/validate-story-artifacts.py` and `git diff --check` after documentation changes. No new full topology is required merely for catalog bookkeeping if product code/effective version equality is verified; report the exact scope of all reused evidence.


### Additional owning Builds integration checks — 2026-10-28

The parent audit identified README:208's additional catalog/consumer/exception checks. On exact Builds successor `94d38129932f750a46adb7a0f6bb0b25b3415b23`, all commands below ran from its retained owning worktree with unchanged tracked bytes:

| Exact command | Exit/result |
|---|---|
| `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1` | 0; 308 entries. |
| `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` | 0; 50 protected identities / three shared versions. |
| `pwsh -NoProfile -File ./Tools/validate-consumer-package-authority.ps1 -RepositoryRoot . -CatalogPath ./Props/Directory.Packages.props` | 0; 22 projects. This is the actual documented consumer validator filename. |
| `pwsh -NoProfile -File ./Tools/validate-package-version-exceptions.ps1 -InventoryPath ./Tools/package-version-exceptions.json -CatalogPath ./Props/Directory.Packages.props` | 0; 16 allowlisted exceptions. |
| `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` | **1**; exactly two refusals: audit catalogSha256 differs from the evaluated catalog, and Contracts.UI has no audit evidence. |

The necessary correction is the documented revision-bound audit refresh of existing `hexalith-frontcomposer`, preserving the prior audit, historical observations, every selected version and all other families. It requires a separately reviewed metadata successor and propagation of that exact catalog selection through Parties/root seals; this was not silently added to the approved two-edit patch. No audit generation, feed discovery, metadata edit or additional commit was performed at this checkpoint. Functional package/source checks pass, but owning integration readiness and bounded closure remain blocked by the stale audit. Full output is retained in `builds-version-audit.log` and the other `builds-*.log` receipts.

Artifact verification: `python3 eng/validate-story-artifacts.py` and `git diff --check` passed. The bounded `--story ... --base 29f865e9a663d05179e0f23cb5f87f0feec858b3` check initially refused the missing File List; it passes after recording the five root-owned paths below. No validator rule changed.


### Derived audit metadata closure and final implementation selections — 2026-10-28

The parent's completeness correction is now implemented; the earlier catalog/audit failures above retain their original revision attribution.

| Owner | Exact earlier revision | Exact final implementation revision | Delta |
|---|---|---|---|
| Builds | `94d38129932f750a46adb7a0f6bb0b25b3415b23` | `326af3d248974b5f3849ff667e97a5d61e3e3f30` | Derived audit metadata only; raw catalog bytes are identical. |
| Parties | `c4fae6120af10b9f139b2e0a564988ff331aebd4` | `492a8cc1417c625cdfb7fd4c946686ef88adab9d` | Nested Builds gitlink metadata only. |
| FrontComposer | `c3f217ee7216f998875b23a4d6b8175280e8a86a` | `6a41a075e1f0512b8acaa80368974b92c4bbe1c3` | Root Parties pointer and necessary root selector seal only. |

The final selector seal is `3b3afd2ec762f34bbe70c029f19ef962bd384098618c6a40e95e72a308e3ae73`. Root Builds remains `81ab049208678bd6616828824729c0ddac02f7d1`; the committed configuration seal remains `68386ed899cdef31131e081c321424fa18af8864b8d8efcd6f59eae99a870fca`. Exact scope assertions show every other policy field/root selection, all 17 raw Platform edges and all product source/SDK bytes remain unchanged.

The prior audit is preserved externally as `package-version-audit.before-refresh.json`. The only configured source was NuGet.org. From exact catalog commit `94d38129932f750a46adb7a0f6bb0b25b3415b23`, the documented command `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath /tmp/fc-story13-6-catalog-implementation-ipz1i_kv/package-version-audit.before-refresh.json -ChangedFamily hexalith-frontcomposer -OutputPath ./Tools/package-version-audit.json` succeeded in incremental mode: **one refreshed / 150 preserved families**. Assertions in `audit-preservation.json` prove all **307 existing selected versions**, **302 unrelated package rows** and **150 unrelated family decisions** are identical. The only added package is Contracts.UI/4.7.1; the audit binds the unchanged raw catalog SHA-256 `367c7b8864e4bc4361e1f3f370307b0200806f1dc18eb7a065485c0347df022b` and its exact committed source revision.

All five owning real validators were rerun successfully after the metadata commit: central catalog **308 entries**, authoritative catalog **50 identities / three shared versions**, Builds-self consumer authority **22 projects**, exception alignment **16 entries**, and package audit **308 packages / 151 families / one source**. Their exact argv are unchanged from the owner-check table above; final outputs are the five `final-builds-*.log` receipts. The documented generator fixtures pass **115 scenarios** and validator fixtures pass **103 scenarios**. The latter ran for 696.458 seconds, beginning before the metadata commit; its validator/catalog/consumer source bytes and refreshed audit bytes remained unchanged while the parent permitted metadata-only commit/pointer progress. Its complete result is retained, with no pending fixture or suppressed failure. All three derived commit messages passed their owning pinned CLI/config before use (`builds-audit-commitlint.log`, `parties-audited-commitlint.log`, `root-audited-commitlint.log`).

Final exact-object verification commands:

- `python3 eng/dependency_graph.py --root . validate --commit 6a41a075e1f0512b8acaa80368974b92c4bbe1c3` — exit 0; **eight selectors / 69 raw edges / 17 unchanged Platform edges**. Graph digest `6e003865423b143ce94ba3ec1ca72876bf93a2527344441dfb8e50aa336812ac`.
- `python3 eng/dependency_graph.py --root . diff --event push --event-base c3f217ee7216f998875b23a4d6b8175280e8a86a --candidate 6a41a075e1f0512b8acaa80368974b92c4bbe1c3` — exit 1 retained: the intermediate old-base selector seal correctly refuses the derived pointer amendment. The parent still owns later-base activation; no candidate-policy substitution occurred.
- From exact isolated `standalone-audited`, the same registry Release/NuGet restore/build argv above both exit 0, with zero build warnings/errors. Engine helpers materialized **2,931 regular files / 58,818,286 bytes**, without nested repository initialization.
- `python3 /tmp/fc-story13-6-catalog-implementation-ipz1i_kv/final-audited-scope.py` — exit 0; final graph/policy scope and all three restored Contracts.UI/4.7.1 consumers pass (`audited-final-scope-and-resolution.json`).
- `dotnet tests/Hexalith.FrontComposer.Shell.Tests/bin/Debug/net10.0/Hexalith.FrontComposer.Shell.Tests.dll -class '*InfrastructureGovernanceTests'` — **20/20**, zero skipped/not run, consuming the final committed root.

The earlier engine **130**, handoff **11**, release consumer **15**, dependency-mode **13**, AppHost **56**, and Debug build results remain attributed to the initial functional selection. The derived changes add only required audit data and immutable selectors; product source, selected versions, SDKs, validator/test code and configuration are unchanged. Those earlier checks were not relabelled as rerun on the derived revisions. No full topology or hosted/runtime proof was repeated.

**Separately retained Parties authority limit:** the additional generic owner validator was invoked against the tracked Parties worktree. The first call with an external catalog path refused import-location mismatch plus the pre-existing `HexalithEventStoreVersion` override (`parties-consumer-authority.log`). After exact regular-file contract materialization, `pwsh -NoProfile -File ./references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1 -RepositoryRoot . -CatalogPath ./references/Hexalith.Builds/Props/Directory.Packages.props` refuses exactly the unchanged EventStore version override (`parties-consumer-authority-materialized.log`). The user-selected 3.128.3 declaration predates this fix and is preserved. This limit is distinct from passing Builds-self authority, FrontComposer's approved semantic profile and the final exact standalone package build. No full Parties authority/CI acceptance is claimed or policy relaxed.

Implementation handoff is ready for parent audit, later activation and formal review. All initial parent-document bytes/frozen baseline/manual-waiver/status remain preserved; independent accessibility/platform/owner/release gates remain open. No push or publication occurred.

## Parent implementation audit and activation preparation — 2026-10-28

The parent read the entire baseline-root/untracked/owning-repository diff, independently recomputed the final selector seal from committed Gitlinks/mappings, and confirmed that only Parties selection and that policy field changed. Independent audit comparison preserves all 307 previous selected versions, 302 unrelated package rows and 150 unrelated family decisions. Both initial parent-document byte prefixes, frozen acceptance, checkboxes and status remain intact. Receipts are in `/tmp/fc-story13-6-parent-catalog-audit-alktsxqp/`; the complete reviewed diff is `/tmp/fc-story13-6-review-iptb7vt0.diff`.

This record supplies the separately authorized later local activation revision after implementation root `6a41a075e1f0512b8acaa80368974b92c4bbe1c3`. Its actual canonical identifier and committed-base validation will be recorded after creation; no transition pass is inferred in advance. Only this bounded spec is staged; both existing parent documents stay unstaged. Current local tracking `origin/main` is `c3f217ee7216f998875b23a4d6b8175280e8a86a`; its local reflog records a push update during this work. This workflow performed no push or hosted query, and local activation will not be presented as hosted qualification.

## File List

- `eng/dependency-graph-policy.json` — Amend only the root selector seal for the approved Parties successor.
- `references/Hexalith.Parties` — Advance the root gitlink to the exact local Parties successor.
- `_bmad-output/implementation-artifacts/spec-13-6-correct-parties-shared-catalog-inheritance.md` — Bounded correction specification, implementation and verification records.
- `_bmad-output/implementation-artifacts/13-6-accessibility-result.md` — Append the separately attributed correction disposition, preserving existing edits.
- `_bmad-output/implementation-artifacts/spec-13-6-verify-responsive-and-assistive-accessibility.md` — Append the corresponding parent-story disposition, preserving original acceptance and status.
