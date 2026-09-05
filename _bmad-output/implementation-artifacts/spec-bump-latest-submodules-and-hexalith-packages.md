---
title: 'Bump Latest Root Submodules and Hexalith Packages'
type: 'refactor'
created: '2026-09-05'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1a7edded603cd557a97dda1277e5ae3101fbec4d'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root EventStore and Memories gitlinks trail (or dirty-ahead of) `origin/main`, while the Builds catalog still selects EventStore `3.101.0` although nuget.org lists aligned stable `3.102.0` on all 13 EventStore packages. Other Hexalith families and six root submodules are already current. EventStore runtime-identity compatibility pins still name `3.101.0` / older SHAs.

**Approach:** Fast-forward in-scope root submodule gitlinks to the chosen EventStore identity and Memories `origin/main`, bump only the EventStore family in Builds to `3.102.0` with matching audit evidence, land the Builds catalog commit then FrontComposer Builds gitlink, refresh current-compatibility identity pins/evidence, and leave unrelated working-tree edits untouched.

## Boundaries & Constraints

**Always:** Re-resolve remote tips and nuget.org listings before editing. Keep all 13 EventStore rows on one `HexalithEventStoreVersion`. Edit catalog authority only in `references/Hexalith.Builds/Props/Directory.Packages.props` and refresh `Tools/package-version-audit.json` via the Builds audit scripts. Preserve UTF-8 BOM and CRLF on Builds catalog files. Use `git -c submodule.recurse=false submodule update --init` (never `--recursive` / `--remote`). Leave CI/Release Builds execution SHAs and `evaluator_authorizations` unchanged for this catalog-only advance. Preserve unrelated dirty paths (`CommandFormEmitterTests.cs`, `spec-dw-683-…`, other untracked work). HALT for a human to create/push the Builds catalog commit before moving FrontComposer’s Builds gitlink.

**Ask First:** Staging/committing/pushing FrontComposer or Builds; advancing any gitlink other than EventStore, Memories, and Builds; changing non-EventStore `Hexalith*Version` pins; claiming Story 11.24 migration approval; moving Builds execution pins.

**Never:** Add FrontComposer-local `PackageVersion` / `Hexalith*Version` overrides; initialize nested submodules; downgrade or move stable→prerelease; rewrite immutable historical identity capture; overwrite unrelated dirty files; auto-push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| EventStore packages | Catalog `3.101.0`; nuget.org stable `3.102.0` on all 13 | `HexalithEventStoreVersion` + 13 rows + audit → `3.102.0` / listed / retained | Halt if any family member unpublished or split |
| Already-latest families | Commons `2.30.0`, Polymorphic `1.19.2`, Memories `2.22.1`, Tenants `5.6.0`, Parties `1.0.0`, FrontComposer `4.2.0` | Pins unchanged | N/A |
| Memories gitlink | Committed `7df55c12…`; tip `3a7a7025…` | Parent gitlink = Memories `origin/main` | Halt on non-fast-forward / divergent dirty submodule |
| EventStore source | Committed `4ae9cee1…` = `v3.102.0`; dirty tip `89564e0c…` | Parent gitlink = Open Question decision | Halt if chosen SHA missing after fetch |
| Runtime identity | Contract/tests still `3.101.0` / `f152995…` / `7e84ff1…` | `currentCompatibility` + `CiGovernanceTests` match landed EventStore source SHA, `3.102.0`, and new Builds catalog SHA | Do not edit historicalCapture / approved tuples |
| Unrelated dirty tree | Spec DW-683 + CommandFormEmitterTests edits present | Left untouched; only stage in-scope paths | Do not clean/revert |

</frozen-after-approval>

## Open Questions

- **EventStore source identity** — options: **A)** keep exact tag `v3.102.0` / `4ae9cee1e9abe050402fd1405a9abd54892ba13f` so Debug source matches Release package `3.102.0` (committed gitlink already there; discard dirty tip checkout) / **B)** advance to Memories-style latest tip `89564e0c290f4bc32ac7ebdb7d33802ff6d5e9d5` (`v3.102.0-6`) so submodule is newest main, accepting Debug≠Release package divergence until a later EventStore release.

## Code Map

- `.gitmodules` -- eight root `references/` submodules on `branch = main`; only these may be initialized.
- `references/Hexalith.EventStore` -- committed gitlink `4ae9cee1…` = exact `v3.102.0`; dirty worktree at `89564e0c…` (6 commits past tag). Choice in Open Questions.
- `references/Hexalith.Memories` -- committed `7df55c12…`; tip `3a7a7025…` (28 commits past `v2.22.1`). Package family already `2.22.1` on nuget.org — gitlink-only.
- `references/Hexalith.Builds` -- tip/gitlink `e0e06946…` (current). Catalog `HexalithEventStoreVersion=3.101.0`; prior audit commit `7e84ff1…`. Needs new catalog+audit commit for `3.102.0`, then FrontComposer gitlink bump to that SHA.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- family selectors + 13 EventStore `PackageVersion` rows; other Hexalith families already match nuget.org.
- `references/Hexalith.Builds/Tools/{audit-central-package-versions,validate-central-package-versions,validate-package-version-audit,test-authoritative-package-catalog}.ps1` -- live NuGet V3 audit + gates; reuse with `-Family hexalith-eventstore` when regenerating.
- `Directory.Packages.props`, `Directory.Build.props`, `deps.local.props`, `deps.nuget.props` -- version-free consumer import / Debug-source vs Release-package switch; do not add overrides.
- `eng/dependency-graph-policy.json` -- shape-only `Hexalith*Version` names; leave execution `evaluator_authorizations` alone for catalog-only landing.
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` -- update only `currentCompatibility`; keep historicalCapture immutable.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` (`EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval`) -- retarget `currentSourceSha` / `currentBuildsSha` / `currentVersion` to landed identities.
- `_bmad-output/implementation-artifacts/evidence/pact-provider-reconciliation/` -- refresh live-compatibility provider evidence to match new source/package/Builds tuple (reuse `eng/eventstore_runtime_evidence.py` / quality Gate 2c patterns).
- Unrelated dirty: `tests/.../CommandFormEmitterTests.cs`, `_bmad-output/.../spec-dw-683-command-form-emitter-test-token-anchoring.md` -- out of scope; preserve.
- Prior patterns: `spec-bump-eventstore-to-3-100-0.md`, `spec-bump-latest-hexalith-nuget-packages-2.md` -- Builds-first catalog then FrontComposer gitlink; catalog independent of execution SHA.

## Tasks & Acceptance

**Execution:**
- [ ] Resolve EventStore source SHA from Open Question; fetch as needed; set `references/Hexalith.EventStore` HEAD to that SHA without nested init -- converge Debug source identity.
- [ ] `references/Hexalith.Memories` -- fast-forward to `3a7a70259d0ff185947fcc2e4216f7a275651d68` (`origin/main`) -- latest Memories submodule.
- [ ] `references/Hexalith.Builds` -- set `HexalithEventStoreVersion` to `3.102.0`; regenerate EventStore-family audit via official NuGet V3; run Builds catalog/audit validators -- Release package authority.
- [ ] HALT for human Builds commit+push of catalog+audit; then FrontComposer `references/Hexalith.Builds` gitlink → that exact SHA -- consumer inherits published selector.
- [ ] `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` + `CiGovernanceTests` current-compatibility constants + pact-provider-reconciliation evidence -- match landed EventStore SHA, `3.102.0`, Builds catalog SHA without touching historicalCapture.
- [ ] Isolated Release AppHost restore/eval and Debug source eval -- prove EventStore Aspire/`3.102.0` package mode vs project-reference mode; leave unrelated dirty files unstaged.

**Acceptance Criteria:**
- Given nuget.org listings, when the Builds catalog/audit are validated, then all 13 EventStore rows are `3.102.0` listed/retained and other Hexalith family selectors remain unchanged.
- Given FrontComposer after Builds gitlink landing, when Release AppHost is evaluated with package mode, then it resolves `Hexalith.EventStore.Aspire/3.102.0` with no EventStore project edges.
- Given the chosen EventStore source SHA and Memories tip, when parent gitlinks and submodule HEADs are read, then they equal those SHAs and nested submodules remain uninitialized.
- Given runtime-identity governance, when `CiGovernanceTests` and contract currentCompatibility are evaluated, then they name the landed EventStore source SHA, package `3.102.0`, and Builds catalog SHA, with `migrationApprovalClaimed=false` and historicalCapture unchanged.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Catalog-only landing: leave `.github/workflows/{ci,release,release-evidence}.yml` Builds execution pins and `evaluator_authorizations` on existing approved closure `4eb33928…`. FrontComposer wrapper stays version-free. Memories NuGet stays `2.22.1` (already latest). Chatbot `1.80.0` retention unchanged.

## Verification

**Commands:**
- `git -C references/Hexalith.EventStore rev-parse HEAD` and (if tag path) `describe --tags --exact-match` -- expected: chosen Open Question SHA / `v3.102.0`.
- `git -C references/Hexalith.Memories rev-parse HEAD` -- expected: `3a7a70259d0ff185947fcc2e4216f7a275651d68`.
- From Builds: `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -Family hexalith-eventstore` then validate/test scripts -- expected: EventStore family `3.102.0`, gates green.
- Isolated Release/Debug AppHost `dotnet restore` / `msbuild -getItem` -- expected: package `3.102.0` vs EventStore project edges.
- Focused `CiGovernanceTests` EventStore runtime-identity fact -- expected: pass against refreshed contract/evidence.
