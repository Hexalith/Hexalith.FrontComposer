---
title: 'Align Latest Hexalith Modules and Simplify CI/CD Governance'
type: 'refactor'
created: '2026-08-11'
status: 'in-review'
baseline_commit: '984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** FrontComposer CI rejected a successful Release build because its policy duplicated an older EventStore version from the authoritative Builds catalog, then repeated the false failure through delayed activation. Builds CI also rejects 38 newer internal catalog rows recorded in an old audit snapshot, while FrontComposer CD executes obsolete Builds commit `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a`.

**Approach:** Update Builds-owned Memories to stable `2.20.7`, make internal-module checks structural and monotonic instead of duplicating selected literals, then align FrontComposer gitlinks, workflow pins, and evaluator closure to the resulting immutable Builds commit. Preserve compatibility builds and exact release provenance.

## Boundaries & Constraints

**Always:** Change artifacts in their owning repository; keep Builds as package-version authority; retain family alignment, valid-version, no-downgrade, import/override, affected-module Release/NuGet builds, exact graph/catalog hashes, workflow pins, evaluator authorization, and release-byte checks. After human integration, use the full Builds commit everywhere in FrontComposer.

**Ask First:** Creating or pushing the Builds commit; selecting another Memories release; proceeding after Builds or Memories `main` advances; altering external-package pins, graph boundaries, workflow trust, or publication controls.

**Never:** Add a FrontComposer-local package override, edit nested submodules, use recursive/remote submodule updates, make governance advisory, accept an internal downgrade, or remove exact dependency/workflow/release provenance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Internal module upgrade | An aligned `Hexalith.*` family advances beyond its audit snapshot | Catalog checks and consumer builds decide compatibility without stale-literal failure | Fail on malformed, split, unpublished, or build-incompatible input |
| Internal downgrade | Catalog selects a version below its audited baseline | Builds validation rejects it | Name the package and compared versions |
| Structural drift | A required property is missing, duplicated, conditional, or overridden | Semantic validation rejects it | Preserve owner/catalog diagnostics |
| Workflow drift | A CI/CD pin or closure differs from selected Builds | Release remains blocked | Report both identities |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:6-12,67-69` -- authoritative family properties and Memories rows.
- `references/Hexalith.Builds/Tools/validate-package-version-audit.ps1:225-310,403-472`, its fixture suite, and `Tools/README.md:61-101` -- separate owner-controlled internal advances from external dependency decisions.
- `eng/dependency-graph-policy.json:54-82,342-end` -- six duplicated module versions plus exact CI/Release/post-release evaluator closures.
- `eng/dependency_graph.py:1021-1057,1211-1325,1593-1652` -- required-property semantics and closed policy-shape validation.
- `tests/eng/test_dependency_graph.py:389-742,1342-1498` and `InfrastructureGovernanceTests.cs:35-62` -- semantic regressions and duplicate live graph consumers.
- `.github/workflows/ci.yml:24-220`, `.github/workflows/release.yml:16-315`, `.github/workflows/release-evidence.yml:228-239` -- reusable/action execution identities that must move in lockstep.
- `references/Hexalith.Builds` and `references/Hexalith.Memories` -- root-only gitlinks; the parent pointer follows the human-created Builds commit.

## Tasks & Acceptance

**Execution:**
- [x] Builds catalog, audit validator, fixtures, and tool docs -- adopt Memories `2.20.7` and allow aligned internal advances without allowing downgrades or weakening external-package decisions.
- [x] FrontComposer graph engine, policy, Python fixtures, and C# consumer -- make six module requirements presence/shape constraints and remove the duplicate live validation call.
- [x] Root gitlinks and three workflows -- use captured latest Memories/Builds identities, update every execution pin, and regenerate evaluator hashes/digests from exact source.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- record that compatibility comes from authoritative structure plus actual builds, while hashes remain provenance.

**Acceptance Criteria:**
- Given Builds selects published Memories `2.20.7`, when its catalog/audit suites run, then they pass with all three Memories packages aligned and still reject an internal downgrade.
- Given a compatible Hexalith module version advance, when FrontComposer dependency governance runs, then version-literal drift does not fail before the exact affected-module Release/NuGet build.
- Given a required catalog property is malformed or absent, when semantic validation runs, then it fails closed with the selecting owner and catalog coordinates.
- Given the integrated Builds commit, when CI, Release, and post-release provenance are evaluated, then every execution pin and authorized closure names that exact commit and the release contract accepts it.

## Spec Change Log

## Design Notes

The shared catalog selects versions; repeating internal values in consumer policy or an old feed snapshot adds synchronization failures, not compatibility evidence. Family alignment, monotonicity, publication checks, and consumer builds still protect upgrades. External dependency decisions remain explicit. Commits, catalog hashes, workflow blobs, graph digests, and package bytes remain exact provenance.

## Verification

**Commands:**
- The four Builds catalog/audit validators above plus `dotnet build Hexalith.Builds.slnx --configuration Release` -- expected: all pass.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_dependency_handoff.py tests/eng/test_workflow_source_closure.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py -v` -- expected: all pass.
- `builds_commit="$(git -C references/Hexalith.Builds rev-parse HEAD)"; python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved "$builds_commit"` plus `actionlint` on the three workflows -- expected: exact identity and syntax pass.
- `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` plus focused Governance tests -- expected: green with zero warnings.
