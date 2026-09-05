---
title: 'Bump Latest Root Submodules and Hexalith Packages'
type: 'refactor'
created: '2026-09-05'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1a7edded603cd557a97dda1277e5ae3101fbec4d'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Root EventStore and Memories gitlinks trail (or dirty-ahead of) `origin/main`, while the Builds catalog still selects EventStore `3.101.0` although nuget.org lists aligned stable `3.102.0` on all 13 EventStore packages. Other Hexalith families and six root submodules are already current. EventStore runtime-identity compatibility pins still name `3.101.0` / older SHAs.

**Approach:** Keep EventStore at exact tag `v3.102.0` / `4ae9cee1e9abe050402fd1405a9abd54892ba13f`, fast-forward Memories to `origin/main`, bump only the EventStore family in Builds to `3.102.0` with matching audit evidence, land the Builds catalog commit then FrontComposer Builds gitlink, refresh current-compatibility identity pins/evidence, and leave unrelated working-tree edits untouched.

**Decisions:** EventStore source identity = exact tag `v3.102.0` (`4ae9cee1e9abe050402fd1405a9abd54892ba13f`) so Debug source matches Release package `3.102.0`; discard the dirty tip checkout `89564e0c…`.

## Boundaries & Constraints

**Always:** Re-resolve remote tips and nuget.org listings before editing. Keep EventStore detached at exact `v3.102.0` / `4ae9cee1e9abe050402fd1405a9abd54892ba13f`. Keep all 13 EventStore rows on one `HexalithEventStoreVersion`. Edit catalog authority only in `references/Hexalith.Builds/Props/Directory.Packages.props` and refresh `Tools/package-version-audit.json` via the Builds audit scripts. Preserve UTF-8 BOM and CRLF on Builds catalog files. Use `git -c submodule.recurse=false submodule update --init` (never `--recursive` / `--remote`). Leave CI/Release Builds execution SHAs and `evaluator_authorizations` unchanged for this catalog-only advance. Preserve unrelated dirty paths (`CommandFormEmitterTests.cs`, `spec-dw-683-…`, other untracked work). HALT for a human to create/push the Builds catalog commit before moving FrontComposer’s Builds gitlink.

**Ask First:** Staging/committing/pushing FrontComposer or Builds; advancing any gitlink other than EventStore, Memories, and Builds; changing non-EventStore `Hexalith*Version` pins; claiming Story 11.24 migration approval; moving Builds execution pins.

**Never:** Add FrontComposer-local `PackageVersion` / `Hexalith*Version` overrides; initialize nested submodules; downgrade or move stable→prerelease; rewrite immutable historical identity capture; overwrite unrelated dirty files; auto-push.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| EventStore packages | Catalog `3.101.0`; nuget.org stable `3.102.0` on all 13 | `HexalithEventStoreVersion` + 13 rows + audit → `3.102.0` / listed / retained | Halt if any family member unpublished or split |
| Already-latest families | Commons `2.30.0`, Polymorphic `1.19.2`, Memories `2.22.1`, Tenants `5.6.0`, Parties `1.0.0`, FrontComposer `4.2.0` | Pins unchanged | N/A |
| Memories gitlink | Committed `7df55c12…`; tip `3a7a7025…` | Parent gitlink = Memories `origin/main` | Halt on non-fast-forward / divergent dirty submodule |
| EventStore source | Committed `4ae9cee1…` = `v3.102.0`; dirty tip `89564e0c…` | Detach/reset to exact `v3.102.0` / `4ae9cee1…`; discard tip | Halt if tag SHA missing after fetch |
| Runtime identity | Contract/tests still `3.101.0` / `f152995…` / `7e84ff1…` | `currentCompatibility` + `CiGovernanceTests` match landed EventStore source SHA, `3.102.0`, and new Builds catalog SHA | Do not edit historicalCapture / approved tuples |
| Unrelated dirty tree | Spec DW-683 + CommandFormEmitterTests edits present | Left untouched; only stage in-scope paths | Do not clean/revert |

</frozen-after-approval>

## Code Map

- `.gitmodules` -- eight root `references/` submodules on `branch = main`; only these may be initialized.
- `references/Hexalith.EventStore` -- committed gitlink `4ae9cee1…` = exact `v3.102.0`; dirty worktree at `89564e0c…` must return to the tag.
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
- [x] `references/Hexalith.EventStore` -- detach/reset to `4ae9cee1e9abe050402fd1405a9abd54892ba13f` (`v3.102.0`); confirm exact-match tag; no nested init -- Debug source matches package.
- [x] `references/Hexalith.Memories` -- fast-forward to `3a7a70259d0ff185947fcc2e4216f7a275651d68` (`origin/main`) -- latest Memories submodule.
- [x] `references/Hexalith.Builds` -- set `HexalithEventStoreVersion` to `3.102.0`; regenerate EventStore-family audit via official NuGet V3; run Builds catalog/audit validators -- Release package authority.
- [x] HALT for human Builds commit+push of catalog+audit; then FrontComposer `references/Hexalith.Builds` gitlink → that exact SHA -- consumer inherits published selector. **Landed:** human pushed audit commit `0a54e63a7903bd599e35b79159782b4c84d01c07`; parent gitlink matches.
- [x] `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v1.json` + `CiGovernanceTests` current-compatibility constants + pact-provider-reconciliation evidence -- match landed EventStore SHA, `3.102.0`, Builds catalog SHA without touching historicalCapture.
- [x] Isolated Release AppHost restore/eval and Debug source eval -- prove EventStore Aspire/`3.102.0` package mode vs project-reference mode; leave unrelated dirty files unstaged.

**Acceptance Criteria:**
- Given nuget.org listings, when the Builds catalog/audit are validated, then all 13 EventStore rows are `3.102.0` listed/retained and other Hexalith family selectors remain unchanged.
- Given FrontComposer after Builds gitlink landing, when Release AppHost is evaluated with package mode, then it resolves `Hexalith.EventStore.Aspire/3.102.0` with no EventStore project edges.
- Given EventStore `v3.102.0` and Memories tip, when parent gitlinks and submodule HEADs are read, then they equal `4ae9cee1…` and `3a7a7025…` respectively and nested submodules remain uninitialized.
- Given runtime-identity governance, when `CiGovernanceTests` and contract currentCompatibility are evaluated, then they name the landed EventStore source SHA, package `3.102.0`, and Builds catalog SHA, with `migrationApprovalClaimed=false` and historicalCapture unchanged.

## Implementation Notes

### 2026-09-05 — agent progress

**EventStore (`references/Hexalith.EventStore`)**
- Fetched tags; reset detached HEAD from dirty `c08cb349…` to exact `v3.102.0` / `4ae9cee1e9abe050402fd1405a9abd54892ba13f`.
- Staged parent gitlink update (`references/Hexalith.EventStore` → `4ae9cee1…`); not committed (Ask First).

**Memories (`references/Hexalith.Memories`)**
- Already at `3a7a70259d0ff185947fcc2e4216f7a275651d68` matching `origin/main`; parent gitlink unchanged.

**Builds (`references/Hexalith.Builds`)**
- Catalog selector already at `3.102.0` on local commit `b7493539e4c6ede44d895524f4420f1c0ff51d40` (`fix: update HexalithEventStoreVersion to 3.102.0`).
- Regenerated EventStore-family audit via `audit-central-package-versions.ps1 -Family hexalith-eventstore`; all 13 EventStore rows `selectedVersion`/`auditedVersion`/`latestStable` = `3.102.0`, `listingState=listed`, `disposition=retained`.
- Validators green: `validate-central-package-versions.ps1`, `validate-package-version-audit.ps1`, `test-authoritative-package-catalog.ps1`, `test-package-version-audit-generator.ps1` (111 scenarios), `test-package-version-audit-validator.ps1` (103 scenarios).
- `Tools/package-version-audit.json` is dirty/uncommitted; parent FrontComposer gitlink **not** advanced (HALT gate).

**Verification (FrontComposer)**
- Release: `HexalithEventStoreVersion=3.102.0`, `HexalithEventStoreFromSource=false`, sole EventStore edge `Hexalith.EventStore.Aspire` package, zero EventStore project edges; isolated AppHost Release build 0 warnings/errors.
- Debug: exact tag `v3.102.0`, `HexalithEventStoreFromSource=true`, four EventStore project edges, zero EventStore package edges.
- EventStore nested submodules remain uninitialized (`-` prefix).

**Preserved out-of-scope dirty tree:** no edits to `CommandFormEmitterTests.cs`, `spec-dw-683-…`, or other unrelated paths.

### HALT — human steps before continuing

1. In `references/Hexalith.Builds`: stage and commit `Tools/package-version-audit.json` atop `b749353…` (catalog-only commit already present). Push to `origin/main`.
2. Record the pushed audit commit SHA (`<builds-audit-sha>`).
3. In FrontComposer: `git add references/Hexalith.Builds` to advance gitlink from `e0e06946…` → `<builds-audit-sha>`.
4. Update `currentCompatibility` in `frontcomposer-eventstore-approved-runtime-identity-v1.json`, `CiGovernanceTests` constants, and `pact-provider-reconciliation/` evidence (`observedSourceSha=4ae9cee1…`, `expectedVersion=3.102.0`, `observedBuildsSha=<builds-audit-sha>`) without touching `historicalCapture`.
5. Re-run Gate 2c live provider verification + `eng/eventstore_runtime_evidence.py --write-live-receipt` against refreshed tuple.
6. Commit FrontComposer submodule gitlinks + governance (Ask First).

**Risk:** `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` will fail until steps 3–4 land — contract still names `3.101.0` / `f1529957…` / `7e84ff1…` while EventStore gitlink is staged at `4ae9cee1…`.

### 2026-09-05 — post-HALT governance + live provider evidence

**Human landed (confirmed on main):**
- EventStore gitlink `4ae9cee1e9abe050402fd1405a9abd54892ba13f` (`v3.102.0`)
- Memories gitlink `3a7a70259d0ff185947fcc2e4216f7a275651d68`
- Builds gitlink `0a54e63a7903bd599e35b79159782b4c84d01c07` (catalog `3.102.0` + audit)

**Governance refresh (agent):**
- Updated `currentCompatibility` only in `frontcomposer-eventstore-approved-runtime-identity-v1.json` — `4ae9cee1…`, `3.102.0`, `0a54e63a…`; `historicalCapture` and approved tuples unchanged.
- Retargeted `CiGovernanceTests.EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` `currentSourceSha` / `currentBuildsSha` / `currentVersion` to the same landed identities.

**Live provider evidence (Gate 2c pattern, executed):**
- Built `Hexalith.EventStore.ProviderVerification` Release; 77/77 unit tests passed.
- Ran live-compatibility verifier from `references/Hexalith.EventStore` CWD; exit 0; regenerated `provider-verification.json` with identity tuple matching landed SHAs/versions (`observedVersion=3.102.0+4ae9cee1…`).
- Wrote fresh `run-evidence.json` receipt via `eng/eventstore_runtime_evidence.py --write-live-receipt`.

**Verification:**
- `python3 -m unittest tests.eng.test_eventstore_runtime_evidence` — 45/45 OK.
- `python3 -m unittest tests.eng.test_pact_provider_apphost_smoke` — 4/4 OK.
- `DiffEngine_Disabled=true dotnet test … --filter FullyQualifiedName~EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` — passed.
- `eng/validate-contract-artifacts.ps1 -RequireProviderVerification` — still fails: AppHost smoke is not a clean passing run.

**AppHost smoke attempt (post-HALT):**
- Ran `python3 eng/pact_provider_apphost_smoke.py --timeout-seconds 300`; wrote `apphost-smoke.json` with correct identity (`4ae9cee1…` / `3.102.0` / `0a54e63a…`) but `finalVerdict=failed`, `reasonCodes=['apphost.start.failed']`.
- Manual `aspire start` failed AppHost build with `CS1704` duplicate `Hexalith.FrontComposer.Shell` (NuGet `4.2.0` vs project) under IDE/MSBuild file locks; clean rebuild also hit locked Shell resources. Residual Gate 2c risk until a clean passing smoke can be captured.
- Human landed governance + Tenants follow-up on `d71790bb` (`fix(ci): unblock dependency-governance after Tenants AppHost NuGet fix`), including contract/tests/evidence/spec updates from this bump.

## Spec Change Log

## Review Triage Log

- false — Tenants/EventStore gitlink “missing or unauthorized in this changeset”: HEAD already has EventStore `4ae9cee1…`, Memories `3a7a7025…`, Builds `0a54e63a…`; Tenants `b5e9907c…` advanced in parallel human commits (`58197d7c`, `d71790bb`), not by the bump implementation path. CiGovernanceTests ls-tree assertions match landed EventStore/Builds SHAs.
- false — CommandFormEmitterTests / DW-683 / analyzer-ledger “boundary violations by this bump”: those files entered the baseline→HEAD range via separate human commits (`8b38d7bb`, `1e4743a4`, ledger reseal in `d71790bb`); bump tasks did not author DW-683.
- false — “EventStore gitlink absent so governance retarget will fail”: `git ls-tree HEAD references/Hexalith.EventStore` is `4ae9cee1…`; focused `EventStoreRuntimeIdentitySeparatesCurrentCompatibilityFromHistoricalApproval` passes against that HEAD.
- false — claim that acceptance is unmet for EventStore/Memories gitlink parity: submodule HEADs and parent gitlinks equal the frozen SHAs; nested EventStore submodules remain uninitialized (`-` prefix).
- maybe-false → reject as low — stale HALT / dual tip-SHA wording / empty Spec Change Log in the bump spec: documentation hygiene only; fixing means editing the build spec (forbidden route).
- medium (defer) — `apphost-smoke.json` remains `finalVerdict=failed` / `apphost.start.failed` after identity refresh; `validate-contract-artifacts.ps1 -RequireProviderVerification` stays red. Pre-existing Gate 2c AppHost start failure (prior capture was also failed); provider live-compatibility lane is green. Full quality Gate 2c will keep failing until a clean Aspire AppHost smoke can be captured.
- medium (defer) — no automated FrontComposer test pins Release AppHost MSBuild graph to solely `Hexalith.EventStore.Aspire/3.102.0` with zero EventStore project edges; only catalog string + manual eval (historical bump pattern). Would need a new governance fact beyond this catalog landing.
- low (reject) — CommandFormEmitterTests `IndexOf` uniqueness for SubmittedLogCall: DW-683 surface, not caused by this bump; everyday harm unlikely for this story.
- defer disposition confirmed for verification-gap AppHost focused-vs-Gate2c note: focused governance fact intentionally does not invoke live AppHost validation; CI Gate 2c does.

## Design Notes

Catalog-only landing: leave `.github/workflows/{ci,release,release-evidence}.yml` Builds execution pins and `evaluator_authorizations` on existing approved closure `4eb33928…`. FrontComposer wrapper stays version-free. Memories NuGet stays `2.22.1` (already latest). Chatbot `1.80.0` retention unchanged.

## Verification

**Commands:**
- `git -C references/Hexalith.EventStore rev-parse HEAD` and `describe --tags --exact-match` -- expected: `4ae9cee1e9abe050402fd1405a9abd54892ba13f` / `v3.102.0`.
- `git -C references/Hexalith.Memories rev-parse HEAD` -- expected: `3a7a70259d0ff185947fcc2e4216f7a275651d68`.
- From Builds: `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -Family hexalith-eventstore` then validate/test scripts -- expected: EventStore family `3.102.0`, gates green.
- Isolated Release/Debug AppHost `dotnet restore` / `msbuild -getItem` -- expected: package `3.102.0` vs EventStore project edges.
- Focused `CiGovernanceTests` EventStore runtime-identity fact -- expected: pass against refreshed contract/evidence.
