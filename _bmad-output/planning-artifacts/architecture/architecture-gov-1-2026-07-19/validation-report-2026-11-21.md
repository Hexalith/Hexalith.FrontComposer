# FrontComposer architecture validation

21 November 2026 · GOV-1 dependency provenance and publication governance

> **Verdict: revision required before G-8 conformance can be assessed against this spine.** The three independent reviews identified 7 High, 6 Medium and 2 Low documentation findings after consolidation. None was Critical. The spine passed deterministic lint, but implementers following its handoff, approval and evidence rules can still reach different results on the same release attempt.

The review covered the working GOV-1 spine and its planning and runtime companions. The architecture documents were kept intact. In the findings below, line numbers refer to those reviewed files. A High finding can prevent a supported transition, change an incident disposition, or cause conforming producers and verifiers to reject one another’s evidence.

## High findings

### 1. Define how a sealed Platform revision can advance

AD-12 requires one prior-base policy to check both sides of a change. AD-12a admits exactly one Platform owner commit. A policy sealed to A rejects candidate B. A policy already sealed to B rejects base A. The current collector follows that sequence. The exact-commit failure reproduced below is one observed instance of pin drift. The contract also needs a transition that works when starting from matching pins.

**Decision needed.** Define an owner-reviewed, two-revision transition that preserves prior authorization. Show one accepted A → B change and refusals for a third revision and for self-authorization. Spine lines 399–409, 475–486 and 511–520. [Rubric RW-01](reviews/review-rubric-validate-2026-11-21.md) · [Collector](../../../../eng/dependency_graph.py)

### 2. Reconcile the G-8 packet with the evidence-storage decision

The implementation gate requires committing raw check outputs, reviewer reports, sidecars and the packet through a protected PR. PRD §7 sends raw outputs to CI or local scratch and records run references in the story or spec. The planning architecture cites both instructions. The G-3 record-ref decision has a different scope.

**Decision needed.** Product and Release owners should select the authoritative storage and retention rule for this packet. Specify what a future verifier can retrieve and hash, including after CI artifacts expire. Spine lines 1180–1204 and 1245–1263, planning lines 345 and 360–368, PRD line 582. [PRD](../../prd.md) · [Rubric RW-02](reviews/review-rubric-validate-2026-11-21.md)

### 3. Specify which bytes each digest covers

AD-5 and the conventions table prescribe canonical JSON for every digest. Other clauses require the exact bytes of catalog files, policy blobs and archived evidence. On this commit, the same policy gives SHA-256 `ca0b7b72…` over the Git blob and `7a8cadff…` after AD-5 JSON serialization. A verifier choosing the other rule rejects the policy.

**Correction.** List the hash input for each field. Preserve raw-byte identity for files, artifacts and policy blobs. Apply canonical JSON to the defined structured values, including the graph digest. Include both results as a regression example. Spine lines 195–207, 215–219, 399–403 and 1053–1056. [Current-fit CF-01](reviews/review-current-fit-validate-2026-11-21.md)

### 4. Give the CI handoff its exact caller shape

AD-13 refers to the AD-12 policy shapes. The policy caller has repository, workflow path and blob hash. The implemented CI handoff adds the candidate commit, checks that commit, and hashes the four-member caller. A producer following the three-member definition cannot satisfy the implemented handoff validator.

**Correction.** Define the wire caller and policy lookup projection separately. Pin a shared fixture showing the exact caller members, candidate equality, and digest. Spine lines 411–422 and 550–562. [Handoff producer and verifier](../../../../eng/dependency_handoff.py) · [Divergence review](reviews/review-divergence-validate-2026-11-21.md)

### 5. Apply conformance equalities to the stage that owns them

The packet’s equality map applies Release evaluator identity to every handoff. The closed push-CI handoff carries the CI evaluator. AD-13 allows CI and Release to use different Builds pins. A valid CI handoff can therefore fail the packet check before Release evidence is considered.

**Correction.** Bind the CI handoff to the CI evaluator, the Release handoff to the Release evaluator, and the subsequent verification to the authenticated Release attempt. Keep the shared candidate and policy checks. Test a passing case with different accepted CI and Release pins. Spine lines 550–565 and 1233–1242. [Divergence review](reviews/review-divergence-validate-2026-11-21.md)

### 6. Represent a skipped publisher without inventing an approval

A gate-frozen attempt requires a skipped publisher and can complete without a deployment review. AD-18 nevertheless requires a nonempty set of production approvers containing an authorized Release Owner. The required G-8 fixture cannot produce that evidence for a job that never requested approval.

**Correction.** Define the ledger representation for a verified skip, separately from approval evidence that was required but could not be retrieved. Keep verified Release Owner approval mandatory whenever the protected publisher runs. Spine lines 676–679, 700–717, 896–911 and 1223–1229. [Divergence review](reviews/review-divergence-validate-2026-11-21.md)

### 7. Give a started publication one permanent incident disposition

Suppose the marker says publication started, the first write failed before any external bytes appeared, and a required archive is missing. The disposition table supports a partial-publication incident. The precedence rule prioritizes partial publication only when external evidence exists, then classifies a missing archive as `missing-artifact`. The recovery rule also uses the start marker. Two ledgers can assign different permanent outcomes.

**Correction.** Publish one ordered disposition rule covering the marker, observed effects and missing evidence. Use the same cases in the classifier, ledger and recovery fixtures. Spine lines 670–682, 714–745 and 783–790. Counted once across two reviews. [Rubric RW-03](reviews/review-rubric-validate-2026-11-21.md) · [Divergence review](reviews/review-divergence-validate-2026-11-21.md)

## Medium and Low findings

| Severity | Finding and recommended correction |
|---|---|
| Medium | **Deferred CI handoff.** AD-15 names the sentinel file but leaves its exact JSON members and coordinate checks open. Define the valid payload and the outcome for malformed input. Spine lines 613–623, 716–731. |
| Medium | **Graph diff identity.** AD-4 admits self-references. In a root self-reference at a different revision, AD-8’s logical key can represent edges at two depths. Specify an unambiguous key or an explicit refusal and test both sides of the diff. Spine lines 157–179, 275–283. |
| Medium | **Schema fingerprints.** The runtime survey implies a single encoding and says a mismatch blocks side effects. Document the runtime JSON and SourceTools text algorithms and which negotiation results permit compatible operations. Runtime architecture lines 341 and 350. |
| Medium | **Quickstart registration.** The decorated stub provides the command service. EventStore integration registers the query service. Document the calls an adopter needs to resolve each interface. Runtime architecture lines 70–72. |
| Medium | **Active navigation.** Module tiles compute active state from the route and emit `aria-current`. The runtime survey still attributes selection to `NavLinkMatch`. Document the current route-matching and no-match behavior. Runtime architecture line 76. |
| Medium | **QueryRequest migration.** Both companions present 3.0.0 as a future removal target. The reviewed source still exposes the obsolete flat API and the repository records releases after 3.0.0. Record actual availability and mark the previous target as historical pending an owner decision. Planning line 41 and runtime line 353. |
| Low | **Diagnostic IDs.** The two-band summary omits IDs used by other owners, including MCP HFC4001. Link the published diagnostic registry and use its allocation. Runtime architecture line 355. |
| Low | **Owner-decision register.** The GOV-1 spec and owner companion still ask for the stop observation and runbook tabletop. Reference the dated November receipts and retain the separate incident-preservation and acceptance work. |

The reviews give the source locations and disposition for each item. [Rubric](reviews/review-rubric-validate-2026-11-21.md) · [Current fit](reviews/review-current-fit-validate-2026-11-21.md) · [Producer and verifier consistency](reviews/review-divergence-validate-2026-11-21.md)

## Implementation and release status

| Check | Result on November 21 |
|---|---|
| Committed dependency graph | **Failed.** The validator stopped at the Platform owner pin. The comparison below also shows that the Builds pin differs from the policy. The validator stopped before qualifying the remaining inputs. |
| Graph coverage | AD-1 calls for the direct gitlinks of every selected root module. The collector and acquisition code keep the direct CiCd edge and omit its nested inventory when CiCd has no consumer projection. Platform’s projected inventory is traversed. The spine documents this open implementation difference. |
| Publication | The protected FrontComposer workflow still checks out candidate code and runs Semantic Release (NC-34). G-8 requires a read-only builder and a candidate-free publisher. For release evidence, the current producer writes manifest v3 and release-verification handoff v1. G-8 calls for v4 and v3 respectively. The separate **push-CI handoff** is v1 in both descriptions. |
| Owner decisions | G-3 still needs qualification of an exact runtime candidate and three role decisions. G-8 still needs implementation, evidence and acceptance. The recorded November 19 readbacks showed the publication switch at literal false. This validation used that dated receipt and made no live settings or release assessment. |

| Input | Sealed policy in FrontComposer HEAD | Gitlink recorded by HEAD | Local submodule checkout |
|---|---|---|---|
| Hexalith.Platform | `9c39cd58` | `54d90be3` | `abb62da8` |
| Hexalith.Builds | `6548f248` | `70ce9cb3` | `e1380b81` |

Reproduce the dependency result from the FrontComposer root. The command exited 1.

```sh
python3 eng/dependency_graph.py validate --commit 61dcd07fb412acf2feb66d8ee61513c16ef6ec4d
```

```text
github.com/hexalith/hexalith.platform@54d90be3b661591048640d3585a8eab4aea0e534: consumer source projection owner pin changed
```

Sources include the [dependency policy](../../../../eng/dependency-graph-policy.json), [graph engine](../../../../eng/dependency_graph.py), [protected workflow](../../../../.github/workflows/release-publish.yml), [manifest producer](../../../../eng/release_evidence.py), [handoff producer](../../../../eng/dependency_handoff.py), [NC-35 receipt](nonconformance-register-2026-09-08.md#nc-35-stop-observation-on-2026-11-19) and [milestone gates](../../prd.md#01-v10-readiness-milestone-gates).

## Checks and reviewed sources

| Check | Evidence |
|---|---|
| BMad spine lint | Zero findings. All 20 decision IDs, including AD-12a, match the committed sequence. |
| Document links | All 73 local references checked across the three documents resolve, including Markdown heading fragments. |
| Focused regressions | 37 release-contract tests, two graph-inventory omission tests and one projection-owner refusal test passed. These cover the current implementation. |
| Inputs | The working architecture texts were read against the FrontComposer source at `61dcd07fb412acf2feb66d8ee61513c16ef6ec4d`. All ten direct submodule worktrees differ from that commit’s gitlinks. |
| Scope of evidence | Static source, recorded owner decisions and focused local tests. Live GitHub environment protection, NuGet publication, the full runtime suite and an end-to-end G-8 conformance run were outside this assessment. |

| Reviewed document | SHA-256 of working file (CRLF) |
|---|---|
| [GOV-1 decision spine](ARCHITECTURE-SPINE.md) | `d38994cceeb5cd9e0a0682ddb3b6ff642a53b3cbfe1c13587b7f91185dfeab07` |
| [Planning architecture](../../architecture.md) | `dee18d9072f5fa46b4bb6b2cb7da6f5d2a72fde2cd52d348baec372d326f263f` |
| [Runtime architecture](../../../project-docs/architecture.md) | `10f799777c1db7d9a2d57b87056c6075b5277be28a5aa3c77a7fbf0e641a91cd` |

The spine’s LF-normalized Git blob has SHA-256 `e7550d16adb444ed92070fa6fcf877c49419f3a02a7bcf2205e98de0406a31f7`. The three independent reviews are available alongside this report. [Rubric](reviews/review-rubric-validate-2026-11-21.md) · [Current fit](reviews/review-current-fit-validate-2026-11-21.md) · [Producer and verifier consistency](reviews/review-divergence-validate-2026-11-21.md)
