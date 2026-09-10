# Reviewer Gate — current-technology / reality-check lens — pass 4 — 2026-09-09

**Subject:** `ARCHITECTURE-SPINE.md` SHA-256
`4a765adbf2026396d9d1258163a41f155522d8c3a7304d6d6857533a5a37624f`

## Verdict

**PASS — the changed mechanisms introduce no current-platform impossibility.** Critical 0, High 0,
Medium 0, Low 0. This was a narrow delta review; pass 3 remains the evidence for the unchanged
artifact, attestation, runner, immutable-Release, and NuGet contracts.

## Delta checks

### Run-bound fallback comment — implementable

The exact deployment comment
`hexalith-attestation-fallback-v1:<request_sha256>:<release_run.run_id>:<release_run.run_attempt>` is
representable in GitHub's deployment-review comment and is returned byte-for-byte by
`GET /repos/{owner}/{repo}/actions/runs/{run_id}/approvals`. The run ID exists before environment
approval because the protected job is already pending in that run; `run_attempt` is available from the
run/context and API. The approver can therefore submit the exact current-attempt suffix before the job
starts, and the publisher can authenticate it after approval.

The API projection still has the shape verified in pass 3: `user`, `state`, `comment`, and
`environments`, with no approval-event timestamp; duplicate byte-equivalent rows can occur. The spine
continues to collapse equivalent projections, reject unequal duplicates, and use only verifier
`observed_at`. Parsing and comparing both safe integers against the authenticated current run closes
cross-attempt replay without requiring an unavailable API field or advancing `main`. The new negative
fixture for an attempt-1 comment in attempt 2 matches the platform's possible review-history behavior.

Primary reference: GitHub's
[workflow-run review-history API](https://docs.github.com/en/rest/actions/workflow-runs#get-the-review-history-for-a-workflow-run).

### Reviewer and conformance-packet evidence — representable

The new packet model avoids self-reference and has a feasible chronology:

1. Each reviewer runs against the already-fixed `frontcomposer_commit` and uploads the Markdown report
   plus sidecar in one run artifact. The report header binds the spine, implementation, Builds/policy,
   lens, verdict/counts, and producer run without containing the report hash; the separate sidecar can
   therefore bind the completed report SHA-256 without a hash cycle.
2. A later evidence-only packet PR commits the byte-identical artifact members and packet. GitHub's
   artifact object supplies the artifact ID and archive digest, while the pinned validator can stream
   and recompute the member/report/sidecar hashes using the already-validated raw-ZIP mechanism.
3. The packet hash is stable before review, so the Release Owner can place its exact canonical approval
   JSON in a review body. GitHub pull-request review records expose review ID, user, state, body,
   `commit_id`, and `submitted_at`, making the first-PR approval projection representable.
4. A distinct projection PR can commit that authenticated first-review response without changing the
   prior packet or implementation bytes. Requiring unchanged non-squashed heads through protected
   `main` is supported once the explicitly deferred no-bypass ruleset exists.

The equality map is also feasible: producer run `head_sha`, workflow/job identity, artifact ownership,
and run attempt are available from Actions APIs; commit parentage and changed paths are Git data; raw
output and artifact digests are locally recomputable; Release/NuGet absence checks are read-only API
operations. Artifact expiry can make a packet ineligible, but the spine treats unavailable/expired
evidence as failure rather than assuming indefinite retention.

Primary references: GitHub's
[Actions artifacts REST API](https://docs.github.com/en/rest/actions/artifacts),
[workflow-runs API](https://docs.github.com/en/rest/actions/workflow-runs), and
[pull-request reviews API](https://docs.github.com/en/rest/pulls/reviews).

## Timebox note

No live fallback approval or implementation packet was created because both would mutate external
state and the split implementation is not yet eligible. This pass verified API representability and
the absence of temporal/hash cycles; the required authenticated rehearsals remain correctly assigned
to the implementation gate.

## Finding counts

Critical: **0** · High: **0** · Medium: **0** · Low: **0**.
