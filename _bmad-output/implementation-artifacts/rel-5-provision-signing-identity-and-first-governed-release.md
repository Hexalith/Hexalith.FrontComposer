---
created: 2026-07-15
updated: 2026-07-15
owner: Release Owner (executes) + Developer (verification tooling/evidence assistance)
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md
status: in-progress
scope: moderate
implementationRisk: medium (operational/custodial, not code)
ordering: T1 (upstream filing + identity selection) executes immediately; ACs 5-9 trail REL-3 completion
releaseControl: REL-AI-1 closure routes through this story; the REL-4 freeze variable stays non-`true` until AC6
---

# REL-5: Provision the Production Signing Identity and Prove the First Governed Release

Status: ready-for-dev (owner-executed story; the developer agent assists with verification
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
   the caller-side authorization remains the REL-4 `HEXALITH_RELEASE_PUBLISH_ENABLED` variable and
   no approval tokens are added to FrontComposer's `release.yml`.

6. Given REL-3's exact-artifact gate is operational and a candidate release's pre-publication
   evidence is `classification=ready` with `publish_authorized=true`, when the Release Owner
   authorizes the first gated release, then authorization is an explicit recorded owner action
   (setting the freeze variable to exactly `true` for the release), and it is never granted on
   dry-run or reconstructed evidence.

7. Given the first governed release publishes, when post-publication verification runs, then the
   Release Owner confirms downloaded NuGet and GitHub assets verify (signatures, timestamps, exact
   hashes) against the sealed manifest.

8. Given verification passes, when the compliance ledger is updated, then
   `rel-ai-1-release-evidence-ledger.md` gains the first compliant release record with every
   required field populated and durable evidence paths.

9. Given the ledger records passing real-release evidence, when the Release Owner reviews
   REL-AI-1, then REL-AI-1 is closed only if every FR-24 artifact is durable and downloaded bytes
   match the authorized manifest; any gap keeps REL-AI-1 open with the exact blocker recorded.

## Tasks

- [ ] T1 — Immediate enablement (does not wait for REL-3).
  - [ ] Select and record the production package-signing identity and trust model (AC1).
        *2026-07-18 (REL-3 review constraint): the identity MUST chain to the publicly trusted
        NuGet code-signing roots — the independent verifier checks downloaded bytes against the
        stock public bundle, so an internal/self-signed CA passes preparation but always fails
        post-publication verification. Certificate acquisition remains a physical owner action.*
  - [x] File BUILD-REL-1 upstream with the full governed contract; record the URL in the G2
        request (AC4). *Filed 2026-07-18 under Release Owner directive:
        <https://github.com/Hexalith/Hexalith.Builds/issues/17> (both items: governed contract +
        common freeze gate). Accepted revision still pending.*
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
  - [ ] Record the accepted upstream revision in the G2 request (AC4).
  - [ ] Configure protected-environment reviewers if the environment input lands (AC5).
  - [ ] If upstream cannot land before a required release, decide and record the bounded
        FrontComposer-owned contingency (scope, approver, expiry/reopen trigger, migration back).
- [ ] T4 — First governed release (trails REL-3 completion).
  - [ ] Authorize the release only on ready/authorized pre-publication evidence (AC6).
  - [ ] Verify downloaded NuGet and GitHub assets against the sealed manifest (AC7).
  - [ ] Record the first compliant ledger entry (AC8) and disposition v3.2.1/v3.2.2 remediation.
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
- [ ] Compliance ledger carries the first compliant record; v3.2.1/v3.2.2 dispositions finalized.
- [ ] REL-AI-1 closed on durable evidence, or open with the exact blocker recorded.

## References

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md`
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`
- `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`
