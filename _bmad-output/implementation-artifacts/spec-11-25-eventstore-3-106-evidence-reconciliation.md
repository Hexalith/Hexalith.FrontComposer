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
- Quality run `35439820984` sealed the provider package root but launched the pinned
  EventStore verifier from FrontComposer's working directory, so three interactions could
  not locate their current Pact documents. The lane now stages the six repository Pacts
  into the pinned EventStore checkout and executes the 83-test verifier from that root.
- Quality runs `35440661153` and `35441438442` passed all provider interactions but failed
  closed during AppHost source-graph evaluation. Bounded diagnostics narrowed the failure
  from `apphost.source-graph.not-exact` to `apphost-msbuild-output-invalid` without
  retaining unbounded MSBuild output or runner paths.
- Quality run `35441948746` identified a pruned `Dapr.Common 1.18.7` package required by
  conditionally restored source projects. AppHost closure discovery now includes every
  repository assets graph bound to the same fresh package root and explicitly binds the
  `Aspire.AppHost.Sdk 13.5.4` MSBuild SDK package; 64 focused evidence tests passed before
  retrying the hosted lane.
- Quality run `35442653835` passed the provider capture and reached AppHost evaluation,
  where `BuildProjectReferences=true` started transitive pack/build work and failed on a
  missing package README plus concurrent resource writes. Evaluation is now isolated,
  serialized, package-generation-free, and followed by the existing explicit serial
  no-restore AppHost build.
- Quality runs `35443476777` through `35444622055` successively exposed stale governance
  assertions, an incorrectly located assertion, and truncation of the large MSBuild JSON
  evaluation document. The assertions now bind the effective evaluation controls and the
  evaluator uses a dedicated bounded result channel instead of the ordinary command-output
  budget.
- Quality runs `35445285784` and `35445911478` proved that one global `net10.0` property
  cannot govern both restore/build and every independently evaluated project: the former
  changed restore semantics, while the latter rejected the analyzer-only SourceTools
  project's sole restored `netstandard2.0` target. Restore/build no longer receive that
  property; evaluation selects each project's deterministic target from its fresh assets
  graph and rejects ambiguous non-runtime target sets.
- Quality runs `35446649229` and `35447067363` were cancelled by newer main-branch pushes
  before candidate publication. Run `35447465188` then passed Epic 9 and the visual lane
  but failed before live capture because a newly added governance assertion searched for
  non-f-string source text. Commit `4d0042f8` corrects that exact assertion while retaining
  the bounded MSBuild result-file control.
- Quality run `35448171676` passed provider verification and both independent acceptance
  lanes, then proved that a successful build legitimately adds generated `Compile`,
  analyzer, and reference items to the evaluated input binding. Both evaluations still
  enforce the exact source project closure; the post-build binding is now authoritative
  and remains subject to the final Gate 2c independent recomputation.
- Quality run `35448951466` reached post-build runtime-output sealing and exposed that the
  independent validator still discovered only explicit project-reference metadata while
  the smoke lane already included conditional projects restored into the same fresh
  package root. Independent discovery now applies that identical fresh-root discriminator
  and rejects stale assets from earlier build lanes.
- Quality run `35449805021` attempt 1 passed provider verification and both independent
  acceptance lanes, then reached the real AppHost. Its first Keycloak health wait exhausted
  the 300-second capture envelope after fresh restore, graph evaluation, and build, leaving
  only the reserved cleanup interval. The validator already caps this fail-closed envelope
  at 600 seconds; the workflow and its governance assertion now use that bound.
- Quality run `35449805021` attempt 2 made every Aspire resource healthy but exhausted the
  remaining 300-second window while EventStore readiness stayed unavailable. Run
  `35451136802` proved the widened envelope reached the service and consistently received
  HTTP 503. EventStore 3.106 intentionally holds `/health` unhealthy until its store-global
  projection-delivery writer protocol is activated. The smoke now mirrors EventStore's own
  disposable Aspire fixtures: it permits activation only when that marker is the sole
  unhealthy check, proves the endpoint rejects an invalid bearer, and submits authenticated
  no-legacy-writer/no-durable-data attestations before continuing.
- Quality runs `35452087655` and `35452808521` passed the full authenticated runtime
  sequence after that cutover: health, command submission and completion, exact-tenant
  handler-computed query provenance, and SignalR. Cleanup also stopped the AppHost, closed
  every probed port, and removed all invocation-created Dapr name-resolution files, but
  failed closed because both the isolated NuGet authority and retained runtime-output byte
  bindings changed during execution. Bounded coordinate/path diagnostics were added to
  identify those mutations without disclosing file contents.
- Quality run `35453476895` identified the remaining mutation as the Tenants sample and
  its dependency closure. Its path-only `IProjectMetadata` was the sole AppHost resource
  locator missing `SuppressBuild=true`, so Aspire rebuilt it after the package and output
  boundaries were captured. Commit `a4540359` suppresses that runtime build, re-seals the
  package authority after the explicit prebuild, and records `executionStartedAt` only
  after both the package and output boundaries are complete.
- Quality run `35454666528` stopped before live capture because the governance test still
  enforced the superseded prebuild chronology and an underscore-bearing regression-test
  name changed the sealed CA1707 identifier inventory. The assertion now requires restore,
  initial authority validation, evaluation, prebuild, final authority seal, execution
  timestamp, and start in that order; the regression test does not expand the exception.
- Quality run `35455094590` passed the authenticated AppHost smoke and clean byte-stability
  checks. The following independent validation rejected the pretty-printed exhaustive smoke
  document above its 1 MiB bound and found that AppHost restore had replaced eight provider
  assets graphs with the AppHost package-root selection. The smoke packet now uses compact
  JSON, and the final gate recomputes AppHost and provider package authorities sequentially
  around an exact eleven-file provider-assets snapshot.
- Quality run `35456374556` proved the sequential provider/AppHost snapshot path reached
  validation and measured the compact exhaustive smoke packet at 1,122,084 bytes. The
  runtime-output inventory is now retained as an exact file count and SHA-256 tree binding;
  full file entries remain independently recomputed before execution, after shutdown, and
  in the final validator without duplicating those paths in the bounded document.
- Quality run `35457643860` passed all 19 provider interactions, the authenticated AppHost
  smoke, clean shutdown, and the 799,656-byte compact packet. Independent validation then
  failed closed on a local-runner path and a non-canonical evaluated-input binding before
  candidate publication. Authority-path bindings are now deduplicated after resolution, and
  bounded diagnostics report only JSON locations and structural reason codes without
  disclosing evidence values.
- Quality run `35458730675` retained the same passing runtime result and proved the remaining
  structural mismatch is the sealed assets-graph list rather than duplicate authority paths.
  The next retry reports bounded binding-only/ledger-only repository coordinates and detects
  a local-path leak in dynamic JSON keys without emitting the key or value.

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
