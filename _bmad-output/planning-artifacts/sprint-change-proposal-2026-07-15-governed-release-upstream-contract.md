# Sprint Change Proposal — BUILD-REL-1 Governed Release Contract, REL-3 Amendment, and REL-5 Release Owner Enablement

---
date: 2026-07-15
mode: batch
status: approved-and-applied
approval: approved-by-administrator-2026-07-15
scope: moderate
trigger: REL-3 pre-development upstream dependency audit
relatedProposals:
  - sprint-change-proposal-2026-07-15-rel-ai-1-prepublish-enforcement.md
  - sprint-change-proposal-2026-07-15-release-freeze-enforcement.md
---

## Section 1: Issue Summary

REL-3 (`rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`, ready-for-dev) depends on an
upstream Hexalith.Builds change that, as currently specified, cannot deliver what REL-3's own
acceptance chain requires, and that upstream change has not been filed.

Three verified facts establish the problem:

1. **No upstream story exists.** A live 2026-07-15 search of
   [Hexalith/Hexalith.Builds](https://github.com/Hexalith/Hexalith.Builds) found only two closed
   2025 issues (#1, #2) and dependabot/docs PRs — no issue or PR matching the governed release
   contract. The G2 request document is marked required/blocking with `Issue/story URL: pending`.

2. **The current G2 ask is under-scoped.** It requests only signing-secret forwarding
   (`NUGET_SIGNING_CERTIFICATE_BASE64`, `NUGET_SIGNING_CERTIFICATE_PASSWORD`) plus an RFC 3161
   timestamp input. But `eng/release_evidence.py` manifest validation requires every package row to
   carry `attestation_status` ∈ {`attested`, `approved-unsupported`} with an `attestation_bundle`
   (or a sealed owner-approved fallback record). Minting a production attestation requires
   `actions/attest-build-provenance` running **inside the workflow that owns the candidate
   packages**, with `id-token: write` and `attestations: write` — workflow-level permissions that
   secret forwarding cannot convey. The shared `domain-release.yml` today grants only
   `contents/issues/pull-requests: write`, accepts only `NUGET_API_KEY`, and has no pre-publication
   candidate phase. Secret forwarding alone therefore cannot interleave a GitHub attestation between
   package preparation and readiness classification.

3. **REL-3 carries unresolved decisions** that should be settled before development starts:
   the approval mechanism (protected environment vs. workflow-dispatch vs. the REL-4 variable),
   production attestation-bundle creation before classification, and verification behavior when the
   Release workflow fails after partial publication. (A fourth item from the trigger analysis — the
   technical freeze implementation — is **already resolved**: REL-3 AC1 delegates to REL-4's
   fail-closed `HEXALITH_RELEASE_PUBLISH_ENABLED` gate, approved 2026-07-15.)

Additionally, the operational work only the Release Owner can perform (signing identity selection,
certificate provisioning/rotation, timestamp-authority approval, first-release authorization,
ledger sign-off) is currently embedded in REL-3 tasks rather than separated into an owner-executed
story, which would let a developer-complete REL-3 sit blocked on operational authority.

**Submodule conclusion:** Hexalith.Builds requires a new upstream feature (BUILD-REL-1) not yet
represented by any upstream story. Tenants, EventStore, Parties, Memories, and the other submodules
require no new feature for this release gate. FrontComposer's REL-3 exists but must be amended; the
Release Owner enablement story is missing (the technical-freeze story already exists as REL-4).

## Section 2: Impact Analysis

### Epic impact

- **Release governance track (REL-*)** — the only affected track:
  - REL-2 stays done; REL-4 (freeze) stays ready-for-dev and unchanged.
  - REL-3 stays ready-for-dev but is amended (new ACs 18–19, resolved-decisions section, task and
    boundary updates). No completed work is invalidated.
  - New **REL-5** story is added for Release Owner enablement. The proposed key "REL-4" from the
    trigger analysis collides with the existing freeze story; REL-5 is the next free key.
- **Product epics (1–11):** no impact. Product development continues under the freeze.
- **Sequencing:** REL-4 (freeze) → REL-3 development (can start after this amendment; its
  upstream-dependent tasks block on BUILD-REL-1 or the bounded contingency) → REL-5 execution
  (T1 filing is immediate; the rest trails REL-3 completion). REL-AI-1 closes only via REL-5's
  final sign-off.

### Story impact

- `rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md` — amended (Proposal E2).
- `rel-5-provision-signing-identity-and-first-governed-release.md` — created (Proposal E3).
- `rel-4-enforce-temporary-release-freeze.md` — no change.

### Artifact conflicts

- `g2-hexalith-builds-inline-pre-publish-gate-request.md` — required-change section is under-scoped;
  expanded to the full BUILD-REL-1 opt-in governed contract (Proposal E1).
- `epics.md` — FR-24 traceability row and RG-1 update trail need the upstream-contract correction
  and REL-5 (Proposal E4).
- `prd.md` — FR-24 consequences and decision D-6 need the same correction (Proposal E5).
- `sprint-status.yaml` — REL-3 amendment note, REL-5 entry, REL-AI-1 progress/implementation_story
  update (Proposal E6).
- UX artifacts — N/A (no UI surface).
- `deployment-guide.md` — no change now; REL-3 T7 keeps it aligned with implemented behavior.
- Governance tests / workflows — no change in this proposal; REL-3/REL-4 own them. The existing
  `ReleaseWorkflow_RunsViaWorkflowRunAfterCiSuccess` pins (no `workflow_dispatch`/approval tokens in
  `release.yml`) are preserved by the approval-mechanism decision below.

### Technical impact

- No code, workflow, or submodule edits in this proposal. `references/Hexalith.Builds` is not
  modified from FrontComposer; filing and implementing BUILD-REL-1 are Release Owner +
  Hexalith.Builds-owner actions.

## Section 3: Recommended Approach

**Direct Adjustment (Option 1).** Amend the G2 request into the complete BUILD-REL-1 contract,
resolve REL-3's three open decisions in place, and add REL-5 for the operational work. Rationale:

- The pre-publication enforcement direction was approved twice on 2026-07-15; nothing is rolled back
  and the MVP is untouched (Option 2 and Option 3 rejected — nothing to revert, scope unchanged).
- Under-scoping is cheapest to fix now, before the upstream issue is filed and before REL-3
  development bakes in an attestation gap that would force `approved-unsupported` fallbacks on every
  release.
- Separating REL-5 keeps REL-3 completable by a developer: certificate custody, approvals, and
  real-release evidence are authority the developer does not hold.

Effort: documentation/planning only (~1 session). Risk: low — the main residual risk is upstream
timeline, which is exactly what the existing bounded contingency covers. Timeline impact: none on
product epics; the release track already sits behind the REL-4 freeze.

### Resolved decisions carried into the edits

- **D-A (approval mechanism):** Release authorization stays variable-based on the caller side —
  the REL-4 `HEXALITH_RELEASE_PUBLISH_ENABLED` gate is the Release Owner's authorization switch.
  FrontComposer's `release.yml` gains no `workflow_dispatch`, dry-run input, or approval
  environment (existing governance pins stay). A **protected release-environment input is part of
  the upstream governed contract** and applies inside `domain-release.yml` when it lands; REL-3
  must not block on it.
- **D-B (attestation before classification):** the pre-publication chain must produce a provenance
  attestation over the exact signed candidates and bind the bundle path into the manifest before
  sealing/classification. Absent the upstream governed contract, the **only** alternative is the
  Release Owner-approved sealed `approved-unsupported` fallback record already modeled by
  `release_evidence.py`; classification fails otherwise.
- **D-C (failed-run verification):** independent post-publication verification triggers on Release
  workflow **completion regardless of conclusion**, reconciles observed NuGet/GitHub state against
  the sealed manifest (or records its absence), and feeds the partial-publication incident path.
- **D-D (technical freeze):** already resolved by REL-4; no REL-3 change needed beyond the existing
  AC1 reference.

## Section 4: Detailed Change Proposals

### E1 — Expand the G2 request into the BUILD-REL-1 governed release contract

File: `_bmad-output/planning-artifacts/g2-hexalith-builds-inline-pre-publish-gate-request.md`

**E1.a — Title and header.**

OLD (line 1):

```markdown
# Required upstream dependency (G2): signing contract for the FR24 pre-publication gate
```

NEW:

```markdown
# Required upstream dependency (G2 / BUILD-REL-1): opt-in governed NuGet release contract for the FR24 pre-publication gate
```

In the header list, after the `- **Status:** …` line, add:

```markdown
- **Suggested upstream story title:** "BUILD-REL-1: Add an opt-in governed NuGet release contract
  to Hexalith.Builds"
- **Upstream verification (2026-07-15):** a live search of Hexalith.Builds issues and pull requests
  found no matching issue or PR (only closed 2025 issues #1/#2 and dependabot/docs PRs); filing
  remains pending.
```

**E1.b — Replace the under-scoped required-change section.**

OLD (section "## Required change (backward-compatible for existing callers)", full body):

```markdown
Declare the following optional secrets on `workflow_call` and expose them only to the Semantic Release
step:

- `NUGET_SIGNING_CERTIFICATE_BASE64`
- `NUGET_SIGNING_CERTIFICATE_PASSWORD`

Add a configurable RFC 3161 timestamp-service input (default may remain the approved public timestamp
authority). Preserve current behavior when the optional signing values are unset so existing callers
remain compatible; FrontComposer itself will fail closed when they are missing.

A generic `pre-publish-command` is not sufficient by itself. FrontComposer's `.releaserc.json` already
owns the semantic-release `prepareCmd`/`publishCmd` lifecycle and can consume `${nextRelease.version}`;
the missing shared contract is secure signing-secret and timestamp forwarding.
```

NEW:

```markdown
Signing-secret forwarding alone is **under-scoped** and is superseded by this revision. FrontComposer's
manifest validation (`eng/release_evidence.py`) requires every package to carry
`attestation_status=attested` with an attestation bundle (or a sealed Release Owner-approved
`approved-unsupported` fallback). Minting that attestation requires `actions/attest-build-provenance`
running inside the workflow that owns the candidate packages, with `id-token: write` and
`attestations: write` — workflow-level permissions and a lifecycle position that no forwarded secret
can provide. Secret forwarding cannot interleave a GitHub attestation between package preparation and
readiness classification.

The required upstream feature is an **opt-in governed mode** on `domain-release.yml` — or a sibling
governed release workflow — providing:

- an opt-in activation input (default off; existing callers unchanged);
- a protected release-environment/approval input applied to the governed release job;
- optional `workflow_call` secrets `NUGET_SIGNING_CERTIFICATE_BASE64` and
  `NUGET_SIGNING_CERTIFICATE_PASSWORD`;
- a configurable RFC 3161 timestamp-service input (default may remain the approved public timestamp
  authority);
- signing secrets scoped only to the governed release steps, never printed or persisted;
- `id-token: write` and `attestations: write` granted only where required by the governed mode,
  preserving minimum permissions elsewhere;
- a pre-publication candidate phase that knows the semantic-release version and produces the exact
  candidate packages before any publication side effect;
- `actions/attest-build-provenance` executed over those exact candidate packages;
- the resulting attestation-bundle path passed to the caller's manifest-finalization hook (e.g., an
  environment variable or output available to the semantic-release `prepareCmd` lifecycle);
- semantic-release publishing the already-authorized artifacts without rebuilding or repacking;
- durable failure and partial-publication evidence surfaced by the governed mode;
- backward compatibility for every existing Hexalith module caller (all new inputs unset → current
  behavior);
- root-only, non-recursive submodule initialization preserved.

FrontComposer's `.releaserc.json` continues to own the semantic-release `prepareCmd`/`publishCmd`
evidence lifecycle and consumes `${nextRelease.version}`; the shared workflow provides the governed
execution context (secrets, permissions, environment, candidate phase, attestation) — not
FrontComposer's evidence logic.
```

**E1.c — Extend the caller shape.** In the "Caller shape once G2 lands" YAML, add the governed
inputs to `with:` (illustrative names, upstream owner finalizes):

```yaml
    with:
      solution: Hexalith.FrontComposer.slnx
      test-projects: ''
      governed-release: true
      release-environment: production-release
      nuget-signing-timestamper: https://<approved-rfc3161-authority>
```

**Rationale:** the upstream issue must be filed once, with the complete contract; filing the
secret-forwarding subset would leave REL-3 AC8/AC9 unsatisfiable without a standing fallback.

### E2 — Amend REL-3 with the resolved decisions

File: `_bmad-output/implementation-artifacts/rel-3-enforce-fr24-pre-publish-and-reconcile-releases.md`

**E2.a — New section after "## Why This Story Exists":**

```markdown
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
```

**E2.b — Required Artifact Invariant.**

OLD:

```text
  → verify signatures and timestamps
  → checksum packages, symbols, and evidence
```

NEW:

```text
  → verify signatures and timestamps
  → attest build provenance over the exact signed packages (bundle bound into the manifest,
    or a sealed owner-approved unsupported-attestation fallback)
  → checksum packages, symbols, and evidence
```

**E2.c — Append acceptance criteria (existing AC numbering preserved — REL-4 and other artifacts
reference AC1 by number):**

```markdown
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
```

**E2.d — Task updates.**

- T1, first bullet — OLD: "Record the Hexalith.Builds owner-approved issue/story URL and accepted
  revision in the G2 request." NEW: "Record the Hexalith.Builds owner-approved issue/story URL and
  accepted revision in the G2 request; the filed scope must be the full BUILD-REL-1 governed
  contract (environment, signing secrets, timestamp input, attestation permissions, candidate
  phase, bundle handoff), not signing-secret forwarding alone."
- T2, add bullet: "Bind the provenance attestation-bundle path over the exact signed candidates
  into the manifest before sealing; fail classification when attestation evidence is neither
  `attested` nor a sealed owner-approved fallback."
- T4, first bullet — OLD: "Trigger after a real Release and resolve its tag/version without
  repacking." NEW: "Trigger on Release workflow completion regardless of conclusion; resolve its
  tag/version without repacking, no-op only when no publication side effect occurred, and run full
  reconciliation for failed or partial runs."
- T5, add bullet: "Prove classification fails when attestation evidence is absent and no sealed
  owner-approved fallback exists."

**E2.e — Implementation Boundary, Hexalith.Builds bullet.**

OLD:

```markdown
- **Hexalith.Builds:** its owner must declare and forward
  `NUGET_SIGNING_CERTIFICATE_BASE64`, `NUGET_SIGNING_CERTIFICATE_PASSWORD`, and configurable RFC 3161
  timestamp information to semantic-release. Do not directly modify or commit the shared submodule
  from FrontComposer.
```

NEW:

```markdown
- **Hexalith.Builds:** its owner must land the BUILD-REL-1 opt-in governed release contract —
  protected release environment, `NUGET_SIGNING_CERTIFICATE_BASE64` /
  `NUGET_SIGNING_CERTIFICATE_PASSWORD`, configurable RFC 3161 timestamp input, `id-token: write` +
  `attestations: write` scoped to the governed steps, a version-aware pre-publication candidate
  phase, `actions/attest-build-provenance` over the exact candidates with the bundle path handed to
  manifest finalization, and no-repack publication — per the G2 request document. Do not directly
  modify or commit the shared submodule from FrontComposer.
```

**E2.f — Frontmatter:** `updated: 2026-07-15` gains the note
`amended: 2026-07-15 (governed-release upstream contract; ACs 18-19; REL-5 split)`, and the
operational tasks note: T6's "Obtain Release Owner authorization…" and T7's ledger/evidence-URL
bullets record that **execution authority for provisioning/authorization/sign-off moved to REL-5**;
REL-3 retains the technical implementation and verification tooling.

### E3 — Create REL-5: Provision the production signing identity and prove the first governed release

New file: `_bmad-output/implementation-artifacts/rel-5-provision-signing-identity-and-first-governed-release.md`

Story (summary; full file created on approval):

- **As** the Release Owner, **I want** the production signing identity, approvals, and governed
  release environment provisioned and the first gated release proven end to end, **so that**
  REL-AI-1 can close on durable real-release evidence no developer can produce alone.
- **Key ACs:** (1) production package-signing identity/trust model selected and recorded;
  (2) certificate secrets provisioned with rotation custody (never logged, Release Owner-only);
  (3) RFC 3161 timestamp authority approved and configured; (4) BUILD-REL-1 filed upstream with the
  full governed contract, URL + accepted revision recorded in the G2 request; (5) protected-
  environment reviewers configured when the upstream environment input lands (variable gate remains
  the caller-side authorization); (6) first gated release explicitly authorized only after REL-3
  pre-publication evidence is ready/authorized; (7) downloaded NuGet and GitHub assets verified
  against the sealed manifest; (8) compliance ledger updated with the first compliant record;
  (9) REL-AI-1 closed only when every FR-24 artifact is durable and verification passes.
- **Ordering:** T-filing (AC4) executes immediately; ACs 1–3 proceed in parallel with REL-3
  development; ACs 5–9 trail REL-3 completion. Blocked-on: REL-3 operational gate; BUILD-REL-1
  acceptance or the bounded owner-approved contingency.
- **Owner:** Release Owner (executes); Developer assists with verification tooling/evidence capture.
- **Boundary:** no FrontComposer code changes owned here beyond evidence records; no submodule
  edits; certificate material never enters the repository, logs, or artifacts.

### E4 — epics.md FR-24 corrections

File: `_bmad-output/planning-artifacts/epics.md`

**E4.a — FR-24 traceability row.**

OLD:

```markdown
| FR-24 | Release Governance Gate RG-1; REL-AI-1 remains open and REL-3 owns correction; REL-2 is completed evidence, not closure |
```

NEW:

```markdown
| FR-24 | Release Governance Gate RG-1; REL-AI-1 remains open; REL-3 owns correction, REL-4 the technical freeze, REL-5 Release Owner enablement; REL-2 is completed evidence, not closure |
```

**E4.b — New update paragraph** after the 2026-07-15 freeze-enforcement update:

```markdown
**Update (correct-course 2026-07-15, upstream governed contract):** the Hexalith.Builds dependency
is corrected from signing-secret forwarding to the full **BUILD-REL-1 opt-in governed NuGet release
contract** (protected release environment, signing secrets, RFC 3161 timestamp input,
`id-token`/`attestations` permissions, a version-aware pre-publication candidate phase,
`actions/attest-build-provenance` over the exact candidates with the bundle bound into manifest
finalization, no-repack publication, backward compatibility, root-only submodule init). A live
upstream search found no matching issue or PR; filing is a Release Owner action. `REL-3` is amended
in place (attestation-before-classification AC, failed-run verification AC, approval-mechanism
resolution — the REL-4 variable remains the caller-side authorization; no approval tokens enter
`release.yml`). New **`REL-5: Provision the production signing identity and prove the first
governed release`** separates operational authority (identity/trust model, certificate custody,
timestamp-authority approval, upstream filing, first-release authorization, download verification,
ledger sign-off, REL-AI-1 closure) from REL-3 development work. See
`_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md`.
```

### E5 — prd.md FR-24 corrections

File: `_bmad-output/planning-artifacts/prd.md`

**E5.a — FR-24 consequences, append bullet after the `REL-4` freeze bullet:**

```markdown
- The shared-workflow dependency is the full opt-in governed release contract on Hexalith.Builds
  (`BUILD-REL-1`): protected release environment, signing secrets, RFC 3161 timestamp input,
  attestation permissions with a version-aware candidate phase, attestation-bundle handoff to
  manifest finalization, and no-repack publication. `REL-5` separates Release Owner enablement —
  production signing identity, certificate custody, timestamp-authority approval, upstream filing,
  first gated-release authorization, downloaded-asset verification, and `REL-AI-1` closure — from
  `REL-3` development work.
```

**E5.b — Decision D-6, append to the Resolution cell:**

```markdown
Follow-up 2026-07-15 (2): the upstream dependency is corrected to the full BUILD-REL-1 opt-in
governed release contract (secret forwarding alone cannot mint the pre-classification attestation);
REL-3 is amended with attestation/failed-run/approval-mechanism resolutions and REL-5 owns Release
Owner enablement and REL-AI-1 closure.
```

### E6 — sprint-status.yaml updates

File: `_bmad-output/implementation-artifacts/sprint-status.yaml`

- After the `rel-3-…: ready-for-dev` line's existing comment, add:

```yaml
  # rel-3 amended 2026-07-15 (approved correct-course sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md). Upstream scope corrected: signing-secret forwarding is under-scoped — release_evidence.py manifest validity requires attestation_status=attested with a bundle, and attestation needs id-token/attestations permissions plus a candidate phase only the shared workflow can host. G2 request expanded to the full BUILD-REL-1 opt-in governed contract (live upstream search 2026-07-15: no matching issue/PR). REL-3 gains AC18 (attestation before classification; sealed approved-unsupported fallback is the only alternative), AC19 (verification on Release completion regardless of conclusion + partial-publish reconciliation), the approval-mechanism resolution (REL-4 variable stays the caller-side authorization; no approval tokens in release.yml; protected environment lives upstream), and task/boundary updates. Operational authority (identity, certificates, approvals, first-release evidence, REL-AI-1 closure) moved to REL-5.
```

- After the `rel-4-…: ready-for-dev` entry, add:

```yaml
  # rel-5 added ready-for-dev on 2026-07-15 (approved correct-course sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md). Release Owner enablement split out of REL-3: select/record the production package-signing identity and trust model, provision/rotate certificate secrets, approve the RFC 3161 timestamp authority, file BUILD-REL-1 upstream (full governed contract; filing is immediate), configure protected-environment reviewers when the upstream input lands, authorize the first gated release only on ready/authorized pre-publication evidence, verify downloaded NuGet/GitHub assets, record the first compliant ledger entry, and close REL-AI-1 only on durable passing evidence. Owner-executed; developer assists with verification tooling. Trails REL-3 for ACs 5-9.
  rel-5-provision-signing-identity-and-first-governed-release: ready-for-dev
```

- REL-AI-1 action item: `implementation_story: "REL-3"` → `implementation_story: "REL-3 (development) + REL-5 (Release Owner enablement and closure)"`; prepend to `progress`:

```text
2026-07-15 (governed upstream contract): approved correct-course sprint-change-proposal-2026-07-15-governed-release-upstream-contract.md corrects the Hexalith.Builds dependency from signing-secret forwarding to the full BUILD-REL-1 opt-in governed release contract (environment, signing secrets, timestamp input, id-token/attestations permissions, version-aware candidate phase, attest-build-provenance over exact candidates with bundle-to-manifest handoff, no-repack publish; live search confirmed no upstream issue/PR exists). REL-3 amended (AC18 attestation-before-classification, AC19 failed-run verification, approval-mechanism resolution). REL-5 added for Release Owner enablement; REL-AI-1 closure now routes through REL-5.
```

## Section 5: Implementation Handoff

**Scope classification: Moderate** — backlog reorganization (story amendment + new story) plus
planning-artifact updates; no code changes.

| Recipient | Responsibility |
| --- | --- |
| Developer agent (on approval, this session) | Apply E1–E6: amend the G2 request, amend REL-3, create the REL-5 story file, update epics.md, prd.md, sprint-status.yaml. |
| Release Owner | Execute REL-5: file BUILD-REL-1 upstream immediately (URL + accepted revision into the G2 request), select the signing identity, provision secrets, approve the timestamp authority; later authorize and verify the first governed release; close REL-AI-1. |
| Hexalith.Builds owner | Accept/implement BUILD-REL-1 upstream (outside this repo). |
| Developer (REL-3 dev-story, later) | Implement REL-3 against the amended ACs; upstream-dependent tasks block on BUILD-REL-1 acceptance or the bounded contingency. |

**Success criteria:** all six edit sets applied without disturbing unrelated in-progress worktree
changes; REL-3/REL-5 consistent with the G2 request; the next `create-story`/`dev-story` pass on
REL-3 finds no unresolved pre-development decisions; BUILD-REL-1 filing has a complete,
self-contained contract to reference.

## Checklist Record

| Item | Status | Note |
| --- | --- | --- |
| 1.1–1.3 Trigger, problem, evidence | [x] | REL-3 dependency audit; live upstream search, domain-release.yml shape, release_evidence.py attestation requirement all re-verified this session. |
| 2.1–2.5 Epic impact | [x] | Release track only; REL-4 key collision corrected to REL-5; sequencing REL-4 → REL-3 → REL-5. |
| 3.1 PRD | [x] | FR-24 consequences + D-6 amendments (E5). |
| 3.2 Architecture | [N/A] | FR24 release-evidence architecture section unchanged; behavior corrections live in REL-3/G2 artifacts. |
| 3.3 UX | [N/A] | No UI surface. |
| 3.4 Other artifacts | [x] | G2 request, sprint-status, REL story files; deployment guide deferred to REL-3 T7. |
| 4.1–4.4 Path forward | [x] | Direct Adjustment selected; rollback/MVP-review rejected. |
| 5.1–5.5 Proposal components | [x] | This document. |
| 6.1–6.2 Review | [x] | Verified against live upstream state and current artifact text. |
| 6.3 Approval | [x] | Approved by Administrator on 2026-07-15 (Batch-mode review, "apply all edits"). |
| 6.4 Sprint status update | [x] | Applied: rel-3 amendment comment, rel-5 entry (ready-for-dev), REL-AI-1 implementation_story + progress updated; unrelated in-progress edits preserved. |
| 6.5 Handoff confirmation | [x] | E1-E6 applied this session; Release Owner executes REL-5 T1 (BUILD-REL-1 filing, identity, timestamp authority) immediately; REL-3 dev-story may start against the amended ACs. |
