---
title: 'Story 16.3: Approve or Reject the Exact Runtime Migration'
type: 'chore'
created: '2026-11-15'
status: 'in-progress'
route: 'dispatch'
story_id: '16.3'
story_key: '16-3-a-evt-app-1-approve-or-reject-the-exact-runtime-migration'
baseline_commit: '695d23149c56a6104e81d23a54116b4fb914df95'
review_loop_iteration: 0
g_3: 'open'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-16-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Obtain separate EventStore-maintainer, FrontComposer-maintainer and Release Owner approve/reject decisions for the accepted Story 16.1 tuple after authority, coordination and candidate lineage are resolved. Engineering acceptance, including D1, grants no migration approval.

## Boundaries & Constraints

Change only `_bmad-output/`. Preserve all tuple records and historical outcomes. Cite existing story/CI evidence; add no schema, validator, evidence workflow or bundle. Never move gitlinks, edit submodules, infer role equivalence, renew deadlines silently or transfer evidence/receipts to descendants. Record only actual human decisions, with signer, role, authority, UTC date, exact tuple, evidence and rationale.

## I/O & Edge-Case Matrix

| Condition | Required behavior |
|---|---|
| Original EventStore owner available | 16.2 not invoked; obtain that owner's decision |
| EventStore authority unavailable/proposed to move | Invoke 16.2 only then; Product, Architect and Release decisions precede substitution |
| Drift, expired window, missing authority or receipt | G-3 open; bounded owner disposition |
| Three valid approvals and all prerequisites met | Recognize only the exact approved tuple |
| Rejection | G-3 open; bounded corrective residual only if work is needed |

## Human decisions — 2026-11-15

The user's numbered answer confirms jpiquot separately in all five roles, selects the recommended amendment-request disposition and supplies approve for all three exact-C migration decisions. No ownership transfers. The actual statements are preserved below; their prerequisite validity remains blocked. This answers the original questions without approving an unwritten amendment.

## Approved planning and coordination answer — 2026-11-15

The user answered “1 approve, 2 jpiquot and confirm” to the concrete amendment and prospective coordination requests. This explicitly approves the linked amendment as Product Owner, Architect and Release Owner, names jpiquot as workspace automation owner, and confirms actual session/automation coordination. Record confirmation as of 2026-11-15T21:58:49Z; no earlier effective timestamp or individual roster was supplied. Use that receipt time for prospective effectiveness and the existing outer deadline 2026-11-16T17:27:10Z as the maximum end, subject to earlier decision, drift or urgent-fix termination. Treat this as the accountable owner's attestation; do not invent a historical global hold or independently observed acknowledgments.

Apply the amendment's exact-current-main gates, no automatic activation/transfer of earlier C decisions, terminal deadline and scope as written. No fresh attempt or release publication is authorized by this planning adoption. Keep Story16.3 in-progress and G-3 open until current-candidate operative approvals actually satisfy the requirements.

</frozen-after-approval>

## Exact proposed tuple

- FrontComposer: `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`.
- EventStore source: `fdb59895975e86a1e234325a01237f259cdbd53b`.
- Builds: `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`.
- Catalog EventStore default: `5.1.0`; no root preset.

The [accepted record](spec-16-1-capture-the-current-eventstore-runtime-tuple.md#tuple-record--2026-11-15-1) is preserved at baseline_commit above. [Primary Quality 43755163930, attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1) tests exactly this tuple. Eight jobs, all four Gate 2c steps and all 27 Epic 9 steps succeed; provider 80/80, interactions 19/19, default tests 6,685/6,685. Primary browser passed on configured retry; no first-attempt claim.

Source lanes prove no NuGet runtime compatibility. D4 covers 35 source-consuming contexts/27 projects; standalone Parties controls select 4.2.0 versus inherited 5.1.0. Nonconsumer framework negotiation, launch globals and restored assets remain limits. CounterFixture standalone Release is invalid/excluded. BH2/BH3 and permanent concurrency regression coverage remain deferred. Full limitations and artifact expiry links stay in the accepted record and [decision preparation](epic-16-context.md#prepared-owner-decisions--2026-11-15).

## Code Map

- `epic-16-context.md` — authority sources, conditional 16.2 assessment and concrete disposition/decision requests.
- `spec-16-1-capture-the-current-eventstore-runtime-tuple.md` — immutable evidence; leave untouched.
- `deferred-work.md` and `sprint-status.yaml` — append preparation pointers; keep G-3 open and approval stories pending.

## Tasks & Acceptance

**Execution:**

- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md` — mark approved and append the actual three-capacity planning receipt and owner coordination attestation; preserve the proposed wording as history.
- [x] `_bmad-output/planning-artifacts/prd.md` — append the approved G-3/D-12 resolution with a receipt link, superseding prior pending-identity/amendment text only as of this decision.
- [x] `_bmad-output/planning-artifacts/architecture.md` — append the approved EventStore Active-Identity resolution; preserve current-main guards and exact tuple distinctions.
- [x] `_bmad-output/planning-artifacts/epics.md` — append the approved Epic16 disposition,16.2 not-invoked state and16.3 pending operative-receipt requirement.
- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md` — append the adopted amendment link, actual confirmation time and unchanged outer deadline.
- [x] `_bmad-output/implementation-artifacts/epic-16-context.md` — append the latest approved state after updating planning sources, so cached context includes all new facts and preserves historical sections.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — append closure of identity/amendment/coordination questions and the exact remaining current-candidate blocker and accountable next action.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — keep16.1 done,16.2 backlog with not-invoked annotation,16.3 in-progress, Epic16/G-3 open; update last_updated and preserve every unrelated YAML value/comment.
- [x] `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` — append actual planning/coordination decision and checks, reconcile these completed documentation tasks, and keep in-progress with the concrete operative-approval blocker.

**Acceptance Criteria:**

- Given the user's two numbered confirmations, when recorded, then jpiquot is explicitly named in each planning capacity and the additional automation-owner capacity; coordination is owner-attested as of receipt, never retroactively claimed.
- Given C approvals and current main R, when readiness is assessed, then no C approval becomes operative or transfers to R merely because the amendment is approved or coordination now exists; G-3 remains open.
- Given the approved amendment, when authoritative planning sources are read, then each points to its actual receipt and preserves exact-current-main release guards, the original deadline and prior records.
- Given the diff, when verified, then all changes stay under `_bmad-output/`, all Story16.1 bytes and historical planning text remain intact, and unrelated sprint YAML data is unchanged.

## Implementation Notes

At preparation, authority and all three decisions were pending. The user response below supersedes that preparation state: authority confirmed; 16.2 **not invoked**; three exact-C approve statements received but ineligible for G-3 while prerequisites remain unmet. The planning amendment remains proposed.

## Spec Change Log

## Review Triage Log

| Finding | Verdict and route | Verified evidence |
|---|---|---|
| BH-1 | low; rejected as spec-only/transient | The in-review frontmatter is the required temporary build review state; the adopted final state and sprint stay in-progress. Restore the final status after review; no approval/G-3 completion is claimed. |
| BH-2 | false; rejected | The nine checked items explicitly describe completed documentation adoption; the frozen decision and the concrete external-blocker section keep Story16.3 in-progress until independently valid current-candidate capture/receipts. The diff does not claim story completion. |
| BH-3 | false; rejected | Epic16 context's evidence policy explicitly interprets older record-digest/artifact-hash wording as the owning story/spec plus cited CI run, and forbids an added identity schema/validator/bundle. Exact C identities, accepted record and primary attempt are cited; a new digest requirement would contradict that active policy. |
| BH-4 | medium; patch completed | Provision4 still requires identifying all active sessions, but receipt text closes coordination questions despite no roster. Distinguish the accepted owner attestation from the remaining undocumented roster/acknowledgment prerequisite; do not invent names or request a new rule change. |
| BH-5 | medium; patch completed | The immutable candidate/record distinction and unchanged latest-main selector still create the documented record-descendant cycle. State this structural blocker explicitly alongside missing R capture/receipts, and make clear another capture alone is insufficient; retain the named terminal disposition instead of a retry loop. |
| BH-6 | medium; patch completed | The outer deadline cannot replace R's retained two-hour capture deadline. Read-only primary attempt1 jobs now establish all32 required steps successful, last20:57:29Z and capture deadline22:57:29Z on2026-11-15. Record time-only eligibility and all remaining prerequisites without authorizing recapture. |
| BH-7 | medium; patch completed | Unqualified decision can be read to include planning approval at hold start. Clarify that qualification-hold termination means the final migration disposition or named terminal blocker disposition, alongside drift/urgent fix/deadline; no earlier effective time or renewed window. |
| BH-8 | low; rejected as spec-only | The custom assertion stdin is not retained, so that particular ad hoc command is less reproducible. Existing story-validator and whitespace commands are fully specified; the improvement would edit this build's verification account, which this review excludes. No missing runtime behavior/test is alleged and no new evidence workflow is justified. |
| BH-9 | low; rejected as spec-only | The record describes this actual all-approve/current-candidate-blocked session. Existing Epic16 rejection criteria remain unchanged and allow a valid rejection with a bounded residual only when work is needed. A general future rejection-status addition would edit the build's frozen/spec wording; no current rejection or false completion is present. |
| BH-10 | low; patch completed | The supplied live observation is real but its date/source need precision. Parent GitHub main and C/R run reads completed by2026-11-15T21:59:14Z; cite those sources and time as supplied observation, and distinguish any later review-time read. |

All three review layers ran after implementation. Edge Case Hunter returned no findings; Verification Gap returned no gaps. Five independent documentation corrections remain (BH-4/5/6/7/10); none changes approved requirements or authorizes a new attempt.

## File List

- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md`

- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md`

## Verification

Run existing story-artifact validation, whitespace/scope checks, and byte comparisons against baseline for all Story 16.1 content. Read GitHub main and primary attempt metadata again before any receipt. No runtime change or new test/evidence workflow.

### Preparation checks — 2026-11-15

`run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` passed; `git diff --check` passed. Read-only assertions passed: entire Story 16.1 byte-identical, earlier context/ledger/sprint text preserved, sprint YAML data unchanged, four-file scope/CRLF/local links valid. This validates preparation only.

## Human response and incident disposition — 2026-11-15

**Recorded at 2026-11-15T21:49:02Z.** The requesting user answered the three numbered requests: “1 jpiquot for all; 2 do recommended; 3 approve.”

1. **Current authority confirmed:** Jérôme Piquot (`jpiquot`/Administrator) is confirmed separately as EventStore maintainer, FrontComposer maintainer, Release Owner, Product Owner and Architect, including the requested EventStore availability/remit. This is explicit current confirmation alongside the historical authority sources, not inferred role equivalence.
2. **Story 16.2: not invoked.** The original EventStore authority is available; no duty or approval authority transfers.
3. **Release Owner with Architect disposition:** request the recommended explicit planning amendment for the tested-C/capture-R latest-main conflict and unmet repository-wide coordination. This records the named selected disposition before incident due `2026-11-15T21:49:35Z`; it does not adopt an amendment, establish global coordination or renew the original outer deadline `2026-11-16T17:27:10Z`.
4. **Migration answer received:** “approve” is directed to the three prepared EventStore/FrontComposer/Release decisions for the exact proposed tuple and cited evidence. Preserve the actual answer, but do not treat it as an operative exact-tuple approval receipt while lineage and coordination prerequisites are unresolved. The three role-specific approval statements will be recorded with that limitation; no descendant inherits them.

G-3 remains open pending the concrete amendment decision and satisfaction of its applicable prerequisites. Story 16.1 and every Tuple Record remain unchanged.

## Migration decisions received — 2026-11-15

**Signer:** Jérôme Piquot (jpiquot/Administrator). **Authority source:** explicit current five-role confirmation in the user's numbered response, supported by the separate historical grants cited in Epic 16 context. **Recorded at:** 2026-11-15T21:49:02Z. **Decision date:** 2026-11-15; no separate authoring timestamp was supplied.

All three statements below refer only to the exact tuple in this spec: FrontComposer `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`, catalog version `5.1.0`. They cite the accepted 16.1 record, primary Quality43755163930 attempt1 and the disclosed limits above; they do not approve any descendant.

| Separately confirmed capacity | Actual answer to the prepared decision | Prerequisite validity |
|---|---|---|
| EventStore maintainer — jpiquot | Approve | Received; not operative for G-3 |
| FrontComposer maintainer — jpiquot | Approve | Received; not operative for G-3 |
| Release Owner — jpiquot | Approve | Received; not operative for G-3 |

The rationale/evidence scope presented for these decisions is the accepted live-provider/AppHost capture with the stated source/package, D4, retry and deferred-behavior limits. No additional rationale, waiver, rejection or missing runtime evidence is invented from “approve.” This is the durable record of the actual human answer, not a claim that the missing coordination and lineage prerequisites were met.

**G-3: open. Operative migration approval: not claimed.** No automatic later activation, backdated prerequisite satisfaction or receipt transfer is permitted. The [requested planning amendment](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md) is concrete and proposed; the earlier amendment-request decision is not its adoption.

### Response verification — 2026-11-15

After recording the human response and preparing the amendment, the existing `run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` and `git diff --check` both pass. Read-only assertions pass for Story16.1 byte preservation, append-only baseline history, unchanged sprint YAML data, five-file `_bmad-output/` scope, CRLF/local links, three separately named received approve statements and proposed/draft/open gate states. No current-main release readiness is claimed.

## Authorized adoption handoff — 2026-11-15

The latest user response is “1 approve, 2 jpiquot and confirm.” Planning and coordination questions are answered; apply the concrete approved amendment now without another confirmation. Its full text is in the File List. All three named planning roles approve; the additional automation-owner identity and coordination are explicitly confirmed. Receipt time is 2026-11-15T21:58:49Z. Preserve the earlier received C migration decisions and their original prerequisite assessment.

Verified live just before implementation: main/local HEAD remain R `695d23149c56a6104e81d23a54116b4fb914df95`; C's primary Quality43755163930 attempt1 and R's independent Quality43759663406 attempt1 both succeed. No new R capture or R owner decision exists. The original outer deadline remains `2026-11-16T17:27:10Z`; the incident disposition was already timely recorded at `2026-11-15T21:49:02Z`.

Do not dispatch workflows, recapture R, renew a window, stage, commit, push, contact anyone, suspend automation, edit submodules or move gitlinks. No remote operation is needed. Use `run-with-temp` for disposable verification. Validate the completed records with the existing story-artifact validator and `git diff --check`; compare Story16.1's entire file SHA-256 to `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, preserve baseline planning/ledger/context bytes by appending only, and check sprint YAML semantic changes are restricted to16.3 status and last_updated. Record actual passed checks, not planned results. No runtime code changed, so do not add tests for this documentation work.

Unfulfilled external acceptance is not an Open Question or reason to ask again: R (or any different proposed release candidate) needs its own independent exact 16.1 capture and valid named decisions within an authorized window. The Release Owner/Architect owns that bounded next action or a terminal blocker disposition by the original deadline. This authorized build only adopts the plan and records actual answers; keep16.3 in-progress and G-3 open. All prior tuple records and source/property/runtime/browser/deferred-work limits remain unchanged.

## Planning adoption and operative-approval blocker — 2026-11-15

The [actual planning receipt](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15) records the supplied “1 approve, 2 jpiquot and confirm” answer at `2026-11-15T21:58:49Z`, separately naming jpiquot as approving Product Owner, Architect and Release Owner. The earlier five-role confirmation remains the authority source; the latest answer additionally and explicitly identifies jpiquot as workspace automation owner. The proposal is approved, its exact proposed wording remains history, and all four authoritative planning sources now link the adopted resolution and receipt. The cached context, ledger and sprint annotations reflect that state.

The actual coordination confirmation is recorded as an accountable-owner attestation, prospectively effective from receipt `2026-11-15T21:58:49Z` to no later than original outer deadline `2026-11-16T17:27:10Z`, ending earlier on decision, drift or urgent fix. No earlier effective time, individual roster or independently observed acknowledgments were supplied. No historical global hold or retrospective satisfaction of C's prerequisites is claimed. The earlier timely incident disposition at `2026-11-15T21:49:02Z` remains separate from this later amendment adoption.

The three exact-C migration approve statements and their original prerequisite assessment remain untouched. Amendment adoption and subsequent coordination never activate, backdate or transfer them. C `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` remains distinct from record/main R `695d23149c56a6104e81d23a54116b4fb914df95`; EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` and catalog `5.1.0` remain as captured. The supplied pre-implementation live observation reports C Quality43755163930 attempt1 and R Quality43759663406 attempt1 successful. No fresh remote check, R capture or R decision is asserted here.

**Concrete external blocker:** R or another proposed current release candidate lacks its own independent exact four-part 16.1 capture and valid named operative migration decisions within an authorized window. Current-candidate readiness still requires passing same-attempt live lanes and overall Quality, all three valid receipts, exact-current-main release comparison, successful push CI/Quality and the protected-credential current-main recheck. Record/receipt descendants have no documentation exemption. jpiquot as Release Owner, with Architect involvement, owns satisfied readiness or a terminal concrete blocker/next-action disposition by `2026-11-16T17:27:10Z`. If the attempt cannot finish by then, end it with G-3 open; any fresh attempt needs a separately explicit named schedule of at most 24 hours and its own evidence and decisions. This adoption authorizes no recapture, workflow dispatch, fresh attempt or publication and creates no automatic retry loop.

The nine authorized documentation tasks are complete. Story 16.1 remains done; 16.2 remains backlog with **not invoked** annotation; this spec and sprint Story 16.3 remain **in-progress**; **Epic 16 / G-3 remain open**. Unfulfilled external acceptance is not an unanswered planning question. Runtime code, release configuration, dependencies, gitlinks, submodules and all earlier evidence/limitations remain unchanged.


### Adoption verification — 2026-11-15

Verified at `2026-11-15T22:07:07Z` after the nine documentation updates:

- `run-with-temp python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` — passed: “Story artifact validation passed.”
- `git diff --check` — passed with no whitespace errors.
- `run-with-temp python3 -` read-only assertions — passed: exactly the nine File List paths changed and all are under `_bmad-output/`; original working-tree prefixes are preserved except the authorized proposal status, completed task markers and `last_updated`; all tracked baseline planning/context/ledger text remains an append-only prefix; CRLF/final newlines and all newly added local links/anchors are valid. The checker handles duplicate Markdown heading suffixes in the unchanged 16.1 record.
- Entire Story 16.1 SHA-256 — `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, matching the supplied expected bytes; its normalized content also equals baseline commit `695d23149c56a6104e81d23a54116b4fb914df95`.
- Parsed sprint YAML compared with that baseline — all semantic data is identical after excluding only Story 16.3 status and `last_updated`; 16.1 done, 16.2 backlog, 16.3 in-progress and Epic 16 in-progress are explicitly checked. Earlier unrelated comments remain byte-preserved; the new comments record 16.2 not invoked and G-3 open.
- Receipt/state assertions — passed for three separately named Product/Architect/Release approve decisions, the additional named automation-owner attestation and exact prospective/maximum-end timestamps, nonoperative/nontransferring C decisions, approved proposal, nine checked documentation tasks and in-progress/open story/gate states. The spec's frozen block and all earlier human-response/tuple evidence remain preserved.

No runtime code or tests were added or changed. No new runtime evidence, current-main observation or operative migration approval is claimed by these documentation checks.

### Adoption implementation change log — 2026-11-15

Applied the already approved concrete amendment and owner coordination answer to its receipt, the four authoritative planning sources and the three tracking/context records; reconciled this spec's nine documentation tasks and recorded passed checks. Preserved all historical text and kept the remaining current-candidate capture/receipt requirement external, bounded and owned. Story 16.3 intentionally remains in-progress with G-3 open.


### Review-time cadence observation — 2026-11-15

At `2026-11-15T22:13:43Z`, parent read-only GitHub branch metadata still reports main R `695d23149c56a6104e81d23a54116b4fb914df95`. [R Quality43759663406 attempt1 jobs](https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1/jobs?per_page=100) report all eight jobs and all32 required catalog/Gate2c/Epic9 steps successful. Last required completion is `2026-11-15T20:57:29Z`; the retained two-hour capture rule yields **`2026-11-15T22:57:29Z`**. At observation this time condition is unexpired. No R capture, owner decision, historic coordination or structural-lineage resolution is supplied by those metadata reads. Outer deadline `2026-11-16T17:27:10Z` cannot extend R's capture cadence and no recapture is authorized here.

The earlier supplied pre-implementation observation came from the parent's read-only GitHub main branch and C/R workflow-run queries completed by `2026-11-15T21:59:14Z`; it was not a fresh implementation-agent check. The review-time read above is separately dated and creates no new tuple record.

### Review clarification of coordination, lineage and cadence — 2026-11-15

The [review clarifications](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#review-clarifications--2026-11-15) preserve the approved rule and earlier records. Accept jpiquot's resolved automation-owner identity and coordination attestation from `2026-11-15T21:58:49Z`; provision 4's active-session roster/acknowledgments remain undocumented and required. Hold-ending “decision” means final migration disposition or named terminal blocker disposition, with drift, urgent fix or original deadline `2026-11-16T17:27:10Z` also ending it; the planning approval at hold start does not end it.

The structural record-descendant/current-main cycle remains blocked: committing another capture/receipt advances main, so another capture alone is insufficient. jpiquot as Release Owner with Architect must bound its resolution under unchanged exact-current-main/evidence/receipt and release guards, or record a terminal blocker and next action by that outer deadline. The supplied parent GitHub main and C/R run queries completed by `2026-11-15T21:59:14Z`; separate parent review metadata at `2026-11-15T22:13:43Z` reports R's last required success `2026-11-15T20:57:29Z` and capture deadline **`2026-11-15T22:57:29Z`**. This establishes time-only eligibility at that observation, without capture/receipt transfer, deadline extension, recapture, retry or new-attempt authorization. No new remote check is made. **16.3 remains in-progress; G-3 remains open**, with 16.1 done and 16.2 not invoked/backlog.

### Review completion — 2026-11-15

All three independent layers returned. Each of ten Blind Hunter findings has an individual verdict above; Edge Case Hunter and Verification Gap returned no findings. The five retained documentation corrections were applied as append-only clarifications across the adopted receipt and authoritative/tracking records, then read by the parent. No unrelated technical work was deferred by this review. Existing external acceptance remains open: undocumented active-session roster/acknowledgments, structural record-descendant/current-main lineage, and independent current-candidate capture/operative receipts within the applicable cadence.

The authorized documentation adoption is complete. Keep Story16.3 in-progress and G-3 open as explicitly required by the approved amendment; do not mark the approval story done from completion of documentation tasks. No commit, staging, push or new attempt is part of this bounded handoff.

## Authorized lineage-procedure preparation — 2026-11-15

The latest user authorizes preparation of one executable resolution from the approved amendment, with all changes confined to `_bmad-output/`. This continuation supersedes the earlier adoption-only handoff for this preparation. Do not execute Git mutations, create branches, stage, commit, push, move gitlinks, edit submodules, dispatch any workflow, contact other sessions or suspend automation. Do not capture a new tuple or fabricate owner decisions. Present the concrete result before requesting a new decision or Git authorization. Preserve this entire spec's frozen block, earlier sections and all Story 16.1 bytes. Keep 16.3 in-progress and G-3 open; 16.2 remains not invoked/backlog. Preserve existing baseline_commit.

### Investigation supplied to the preparation

- ListAgents before edits returned only `/root` running. Initial UTC was `2026-11-15T22:52:36Z`; a later check was `2026-11-15T22:55:29Z`. R's capture deadline is `2026-11-15T22:57:29Z`; the original outer deadline is `2026-11-16T17:27:10Z`. Recheck at completion, explicitly record expiry, and renew neither. C's timely historical capture remains valid history, but its capture deadline `2026-11-15T22:12:06Z` has passed; its three earlier decisions remain nonoperative.
- Read-only GitHub `branches/main` now reports M `b34be9245443d21b72a6076cf205bbbc8a8c521f`, committed `2026-11-15T22:50:16Z`, parent R `695d23149c56a6104e81d23a54116b4fb914df95`. Local main/HEAD matches M. The R-to-M diff contains exactly the nine adopted planning/tracking documents; it changes no runtime or selection, which establishes provenance, never exact tuple equivalence. Git ls-tree at M selects EventStore `fdb59895975e86a1e234325a01237f259cdbd53b` and Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`; committed root Directory.Packages.props has no version preset. Read the catalog with `git -C references/Hexalith.Builds show 73e60af8a1fcdf3f06726d5d1b6e1199cce778ee:Props/Directory.Packages.props` before asserting default 5.1.0 (SDK42 opt-in 4.2.0).
- GitHub actions/runs filtered to M reports push Quality `43771512600` attempt 1, CI `43771512961` attempt 1 and CodeQL `43771512909` attempt 1 in progress; Commitlint `43771512934` succeeds. R Quality `43759663406` attempt 1 independently remains completed/success. These are observations only. M has no passing capture, owner receipts or authorized successor schedule. Existing running workflows may be inspected; authorize no fresh run or retry. Never substitute C/R counts or D4 evaluations for new M evidence.
- App-server list_threads(limit=50) returns a truncated inventory; larger requested limits are rejected (allowed 1..50). Read_thread durable histories identify active root tasks: `01a1e169-527e-7800-bc63-aa56bca0b1eb` current FrontComposer preparation; `01a1e168-75f4-7622-9375-12711a59770e` Platform story 2.13, cwd `/home/administrator/projects/hexalith/platform`; `01a1e167-e67f-7531-a363-f05996b13707` EventStore story 9.2, cwd `/home/administrator/projects/hexalith/eventstore`; `01a1e0fb-4a58-7aa1-9713-5a3857c2dbac` Parties deferred work with prior push authorization, cwd `/home/administrator/projects/hexalith/parties`; `01a1e033-c7ef-77f2-9d9c-8549e82613ba` Tenants synchronization, cwd `/home/administrator/projects/hexalith/tenants`; `01a1e131-7d55-75e3-b289-496fca71c5bd` Projects story 6.6, cwd `/home/administrator/projects/hexalith/projects`; `01a1e038-b2f9-7831-9b9d-be983eedb85e` Agents synchronization/release, cwd `/home/administrator/projects/hexalith/agents`. These sibling tasks are possible coordination contacts, not proven writers to this checkout. Durable read_thread of prior FrontComposer task `01a1e0f8-9c56-7eb2-a9e4-7d1eb7b2b386` confirms the approved amendment handoff and unresolved roster. Recent inspected turns contain no individual Epic16 hold acknowledgment. ListThreads is not proof of all hosts, processes, descendants or automation. Do not invent acknowledgment or presume sibling work must stop. Existing jpiquot authority and coordination attestation are settled; only coverage of actual FrontComposer-main writers, their dated acknowledgments, automation identities/scope, and effective candidate-specific hold continuity remain missing. The M drift must be considered against the earlier hold's drift termination, never silently continued or backdated.
- Relevant planning controls read: approved six-provision amendment and clarifications; qualification proposal cadence; PRD evidence storage and G-3/D-12; architecture Active-Identity; Epic16 story criteria. They require owning story/spec records plus CI, separate candidate and provenance identities, exact candidate approvals, coordinated qualification and deadline; no examined provision requires those records to land on main before decision. Same-repository owning specs on a separate ref are a plausible implementation, not an external evidence store or identity exemption. State this as a reasoned interpretation and verify it, without asserting owner selection or execution.
- Actual release controls read: `.github/workflows/release.yml` authenticates dispatch ref `refs/heads/main`, exact live main and successful exact-SHA push CI/Quality; validates AD-13 handoff/package boundary and rechecks current main before protected credentials. `eng/release_contract.py:select_exact_ci_run` / `select_release_prerequisites` enforce these identities. `.github/workflows/release-publish.yml` checks out github.sha and prepares caller-owned release; `.releaserc.json` permits only main. No G-3 receipt or alternate-record-ref parser was found in the examined release code; acceptance of receipt provenance requires explicit human release review, not an invented workflow input. Keep all unrelated release gates. No workflow may execute from the record ref.

### Preparation tasks and acceptance

- [ ] Create `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`: one conditional, executable M procedure, with exact candidate tuple, same-repository proposed record ref `refs/heads/docs/epic-16-b34be924-records`, owning 16.1 capture and 16.3 receipt paths, immutable commit permalink discovery, evidence IDs/attempts, capture/approval ordering, coordination coverage and acknowledgments, timestamps/deadlines, strict stop rules and bounded owner disposition. Records stay off main through release; do not merge them back before the candidate is consumed. Explain why another main capture restarts the cycle and why C/R cannot be selected now. No Git mutations or evidence capture during preparation. Any future ref, commit or push needs separate explicit Git authorization; use the pinned commitlint and repository rules then, without inventing a commit message here. Candidate-specific scheduling/coordination must be explicitly resolved before execution; an already-running green run does not authorize a fresh attempt. If current M evidence cannot be admitted without a prospective authorized window, record that blocker and stop rather than reinterpret the existing schedule.
- [ ] Append a preparation result and procedure link to this spec, the deferred ledger and epic context; append only to historical text. Add a current comment to sprint tracking without changing semantic states or unrelated data. Do not modify authoritative planning documents or claim a new amendment is approved. Do not edit Story16.1.
- [ ] Validate existing story artifact checks and whitespace/scope/CRLF/local links; preserve Story16.1 SHA-256 `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, all frozen and prior bytes, unchanged gitlinks, main and index. Run the narrow existing release-contract tests for successful exact-source CI/Quality, malformed/duplicate evidence, stale main, dispatch/workflow checks under run-with-temp; record actual outcomes. Do not write new tests for this preparation.

Given unchanged exact-main selection, when records are stored only on a separate same-repository ref, then candidate M remains M and each receipt cites its own immutable record commit plus M's independent evidence; record commit SHA is never used as runtime identity. Given missing roster, schedule, green M evidence, capture or receipts, when this procedure is assessed now, then it remains prepared/conditional with Story16.3 in-progress and G-3 open. Given expiry or drift, when execution is considered, then the attempt stops with a bounded jpiquot Release Owner/Architect disposition due no later than the original outer deadline; no deadline extension, automatic retry or decision is fabricated.


## Lineage-procedure preparation result — 2026-11-15

Prepared [one conditional M procedure](epic-16-lineage-procedure-2026-11-15.md) under the latest bounded instruction. It proposes the same-repository `refs/heads/docs/epic-16-b34be924-records` for owning 16.1/16.3 records, immutable capture/receipt permalink discovery and human release review while main remains M `b34be9245443d21b72a6076cf205bbbc8a8c521f`. This is a reasoned implementation for owner review of the already approved amendment, not a new adopted amendment or a workflow input. Another record commit on main restarts the lineage cycle. No future branch, commit, push, capture, coordination contact, workflow run or release is authorized by preparing this document.

The implementation agent's read-only main/run checks completed by `2026-11-15T22:57:50Z`: main remains M; Quality43771512600 attempt1 and CI43771512961 attempt1 remain in progress. Exact M tree/catalog reads confirm EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` and empty-property default `5.1.0`, preserving SDK42 opt-in `4.2.0`. No M runtime capture, green-run claim, copied C/R counts, D4 evaluation or migration decision is added.

R's capture deadline **`2026-11-15T22:57:29Z` has expired**, checked at22:57:50Z and separately by the parent at22:57:55Z, with no new capture. C's timely accepted record remains historical and its22:12:06Z capture deadline is past; its earlier decisions remain nonoperative. M's22:50:16Z drift ends reliance on the earlier attested hold. Complete actual FrontComposer-main writer/automation coverage and dated acknowledgments, M-specific scheduling/hold continuity and admissibility of the already-running evidence remain unestablished. If current M evidence cannot be admitted without a prospective authorized window, stop rather than reinterpret the prior schedule. A green run or off-main storage/Git permission does not authorize qualification or a fresh attempt.

jpiquot as Release Owner with Architect involvement owns the actual bounded disposition no later than unchanged **`2026-11-16T17:27:10Z`**: satisfied readiness or the concrete remaining blocker and next owner action. This preparation supplies no decision, renewal, automatic retry or publication. **16.1 done;16.2 not invoked/backlog;16.3 in-progress; Epic16/G-3 open.** Historical text, frozen intent, all16.1 bytes and all prior evidence/limits remain intact.

### File List

- `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`
- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`


### Completion observation and disposition target — 2026-11-15

At the implementation agent's read-only GitHub checks completed by **2026-11-15T23:01:52Z**, main remains M, M CI43771512961 attempt1 succeeds, and M Quality43771512600 attempt1 remains in progress. R's22:57:29Z capture deadline is expired; C's timely accepted capture remains valid historical evidence. Neither deadline is renewed and no M capture is made. The original outer cap2026-11-16T17:27:10Z remains time-unexpired at this observation.

The original incident disposition was already met at21:49:02Z. M drift at22:50:16Z separately engages the retained four-working-hour owner rule; no working calendar is supplied. The procedure recommends **2026-11-16T02:50:16Z** as a conservative four-elapsed-hour owner-action target, bounded by the unchanged outer cap, strictly as a preparation recommendation. It creates no adopted calendar, renewed allowance, schedule, owner receipt or execution permission. jpiquot as Release Owner with Architect involvement must record the actual disposition.


## Tasks — lineage-procedure preparation result

These completion entries reconcile the latest preparation checklist without editing its preserved original bytes. They complete preparation only; no operative approval acceptance is claimed.

- [x] Created `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`: conditional exact-M procedure with proposed off-main record ref, owning paths, immutable permalink discovery, admission/coordination/ordering/deadlines, unchanged release controls and bounded stop/disposition rules.
- [x] Appended preparation results and the procedure link to this spec, `_bmad-output/implementation-artifacts/deferred-work.md` and `_bmad-output/implementation-artifacts/epic-16-context.md`; added comments only to `_bmad-output/implementation-artifacts/sprint-status.yaml`; preserved prior bytes, authoritative planning and Story16.1.
- [x] Ran existing narrow release-contract tests (9 passed), story-artifact validation and whitespace checks. No new tests, schema, validator, evidence bundle or workflow were written. Detailed preservation/scope results follow after their actual verification.

### Preparation verification — 2026-11-15

The exact narrow existing release-contract command was:

```bash
run-with-temp env PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=tests/eng python3 -m unittest -v \
  test_release_contract.ReleaseContractTests.test_selects_one_successful_exact_source_push_ci \
  test_release_contract.ReleaseContractTests.test_unsuccessful_runs_do_not_make_one_success_ambiguous \
  test_release_contract.ReleaseContractTests.test_release_requires_and_records_exact_quality_success \
  test_release_contract.ReleaseContractTests.test_quality_duplicate_success_pagination_and_malformed_data_fail \
  test_release_contract.ReleaseContractTests.test_select_ci_cli_requires_quality_and_never_writes_failed_selection \
  test_release_contract.ReleaseContractTests.test_dispatch_rejects_zero_sha_and_accepts_safe_integer_boundary \
  test_release_contract.ReleaseContractTests.test_checked_in_workflow_checks_quality_before_emitting_candidate \
  test_release_contract.ReleaseContractTests.test_rejects_stale_main_missing_or_failed_ci_and_malformed_api \
  test_release_contract.ReleaseContractTests.test_release_planner_and_workflow_keep_planning_tokenless_and_read_only
```

Result: **9 tests passed, 0 failures/errors/skips**. This covers exact-source push CI and Quality, unsuccessful versus duplicate successes, malformed/paginated evidence, CLI refusal without Quality, dispatch identity, stale main and the actual checked-in workflow's candidate-emission ordering/tokenless planning. It is synthetic/local control verification and proves no live M runtime or owner approval.

`run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` passed: “Story artifact validation passed.” `git diff --check` passed. Entire unchanged Story16.1 SHA-256 is `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, matching the supplied expected bytes.


Final read-only preservation assertions passed under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -`: exactly five changed/untracked preparation paths, all under `_bmad-output/`; all four pre-implementation file prefixes byte-preserved (including this spec's frozen and prior sections); Story16.1 hash exactly as above; sprint YAML semantics identical to HEAD; HEAD/main still M; index bytes and entries, including every gitlink, unchanged; all new text CRLF with final newlines; eight new relative Markdown links/anchors resolved. The initial custom checker had an escaping error in its newline assertion; correcting the disposable checker produced the stated passing result without changing preserved content. No new checker or test file was saved.

All three latest preparation tasks are complete. The procedure remains conditional and all actual admission/coordination, capture, owner-decision and execution blockers remain explicit. No Git mutation, submodule change, workflow dispatch, capture, contact, automation suspension, new owner receipt or authoritative planning edit occurred. Story16.3 intentionally remains in-progress with G-3 open.


## Review Triage Log

### Lineage-procedure preparation — 2026-11-15

All three review layers returned before triage. Blind Hunter's floor was 10 and it supplied 11 findings; Edge Case Hunter returned no findings and Verification Gap reported no gaps. Each finding is assessed below before grouping. This review does not change the in-progress story state, the approved amendment or any earlier record.

| Finding | Verdict and route | Verified evidence |
|---|---|---|
| LP-BH-1 | medium; patch | The live branch response supplies 22:50:16Z as M's committer timestamp, not push time. Main M was freshly observed by 22:53:23Z; push-run creation is 22:50:30Z. Correct the procedure and new tracking summaries; retain 02:50:16Z only as an explicitly proposed conservative handling target, not a derived observed-drift deadline. |
| LP-BH-2 | medium; patch | The new procedure guarantees reachability only through consumption and leaves subsequent retention unspecified. Durable owning records must remain reachable after consumption too; require retention until any separately authorized preservation disposition has verified another durable same-repository reference. |
| LP-BH-3 | low; patch | The approved hold ends at final disposition, while the procedure discusses later release. Explicitly end that qualification hold without extending it; later release requires separate authorization/coordination and exact-main rechecks, and drift preserves M as history. |
| LP-BH-4 | medium; patch | Step 6 recognizes an eventual exact-M outcome but omits the off-main tracking destination. Name the owning spec, context, deferred ledger and sprint as record-ref-only terminal records; main remains unchanged, and current preparation remains open. |
| LP-BH-5 | low; patch | PRD section 0.1 and G-8 retain the adopted production halt and open publication-safety work. The procedure already preserves other gates, so it grants no unsafe publication, but name this concrete halt and explain that any implementation commit advancing main requires a different candidate's evidence/decisions. |
| LP-BH-6 | medium; patch | Admission/schedule/acknowledgments are required before steps whose Git storage is not created until step 3. Specify retrievable, dated owner messages in the existing decision conversation as bootstrap sources, then the exact owning-spec section on the authorized record ref before capture; absent sources remain blocking. |
| LP-BH-7 | medium; patch | The subsequent final human review citation has no named durable section. Place M/Q/all S permalinks and the actual terminal review in the owning 16.3 spec on the record ref, then resolve its enclosing commit permalink after commit without self-reference. |
| LP-BH-8 | false; rejected | The governing receipt form is the dated owning spec plus CI, not a required original-message-coordinate schema. The requesting user explicitly re-establishes all roles, amendment approval and owner attestation in this turn; earlier C decisions remain nonoperative. No missing current authority or operative receipt is concealed by retaining historical quotations unchanged. |
| LP-BH-9 | low; rejected as spec-only | The C tuple and proposed-amendment language are preserved dated history; the later authorized-preparation and result sections explicitly identify M and the approved state. A new top-of-spec navigation edit would change this build's preserved spec prefix, which this review excludes. The standalone procedure begins with current conditional M state. |
| LP-BH-10 | false; rejected | The later completion section already explicitly says it reconciles the latest preparation checklist while preserving original bytes, and marks all three preparation results complete without claiming operative approval. A further cross-reference is a spec-only presentation change, not missing preparation work. |
| LP-BH-11 | low; patch for non-spec additions | Dense spacing occurs in the new procedure/context/ledger summaries and can obscure timestamps. Correct those newly added sentences only. Preserve historical/baseline text and this build's existing spec wording; no source or gate change is needed. |
| LP-P-1 | medium; patch | The two-hour rule starts at the last required same-attempt success, independently of later overall Quality completion. Clarify that all required successful step timestamps can establish a time-only deadline while overall Quality remains pending; partial results establish none, and capture/readiness still require overall success. |

Patch grouping after individual verdicts: LP-BH-1/11 correct new timestamp provenance and prose; LP-BH-2/3 clarify durable retention and hold termination; LP-BH-4/6/7 specify existing-record destinations and terminal tracking; LP-BH-5 names the already-open independent G-8 limitation; LP-P-1 clarifies existing cadence. These are direct documentation corrections within the authorized intent, with no new rule, schema, workflow, approval or execution authorization. No unrelated technical work is deferred. The three rejected findings leave all established decisions and historical records intact.


### Final parent observation — 2026-11-15

The parent's read-only batch started with UTC clock `2026-11-15T23:17:14Z` and completed before the next clock read `2026-11-15T23:18:19Z`. GitHub main remained M `b34be9245443d21b72a6076cf205bbbc8a8c521f`. Primary Quality 43771512600 attempt 1 remained in progress, with build-and-test still pending overall completion. Its [attempt-specific jobs](https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/43771512600/attempts/1/jobs?per_page=100) contained all eight jobs and all 32 required catalog/four Gate 2c/27 Epic 9 steps successful. Last required completion was **`2026-11-15T23:11:05Z`**, so M's independent time-only capture deadline is **`2026-11-16T01:11:05Z`**. Overall Quality success remains required; waiting for it does not pause this clock. These are metadata observations, not a new 16.1 capture or evidence admission.

R's no-capture deadline `2026-11-15T22:57:29Z` is expired. The original outer deadline remains **`2026-11-16T17:27:10Z`**. No deadline is renewed, and M's independently calculated cadence neither transfers R/C evidence nor authorizes an attempt. The procedure remains conditional on actual M-specific schedule/admission, complete writer/automation coverage and dated acknowledgments, separate Git/capture authority, passing overall Quality and valid exact-M receipts. The independent G-8 production halt remains. **Story 16.3 in-progress; G-3 open; Story 16.2 not invoked.** No new decision or Git authorization is requested by recording this observation.


### Preparation review completion and parent verification — 2026-11-15

The implementation agent applied the retained direct documentation corrections from LP-BH-1/2/3/4/5/6/7/11 and LP-P-1. The parent read the revised procedure and checked the actual controls: commit/run/observation timestamps are distinct; bootstrap and final receipts use the owning 16.3 spec on the separately authorized record ref; terminal tracking stays on that ref; durable record reachability continues after consumption; qualification ends without extending its hold; G-8 remains independently open; all-required-step success starts the two-hour clock even while overall Quality is pending. A final live-main/UTC recheck after durable final review prevents recognition for a drifted candidate. LP-BH-8/9/10 are rejected for their individually recorded reasons. No unrelated technical review work is deferred and no current owner decision is fabricated.

Passed after these corrections and the final metadata observation:

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` — “Story artifact validation passed.”
- `git diff --check` — no whitespace errors.
- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` parent read-only assertions — five changed/untracked paths, all under `_bmad-output/`; four complete baseline-M prefixes preserved; full Story 16.1 SHA-256 `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`; unchanged sprint YAML semantics and open/in-progress states; HEAD/main M; empty staged diff and unchanged selected gitlink entries; CRLF/final newlines/no added trailing whitespace; eight new relative links/anchors valid; exact deadlines and durable record destinations present.

The nine earlier focused release-contract tests remain the passed control evidence; no runtime/release-control source changed, so those tests were not repeated for prose corrections. Review used a read-only unified diff from the preserved baseline plus the new file, with a separate current-preparation diff to distinguish already adopted history; no staging occurred. The user-authorized preparation is complete and reviewable. This completion does not change Story 16.3's in-progress status, G-3's open state, the original deadlines or any tuple/approval scope, and authorizes no branch, commit, push, capture, workflow or release.
