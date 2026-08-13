---
title: 'Fix Release Builds execution SHA drift after gitlink advance'
type: 'bugfix'
created: '2026-08-13'
status: 'done'
review_loop_iteration: 1
baseline_commit: 'd35548251aefad8e235b2fe69a4f9611fb741173'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/project-docs/deployment-guide.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Release run [31716385563](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/31716385563) failed `verify-source` because `references/Hexalith.Builds` is `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38` while release/CI still approve `0a3508b3e5685602dc13983c5371cab7fabaf015`. A same-commit pin+auth bump would still soft-defer AD-13 `create-ci` (policy is loaded from the push base).

**Approach:** Two-phase AD-13 landing: (1) commit policy rows that pre-authorize the future `99d5a46…` caller closures while workflows stay on `0a3508…`; (2) after that policy is on the branch tip used as push base, commit the CI/release/evidence pin move to `99d5a46…` plus docs. Leave the Builds gitlink and EventStore tip unchanged.

## Boundaries & Constraints

**Always:** End state keeps gitlink, `uses:@`, `builds-execution-sha`, `BUILDS_EXECUTION_SHA` / `HEXALITH_BUILDS_EXECUTION_SHA`, and evidence checkout `ref` identical lowercase 40-hex = `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`. Phase 1 lands only `eng/dependency-graph-policy.json` authorization rows (retain historical rows). Phase 2 lands workflow pins + docs and must not change policy again unless draft-evaluator proves a mismatch. Preserve package count `8`, freeze/publish fail-closed defaults, and AD-13/AD-15 shape.

**Ask First:** Resetting the Builds gitlink to `0a3508…`, enabling publication / touching `NUGET_API_KEY` or `production`, changing EventStore or other submodule tips, dropping historical evaluator rows, or collapsing both phases into one commit.

**Never:** Do not authorize a real release, mutate secrets/environments, invent a different Builds SHA, weaken `validate_builds_identity`, or claim Phase-1 push emits the new-pin AD-13 handoff.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Phase 1 pre-auth | Policy adds future `99d5a46…` rows; workflows still `0a3508…` | Current `0a3508…` closures still authorize; future caller blobs authorize under that policy object | Missing future row → Phase 2 `create-ci` soft-defers |
| Phase 2 lockstep | Pin commit tip; push base already has Phase 1 policy | `release_contract.py builds` exits 0; `create-ci` finds exactly one matching `ci` row and uploads AD-13 | Unequal pins → `ContractError`; missing base auth → soft-defer / no artifact |
| Stale pin retained | Workflows still `0a3508…` while gitlink is `99d5a46…` | Gate rejects (run 31716385563) | Fail closed — do not bypass |
| Partial env pin | `uses:@`/`builds-execution-sha` updated but `BUILDS_EXECUTION_SHA` or `HEXALITH_BUILDS_EXECUTION_SHA` left stale | Focused governance test fails before merge | Fail closed |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` L71–81, L177–185 — `event_base` / `PUSH_BEFORE` becomes `--policy-commit` for `draft-evaluator`/`create-ci` (zero base falls back to candidate).
- `.github/workflows/release.yml` — L17 `BUILDS_EXECUTION_SHA`; L149–154 `eng/release_contract.py builds --approved "$BUILDS_EXECUTION_SHA"`; prepare `ref` (~L233); `HEXALITH_BUILDS_EXECUTION_SHA` (~L286); `uses:…domain-release.yml@` (~L321); `builds-execution-sha:` (~L329). All five must move together in Phase 2.
- `.github/workflows/ci.yml` L25 — `domain-ci.yml@` pin (Phase 2).
- `.github/workflows/release-evidence.yml` ~L233 — Builds checkout `ref` (Phase 2).
- `eng/release_contract.py` `validate_builds_identity` L320–329 — gitlink + `uses:@` + `builds-execution-sha` must equal `--approved` (reads workflow via `git show <commit>:…`).
- `eng/dependency_handoff.py` — `create_ci_handoff_from_evidence` requires active-policy commit == non-zero push base (L162); `draft-evaluator` loads policy at `--policy-commit` (L565–567); `require_evaluator_authorized` exact one-row match.
- `eng/dependency-graph-policy.json` — Phase 1 append `evaluator_authorizations.{ci,release,post_release}` for **future** caller blobs at reusable commit `99d5a46…` without removing historical `0a3508…` rows; keep ordinal uniqueness.
- Phase 1 authoring recipe (local, uncommitted): temporarily set the three workflow pins to `99d5a46…` → `python3 eng/dependency_handoff.py draft-evaluator --stage … --caller-commit <temp-tree> --policy-commit <temp-or-HEAD>` → append resulting projections into policy → restore workflows to `0a3508…` → commit **policy only**. Domain workflow/action bytes are identical at `0a3508…` and `99d5a46…`; only commit coordinates and caller workflow blobs change.
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` `ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate` (~L584) — today asserts only `uses:@` == `builds-execution-sha`; extend in Phase 2 so every in-file Builds 40-hex coordinate (`BUILDS_EXECUTION_SHA`, `HEXALITH_BUILDS_EXECUTION_SHA`, prepare `ref`, `uses:@`, `builds-execution-sha`) equals one shared SHA and equals `git ls-tree HEAD references/Hexalith.Builds`.
- `tests/eng/test_release_contract.py` `test_builds_identity_rejects_mismatched_workflow_input_or_gitlink` — keep; do not relax.
- Docs (Phase 2 only): `_bmad-output/project-context.md` ~L248–266, `_bmad-output/planning-artifacts/architecture.md`, `_bmad-output/project-docs/deployment-guide.md` (also replaces stale `3ac633…` citations).
- Read-only: `references/Hexalith.Builds` gitlink stays `99d5a46…` (EventStore catalog 3.94.0 already selected). Precedent `e93b351c` lacked AD-13 pre-auth.

## Tasks & Acceptance

**Execution:**
- [x] Phase 1 — `eng/dependency-graph-policy.json` only: append `ci`/`release`/`post_release` rows pre-authorizing the future `99d5a46…` caller closures (draft via temporary pin edits, then restore workflows); retain historical rows.
- [x] Phase 1 verify: with workflows still on `0a3508…`, current closures remain authorized; drafted future projections match the new rows under the Phase-1 policy object.
- [x] Phase 2 — `.github/workflows/release.yml`: move all five Builds coordinates to `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`.
- [x] Phase 2 — `.github/workflows/ci.yml` and `.github/workflows/release-evidence.yml`: pin `domain-ci.yml@` and Builds `ref` to the same SHA.
- [x] Phase 2 — `tests/…/CiGovernanceTests.cs`: assert all five `release.yml` Builds coordinates equal each other and the Builds gitlink.
- [x] Phase 2 — docs (`project-context.md`, `architecture.md`, `deployment-guide.md`): record the new approved identity where old SHAs are documented.
- [x] Phase 2 verify: builds/manifest/unittest/governance commands below; `draft-evaluator` for `ci`/`release`/`post_release` against Phase-2 tip with `--policy-commit` = Phase-1 commit reports `authorized_draft: true`.

**Acceptance Criteria:**
- Given only Phase 1 is landed, when workflows still name `0a3508…`, then current CI/release closures still authorize and the Phase-1 policy object already contains exactly one authorizing row per stage for the future `99d5a46…` caller blobs.
- Given Phase 2 is landed with Phase 1 as its push base, when `release_contract.py builds --commit <phase2> --approved 99d5a46…` runs, then it exits 0 and gitlink remains `99d5a46…`.
- Given Phase 2 `release.yml`, when the extended governance test runs, then a stale `BUILDS_EXECUTION_SHA` or `HEXALITH_BUILDS_EXECUTION_SHA` fails closed while equal coordinates pass.
- Given Phase 2 push base = Phase 1, when `draft-evaluator`/`create-ci` authorization is projected for the Phase-2 caller blobs, then each stage matches exactly one policy row (AD-13 emission eligible on that push).
- Given this work alone, when publication controls are inspected, then freeze remains fail-closed and no secret/environment/release authorization was touched.

## Spec Change Log

- 2026-08-13 review loop 1 (intent_gap): Human chose Option 1 two-phase AD-13 landing. Frozen Approach/Always/Never/Matrix now require Phase-1 policy pre-auth before Phase-2 pin move; same-commit pin+auth is Ask First. KEEP: target SHA `99d5a46…`, no gitlink reset, historical auth rows retained, no publication/secrets.

## Design Notes

Phase 1 must ship and become the non-zero push base before Phase 2. Do not open Phase 2 until Phase 1 is committed on the integration branch tip you will push from. Local recipe uses a temporary index/commit-tree for `draft-evaluator` caller commits; never leave temporary pin edits in Phase 1’s committed tree.

## Verification

**Commands:**
- Phase 1: `python3 eng/dependency_handoff.py draft-evaluator` for `ci`/`release`/`post_release` with `--policy-commit <phase1>` against future caller trees → `authorized_draft: true`; and against current `0a3508…` workflows → still authorized under Phase 1.
- Phase 2: `python3 eng/release_contract.py builds --root . --commit <phase2> --approved 99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38` → exit 0
- `python3 eng/release_contract.py manifest --root . --manifest tools/release-packages.json --expected-count 8` → exit 0
- `python3 -m unittest tests.eng.test_release_contract -v` → all pass
- `python3 eng/dependency_graph.py --root . validate --commit <phase2>` → exit 0
- `DiffEngine_Disabled=true dotnet test tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj --configuration Release --filter "FullyQualifiedName~ReleaseWorkflow_DelegatesToReusableDomainReleaseAfterCiGate"` → pass
- `git ls-tree <phase2> references/Hexalith.Builds` → still `99d5a46c3d0db007b2d2f9c5e277a7d2c32b9a38`
