# Reviewer Gate — rubric walker pass 3 — GOV-1, 2026-09-09

**Reviewed artifact:** `ARCHITECTURE-SPINE.md`  
**SHA-256:** `a0efa3e02e15cfc1a4bbd73068cdd55ab38342136345d29883bec8b5f2ff9c86`  
**Lens:** complete BMad Architecture good-spine checklist plus explicit re-test of RW2-01..05  
**Intent:** independent review only; no spine, memlog, source, workflow, policy, or setting was changed

## Verdict

**FAIL — 0 Critical, 1 High, 1 Medium, 0 Low.** RW2-02 through RW2-05 are closed, and RW2-01's
same-branch approval paradox is removed. The replacement fallback approval still does not bind the
deployment review to the Release **run attempt**: GitHub's approvals endpoint is keyed only by run ID,
its rows carry no attempt, and the required comment contains only the request digest. An exact approval
from attempt 1 can therefore be reused by the publisher on attempt 2 even though the spine says every
retry needs a new request and deployment approval.

The deterministic spine lint passed with zero findings. The live
`HEXALITH_RELEASE_PUBLISH_ENABLED=true` condition remains a separately and correctly declared Critical
**operational blocker**; it is not counted again as a defect in this rubric-reviewed spine.

## Severity counts

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 1 |
| Low | 0 |

## Re-test of pass-2 findings

| Prior finding | Result | Evidence |
| --- | --- | --- |
| RW2-01 — committed fallback approval invalidates candidate | **Original issue closed; replacement has RW3-01** | AD-9 now uses a workflow-dispatch request plus exact protected-environment review comment and does not advance `main`. The review evidence still lacks run-attempt identity. |
| RW2-02 — artifact redirect forbidden | **Closed** | AD-12 permits exactly one authenticated HTTP 302 data redirect, strips credentials/headers, forbids chaining/logging, retains the streaming cap, and authenticates final bytes by run/artifact identity and digest. |
| RW2-03 — implementation gate unauthenticated | **Closed** | The packet now pins an exact validator/check, closes run and artifact projections, binds raw committed outputs to authenticated producers/artifacts, specifies hostile/frozen outcomes, performs live API and external-effect checks, and forbids implementation drift in the later packet PR. |
| RW2-04 — Structural Seed mixes current and target state | **Closed** | The section is explicitly labeled target-after-gate and names the current brownfield delta as mandatory register work. |
| RW2-05 — AD-12 omits owner/capability policy fields | **Closed** | AD-12 now names `release_owner_logins` and `attestation_capability`; the target policy seed does too. |

## Good-spine checklist

| Checklist item | Result | Judgment |
| --- | --- | --- |
| Fixes every real divergence point for the level below | **Fail on one seam** | Graph, evaluator, split jobs, schemas, ordering, evidence, archive, incident, ledger, and implementation-gate seams converge. Fallback retry authorization does not (RW3-01). |
| Every AD Rule is enforceable and prevents its stated divergence | **Fail on AD-9 retry binding** | All other reviewed rules are enforceable at this altitude. AD-9's assertion that fallback authorization is run-attempt-bound is not derivable from the specified API/comment projection (RW3-01). |
| Nothing under Deferred could let two units diverge | **Pass** | The conformance packet now closes its values and verification path; the other deferrals have explicit triggers, ownership, or release-ineligibility boundaries. |
| Named technology is verified-current and fits | **Pass, with AD-9 correction needed** | Exact action/runner/API assumptions are current and the one-redirect artifact transport fits GitHub. The workflow-run approvals API behaves as stated except that its absence of run-attempt identity is not compensated in the comment (RW3-01). |
| Ratifies rather than contradicts the brownfield codebase | **Pass** | Target versus current state is explicit, the live-enabled legacy release path is called out as a blocker, and the implementation packet/register prevent target claims against current v1/v3/same-job code. |
| Covers the driving spec/source capabilities | **Pass, gated** | GOV-1 graph/semantic requirements and the G2/FR-24 exact-artifact, fallback, attestation, publication, and incident requirements are present; superseded source wording must be reconciled before integration. |
| Does not weaken or contradict inherited parent invariants | **Pass** | The split strengthens exact-artifact and pre-publication authorization; intentional same-job supersession is explicit and adoption-gated. |
| Every owned structural/operational dimension is decided, deferred, or open | **Pass** | Ownership, policy, source identity, state/data shapes, privileges, environment, runner, network, artifact lifecycle, publication ordering, verification, incidents, adoption, and provider/deployment scope are covered. |

## Findings

### RW3-01 — High — fallback deployment-review evidence is not bound to `run_attempt`

**Affected rules:** AD-9 lines 271–307; AD-15 release-attempt identity; AD-18 environment approval.

The fallback request binds the candidate and CI run but cannot contain the Release run coordinates,
because it is supplied at `workflow_dispatch` before GitHub assigns the Release run ID. The publisher
later queries `GET /actions/runs/RUN_ID/approvals`, filters on the exact comment
`hexalith-attestation-fallback-v1:<request_sha256>`, and creates an authorization record containing the
current `{run_id,run_attempt}`.

The selected GitHub endpoint takes a run ID, not a run attempt, and its approval rows contain no
`run_attempt`. The exact comment also contains neither Release run ID nor attempt. A workflow rerun
retains its dispatch input and run ID while incrementing `run_attempt`. Consequently:

1. attempt 1 receives an owner approval with the exact request-digest comment;
2. attempt 2 is started with the same workflow-dispatch input and must pass the environment gate
   again, but its reviewer can supply a generic/different comment;
3. the publisher's run-level approval-history query still returns attempt 1's exact qualifying row;
4. pinned code can project that old row into a new authorization record naming attempt 2, even though
   the owner never gave attempt 2 the fallback-specific approval.

This is not hypothetical API-shape ambiguity. A live read-only response for FrontComposer Release run
`34153315061` returned duplicate approval rows with `{user,state,comment,environments}` and no event ID,
timestamp, job ID, or attempt. Collapsing byte-identical rows avoids ambiguity but cannot establish
which attempt was approved. The current wording “binds authorization to one Release run/attempt” and
“a retry needs a new request and deployment approval” is therefore not enforceable.

**Disposition:** **autofix before finalization.** Bind the exact review comment to the current Release
coordinates, for example
`hexalith-attestation-fallback-v1:<request_sha256>:<run_id>:<run_attempt>`, and require the final
authorization's `release_run` to byte-match those comment coordinates. Add a fixture containing an
attempt-1 qualifying approval plus an attempt-2 nonqualifying approval and prove attempt 2 fails. An
alternative is to make fallback ineligible on reruns (`run_attempt != 1`), but that is less flexible.

Primary API reference: [GitHub REST — review custom deployment protection rules for a workflow run](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run).

### RW3-02 — Medium — AD-13 still calls recovery action pins `post_release` pins

**Affected rule:** AD-13 lines 477–480 and static-closure line 494; AD-12 lines 366–388.

AD-12 correctly defines distinct `post_release` and `incident_recovery` evaluator stages, each with a
nullable reusable and its own action closure. AD-13 then says “Post-release and recovery use their own
active-policy-authorized `post_release` action commits,” and the local Builds-action carve-out names
only `post_release`. A recovery implementation using the same exact local-action checkout mechanism
could therefore be rejected because its row has `reusable:null` but it is not covered by the
`post_release` exception; another implementation could avoid the issue by using only external
literal-SHA actions. The two readings diverge unnecessarily.

**Disposition:** **autofix.** Say that post-release and recovery use their respective active-policy
`post_release` and `incident_recovery` action commits, and extend the nullable-reusable local-action
carve-out to both stages (or expressly forbid local checked-out actions for `incident_recovery` and
require external literal-SHA action references).

## Confirmed strengths

- **Fallback lifecycle:** the unresolvable same-`main` approval-file cycle is gone; request, exact owner
  comment, policy/candidate/CI/fingerprint binding, expiry, sampled environment, final authorization,
  and durable raw asset are otherwise closed and fail-closed.
- **Artifact/network path:** raw ZIP pre-scan and all AD-7 caps remain mandatory while the required
  signed-storage redirect is narrowly admitted as data, not executable acquisition.
- **Incident recovery:** the additional writer is separated from product publication as an exact
  `incident_recovery` stage, pinned to `ubuntu-24.04`, constrained to a reserved tag namespace, and
  equipped with closed slot states plus non-overwriting recovery attempts.
- **Attestation:** exact repository casing, signer digest, source ref/digest, GitHub workflow predicate
  members, subject set, invocation ID, runner environment, and separate production approval are now
  distinguished and pinned by golden bundles.
- **Ledger:** multiple signers have collision-free normalized paths, and the two-PR observation/
  approval/projection process avoids self-reference and approved-head mutation.
- **Implementation gate:** the validator, evidence producers, raw outputs, required rehearsals,
  reviewer reports, unchanged packet PR, live checks, and failure cases are now concrete enough for one
  implementation.

## Incomplete lower-severity checks

- No accepted split reusable, incident-recovery workflow, or conformance packet exists yet, so the
  target workflow behavior could not be exercised end to end.
- The full repository suite was not rerun; this pass used deterministic spine lint, exact source/API
  inspection, the live workflow-run approvals response, and earlier focused test evidence.
- The live release variable remains `true`; architecture finalization does not itself change that
  external setting, and production remains ineligible exactly as Adoption states.

