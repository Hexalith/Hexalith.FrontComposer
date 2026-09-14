---
title: 'Actions 34817507610: Restore CI and governed release readiness'
type: 'bugfix'
created: '2026-09-14'
status: 'done'
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
- [x] Reconcile package-boundary mirrors and the four stale FrontComposer catalog-policy package values; validate the committed-object graph and focused Testing/Governance lanes.
- [x] Replace obsolete FC-NIP fixture fragments with current D-4 completion, Stories 9.3–9.8 live proof, typed target/materiality, and server-allocated-key non-goal assertions; run both consumers.
- [x] Review and reseal the two intended CA1707 test declarations without changing analyzer severity or exception scope.
- [x] Keep Story 11.25 work isolated; either validate a separately approved current EventStore tuple or leave the release blocked rather than weakening its test.
- [x] If authorized, advance the `4.5.0` release lifecycle, validate commit messages with pinned commitlint, land via delayed policy activation, and verify final push workflows.
- [x] If authorized and green, dispatch Release, perform supported production reviews, wait for Release Evidence, and verify tag/source/assets plus all eight NuGet and symbol packages.

**Acceptance Criteria:**
- Given the final exact `main` SHA, when push workflows complete, then CI, Quality, Commitlint, CodeQL, and dependency/flaky governance are successful with authentic artifacts.
- Given valid publication authority and a green exact-source handoff, when Release completes, then immutable `v4.5.0` and all eight NuGet packages resolve to that SHA and Release Evidence succeeds.
- Given publication authority is absent, when verification completes, then no release, tag, or NuGet mutation is attempted.

## Implementation Notes

- Updated the explicit package-test mirrors and FrontComposer semantic-policy values to the selected Builds catalog. The System.CommandLine presence-only policy tests now derive the selected version while retaining missing/duplicate fail-closed checks.
- Replaced the obsolete FC-NIP PRD assertions with the completed D-4, Stories 9.3–9.8 live-proof, typed target/materiality, and server-allocated-key non-goal facts.
- Resealed only the two reviewed CA1707 test declarations. Analyzer severity and exception scope are unchanged.
- Publication authorization is absent: the active identity records zero receipts and `migrationApprovalClaimed: false`; the current Builds/EventStore gitlinks also differ from the sealed tuple. The conditional lifecycle and release tasks therefore completed by stopping before any commit, push, tag, release, package, deployment, or remote mutation.
- Story 11.25 evidence, moved submodule checkouts, workflow bytes, release lifecycle files, and package inventory were not changed by this implementation.

## Spec Change Log

- 2026-09-14: Reconciled local CI/governance contracts, recorded verification, and retained the governed release block because publication authority is absent.

## Review Triage Log

| Finding | Verdict / route | Evidence |
| --- | --- | --- |
| verification-gap-01 | **medium / defer** | Pre-verified: the dependency-output classifier has no regression distinguishing a real project output root from a nested source path named `bin`; reverting it to broad any-segment filtering leaves the cited tests green. This belongs to the preserved concurrent Story 11.25 evidence work. |
| blind-01 | **high / defer** | Verified in `eng/eventstore_runtime_evidence.py`: every untracked file beneath a project-adjacent `bin`/`obj` root is excluded before the clean/build/no-build lane, so an arbitrary stale assembly, plug-in, or build input can escape the pre-run manifest. This is preserved concurrent Story 11.25 work. |
| blind-02 | **high / defer** | Verified: generated-output filtering runs before the untracked-symlink check, so an ignored project `bin`/`obj` symlink can be removed from `relevant_untracked` and redirect subsequent output outside the checkout. This is preserved concurrent Story 11.25 work. |
| blind-03 | **medium / defer** | Verified: `_capture` reads supplied manifest bytes and then calls a validator that independently rereads the path; a replace-between-reads race can validate different bytes and later restore the first bytes before the final equality check. This is preserved concurrent Story 11.25 work. |
| blind-04 | **low / defer** | Verified: the readiness loop accepts `/alive` after `/health` fails and records `health.readiness.succeeded` without recording which endpoint passed. Later authenticated probes still protect the final verdict, but the readiness observation can be false. This is preserved concurrent Story 11.25 work. |
| blind-05 | **high / defer** | Verified: all four authorization controls send only a malformed bearer token. An anonymously accessible endpoint that rejects malformed authorization syntax could satisfy the controls, so anonymous requests need independent negative probes. This is preserved concurrent Story 11.25 work. |
| blind-06 | **medium / defer** | Verified: the documented live-lane Bash block has no fail-fast guard or short-circuiting, so a failed manifest creation can be followed by report deletion and later lane commands. The documentation is preserved concurrent Story 11.25 work. |
| blind-07 | **low / defer** | Verified: `docs/reference/pact-contracts.md` still says `reviewed: 2026-09-12` although the documented current lane and capture were rewritten on 2026-09-14. The documentation is preserved concurrent Story 11.25 work. |
| blind-08 | **maybe-false / reject** | The outcome text calls `a32cb422...` the “current Builds catalog” while the active checkout now points elsewhere, but the paragraph can also be read as describing the sealed Story 11.25 capture. If real this is only low-severity wording ambiguity, so the workflow rejects it rather than deferring it. |
| blind-09 | **false / reject** | The two release tasks are explicitly conditional; authorization evaluated false and the implementation notes record completion by stopping before every release mutation. The proposed remedy would edit this build spec, which the review workflow also forbids. |
| blind-10 | **high / defer** | Verified in the moved EventStore checkout: Story 8.3 says to implement sections 5–8 exactly while also forbidding public contracts and registration, but section 5.2 specifies a public registration/options surface. This is separately owned submodule planning work. |
| blind-11 | **high / defer** | Verified: assigned V046–V048 require collision recovery and conditional reservation behavior while Story 8.3 forbids provider wiring and durable lifecycle behavior; calling the seams test-only does not reconcile the normative vector outcomes. This is separately owned submodule planning work. |
| blind-12 | **high / defer** | Verified: implementing sections 14–15 exactly includes public/admin/export/provider/no-leak surfaces assigned to later vectors, while Story 8.3 expressly excludes those surfaces and V127/V128. This is separately owned submodule planning work. |
| blind-13 | **medium / defer** | Verified: inherited V001–V003, V004–V048, V135–V136, and V138 total 51 vectors, but the prescribed command requires only 48 tests; the execution-manifest task helps traceability but the count gate alone cannot prove every assigned vector ran. This is separately owned submodule planning work. |
| blind-14 | **medium / defer** | Verified: the Story 8.2 approval makes a scoped patch digest authoritative while locating its only named bytes under an absolute `/tmp/...` path. The file exists on this host but the packet has no repository-portable derivation that another checkout can reproduce. This is separately owned submodule evidence work. |
| blind-15 | **maybe-false / defer** | Verified that the Memories memlog retains a malformed question immediately followed by an explicit correction and replacement. No parser contract was found proving the correction marker is honored, so the duplicate-open-question risk needs owner/parser validation in the moved checkout. |
| blind-16 | **high / defer** | Verified: AD-16 requires only one sampled pre-deletion record per physical target to be absent, which cannot demonstrate that all other records or copies in that target were purged even though the decision claims complete erasure. This is separately owned Memories architecture work. |
| blind-17 | **high / defer** | Verified: AD-16 allows confirmed credential revocation instead of a target-local generation compare, while a provider rejecting new authentication does not itself cancel a write already admitted on an established session. This is separately owned Memories architecture work. |
| blind-18 | **high / defer** | Verified: AD-16 describes completion evidence and the irreversible tombstone as distinct appends linked by reference and guarded by a prior CAS, but does not require one atomic append; a crash between writes can contradict the claim that they cannot disagree. This is separately owned Memories architecture work. |
| edge-01 | **high / defer** | Verified; same generated-output-symlink root cause as `blind-02`: a project output-root symlink is filtered out before symlink rejection and can redirect build outputs externally. This is preserved concurrent Story 11.25 work. |
| edge-02 | **high / defer** | Verified; same generated-output trust root cause as `blind-01`: a project can explicitly consume an untracked file located under its real `bin`/`obj` tree, but the pre-run manifest omits it unconditionally. This is preserved concurrent Story 11.25 work. |
| edge-03 | **low / reject** | Redirected MSBuild output outside a direct project `bin`/`obj` root is not silently omitted: it remains an untracked dependency input and fails the capture. Supporting arbitrary custom output roots would add complex evaluation for an uncommon availability-only case. |
| edge-04 | **medium / defer** | Verified: regenerated `project.assets.json` is checked for symlink components and then opened by path in a separate operation, so a local replace-between-check-and-read race can redirect the graph traversal. This is preserved concurrent Story 11.25 work. |
| edge-05 | **medium / defer** | Verified; same supplied-manifest replace-between-reads root cause as `blind-03`. This is preserved concurrent Story 11.25 work. |
| edge-06 | **medium / defer** | Verified; same non-fail-fast documented live-lane root cause as `blind-06`. This is preserved concurrent Story 11.25 work. |
| edge-07 | **high / defer** | Verified; same non-atomic completion-evidence/tombstone root cause as `blind-18` in the moved Memories architecture. |
| edge-08 | **false / reject** | The semantic policy changes four stale version values/families: Localization, TimeProvider, Immutable, and the shared Verify version applied to both `Verify` package IDs. Five package IDs do not imply five distinct stale values. |
| edge-09 | **false / reject** | `git diff HEAD` confirms this implementation did not touch the concurrent Story 11.25 source, evidence machinery, documentation, or submodule contents; those files appear only because the approved spec baseline predates the preserved concurrent commit and moved checkouts. |

## Design Notes

The test constants remain explicit reviewed mirrors rather than becoming dynamic catalog lookups. Dependency-policy updates activate only as the base policy of a later push, so a second compliant landing is required before AD-13 can authorize release.

## Verification

**Commands:**
- Focused MTP assemblies for PackageBoundary, FC-NIP, analyzer inventory, dependency policy, and EventStore identity -- expected: every selected fact passes without suppression.
- `npm --prefix tests/e2e run test:fc-nip` and `actionlint .github/workflows/*.yml` -- expected: browserless contract and workflow syntax pass.
- `python3 -m unittest` for dependency, handoff, release contract, package, and prepublish suites -- expected: all non-publishing release gates pass.
- `gh run watch <run-id> --exit-status` for exact-source push, Release, and Release Evidence workflows -- expected: each required run succeeds before package verification.

**Results:**
- Release builds succeeded with zero warnings/errors for Testing and SourceTools; focused MTP runs passed PackageBoundary 5/5, FC-NIP 7/7, analyzer governance 2/2, and central catalog governance 1/1.
- Dependency policy passed 99/99; committed-object validation succeeded for all seven selectors. Browserless FC-NIP passed 3/3.
- Dependency handoff/release suites passed 57/57; package/prepublish suites passed 60/60. `actionlint`, JSON parsing, Python compilation, and `git diff --check` passed.
- The current-tree EventStore identity gate failed closed as required: current committed runtime inputs and dependency provenance differ from the sealed v2 tuple, local AppHost `obj`/`nr.db*` outputs are present, dependency `node_modules` inputs are present, and the active identity has no approval receipts. No remote workflow or publication command was run.
