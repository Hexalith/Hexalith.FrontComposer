---
created: 2026-07-15
updated: 2026-07-15
amended: 2026-07-15 (governed-release upstream contract; ACs 18-19; operational authority split to REL-5)
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
amendmentProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md
status: ready-for-dev
scope: moderate
implementationRisk: high
releaseControl: frozen-until-real-release-evidence
---

# REL-3: Enforce FR24 Before Publication and Reconcile Affected Releases

Status: ready-for-dev.

Approval: approved by Administrator on 2026-07-15.

## Story

As the Release Owner,
I want the exact package artifacts intended for publication to pass the complete FR24 evidence gate
before any publication side effect,
so that consumers receive only authorized, verifiable packages.

## Why This Story Exists

REL-2 is done against its approved G1 criteria. Its first real executions proved that G1 generates
useful diagnostics but cannot enforce FR24:

- v3.2.2 published eight `.nupkg` and eight `.snupkg` assets;
- the published Contracts package fails `dotnet nuget verify --all` with `NU3004` (unsigned);
- `signing-readiness.json` is blocking;
- `manifest-verification.json` is invalid;
- `release-readiness.json` reports `classification=blocked` and `publish_authorized=false`;
- the Release Evidence workflow still concluded successfully;
- its evidence is a 30-day Actions artifact, not an asset on the immutable GitHub Release.

G1 reconstructs packages after semantic-release has already pushed unsigned bytes. A reconstructed
package can never establish the identity of the package NuGet received. REL-3 moves authorization in
front of every publication side effect and reserves the supplemental workflow for downloaded-artifact
verification.

## Resolved Pre-Development Decisions (2026-07-15 amendment)

Source: sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md.

- **Approval mechanism:** Release Owner authorization is the REL-4
  `HEXALITH_RELEASE_PUBLISH_ENABLED` variable on the caller side. FrontComposer's `release.yml`
  gains no `workflow_dispatch`, dry-run input, or approval environment (existing
  `ReleaseWorkflow_RunsViaWorkflowRunAfterCiSuccess` pins stay). A protected release-environment
  input belongs to the upstream BUILD-REL-1 governed contract and applies inside
  `domain-release.yml` when it lands; REL-3 does not block on it.
- **Attestation before classification:** the pre-publication chain must attest provenance over the
  exact signed candidates and bind the bundle path into the manifest before sealing and
  classification (AC18). The only alternative is the sealed Release Owner-approved
  `approved-unsupported` fallback record already modeled by `eng/release_evidence.py`.
- **Failed-run verification:** independent verification runs on Release workflow completion
  regardless of conclusion and reconciles observed NuGet/GitHub state against the sealed manifest
  (AC19).
- **Technical freeze:** already resolved by REL-4's fail-closed gate (AC1); no further REL-3 scope.
- **Upstream scope:** the Hexalith.Builds dependency is the full BUILD-REL-1 opt-in governed
  contract (environment, signing secrets, timestamp input, attestation permissions, candidate
  phase, bundle handoff, no-repack publish), not signing-secret forwarding alone.
- **Operational authority split:** provisioning the production signing identity, certificate
  custody, timestamp-authority approval, upstream filing, first-release authorization, and
  REL-AI-1 closure are executed by the Release Owner under REL-5; REL-3 retains the technical
  implementation and verification tooling.

## Acceptance Criteria

1. Given REL-3 is not yet operational, when a publish-capable release would start, then publication
   remains frozen and no NuGet or GitHub package release is authorized. The freeze is technically
   enforced by REL-4's fail-closed publish gate (`HEXALITH_RELEASE_PUBLISH_ENABLED` must be exactly
   `true`; disabled by default — see
   `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md`). REL-3 may
   re-enable publication, and may re-scope or replace the gate, only after the exact-artifact gate is
   operational and the Release Owner authorizes it.

2. Given a release version is prepared, when package inventory validation runs, then exactly the eight
   packable packages are present and `AppHost` plus the combined `UI` host remain explicit non-packable
   exceptions; unexpected projects, IDs, versions, or symbol requirements fail closed.

3. Given semantic-release supplies `${nextRelease.version}`, when preparation runs, then packages are
   packed once and every later gate and publisher consumes those candidates without repacking.

4. Given release candidates exist, when tests and package-consumer validation run, then the release
   records valid test evidence and proves both the Contracts-only and Shell/UI package-only consumer
   boundaries against those candidates.

5. Given the exact `.nupkg` candidates intended for publication, when signing runs, then every package
   is author-signed, RFC 3161 timestamped, and successfully verified; absent credentials, unsigned
   packages, invalid chains, or missing timestamps fail closed.

6. Given package evidence is prepared, when symbols and SBOMs are inspected, then every required
   `.snupkg` exists and SBOM/symbol evidence is bound to the corresponding candidate artifacts.

7. Given the evidence bundle is prepared, when checksums run, then they cover packages, symbols, SBOM,
   inventory, test results, consumer-validation results, signing verification, and supporting evidence.

8. Given checksums and release metadata exist, when the manifest is prepared, sealed, and verified,
   then it binds normalized candidate paths and hashes and verification is valid.

9. Given the sealed manifest is valid, when
   `classify-release --require-publishable` runs, then it produces `classification=ready` and
   `publish_authorized=true`; every other result exits non-zero.

10. Given any evidence is blocked, invalid, unsigned, unverified, incomplete, or unauthorized, when
    semantic-release prepares the release, then it fails before NuGet, GitHub Release, tag/changelog,
    or other external publication side effects.

11. Given publication is authorized, when NuGet and GitHub Release publishing runs, then both receive
    the same signed package bytes covered by the verified manifest; the publisher does not rebuild,
    repack, or substitute artifacts.

12. Given the initial GitHub Release is created, when assets are inspected, then packages, symbols,
    SBOM, inventory, checksums, signing verification, tests, consumer results, sealed/verified manifest,
    and release readiness are durable release assets; a short-retention Actions artifact alone fails
    the criterion.

13. Given publication completes, when independent post-publication verification runs, then it downloads
    the NuGet and GitHub assets, verifies package signatures, and compares all hashes with the sealed
    manifest.

14. Given any publication phase partially succeeds, when the failure is handled, then a
    `partial-publish-incident.json` record identifies the failed phase and observed NuGet/GitHub state,
    the release is failed, and success cannot be reported until owner-led reconciliation.

15. Given releases created under G1, when historical reconciliation runs, then the controlled ledger
    records at least v3.2.1 and v3.2.2 with release/run URLs, package inventory, signing/timestamp state,
    manifest/readiness state, consumer evidence, durable paths, disposition, owner, and remediation.

16. Given governance tests run, when they inspect the release model, then they reject unsigned
    record-and-proceed, missing `--require-publishable`, reconstructed-package authorization,
    non-authorized publish paths, and post-publication authorization.

17. Given a real release completes, when the Release Owner reviews REL-AI-1, then REL-AI-1 closes only
    if every FR24 artifact is durable, `classification=ready`, `publish_authorized=true`, and downloaded
    NuGet/GitHub bytes match the authorized manifest.

18. Given signed candidate packages exist, when provenance attestation runs in the governed release
    workflow, then an attestation bundle covering the exact candidate `.nupkg` files is produced
    before manifest sealing and classification and its path is bound into the sealed manifest with
    `attestation_status=attested`; when the upstream governed contract is unavailable and the
    Release Owner has approved the bounded contingency, a sealed `approved-unsupported` fallback
    record is the only accepted alternative; any other state fails classification.

19. Given a Release workflow run concludes with failure or cancellation after any publication side
    effect, when independent post-publication verification runs, then it still executes (triggered
    on workflow completion regardless of conclusion), reconciles observed NuGet and GitHub state
    against the sealed manifest or records the manifest's absence, creates or updates the
    partial-publication incident record, and no compliant ledger disposition is possible until
    owner-led reconciliation completes.

## Required Artifact Invariant

```text
Pack once
  → validate inventory, tests, and package consumers
  → generate SBOM and symbol evidence
  → sign and timestamp the exact .nupkg files
  → verify signatures and timestamps
  → attest build provenance over the exact signed packages (bundle bound into the manifest,
    or a sealed owner-approved unsupported-attestation fallback)
  → checksum packages, symbols, and evidence
  → seal and verify the release manifest
  → classify-release --require-publishable
  → publish those same authorized bytes
  → verify published NuGet and GitHub assets
```

## Implementation Boundary

- **Hexalith.Builds:** its owner must land the BUILD-REL-1 opt-in governed release contract —
  protected release environment, `NUGET_SIGNING_CERTIFICATE_BASE64` /
  `NUGET_SIGNING_CERTIFICATE_PASSWORD`, configurable RFC 3161 timestamp input, `id-token: write` +
  `attestations: write` scoped to the governed steps, a version-aware pre-publication candidate
  phase, `actions/attest-build-provenance` over the exact candidates with the bundle path handed to
  manifest finalization, and no-repack publication — per the G2 request document. Do not directly
  modify or commit the shared submodule from FrontComposer.
- **FrontComposer:** owns the semantic-release lifecycle command, artifact/evidence paths,
  classification, same-byte publication, GitHub assets, downloaded-artifact verification, tests, and
  ledger.
- **Contingency:** if the shared contract cannot land before a required release, stop and obtain explicit
  Release Owner approval for a bounded FrontComposer-owned gated release workflow. G1 is not a fallback.

## Tasks

- [ ] T1 — Confirm the release freeze and upstream dependency.
  - [ ] Record the Hexalith.Builds owner-approved issue/story URL and accepted revision in the G2
        request; the filed scope must be the full BUILD-REL-1 governed contract (environment,
        signing secrets, timestamp input, attestation permissions, candidate phase, bundle
        handoff), not signing-secret forwarding alone (filing itself is REL-5 owner work).
  - [ ] Confirm the two signing secrets and timestamp configuration are available to semantic-release
        without exposing them to unrelated steps.
  - [ ] If blocked upstream, record explicit approval and scope for the bounded owned-workflow
        contingency before editing release topology.

- [ ] T2 — Build a repository-owned pre-publication orchestration command.
  - [ ] Consume `${nextRelease.version}` and pack once using `eng/release-package-inventory.json`.
  - [ ] Run inventory, tests, package-only consumer validation, SBOM, signing/timestamp verification,
        checksums, manifest preparation/sealing/verification, and publishable classification in order.
  - [ ] Bind the provenance attestation-bundle path over the exact signed candidates into the
        manifest before sealing; fail classification when attestation evidence is neither
        `attested` nor a sealed owner-approved fallback.
  - [ ] Keep signing material out of artifacts, logs, summaries, and manifest fields.
  - [ ] Fail before side effects on every missing/blocked result.

- [ ] T3 — Make semantic-release publish only authorized artifacts.
  - [ ] Replace the pack-only `prepareCmd` with the orchestration command.
  - [ ] Make `publishCmd` re-verify the sealed manifest/readiness immediately before pushing.
  - [ ] Push only signed manifest-authorized `.nupkg` paths and matching `.snupkg` paths.
  - [ ] Configure `@semantic-release/github` to attach the full evidence chain during initial release
        creation.
  - [ ] Record and fail partial publication.

- [ ] T4 — Refactor supplemental Release Evidence into independent verification.
  - [ ] Trigger on Release workflow completion regardless of conclusion; resolve its tag/version
        without repacking, no-op only when no publication side effect occurred, and run full
        reconciliation for failed or partial runs.
  - [ ] Download the durable manifest/evidence plus NuGet and GitHub assets.
  - [ ] Verify signatures and exact hashes, then update the historical ledger.
  - [ ] Fail on absent, altered, unsigned, incomplete, or partially published assets.

- [ ] T5 — Reverse G1 governance and add negative coverage.
  - [ ] Prove missing signing credentials and invalid timestamps stop preparation.
  - [ ] Require `classify-release --require-publishable` and authorized publish paths.
  - [ ] Prove the publisher cannot repack, substitute, or consume unsigned paths.
  - [ ] Prove durable initial-release evidence assets are configured.
  - [ ] Prove post-publication evidence cannot authorize a release retroactively.
  - [ ] Prove classification fails when attestation evidence is absent and no sealed
        owner-approved fallback exists.
  - [ ] Cover the partial-publication incident path.

- [ ] T6 — Validate without publishing, then obtain real-release evidence.
  - [ ] Run the complete preparation/classification path in a non-publishing context.
  - [ ] Run the relevant governance and consumer-validation lanes locally; treat GitHub workflow
        execution and secret availability as CI-authoritative.
  - [ ] Obtain Release Owner authorization for the next real release only after pre-publication evidence
        is ready/authorized (authorization execution is owned by REL-5).
  - [ ] Record durable evidence and downloaded-artifact verification; keep REL-AI-1 open on any gap.

- [ ] T7 — Reconcile documentation and historical releases.
  - [ ] Keep the deployment guide aligned with implemented behavior, not intended behavior.
  - [ ] Complete v3.2.1/v3.2.2 ledger entries and remediation disposition.
  - [ ] Add the final upstream issue/revision and real-release evidence URLs (recorded from REL-5
        outcomes).

## Engineering Guardrails

- Never initialize nested submodules; use only root-declared `references/...` paths.
- Do not edit the Hexalith.Builds submodule without its explicit owner workflow.
- Do not log certificate material, passwords, raw secrets, private keys, or unsanitized absolute paths.
- A successful shell command or GitHub job is not equivalent to a ready release; machine-readable
  readiness fields decide authorization.
- `--skip-duplicate` does not make partial publication safe. Detect existing/missing packages by exact
  version and record an incident when phases diverge.
- A local signing root may be useful for non-publishing validation but does not establish a credible
  public package identity. The Release Owner approves the production signing identity and trust model.
- Preserve unrelated worktree changes.

## Verification

- Governance tests for workflow/config shape and all negative authorization paths.
- Evidence-helper unit/fixture tests for exact paths, checksums, manifest verification, publishable
  classification, downloaded-artifact comparison, and incident records.
- Package-only consumer restore/build tests for Contracts-only and Shell/UI boundaries.
- `dotnet nuget verify --all` over every candidate and every downloaded published package.
- Non-publishing end-to-end orchestration run using the actual inventory and semantic-release version
  contract.
- One CI-authoritative real release with durable assets and post-publication hash verification.

## Definition of Done

- [ ] All acceptance criteria and tasks are complete.
- [ ] The shared-workflow dependency or approved contingency is recorded with durable evidence.
- [ ] No G1 record-and-proceed path remains in the publish-capable lifecycle.
- [ ] The next real release proves ready/authorized pre-publication and exact-byte post-verification.
- [ ] v3.2.1 and v3.2.2 are reconciled in the ledger.
- [ ] REL-AI-1 closure is supported by durable real-release evidence, or remains open with the exact
      blocker.

## File List

Implementation file list to be populated by the development workflow.
