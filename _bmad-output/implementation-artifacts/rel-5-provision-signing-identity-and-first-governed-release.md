---
created: 2026-07-15
updated: 2026-08-03
baseline_commit: 874fe13ba4d2a979898fc9b10451827bab94988c
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
  - [x] Approve and record the RFC 3161 timestamp authority (AC3). *Approved 2026-08-03:
        DigiCert
        (`http://timestamp.digicert.com`) — already the pipeline default in
        `eng/release_prepublish.py` and the `NUGET_SIGNING_TIMESTAMPER` fallback. Official
        DigiCert guidance identifies this exact lowercase URL as its RFC 3161 endpoint for
        NuGet; a live SHA-256 request returned `Granted` and verified against the .NET 10.0.302
        stock NuGet timestamp trust bundle.*
- [ ] T2 — Provision custody.
  - [ ] Provision the two signing secrets with Release Owner-only custody and a rotation
        procedure (AC2).
  - [x] Record the org-vs-repo variable/secret posture, honoring the shadowing hazard documented
        in the G2 request and deployment guide. *Approved 2026-08-03: use repository-scoped
        Actions secrets in `Hexalith/Hexalith.FrontComposer`, custodied by the Release Owner;
        do not depend on organization-secret inheritance. Keep the repository-scoped
        `HEXALITH_RELEASE_PUBLISH_ENABLED=false` override so an organization-level value cannot
        enable this repository.*
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
  - [ ] Record the first compliant ledger entry (AC8).
  - [x] Finalize the v3.2.1, v3.2.2, v4.0.0, and v4.0.1 historical dispositions.
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
- [ ] Compliance ledger carries the first compliant record.
- [x] v3.2.1, v3.2.2, v4.0.0, and v4.0.1 dispositions finalized without retroactive relabeling.
- [x] REL-AI-1 closed on durable evidence, or open with the exact blocker recorded.

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
- 2026-08-03: Release Owner approved DigiCert's RFC 3161 timestamp authority and the
  repository-scoped secret custody/rotation posture. Production identity approval and secret
  provisioning remain blocked because no production certificate or password was supplied.
- 2026-08-03: Completed the four-release historical byte/evidence reconciliation. Governed
  preflight failed closed before candidate generation; no version, manifest, authorization, or
  publication action was created.

### REL-5 T1/T2 Release Owner Execution Record (2026-08-03)

#### Implementation Plan

- Revalidate current NuGet.org production-signing requirements against official Microsoft
  documentation.
- Verify DigiCert's documented RFC 3161 URL with its health endpoint and a live SHA-256 timestamp
  response against the .NET SDK's stock NuGet timestamp trust bundle.
- Inspect only approved external environment/secure-path presence and GitHub secret names; never
  print, decode, persist, or pass certificate/password values in command arguments.
- Provision only the two authorized repository secrets if both values are available; otherwise
  stop fail-closed and leave AC1/AC2 incomplete.

#### Debug Log

- Microsoft NuGet guidance checked 2026-08-03:
  <https://learn.microsoft.com/nuget/create-packages/sign-a-package> requires a public-CA code-
  signing certificate for NuGet.org production packages, rejects self-issued production
  signatures, recommends timestamping, and requires the certificate to be registered with the
  publishing NuGet.org account or organization. The NuGet CLI reference defines `--timestamper`
  as an RFC 3161 service and defaults timestamp hashing to SHA-256.
- DigiCert suitability checked 2026-08-03:
  <https://knowledge.digicert.com/solution/troubleshooting-timestamping-problems> identifies
  `http://timestamp.digicert.com` as the exact RFC 3161 NuGet endpoint. The health endpoint returned
  HTTP 204. A live SHA-256 RFC 3161 request returned `Granted`, policy
  `2.16.840.1.114412.7.1`, and `Verification: OK` against
  `/home/administrator/.dotnet/sdk/10.0.302/trustedroots/timestampctl.pem`.
  DigiCert's current Public Trust CP/CPS also states that its timestamp authority complies with
  RFC 3161, is recommended for signed code, and synchronizes to a UTC(k) source:
  <https://www.digicert.com/content/dam/digicert/pdfs/legal/digicert-public-trust-cp-cps.pdf>.
- Approved timestamp authority: DigiCert, `http://timestamp.digicert.com`. The governed signing
  implementation already uses this exact URL as `DEFAULT_TIMESTAMPER`, so no workflow or source
  change is required for AC3.
- Approved custody posture: repository-scoped GitHub Actions secrets in
  `Hexalith/Hexalith.FrontComposer`; Release Owner is the custody and rotation owner. Repository
  scope prevents reliance on an inherited organization secret whose visibility cannot be audited
  with the current GitHub token. The repository-level publication variable was independently
  verified as exact lowercase `false` at `updated_at=2026-08-03T06:24:13Z`.
- Rotation procedure: begin replacement no later than 60 days before certificate expiry; acquire a
  successor public-CA code-signing certificate, validate its code-signing EKU/public trust chain
  and validity, register it with the publishing NuGet.org owner, update both repository secrets via
  stdin, verify names only, and run the non-publishing governed validation. Retire the predecessor
  only after a successor-signed governed release passes independent downloaded-byte verification.
  On compromise or revocation, freeze publication, rotate immediately, revoke/remove the old
  certificate at the CA and NuGet.org, audit attempted releases, and record the incident/rotation
  in this story and the compliance ledger.
- Blocking evidence: `NUGET_SIGNING_CERTIFICATE_BASE64`,
  `NUGET_SIGNING_CERTIFICATE_PASSWORD`, recognized certificate/password file variables, and
  `/run/secrets` were unavailable. GitHub's repository secret-name list was empty. Organization
  secret/variable listing returned HTTP 403, so no organization-level value is claimed absent.
  No secret was created or changed.

#### Completion Notes

- Completed only the RFC 3161 authority approval (AC3) and the repository-vs-organization posture
  record. T1 remains incomplete because no production certificate exists to verify or record its
  CA, subject, issuer, SHA-256 thumbprint, validity, NuGet.org registration, or custody artifact.
- T2 and AC2 remain incomplete because the production PFX/base64 value and password were not
  supplied. Exact Release Owner action: place the approved public-CA production PFX/base64 and its
  password in the external environment as `NUGET_SIGNING_CERTIFICATE_BASE64` and
  `NUGET_SIGNING_CERTIFICATE_PASSWORD` (or provide separately identified secure files outside the
  repository), ensure the PFX includes the full issuing chain, and provide non-secret CA/subject/
  issuer/SHA-256-thumbprint/validity plus NuGet.org registration evidence. Then rerun REL-5 T1/T2.
- `HEXALITH_RELEASE_PUBLISH_ENABLED` remains exactly `false`. No release was triggered or
  authorized.

### Historical Reconciliation and Governed Preflight Record (2026-08-03)

#### Historical result

- Completed the CI/Release/Release Evidence mapping, exact asset enumeration, SHA-256 comparison,
  `dotnet nuget verify --all`, symbols/evidence inspection, consumer lineage, and provenance audit
  for v3.2.1, v3.2.2, v4.0.0, and v4.0.1. The compliance ledger contains the complete hashes,
  timestamps, run URLs, source/tag SHAs, and evidence-availability record.
- All 32 GitHub `.nupkg` files and all 32 GitHub `.snupkg` files are unsigned (`NU3004`). All 32
  nuget.org packages verify only with NuGet.org's Repository signature and timestamp, have no
  Author signature, and differ byte-for-byte from the corresponding GitHub package.
- Historical CI consumer validation passed against independently packed `0.0.0-ci-test` packages,
  not the exact subsequently published candidates. Release Evidence later rebuilt another set;
  its checksums matched GitHub Release assets 0/16 per release. Its SBOM, checksums, inventory,
  manifest/readiness, and SLSA provenance therefore do not bind published bytes.
- CI test artifacts have expired. The original Release Evidence artifacts remain temporarily
  available under 30-day retention, but no complete FR24 evidence set is attached to any GitHub
  Release. Unavailable evidence was not reconstructed or labeled original.
- Historical dispositions remain non-compliant. Completion of this reconciliation is an explicit
  non-closing residual and cannot supply retrospective pre-publication authorization.

#### Preflight gate

| Preflight condition | Result | Evidence / blocker |
| --- | --- | --- |
| Coarse switch exactly false | **PASS** | Repository variable is exact lowercase `false`, `updated_at=2026-08-03T06:24:13Z`. |
| Accepted immutable Builds revision pinned | **FAIL** | `release.yml` mechanically pins `79f82acc9cb9259ddcb90217c89bc72024ab7f72`, but the G2 accepted-revision field remains `pending`; issue 17 closed without a qualifying BUILD-REL-1/GOV-1 governed-mode revision. The pinned commit is not owner-accepted release governance evidence. |
| Production signing identity, secrets, timestamp authority, rotation | **FAIL** | DigiCert RFC 3161 and rotation posture are approved, but no production certificate is approved and both repository signing-secret names are absent. |
| Protected reviewers and post-classification/pre-publication pause | **FAIL** | GitHub environment `production` has no protection rules or required reviewers; the current caller/reusable workflow exposes no operational protected post-evidence approval pause. |
| GOV-1 handoffs and hostile tests | **FAIL (operational)** | Local hostile suites pass 77/77, but `eng/dependency-graph-policy.json` authorizes no CI, release, or post-release evaluator and no accepted upstream governed seam can execute the two exact-candidate handoffs end to end. |
| Historical reconciliation complete or explicit non-closing residuals | **PASS** | Four releases fully reconciled; expired/temporary evidence and historically impossible pre-authorization are recorded honestly as non-closing residuals. |

#### Stop decision

Preflight is not eligible for the candidate phase. No candidate was packed, signed, timestamped,
attested, classified, or sealed; therefore there is no candidate version or manifest SHA-256, no
approval packet, and no publication authorization request. `classification=ready` and
`publish_authorized=true` have not been established. Publication remains unauthorized,
`HEXALITH_RELEASE_PUBLISH_ENABLED` remains exactly `false`, and REL-AI-1 remains open.

Exact correction required before rerunning preflight:

1. accept and record an immutable Builds revision that implements the governed release seam;
2. approve/provision the production public-CA signing identity and both repository secrets;
3. configure required reviewers and prove the protected pause after sealed evidence but before
   publication;
4. authorize GOV-1 evaluators and pass both exact-candidate handoffs end to end.

## Suggested Review Order

**Historical evidence**

- Start with the complete run, byte, signature, evidence-lineage, and classification record.
  [rel-ai-1-release-evidence-ledger.md:72](rel-ai-1-release-evidence-ledger.md#L72)

- Confirm each historical non-compliant classification remains explicit and owner-routed.
  [rel-ai-1-release-evidence-ledger.md:239](rel-ai-1-release-evidence-ledger.md#L239)

**Fail-closed preflight**

- Review the consolidated historical conclusion and governed-release preflight boundary.
  [rel-5-provision-signing-identity-and-first-governed-release.md:313](rel-5-provision-signing-identity-and-first-governed-release.md#L313)

- Verify failed prerequisites stopped execution before any candidate or authorization request.
  [rel-5-provision-signing-identity-and-first-governed-release.md:334](rel-5-provision-signing-identity-and-first-governed-release.md#L334)

**Open-gate tracking**

- Confirm sprint tracking keeps REL-AI-1 open and publication unauthorized.
  [sprint-status.yaml:642](sprint-status.yaml#L642)
