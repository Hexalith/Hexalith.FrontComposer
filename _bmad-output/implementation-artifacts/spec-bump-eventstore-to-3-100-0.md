---
title: 'Bump EventStore Source and Package Family to 3.100.0'
type: 'refactor'
created: '2026-08-30'
status: 'done'
baseline_commit: 'f84b68b4e147238f28ca70219f19233d4b4b64d1'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer Debug is pinned to EventStore `38967215e6c1b13e77f2b0006efd95d88d7ad7b8` (`v3.99.0-18`), two commits behind exact tag `v3.100.0`. Release still inherits the Builds-owned 13-package family at `3.99.0`. The modes therefore select different EventStore releases.

**Approach:** Point the root EventStore gitlink at exact tag `v3.100.0`, fast-forward Builds to the already-pushed selector `3.100.0`, refresh only that family's governed audit evidence, then prove Release restores `Hexalith.EventStore.Aspire/3.100.0` while Debug consumes the tagged source.

## Boundaries & Constraints

**Always:** Keep all 13 EventStore package rows on the one conditional `HexalithEventStoreVersion`. Fast-forward Builds to `e1026cb61162546571ee0102c525bcf42b9ce7fa` (catalog already `3.100.0`) then regenerate audit evidence only for the EventStore family. Preserve unrelated catalog selections and audit dispositions; retain catalog UTF-8 BOM and CRLF. Use official NuGet V3 listing evidence. Detach EventStore to `10051a68eb1db322a4f7fa91934d880ce1409687` (`v3.100.0`). Leave `_bmad-output/implementation-artifacts/spec-actions-33264036185-33264035739-fix-cicd-release.md` untouched.

**Ask First:** Committing, staging, or pushing either repository; changing another package family or any gitlink other than EventStore and Builds; accepting an unrelated audit upgrade; editing EventStore source, dependency wiring, application code, policy, or CI; continuing if the Builds fast-forward changes any catalog version besides `HexalithEventStoreVersion`.

**Never:** Add a FrontComposer-local or inline package override; initialize nested submodules or use recursive/`--remote` updates; split the EventStore family across versions; weaken validation; overwrite unrelated work; claim provider/Pact compatibility from identity selection alone.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Release package mode | `UseHexalithProjectReferences=false` | Only `Hexalith.EventStore.Aspire/3.100.0`; no EventStore project edge | Fail without falling back to source |
| Debug source mode | Root EventStore at `v3.100.0` | Four EventStore project edges; no EventStore package edge; HEAD `10051a68eb1db322a4f7fa91934d880ce1409687` | Stop if the source identity is not the exact tag |
| Catalog audit | 13 aligned rows at `3.100.0` | Every row is listed/latest stable and remains `retained` | Stop on missing rows or unrelated selection/disposition drift |

</frozen-after-approval>

## Code Map

- `references/Hexalith.EventStore` -- parent gitlink currently `38967215e6c1b13e77f2b0006efd95d88d7ad7b8`; local object `v3.100.0` = `10051a68eb1db322a4f7fa91934d880ce1409687` (two commits ahead: `f44b5800`, `10051a68`). Detach only; do not init nested Tenants/Builds.
- `references/Hexalith.Builds` -- parent gitlink currently `2b0faab931ec581c7503270e7dd73074654e2eee`, behind `origin/main` by three commits: `9704d16` (tag `v4.27.0`, generator test), `c06d5a6` (audit validator), `e1026cb61162546571ee0102c525bcf42b9ce7fa` (`HexalithEventStoreVersion=3.100.0` only). Audit JSON still records `3.99.0`.
- `references/Hexalith.Builds/Props/Directory.Packages.props:8,40-52` -- after fast-forward, selector is already `3.100.0`; 13 rows already consume that property. Preserve BOM and CRLF; do not re-edit the selector unless the fast-forward is rejected.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- generated governance evidence; refresh the 13 `hexalith-eventstore` rows without accepting unrelated upgrades.
- `references/Hexalith.Builds/Tools/audit-central-package-versions.ps1` -- canonical live NuGet V3 generator; reuse without editing.
- `references/Hexalith.Builds/Tools/{validate-central-package-versions,test-authoritative-package-catalog,validate-package-version-audit,test-package-version-audit-generator,test-package-version-audit-validator}.ps1` -- catalog and audit gates; reuse. Fast-forward updates the last two scripts; do not edit them further.
- `Directory.Packages.props`, `deps.local.props`, `deps.nuget.props`, `Directory.Build.props:16-26` -- read-only import and Debug/source versus Release/package switch; no local EventStore version belongs here.
- `src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj:16-20,31-40` -- Release consumes `Hexalith.EventStore.Aspire`; Debug identity checks use four EventStore project edges (Aspire + EventStore + Admin.Server.Host + Admin.UI).
- `eng/dependency-graph-policy.json` -- EventStore is presence/shape of `HexalithEventStoreVersion` only; do not change policy or CI execution SHAs.
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-package-to-3-99-0.md` -- prior bump's catalog/audit and isolated restore pattern; do not copy its obsolete identities (`3.99.0`, `f18fbf11…`).
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.EventStoreRuntimeIdentityPinsOwnerApprovedTupleAndTruthfulDriftEvidence` -- already stale vs current pins; out of scope (do not retarget 11.24 tuples in this bump).

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.EventStore` -- `git fetch` as needed, then `git switch --detach 10051a68eb1db322a4f7fa91934d880ce1409687`; confirm `rev-parse HEAD` and `describe --tags --exact-match` equal `v3.100.0`; deinitialize any nested submodule that appears -- converge Debug source identity without recursive updates.
- [x] `references/Hexalith.Builds` -- fast-forward to `e1026cb61162546571ee0102c525bcf42b9ce7fa`, confirm the catalog diff versus `2b0faab931ec581c7503270e7dd73074654e2eee` changes only `HexalithEventStoreVersion` plus the two already-pushed Tools validator files, regenerate `Tools/package-version-audit.json` from the live catalog audit generator, and inspect so the 13 EventStore rows move to listed `3.100.0`/`retained` while unrelated selections/dispositions stay unchanged -- converge Release package identity on official listing evidence.
- [x] FrontComposer dependency evaluation -- isolated Release restore/MSBuild item evaluation and Debug source evaluation using the 3.98/3.99 AppHost pattern, without modifying dependency wiring -- prove both modes select `3.100.0`.

**Acceptance Criteria:**
- Given the Builds catalog after fast-forward and audit refresh, when it is evaluated and validated, then exactly 13 EventStore rows resolve through `HexalithEventStoreVersion=3.100.0` and all catalog/audit gates pass.
- Given official NuGet metadata, when the audit is regenerated, then all 13 EventStore entries record `auditedVersion`, `selectedVersion`, and `latestStable` as `3.100.0`, `listingState=listed`, and `disposition=retained`.
- Given FrontComposer Release mode, when the AppHost is restored and evaluated with an isolated package cache, then it contains only `Hexalith.EventStore.Aspire/3.100.0` and no EventStore project reference.
- Given Debug source mode, when the AppHost is evaluated, then it retains four root EventStore project references, no EventStore package reference, and exact source identity `v3.100.0` / `10051a68eb1db322a4f7fa91934d880ce1409687`.

## Spec Change Log

- 2026-08-30 implementation -- detached EventStore to exact tag `v3.100.0`, fast-forwarded Builds to selector commit `e1026cb61162546571ee0102c525bcf42b9ce7fa`, regenerated EventStore-family audit evidence from nuget.org V3, and proved isolated Release/Debug AppHost identities. No commit or push. KEEP: frozen identities, no local package override, and the Ask First commit/push gate.

## Design Notes

Builds `origin/main` already contains selector commit `e1026cb61162546571ee0102c525bcf42b9ce7fa` but still audits `3.99.0`. Fast-forwarding is the linear path to that selector and also takes `v4.27.0` audit-tooling commits `9704d16` and `c06d5a6`; those are not a second FrontComposer deliverable. The catalog gitlink stays independent of immutable CI/Release execution SHAs.

All 13 EventStore packages are listed on nuget.org at `3.100.0` as latest stable (checked 2026-08-30).

## Verification

**Commands:**
- `git -C references/Hexalith.EventStore rev-parse HEAD` and `git -C references/Hexalith.EventStore describe --tags --exact-match HEAD` -- expected: `10051a68eb1db322a4f7fa91934d880ce1409687` and `v3.100.0`.
- `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1` from `references/Hexalith.Builds` -- expected: refreshes audit evidence from the configured official source.
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1; pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1; pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1` from Builds -- expected: all catalog and production-audit gates pass.
- `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1; pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1; dotnet build Hexalith.Builds.slnx --configuration Release` from Builds -- expected: generator/validator scenarios and Release build pass.
- Isolated `dotnet restore`, `dotnet msbuild -getProperty/-getItem`, and focused AppHost Release build with package mode (`UseHexalithProjectReferences=false`) -- expected: only Aspire `3.100.0`, zero EventStore project edges, zero warnings/errors.
- Debug `dotnet msbuild -getProperty/-getItem` plus exact EventStore tag checks -- expected: four EventStore project edges, zero package edges, checkout `v3.100.0`.

**Observed results (2026-08-30):**
- EventStore `HEAD` is `10051a68eb1db322a4f7fa91934d880ce1409687` with exact tag `v3.100.0`. Nested EventStore submodules remain uninitialized (`-` prefix). No EventStore source edits.
- Builds fast-forwarded `2b0faab931ec581c7503270e7dd73074654e2eee` → `e1026cb61162546571ee0102c525bcf42b9ce7fa`. Catalog diff versus that base changed only `HexalithEventStoreVersion` `3.99.0` → `3.100.0` plus the two already-pushed Tools validator files. Catalog retains UTF-8 BOM and CRLF; all 13 EventStore rows still consume `$(HexalithEventStoreVersion)`.
- Live NuGet V3 audit wrote 286 packages from `https://api.nuget.org/v3/index.json`. Exactly the 13 `hexalith-eventstore` rows moved to `auditedVersion`/`selectedVersion`/`latestStable` `3.100.0`, `listingState=listed`, `disposition=retained`. Zero unrelated selected-version, disposition, latest-stable, listing, or audited-version changes. Provenance: `generatedFromRevision=e1026cb61162546571ee0102c525bcf42b9ce7fa`, `catalogSha256=53c80491d16674d40c4dcf47f4d8f77a80454b49b3add8716586b684e98af235`. The audit refresh is uncommitted in Builds (`Tools/package-version-audit.json` only).
- Catalog validation passed 286 entries; authoritative-catalog tests passed 50 identities and three shared selectors; production audit validation passed 286 packages, 141 families, and one source; generator tests passed 55 scenarios; validator tests passed 66 scenarios on the isolated retry after two environment flakes (`The non-terminating Git shim did not record both owned process IDs.`). `dotnet build Hexalith.Builds.slnx --configuration Release` passed with zero warnings and errors.
- Isolated Release restore/evaluation resolved only `Hexalith.EventStore.Aspire/3.100.0`, `HexalithEventStoreFromSource=false`, and zero EventStore project edges. Focused AppHost Release build (`BuildProjectReferences=false`) passed with zero warnings and errors.
- Isolated Debug evaluation resolved four root EventStore project edges (Aspire, EventStore, Admin.Server.Host, Admin.UI), no EventStore package edge, `HexalithEventStoreFromSource=true`, and exact source tag `v3.100.0`.
- This session did not stage, commit, or push either repository. Unrelated FrontComposer working-tree files and `_bmad-output/implementation-artifacts/spec-actions-33264036185-33264035739-fix-cicd-release.md` were left untouched. Policy, CI execution SHAs, and AppHost wiring were not edited.

## File List

- `references/Hexalith.EventStore` -- parent gitlink moves from `38967215e6c1b13e77f2b0006efd95d88d7ad7b8` to detached `10051a68eb1db322a4f7fa91934d880ce1409687` (`v3.100.0`).
- `references/Hexalith.Builds` -- parent gitlink moves from `2b0faab931ec581c7503270e7dd73074654e2eee` to `e1026cb61162546571ee0102c525bcf42b9ce7fa`; working tree additionally refreshes `Tools/package-version-audit.json` for EventStore `3.100.0` (uncommitted).
- `_bmad-output/implementation-artifacts/spec-bump-eventstore-to-3-100-0.md` -- approved scope, completed tasks, and validation evidence.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- review-deferred mixed-branch and Builds validator-flake notes.


## Documented Unrelated Workspace State

Parallel `fix/cicd-mtp-release` work on the same HEAD as `f84b68b4e147238f28ca70219f19233d4b4b64d1`. This EventStore bump did not author, stage, or claim these paths.

- `.github/workflows/ci.yml` - concurrent MTP/CI repair on `fix/cicd-mtp-release`
- `.github/workflows/nightly.yml` - concurrent MTP/CI repair on `fix/cicd-mtp-release`
- `.github/workflows/quality.yml` - concurrent MTP/CI repair on `fix/cicd-mtp-release`
- `.github/workflows/quarantine-governance-nightly.yml` - concurrent MTP/CI repair on `fix/cicd-mtp-release`
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` - concurrent CI repair ledger
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/apphost-smoke/apphost-smoke.json` - concurrent evidence rewrite
- `_bmad-output/implementation-artifacts/evidence/frontcomposer-story-11-24/sha256-manifest.json` - concurrent evidence rewrite
- `_bmad-output/implementation-artifacts/spec-actions-33264036185-33264035739-fix-cicd-release.md` - pre-existing untracked CI/release draft left untouched
- `eng/dependency-graph-policy.json` - concurrent CI policy edit
- `eng/release_prepublish.py` - concurrent CI repair
- `eng/run-lifecycle-property-suite.ps1` - concurrent CI repair
- `global.json` - concurrent SDK/CI repair
- `samples/Counter/Counter.Web/Program.cs` - concurrent catch-up circuit repair
- `tests/Directory.Build.props` - concurrent untracked MTP test props
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CommandTargetGeneratedFormTests.cs` - concurrent generated-test repair
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.CounterProjectionView_LoadedState_RendersColumnsAndFormatting.verified.txt` - concurrent snapshot repair
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.StatusProjectionView_NullAndBooleanValues_RenderSnapshot.verified.txt` - concurrent snapshot repair
- `tests/Hexalith.FrontComposer.Shell.Tests/Generated/CounterStoryVerificationTests.cs` - concurrent generated-test repair
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` - concurrent CI governance repair
- `tests/README.md` - concurrent CI documentation
- `tests/e2e/package-lock.json` - concurrent Playwright script repair
- `tests/e2e/package.json` - concurrent Playwright script repair
- `tests/eng/test_release_prepublish.py` - concurrent CI repair

## Suggested Review Order

**Catalog selector**

- Shared EventStore family pin is already `3.100.0` on Builds `e1026cb`.
  [`Directory.Packages.props:8`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L8)

- All 13 EventStore rows still consume that one property.
  [`Directory.Packages.props:40`](../../references/Hexalith.Builds/Props/Directory.Packages.props#L40)

**Audit evidence**

- Live NuGet V3 refresh records Aspire `3.100.0` listed and retained.
  [`package-version-audit.json:91647`](../../references/Hexalith.Builds/Tools/package-version-audit.json#L91647)

**Consumer identity**

- Debug AppHost takes EventStore Aspire from the root source checkout.
  [`Hexalith.FrontComposer.AppHost.csproj:16`](../../src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj#L16)

- Release AppHost takes the catalog package with no `Version=` override.
  [`Hexalith.FrontComposer.AppHost.csproj:19`](../../src/Hexalith.FrontComposer.AppHost/Hexalith.FrontComposer.AppHost.csproj#L19)

- Root EventStore gitlink is the source identity to detach at `v3.100.0`.
  [`.gitmodules:2`](../../.gitmodules#L2)

