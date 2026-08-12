---
title: 'Bump EventStore Package Family to 3.93.0'
type: 'chore'
created: '2026-08-12'
status: 'done'
baseline_commit: 'efa1c18f4cbc3ea060e169dcdc48e336e5b7e573'
review_loop_iteration: 2
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer's shared Hexalith.Builds catalog still selects the EventStore package family at `3.92.0`, while the user-owned root EventStore checkout already points at exact tag `v3.93.0`. Release/package mode therefore does not use the requested EventStore release, but Story 11.24 normally prohibits this identity change until its broader owner-approved runtime gate is satisfied.

**Approach:** Treat the user's 2026-08-12 approvals as a one-time Story 11.24 waiver solely for selecting EventStore `3.93.0`. Reconcile the regenerated audit against source catalog revision `1711587...`, commit only that audit correction locally in Builds, and select the resulting local Builds commit in FrontComposer. Leave dependency wiring and the EventStore gitlink untouched; do not claim Story 11.24 completion or broader runtime authorization.

## Boundaries & Constraints

**Always:** Limit the waiver to Builds base `1711587...` plus the authorized local audit commit; retain the conditional property default, BOM, CRLF, catalog structure, and all 13 EventStore rows; reconcile refreshed candidate facts with durable family decisions; preserve every pre-existing FrontComposer change and the clean EventStore checkout at `77f34d13b6cce8d906466486f432cd0ed524c9a4` / `v3.93.0`; keep Story 11.24 and its provider/package-hash prerequisites open.

**Ask First:** Any change beyond reconciling and locally committing the Builds audit, selecting that commit in FrontComposer, and this spec; any dependency wiring, policy, source, test, compatibility, provider verification, package hash, Story 11.24 status, push, branch, parent commit, or other submodule-pointer operation.

**Never:** Reuse this waiver for another dependency or claim it supplies owner-approved runtime identity, provider compatibility, or package-hash evidence; add a local package override or inline version; edit or move `references/Hexalith.EventStore`; hand-edit generated selections, weaken validation, accept unrelated catalog changes, push, reset, reformat, stage or commit unrelated files, or overwrite user work.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Catalog advance | Builds catalog selects `3.93.0` | Every EventStore package row evaluates to `3.93.0` through the shared selector | Fail if any family row resolves differently or another catalog value changes |
| Story 11.24 boundary | Explicit one-time user waiver for this package/Builds selection | Package bump proceeds while Story 11.24 remains backlog and its provider/hash gates remain unsatisfied | Reject reuse of the waiver or any claim of broader runtime approval |
| Audit refresh | Complete catalog plus live configured NuGet sources | Generated audit records accepted evidence for every actual catalog selection, including EventStore `3.93.0` | Fail closed on unresolved sources, missing selections, family splits, or unreviewed catalog changes |
| Release consumption | FrontComposer AppHost uses NuGet dependencies | `Hexalith.EventStore.Aspire` restores at exact version `3.93.0` with no EventStore project fallback | Report the exact restore/build failure; do not fall back to source or alter wiring |
| Source consumption | FrontComposer AppHost uses project references | Debug mode consumes the preserved root EventStore checkout at exact tag `v3.93.0` | Stop if the checkout identity changes or source compilation exposes incompatibility |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52`, `Tools/package-version-audit.json` -- base `1711587ed7969016e6c99dfa1c0fa28b7889f29b` selects the 13-package EventStore `3.93.0` family; local audit-only commit `5d268c6b00938070c4f8bb6e9d0156c9a4539eb6` reconciles provenance and refreshed family evidence without changing selections.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1:430-472` -- provenance binds HEAD; generation rewrites family-decision prose, which must remain durable.
- `Directory.Build.props:12-26`, `deps.*.props`, AppHost project lines 11-39 -- unchanged source/package edges.
- `_bmad-output/implementation-artifacts/spec-11-24-adopt-the-owner-approved-eventstore-runtime-identity.md:17-31,56-70` -- read-only broader gate; this waiver does not close it.
- `references/Hexalith.EventStore` -- read-only checkout at `77f34d...` / `v3.93.0`.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds` -- reconcile stale latest-version prose with refreshed candidates without changing selections or durable constraints; commit only the audit locally.
- [x] FrontComposer -- select the authorized local Builds commit while preserving all other work and the EventStore gitlink.
- [x] Validation -- prove catalog/audit integrity, Release package `3.93.0` without source fallback, Debug source identity, and the open Story 11.24 boundary.

**Acceptance Criteria:**
- Given the selected local Builds commit, when a clean checkout and validators are inspected, then all EventStore rows resolve to `3.93.0`, audit provenance names source revision `1711587...`, and family decisions match refreshed evidence without losing durable constraints.
- Given Release and Debug modes, when the AppHost restores/builds, then Release uses the exact Aspire package with no EventStore project edge and Debug retains root tag `v3.93.0`.
- Given the one-time waiver, when work completes, then Story 11.24 remains backlog and unrelated dirty files remain unchanged.

## Spec Change Log

- Scope expansion, 2026-08-12 -- the user approved the complete audit refresh required by deterministic validation.
- Concurrent-state reconciliation, 2026-08-12 -- the user approved retaining external pushed commit `1711587ed7969016e6c99dfa1c0fa28b7889f29b`, which contains the requested catalog and initial audit changes, as the Builds base.
- Review-loop authorization, 2026-08-12 -- the user approved reconciling stale audit prose, creating a local Builds commit containing the correction, and updating FrontComposer's Builds gitlink to it. Push remains prohibited.
- Delivery authorization, 2026-08-12 -- after workflow completion, the user approved pushing the Builds commit and preparing a scoped FrontComposer commit and pull request. Direct push to FrontComposer `main` remains excluded.

## Verification

**Observed commands and results (2026-08-12):**
- In `references/Hexalith.Builds`, `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1`, `test-authoritative-package-catalog.ps1`, `validate-package-version-audit.ps1`, `test-package-version-audit-generator.ps1`, and `test-package-version-audit-validator.ps1` all exited `0`; the reported scopes were 284 catalog entries, 49 authoritative identities/3 shared versions, 284 audit packages/139 families/1 source, and 14 generator scenarios.
- A `jq -e` assertion over `Tools/package-version-audit.json` exited `0`: provenance is exactly `1711587ed7969016e6c99dfa1c0fa28b7889f29b`; exactly 13 `Hexalith.EventStore*` rows are listed with audited, selected, and latest-stable version `3.93.0`; and the five reviewed family decisions match the refreshed AngleSharp `1.7.1`, bUnit `2.9.0`, Fluent rc.5, Dapr beta.706, SourceLink `10.0.400`, and Sonar `10.32.0.713` facts without accepting them or losing their durable constraints.
- `printf '%s\n' 'build(audit): reconcile package family decisions' | ./node_modules/.bin/commitlint --verbose` and `./node_modules/.bin/commitlint --last --verbose` both found zero problems. `git commit --amend --no-edit` produced `5d268c6b00938070c4f8bb6e9d0156c9a4539eb6`; its parent is exactly `1711587ed7969016e6c99dfa1c0fa28b7889f29b`, it is the only commit above that base, and `git diff-tree --no-commit-id --name-only -r HEAD` reports only `Tools/package-version-audit.json`.
- Release/package mode used the fresh cache `/tmp/frontcomposer-eventstore-393-review-release-NHDHiOKI` with `dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --packages /tmp/frontcomposer-eventstore-393-review-release-NHDHiOKI --no-cache --force -p:Configuration=Release -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false`. MSBuild evaluation reported `HexalithEventStoreVersion=3.93.0`, `HexalithEventStoreFromSource=false`, one `Hexalith.EventStore.Aspire` package edge, and zero EventStore project edges. The isolated assets contained only `Hexalith.EventStore.Aspire/3.93.0`; `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Release --no-restore -p:UseNuGetDeps=true -p:UseHexalithProjectReferences=false -p:BuildProjectReferences=false -p:RestorePackagesPath=/tmp/frontcomposer-eventstore-393-review-release-NHDHiOKI` succeeded with 0 warnings and 0 errors.
- Debug/source mode used the fresh cache `/tmp/frontcomposer-eventstore-393-review-debug-vLHXq243` with `dotnet restore src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --packages /tmp/frontcomposer-eventstore-393-review-debug-vLHXq243 --no-cache --force -p:Configuration=Debug -p:UseNuGetDeps=false -p:UseHexalithProjectReferences=true`. MSBuild evaluation reported source `true`, no EventStore package edge, and the root EventStore project edges. `dotnet build src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj --configuration Debug --no-restore -m:1 -v:quiet --tl:off -p:UseNuGetDeps=false -p:UseHexalithProjectReferences=true -p:RestorePackagesPath=/tmp/frontcomposer-eventstore-393-review-debug-vLHXq243` succeeded with 0 warnings and 0 errors; the clean root checkout remained `77f34d13b6cce8d906466486f432cd0ed524c9a4`, exact tag `v3.93.0`.
- Story 11.24 remained `status: 'draft'` with sprint status `backlog`. `git diff --check`, the Builds `HEAD^..HEAD` check, audit-only scope assertions, and catalog byte checks passed; the catalog retained UTF-8 BOM, CRLF-only line endings, and final CRLF.
- Preserved-file SHA-256 values remained: row-identity contract `944fcd08936beb4e709b871208eabc085dcb1798d5e07dc6e013345f5203a226`; sprint status `e6d7ee2c00d020a6b7f36c17c23fe77b0c55dab759e877b8009fab00c0c8390f`; Story 11.24 spec `c6e906f5b1f1d9da374657dc6274f1ad0527e07cfc7be35492f3a6e47b324d63`; planning architecture `a14b1ff92977ef396ec8b6807c53c875baf3bc5220cd384616e7a92f92d99eb0`; epics `bc287aab921bc35324ab746bb440d4ef7bc13589dd17768611ff3944cf8bf133`; PRD `8fb6cff9824604b38e06fa33ad7fcbc70ad7a8ae836b1d774fafece3cf2fb055`; UX design `23acac1fc1fe14e903e70bcf0118b81540cfb7b3193971b25471e847d089a38c`; UX experience `0b611aaeb7791b7fbcfb6e914fd00d108f06836790216a515e4dba1779d5d4b4`; project-docs architecture `35244de276f58a620cd906d461ea4942083130f3663cb809a8c6443252d3a53b`; DataGrid reference `ca847823c5d867ca1cbcc65d6920b33e46452caf20aafdee5355abf8f59e51bb`; sprint-change proposal `de4a7d56928b70233236a2faf90133fd316eaadedd711b0c6142591daedff85d`; Builds catalog `12d178218f715bb2268bd4eb7821f710d4df435e59abdc71fbc85fae9fd90ff1`.
- Builds push verification passed: `origin/main` resolves to audit-only commit `5d268c6b00938070c4f8bb6e9d0156c9a4539eb6`, whose parent is catalog revision `1711587ed7969016e6c99dfa1c0fa28b7889f29b`. The scoped FrontComposer delivery includes only that Builds gitlink, this completed spec, and its three review-derived deferred-work entries; it does not directly push FrontComposer `main`.

## Suggested Review Order

**Package selection**

- The shared selector advances the complete EventStore family to 3.93.0.
  [`Directory.Packages.props:8`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L8)

- All thirteen package rows continue using the single family property.
  [`Directory.Packages.props:40`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L40)

**Audit evidence**

- Provenance identifies the exact pushed catalog revision audited before the local evidence commit.
  [`package-version-audit.json:4`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L4)

- The waiver remains FrontComposer-specific and explicitly excludes broader runtime authorization.
  [`package-version-audit.json:255`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L255)

- Refreshed candidate facts preserve the existing bUnit and Fluent rollback constraints.
  [`package-version-audit.json:48`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L48)

- Dapr, SourceLink, and analyzer candidates remain unaccepted pending their compatibility gates.
  [`package-version-audit.json:884`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L884)

- Every EventStore audit row records selected, audited, and latest stable 3.93.0.
  [`package-version-audit.json:4268`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L4268)

**Scope and verification**

- The approved one-time waiver keeps Story 11.24 and its broader gates open.
  [`spec-bump-eventstore-package-to-3-93-0.md:18`](spec-bump-eventstore-package-to-3-93-0.md#L18)

- Observed Release, Debug, audit, identity, and preservation evidence is recorded together.
  [`spec-bump-eventstore-package-to-3-93-0.md:68`](spec-bump-eventstore-package-to-3-93-0.md#L68)

- Pre-existing audit and CI hardening gaps are explicitly deferred.
  [`deferred-work.md:2318`](deferred-work.md#L2318)
