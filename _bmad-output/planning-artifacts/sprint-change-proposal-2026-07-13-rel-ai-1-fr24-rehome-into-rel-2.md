---
title: Sprint Change Proposal - Re-home FR24 Release Evidence Gate into REL-2
project: frontcomposer
date: 2026-07-13
workflow: bmad-correct-course
mode: Batch
trigger: "REL-AI-1: Own FR24 release evidence gate for signed packages, symbols, SBOM, checksums, package inventory, release manifest/evidence chain, GitHub Release assets, and package-consumer validation before v1.0 RC."
intent: Re-home FR24 evidence into REL-2 (Tenants reusable domain-release) — the 2026-07-09 alignment superseded REL-1's standalone release.yml; this proposal folds the FR24 obligations into the still-backlog REL-2.
supersedes_relationship: builds-on
prior_proposals:
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-05-rel-ai-1-release-evidence-gate.md
  - _bmad-output/planning-artifacts/sprint-change-proposal-2026-07-09-tenants-cicd-alignment.md
status: approved
approval: approved-by-administrator-2026-07-13
gatingDecision: "G1 now (post-publish evidence + next-release fail-closed), G2 flagged as durable Hexalith.Builds upstream follow-up. Approved 2026-07-13."
scope: Moderate
owner: Release Owner
---

# Sprint Change Proposal — Re-home FR24 Release Evidence Gate into REL-2

## Section 1 — Issue Summary

`REL-AI-1` (owns PRD **FR24** / Release Governance Gate **RG-1**) is still `open` in
`_bmad-output/implementation-artifacts/sprint-status.yaml`. Two prior approved course corrections
already cover this trigger, but neither has closed the gate, and the second one moved the goalposts:

- **2026-07-05** (`…rel-ai-1-release-evidence-gate.md`): created **REL-1** and shipped only an
  *advisory, non-gating* FR24 evidence layer wired into the standalone `.github/workflows/release.yml`
  (auto-publish-from-`main`). AC2 signing, AC4–5 gating, AC6 package-consumer validation, and AC8
  evidence-recording were deferred; `REL-AI-1` stayed open.
- **2026-07-09** (`…tenants-cicd-alignment.md`): **superseded** the July-5 auto-publish model. Approved
  moving FrontComposer's primary CI/CD onto the shared **Hexalith.Builds reusable workflows**
  (`domain-ci.yml` / `domain-release.yml` via `workflow_run` after CI success) and created **REL-2**.
  REL-2's AC6 explicitly left the FR24 home undecided: *"evidence generation is either implemented in
  the reusable release path or `REL-AI-1` remains open with an explicit owner/date/reopen criterion."*

**The core problem this proposal resolves:** REL-1's only shipped artifact (the advisory evidence layer
inside `release.yml`) is about to be **deleted** when REL-2 replaces `release.yml` with the pristine
reusable `domain-release.yml`. So the FR24 obligations must be **re-homed into REL-2** with a concrete
architecture, or they will silently fall through the alignment. REL-2 is still `backlog` — this is the
moment to fold FR24 in before REL-2 is implemented.

**Category:** new understanding forced by a superseding architectural decision, plus a material
package-boundary change (below). This is release-governance course correction, not a product-feature
change.

### Decisive technical finding (why the naive "move it into the reusable workflow" fails)

The reusable **`references/Hexalith.Builds/.github/workflows/domain-release.yml@main`** provides **none**
of the FR24 evidence. Its entire release job is: checkout → init .NET/Node → cache → `npm ci` +
`npm audit signatures` → `dotnet restore` → `dotnet build -c Release -warnaserror` → (optional tests,
skipped on `workflow_run`) → (optional container publish) → **`npx semantic-release`**. There is **no**
signing, SBOM, checksum, manifest, `classify-release`, readiness gate, or evidence-asset step, and **no
hook** to inject one before `semantic-release` publishes.

Per `references/Hexalith.Builds/CLAUDE.md`, that reusable workflow is a **shared submodule referenced at
`@main`** and consumed by every Hexalith module; our root rules forbid modifying `references/Hexalith.*`
submodule files without explicit owner approval. **Therefore FR24 cannot simply "live inside" the shared
release workflow** without a separate, owner-approved Hexalith.Builds change. This constraint shapes the
recommended architecture.

### What is genuinely new since 2026-07-09 (fresh evidence)

Commit **`b6e985f4 refactor!: govern the FrontComposer 2.0 package split`** has **landed**. The package
inventory is now final at **8 packable packages including `Hexalith.FrontComposer.Contracts.UI`**
(`packable: true`, `symbol_required: true`) per `eng/release-package-inventory.json`; `AppHost` and the
combined `UI` host remain non-packable. Contracts.UI ships at a **2.0.0** public-API baseline. Both the
07-05 proposal and REL-1's own notes flagged that **any** package-boundary change *must* re-trigger
inventory + package-consumer validation. That trigger has now fired and is unaddressed.

### Evidence gathered

`sprint-status.yaml` (REL-AI-1/REL-1/REL-2 blocks), REL-1 story, REL-2 story, both prior proposals,
live `.github/workflows/release.yml` (still `on: push:[main]`, bespoke, advisory-evidence),
`.github/workflows/ci.yml` (still bespoke, not `domain-ci.yml`), `.releaserc.json` (pack + unsigned
push), `eng/release_evidence.py` (12 subcommands; **no** consumer-validation command),
`eng/release-package-inventory.json` (8-package/Contracts.UI set), `CiGovernanceTests.cs` (model-guard
tests below), and the Tenants baselines (`references/Hexalith.Tenants/.github/workflows/release.yml`,
`references/Hexalith.Builds/.github/workflows/{domain-ci,domain-release}.yml`, `references/Hexalith.Tenants/scripts/`).

## Section 2 — Impact Analysis

### Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the open release action `REL-AI-1` under a superseding architecture decision, not a failed implementation story. |
| 1.2 Core problem | [x] | REL-1's advisory FR24 layer is about to be deleted by REL-2's reusable-workflow adoption; FR24 obligations must be re-homed into REL-2. |
| 1.3 Evidence | [x] | See "Evidence gathered" and "Decisive technical finding" above. Reusable `domain-release.yml` provides no FR24 evidence and cannot be edited here. |
| 2.1 Current epic impact | [x] | No product epic invalidated. Release Governance / RG-1 is the correct owner. |
| 2.2 Epic-level changes | [!] | `epics.md` RG-1 note should record the trigger change (push → `workflow_run`) and that FR24 evidence re-homes to a supplemental workflow, not `release.yml`. |
| 2.3 Future epic impact | [!] | Epic 11 (2.0 package split) is now landed; its final 8-package/Contracts.UI-2.0 inventory is the input to FR24 inventory + consumer validation. |
| 2.4 New/remove epics | [x] | No new epic. Fold FR24 into existing **REL-2**; retire **REL-1**'s obligations into REL-2. |
| 2.5 Priority/order | [!] | REL-2 must land before v1.0 RC. FR24 evidence + consumer validation must run against the **final** package set, i.e. after the 2.0 split (now true). |
| 3.1 PRD conflicts | [!] | FR24 wording (tightened 07-05) is correct; add that the release trigger is `workflow_run`-after-CI and FR24 evidence is produced by a supplemental workflow reusing `eng/release_evidence.py`. |
| 3.2 Architecture conflicts | [x] | No runtime architecture change. CI/CD architecture shifts to shared Hexalith.Builds ownership; FR24 evidence is a FrontComposer-owned supplement. |
| 3.3 UX conflicts | [N/A] | No UI/UX impact. |
| 3.4 Other artifacts | [!] | `release.yml`, `ci.yml`, new `scripts/`, new `release-evidence.yml`, `CiGovernanceTests.cs`, `release-package-inventory.json` baseline, deployment-guide, REL-1/REL-2 stories, sprint-status. |
| 4.1 Direct adjustment | Viable | Recommended: amend REL-2 to carry FR24 obligations + split-homing architecture. |
| 4.2 Rollback | Not viable | No product work to roll back. REL-1's advisory layer is intentionally superseded, not "rolled back". |
| 4.3 MVP review | Not viable | v1 scope unchanged; this is a publication gate. |
| 4.4 Recommended path | [x] | **Hybrid Direct Adjustment**: fold FR24 into REL-2 with a 3-layer split-homing architecture + one flagged upstream dependency. |
| 5.1–5.5 Proposal components | [x] | Captured in Sections 3–5. |
| 6.1–6.2 Final review | [x] | Proposal is specific and actionable. |
| 6.3 Approval | [ ] | **Pending Administrator approval.** |
| 6.4 Sprint status update | [ ] | Apply on approval (Proposal F). |
| 6.5 Handoff | [x] | Moderate: Release Owner + Developer + QA/Test Architect + PO. |

### Epic Impact

No feature epic reopens. FR24 stays owned by RG-1 / `REL-AI-1`. The implementation vehicle changes from
**REL-1** (superseded standalone release.yml) to **REL-2** (Tenants-aligned reusable workflows), which
must now carry the FR24 obligations explicitly.

### Story Impact

- **REL-1** (`review`): its shipped advisory evidence layer in `release.yml` is removed by REL-2's
  reusable-workflow adoption. REL-1 should be **closed as superseded**, with its open FR24 ACs
  (AC2/AC4–5/AC6/AC8) **transferred to REL-2** so nothing is lost.
- **REL-2** (`backlog`): expand its acceptance criteria to embed the FR24 evidence gate under the
  reusable-workflow model (Proposal A). REL-2 becomes the single owner of both alignment and FR24.
- **Governance tests**: three model-guard tests actively pin the *old* model and must flip (Proposal D).

### Artifact Conflicts — release governance tests that block REL-2

`tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs` currently pins the superseded
model and will fail REL-2 until updated:

- `ReleaseWorkflow_PublishesFromMainWithoutManualDispatchGates` — asserts `on: push:[main]` and forbids
  dispatch gates; **directly conflicts** with the Tenants `workflow_run` trigger.
- `ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests` — pins the bespoke auto-publish job.
- `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating` — requires the advisory FR24 layer to
  live *inside* `release.yml` and stay advisory; that layer is being removed and re-homed.
- `SemanticReleasePack_EnablesEvaluatedPackageValidationAgainst112Baseline` — pinned to the **1.12.0**
  package-validation baseline; the 2.0 split (Contracts.UI @ 2.0.0) makes this stale.

The remaining `ReleaseEvidenceScript_*` suite (classify-release, seal/verify-manifest, test-results
fail-closed, approval matrix, fingerprint, etc.) already exercises `eng/release_evidence.py` thoroughly —
that tooling is **proven**; it is simply **not wired into the live release path as a gate**. Re-homing
reuses it rather than rebuilding it.

## Section 3 — Recommended Approach

**Hybrid Direct Adjustment**: fold FR24 into REL-2 using a **3-layer split-homing** architecture that
keeps the shared reusable release path pristine, plus **one flagged upstream dependency** on
Hexalith.Builds for durable pre-publish gating.

### Re-homing architecture

| Layer | Home | FR24 coverage | Submodule change? |
| --- | --- | --- | --- |
| **CI** | `domain-ci.yml` with `run-consumer-validation: true` + new FrontComposer `scripts/` | AC1 inventory, AC6 package-consumer validation (against the final 8-package/Contracts.UI-2.0 set) | No (uses existing reusable input) |
| **Release (publish)** | pristine reusable `domain-release.yml` via `workflow_run` | none — publish only | No |
| **FR24 evidence** | **new supplemental FrontComposer `release-evidence.yml`** (`workflow_run` on CI success), reusing `eng/release_evidence.py` | AC2 signing/verify, AC3 SBOM+checksums+manifest, AC4–5 `classify-release`/readiness, AC8 evidence recording + GitHub Release assets | No |

Rationale: this is exactly the "supplemental FrontComposer quality workflow" escape hatch the 07-09
proposal (AC8) sanctioned. It preserves Tenants alignment (primary CI/CD is the shared reusable pair),
reuses the already-tested 170 KB evidence tool, and never touches the shared submodule.

### The one hard decision for the Release Owner — pre-publish gating (AC4–5)

Because the pristine `domain-release.yml` runs `semantic-release` (which publishes) with **no
evidence/classify hook**, **true pre-publish gating cannot be inline** without changing Hexalith.Builds.
Three options:

- **G1 — Post-publish evidence + next-release fail-closed (recommended to unblock now).** Publish proceeds
  under the reusable workflow; the supplemental `release-evidence.yml` produces the full evidence bundle,
  runs `classify-release`, attaches assets, and **fails closed on the next release** if the prior evidence
  is missing/invalid. No submodule change. Weaker gate (a bad release can publish once), but auditable and
  immediately shippable.
- **G2 — Upstream an opt-in evidence gate into Hexalith.Builds `domain-release.yml` (durable target).**
  Add opt-in inputs (e.g. `pre-publish-command` / signing+SBOM+classify inputs) so `classify-release` can
  block **before** `dotnet nuget push`. Strongest gate; benefits every Hexalith module. Requires a
  **separate Hexalith.Builds story + owner approval** (Proposal E) — **not** editable in this repo.
- **G3 — FrontComposer keeps a thin owned gated release job.** Rejected: diverges from Tenants alignment
  and defeats REL-2's purpose.

**Recommendation:** implement **G1 now** to unblock REL-2 and record FR24 evidence; raise **G2** as the
durable Hexalith.Builds dependency. The Release Owner decides whether **v1.0 RC may ship on G1 evidence**
or must wait for **G2 inline gating**.

Effort: **Medium.** Risk: **Medium** (touches publish automation + governance tests; G1 keeps the reusable
path pristine and fail-closes on the following release). Timeline: must complete before v1.0 RC; run FR24
inventory + consumer validation against the **now-final** 2.0 package set.

## Section 4 — Detailed Change Proposals

### Proposal A — Amend REL-2 to own the FR24 evidence gate

Artifact: `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md`

Replace REL-2's single soft AC6 (*"evidence generation is either implemented … or REL-AI-1 remains open
with an explicit owner/date/reopen criterion"*) with concrete FR24 obligations under the split-homing
architecture:

```markdown
6. Given the final packable set (8 packages incl. Contracts.UI @ 2.0.0), when shared CI runs with
   `run-consumer-validation: true`, then FrontComposer `scripts/` pack/validate/consumer wrappers
   validate the inventory and prove the documented Contracts-only vs Shell/UI consumer boundaries
   (FR24 AC1 + AC6) before release.
7. Given a release runs (workflow_run after CI success), when the supplemental
   `.github/workflows/release-evidence.yml` executes, then it reuses `eng/release_evidence.py` to
   produce SBOM, checksums, test-results, package inventory, and a sealed+verified release manifest
   bound to commit SHA/tag/run-id/workflow-ref/package-set-fingerprint/version, runs
   `classify-release`, and attaches the evidence bundle as GitHub Release assets (FR24 AC3 + AC8).
8. Given signing is provisioned, when packages are produced, then `.nupkg` are signed and verified
   (`dotnet nuget verify --all`, RFC 3161 timestamp) and unsigned/timestamp-missing packages are
   recorded as a blocking readiness reason (FR24 AC2).
9. Given the reusable `domain-release.yml` publishes without an inline hook, then release readiness is
   enforced by the approved gating model (G1 post-publish + next-release fail-closed, unless the Release
   Owner selects G2 upstream inline gating) and the choice is recorded in this story (FR24 AC4–5).
10. Given governance tests run, then they assert the Tenants `workflow_run` + reusable `domain-release.yml`
    model AND the re-homed FR24 supplement, and no longer assert the superseded auto-publish/advisory model
    (FR24 AC7).
11. Given Release Owner review, then `REL-AI-1` moves to `done` only when evidence paths are recorded for
    every FR24 artifact under the chosen gating model; otherwise it stays `open` with the blocking gap.
```

Rationale: makes REL-2 the single implementation owner of both alignment and FR24, with the homing
architecture explicit so a future agent cannot restore the superseded advisory-in-`release.yml` layer.

### Proposal B — Add FrontComposer package-validation scripts + consumer-validation tooling (AC1/AC6)

Artifacts to create/update:

- `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`,
  `scripts/validate-consumer-package-references.py` — FrontComposer-compatible wrappers mirroring
  `references/Hexalith.Tenants/scripts/` so `domain-ci.yml`'s `run-consumer-validation: true` path works.
- `eng/release_evidence.py` **has no consumer-validation subcommand** (verified: inventory, checksums,
  test-results, verify/seal/prepare-manifest, release-budget, path-check, partial-publish-incident,
  classify-release, classify-fixtures, fallback-digest). Consumer validation is **new tooling**.
- Consumer validation must exercise the **final** 2.0 boundaries: a Contracts-only consumer must **not**
  inherit Blazor/Fluent runtime deps (the kernel-split invariant), and a Shell/UI consumer must reference
  Contracts + Contracts.UI + Shell and compile the documented bootstrap surface. Record command, local
  package source, produced versions, and result under `release-evidence/`.

Rationale: FR24 is about consumer safety, and the just-landed 2.0 split is precisely the boundary this
must catch before RC.

### Proposal C — Add supplemental `release-evidence.yml` workflow (AC2/AC3/AC4–5/AC8)

New artifact: `.github/workflows/release-evidence.yml`, triggered `workflow_run` on `CI` success (same
guard as Tenants release: `conclusion == 'success' && event == 'push'`), running after / alongside the
reusable release. Steps reuse `eng/release_evidence.py`: `inventory` → `checksums` → `test-results` →
`prepare-manifest` → `seal-manifest` → `verify-manifest` → `classify-release` → SBOM (CycloneDX over the
`.slnx`) → sign+verify (`dotnet nuget sign` / `dotnet nuget verify --all`, when the signing cert secret is
provisioned) → upload evidence bundle + attach to the GitHub Release. Must keep `submodules: false` /
root-only init and must not recurse submodules.

Rationale: keeps the shared reusable release pristine while giving FrontComposer the full FR24 evidence
chain from proven tooling.

### Proposal D — Flip the model-guard governance tests + refresh the package-validation baseline (AC7)

Artifact: `tests/Hexalith.FrontComposer.Shell.Tests/Governance/CiGovernanceTests.cs`

- `ReleaseWorkflow_PublishesFromMainWithoutManualDispatchGates` → assert release runs via `workflow_run`
  after `CI` success (not `on: push`), delegating to `Hexalith.Builds/.github/workflows/domain-release.yml@main`.
- `ReleaseWorkflow_RunsAutomaticPackageReleaseAfterBlockingTests` → assert the reusable-workflow release
  shape (CI gate upstream; no duplicated in-release test job by default).
- `ReleaseWorkflow_ProducesAdvisoryFr24EvidenceBundleWithoutGating` → re-point to
  `release-evidence.yml`: require the FR24 evidence chain there and assert the chosen gating posture
  (G1 next-release fail-closed, or G2 inline).
- `SemanticReleasePack_EnablesEvaluatedPackageValidationAgainst112Baseline` → update from the 1.12.0
  baseline to the **2.0.0 / Contracts.UI** reality and the final 8-package inventory.

Rationale: these tests are the strongest signal of the live model; they must guard the new model, not the
old one, and must reflect the landed 2.0 split.

### Proposal E — Raise the Hexalith.Builds upstream gate as a separate, owner-gated request (G2)

Artifact: a **new Hexalith.Builds issue/story** (NOT edited in this repo — submodule rule).

Request: add opt-in inputs to `domain-release.yml` (e.g. `pre-publish-command`, or signing/SBOM/classify
inputs) so a caller can enforce `classify-release --require-publishable` **before** `dotnet nuget push`.
This makes inline pre-publish gating available to every Hexalith module.

Rationale: the durable, ecosystem-wide FR24 gate; keeps FrontComposer from forking a bespoke gated release
job. Blocked on Hexalith.Builds owner approval; tracked as a dependency of `REL-AI-1`'s strongest closure.

### Proposal F — Update sprint-status, PRD, and epics traceability

- `sprint-status.yaml`: keep `REL-AI-1` **open**; set `implementation_story: "REL-2"` (retire REL-1 from
  the active FR24 path); add a `2026-07-13` progress note (FR24 re-homed into REL-2; 3-layer split; G1
  recommended, G2 flagged; 2.0 inventory re-validation required). Mark
  `rel-1-release-evidence-gate-before-v1-rc` **superseded/closed** with obligations moved to REL-2. Keep
  `rel-2-align-frontcomposer-cicd-with-tenants` in `backlog` (or promote if accepted into the sprint) with
  the expanded FR24 ACs.
- `prd.md` FR-24: keep the tightened wording; add that the release trigger is `workflow_run`-after-CI and
  FR24 evidence is produced by a supplemental FrontComposer workflow reusing `eng/release_evidence.py`.
- `epics.md` RG-1: add a note that REL-2 (not REL-1) implements FR24 under the reusable-workflow model, and
  that inventory + consumer validation run against the final post-2.0-split package set.

### Proposal G — Re-validate inventory + consumer references against the landed 2.0 split

Artifacts: `eng/release-package-inventory.json` (already reflects the 8-package/Contracts.UI set — confirm
`symbol_required` per packable), plus the new consumer-validation run (Proposal B). Re-run FR24 inventory
+ consumer validation now that `refactor!: govern the FrontComposer 2.0 package split` has landed; record
Contracts.UI at its `2.0.0` baseline and the Contracts-only-no-UI-deps invariant as evidence.

Rationale: this is the genuinely new material fact since 07-09 and the exact package-boundary trigger both
prior artifacts said must re-run FR24 validation.

## Section 5 — Implementation Handoff

Scope classification: **Moderate** (backlog reorganization + governance-test reversal + new tooling; no
product-feature or MVP change).

Route to:

- **Release Owner** — owns `REL-AI-1`; **decides G1 vs G2** and whether v1.0 RC ships on G1 evidence;
  records final evidence paths; approves the Hexalith.Builds upstream request (Proposal E).
- **Developer** — implements REL-2: `scripts/` wrappers, `release-evidence.yml`, ci.yml/release.yml
  reusable-workflow adoption, and the deployment-guide reconciliation.
- **QA/Test Architect** — flips the model-guard governance tests, refreshes the package-validation
  baseline to 2.0, and builds the consumer-validation coverage (Proposals B & D).
- **Product Owner** — confirms the PRD FR-24 wording delta (Proposal F).

Recommended sequence:

1. Approve this proposal; choose the gating posture (G1 now / wait for G2).
2. Amend REL-2 (Proposal A); retire REL-1's FR24 obligations into REL-2 (Proposal F).
3. Add `scripts/` wrappers + consumer-validation tooling and re-validate the 2.0 inventory (Proposals B, G).
4. Add `release-evidence.yml`; adopt reusable `domain-ci.yml`/`domain-release.yml` (Proposals C + REL-2 alignment).
5. Flip governance tests + refresh the 2.0 baseline (Proposal D).
6. Raise the Hexalith.Builds upstream request for inline gating (Proposal E).
7. Reconcile the deployment-guide to the re-homed model; run the governance lane locally (CI authoritative).
8. Release Owner runs a real release, records evidence paths, and closes `REL-AI-1` only if all FR24
   evidence exists under the chosen gating model.

Success criteria:

- REL-2 owns and implements FR24 under the Tenants reusable-workflow model; REL-1 is closed/superseded
  with no lost obligations.
- FR24 evidence lives in a supplemental FrontComposer workflow; the shared `domain-release.yml` is never
  modified in this repo.
- Inventory + consumer validation pass against the final 8-package / Contracts.UI-2.0 set.
- Governance tests guard the new model and fail if it regresses to auto-publish/advisory.
- `REL-AI-1` stays open until the Release Owner records evidence paths under G1 (or G2).

## Section 6 — Approval State

**Approved by Administrator on 2026-07-13.** Gating posture selected: **G1 now** (post-publish evidence +
next-release fail-closed, no submodule change) with **G2 flagged** as the durable Hexalith.Builds upstream
follow-up (inline pre-publish gating; owner-approved, separate story). The Release Owner will decide at RC
time whether v1.0 may ship on G1 evidence.

Applied on approval (Proposal F):

- `_bmad-output/implementation-artifacts/sprint-status.yaml` — `REL-AI-1` kept open, `implementation_story`
  set to `REL-2`, 2026-07-13 progress note added; `rel-1-…` retired as superseded; `rel-2-…` annotated
  with the FR24 fold-in.
- `_bmad-output/planning-artifacts/prd.md` FR-24 — release trigger is `workflow_run`-after-CI; FR24
  evidence produced by a supplemental FrontComposer workflow reusing `eng/release_evidence.py`.
- `_bmad-output/planning-artifacts/epics.md` RG-1 — REL-2 (not REL-1) implements FR24 under the
  reusable-workflow model; inventory + consumer validation run against the final post-2.0-split set.
- `_bmad-output/implementation-artifacts/rel-1-release-evidence-gate-before-v1-rc.md` — closed as
  superseded; open FR24 ACs transferred to REL-2.
- `_bmad-output/implementation-artifacts/rel-2-align-frontcomposer-cicd-with-tenants.md` — ACs expanded
  with the FR24 obligations, the 3-layer split-homing architecture, and the G1/G2 gating decision.

All executable changes route to **REL-2** (Developer + QA/Test Architect). The Hexalith.Builds inline-gate
request (Proposal E / G2) routes to the **Release Owner** as an owner-approved upstream dependency. No
workflows, scripts, governance tests, or product source were modified by this proposal itself.
