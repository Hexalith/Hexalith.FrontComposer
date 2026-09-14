---
title: 'Actions 34817507610: Restore CI and governed release readiness'
type: 'bugfix'
created: '2026-09-14'
status: 'in-progress'
route: 'dispatch'
baseline_commit: '6e064785e765102fa1a90039f06e10e0b252fdec'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/prd.md'
  - '{project-root}/_bmad-output/implementation-artifacts/spec-11-25-current-eventstore-release-identity-and-evidence.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Push CI run `34817507610` fails because package-boundary tests and dependency policy lag the pinned Builds catalog. The same SHA's Quality run `34817507190` also exposes a stale FC-NIP semantic fixture, an unreviewed analyzer-inventory delta, and deliberate fail-closed drift between current dependency gitlinks and the active EventStore runtime identity; these failures prevent an authenticated release handoff.

**Approach:** Reconcile each owned contract to current reviewed source truth, preserve all workflow authorization and publication controls, land the policy change through delayed activation, and prove the final exact `main` SHA green before any governed release attempt. If publication becomes authorized, advance the compatibility lifecycle for semantic-release `4.5.0`, approve protected deployments through GitHub's supported reviewer path, and independently verify the immutable GitHub Release and all eight NuGet packages.

## Boundaries & Constraints

**Always:** Preserve the Builds execution SHA `4eb33928a1d8c7775f97221cf9edc171db0cb5f8`, workflow bytes, eight-package inventory, AD-13/AD-15 handoffs, pack-once candidate, exact-SHA tag, immutable assets, and post-publication byte/signature verification. Preserve the user's moved EventStore and Memories working-tree checkouts and all concurrent Story 11.25 changes. Treat the first policy landing and the later active-policy run as distinct evidence.

**Never:** Forge approval receipts, weaken or skip CI/Quality/release gates, directly push packages, bypass credentials or protected-environment controls, rewrite historical EventStore evidence, include unrelated working-tree changes, or claim success from a run whose source differs from the released tag.

**Decision (2026-09-14):** Follow the governed release path. Repair and prove the exact-source workflows first, then publish only after the repository's dated Product, Release Owner, Architecture, and EventStore identity approvals genuinely exist. If those authorities remain absent, stop without creating a release rather than bypassing or weakening them.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Catalog drift | Builds supplies newer reviewed package versions | Tests and semantic policy agree with the selected catalog | A missing or unexpected package remains fail-closed |
| Delayed activation | First push is evaluated by the old base policy | Land a second unchanged-policy push before release eligibility | Do not treat the expected first AD-16 failure as release evidence |
| Current release | Final SHA has green push workflows and authorization | Governed `v4.5.0` publishes eight package/symbol pairs | Stop on version, SHA, approval, asset, or package mismatch |
| No publication authority | Halt policy or required approval remains open | No release mutation occurs | Report the exact unmet authority |

</frozen-after-approval>

## Code Map

- `tests/Hexalith.FrontComposer.Testing.Tests/PackageBoundaryTests.cs` -- reviewed point-value mirrors for package-consumer tests; update all changed selected-catalog values together.
- `eng/dependency-graph-policy.json` -- FrontComposer semantic profile and delayed-activation evaluator authority; preserve workflow digests and execution identities.
- `tests/contract-fixtures/fc-nip-command-target-identity-contract.json` -- shared SourceTools/Playwright semantic fixture; align to current PRD delivery truth, not obsolete wording.
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` -- CA1707 identifier seal; the two reviewed additions are `Wrapper_EmitsOnlyWhenItsDeclaredLevelIsEnabled` and `WrapperContracts_CoverEveryPublicWrapper`.
- `_bmad-output/contracts/frontcomposer-eventstore-approved-runtime-identity-v2.json` and `_bmad-output/implementation-artifacts/evidence/eventstore-runtime-identity-v2/**` -- concurrently modified active evidence; do not alter or stage without separately authorized current-tuple recapture.
- `Directory.Build.targets`, `src/Hexalith.FrontComposer.Contracts.UI/Hexalith.FrontComposer.Contracts.UI.csproj`, `eng/release_compatibility.py`, `docs/diagnostics/**`, and behavior-bound tests -- advance the published baseline to `4.4.0` and target release line to `v4.5` only for an authorized release.
- `.github/workflows/{ci,quality,release,release-evidence}.yml` -- verification surface; keep bytes unchanged unless exact closure reauthorization is separately approved.

## Tasks & Acceptance

**Execution:**
- [ ] Reconcile package-boundary mirrors and the four stale FrontComposer catalog-policy package values; validate the committed-object graph and focused Testing/Governance lanes.
- [ ] Replace obsolete FC-NIP fixture fragments with current D-4 completion, Stories 9.3–9.8 live proof, typed target/materiality, and server-allocated-key non-goal assertions; run both consumers.
- [ ] Review and reseal the two intended CA1707 test declarations without changing analyzer severity or exception scope.
- [ ] Keep Story 11.25 work isolated; either validate a separately approved current EventStore tuple or leave the release blocked rather than weakening its test.
- [ ] If authorized, advance the `4.5.0` release lifecycle, validate commit messages with pinned commitlint, land via delayed policy activation, and verify final push workflows.
- [ ] If authorized and green, dispatch Release, perform supported production reviews, wait for Release Evidence, and verify tag/source/assets plus all eight NuGet and symbol packages.

**Acceptance Criteria:**
- Given the final exact `main` SHA, when push workflows complete, then CI, Quality, Commitlint, CodeQL, and dependency/flaky governance are successful with authentic artifacts.
- Given valid publication authority and a green exact-source handoff, when Release completes, then immutable `v4.5.0` and all eight NuGet packages resolve to that SHA and Release Evidence succeeds.
- Given publication authority is absent, when verification completes, then no release, tag, or NuGet mutation is attempted.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The test constants remain explicit reviewed mirrors rather than becoming dynamic catalog lookups. Dependency-policy updates activate only as the base policy of a later push, so a second compliant landing is required before AD-13 can authorize release.

## Verification

**Commands:**
- Focused MTP assemblies for PackageBoundary, FC-NIP, analyzer inventory, dependency policy, and EventStore identity -- expected: every selected fact passes without suppression.
- `npm --prefix tests/e2e run test:fc-nip` and `actionlint .github/workflows/*.yml` -- expected: browserless contract and workflow syntax pass.
- `python3 -m unittest` for dependency, handoff, release contract, package, and prepublish suites -- expected: all non-publishing release gates pass.
- `gh run watch <run-id> --exit-status` for exact-source push, Release, and Release Evidence workflows -- expected: each required run succeeds before package verification.
