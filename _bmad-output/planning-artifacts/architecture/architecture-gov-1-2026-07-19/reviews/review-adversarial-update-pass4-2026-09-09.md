# Adversarial Divergence Review — GOV-1 Update, Pass 4

- Review date: 2026-09-09
- Lens: adversarial divergence, final recheck
- Artifact: `ARCHITECTURE-SPINE.md`
- Spine SHA-256: `a0efa3e02e15cfc1a4bbd73068cdd55ab38342136345d29883bec8b5f2ff9c86`
- Deterministic lint: **PASS**, 0 findings
- Verdict: **BLOCK — 0 Critical, 3 High, 0 Medium findings**

The seven pass-3 findings are directly closed. Three remaining High gaps concern attempt-bound
fallback approval and the authenticated implementation-gate packet. No new Critical architecture
contradiction was found.

## Pass-3 closure retest

| Pass-3 finding | Status | Pass-4 result |
| --- | --- | --- |
| ADV3-C1, fallback PR/main recursion | Closed, with ADV4-H1 residual | Approval moved out of `main` to the same Release run's authenticated production deployment review. |
| ADV3-C2, incident writer versus sole publisher | Closed | AD-12 registers `incident_recovery`; AD-18 grants only reserved-namespace `contents: write` and expressly withholds product/NuGet/OIDC authority. |
| ADV3-H1, provenance spelling | Closed | AD-19 pins the case-preserved GitHub/OIDC values and exact signed SLSA paths, including builder and invocation IDs. |
| ADV3-H2, cross-platform archive paths | Closed | All trust-bearing jobs are fixed to GitHub-hosted `ubuntu-24.04`; Windows/macOS/self-hosted variants are ineligible. |
| ADV3-H3, multi-owner signoff | Closed | Approval and signoff paths now include normalized signer/review identity with per-owner uniqueness and append rules. |
| ADV3-M1, incident completeness | Closed | The incident schema now has an exact required-slot map with authenticated absence/error evidence. |
| ADV3-M2, partial recovery retry | Closed | Every recovery attempt has a distinct run-bound namespace and later success must account for prior attempts. |

## Critical

None.

## High

### ADV4-H1 — A fallback deployment comment can be replayed across rerun attempts

- **Applies to:** AD-9.
- **Hole:** the request input is immutable for a `workflow_dispatch` run, while a rerun increments
  `run_attempt` without changing `run_id` or inputs. GitHub's review-history endpoint is keyed only by
  `RUN_ID`; its rows expose user, state, comment, and environments but no run-attempt coordinate or
  review timestamp. AD-9 binds the derived v3 authorization to the current attempt, but the qualifying
  approval row itself cannot be proven to belong to that attempt.
- **Divergence:** one verifier reuses attempt 1's exact fallback comment on attempt 2; another interprets
  “a retry needs a new request and deployment approval” as forbidding all reruns. Both are compatible
  with the observable API shape.
- **Falsifier:** approve attempt 1 with the exact request hash, fail before publication, rerun the same
  workflow with attempt 2, approve the environment with a generic comment, and query run approvals.
  If the old exact row still qualifies, the attempt-2 fallback was not specifically approved.
- **Required closure:** make fallback ineligible when `run_attempt != 1` and require a fresh dispatch,
  or bind a server-authenticated deployment/review identity that exposes the attempt. Also reject a
  request SHA already consumed by any prior attempt/run, with an authenticated durable lookup.
- **Platform evidence:** GitHub documents the endpoint as
  [`GET /actions/runs/{run_id}/approvals`](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run),
  and its response schema has no attempt or timestamp.

### ADV4-H2 — Authenticated packet components are not given exact cross-field commit/run bindings

- **Applies to:** GOV-1 split implementation gate.
- **Hole:** the validator authenticates each run, job, artifact, and output independently, but the spine
  does not state the equality map proving they exercised the packet's claimed
  `frontcomposer_commit`, `builds_commit`, `caller_blob_sha256`, and policy. In particular, it does not
  require the CI and gate-frozen handoff candidates to equal `frontcomposer_commit`, the handoff's
  reusable to equal `builds_commit`, or every ordinary check producer/output to derive from those same
  coordinates. The hostile-candidate run also lacks a base-implementation relation.
- **Divergence:** one validator enforces those transitive equalities; another authenticates old passing
  runs and artifacts and associates them with a newer claimed implementation commit. Both can satisfy
  “authenticates each” and every current row shape.
- **Falsifier:** construct a packet naming implementation commit `B` while supplying otherwise valid
  CI/release/check evidence from prior commit `A`. If the pinned validator has no spine-mandated
  equality to reject it, stale evidence passes the gate.
- **Required closure:** specify a per-evidence-run equality table: which head/candidate, caller blob,
  policy hash, reusable commit, artifact producer, and output bytes must equal which packet member.
  For the hostile run, bind the exact tested base plus fixture delta/tree. Add mixed-commit and
  mixed-Builds negative packets.

### ADV4-H3 — Reviewer PASS and packet-PR owner approval are asserted, not authenticated

- **Applies to:** GOV-1 split implementation gate.
- **Hole:** `reviewer_reports` contains only `{lens,path,sha256,verdict,critical,high}`. It has no closed
  binding to the reviewed spine, FrontComposer commit, Builds commit, or report producer, and no rule
  says the report bytes' own verdict/counts must agree with the packet row. Likewise, “owner-approved”
  for the evidence PR has no exact owner registry, API review projection, review body, or durable
  approval record in the packet schema.
- **Divergence:** a validator may trust the packet's PASS literals, parse informal Markdown, require a
  sidecar, or authenticate a GitHub review ad hoc. Stale/forged PASS rows or an ordinary collaborator's
  approval can therefore satisfy some conforming implementations but not others.
- **Falsifier:** point a PASS row at a hash-valid report for an older spine/implementation or one whose
  body says BLOCK, then approve the packet PR with a reviewer outside `release_owner_logins`. The
  current schema contains no closed datum that must reject either substitution.
- **Required closure:** use a duplicate-rejecting machine review sidecar for each lens containing
  `{lens, spine_sha256, frontcomposer_commit, builds_commit, report_sha256, verdict, critical, high}`
  and require byte agreement with the report. Add an exact GitHub PR-review projection for the packet
  PR, bind its unchanged head/merge, require the API user in the named owner registry, and preserve the
  authenticated approval bytes or IDs.

## Gate disposition

Do not mark the configured adversarial review PASS or satisfy the implementation gate while
ADV4-H1–H3 remain open. The spine separately and correctly records the current operational Critical
blocker that `HEXALITH_RELEASE_PUBLISH_ENABLED` was observed literal `true`; that acknowledged external
state is not double-counted as a new architecture finding. No lower-severity findings are carried from
this focused final recheck.
