---
title: 'Reconcile EventStore 3.106 Runtime Evidence'
type: 'fix'
created: '2026-09-19'
status: 'in-review'
route: 'dispatch'
baseline_commit: 'e0388be3e3746288bc2e0069ae6e26696c17b1f3'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Record genuine current compatibility evidence for FrontComposer at EventStore source
`2d680d7d08e00baef63f5b2aca98c6ad6fcc178d`, package `3.106.0`, and Builds catalog
`87f6f27425666c540fb6db41800a3af1d3767e39`. Preserve the Story 11.25 v2
`059f6a89… / 3.103.0 / a32cb422…` identity and evidence byte-for-byte as historical
approval context. A reviewed v3 candidate may supersede v2 only for active technical
release selection.

## Boundaries & Constraints

**Always:** Generate `run-evidence.v4`, `apphost-smoke.v3`, both package-ledger
sidecars, and one pre-run runtime-input manifest from the same clean invocation. Validate
the five-file live tree before publishing it as a candidate. Keep the approval authority
bootstrap empty, receipts empty, `migrationApprovalClaimed=false`, and the A6b/A8 holds
fail-closed.

**Never:** Rewrite the frozen Story 11.25 specification block, identity v1, identity v2,
the v2 evidence tree, historical archives, gitlinks, package versions, Pacts, production
adapters, or AppHost topology. Never label a failed or locally incomplete capture as
passing, approved, published, or deployed.

</frozen-after-approval>

## Tasks & Acceptance

- [x] Separate immutable v2 tuple assertions from the current repository tuple in
  governance tests.
- [x] Add a CI capture-candidate upload that runs only after independent current-live
  validation succeeds.
- [ ] Run the GitHub-hosted live lane and download the validated candidate.
- [ ] Record the candidate as a hash-bound v3 active identity without changing v1/v2.
- [ ] Re-run the focused evidence suites and full FrontComposer CI.

Acceptance requires exact current gitlink/catalog identity, 19/19 provider interactions,
the ten-resource authenticated AppHost smoke, all five evidence files plus the runtime
manifest, and an explicitly open migration approval state.

## Evidence Log

- Local fresh-package restore attempts on 2026-09-19 failed closed before assets were
  generated: one unbounded diagnostic attempt was cancelled after more than five minutes;
  bounded retries exited `124` after 600 and 180 seconds respectively with zero bytes in
  their fresh `NUGET_PACKAGES` roots. NuGet.org index and direct package downloads were
  reachable, so the authoritative recapture is routed to the GitHub-hosted Quality lane.
- Quality run `35438506827` restored the provider successfully but failed closed while
  building its package ledger: the lane declared only the two verifier assets graphs even
  though the isolated restore populated private build packages selected by the complete
  eleven-project restore closure. The retry binds all eleven generated assets graphs.
- Quality run `35438982460` and an exact local fresh-root reproduction showed four NuGet
  download candidates absent from every final assets graph (`Google.Protobuf 3.31.1` and
  three `Microsoft.Extensions.Logging*` versions). Provider and AppHost ledger capture now
  explicitly remove only unselected version directories from the validated external root
  before sealing; the subsequent no-restore build proves none was an execution input.

## Review Triage Log

| ID | Finding | Severity | Verdict | Evidence / Disposition |
| --- | --- | --- | --- | --- |
| VG-01 | No governance test pinned the new validation and publication contract. | medium | patch | Preverified against the focused governance suite; add exact arguments, fail-closed posture, ordering, trigger, staging, and upload assertions. |
| EH-01 | A fixed artifact name can collide when a workflow run is re-run. | medium | patch | Suffix the candidate artifact with `github.run_attempt`. |
| EH-02 | Uploading paths from runner temp and the repository creates an unstable archive layout. | medium | patch | Copy all six validated inputs into one runner-temp directory and upload that directory only. |
| EH-03 | A drifted tuple could be uploaded under the EventStore 3.106 candidate name. | high | reject-false | The earlier blocking Gate 2b governance fact pins the exact EventStore source SHA, Builds SHA, and package version; job execution stops before Gate 2c on drift. |
| BH-01 | Pull-request runs use a synthetic merge revision that is unsuitable as the later branch evidence revision. | high | patch | Keep live validation on pull requests, but stage and publish candidates only for a push to `refs/heads/main`. |
| BH-02 | The two-source archive layout does not establish one stable candidate root. | medium | patch | Same independently reported root cause as EH-02; retain this row and apply the shared staging fix. |
| BH-03 | The capture/upload contract is not independently enforced. | medium | patch | Same independently reported root cause as VG-01; retain this row and apply the shared governance coverage. |
| BH-04 | The task does not fully prescribe the future v3 identity schema, predecessor binding, paths, and chronology. | medium | reject-spec-edit | The review protocol forbids repairing a build-spec defect during code review. The implementation phase must derive and test these bindings without changing the frozen approved intent. |
| BH-05 | A6b/A8 are not locally defined as FrontComposer validator concepts. | medium | reject-spec-edit | These are explicitly user-owned cross-repository Folders holds. They remain fail-closed there; inventing a local FrontComposer authority or changing the frozen build spec is out of scope. |
