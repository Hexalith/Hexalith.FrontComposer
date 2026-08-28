---
title: 'Bump EventStore Package Family to 3.99.0'
type: 'refactor'
created: '2026-08-28'
status: 'in-review'
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
