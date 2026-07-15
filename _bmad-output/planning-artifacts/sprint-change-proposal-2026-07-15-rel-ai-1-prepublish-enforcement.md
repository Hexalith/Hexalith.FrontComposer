---
title: Sprint Change Proposal - Enforce FR24 Before Publication and Reconcile Affected Releases
project: frontcomposer
date: 2026-07-15
workflow: bmad-correct-course
mode: Incremental
trigger: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
intent: Replace the insufficient G1 post-publication evidence posture with an exact-artifact, fail-closed pre-publication gate before the next release, and reconcile releases produced under G1.
supersedes_relationship: supersedes-g1-gating-decision
prior_proposals:
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-13-rel-ai-1-fr24-rehome-into-rel-2.md
status: approved
approval: approved-by-administrator-2026-07-15
scope: Moderate
implementation_risk: High
owner: Release Owner
release_control: frozen-until-rel-3-real-release-evidence
---

# Sprint Change Proposal — Enforce FR24 Before Publication and Reconcile Affected Releases

## Section 1 — Issue Summary

`REL-AI-1` owns PRD **FR24** and Release Governance Gate **RG-1**. The July 13 course correction
approved **G1**: REL-2 could publish through the shared Hexalith.Builds workflow, then run a
FrontComposer-owned evidence workflow after publication. G1 was intended to produce evidence now and
fail closed on a later release if the evidence exposed a problem.

The first real executions prove that G1 produces useful diagnostics but does **not** enforce FR24.
FrontComposer v3.2.2 was published, its evidence workflow concluded `success`, and the retained evidence
simultaneously reported all of the following:

- `signing-readiness.json`: `signed=false`, `verified=false`, `blocking=true` because no signing
  certificate was provisioned;
- `manifest-verification.json`: `status=invalid`, with 40 diagnostics covering missing signed-package
  checksums, unverified signing/timestamps, and missing sealed artifacts;
- `release-readiness.json`: `classification=blocked` and `publish_authorized=false`;
- `package-inventory.json`: the expected eight packable packages and two explicit non-packable projects;
- `test-results.json`: 4,122 tests and zero failures;
- the upstream CI package-consumer validation step succeeded.

The v3.2.2 GitHub Release contains 16 package assets (eight `.nupkg` and eight `.snupkg`) but no durable
release-evidence assets. The evidence bundle exists only as a 30-day Actions artifact because the
release was already immutable. Direct verification of the published
`Hexalith.FrontComposer.Contracts.3.2.2.nupkg` returned `NU3004: The package is not signed`.

Authoritative execution references:

- GitHub Release: <https://github.com/Hexalith/Hexalith.FrontComposer/releases/tag/v3.2.2>
- successful Release Evidence run: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375505915>
- successful upstream CI run: <https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/29375165477>

### Root cause

The failure is architectural, not merely a missing certificate:

1. `references/Hexalith.Builds/.github/workflows/domain-release.yml` forwards `NUGET_API_KEY` and runs
   `npx semantic-release`; it exposes no pre-publication evidence phase and does not forward package
   signing credentials.
2. `.releaserc.json` packs to `nupkgs/` and directly pushes unsigned `.nupkg`/`.snupkg` files.
3. `.github/workflows/release-evidence.yml` starts only after `Release` completes, reconstructs packages,
   and cannot establish the identity of the bytes NuGet already received.
4. Its `require_or_record` branch deliberately records unsigned/invalid evidence without failing, and
   `classify-release` is invoked without `--require-publishable`.
5. Therefore a green workflow conclusion can contain a blocked release decision.

The original “before v1.0 RC” deadline is also no longer actionable: public releases have progressed to
v3.2.2. The corrected control must block the **next** NuGet or GitHub package publication and must record
the compliance state of releases already produced under G1.

**Approved problem statement:** REL-AI-1's G1 implementation produces useful post-publication evidence
but does not enforce FR24. FrontComposer can publish unsigned packages while the manifest is invalid and
readiness is blocked, yet report a successful evidence workflow. Because releases have already
progressed to v3.2.2, the correction must govern the next release and define reconciliation for affected
historical releases rather than retain impossible before-v1.0 framing.

## Section 2 — Impact Analysis

### Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the live REL-AI-1/REL-2 execution and its release evidence, not an unimplemented product story. |
| 1.2 Core problem | [x] | Failed approach plus technical limitation: G1 permits publication before authorization and reports workflow success for blocked evidence. |
| 1.3 Evidence | [x] | v3.2.2 package verification, release assets, CI, Release Evidence run, and downloaded JSON evidence establish the gap. |
| 2.1 Current epic impact | [x] | Keep RG-1. No product epic is invalidated or reopened. |
| 2.2 Epic-level changes | [!] | Add REL-3 as the focused implementation owner. Keep REL-2 done against its approved G1 criteria. |
| 2.3 Future epic impact | [!] | Epic 11 work may continue, but any package/public-surface change feeds the corrected gate and may not auto-publish first. |
| 2.4 New/remove epics | [x] | No new product epic. Add one release-governance story under RG-1. |
| 2.5 Priority/order | [!] | Stop the line before the next publish-capable release. Product development need not stop. |
| 3.1 PRD conflicts | [!] | FR24/NFR-12 require a publication gate, while the accepted G1 wording permits publication before evidence. D-6 and the v1.0 deadline are stale. |
| 3.2 Architecture conflicts | [!] | Runtime architecture is unaffected; delivery architecture is invalid because a post-publication repack is not the artifact NuGet received. |
| 3.3 UX conflicts | [N/A] | No user-interface or interaction-design impact. |
| 3.4 Other artifacts | [!] | Epics, sprint status, REL-3, release workflows, semantic-release config, evidence helper/tests, deployment guide, upstream request, and historical ledger require changes. |
| 4.1 Direct adjustment | Viable | Exact-artifact gate in semantic-release preparation. Effort high; implementation risk high. |
| 4.2 Rollback | Contingency | If the shared workflow cannot support signing secrets, use a bounded FrontComposer-owned gated release workflow. |
| 4.3 PRD rebaseline | Necessary, insufficient | Replace obsolete v1.0 framing, but do not treat wording changes as enforcement. |
| 4.4 Recommended path | [x] | Hybrid Direct Adjustment: mandatory shared-workflow support plus repository-owned exact-artifact orchestration and a bounded contingency. |
| 5.1–5.5 Proposal components | [x] | Captured in Sections 3–5 and approved incrementally on 2026-07-15. |
| 6.1–6.2 Final review | [x] | Proposal is specific, testable, and preserves product scope. |
| 6.3 Final approval | [x] | Approved by Administrator on 2026-07-15 after incremental proposal review and compiled-proposal confirmation. |
| 6.4 Sprint status update | [x] | Applied on approval while preserving unrelated in-progress edits. |
| 6.5 Handoff | [x] | Moderate course correction with high-risk release implementation; route to Release Owner, Developer, QA/Test Architect, Architect, Product Owner, and Hexalith.Builds owner. |

### Epic and story impact

- Keep **RG-1 / REL-AI-1** as the release-governance owner.
- Keep **REL-2** `done`; it implemented the accepted G1 posture and exposed the decisive evidence. Do not
  rewrite its history or reopen it against criteria added after completion.
- Add **REL-3: Enforce FR24 before publication and reconcile affected releases** as `ready-for-dev`.
- Keep **REL-AI-1** open with `implementation_story: "REL-3"` until a real release proves the corrected
  gate.
- Epic 11 and other product work may continue, but no automatic package release may occur until REL-3
  authorizes it.

### Requirements and architecture impact

FR24, NFR-12, SM-2, the PRD risk register, and decision D-6 must distinguish workflow execution from
release authorization. Delivery architecture must bind evidence to the exact publishable bytes. No
runtime component, public product behavior, UI, or MVP scope changes.

### Executable artifact impact

| Artifact | Required correction |
| --- | --- |
| `references/Hexalith.Builds/.github/workflows/domain-release.yml` | Upstream owner change to declare and forward signing secrets and timestamp configuration to semantic-release. |
| `.github/workflows/release.yml` | Forward the signing contract; retain CI-success provenance and required permissions. |
| `.releaserc.json` | Prepare and authorize exact artifacts before publish; push only authorized signed bytes; attach durable evidence during initial release creation. |
| `.github/workflows/release-evidence.yml` | Stop repacking/signing after publish; become independent verification and ledger update only. |
| `eng/release_evidence.py` and release scripts | Bind consumer results and exact artifact identity; require publishable classification; verify published NuGet/GitHub bytes; record partial publication. |
| Governance tests | Reject G1 record-and-proceed and pin the corrected fail-closed lifecycle. |
| Deployment and dependency docs | Correct the overstated G1 behavior and track the mandatory upstream dependency/contingency. |

## Section 3 — Recommended Approach

Use a **Hybrid Direct Adjustment**. Make the minimum shared-workflow contract change needed to pass
signing configuration into semantic-release, while keeping FrontComposer's product-specific release
orchestration in this repository.

### Required artifact invariant

```text
Pack once
  → validate inventory, tests, and package consumers
  → generate SBOM and symbol evidence
  → sign and timestamp the exact .nupkg files
  → verify signatures and timestamps
  → checksum packages, symbols, and evidence
  → seal and verify the release manifest
  → classify-release --require-publishable
  → publish those same authorized bytes
  → verify published NuGet and GitHub assets
```

No step after packing may rebuild or replace an authorized package. Pre-publication authorization and
post-publication verification are distinct phases; the latter can detect an incident but can never
authorize a release retroactively.

### Ordered correction

1. Freeze publish-capable FrontComposer releases.
2. Create REL-3 and move active REL-AI-1 implementation ownership from REL-2 to REL-3.
3. Change the G2 request from optional follow-up to a mandatory, owner-approved Hexalith.Builds
   dependency.
4. Extend the reusable workflow to declare and forward optional package-signing secrets and timestamp
   configuration.
5. Invoke the exact release-evidence lifecycle from the repository-owned semantic-release `prepareCmd`.
6. Make `publishCmd` re-verify authorization and push the same signed bytes; configure the initial
   GitHub Release to include packages and durable evidence.
7. Treat blocked/invalid evidence or `publish_authorized=false` as a hard release failure before any
   external side effect.
8. Retain the supplemental workflow only for post-publication package/download verification and the
   historical ledger.
9. If the shared change cannot land before the next release, require explicit Release Owner approval
   for a bounded FrontComposer-owned gated workflow; do not fall back to G1.
10. Reconcile at least v3.2.1 and v3.2.2 in the historical compliance ledger.

### Options rejected or constrained

- **Status-only closure:** rejected; current evidence explicitly says the release is blocked.
- **Continue G1:** rejected; it allows one known-bad release and cannot prove published artifact identity.
- **Generic `pre-publish-command` alone:** insufficient. The called workflow must also expose signing
  material to semantic-release. Repository lifecycle commands can then use `${nextRelease.version}`.
- **Permanent bespoke release fork:** rejected as the primary path because it abandons the shared
  Hexalith release model. It remains the bounded contingency only.
- **Product-scope rollback:** unnecessary. This correction changes release safety, not features.

### Scope and risk

Course-correction scope is **Moderate**: one new release-governance story and coordinated planning,
architecture, documentation, and sprint changes, without feature re-planning. Implementation risk is
**High** because the work touches irreversible publication, signing identity, two repositories, and
partial-publication recovery.

## Section 4 — Detailed Change Proposals

### Proposal A — Correct PRD release-governance requirements

Artifact: `_bmad-output/planning-artifacts/prd.md`

Sections: FR-24, NFR-12, SM-2, Risk Register, D-6.

Replace FR-24's G1-compatible wording with:

> FrontComposer must publish only the expected NuGet package set, using package artifacts that were
> signed, timestamped, verified, checksummed, manifest-bound, consumer-validated, and classified as
> publishable before any NuGet or GitHub Release side effect.

Add these consequences:

- Validated package bytes must be identical to published bytes; post-publication reconstruction is not
  equivalent evidence.
- Inventory, symbols, SBOM, consumer validation, test evidence, signature/timestamp verification,
  checksums, sealed-manifest verification, and `classify-release --require-publishable` run before
  publication.
- A blocked classification or `publish_authorized=false` fails the release.
- Durable evidence is attached during initial GitHub Release creation or stored in an approved
  equivalent; a 30-day workflow artifact is insufficient.
- Historical releases with blocked evidence are non-compliant until reconciled in the ledger.
- REL-AI-1 closes only on a real release with `classification=ready`, `publish_authorized=true`, verified
  signing/timestamping, a valid manifest, exact checksums, consumer validation, and durable paths.

Replace NFR-12 with:

> Signed and timestamped NuGet packages, symbols, SBOM, exact package inventory, consumer validation,
> checksums, a valid sealed manifest, and `publish_authorized=true` readiness evidence are blocking
> pre-publication requirements. Evidence must bind the exact published bytes.

Replace SM-2 with:

> Before every publish-capable release, exact artifacts intended for NuGet/GitHub pass inventory,
> consumer, signing/timestamp, symbol, SBOM, checksum, manifest, test, and readiness gates. Success
> requires `classification=ready`, `publish_authorized=true`, and durable evidence paths.

Amend D-6 to record that REL-2 delivered G1, live v3.2.2 evidence proved G1 insufficient, and REL-3 owns
the pre-publication correction and historical ledger. Add a risk that workflow success can be confused
with readiness, mitigated by failing on blocked evidence, binding exact identity, and publishing durable
machine evidence. Replace the impossible v1.0 deadline with the next-release stop-the-line control.

### Proposal B — Correct RG-1 ownership and coverage

Artifact: `_bmad-output/planning-artifacts/epics.md`

- Keep RG-1 and REL-AI-1; create no new product epic.
- Keep REL-2 done against the accepted G1 criteria.
- Add REL-3 as the focused release-governance implementation story.
- Make Hexalith.Builds G2 integration, or an explicitly approved FrontComposer-owned equivalent,
  mandatory before the next publish-capable release.
- Assign REL-3 the exact-artifact chain, shared-workflow signing contract, fail-closed classification,
  same-byte publication, durable evidence, downloaded-artifact verification, partial-publication
  handling, and historical ledger.
- Allow Epic 11 development to continue while prohibiting automatic package release.
- Close REL-AI-1 only after a real release satisfies every corrected FR24 criterion.

### Proposal C — Add REL-3

New artifact:
`_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`

> As the release owner, I want the exact package artifacts intended for publication to pass the complete
> FR24 evidence gate before any publication side effect, so consumers receive only authorized,
> verifiable packages.

Acceptance criteria:

1. Publish-capable releases remain frozen until this gate is operational.
2. The release validates the expected inventory: eight packable packages and the explicitly
   non-packable projects.
3. Packages are packed once using the semantic-release version.
4. Tests and package-consumer validation run against the release candidate artifacts.
5. The exact `.nupkg` files intended for publication are signed, timestamped, and successfully verified.
6. Symbols and SBOMs are present and bound to the corresponding package artifacts.
7. Checksums cover packages, symbols, SBOMs, inventory, test results, consumer-validation results, and
   supporting evidence.
8. The release manifest is sealed and independently verified.
9. `classify-release --require-publishable` produces `classification=ready` and
   `publish_authorized=true`.
10. Any blocked, invalid, unsigned, unverified, incomplete, or unauthorized result fails before NuGet
    or GitHub publication begins.
11. NuGet and the initial GitHub Release receive the same signed package bytes covered by the verified
    manifest.
12. Durable evidence assets are published with the GitHub Release; a 30-day Actions artifact is not
    sufficient.
13. Post-publication verification compares downloaded NuGet and GitHub assets with the manifest
    checksums.
14. Partial publication is detected and recorded as an incident; the workflow does not report the
    release as successful.
15. A historical compliance ledger records at least v3.2.1 and v3.2.2, including their available
    signing and blocked-readiness evidence.
16. Governance tests reject the former G1 record-and-proceed behavior.
17. REL-AI-1 closes only on evidence from a real release satisfying every FR24 criterion.

Implementation boundary:

- Hexalith.Builds forwards the required signing credentials through its reusable release workflow.
- FrontComposer owns packing, validation, evidence generation, classification, publication of the
  authorized bytes, and post-publication verification.
- If the shared workflow cannot change in time, an explicitly approved repository-owned gated release
  workflow is the bounded contingency.

### Proposal D — Add FR24 delivery architecture

Artifact: `_bmad-output/planning-artifacts/architecture.md`

Add an **FR24 Release Evidence Architecture** section containing the required artifact invariant from
Section 3 and these rules:

- Only pre-publication readiness can authorize publication.
- A repack or post-publication signature cannot establish the identity of an already published package.
- The sealed manifest identifies immutable release candidates by normalized path and hash.
- Publication consumes those paths without rebuilding or repacking.
- Blocked classification, invalid manifest, missing evidence, or `publish_authorized=false` terminates
  the release before external side effects.
- Post-publication verification downloads both NuGet and GitHub assets and compares them with the
  manifest.
- Partial publication fails the release and creates an incident record.
- Durable GitHub Release assets are the public evidence chain; workflow artifacts are supplemental.

Record ownership as: Hexalith.Builds owns the reusable workflow contract and minimum permissions;
FrontComposer owns artifact creation through verification; the Release Owner owns signing identity,
timestamp authority, secret provisioning, the release freeze, exceptions, and incident response. No UX
change is required.

### Proposal E — Correct sprint status

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

- Keep `rel-2-align-frontcomposer-cicd-with-tenants: done`.
- Add `rel-3-enforce-fr24-pre-publish-and-reconcile-releases: ready-for-dev`.
- Keep REL-AI-1 `open`; change `implementation_story` from `REL-2` to `REL-3`.
- Replace `before v1.0 release candidate` with `before the next NuGet or GitHub package release`.
- Record REL-3 as a stop-the-line release item and add the v3.2.2 trigger evidence.
- Replace the evidence list with the corrected exact-artifact and real-release closure evidence.
- Record that product work may continue but automated publication is frozen.

### Proposal F — Implement the exact-artifact release pipeline

Affected executable artifacts:

- `references/Hexalith.Builds/.github/workflows/domain-release.yml` — owner-approved upstream change;
- `.github/workflows/release.yml`;
- `.releaserc.json`;
- `.github/workflows/release-evidence.yml`;
- `eng/release_evidence.py`, release orchestration scripts, and governance tests.

Required shared-workflow contract:

- declare and forward `NUGET_SIGNING_CERTIFICATE_BASE64` and
  `NUGET_SIGNING_CERTIFICATE_PASSWORD` only to semantic-release;
- accept a configurable RFC 3161 timestamp-service input;
- preserve root-declared, non-recursive submodule initialization and minimum permissions.

Required semantic-release preparation:

1. Pack once at `${nextRelease.version}`.
2. Validate inventory, tests, and package consumers.
3. Produce symbols and SBOM evidence.
4. Sign, timestamp, and verify the exact `.nupkg` files intended for publication.
5. Generate complete checksums, seal the manifest, and verify it.
6. Run `classify-release --require-publishable`.
7. Exit non-zero unless readiness is `ready` and publication is authorized.

Required publication behavior:

- Re-verify authorization immediately before publication.
- Push only signed `.nupkg` files and their matching `.snupkg` files.
- Never repack or replace an authorized artifact.
- Attach packages, symbols, sealed manifest, readiness record, SBOM, checksums, inventory, test results,
  and consumer results to the initial GitHub Release.
- Record partial publication and fail the release.

Required supplemental behavior:

- Do not repack or sign reconstructed artifacts.
- Download durable release evidence and both NuGet/GitHub packages.
- Compare published hashes with the authorized manifest and verify package signatures.
- Update the historical ledger; fail on missing, altered, unsigned, incomplete, or partial publication.

Governance tests must prove blocked readiness terminates semantic-release before publication, missing
signing credentials fail closed, `--require-publishable` is mandatory, publish commands consume only
authorized paths, initial GitHub Release assets include evidence, G1 `require_or_record` is absent, and
post-publication verification cannot authorize a release retroactively.

### Proposal G — Correct documentation and establish the ledger

Affected artifacts:

- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`;
- `_bmad-output/project-docs/deployment-guide.md`;
- new `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md`;
- `docs/index.md` where required for discoverability.

Change the G2 request from optional to required. It must identify the Hexalith.Builds owner, secret and
timestamp forwarding contract, owner-approved issue/story URL, accepted upstream revision, and the
bounded FrontComposer contingency. Filing the request does not clear the release freeze.

Correct the deployment guide to state that G1 was post-publication evidence, not a publication gate;
repacked packages do not prove NuGet bytes; Release Evidence triggers after Release; and short-retention
Actions artifacts are supplemental. Document the corrected operational lifecycle, secret provisioning
and rotation, timestamp configuration, non-publishing validation, partial-publication response, and
historical reconciliation.

Create a controlled historical ledger with release/run URLs, expected and observed inventory, NuGet and
GitHub hashes, signing/timestamp state, manifest verification, readiness classification,
`publish_authorized`, consumer results, durable evidence paths, disposition, owner, remediation, and
verification date. Seed v3.2.1 and v3.2.2. The v3.2.2 entry records the directly observed unsigned
package, invalid manifest, blocked readiness, `publish_authorized=false`, and green-workflow mismatch.
Neither historical release can close REL-AI-1.

## Section 5 — Implementation Handoff

### Change-scope classification

**Moderate course correction; high implementation risk.** This adds and reorders release-governance
work without changing product scope. It crosses a shared workflow boundary and controls irreversible
publication, so Release Owner and architecture oversight are mandatory.

### Owners

- **Release Owner:** owns REL-AI-1, freeze/unfreeze decision, signing identity, timestamp authority,
  secret provisioning, upstream dependency, contingency approval, real-release authorization, incident
  response, and final evidence sign-off.
- **Hexalith.Builds owner:** reviews and implements the reusable-workflow signing contract. No
  FrontComposer task may directly edit or commit the shared submodule without that owner workflow.
- **Developer:** implements REL-3's repository release orchestration, semantic-release configuration,
  verification workflow, helper changes, and deployment-guide reconciliation.
- **QA/Test Architect:** implements fail-closed governance coverage, package-consumer verification,
  negative cases, exact-byte assertions, and partial-publication tests.
- **Architect:** reviews the artifact-identity invariant, cross-repository contract, permissions, and
  contingency design.
- **Product Owner:** approves the FR24/NFR-12/SM-2/D-6 rebaseline without reducing the release standard.

### Recommended sequence

1. Approve this compiled proposal and apply Proposals A–E and the planning/documentation parts of G.
2. Enforce the release freeze and create/accept REL-3.
3. File the owner-approved Hexalith.Builds request; record its durable URL and accepted contract.
4. Implement the reusable secret forwarding or explicitly approve the bounded owned-workflow
   contingency.
5. Implement the exact-artifact lifecycle and reverse the G1 governance tests.
6. Exercise signing, timestamping, inventory, consumer, manifest, readiness, and publication commands
   in non-publishing validation contexts, including every fail-closed case.
7. Release Owner authorizes the next real release only after the pre-publication evidence reports
   `ready` and `publish_authorized=true`.
8. Independently verify downloaded NuGet/GitHub assets, publish durable evidence, update the ledger, and
   close REL-AI-1 only if all criteria pass.

### Validation matrix

| Gate | Passing evidence | Failure behavior |
| --- | --- | --- |
| Inventory | Eight expected packable packages; explicit non-packable set; no drift | Stop before signing/publish |
| Tests and consumers | Valid TRX summary plus clean package-only consumers | Stop before signing/publish |
| Signing | Every `.nupkg` author signature and RFC 3161 timestamp verified | Stop before manifest/publish |
| Symbols/SBOM | Required `.snupkg` and SBOM bound by checksums | Stop before manifest/publish |
| Manifest | Sealed manifest verifies against exact candidate paths and hashes | Stop before classification/publish |
| Readiness | `classification=ready`, `publish_authorized=true`, `--require-publishable` succeeds | Hard failure; no side effects |
| Publication identity | NuGet and GitHub bytes match authorized hashes | Failed release plus incident record |
| Durability | Initial GitHub Release exposes the evidence chain | Release incomplete/non-compliant |
| Historical reconciliation | v3.2.1 and v3.2.2 dispositions recorded with evidence links | REL-AI-1 remains open |

### Completion criteria

- REL-3 is implemented and reviewed; G1 record-and-proceed behavior is absent.
- A real release is authorized before publication and publishes only manifest-bound signed bytes.
- Downloaded NuGet and GitHub assets match the authorized manifest and pass signature verification.
- Durable release evidence contains inventory, symbols, SBOM, checksums, sealed/verified manifest,
  tests, consumer validation, readiness, and publication verification.
- The historical ledger records affected G1 releases.
- REL-AI-1 remains open unless every criterion above is satisfied.

## Section 6 — Approval State

**Approved by Administrator on 2026-07-15.** Proposals A through G were approved individually in
**Incremental** mode, followed by explicit approval of the compiled Sprint Change Proposal.

Applied on approval:

- `_bmad-output/planning-artifacts/prd.md` — FR24, NFR-12, SM-2, the release risk, and D-6 now require
  exact-artifact pre-publication authorization and next-release control.
- `_bmad-output/planning-artifacts/epics.md` — RG-1 retained; REL-2 stays done; REL-3 owns the correction.
- `_bmad-output/planning-artifacts/architecture.md` — FR24 delivery architecture and ownership invariant
  added; runtime/UX architecture unchanged.
- `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md` —
  created `ready-for-dev` with the approved acceptance contract.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` — REL-3 added, REL-AI-1 re-pointed and kept
  open, next-release freeze/evidence recorded; REL-2 remains done.
- `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md` — G2 converted
  from optional follow-up to a required owner-approved signing-contract dependency with a bounded
  contingency.
- `_bmad-output/project-docs/deployment-guide.md` — G1 described accurately as post-publication
  diagnostics; target lifecycle, freeze, signing ownership, and incident/verification process added.
- `_bmad-output/implementation-artifacts/rel-ai-1-release-evidence-ledger.md` — created and seeded with
  directly verified v3.2.1/v3.2.2 evidence.

`docs/index.md` was intentionally unchanged: generated planning/implementation artifacts remain outside
the published DocFX tree; the project deployment guide links the controlled ledger for discoverability.

Executable release code remains routed to REL-3 and the named owners. This Correct Course application
did not modify workflows, semantic-release configuration, package scripts, product source, or the
Hexalith.Builds submodule.
