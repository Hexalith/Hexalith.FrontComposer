---
title: 'Fix dependency-governance diff and materialization'
type: 'bugfix'
created: '2026-08-03'
status: 'in-review'
review_loop_iteration: 1
baseline_commit: '663a88ec647d6ea804dd3f4c900ff2a139488c50'
context:
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/references/Hexalith.Tenants/_bmad-output/project-context.md'
  - '{project-root}/references/Hexalith.Parties/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** GitHub Actions run `30793884545` fails after the primary build succeeds because the dependency-governance materializer rejects the legitimate `references/Hexalith.AI.Tools` gitlink present in every selected Hexalith.Builds tree. The same gate also compares depth-1 edge records including the always-changing FrontComposer `owner_commit`, so a root-only documentation commit falsely schedules all eight dependencies, as run `30793441242` demonstrates.

**Approach:** Preserve complete exact graph records for provenance, but compare depth-1 edges by dependency meaning rather than the root owner revision. Materialize only normalized regular-file Builds content, explicitly omitting nested gitlinks without initializing them while continuing to reject symlinks and every other unsupported mode/type.

## Boundaries & Constraints

**Always:** Validate every tree-entry path before accepting or omitting it; omit only the exact Git gitlink pair `160000 commit`; preserve exact regular-file bytes, executable modes, resource ceilings, catalog re-hashing, static policy-owned Release/NuGet commands, deterministic evidence, and at-most-once scheduling. Keep `owner_commit` in graph and diff evidence even when it is excluded from depth-1 change equality.

**Ask First:** Changing workflow structure, dependency policy/schema, approved architecture, resource limits, root or nested gitlinks, any file under `references/`, or downstream Release/Release Evidence behavior requires approval.

**Never:** Do not initialize nested submodules, materialize gitlink content, broadly ignore unsupported modes, accept symlinks, weaken path/hash/limit checks, roll dependency pointers backward, edit generated evidence, or hide a failed restore/build.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Real Builds contract tree | Regular blobs plus `.gitmodules` and a normalized `160000 commit` entry | Materialize all regular files byte-for-byte; create no nested checkout or gitlink path | A later static build that genuinely needs omitted nested content remains blocking |
| Unsafe tree entry | Symlink or any mode/type other than supported blobs and the exact gitlink pair | Materialization fails before destination extraction | Report the offending normalized path; do not partially weaken the gate |
| Root-only commit | Root revision changes while every depth-1 target and nested graph remains identical | Emit no edge changes and schedule no affected modules | Structural/schema or graph drift still fails closed |
| Genuine pointer advance | A depth-1 gitlink target changes | Record exact before/after provenance and schedule the candidate target once; descendant churn is subsumed | Missing policy, object, catalog, or build result remains blocking |

</frozen-after-approval>

## Code Map

- `references/Hexalith.{Commons,Tenants,Parties}/Hexalith.*.Standalone.slnx` -- complete owned-project Release/NuGet surfaces.
- `references/Hexalith.Tenants/{src,tests}/**` -- package-mode fallbacks and solution tests.
- `references/Hexalith.Parties/{src,tests}/**` -- path-based AppHost resources, package-built gateway test host, and tests.
- `eng/dependency_graph.py`, its policy/tests, and the three parent gitlinks -- safe diff/materialization and delayed-activation targets.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Commons` -- add the 20-project standalone solution, inventory test, and build note.
- [x] `references/Hexalith.Tenants` -- add the 17-project standalone solution; gate the six external project edges, add matching package references, and extend solution/package-governance tests and guidance.
- [x] `references/Hexalith.Parties` -- add the complete standalone solution, path-based AppHost resources, an owned package-built gateway test host, tests, and guidance.
- [x] `eng/dependency_graph.py` -- validate depth-1 root ownership and depth-2 ancestry, reject duplicate logical edges, ignore only validated depth-1 root `owner_commit` churn, and omit only path-validated `160000 commit` entries.
- [x] `eng/dependency-graph-policy.json` and `tests/eng/test_dependency_graph.py` -- select the standalone solutions and cover target shape, hostile envelopes, pointer advances, extraction, unsafe modes, and mount replacement.
- [x] Git history -- on typed local branches, commit each submodule, then make separate commitlint-validated FrontComposer policy/code and pointer-advance commits after isolated builds pass; do not push.

**Acceptance Criteria:**
- Given isolated exact module trees plus the Builds contract, when policy commands run, then every owned project (Commons 20, Tenants 17, and all Parties projects including its gateway host) builds without nested checkouts.
- Given root-only churn, hostile ownership/duplicate edges, or a genuine pointer advance, then the diff respectively schedules nothing, fails closed, or schedules the target once with full provenance.
- Given delayed policy activation, the policy/code commit precedes the separate pointer-advance commit so a later CI push can execute the new immutable-base commands rather than merely assert their strings.

## Spec Change Log

- Review patches preserve the approved architecture while closing self-referential inventory checks, runtime path-resolution coverage, source/package gateway-host selection, and sealed-evidence trust binding.

## Design Notes

Primary source/topology solutions remain canonical; standalone files are governance-only package-mode surfaces. Tenants gets package fallbacks. Parties retains runtime topology validation while using path-based Aspire resources and an owned package gateway host. Policy activation precedes pointer advancement because CI authorizes commands from the immutable event base.

Activation requires two remote merge/push boundaries: land the policy/code commit and wait for it to become the immutable base before advancing dependency pointers. Do not publish the baseline-to-pointer range as one event; that event would correctly remain governed by the old base policy.

## Verification

**Commands:**
- `python3 -m py_compile eng/dependency_graph.py` -- expected: helper parses successfully.
- `python3 -m unittest tests/eng/test_dependency_graph.py -v` -- expected: all dependency graph, diff, materialization, policy, and regression tests pass.
- `dotnet restore Hexalith.*.Standalone.slnx -p:Configuration=Release -p:UseNuGetDeps=true` then the matching Release `--no-restore` build in each isolated module tree -- expected: all three exit zero without nested content.
- Focused Commons inventory, Tenants Contracts, and Parties CI/topology/gateway test projects -- expected: standalone membership, package routing, and preserved behavior pass.
- Repository-pinned commitlint against every exact local commit message -- expected: zero violations before committing.
- `git diff --check` -- expected: no whitespace errors.

**Results:**
- Root dependency-graph suite: 67/67 passed; Python compilation and `git diff --check` passed.
- Isolated Release/package-mode builds with only regular Builds contract files materialized: Commons 20/20, Tenants 17/17, and Parties 30/30 projects built with zero warnings and zero errors; no nested gitlink content was initialized.
- Focused changed-surface tests: Commons 1/1, Tenants 26/26, and Parties 75/75 passed; Parties' source-mode gateway test project also built successfully against the real EventStore host.
- Frozen matrix rows passed in the root suite: exact nested-gitlink omission, unsafe path/mode rejection, root-only no-op scheduling, and at-most-once pointer-advance scheduling with provenance.
- Exact submodule and root commit messages passed the owning repositories' pinned commitlint CLIs before commit creation.
- Exact delayed-activation replay from policy commit `569ad3e441cd83661e1863c438644c789575c6ee` to pointer commit `1f0387e313ec0e157c5fdda4ed5a058aab121569` scheduled only Commons, Parties, and Tenants; all six static restore/build commands exited zero (`result_digest=ac0d9a062304c1026fdb936641bfaf228f8754ac85f101371a616273564c8137`).
- Adversarial review patches bind evidence to the policy root/revisions/edge ceiling, discover owned projects from disk, behaviorally test all supported Parties dependency layouts and missing-path guidance, prefer umbrella dependencies over stale nested checkouts, and retain the production EventStore host for source-mode routing tests.
