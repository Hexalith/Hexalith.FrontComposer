---
title: 'Eliminate stale dependency version mirror failures'
type: 'bugfix'
created: '2026-08-11'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: '984b459e5cd4fc6d2625cd21f4d8219d4f0f4d1d'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** CI runs 31520364772 and 31524618072 passed the Release build, Tier 1 tests, and package-consumer validation, but dependency governance rejected both because FrontComposer duplicated `HexalithEventStoreVersion` as an exact local policy value. The immutable event-base policy made the mismatch sticky even after a later commit updated the mirror; since exact-pin governance activation, 12 of 15 non-cancelled CI failures were the same class of stale cross-module version mirror rather than product defects.

**Approach:** Stop treating the six `Hexalith*Version` catalog properties as FrontComposer-owned point-version acceptance controls. Keep the dependency graph, trusted identities, catalog structure and ownership checks, exact required package checks, delayed policy activation, affected-module Release/NuGet builds, evidence, and release handoff fail-closed.

## Boundaries & Constraints

**Always:** Preserve `frontcomposer-catalog-v1` as a closed profile with `selected_catalog_required_properties` present but empty; retain all `selected_catalog_required_packages` and `owner_checks`; add regression coverage proving compatible cross-module version movement is not rejected while meaningful structural, package, override, and build failures remain blocking; update contributor context so it no longer prescribes lockstep point-version mirroring.

**Ask First:** Removing or weakening direct package requirements, structural catalog checks, affected-module builds, exact graph/evidence validation, workflow provenance, release handoffs, or delayed activation; modifying a root-declared submodule; changing GitHub settings or dispatching a release.

**Never:** Make dependency governance advisory, use commit/fingerprint allowlists, accept missing or ambiguous catalog data, bypass affected-module validation, edit `references/**`, or repair the historical runs by fabricating evidence.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| Compatible module bump | Selected Builds catalog changes a `Hexalith*Version`; required packages, ownership, and affected build remain valid | Semantic validation and CI proceed without a local point-version policy edit | Any independent blocking control still fails normally |
| Required package regression | Selected catalog loses or changes an exact required package | Governance rejects the selecting edge with its existing targeted diagnostic | Fail closed; do not suppress or downgrade |
| Structural or build regression | Catalog/owner violates import, override, identity, graph, or affected-build rules | Existing governance remains blocking | Preserve deterministic evidence and nonzero result |

</frozen-after-approval>

## Code Map

- `eng/dependency-graph-policy.json:54` -- `frontcomposer-catalog-v1`; replace the six duplicated point-value requirements with a closed, sorted `selected_catalog_required_property_names` shape-only list while retaining the required-property value map as an empty object plus every package and ownership control.
- `eng/dependency_graph.py:36,1057,1247,1639` -- extend the closed profile schema and canonical semantic evaluator with a shape-only required-property contract: exactly one literal NuGet version, no conditional ancestor or `Choose` branch, and only an absent condition or canonical self-default condition. It must never compare these six properties to FrontComposer-owned point values.
- `tests/eng/test_dependency_graph.py:1342,1488` -- replace the six-pin preservation test with profile-schema, positive independent version-movement, and negative missing/duplicate/conditional/malformed/property-shape plus required-package regressions using the real landed FrontComposer profile.
- `_bmad-output/project-context.md:234` -- contributor guidance currently mandates lockstep selected-catalog property updates and describes the resulting sticky-red sequence.
- `.github/workflows/ci.yml:44` -- read-only boundary: the blocking graph diff, affected-module build, evidence, and handoff job remains intact.
- GitHub Actions runs `31520364772` and `31524618072` plus artifacts `9112882998` and `9114518104` -- read-only failure evidence for the regression scenario.

## Tasks & Acceptance

**Execution:**
- [x] `eng/dependency-graph-policy.json` and `eng/dependency_graph.py` -- empty the FrontComposer profile's cross-module point-value map, require all six property names through a closed shape-only contract, and preserve every required package, owner check, registry, authorization, and limit.
- [x] `tests/eng/test_dependency_graph.py` -- prove each of the six well-shaped property values can move without a local point-value edit, while missing, duplicate, conditional, `Choose`-selected, malformed, and required-package regressions fail with selecting-owner and catalog coordinates.
- [x] `_bmad-output/project-context.md` -- document that Builds re-pins do not mirror `Hexalith*Version` values locally; preserve the separate lockstep requirements for immutable release workflow coordinates when those coordinates change.

**Acceptance Criteria:**
- Given a supported Builds catalog advance changes only cross-module version properties and all retained checks pass, when dependency semantics are evaluated, then no stale FrontComposer point-version expectation fails the run.
- Given any one of the six required module-version properties is missing, duplicated, empty, malformed, placed under a conditional or `Choose` branch, or carries a noncanonical condition, when the landed FrontComposer profile evaluates the catalog, then governance fails with selecting-owner and catalog coordinates without enforcing a particular version value.
- Given a required package, structural rule, trusted identity, affected-module build, evidence, or release-handoff contract is invalid, when its existing gate runs, then it still fails closed with actionable evidence.
- Given the changed working tree and its eventual committed revision, when semantic-policy and Governance tests run, then they pass without a graph-neutral activation commit or edits under `references/**`.

## Spec Change Log

- **Review loop 1 (2026-08-11):** The three review layers independently found that an empty `selected_catalog_required_properties` map disables the only presence/shape validation for the six module-version properties. Amended the non-frozen Code Map, tasks, and acceptance criteria to require a separate closed `selected_catalog_required_property_names` schema and evaluator path plus FrontComposer-profile regressions for missing, duplicate, conditional, `Choose`-selected, malformed, and independently moving values. This avoids the known-bad state where stale point-value checks are gone but ambiguous or absent catalog selections silently pass. **KEEP:** remove all six FrontComposer-owned exact value mirrors; retain every exact required-package and owner control; preserve delayed activation, affected builds, evidence, Release handoffs, and the contributor distinction for immutable Release coordinates; reuse the working synthetic graph fixture and targeted diagnostics; make no `references/**` edits; retain the previously green dependency-graph and Governance verification commands.

## Design Notes

This removes duplicated approval data, not dependency governance. The shared Builds catalog remains the version authority; FrontComposer proves compatibility through shape-only module-property validation, exact required package/structure rules, consumer validation, and the affected module's Release/NuGet restore and build. `selected_catalog_required_property_names` expresses presence and shape without a point value; the generic required-property map stays available for future properties whose values encode an actual compatibility contract rather than a mirrored package point version.

## Verification

**Commands:**
- `python3 -m unittest tests/eng/test_dependency_graph.py` -- expected: all graph, semantic-policy, diff, and materialization fixtures pass.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Governance"` -- expected: blocking Governance lane passes.
- `git diff --check` -- expected: no whitespace errors.

**Results:**
- `python3 -m unittest tests/eng/test_dependency_graph.py` passed: 83 tests.
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "Category=Governance"` passed: 221 tests.
- `git diff --check` passed with no whitespace errors.
