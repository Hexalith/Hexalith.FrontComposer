---
title: 'Align Latest Hexalith Modules and Simplify CI/CD Governance'
type: 'refactor'
created: '2026-08-11'
status: 'in-review'
baseline_commit: '984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d'
review_loop_iteration: 1
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
---

<frozen-after-approval reason="human-owned intent — renegotiated 2026-08-14 by Administrator (bmad-review: Memories 2.21.1 + historical evaluator keep-vs-replace)">

## Intent

**Problem:** FrontComposer CI rejected a successful Release build because its policy duplicated an older EventStore version from the authoritative Builds catalog, and delayed activation then repeated the false failure. Builds CI also rejects 38 newer internal catalog rows when they are checked against an old audit snapshot, while FrontComposer CD still embeds obsolete Builds commit `a8a50859fa2f27f511a9470dfe1e3ae54d0ebc1a` in historical evaluator rows after workflows moved to `3f0e3595be693fce56a37648c0bd0f89390f5fd3`.

**Approach:** Update Builds-owned Memories to stable `2.21.1`, make internal-module checks structural and monotonic instead of duplicating selected literals, then align FrontComposer gitlinks, workflow pins, and active evaluator closures to the resulting immutable Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`. Preserve compatibility builds and exact release provenance, including historical evaluator rows that already-published releases still require.

## Boundaries & Constraints

**Always:** Change artifacts in their owning repository; keep Builds as package-version authority; retain family alignment, valid-version, no-downgrade, import/override, affected-module Release/NuGet builds, exact graph/catalog hashes, workflow pins, evaluator authorization, and release-byte checks. After human integration, use the full Builds commit on every in-scope FrontComposer execution pin and every active CI/Release/post-release evaluator closure. Retain historical `evaluator_authorizations` rows only when already-published release provenance still requires them.

**Ask First:** Creating or pushing the Builds commit; selecting another Memories release; proceeding after Builds or Memories `main` advances; altering external-package pins, graph boundaries, workflow trust, or publication controls.

**Never:** Add a FrontComposer-local package override, edit nested submodules, use recursive/remote submodule updates, make governance advisory, accept an internal downgrade, or remove exact dependency/workflow/release provenance.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Internal module upgrade | An aligned `Hexalith.*` family advances beyond its audit snapshot | Catalog checks and consumer builds decide compatibility without stale-literal failure | Fail on malformed, split, unpublished, or build-incompatible input |
| Internal downgrade | Catalog selects a version below its audited baseline | Builds validation rejects it | Name the package and compared versions |
| Structural drift | A required property is missing, duplicated, conditional, or overridden | Semantic validation rejects it | Preserve owner/catalog diagnostics |
| Workflow drift | A CI/CD pin or closure differs from selected Builds | Release remains blocked | Report both identities |
| Historical evaluator rows | Policy already contains prior CI/Release/post-release closures while workflows pin the integrated commit | Active CI/Release/post-release closures name the integrated commit; prior rows remain only if already-published release provenance still requires them | Do not wipe historical rows; do not require every stored row to name the new commit |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props:6-13,67-69` -- authoritative family properties (including Memories `2.21.1`) and the three Memories package rows. `HexalithFrontComposerVersion` is self-version / release-owned; `HexalithChatbotVersion` has no consumer here — neither joins the six governed names.
- `references/Hexalith.Builds/Tools/test-authoritative-package-catalog.ps1`, `validate-package-version-audit.ps1`, `test-package-version-audit-generator.ps1`, `test-package-version-audit-validator.ps1`, and `Tools/README.md:61-101` -- separate owner-controlled internal advances from external dependency decisions.
- `eng/dependency-graph-policy.json:54-67,346-end` -- `selected_catalog_required_property_names` (six consumed `Hexalith*Version` names) plus empty `selected_catalog_required_properties`, then exact CI/Release/post-release `evaluator_authorizations` (active `3f0e3595…` rows and retained historical `a8a50859…` rows).
- `eng/dependency_graph.py:1139-1174,1438-1442,1784-1847` -- `assert_selected_catalog_property_shape`, the `required_property_names` loop, and closed name validation. Do not treat `1021-1057` / `1211-1325` as property-shape checks.
- `tests/eng/test_dependency_graph.py:557-611,1933-2037` and `tests/Hexalith.FrontComposer.Shell.Tests/Governance/InfrastructureGovernanceTests.cs:35-55` -- semantic property-shape regressions and the single live `RunDependencyGraphValidate` consumer (`CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds`).
- `.github/workflows/ci.yml:24-25`, `.github/workflows/release.yml:16-17,321-329`, `.github/workflows/release-evidence.yml:228-239` -- in-scope reusable/action execution identities that must move in lockstep to `3f0e3595…`. Out of scope (remain `@main` unless Ask First changes workflow trust): `commitlint.yml`, `codeql.yml`, `dependency-review.yml`, and other non-release reusable workflows.
- `references/Hexalith.Builds` (`3f0e3595be693fce56a37648c0bd0f89390f5fd3`) and `references/Hexalith.Memories` (`301041626f32d4fb9b6a1154e5e09d65a70a2fcc`) -- root-only gitlinks. Update with `git -c submodule.recurse=false submodule update --init`; never `--remote` or recursive. Regenerating an active closure uses `python3 eng/dependency_handoff.py draft-evaluator --stage {ci|release|post_release} --caller-commit <HEAD> --caller-workflow <path> --policy-commit <HEAD> --output <file>` and writes back caller blob, reusable commit, action commits, `closure_digest`, and `definition_digest`.

## Tasks & Acceptance

**Execution:**
- [x] Builds catalog, audit validator, fixtures, and tool docs -- adopt Memories `2.21.1` and allow aligned internal advances without allowing downgrades or weakening external-package decisions.
- [x] FrontComposer graph engine, policy, Python fixtures, and C# consumer -- make six module requirements presence/shape constraints and remove the duplicate live validation call.
- [x] Root gitlinks and three in-scope workflows -- use captured latest Memories/Builds identities, update every in-scope execution pin, regenerate active evaluator hashes/digests from exact source, and keep historical authorization rows that published-release provenance still requires.
- [x] `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- record that compatibility comes from authoritative structure plus actual builds, while hashes remain provenance.

**Acceptance Criteria:**
- Given Builds selects published Memories `2.21.1`, when its catalog/audit suites run, then they pass with all three Memories packages aligned and still reject an internal downgrade.
- Given a compatible Hexalith module version advance, when FrontComposer dependency governance runs, then governance does not fail on version-literal drift before the exact affected-module Release/NuGet build.
- Given a required catalog property is malformed or absent, when semantic validation runs, then it fails closed with the selecting owner and catalog coordinates.
- Given the integrated Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`, when CI, Release, and post-release provenance are evaluated, then every in-scope execution pin and every active authorized closure names that exact commit, historical `evaluator_authorizations` rows remain only when already-published release provenance still requires them, and the release contract accepts the integrated commit.

## Spec Change Log

- 2026-08-14 (`bmad-review` loop 1, Administrator): renegotiated frozen Memories target `2.20.7` → live published `2.21.1` and Builds commit `3f0e3595be693fce56a37648c0bd0f89390f5fd3`; added keep-vs-replace for historical `evaluator_authorizations` (active closures name the integrated commit; retain prior rows only for already-published release provenance). Retargeted Code Map and Verification to the live property-name shape, named Builds validators, in-scope workflow pins, `draft-evaluator` inputs, and the single Governance consumer. Prose: delayed-activation subject, catalog-vs-audit-snapshot wording, governance-as-fail-subject, Design Notes condensed to the unique rationale.

## Design Notes

The shared catalog selects versions; repeating internal values in consumer policy or an old audit snapshot adds synchronization failures, not compatibility evidence. Family alignment, monotonicity, publication, consumer builds, and exact provenance still bind.

## Verification

**Commands:**
- From `references/Hexalith.Builds`: `pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1`, `pwsh -NoProfile -File ./Tools/validate-package-version-audit.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-generator.ps1`, `pwsh -NoProfile -File ./Tools/test-package-version-audit-validator.ps1`, plus `dotnet build Hexalith.Builds.slnx --configuration Release` -- expected: all pass.
- `python3 -m unittest tests/eng/test_dependency_graph.py tests/eng/test_dependency_handoff.py tests/eng/test_workflow_source_closure.py tests/eng/test_release_contract.py tests/eng/test_release_evidence_v2.py -v` -- expected: all pass.
- `approved=3f0e3595be693fce56a37648c0bd0f89390f5fd3`; `test "$(git -C references/Hexalith.Builds rev-parse HEAD)" = "$approved"`; `python3 eng/release_contract.py builds --root . --commit "$(git rev-parse HEAD)" --approved "$approved"` plus `actionlint` on `ci.yml`, `release.yml`, and `release-evidence.yml` -- expected: gitlink HEAD equals the recorded commit, exact identity and syntax pass.
- `DiffEngine_Disabled=true dotnet build Hexalith.FrontComposer.slnx --configuration Release` plus `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~CentralPackageVersions_WhenCatalogIsCentralized_AreInheritedFromPinnedBuilds"` -- expected: green with zero warnings.
- Inspect `_bmad-output/planning-artifacts/architecture.md` and `tests/README.md` -- expected: compatibility is authoritative structure plus affected-module builds; hashes remain provenance.
