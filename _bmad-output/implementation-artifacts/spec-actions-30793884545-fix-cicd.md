---
title: 'Fix dependency-governance diff and materialization'
type: 'bugfix'
created: '2026-08-03'
status: 'in-progress'
review_loop_iteration: 0
baseline_commit: '874fe13bbecc0bbebdbe765b081782381c93b3fa'
context:
  - '{project-root}/_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
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

- `eng/dependency_graph.py` -- owns logical edge diffing, bounded Builds contract-tree extraction, exact module materialization, and policy-authorized affected builds.
- `tests/eng/test_dependency_graph.py` -- synthetic Git fixtures for edge diff/cascade behavior, unsafe modes, exact-byte extraction, and isolated gitlink handling.
- `.github/workflows/ci.yml` -- read-only caller proving the failed `diff` to `run-affected` execution path; no workflow edit is expected.
- `_bmad-output/contracts/shared-catalog-dependency-governance-2026-07-19.md` -- approved FC-DEP-1 invariants: targeted pointer-change cost, regular-file contract trees, and no nested initialization.

## Tasks & Acceptance

**Execution:**
- [ ] `eng/dependency_graph.py` -- use a deterministic depth-aware equality projection that ignores only the depth-1 root `owner_commit` for change detection while retaining full before/after edge records; normalize tree paths, omit only `160000 commit` gitlinks, and reject all other non-regular entries.
- [ ] `tests/eng/test_dependency_graph.py` -- add regression fixtures for root-owner-only revision churn and a real-shaped Builds tree containing `.gitmodules`, regular blobs, and a nested gitlink; retain genuine pointer-advance, exact-byte, symlink, and resource-limit coverage.

**Acceptance Criteria:**
- Given the exact `f25c4493` to `874fe13b` run revisions, when dependency evidence is replayed and affected modules execute, then contract-tree materialization no longer fails on `references/Hexalith.AI.Tools` and every scheduled static command remains blocking.
- Given two collected graphs whose root revisions differ only by an unrelated regular file, when they are diffed, then `changes` and `affected_modules` are empty without removing `owner_commit` from either graph.
- Given a genuine root gitlink advance, when graphs are diffed, then the target is scheduled exactly once and complete before/after provenance remains reviewable.

## Spec Change Log

## Design Notes

Depth-1 `owner_commit` is the FrontComposer root revision and changes on every commit; it is provenance, not dependency drift. Depth-2 `owner_commit` remains change-significant so descendant changes can be recorded and subsumed when their owning root dependency advances. Gitlinks are normalized and recognized, then omitted from the regular-file projection, matching the approved no-nested-initialization boundary without turning unsupported modes into an allowlist.

## Verification

**Commands:**
- `python3 -m py_compile eng/dependency_graph.py` -- expected: helper parses successfully.
- `python3 -m unittest tests/eng/test_dependency_graph.py -v` -- expected: all dependency graph, diff, materialization, policy, and regression tests pass.
- Replay `python3 eng/dependency_graph.py diff --event push --event-base f25c4493ff2e26f38c641394ab699309f03679be --candidate 874fe13bbecc0bbebdbe765b081782381c93b3fa`, then feed its evidence to `run-affected` in a fresh temporary output root -- expected: successful result for all eight policy dispositions with no unsupported gitlink error.
- `git diff --check` -- expected: no whitespace errors.
