---
created: 2026-07-15
updated: 2026-07-18
amended: 2026-07-16 (freeze truth-state; approval-mechanism contract in AC20; prior governed-release and operational-authority amendments retained)
owner: Release Owner + Developer + QA/Test Architect
sourceProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
amendmentProposal: _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md
status: in-review
baseline_commit: 5c284c89d37dfc3d39593962631e376bd4c5e033
scope: moderate
implementationRisk: high
releaseControl: frozen-until-real-release-evidence
---

# REL-3: Enforce FR24 Before Publication and Reconcile Affected Releases

Status: in-review.

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

20. Given the machine-readable `APPROVAL_MATRIX`, when REL-3 implements release authorization, then it describes the actual approved mechanisms: the Release Owner-controlled REL-4 variable, publishable readiness evidence, and any upstream protected release environment. It must not name `workflow_dispatch`, `release_owner_approved`, or `release_approver` inputs that `release.yml` forbids. Governance tests pin consistency between the helper, release workflow, REL-4, and REL-5.

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

- [ ] T1 — Confirm the release freeze and upstream dependency. **(owner-blocked residuals; freeze
      itself is confirmed: REL-4's fail-closed gate landed 2026-07-18 as this story's stop-the-line
      precondition — see `rel-4-enforce-temporary-release-freeze.md`.)**
  - [ ] Record the Hexalith.Builds owner-approved issue/story URL and accepted revision in the G2
        request; the filed scope must be the full BUILD-REL-1 governed contract (environment,
        signing secrets, timestamp input, attestation permissions, candidate phase, bundle
        handoff), not signing-secret forwarding alone (filing itself is REL-5 owner work).
        *2026-07-18: no upstream filing exists yet (REL-5 AC); the G2 request document already
        carries the full contract including the common freeze gate.*
  - [ ] Confirm the two signing secrets and timestamp configuration are available to semantic-release
        without exposing them to unrelated steps. *2026-07-18: CI-authoritative and blocked on the
        upstream BUILD-REL-1 forwarding; the orchestrator consumes them from env only inside the
        signing phase and never logs them.*
  - [ ] If blocked upstream, record explicit approval and scope for the bounded owned-workflow
        contingency before editing release topology. *2026-07-18: contingency not triggered — the
        REL-4 freeze means no release is required before upstream lands; release.yml's delegation
        topology is unchanged.*

- [x] T2 — Build a repository-owned pre-publication orchestration command.
  - [x] Consume `${nextRelease.version}` and pack once using `eng/release-package-inventory.json`.
  - [x] Run inventory, tests, package-only consumer validation, SBOM, signing/timestamp verification,
        checksums, manifest preparation/sealing/verification, and publishable classification in order.
  - [x] Bind the provenance attestation-bundle path over the exact signed candidates into the
        manifest before sealing; fail classification when attestation evidence is neither
        `attested` nor a sealed owner-approved fallback.
  - [x] Keep signing material out of artifacts, logs, summaries, and manifest fields.
  - [x] Update `eng/release_evidence.py` so `APPROVAL_MATRIX` records the actual REL-4 variable,
        publishable readiness evidence, and upstream protected release environment without naming
        prohibited `workflow_dispatch`, `release_owner_approved`, or `release_approver` inputs.
  - [x] Fail before side effects on every missing/blocked result.

- [x] T3 — Make semantic-release publish only authorized artifacts.
  - [x] Replace the pack-only `prepareCmd` with the orchestration command.
  - [x] Make `publishCmd` re-verify the sealed manifest/readiness immediately before pushing.
  - [x] Push only signed manifest-authorized `.nupkg` paths and matching `.snupkg` paths.
  - [x] Configure `@semantic-release/github` to attach the full evidence chain during initial release
        creation.
  - [x] Record and fail partial publication.

- [x] T4 — Refactor supplemental Release Evidence into independent verification.
  - [x] Trigger on Release workflow completion regardless of conclusion; resolve its tag/version
        without repacking, no-op only when no publication side effect occurred, and run full
        reconciliation for failed or partial runs.
  - [x] Download the durable manifest/evidence plus NuGet and GitHub assets.
  - [x] Verify signatures and exact hashes, then update the historical ledger. *(Deliberately
        re-scoped from the task's literal wording: the workflow does NOT write the ledger. The
        controlled ledger is owner-updated by design — a workflow committing to the repository
        would violate the controlled-ledger discipline and the no-commit rule — so the workflow
        emits a machine-readable `ledger-record.json` disposition proposal the Release Owner
        applies. Recorded here per review BH-17 so the re-scope is explicit, not silent.)*
  - [x] Fail on absent, altered, unsigned, incomplete, or partially published assets.

- [x] T5 — Reverse G1 governance and add negative coverage.
  - [x] Prove missing signing credentials and invalid timestamps stop preparation. *(Evidence
        precision per review BH-15: the missing-credential abort is proven by a runtime negative
        plus static fail-closed pins. Timestamp enforcement rides `dotnet nuget verify --all`
        gating every signed candidate — proven end-to-end by the T6 non-publishing run — but a
        dedicated invalid-timestamper runtime negative does not exist; a regression that keeps
        the abort literal while breaking the raise would need the T6 lane, not this suite, to
        surface.)*
  - [x] Require `classify-release --require-publishable` and authorized publish paths.
  - [x] Prove the publisher cannot repack, substitute, or consume unsigned paths.
  - [x] Prove durable initial-release evidence assets are configured.
  - [x] Prove post-publication evidence cannot authorize a release retroactively.
  - [x] Prove classification fails when attestation evidence is absent and no sealed
        owner-approved fallback exists.
  - [x] Prove `APPROVAL_MATRIX`, `release.yml`, REL-4, and REL-5 agree on the authorization
        mechanism and contain none of the forbidden dispatch/approver input tokens.
  - [x] Cover the partial-publication incident path.

- [ ] T6 — Validate without publishing, then obtain real-release evidence. **(local validation
      complete; real-release evidence is REL-5-owned and REL-AI-1 stays open)**
  - [x] Run the complete preparation/classification path in a non-publishing context.
        *2026-07-18: `prepare --version 9.9.9 --non-publishing` green end-to-end — pack-once (8
        `.nupkg` + 8 `.snupkg`), inventory, 7-project release test lane (4,176 tests, 0 failures),
        consumer validation, SBOM + symbols, local-chain signing with `dotnet nuget verify --all`
        (8/8 verified), checksums, sealed manifest `verification=valid`,
        `classify-release --require-publishable` → `fallback-approved`, `publish_authorized=false`,
        `local-candidate` context, exit 0.*
  - [x] Run the relevant governance and consumer-validation lanes locally; treat GitHub workflow
        execution and secret availability as CI-authoritative. *2026-07-18: 96/96 across
        CiGovernanceTests, ReleaseModelGovernanceTests, Story12_4_RedPhaseDefTests,
        AnalyzerPolicyGovernanceTests, InfrastructureGovernanceTests (direct xUnit v3 runner,
        Release build 0 warnings/errors).*
  - [ ] Obtain Release Owner authorization for the next real release only after pre-publication evidence
        is ready/authorized (authorization execution is owned by REL-5).
  - [ ] Record durable evidence and downloaded-artifact verification; keep REL-AI-1 open on any gap.

- [ ] T7 — Reconcile documentation and historical releases. **(REL-5-owned residual only)**
  - [x] Keep the deployment guide aligned with implemented behavior, not intended behavior.
  - [x] Complete v3.2.1/v3.2.2 ledger entries and remediation disposition. *(Entries complete since
        2026-07-15; a dated status note records that REL-3/REL-4 enforcement landed without
        changing any disposition.)*
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

## Implementation Record (2026-07-18)

Intent: "REL-AI-1: Own the FR24 exact-artifact pre-publication gate" (bmad-quick-dev; baseline
`5c284c89`). REL-4's freeze gate was implemented first per the sprint-status stop-the-line mandate.

- **Orchestrator** (`eng/release_prepublish.py`, new): `prepare` enforces the Required Artifact
  Invariant fail-closed in order with no record-and-proceed signing path; `publish` re-verifies the
  sealed manifest/readiness, audits every manifest row (path shape + exact sha256), pushes only
  manifest-authorized `nupkgs-signed/*.nupkg` + matching symbols without `--skip-duplicate`, and
  records typed partial-publish incidents. `--non-publishing` performs honest local validation
  (validation-scoped AC18 fallback + approval inputs; `publish_authorized` stays false in the
  `local-candidate` context).
- **semantic-release** (`.releaserc.json`): `prepareCmd`/`publishCmd` are the orchestrator;
  `@semantic-release/github` attaches signed packages, symbols, and the full evidence chain as
  durable assets at initial release creation (AC12).
- **Independent verification** (`.github/workflows/release-evidence.yml`, rewritten): triggers on
  Release completion regardless of conclusion (AC19), downloads GitHub Release assets + nuget.org
  flat-container bytes, three-way sha256 compare against the sealed manifest, `dotnet nuget verify`
  with the public trust bundle only, retroactive-authorization ban, typed incidents, and a
  `ledger-record.json` disposition proposal. It never rebuilds, repacks, re-signs, or attests.
- **AC20 approval mechanism**: `APPROVAL_MATRIX` rewritten to the REL-4 variable + publishable
  readiness evidence + upstream protected environment (zero forbidden dispatch/approver tokens);
  the orchestrator supplies classifier approval inputs derived from the REL-4 gate context in the
  publish-capable path (reaching `domain-release.yml` proves `HEXALITH_RELEASE_PUBLISH_ENABLED`
  was exactly `true`), plus `--concurrent-same-version false` justified by the release.yml
  concurrency group + semantic-release tag atomicity. `eng/release_evidence.py` `__version__`
  1.1.0 → 1.2.0.
- **Helper fix** (`eng/release_evidence.py` `classify_release`): the round-8 `--dry-run-clean-exit`
  gate required a non-empty blocking list, but the round-10 P247 carve-out empties it for exactly
  the healthy dry-run the gate was built for — dead seam removed (empty list now satisfies the
  allowlist; classification gating unchanged).
- **Governance** (75 release-model tests + suites): REL-4 freeze pins, new
  `ReleaseModelGovernanceTests` (9 negative/consistency tests covering every T5 bullet),
  `.releaserc.json`/verifier pins reversed from G1, Story12_4 Def14 attestation pins reversed
  (attestation belongs upstream, not the verifier), CA1707 identifier inventory refreshed
  (6174 → 6185) in `analyzer-policy-exception-ledger-v1.json`.
- **Fail-closed proof in anger**: the first T6 run failed closed on 2 real governance regressions
  (analyzer-inventory drift from the new tests; a package-catalog assertion tracing to submodule
  checkout state) — both fixed, definitive run green.
- **Worktree note for the commit step**: `references/Hexalith.Builds` (337f023) and
  `references/Hexalith.Tenants` (2d85e35) checkouts are ahead of the recorded gitlinks — in-flight
  catalog-centralization state the committed `InfrastructureGovernanceTests` already expects
  (`System.Reactive 7.0.0-rc.1`); the commit should include those gitlink bumps.
- **CI-authoritative residuals (REL-AI-1 stays open)**: workflow_run→workflow_run chaining, the
  first frozen Release run URL (REL-4 AC6), upstream BUILD-REL-1 filing/landing, signing-secret
  provisioning, first governed real release with durable evidence and downloaded-byte verification
  (REL-5).

## Review Findings (2026-07-18, adversarial 3-layer round)

Blind Hunter, Edge Case Hunter, and Verification Gap ran over the full baseline diff. No
intent-gap or bad-spec findings (no loopback); 30 patch findings were applied in two passes and
5 residuals were routed to the deferred-work ledger.

- **High (fixed):** symbol integrity was anchored to the unsealed `checksums.json` with fail-open
  skips when rows were missing — `prepare-manifest` now seals a per-row `symbol_checksum`, and both
  the publisher and the verifier fail closed on unsealed/malformed/mismatched symbols.
- **Verifier hardening (fixed):** immutability probe restored (non-immutable release fails),
  404-vs-transient distinction, zero-asset/download/JSON-decode typed incidents, per-package
  download deadline, surplus-asset detection, deleted-tag orphan-release probe, incident on
  missing evidence assets and on failed signature verification.
- **Orchestrator hardening (fixed):** stale-evidence purge at prepare start, sanitized FAIL-CLOSED
  on all crash paths, typed fallback-digest read, fd hygiene, `sudo -n` (no interactive hang),
  local trust-store snapshot/restore in `--non-publishing`, sanitized push-failure detail,
  path-confinement on manifest rows, openssl password via env indirection, run-id-traceable
  approval mechanism, publisher incident echoed into the job log.
- **Verification gaps (fixed):** runtime CLI test now traverses the real `--dry-run-clean-exit`
  exit gate (mutation-checked), runtime publisher negatives prove the pre-push audit fails closed
  on post-seal mutation and version mismatch (new `tests/ci-governance/stage_release_state.py`
  staging helper), `.yaml` glob and weakened pins tightened.
- **Artifact honesty (fixed):** spec status line, T4 ledger re-scope wording, T5 timestamp-evidence
  precision, deployment-guide contradiction + upstream job-timeout sizing note, the hard REL-5
  requirement that the production signing identity chain to publicly trusted NuGet code-signing
  roots, stale `release.yml` header comments.

Final validation: governance suites 78/78 green (Release build 0 warnings/0 errors); the
`--dry-run-clean-exit` fix and both publisher audits are proven by runtime negatives. The
post-review full-chain re-run fails closed in its test phase on an **unrelated main-branch
baseline inconsistency** (committed catalog pins expect `System.Reactive 7.0.0-rc.1` while the
parallel AngleSharp-remediation gitlink `08b5708` pins `7.0.0`; see the deferred-work entry) —
the authoritative full-green chain evidence is the 2026-07-18 run recorded under T6, and the
fail-closed lane catching the parallel regression is the gate behaving as specified.

## File List

- `.github/workflows/release.yml` — REL-4 freeze-guard gate (stop-the-line precondition)
- `.github/workflows/release-evidence.yml` — rewritten as independent post-publication verification
- `.releaserc.json` — orchestrator prepare/publish + durable evidence assets
- `.gitignore` — generated output dirs (`nupkgs-signed/`, `release-evidence/`, `verification-evidence/`, `TestResults/`)
- `eng/release_prepublish.py` — new pre-publication orchestration command
- `eng/release_evidence.py` — APPROVAL_MATRIX rewrite, dry-run-clean-exit fix, `__version__` 1.2.0
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` — REL-4 pins, releaserc/verifier pin reversal, helper-version literal
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/ReleaseModelGovernanceTests.cs` — new T5 negative/consistency suite
- `tests/Hexalith.FrontComposer.Shell.Tests/Governance/Story12_4_RedPhaseDefTests.cs` — Def14 attestation pin reversal
- `_bmad-output/contracts/analyzer-policy-exception-ledger-v1.json` — CA1707 inventory refresh
- `_bmad-output/project-docs/deployment-guide.md` — implemented-behavior alignment
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md` — dated status note
- `_bmad-output/implementation-artifacts/rel-4-enforce-temporary-release-freeze.md` — implementation record, status in-review
- `_bmad-output/implementation-artifacts/deferred-work.md` — CR-12-4-Def14 superseded reconciliation
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — rel-3 in-progress, rel-4 review
