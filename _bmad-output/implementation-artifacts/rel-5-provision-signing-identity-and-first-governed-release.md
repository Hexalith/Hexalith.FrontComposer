---
created: 2026-07-15
updated: 2026-08-03
owner: Release Owner (executes) + Developer (verification tooling/evidence assistance)
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md
correctionProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md
status: in-progress
scope: moderate
implementationRisk: medium (operational/custodial, not code)
ordering: T0 control restoration/audit executes immediately; T1 owner prerequisites continue in parallel; ACs 5-10 trail the qualifying governed seam
releaseControl: REL-AI-1 closure routes through this story; the REL-4 variable is coarse execution enablement only and stays non-`true` until a qualifying governed candidate and protected post-evidence authorization mechanism exist
---

# REL-5: Provision the Production Signing Identity and Prove the First Governed Release

Status: in-progress (owner-executed story; the developer agent assists with verification
tooling and evidence capture, and must not perform custody, approval, or authorization actions).

Approval: approved by Administrator on 2026-07-15 (Batch-mode correct-course).

## Story

As the Release Owner,
I want the production signing identity, approvals, and governed release environment provisioned and
the first gated release proven end to end,
so that REL-AI-1 can close on durable real-release evidence that no developer can produce alone.

## Why This Story Exists

REL-3 makes the FR-24 exact-artifact gate technically enforceable, but several of its terminal
steps require authority a developer does not hold: selecting and provisioning the production
package-signing identity, custody and rotation of certificate secrets, approving the RFC 3161
timestamp authority, filing and accepting the upstream Hexalith.Builds contract, authorizing the
first gated release, and signing off REL-AI-1. Leaving these inside REL-3 lets a developer-complete
story sit indefinitely blocked on operational work with no owner. This story separates operational
authority from development work.

A local or test signing root can validate the REL-3 pipeline without publishing, but it does not
establish a credible public package identity; only the Release Owner can select the production
trust model (REL-3 Engineering Guardrails).

## Acceptance Criteria

1. Given the production trust requirements, when the Release Owner selects the package-signing
   identity and trust model (certificate authority, subject, validity, storage), then the decision
   is recorded durably (this story + the compliance ledger) before the first governed release.

2. Given the selected identity, when certificate secrets are provisioned, then
   `NUGET_SIGNING_CERTIFICATE_BASE64` and `NUGET_SIGNING_CERTIFICATE_PASSWORD` exist as
   Release Owner-custodied repository (or organization) secrets with a recorded rotation
   procedure, and certificate material never enters the repository, logs, artifacts, or manifests.

3. Given RFC 3161 timestamping is required, when the Release Owner approves the timestamp
   authority, then the approved service URL is recorded and configured for the governed release
   path.

4. Given the BUILD-REL-1 contract in the G2 request document, when the upstream story is filed
   against Hexalith/Hexalith.Builds, then the issue/story URL and later the accepted revision are
   recorded in `g2-hexalith-builds-inline-pre-publish-gate-request.md` (fields currently
   `pending`), and the filed scope is the full opt-in governed contract, not signing-secret
   forwarding alone.

5. Given the upstream governed mode lands with a protected release-environment input, when the
   environment is adopted, then its required reviewers are configured under Release Owner control;
   the caller-side REL-4 `HEXALITH_RELEASE_PUBLISH_ENABLED` variable remains a coarse execution
   guard and is never sufficient FR24 publication authorization. The governed seam must pause
   after exact-candidate evidence is sealed and before publication so that protected owner
   authorization can be recorded. No approval tokens are added to FrontComposer's `release.yml`.

6. The Release Owner treats `HEXALITH_RELEASE_PUBLISH_ENABLED` as coarse workflow-execution
   enablement only, never as sufficient FR24 publication authorization. While no approved governed
   candidate mechanism is active, the variable is absent or not exactly `true`. A qualifying
   mechanism creates and seals the exact candidate evidence before a protected owner decision and
   preserves byte identity from that decision through publication.

7. Given the first governed release publishes, when post-publication verification runs, then the
   Release Owner confirms downloaded NuGet and GitHub assets verify (signatures, timestamps, exact
   hashes) against the sealed manifest.

8. Given verification passes, when the compliance ledger is updated, then
   `rel-ai-1-release-evidence-ledger.md` gains the first compliant release record with every
   required field populated and durable evidence paths.

9. Given the ledger records passing real-release evidence, when the Release Owner reviews
   REL-AI-1, then REL-AI-1 is closed only if every FR-24 artifact is durable and downloaded bytes
   match the authorized manifest; any gap keeps REL-AI-1 open with the exact blocker recorded.

10. Every authorized release attempt is bounded and auditable: the owner records the candidate
    identity, enablement time, sealed-ready evidence, protected approval, run URL, publication
    result, verification result, and switch reset. Any failed, cancelled, invalid, or unauthorized
    path remains fail-closed and triggers inspection for partial external side effects.

## Tasks

- [x] T0 — Restore and audit the coarse release control immediately.
  - [x] Restore `HEXALITH_RELEASE_PUBLISH_ENABLED` to absent/non-`true`; record before/after
        evidence without exposing secrets (AC6, AC10).
  - [x] Reconcile every publish-capable run during the enabled window across GitHub Releases and
        every configured package registry; record any partial side effect (AC10).
- [ ] T1 — Owner prerequisites (do not wait for REL-3).
  - [ ] Select and record the production package-signing identity and trust model (AC1).
        *2026-07-18 (REL-3 review constraint): the identity MUST chain to the publicly trusted
        NuGet code-signing roots — the independent verifier checks downloaded bytes against the
        stock public bundle, so an internal/self-signed CA passes preparation but always fails
        post-publication verification. Certificate acquisition remains a physical owner action.*
  - [x] File BUILD-REL-1 upstream with the full governed contract; record the URL in the G2
        request (AC4). *Filed 2026-07-18 under Release Owner directive:
        <https://github.com/Hexalith/Hexalith.Builds/issues/17> (both items: governed contract +
        common freeze gate). The issue closed 2026-07-20 without a qualifying accepted revision;
        a reopened or successor accepted revision is required for integration, end-to-end
        completion, release eligibility, and unfreeze.*
  - [ ] Approve and record the RFC 3161 timestamp authority (AC3). *Candidate: DigiCert
        (`http://timestamp.digicert.com`) — already the pipeline default in
        `eng/release_prepublish.py` and the `NUGET_SIGNING_TIMESTAMPER` fallback; needs one
        explicit owner confirmation line here to close AC3.*
- [ ] T2 — Provision custody.
  - [ ] Provision the two signing secrets with Release Owner-only custody and a rotation
        procedure (AC2).
  - [ ] Record the org-vs-repo variable/secret posture, honoring the shadowing hazard documented
        in the G2 request and deployment guide.
- [ ] T3 — Adopt the upstream governed mode when accepted.
  - [ ] Reopen issue 17 or file a successor and record the qualifying accepted upstream revision
        in the G2 request (AC4).
  - [ ] Configure protected-environment reviewers if the environment input lands (AC5).
  - [ ] If upstream cannot land before a required release, decide and record the bounded
        FrontComposer-owned contingency (scope, approver, expiry/reopen trigger, migration back).
- [ ] T4 — First governed release (trails REL-3 completion).
  - [ ] Enable only one bounded candidate through the approved mechanism; grant protected
        publication authorization only on sealed ready/authorized evidence and reset the coarse
        switch after the attempt (AC6, AC10).
  - [ ] Verify downloaded NuGet and GitHub assets against the sealed manifest (AC7).
  - [ ] Record the first compliant ledger entry (AC8) and finalize the v3.2.1, v3.2.2, v4.0.0,
        and v4.0.1 historical dispositions.
  - [ ] Close REL-AI-1 only on durable passing evidence (AC9).

## Implementation Boundary

- Release Owner owns: identity/trust decisions, secret custody and rotation, timestamp-authority
  approval, upstream filing/acceptance, environment reviewers, release authorization, ledger
  sign-off, REL-AI-1 closure.
- Developer assists with: verification tooling, evidence capture/formatting, ledger mechanics —
  never custody, approval, or authorization.
- Hexalith.Builds owner owns: BUILD-REL-1 implementation upstream. Do not modify or commit the
  shared submodule from FrontComposer.
- REL-3 owns: the technical gate, orchestration command, governance tests, and workflow changes.
- No FrontComposer code changes are owned here beyond evidence records.

## Engineering Guardrails

- Never log, print, commit, or persist certificate material, passwords, or raw secrets.
- Never authorize publication on dry-run, reconstructed, or partial evidence.
- Never initialize nested submodules; use only root-declared `references/...` paths.
- Preserve unrelated worktree changes.

## Definition of Done

- [ ] ACs 1-4 recorded (identity, secrets, timestamp authority, upstream filing).
- [ ] Upstream accepted revision or approved bounded contingency recorded.
- [ ] First governed release authorized, published, and byte-verified from NuGet and GitHub.
- [x] Coarse execution switch reset and the enabled-window containment audit recorded.
- [ ] Compliance ledger carries the first compliant record; v3.2.1, v3.2.2, v4.0.0, and v4.0.1
      dispositions finalized.
- [ ] REL-AI-1 closed on durable evidence, or open with the exact blocker recorded.

## References

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md`
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
- `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-03.md`

## Dev Agent Record

### REL-5 T0 Release Owner Execution Record (2026-08-03)

#### Implementation Plan

- Capture the repository-variable state through the GitHub API.
- Change only `HEXALITH_RELEASE_PUBLISH_ENABLED` from exact `true` to exact lowercase `false`.
- Audit the complete enabled interval across Release workflow executions, GitHub Releases/tags,
  and the eight configured nuget.org package IDs.
- Record durable evidence without triggering a release, accessing secrets, or authorizing a
  candidate.

#### Debug Log

- Authorized interval: `2026-08-02T08:27:15Z` through `2026-08-03T06:24:13Z`.
- Before: repository API returned `value=true`, `created_at=2026-08-02T08:27:15Z`, and
  `updated_at=2026-08-02T08:27:15Z`.
- After: repository API returned exact lowercase `value=false` and
  `updated_at=2026-08-03T06:24:13Z`. Changing the variable did not trigger a workflow.
- Seven Release runs were created in the interval. Four entered `release / release`; all four
  generated runner-local `4.1.0` candidates and failed closed during `prepare` at package-inventory
  validation. Their publication/evidence-upload steps did not run. Three Release runs skipped the
  reusable release path.

| Release run | Created (UTC) | Head | Result |
| --- | --- | --- | --- |
| [30743463963](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30743463963) | 2026-08-02T10:17:53Z | `22c130d9` | Entered; `prepare --version 4.1.0` failed closed at inventory |
| [30757806987](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757806987) | 2026-08-02T16:57:48Z | `6521550a` | Release path skipped |
| [30757835682](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757835682) | 2026-08-02T16:58:34Z | `d9f0d526` | Release path skipped |
| [30757956331](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30757956331) | 2026-08-02T17:01:39Z | `d9f0d526` | Entered; `prepare --version 4.1.0` failed closed at inventory |
| [30758637451](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30758637451) | 2026-08-02T17:20:06Z | `4302301a` | Entered; `prepare --version 4.1.0` failed closed at inventory |
| [30760188983](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30760188983) | 2026-08-02T18:01:57Z | `52f4327c` | Entered; `prepare --version 4.1.0` failed closed at inventory |
| [30785942090](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/30785942090) | 2026-08-03T05:00:32Z | `8a6a6cb3` | Release path skipped |

#### Completion Notes

- GitHub had no Release in the enabled interval and no tag newer than `v4.0.1`; the latest Release
  remains [v4.0.1](https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v4.0.1),
  published 2026-07-16 with 16 package/symbol assets.
- NuGet registration records for `Cli`, `Contracts`, `Contracts.UI`, `Mcp`, `Schema`, `Shell`,
  `SourceTools`, and `Testing` contained no publication in the enabled interval. Every package's
  latest version remains `4.0.1`, published 2026-07-16.
- No partial publication was observed across the configured GitHub Release/tag and nuget.org
  surfaces. Runner-local `4.1.0` candidates are not published artifacts.
- This completes REL-5 T0 only. REL-AI-1 remains open, publication remains unauthorized, and AC6 /
  AC10 still govern any future bounded candidate.

### File List

- `_bmad-output/implementation-artifacts/rel-5-provision-signing-identity-and-first-governed-release.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

### Change Log

- 2026-08-03: Release Owner restored the coarse publication switch to exact lowercase `false`,
  audited the complete enabled window, found no partial external publication, and completed T0.
