---
title: 'Story 16.3: Approve or Reject the Exact Runtime Migration'
type: 'chore'
created: '2026-11-15'
status: 'in-progress'
route: 'dispatch'
story_id: '16.3'
story_key: '16-3-a-evt-app-1-approve-or-reject-the-exact-runtime-migration'
baseline_commit: '695d23149c56a6104e81d23a54116b4fb914df95'
review_loop_iteration: 1
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


## Authorized terminal disposition handoff — 2026-11-16

This section was amended in review loop 1. The [change log](#spec-change-log--terminal-disposition-loop-1) records the amendments and the KEEP instructions.

**Receipt.** At the parent's first UTC clock read after the answer arrived, `2026-11-16T10:10:24Z` (receipt observation, not an authoring time), the requesting user jpiquot, acting as Release Owner with Architect involvement under provision 5 of the [approved amendment](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15), answered two parent questions. The parent wrote every option label and description.

1. **Disposition — “End; correct course”.** End the M attempt and record the terminal blocker. The accountable next action is a `bmad-correct-course` on G-3 candidate identity. The selected description summarized the blocker as “three candidates (C, R, M) were each lost to main drift within hours, so exact-FrontComposer-main identity can't be met in this auto-committing workspace.” Preserve that quote as the answered text. Then record the factual correction from the observations below: the summary is wrong for M and imprecise for C and R. The correction leaves the selected disposition and next action unchanged, because the correct course's scope covers both actual causes. The description offered one example direction, “approve the runtime tuple (EventStore/Builds/catalog) and have release verify it”. It is input to that correct course only, never an adopted rule. The question stated that every option keeps G-3 open, keeps 16.3 in-progress and leaves all C/R/M history unchanged. No due date, calendar, escalation trigger or schedule was supplied.
2. **Git target — “Current branch, no commit”.** Edit in place; no staging, commit or push; the user decides Git afterwards. The question stated that the procedure's keep-off-main rule existed only to protect M's candidacy, which drift had already ended. When the question was posed, after the parent's `2026-11-16T08:43:16Z` reads, the checkout was `docs/story-16-2-not-invoked` with unpushed `c215a54ecfde7e18d3e80b81da2994a0e64979b1`. Before the answer arrived, workspace `/pushall` merged that branch into main as `ebd3f76dc0b320d455917c593ff86526b58d0c77` (commit time `2026-11-16T08:48:14Z`) and left this checkout on a clean `main`. Applying the answer to this `main` working tree is the **parent's interpretation**: it involves no checkout switch and no Git action by this build. The parent presents that interpretation to the user for confirmation at completion. Workspace `/pushall` may commit these edits straight to main, and that would be the user's Git action.

### Parent observations supplied to this disposition

The parent's read-only checks completed by `2026-11-16T10:29:40Z`. The implementation agent makes no remote call and never substitutes these observations for a capture. Times are commit times unless stated otherwise.

- **C and R were superseded by their own record lineage.** C `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` (19:53:48Z) was followed on first-parent main by `902fe13fc411c9eb7cd3786180761a904a3b47fb` (20:39:25Z, governance/cadence docs). Next came R `695d23149c56a6104e81d23a54116b4fb914df95` (20:41:28Z), the commit that records C's own 16.1 capture. R was followed by M `b34be9245443d21b72a6076cf205bbbc8a8c521f` (22:50:16Z, “fix: story 16.3”), which records R-time amendment adoption and review. This is the self-record cycle named in the amendment's [review clarification 2](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#review-clarifications--2026-11-15).
- **M expired while still main.** M stayed main until `11c8377a858a756c4e4069bf08e62db1f58e1733` (2026-11-16T05:01:35Z), unrelated mutation work; that is about six hours. M's capture deadline `2026-11-16T01:11:05Z` expired while M was still main. M was not lost to drift. Its capture never ran because the procedure's preconditions were never supplied: an M-specific admission and schedule, a complete writer roster with dated acknowledgments, and Git authorization for the record ref. The non-adopted `2026-11-16T02:50:16Z` handling target also passed. M Quality 43771512600 attempt 1 completed `success` (last updated `2026-11-15T23:19:08Z`) and CI 43771512961 attempt 1 succeeded. No M Tuple Record or M decision exists.
- **Drift after M, first-parent order.**
  - `11c8377a…` (unrelated; push Quality 43807619578 and CI 43807620376 succeeded).
  - `51d13970384597fc92dd3c5790c20e1dc11dcee7` (16.3 lineage-preparation records).
  - `fb454047dbf45f2bbd1a58bd6b84bb7ef056a3e2` (16.2 records).
  - `33e71036c04f2b99955376e9c90cbb1fc3a81e64` (08:07:01Z, `/pushall` gitlink sync).
  - Merge `ebd3f76dc0b320d455917c593ff86526b58d0c77` (08:48:14Z). Its second parent `c6233bdaf445d6a87e465a1e67f9eb266b0e322f` is a `/pushall` gitlink sync on `docs/story-16-2-not-invoked`, whose parent is the 16.2 closure `c215a54e…`. It brought the second gitlink move to main, so on first-parent history that move appears at `ebd3f76d…`.
- **Tuple drift.**
  - EventStore moved `fdb59895975e86a1e234325a01237f259cdbd53b` → `eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563` (`33e71036…`) → `97c02f7bf99a694528a2b15f0b3b0a6e5fb10e9d` (`c6233bda…`, reaching main at `ebd3f76d…`).
  - Builds moved `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` → `801f268cd1b37ecae41d4fb20d115c055b4c5793` → `65fd22f37b9fad64b595841923cdbe050563ab99` in the same commits.
  - Catalog `Props/Directory.Packages.props`:
    - At `73e60af8…`: empty-property default `5.1.0`; the SDK42-view selector yields `4.2.0`.
    - At `801f268c…`, the first move: the default is already `5.1.1`, and the SDK42 selector line gains `('$(UseHexalithProjectReferences)' != 'true' or '$(HexalithEventStoreFromSource)' == 'false')` gating. The selection logic changed, not only the value.
    - At `65fd22f3…`: default `5.1.1`, with the same gating.
  - The root `Directory.Packages.props` at HEAD sets no `HexalithEventStoreVersion`.
  - All four coordinates differ from C, R and M. This is an observation, not a Tuple Record.
- **Red current main.** All four push runs failed at attempt 1: `33e71036…` CI 43828712114 and Quality 43828712236, and `ebd3f76d…` CI 43834275373 and Quality 43834274896. They fail at the same steps:
  - CI job dependency-governance, at “Collect exact graph diff and affected-module proof” and “Enforce dependency-governance result”.
  - Quality job build-and-test, at “Prepare exact governance catalog objects”, then “Quarantine Summary” and “Coverage Summary”.

  The pattern matches a gitlink-bump seal transition; this disposition does not diagnose it. No current candidate is green.
- **Release 43810326251** (`workflow_dispatch` on `11c8377a…`, actor jpiquot, created `2026-11-16T05:27:14Z`) was waiting on `production` at `08:43Z`.
  - Its `prepare-candidate` job was cancelled at `09:47:35Z`, `release` was skipped and `verify-publication` was cancelled. The run was last updated `09:48:18Z`, before the receipt.
  - No release job ran, and the read does not expose who cancelled it.
  - It targeted a non-candidate commit while G-3 was open and the independent G-8 production halt remained. It is an observation only, not a G-3 action.
- ListAgents near `08:44Z` showed peer sessions conversations, platform, memories and mcpcli, none named for FrontComposer. That is not writer-coverage evidence.

## Tasks & Acceptance — terminal disposition

- [x] `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md` — append `## M terminal disposition — 2026-11-16` with:
  - signer/capacity, the receipt observation and the selected options quoted;
  - the correction of the parent-authored summary;
  - the ended M attempt and the end of any remaining hold reliance;
  - the per-candidate concrete blocker: C/R self-record supersession, M expiring while still main because preconditions were never supplied, later main/gitlink/catalog drift including the selection-logic change, and red current main with its failing steps;
  - the accountable next action, closure condition and “No due date supplied”;
  - the downstream effect and the post-rejection rule;
  - G-8 and the release observation;
  - the durability status and the Git interpretation.

  Then reconcile these checkboxes and record the actual verification. Leave the frontmatter workflow fields (`status`, `baseline_commit`, `review_loop_iteration`) as the parent sets them.
- [x] `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md` — append `## Terminal disposition — 2026-11-16`: the M procedure is ended, was never executed and must not be executed; link the owning section. Its off-main storage reasoning remains history and correct-course input only. Do not claim that the stop rules' terminal-record destination “applied only to an executed attempt”. State the actual basis instead: the user's answer, given after the question stated that the off-main rule only protected M's candidacy. Then state the durability status below.
- [x] `_bmad-output/implementation-artifacts/epic-16-context.md` — append the latest state: M ended, the corrected blocker, current tuple drift, the next action with its downstream effect, durability and G-3 open. Link provision 5 to the approved amendment.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` — append one dated section in the ledger's existing Epic 16 style (heading plus one `source_spec`/`summary`/`evidence` entry). Name the G-3 candidate-identity correct course, owner jpiquot (Release Owner with Architect involvement), the closure condition and this spec as source. Use the corrected blocker evidence.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` — make these three changes and no semantic change; 16.1 and 16.2 stay `done`, 16.3 and `epic-16` stay `in-progress`:
  - add one dated comment between the 16.2 and 16.3 rows;
  - append one dated pointer comment line directly after the trailing Epic 16 block's last line, `# 16.1 done;16.2 not invoked/backlog;16.3 in-progress; Epic16/G-3 open.`, saying the procedure ended and the disposition was received at `2026-11-16T10:10:24Z`;
  - update both `last_updated` values in their existing formats.

**Next action, closure and downstream effect.**
- **Next action:** jpiquot runs `bmad-correct-course` on G-3 candidate identity: what a migration approval binds to, how release verifies it, and whether the coordination, cadence and record-lineage controls remain. It must address both actual causes above: self-record supersession for C and R, and preconditions unmet within the two-hour window for M.
- **Closure:** that proposal is approved or rejected.
- **No due date or escalation trigger was supplied, and none is invented.** The earlier four-working-hour rule concerned the now-ended M drift handling and creates no new due date.
- **If the proposal is rejected,** the existing approved rules continue unchanged: any fresh attempt needs a separately explicit named schedule of at most 24 hours and its own evidence and decisions (provision 5).
- **Until closure,** no fresh attempt is scheduled and no recapture runs.
- **Downstream:** [Epic 16 context](epic-16-context.md) states that Epic 17's final Product readiness decision waits on G-3 evidence, so it keeps waiting through this open next action.

**Deadline and durability.** jpiquot's disposition was received at `2026-11-16T10:10:24Z`, before the outer deadline `2026-11-16T17:27:10Z`. It is written into this working tree and preserved in the owner decision conversation. Under the user's “no commit” answer, repository durability is the user's Git action. Until a commit containing these records is reachable on `origin`, report missing repository durability as an open item. If no such commit is reachable by `2026-11-16T17:27:10Z`, that missing durability is the remaining blocker to report. Never claim that the records were committed or that durability is met. Nothing is extended.

Given the answer, when recorded, then both selected options are quoted and the inaccuracy of the parent-authored summary for M is corrected without altering the quote or the decision. jpiquot is named as Release Owner with Architect involvement. The receipt observation time is used, with no invented authoring time, due date, trigger, roster or acknowledgment.

Given C, R and M, when the blocker is stated, then each candidate's cause matches the first-parent history above, and later drift is described in first-parent terms.

Given M's expiry and drift, when the attempt ends, then no capture, migration decision, recapture, dispatch or new attempt is created. C's three decisions stay nonoperative, G-3 stays open and 16.3 stays in-progress.

Given uncommitted records, when the deadline duty is described, then the records claim only timely receipt and working-tree recording, and they state the repository-durability condition.

Given the correct-course next action, when tracked, then its example direction is input only, the Epic 17 dependency and the post-rejection rule are stated, and no planning artifact or rule changes.

Given the diff, then exactly these five files under `_bmad-output/implementation-artifacts/` change. Every HEAD byte is unchanged, with only these exceptions:
- the spec frontmatter workflow fields;
- the two sprint `last_updated` values plus the two added sprint comment lines;
- this section's checkboxes.

Story 16.1's SHA-256 stays `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, and HEAD, index and gitlinks are unchanged.

**Boundaries.**
- No staging, commit, push, branch, switch, stash, reset or clean.
- No workflow dispatch, no remote call, and no submodule or gitlink change.
- No edit to Story 16.1, Story 16.2, planning artifacts or the frozen block.
- Workspace `/pushall` may commit working-tree edits at any time; that is the user's Git, never this build's.

**Verification.** The frontmatter-baseline validator form fails on unrelated later main commits (stale baseline: the `11c8377a…` mutation work, gitlinks and 16.2). Validate this round against current HEAD with:

```bash
run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base ebd3f76dc0b320d455917c593ff86526b58d0c77
```

Also run:
- `git diff --check`;
- the Story 16.1 SHA-256 check;
- disposable `run-with-temp` read-only assertions for scope, HEAD-byte preservation (compare checked-out bytes via `git cat-file --filters`), sprint YAML semantics, CRLF and final newlines, new link/anchor resolution (report raw and unique counts), and unchanged HEAD, index and gitlinks.

Record the actual results. There is no runtime change, so add no tests.

## Review Triage Log

### Terminal disposition — 2026-11-16 (review loop 1)

All three layers returned before triage:
- Blind Hunter: floor 6, 12 findings. Its multi-claim findings BH-11 and BH-12 are split below so that each claim gets one verdict.
- Edge Case Hunter: 11 findings.
- Verification Gap: no gaps, and one other finding.

The reviewed content was this round's delta from HEAD `ebd3f76dc0b320d455917c593ff86526b58d0c77` to the working tree. Earlier 16.3 rounds were reviewed and committed at `51d13970…`, and the frontmatter `baseline_commit` predates unrelated main work.

| Finding | Verdict and route | Verified evidence |
|---|---|---|
| TD-BH-1 | low; rejected (carried BH-1) | carried — `status: 'in-review'` is step 4's transient review state. The parent restores `in-progress` after review, and the sprint row stays `in-progress`. The verification account's “unchanged” claim was true when the check ran. |
| TD-BH-2 | medium; bad_spec (G1) | First-parent log: M `b34be924…` (22:50:16Z) stayed main until `11c8377a…` (05:01:35Z), so its 01:11:05Z capture deadline expired while M was still main. The cause is preconditions that were never supplied, not drift. The handoff's quoted, parent-authored option summary seeded the misstatement. |
| TD-BH-3 | medium; bad_spec (G1) | C was followed by `902fe13f…`, then by its own capture record `695d2314…` (R); R was followed by its own records commit `b34be924…` (M). This is the amendment's clarification-2 self-record cycle, which the blocker omitted. |
| TD-BH-4 | low; bad_spec (G7) | Builds `801f268c…` already defaults to 5.1.1 and adds `UseHexalithProjectReferences`/`HexalithEventStoreFromSource` gating to the SDK42 selector; `73e60af8…` has neither. The records checked only the end points. |
| TD-BH-5 | medium; bad_spec (G2) | The procedure's stop rules ask for committed terminal records within the deadline, or a reported missing-durability blocker. The handoff's “satisfies provision 5's duty” and the records' “applied only to an executed attempt” both overstate. The basis that holds is the user's no-commit answer, given after the question stated the off-main rule's narrow purpose. |
| TD-BH-6 | low; rejected | The procedure banner already reads “execution blocked”, so nothing invites a reader to execute it. The ended state is appended under the append-only rule, and rewriting the banner needs an exception for little benefit. |
| TD-BH-7 | low; bad_spec (G3) | Merge `ebd3f76d…` (08:48:14Z) landed after the question was posed (after the 08:43:16Z reads) and before the 10:10:24Z answer. The handoff asserted compliance instead of recording the parent's interpretation for confirmation. No Git action occurred. |
| TD-BH-8 | medium; bad_spec (G4) | `epic-16-context.md` states that Epic 17's final readiness waits on G-3. The records omitted that, and the existing provision-5 rule that applies after a rejection. No due date or trigger may be invented, because the user supplied none. |
| TD-BH-9 | low; bad_spec (G5) | The G-8 restatement was dropped. The run's jobs show `prepare-candidate` cancelled at 09:47:35Z and `release` skipped, before the 10:10:24Z receipt, so the “9 seconds after receipt” inference is false. The canceller is not exposed. |
| TD-BH-10 | low; bad_spec (G6) | Current main's CI and Quality fail at the same named steps as `33e71036…`, but the records named the steps only for `33e71036…`. |
| TD-BH-11a | low; bad_spec (G8) | The handoff's task heading was not a validator-recognized form, so the five checkboxes only produced non-failing notices. The amended handoff uses `Tasks & Acceptance — terminal disposition`. |
| TD-BH-11b | false; rejected | The five planning paths are explicitly labelled “pre-existing … unchanged by this disposition”. The validator requires each File List path to match a change, so the labels make the scope explicit rather than blurring it. |
| TD-BH-12a | low; rejected | The workflow's own defer format is the legacy `source_spec` bullet, and every earlier Epic 16 ledger section is prose. The item is a human-owned governance decision tracked in the sprint and context. Converting it to a sweep-triaged DW entry would expose it to automated resolution, which is more than a direct correction. |
| TD-BH-12b | false; rejected | The ledger section explicitly states that it supersedes the preceding pending state “from that receipt onward” and that those sections remain history. |
| TD-BH-12c | low; patch (carried into re-derivation) | The context cited provision 5 without the amendment link; the amended task now requires the link. |
| TD-BH-12d | low; patch (carried into re-derivation) | The verification said “nine” new links, but the raw count is 10 and the unique count 9. The amended verification asks for both counts. |
| TD-ECH-1 | low; rejected (carried BH-1) | carried — same transient-status claim as TD-BH-1. |
| TD-ECH-2 | medium; bad_spec (G2) | Same root cause as TD-BH-5: the deadline claim rested on uncommitted edits. |
| TD-ECH-3 | medium; bad_spec (G4) | The missing due date or trigger is the owner's actual answer. Record the Epic 17 effect and the existing post-rejection rule; inventing a review trigger is not allowed. |
| TD-ECH-4 | low; rejected | Same claim and reasoning as TD-BH-12a. |
| TD-ECH-5 | low; bad_spec (G3) | Same root cause as TD-BH-7. The edits stay working-tree only, and the parent presents the interpretation at completion. |
| TD-ECH-6 | low; bad_spec (G9) | `sprint-status.yaml` lines 1324–1328 still describe the conditional procedure with the disposition “due … unchanged”. The handoff's “one dated comment” did not name that trailing block. |
| TD-ECH-7 | low; bad_spec (G8) | Same as TD-BH-11a. |
| TD-ECH-8 | low; rejected (carried BH-1) | carried — the checked task's “keep in-progress” intent holds once the parent restores the status after review. |
| TD-ECH-9 | low; rejected (carried BH-1) | carried — the append-only claim excludes the parent's frontmatter workflow fields, which the amended AC now names. |
| TD-ECH-10 | medium; bad_spec (G2) | Same root cause as TD-BH-5: the stop rule's fallback wording is not limited to executed attempts. |
| TD-ECH-11 | low; bad_spec (G1) | `c215a54e…` and `c6233bda…` are second-parent commits of merge `ebd3f76d…`. A first-parent bisect places the second gitlink move at `ebd3f76d…`. |
| TD-VG-O1 | low; rejected (carried BH-1) | carried. Step 5's done/review sync would contradict the frozen intent that 16.3 stays in-progress until operative approvals exist. The parent keeps 16.3 in-progress and leaves any closure to the user. |

Grouping after the individual verdicts:
- **G1:** blocker causal account (TD-BH-2, TD-BH-3, TD-ECH-11).
- **G2:** deadline/durability overclaim (TD-BH-5, TD-ECH-2, TD-ECH-10).
- **G3:** Git-target interpretation (TD-BH-7, TD-ECH-5).
- **G4:** downstream and post-closure effect (TD-BH-8, TD-ECH-3).
- **G5:** G-8 and release facts (TD-BH-9).
- **G6:** red-run steps (TD-BH-10).
- **G7:** catalog selection change (TD-BH-4).
- **G8:** task heading (TD-BH-11a, TD-ECH-7).
- **G9:** stale sprint trailing block (TD-ECH-6).

G1, G2 and G3 originate in this handoff's own wording, so the entries route to bad_spec and loop back. The two patch items are carried into the amended tasks. Rejected findings leave every record unchanged.

## Spec Change Log — terminal disposition loop 1

- **Trigger:** review loop 1 findings G1–G9 above, with medium findings TD-BH-2/3/5/8 and TD-ECH-2/3/10.
- **Amended:** this handoff (outside the frozen block). The amendments:
  - mark the option text as parent-authored and require correcting it for M;
  - add first-parent supersession facts for C/R/M, the catalog selection change at `801f268c…`, the failing steps for current main, and the release-job facts with G-8;
  - restate the Git target as the parent's interpretation;
  - replace “satisfies provision 5's duty” with a timely-receipt plus repository-durability condition;
  - add the Epic 17 dependency and the post-rejection rule;
  - name the sprint trailing-block pointer;
  - use a validator-recognized task heading;
  - name the frontmatter workflow-field exception.
- **Known-bad state avoided:** records claiming that M was lost to drift; records claiming the deadline duty was satisfied by uncommitted edits; records asserting that a branch-target substitution complied with the answer; the stop-rule fallback dismissed as executed-attempt-only; a stale “due … unchanged” sprint note.
- **Reverted:** this build's first-derivation edits to all five files, restored to HEAD `ebd3f76dc0b320d455917c593ff86526b58d0c77`. Disposable copies remain outside the repository.
- **KEEP:**
  - section headings and anchors: spec `## M terminal disposition — 2026-11-16`, procedure `## Terminal disposition — 2026-11-16`, context `## M terminal disposition — 2026-11-16`, ledger `## Epic 16 M terminal disposition — 2026-11-16`;
  - the signer/capacity paragraph citing the five-capacity confirmation (`2026-11-15T21:49:02Z`, [human response](#human-response-and-incident-disposition--2026-11-15)) and the planning receipt (`2026-11-15T21:58:49Z`), with no capacity inferred;
  - receipt-observation wording (not an authoring time; decision date 2026-11-16 UTC);
  - full SHAs, run IDs and the per-commit gitlink transitions;
  - the example direction as input only, and “No due date supplied”;
  - C's three decisions nonoperative, linked to [migration decisions](#migration-decisions-received--2026-11-15);
  - the closing status line “Story 16.1 done; Story 16.2 done as not invoked; Story 16.3 in-progress; Epic 16 / G-3 open. Migration approval: not claimed.”;
  - the sprint comment between the 16.2 and 16.3 rows, with header `# last_updated: YYYY-MM-DD` and body `last_updated: MM-DD-YYYY HH:MM`;
  - a File List with the five changed paths plus the five 2026-11-15 planning paths labelled pre-existing, because the validator needs that against `--base`;
  - verification comparing checked-out bytes via `git cat-file --filters`, with the validator run against `--base ebd3f76dc0b320d455917c593ff86526b58d0c77`.

## M terminal disposition — 2026-11-16

**Signer and capacity.** Jérôme Piquot (`jpiquot`/Administrator), acting as Release Owner with Architect involvement under provision 5 of the [approved amendment](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15). The authority source is the separate five-capacity confirmation recorded at `2026-11-15T21:49:02Z` ([human response](#human-response-and-incident-disposition--2026-11-15)) and the planning receipt received `2026-11-15T21:58:49Z`. No capacity is inferred from another.

**Receipt observation.** `2026-11-16T10:10:24Z` is the parent's first UTC clock read after the answer arrived. It is a receipt observation, not an authoring time; no authoring time was supplied. Decision date: 2026-11-16 (UTC). The parent wrote every option label and description.

**Selected options, as answered.**

1. **“End; correct course”**: end the M attempt and record the terminal blocker. The accountable next action is a `bmad-correct-course` on G-3 candidate identity. The selected description's summary is preserved as answered: “three candidates (C, R, M) were each lost to main drift within hours, so exact-FrontComposer-main identity can't be met in this auto-committing workspace.” Its example direction, “approve the runtime tuple (EventStore/Builds/catalog) and have release verify it”, is input to that correct course only, never an adopted rule.
2. **“Current branch, no commit”**: edit in place; no staging, commit or push; the user decides Git afterwards.

The question stated that every option keeps G-3 open, keeps 16.3 in-progress and leaves all C/R/M history unchanged. No due date, calendar, escalation trigger or schedule was supplied.

**Correction of the parent-authored summary.** The quoted summary is wrong for M and imprecise for C and R. C and R were not lost to unrelated drift: each was superseded on first-parent main by its own record lineage, the self-record cycle named in the amendment's [review clarification 2](../planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#review-clarifications--2026-11-15). M was not lost to drift at all: it stayed main for at least six hours, its capture never ran because the procedure's preconditions were never supplied, and its capture deadline expired while it was still main. The quote and the selected options stay as answered. The correction changes neither the disposition nor the next action, because the correct course's scope covers both actual causes.

**The M attempt is ended.** The conditional [M lineage procedure](epic-16-lineage-procedure-2026-11-15.md#terminal-disposition--2026-11-16) ends without ever having been executed. No M admission, schedule, complete writer roster, dated acknowledgment, record ref, M Tuple Record or M migration decision was created, and none is created now. Any remaining reliance on the owner-attested qualification hold that began `2026-11-15T21:58:49Z` ends with this named terminal blocker disposition; the observed R-to-M change had already ended its continuity for M. Nothing is continued, renewed, backdated or transferred. No capture, recapture, workflow dispatch or new attempt follows.

**Concrete blocker, per candidate.** Candidates are listed in first-parent order. Times are commit times, on 2026-11-15 unless dated otherwise.

- **C `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` (19:53:48Z): superseded by its own record lineage.** First-parent main moved to `902fe13fc411c9eb7cd3786180761a904a3b47fb` (20:39:25Z, governance/cadence docs) and then to R, the commit that records C's own 16.1 capture. C's timely accepted capture (primary Quality 43755163930 attempt 1) remains history. Its capture deadline `2026-11-15T22:12:06Z` has passed, and its three [migration decisions](#migration-decisions-received--2026-11-15) stay nonoperative.
- **R `695d23149c56a6104e81d23a54116b4fb914df95` (20:41:28Z): superseded by its own records commit.** R was followed by M (“fix: story 16.3”), which records R-time amendment adoption and review. R's Quality 43759663406 attempt 1 succeeded, but R has no capture and no decisions, and its capture deadline `2026-11-15T22:57:29Z` expired.
- **M `b34be9245443d21b72a6076cf205bbbc8a8c521f` (22:50:16Z): expired while still main.** M stayed main at least until `11c8377a858a756c4e4069bf08e62db1f58e1733`, whose commit time is 2026-11-16T05:01:35Z and whose push Quality 43807619578 was created at `2026-11-16T05:01:57Z` (parent observation); that is at least six hours. Its capture deadline `2026-11-16T01:11:05Z` expired while M was still main. The capture never ran because the procedure's preconditions were never supplied. They were required before step 1 and were still unsupplied at the receipt: an M-specific admission and schedule, a complete writer roster with dated acknowledgments, and Git authorization for the record ref. The non-adopted `2026-11-16T02:50:16Z` handling target also passed. M Quality 43771512600 attempt 1 completed `success` (last updated `2026-11-15T23:19:08Z`) and CI 43771512961 attempt 1 succeeded. No M Tuple Record or M decision exists.
- **Later main, gitlink and catalog drift, in first-parent order.**
  - `11c8377a…`: unrelated mutation work; push Quality 43807619578 and CI 43807620376 succeeded.
  - `51d13970384597fc92dd3c5790c20e1dc11dcee7`: 16.3 lineage-preparation records.
  - `fb454047dbf45f2bbd1a58bd6b84bb7ef056a3e2`: 16.2 records.
  - `33e71036c04f2b99955376e9c90cbb1fc3a81e64` (2026-11-16T08:07:01Z, `/pushall` gitlink sync): EventStore `fdb59895975e86a1e234325a01237f259cdbd53b` → `eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563`; Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` → `801f268cd1b37ecae41d4fb20d115c055b4c5793`.
  - Merge `ebd3f76dc0b320d455917c593ff86526b58d0c77` (2026-11-16T08:48:14Z): its second parent `c6233bdaf445d6a87e465a1e67f9eb266b0e322f` is a `/pushall` gitlink sync on `docs/story-16-2-not-invoked`, whose parent is the 16.2 closure `c215a54ecfde7e18d3e80b81da2994a0e64979b1`. It brought EventStore → `97c02f7bf99a694528a2b15f0b3b0a6e5fb10e9d` and Builds → `65fd22f37b9fad64b595841923cdbe050563ab99` to main, so on first-parent history that second move appears at `ebd3f76d…`.

  The Builds catalog `Props/Directory.Packages.props` changed its selection logic, not only a value. At `73e60af8…` the empty-property default is `5.1.0` and the SDK42-view selector yields `4.2.0`. At the first move, `801f268c…`, the default is already `5.1.1`, and the SDK42 selector gains `('$(UseHexalithProjectReferences)' != 'true' or '$(HexalithEventStoreFromSource)' == 'false')` gating. `65fd22f3…` keeps default `5.1.1` with the same gating. The root `Directory.Packages.props` at HEAD sets no `HexalithEventStoreVersion`. All four coordinates of current main differ from C, R and M. This is an observation, not a Tuple Record.

  The parent's later read-only review supplied the following observations. They are correct-course input only and change no rule or decision.

  **Full first catalog change.** The `73e60af8…`→`801f268c…` change to that file has 9 insertions and 6 deletions:
  - the EventStore default `5.1.0`→`5.1.1`, with source-mode gating on the SDK42 selector;
  - the same gating on the SDK42 OpenAPI pair (`HexalithAspNetCoreOpenApiVersion` `10.0.15`, `HexalithOpenApiVersion` `2.13.0`);
  - `HexalithFoldersVersion` `1.4.0`→`2.0.0`;
  - a new SDK42 `HexalithMcpCliVersion` `2.0.1` pin.

  FrontComposer's `deps.local.props` sets `HexalithEventStoreFromSource` to true and `deps.nuget.props` sets it to false, so from `801f268c…` onward SDK42-view projects resolve `4.2.0` only outside source mode.

  **Runtime unchanged from C through `fb454047…`.** From C through `fb454047dbf45f2bbd1a58bd6b84bb7ef056a3e2`, EventStore `fdb59895…`, Builds `73e60af8…`, catalog default `5.1.0` and the absent root preset stayed the same. Only the FrontComposer commit changed, over commit times from 19:53:48Z up to the 2026-11-16T08:07:01Z move in `33e71036…`. C's decisions stay nonoperative, and nothing transfers.

  **Both tuple moves were workspace `/pushall` gitlink syncs.** `33e71036…` and `c6233bda…` are both `/pushall` gitlink syncs. `c6233bda…` was committed at 2026-11-16T08:47:45Z, while the owner question was open, and was not visible to the 08:44Z ListAgents read. This is a further input to the correct course's coordination controls, alongside the two causes.

  **Other gitlinks also moved.**
  - At `33e71036…`: AI.Tools `17a37d83`→`7ef8166e`, Commons `fbf4e942`→`ab5e92fa`, Memories `b7e19503`→`fad5ec5e`, Parties `e59aed9c`→`d4a71068`, Platform `f6f85032`→`17b1f688`, PolymorphicSerializations `e97b5693`→`476cfcca`, Tenants `2ec93839`→`8aa6f169`.
  - At `c6233bda…`: Memories `fad5ec5e`→`9d87500e`, Platform `17b1f688`→`ead9ff0d`.
- **Red current main.** All four push runs failed at attempt 1: `33e71036…` CI 43828712114 and Quality 43828712236, and `ebd3f76d…` CI 43834275373 and Quality 43834274896. Both commits fail at the same steps: CI job dependency-governance at “Collect exact graph diff and affected-module proof” and “Enforce dependency-governance result”; Quality job build-and-test at “Prepare exact governance catalog objects”, then “Quarantine Summary” and “Coverage Summary”. The pattern matches a gitlink-bump seal transition; this disposition does not diagnose it. No current candidate is green.

The run results and release-job facts are the parent's read-only observations, completed by `2026-11-16T10:29:40Z`. This build made no remote call and substitutes none of them for a capture. Read-only local Git confirms the first-parent chain and its commit times, the merge parents, the EventStore/Builds gitlinks at each commit, both catalog changes at `801f268c…`, the `5.1.1` default and gating at `65fd22f3…`, and the absence of a root `HexalithEventStoreVersion` at HEAD. These local reads evaluate no runtime compatibility.

**Accountable next action.** jpiquot runs `bmad-correct-course` on G-3 candidate identity: what a migration approval binds to, how release verifies it, and whether the coordination, cadence and record-lineage controls remain. It must address both actual causes: self-record supersession for C and R, and M's preconditions, which were required before step 1 and still unsupplied at the receipt. The `/pushall` gitlink-sync observation above is a further input to its coordination controls. Per parent observation, two existing action items in `sprint-status.yaml` are related, and neither changes here. E11R-AI-1 (open, gate G-3, owner Architect + Release Owner) is the item the correct course will likely redefine. E12R-AI-2 (in-progress), whose text includes “stop bundling pinned-submodule gitlink bumps into story or spec commits”, bears on the red current main. The example direction above is input only. No planning artifact, selector, gate, receipt requirement, action item or rule changes here.

**Closure condition.** That correct-course proposal is approved or rejected. If it is approved, its adopted provisions govern G-3 and 16.3. The owner did not address withdrawal or revision; either leaves the item open. **No due date supplied.** No due date, calendar or escalation trigger was supplied, and none is invented. No fresh attempt is scheduled and no recapture runs. That describes the current state, not a new bar: provision 5's separately explicit named schedule stays available to the owner.

**Four-working-hour rule.** The approved rule in [PRD §0.1](../planning-artifacts/prd.md#01-v10-readiness-milestone-gates), the [architecture Active-Identity invariant](../planning-artifacts/architecture.md#eventstore-active-identity-invariant) and the [Epic 16 qualification text](../planning-artifacts/epics.md#epic-16-maintainers-verify-and-approve-exact-runtime-compatibility) still applies. The Release Owner, with Architect involvement, records a bounded disposition within four working hours of drift, missing authority, failed qualification, timeout or a latest-main lineage conflict. The options are to schedule one fresh attempt, reject with corrective work, or request an explicit planning amendment. M's `2026-11-16T01:11:05Z` timeout and the later drift engaged that rule. No working calendar was supplied, so timeliness cannot be measured; the receipt came about nine elapsed hours after the timeout. This disposition is the rule's “request an explicit planning amendment” option, bounded by its approve-or-reject closure. No time bound was supplied for the amendment itself, and the non-adopted procedure's “bounded due” suggestion stays unmet.

**Downstream effect and post-rejection rule.** [Epic 16 context](epic-16-context.md#cross-story-dependencies) states that Epic 17's final Product readiness decision waits on G-3 evidence, so it keeps waiting through this open next action. If the proposal is rejected, the existing approved rules continue unchanged: any fresh attempt needs a separately explicit named schedule of at most 24 hours and its own evidence and decisions (provision 5).

**G-8 and the release observation.** The independent G-8 production halt in [PRD §0.1](../planning-artifacts/prd.md#01-v10-readiness-milestone-gates) remains open; this disposition neither resolves nor relies on it. Release 43810326251 (`workflow_dispatch` on `11c8377a…`, actor jpiquot, created `2026-11-16T05:27:14Z`) was waiting on `production` at `08:43Z`. Its `prepare-candidate` job was cancelled at `09:47:35Z`, `release` was skipped and `verify-publication` was cancelled. The run was last updated `09:48:18Z`, before the receipt. No release job ran, and the read does not expose who cancelled it. It targeted a non-candidate commit while G-3 was open and the G-8 halt remained. It is an observation only, not a G-3 action. ListAgents near `08:44Z` showed peer sessions conversations, platform, memories and mcpcli, none named for FrontComposer. That is not writer-coverage evidence, and no roster or acknowledgment is invented.

**Durability and deadline.** jpiquot's disposition was received by `2026-11-16T10:10:24Z` (first clock read after receipt), before the outer deadline `2026-11-16T17:27:10Z`. The disposition is made and preserved in the owner decision conversation for this disposition, Claude Code session `f1c496f3-d5c9-4e70-9e18-2c6ba8cc8d69`, which is distinct from the earlier thread `01a1e169-527e-7800-bc63-aa56bca0b1eb`. As of this build (`2026-11-16T11:03:16Z`), these records are in the working tree and this build committed nothing. They claim only that timely receipt and that recording. Under the user's “no commit” answer, repository durability is the user's Git action. The first commit carrying these records that is reachable on `origin` closes the durability item by itself: its commit and push metadata is the evidence, and no follow-up edit is needed. If no such commit is reachable by `2026-11-16T17:27:10Z`, jpiquot as Release Owner reports missing repository durability as the remaining blocker in that owner decision conversation. Repository durability is not claimed here, and nothing is extended.

**Git interpretation.** When the question was posed, the checkout was `docs/story-16-2-not-invoked` with unpushed `c215a54ecfde7e18d3e80b81da2994a0e64979b1`. Before the answer arrived, workspace `/pushall` merged that branch into main as `ebd3f76dc0b320d455917c593ff86526b58d0c77` (2026-11-16T08:48:14Z) and left this checkout on a clean `main`. Applying “Current branch, no commit” to this `main` working tree is the **parent's interpretation**, which the parent presents to the user for confirmation. The user's confirmation or contrary direction will be recorded in a later dated section of this spec. If it is contrary, the user directs relocation; anything workspace `/pushall` has already committed stays as history. As of this build (`2026-11-16T11:03:16Z`), this build switched no checkout and staged, committed or pushed nothing. Workspace `/pushall` may commit these edits straight to main; that would be the user's Git action. The answer reports no change in EventStore authority, so Story 16.2 stays closed as not invoked.

**Story 16.1 done; Story 16.2 done as not invoked; Story 16.3 in-progress; Epic 16 / G-3 open. Migration approval: not claimed.**

### File List

- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md` — pre-existing: changed by the 2026-11-15 adoption round, unchanged by this disposition
- `_bmad-output/planning-artifacts/prd.md` — pre-existing: changed by the 2026-11-15 adoption round, unchanged by this disposition
- `_bmad-output/planning-artifacts/architecture.md` — pre-existing: changed by the 2026-11-15 adoption round, unchanged by this disposition
- `_bmad-output/planning-artifacts/epics.md` — pre-existing: changed by the 2026-11-15 adoption round, unchanged by this disposition
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md` — pre-existing: changed by the 2026-11-15 adoption round, unchanged by this disposition

### Terminal disposition verification — 2026-11-16

These checks first ran after the five edits were applied at `2026-11-16T10:42:29Z`. They ran again after the five task checkboxes above were reconciled and this subsection was appended. Both runs passed.

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base ebd3f76dc0b320d455917c593ff86526b58d0c77`: passed with “Story artifact validation passed.” Before this round's edits, the same command failed only on File List entries with no matching change against HEAD. This round's actual edits resolved four of them, and the five 2026-11-15 planning paths are labelled pre-existing in this round's File List.
- `git diff --check`: passed with no whitespace errors.
- Story 16.1 SHA-256: `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`, unchanged.
- Disposable read-only assertions, run as `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -I <scratch checker>` from outside the repository, all passed:
  - Scope: exactly these five paths are modified, unstaged, all under `_bmad-output/implementation-artifacts/`, with no untracked files. Story 16.1, Story 16.2 and `_bmad-output/planning-artifacts/` are untouched.
  - Repository state: HEAD and local `main` are still `ebd3f76dc0b320d455917c593ff86526b58d0c77`, and the checkout is still `main`. Nothing is staged. The `git ls-files -s` listing is byte-identical to the pre-edit listing, and all nine index gitlinks equal HEAD's.
  - HEAD-byte preservation, comparing checked-out bytes from `git cat-file --filters`: in the procedure, context and ledger, the HEAD bytes are an unchanged prefix and content is only appended. In the spec, the HEAD bytes are an unchanged prefix apart from the parent-set frontmatter `review_loop_iteration`. Every byte of the parent's handoff is preserved apart from the five `- [ ]` → `- [x]` reconciliations in its task list, and the frontmatter equals the parent-set pre-edit frontmatter.
  - Sprint: exactly two lines are added, the dated comment between the 16.2 and 16.3 rows and the pointer line directly after the trailing Epic 16 block's last line. Exactly two lines are replaced, the header `last_updated` (`2026-11-15` → `2026-11-16`) and the body `last_updated` (`11-16-2026 07:41` → `11-16-2026 10:42`), each in its existing format. The parsed YAML equals HEAD except `last_updated`, with `epic-16` in-progress, 16.1 done, 16.2 done and 16.3 in-progress.
  - Format and links: all five files are CRLF-only with final newlines, and the added text has no trailing whitespace. The added text contains 15 relative Markdown links (raw count), to 8 unique targets (unique count); every file and anchor resolves. The sprint comment's spec anchor also resolves.
  - Content: the spec disposition quotes both selected options and the parent-authored summary, corrects the summary for M, C and R, cites the full SHAs and run IDs listed above, and keeps the example direction as input only. It repeats none of the overclaims or the false release-timing fact that review loop 1 corrected. The procedure, context and ledger additions carry their required headings, links and states, and the ledger addition has exactly one `source_spec`/`summary`/`evidence` entry.

No runtime code changed, so no tests were added or run. This build made no remote call, workflow dispatch, capture, staging, commit or push.

**Review-fix pass.** After the coordinator's review, only this round's appended text was corrected, as of `2026-11-16T11:03:16Z`. The fixes scope the durability wording to this build and name its closing step, reporter and conversation; record the Git-interpretation recording slot; restate the four-working-hour rule and closure outcomes; add the parent-observed correct-course inputs; and use “at least until” for M's drift time. The two sprint comments now state the durability condition. Git commands were excluded for this pass, so the validator, `git diff --check` and the Git-based assertions above were not rerun; the parent runs full verification. A disposable non-Git check against the pre-round copies passed: every pre-round byte is preserved with the same exceptions as above plus the parent's later frontmatter `status` review-state change, all five files are CRLF-only with final newlines and no trailing whitespace in added text, the sprint YAML equals the pre-round copy except `last_updated`, and Story 16.1's SHA-256 is unchanged. The added text now contains 19 relative Markdown links (raw count) to 11 unique targets, all resolving.


## Review Triage Log — terminal disposition review loop 2

All three layers returned before triage:
- Blind Hunter: floor 8, 18 findings.
- Edge Case Hunter: 13 findings.
- Verification Gap: no gaps, and one other finding.

The reviewed content was the re-derived delta from HEAD `ebd3f76dc0b320d455917c593ff86526b58d0c77`. Rows matching a loop-1 row at the same location with the same claim are marked carried. The parent verified every other claim read-only before rendering its verdict.

| Finding | Verdict and route | Verified evidence |
|---|---|---|
| TD2-BH-1 | medium; patch (G10) | The added “uncommitted” and “working tree” sentences have no time scope, so they become false inside the commit that carries them. No closing step or reporter was named. |
| TD2-BH-2 | low; patch (G10) | The durability fallback named “the owner decision conversation” without a handle. This disposition's conversation is Claude Code session `f1c496f3-d5c9-4e70-9e18-2c6ba8cc8d69`, not the earlier thread `01a1e169-527e-7800-bc63-aa56bca0b1eb`. |
| TD2-BH-3 | low; patch (G11) | The approved rule's bounded-disposition options include “request an explicit planning amendment”, which is bounded by its closure event. No time bound was supplied for the amendment, and the non-adopted procedure's “bounded due” suggestion stays unmet; the records now say so. |
| TD2-BH-4 | medium; patch (G11) | PRD line 40, architecture line 398 and epics line 4059 require a disposition within four working hours of drift, missing authority, failed qualification, timeout or lineage conflict. The added claim that the rule “concerned the now-ended M drift handling” misstated an approved rule. That phrase came from the handoff, and the records now correct it. |
| TD2-BH-5 | medium; patch (G12) | `git ls-tree` shows that C, `902fe13f…`, R, M, `11c8377a…`, `51d13970…` and `fb454047…` all select EventStore `fdb59895…` and Builds `73e60af8…`, with no root preset. Only the FrontComposer commit changed until `33e71036…`, a key input for the correct course that the records omitted. |
| TD2-BH-6 | low; patch (G12) | Both later tuple moves were `/pushall` gitlink syncs. The reflog shows `c6233bda…` committed at 08:47:45Z while the owner question was open, and the 08:44Z ListAgents read did not show it. |
| TD2-BH-7 | low; patch (G13) | “Until 05:01:35Z” used a commit time as the drift time. Push Quality 43807619578 was created at 05:01:57Z, so the records now say “at least until”, consistent with LP-BH-1. |
| TD2-BH-8 | low; patch (G12) | Sprint action items E11R-AI-1 (open, gate G-3) and E12R-AI-2 (in-progress, “stop bundling pinned-submodule gitlink bumps”) exist and are relevant, but the records did not link them. |
| TD2-BH-9 | low; rejected (carried TD-BH-1) | carried — the frontmatter `status: 'in-review'` is step 4's transient state. The verification claim was true when it ran, and the parent restores `in-progress` after review. |
| TD2-BH-10 | low; patch (G12) | Diffing Builds `73e60af8…`→`801f268c…` also shows OpenAPI-pair gating, Folders 1.4.0→2.0.0 and a new SDK42 McpCli 2.0.1 pin. The records described only the EventStore lines. |
| TD2-BH-11 | low; patch (G13) | The no-ref claim did not name the ref and could not speak for `origin`. The parent's `git ls-remote origin` at 10:56:43Z returned no `refs/heads/docs/epic-16-b34be924-records`. |
| TD2-BH-12 | low; rejected (carried TD-BH-6) | carried — same banner location and the same “reads as executable” claim. A top-of-file pointer would break the append-only prefix. |
| TD2-BH-13 | low; patch (G10) | The two added sprint comments omitted the 17:27:10Z durability condition. |
| TD2-BH-14 | low; rejected | The validator merges same-named File List and Triage Log sections by design, and earlier rounds already use the same pattern. The duplicate planning paths are explicitly labelled pre-existing, and renaming the headings would rewrite preserved structure for an unlikely harm. |
| TD2-BH-15 | low; patch (G13) | The ledger evidence attributed the deadline's expiry to the missing preconditions; it is the capture that never ran. The summary also omitted the scope requirement and G-8. |
| TD2-BH-16 | low; patch (G11) | “Unmet within the two-hour window” framed M as a window-length problem. The preconditions were required before step 1 and were still unsupplied at the receipt. |
| TD2-BH-17 | low; patch (G10) | The Git interpretation had no slot for recording the user's confirmation and no fallback if the user disagrees. |
| TD2-BH-18 | low; rejected | The loop-1 triage rows quote each corrected wording. Scratchpad copies are non-durable by nature, so a path would add nothing durable, and the fix would edit this build's change log. |
| TD2-ECH-1 | medium; patch (G10) | Same root cause as TD2-BH-1. |
| TD2-ECH-2 | medium; patch (G10) | No reporter, and the validity of the disposition, was defined if durability fails. The disposition is made and preserved in the session either way; jpiquot reports any durability blocker there. |
| TD2-ECH-3 | low; patch (G10) | Same root cause as TD2-BH-17. |
| TD2-ECH-4 | false; rejected | Commit-subject classification (unmapped/interleaved) runs only in `--candidate` mode, at `eng/validate-story-artifacts.py:1540`. The recorded `--base` command never classifies subjects. |
| TD2-ECH-5 | low; patch (G14) | Only the effect of a rejection was stated. The records now say adopted provisions govern on approval, and that withdrawal or revision, which the owner did not address, leaves the item open. |
| TD2-ECH-6 | low; patch (G14) | “Until closure, no fresh attempt is scheduled” could read as a new bar. The records now describe it as the current state, with provision 5's explicit schedule still available. |
| TD2-ECH-7 | low; patch (G12) | `git diff-tree` shows seven further gitlink moves at `33e71036…` and two at `c6233bda…` (Memories, Platform), which the records did not list. |
| TD2-ECH-8 | low; patch (G12) | `deps.local.props` sets `HexalithEventStoreFromSource` to true and `deps.nuget.props` to false, so the SDK42 4.2.0 view applies only outside source mode. |
| TD2-ECH-9 | false; rejected | The reflog shows the checkout moved to `main` at 08:47:49Z and the merge landed at 08:48:14Z. The blocking question returned just before the parent's 10:10:24Z clock read, so the answer post-dates the merge. |
| TD2-ECH-10 | low; rejected (carried TD-BH-1) | carried — transient review status. |
| TD2-ECH-11 | medium; patch (G11) | Same root cause as TD2-BH-4. |
| TD2-ECH-12 | low; patch (G10) | “Received at 10:10:24Z” read as an exact instant. It is the first clock read after receipt, so the records now say “received by”. |
| TD2-ECH-13 | low; patch (G10) | Same root cause as TD2-BH-13. |
| TD2-VG-O1 | low; rejected (carried TD-VG-O1) | carried — the parent restores `in-progress` before presenting, and leaves any Git action to the user. |

Grouping after the individual verdicts:
- **G10:** durability scope, reporter, handle and Git fallback.
- **G11:** the approved four-working-hour rule and the precondition framing.
- **G12:** correct-course inputs (unchanged runtime triple, `/pushall` syncs, other gitlinks, full catalog change, build-mode gating, action items).
- **G13:** timing and wording precision.
- **G14:** closure semantics.

No group's cause lies inside the frozen block, and each fix replaces or adds prose without changing a decision. They therefore route to patch, sent once to the same implementation agent. The handoff wording these records refine — the four-working-hour sentence, “within the two-hour window” and “Until closure, no fresh attempt is scheduled” — is superseded by the corrected records. The parent verified the patched tree as recorded below. No item is deferred.

### Parent verification after review loop 2 — 2026-11-16

The parent applied one direct correction, to the sprint 16-3 row comment added this round. The comment said “M expired while still main because its preconditions were never supplied”, the same causal slip as TD2-BH-15; it now says the capture never ran for lack of preconditions and the deadline expired while M was still main.

The parent then ran these checks on the patched tree at `2026-11-16T11:03:50Z` and repeated them after this section was appended. All passed:

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base ebd3f76dc0b320d455917c593ff86526b58d0c77` printed “Story artifact validation passed.”, with no task-section notices.
- `git diff --check` passed.
- Story 16.1's SHA-256 is still `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`.
- HEAD is still `ebd3f76dc0b320d455917c593ff86526b58d0c77` and nothing is staged. The index gitlink entries equal HEAD's.
- Exactly the five files under `_bmad-output/implementation-artifacts/` are modified. All five are CRLF-only with a final CRLF.
- The parsed `sprint-status.yaml` equals HEAD apart from `last_updated`: `epic-16` and 16.3 are in-progress, 16.1 and 16.2 done.

The patched records were read against each fix instruction. No runtime code changed and no test was added. This build made no Git mutation and no workflow dispatch; its only remote operations were the parent's read-only GitHub and `ls-remote` observations.

## Git-target confirmation — 2026-11-16

**Receipt observation.** `2026-11-16T11:49:32Z` is the parent's first UTC clock read after the answer arrived; it is not an authoring time. jpiquot answered the parent's completion summary with “1 yes;”. That summary's item 1 asked jpiquot to confirm the parent's interpretation of the earlier “Current branch, no commit” answer, recorded in the [M terminal disposition](#m-terminal-disposition--2026-11-16): workspace `/pushall` had merged `docs/story-16-2-not-invoked` into main as `ebd3f76dc0b320d455917c593ff86526b58d0c77` at `2026-11-16T08:48:14Z`, so the interpretation applies that answer to the `main` working tree. The answer **confirms** the interpretation. The trailing “;” supplied nothing further. Item 2 was informational, and nothing else is inferred.

**Scope.** The confirmation authorizes no staging, commit or push. The durability condition in the M terminal disposition is unchanged: the first commit carrying these records that is reachable on `origin` closes it. If no such commit is reachable by `2026-11-16T17:27:10Z`, jpiquot as Release Owner reports missing repository durability as the remaining blocker in Claude Code session `f1c496f3-d5c9-4e70-9e18-2c6ba8cc8d69`.

**Observation at that read.** Local `main` and HEAD are `ebd3f76d…`, with exactly the five record files modified and uncommitted. Remote-tracking `origin/main` is `9ea805dc7bbdf048b282e66e94115add295099a3`, two commits later: `a03ee98a…` “fix: align dependency seals and finite mutation inventory” and `9ea805dc…` “docs: activate repaired dependency source seal”. Neither touches the five record files, and neither moves the EventStore or Builds gitlink. Any later commit of these records must integrate with that newer main before it can reach `origin`; this build performs none of that.

**Git target: confirmed (main working tree) · Durability: open until a commit reaches `origin` · Story 16.3 in-progress · G-3 open**

## Candidate-identity planning adoption — 2026-11-16

**Actual answer:** “approve”, in response to the complete [2026-11-16 sprint change proposal](../planning-artifacts/sprint-change-proposal-2026-11-16.md) and explicit three-capacity approval request. **Signer:** Jérôme Piquot (`jpiquot`/Administrator), separately as Product Owner, Architect and Release Owner. The [owning planning receipt](../planning-artifacts/sprint-change-proposal-2026-11-16.md#planning-approval-receipt--2026-11-16) records the decisions and their scope. The first post-answer UTC clock read was `2026-11-16T12:23:13Z`; this is the receipt observation, not a supplied authoring timestamp. No migration or operational permission is inferred from this planning answer.

Option A and §§3.1–3.5 are now adopted prospectively: keep exact tested FrontComposer/EventStore/Builds/catalog approval, store owning capture and decisions on a separately authorized same-repository record ref, and complete actual readiness before qualification starts. T is a future candidate, Q/S/F its durable record commits; none is selected here. The EventStore/Builds/catalog triple alone and a material-content/documentation-descendant exemption are unadopted alternatives. Human Release Owner review compares live main/dispatch/checkout T, committed selections and exact evidence/receipts alongside unchanged automated release controls. No G-3 receipt parser or new workflow input is claimed.

The two causes remain separate. C and R were superseded by their own record lineage. M's capture never ran because candidate-specific admission/schedule, the complete writer roster/dated acknowledgments and record-ref Git authority were never supplied; its deadline expired while M was still main. The new rule addresses storage and readiness separately. All historical evidence, accepted C engineering capture/D1 exception, validity assessments, limitations and deadlines remain intact. C's received migration statements stay nonoperative; R/M receive no capture, receipt or review. The M procedure stays ended, was never executed and must not be executed.

### Prospective acceptance under the approved rule

**Given** an actual approved planning rule but missing execution prerequisites, **when** readiness is assessed, **then** keep 16.3 in-progress and G-3 open and do not start qualification. **Given** all preflight rows are durably satisfied, a separately authorized schedule may start one future attempt. **Given** T's complete timely capture and three actual approvals, **when** Q/S/F are durably reachable within the window and final main/time rechecks pass, **then** human governance may recognize G-3 only for exact T. Every record stays off main through T consumption. Any failure, rejection, expiry or drift preserves history, keeps current-release G-3 open and routes to the named bounded disposition. Story 16.2 retains its done/not-invoked state and conditional reopening rule.

The approved preflight includes the exact candidate/push sequence, actual schedule, main-writer/automation scope and dated acknowledgments (including `/pushall`), authorized durable storage/Git actions, capture/decision capacity and a supplied working calendar. Retain the at-most-24-hour qualification window, two-hour durable-capture cadence from last required same-attempt success, overall Quality and independent exact push CI/Quality, three durable receipts/final review by window end, four-working-hour disposition, hold termination and retention under the proposal. A stable main, green run or earlier owner attestation is insufficient. Source execution still proves no package-mode runtime compatibility.

**Closure and next action:** the candidate-identity correct-course decision action closes with the actual planning approval. The approved documentation adoption is applied. jpiquot as Release Owner with Architect owns completing §3.2 preflight with the actual workspace automation and repository/Git owners before any separately authorized future schedule. No candidate, ref name, schedule, calendar, acknowledgment or operational authorization is supplied by this answer. Missing prerequisites remain blocking, with no new due date assigned here. Epic 17's final Product readiness continues waiting on G-3; G-8's halt and all separate gates remain independent.

**16.1 done; 16.2 done as not invoked; 16.3 and Epic 16 in-progress; G-3 open.** This planning adoption is recorded in the main working tree under the already confirmed no-commit scope. It performs no staging, commit, push, branch, workflow dispatch, qualification, coordination contact or automation suspension. Prior durability conditions are not renewed or claimed satisfied by this record.

## Tasks & Acceptance — approved planning adoption

- [x] Record the actual three-capacity planning approval in the 2026-11-16 proposal, preserving its prepared text and limiting approval to planning.
- [x] Append the approved P1–P4 resolutions to PRD, architecture, epics and this owning spec without editing frozen intent or any Story 16.1 bytes.
- [x] Append P5 cross-references to both 2026-11-15 proposals and the ended M procedure; refresh Epic 16 context from the adopted sources.
- [x] Apply P6 ledger/sprint tracking and E11R-AI-1 clarification, keeping E11R-AI-1 open, E12R-AI-2 in-progress and all epic/story semantic states unchanged.
- [x] Preserve the P7 runtime/release/dependency/gitlink boundaries; no operational execution or new qualification occurs.

## File List

Approved planning adoption — 2026-11-16; these are this round's changed paths.

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-16.md`
- `_bmad-output/planning-artifacts/prd.md`
- `_bmad-output/planning-artifacts/architecture.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md`
- `_bmad-output/implementation-artifacts/epic-16-lineage-procedure-2026-11-15.md`
- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

## Verification — approved planning adoption

The owning-story command `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0` passed with “Story artifact validation passed.” The global command `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py` and `git diff --check` also passed. The new File List heading was corrected to the validator's exact recognized form after the first run reported the new proposal as unlisted.

Disposable read-only assertions under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` passed: exact 11-path adoption scope under `_bmad-output/`; nine tracked Markdown files append-only from HEAD; prepared proposal preserved except approved status/decision plus append; all historical sprint comments retained; parsed YAML differs only in `last_updated` and approved E11R-AI-1 action wording, with every status and E12R-AI-2 action unchanged. The tuple-freeze addition was moved after its final historical paragraph when the first prefix check detected its placement. An initial scratch-checker assumption that every YAML action has an `id` was corrected without changing repository data.

Story 16.1 and 16.2 remain byte-identical to HEAD; the Story 16.1 SHA-256 is `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`. This spec's entire HEAD prefix, including frozen intent and frontmatter workflow fields, is preserved. CRLF/final newlines/new-text whitespace and all 18 added relative links (6 unique target/anchor pairs) pass. HEAD/main remain `f26304dabf7153c7075fa8c4a82dc8fc537c5ed0`, index/nine gitlinks are unchanged and nothing is staged. No runtime test, remote read, contact, Git mutation, workflow dispatch or qualification occurred. **Planning adoption complete; execution prerequisites remain open; Story 16.3 in-progress; G-3 open.**


## Delegated prospective preflight — 2026-11-16

**Instruction and scope.** The user answered the request for execution details with “supply them yourself”. The first post-answer clock observation was `2026-11-16T14:02:23Z`, not a supplied authoring timestamp. This authorizes selecting and preparing concrete choices here. It supplies no individual writer acknowledgment, future availability confirmation, migration decision or new publication permission. Preserve the previously confirmed no-staging/no-commit/no-push execution scope. No further generic request for the user to design this preflight is needed.

This section is the concrete prospective handoff under the [approved §§3.2–3.5](../planning-artifacts/sprint-change-proposal-2026-11-16.md#32-proposed-readiness-gate-before-qualification). **Readiness: no-go.** The choices below are selected for preparation; appointments, holds and Git operations are not represented as performed. Nothing schedules a background job or starts qualification. Missing real-world receipts remain explicit rather than being signed by an agent on another owner's behalf.

### Candidate and push sequence

Local HEAD/main and local `origin/main` were observed at `bc9dbedc6141b750ca92f01b40ac840dc2066704`, with a clean checkout. This is local remote-tracking evidence, not a fresh live-main read. The already-pushed revision's runs cannot satisfy prospective admission.

Choose the following exact **planned, uncreated** commit as T: `ffb75718c02dafd9b7f0ae777e937d34893962da`. Its object ID was calculated with `git hash-object -t commit --stdin`, without `-w`; no commit object, ref or signature was created. This is a reproducible planned identity, not an existing or qualified candidate. Its complete UTF-8 commit payload, with LF lines and a final LF, is:

```text
tree c8aa041081065627e8320bb7dfd99f5950b92a10
parent bc9dbedc6141b750ca92f01b40ac840dc2066704
author Jérôme Piquot <jpiquot@itaneo.com> 1794837958 +0000
committer Jérôme Piquot <jpiquot@itaneo.com> 1794837958 +0000

chore(16.3): prepare prospective runtime qualification
```

The author/committer fields use the repository's current configured identity and a fixed preparation timestamp, `2026-11-16T14:05:58Z`; they are proposed Git metadata, not a human signature or decision. T has the exact tree of the observed base, with no source or dependency change. Its tree selects EventStore `97c02f7bf99a694528a2b15f0b3b0a6e5fb10e9d`, Builds `65fd22f37b9fad64b595841923cdbe050563ab99` and default EventStore `5.1.1`, with no root preset. The committed catalog also retains conditional SDK42/package-mode `4.2.0`; the future capture must evaluate actual contexts. No package-runtime compatibility is asserted.

Proposed order, executable only after all readiness and separate operational authority are established:

1. Verify live main is still the exact base, required source/dependency work is complete and the existing seal protocol needs no new source transition. If prerequisite work changes the tree or main advances, stop this plan; never silently replace T. G-8 and all independent release-blocking work remain unresolved unless their own evidence says otherwise.
2. After actual Git authorization, materialize the exact planned T object; verify its full SHA. Create the record worktree/ref based on T. Because T has the base tree, carry the full new preparation additions in this owning 16.3 spec, Epic 16 context, deferred ledger and sprint records onto that separately authorized record ref, preserving all historical content. Store the actual preflight receipts alongside those additions and make the complete preparation and receipts durably reachable before the window starts. None of these records is integrated into main. Inspect live branch existence, rules and triggers before any push.
3. Immediately before the named start, verify coverage, appointments, storage and live main again. Run the required successful `python3 references/Hexalith.Builds/check.py` before each proposed push, without weakening any gate. Immediately after window start, perform one ordinary fast-forward push of T to main; no seal transition is presently proposed. Immediately after that push and before admitting either qualifying run, verify live `refs/heads/main` equals planned T; stop on mismatch. If a seal transition is required, stop and prepare its exact sequence separately.
4. Admit only the first push-event runs of the committed `.github/workflows/ci.yml` and `.github/workflows/quality.yml` at exact T, for `refs/heads/main`, full candidate SHA T and attempt 1, created by that future push. Verify each run's workflow identity against those exact committed paths at T alongside its push event, main ref, full SHA and attempt. No old-base run, manual dispatch, rerun, failed-run replacement or C/R/M result qualifies. Record actual run IDs and primary attempt at execution; none exists for planned T now. Require successful overall CI/Quality and every required same-attempt step. Duplicate/ambiguous success or a failed/mismatched run ends the attempt.
5. Append capture Q in owning Story 16.1 on the record ref; validate, commit, push and verify immutable reachability by `min(last required Quality-step success + 2 hours, window end)`. Obtain the three actual role decisions citing Q and T; store S and actual final review F on that ref by window end. Resolve immutable links after each commit, then recheck live main/UTC after F. Prohibit merge, cherry-pick, auto-sync and any other integration of preflight/Q/S/F records onto main until T consumption, even if the qualification hold has ended. Keep those records durably reachable in this same repository afterwards until a separately authorized preservation disposition verifies another durable same-repository reference to every record. No publication follows this preparation.

**Terminal handling.** On any failure, rejection, expiry, drift, unmet cutoff or record-storage failure, preserve all accumulated records, keep G-3 open and end the hold. Record the actual terminal blocker and next owner action in the owning 16.3 spec and its context/ledger/sprint records on the authorized record ref when reachable, and obtain the named jpiquot Release Owner/Architect disposition by the already selected incident due rule below. If record storage is unavailable, preserve locally available records and any still-reachable committed records, report the storage and durability blocker in the existing decision channel, and do not claim unreachable storage is durable. No automatic retry, renewed deadline or substitute evidence follows.

### Selected operating choices

| Item | Concrete selection | Actual readiness limitation |
| --- | --- | --- |
| Qualification window | `2026-11-17T08:00:00Z`–`2026-11-17T16:00:00Z` (09:00–17:00 Europe/Paris), eight hours; complete durable preflight by 07:45Z. | Prospective chosen slot; no timer, workflow, booking or confirmed attendance. If readiness is incomplete at 07:45Z, no attempt starts and this slot lapses without renewal. |
| Capture capacity | Codex prepares records and validation; jpiquot as FrontComposer maintainer with Architect involvement reviews capture, with a 09:30Z review target and continuous deadline monitoring. | Availability and actual delegation for runtime/Git execution must be established. The two-hour deadline overrides the appointment; if the result is late or a reviewer unavailable, stop. |
| Role appointments | jpiquot separately as EventStore maintainer at 10:30Z, FrontComposer maintainer at 10:45Z and Release Owner at 11:00Z; final review target 11:30Z. | Previously confirmed roles are reused, but these are proposed appointments, not accepted calendar events or decisions. Evidence must exist first; absent capacity is no-go. All durable decisions/review remain capped at 16:00Z. |
| Record ref | `refs/heads/docs/epic-16-ffb75718-records` in `origin`, repository `Hexalith/Hexalith.FrontComposer`. | Exact chosen new name; local/remote collision and repository-rule checks still required. It has not been created or pushed. Do not reuse M's ref. |
| Persistent isolated worktree | `/home/administrator/projects/hexalith/.frontcomposer-records/epic-16-ffb75718`, based on T. | Chosen persistent destination, outside disposable TMPDIR; creation is unperformed. Disposable tests still use `run-with-temp`. |
| Requested Git scope | Create planned T and the named isolated record ref/worktree; commit only owning 16.1/16.3, Epic 16 context, deferred ledger and sprint records; ordinary fast-forward pushes of exact T/main and that record ref only. Proposed operator/retention owner: jpiquot; Codex may execute only under actual scoped authority. | This is the concrete authorization scope prepared for the repository/Git owner, not a self-issued permission receipt. No force push, merge, deletion, dependency/gitlink update, workflow edit or publication is included. Existing no-commit scope remains in force. |
| Retention | jpiquot owns retaining preflight/Q/S/F in this same repository through and after consumption; no deletion until a separately authorized preservation disposition verifies another durable reference to every record. | Proposed responsibility; repository ownership/remit and accepted retention responsibility must be real, not inferred from migration roles. |
| Decision channel | This current owner conversation, Codex thread `01a1e495-7a17-7bf1-992e-92a6b748a87e`, with durable records in the owning 16.3 spec on the chosen ref. | This identifies an existing actual channel; it sends no message to another session. |
| Working calendar | Choose Monday–Friday 09:00–17:00, Europe/Paris, for this future attempt only; incident owner jpiquot as Release Owner with Architect involvement. | Agent-selected prospective calendar under the user's delegation, not a claim about historical working hours or personal availability. |
| Incident due rule | Add four working hours on that calendar from the observed incident, capped at `2026-11-17T16:00:00Z`. Example: incident at 09:00Z is due 13:00Z; incident at 14:00Z is capped at 16:00Z. | At execution, record actual observation/due times. This supplies no retrospective due time or extension of C/R/M deadlines. |

### Writer coverage findings and hold text

The session, worktree, process and local-index observations below belong only to this turn, between the parent's UTC reads `2026-11-16T14:02:23Z` and `2026-11-16T14:05:10Z`. Sources were `mcp__codex_tui.list_threads` with limit 50 (truncated), `list_worktrees` (empty), `read_thread` for the cicd session with `turnLimit=1` (empty/truncated), a `/proc` cwd/comm scan, a read-only `state_5.sqlite` threads id/cwd join, and `git worktree list`. These observed sources supply no acknowledgments and establish no complete writer inventory. Execution requires a fresh, complete inventory of actual FrontComposer-main writers, hosts, sessions/children, processes, humans, apps, scheduled jobs, synchronization and `/pushall`, with actual scope assessments and dated owner acknowledgments before start.

Within that observation interval, the active-session listing returned 50 entries and was truncated; it reported no unavailable hosts/sources, which does not establish completeness. The read-only local session-index query linked these active leads to workspaces:

| Session lead | Workspace | Coverage treatment |
| --- | --- | --- |
| `01a1e495-7a17-7bf1-992e-92a6b748a87e` | frontcomposer | This executor; no main mutation will be performed by this preparation. |
| `01a1e43d-5f43-71a2-a044-14fd8b078f48` | eventstore | Potential workspace/sync influence; no direct FrontComposer-main scope proven. |
| `01a1e499-eeb0-7623-abd9-c16f714fd0e9` | folders | Same scope check required. |
| `01a1e450-5cc6-7b93-94da-9121cc1354c0` | conversations | Same scope check required. |
| `01a1e494-3a92-70f3-8aec-5092e973ca15` | cicd | Potential workflow/sync influence; no hold acknowledgment available from the attempted read. |
| `01a1e4a4-fd45-7bb3-b27b-a21258575ecf` | platform | Potential workspace/sync influence. |
| `01a1e033-c7ef-77f2-9d9c-8549e82613ba` | tenants | Potential workspace/sync influence. |
| `01a1e491-a25f-7533-81a3-642d8f17cf37` | mcpcli | Potential workspace/sync influence. |
| `01a1e433-7040-7171-8290-a83e9b971e68` | agents | Potential workspace/sync influence. |
| `01a1e3e8-b603-76b1-bc31-0061c1ba8da6` | parties | Potential workspace/sync influence. |
| `01a1e4a5-b0fb-7a62-b6e3-49e901e6cebe` | projects | Potential workspace/sync influence. |
| `01a1e4a4-8ffa-76d3-b88b-f2286af166f2` | memories | Potential workspace/sync influence. |

Sibling location alone excludes no session with workspace-wide `/pushall` authority. Assess that scope before excluding it. The local index also contains older unarchived FrontComposer tasks; unarchived does not mean active. The managed-worktree list was empty; local `git worktree list` showed only this main checkout. Local process observation found `lazygit`, `micro`, `codex`, shells and editor/runtime processes with this repository as cwd. They are potential human/editor write surfaces, not proof of writes or owner acknowledgments. Other hosts, human sessions, scheduled jobs and installed apps remain outside this observation's proven coverage.

Tracked workflow inspection at the base shows CI, Quality, Commitlint and CodeQL automatic push triggers restricted to main; Release is manual, its publisher reusable, and Release Evidence push is main plus its recovery-file path. An ordinary push of the proposed record branch therefore does not select these automatic qualification/release triggers. This does not replace live repository-rule/app checks; never open a record-ref PR or manually dispatch from it. Flaky/quarantine governance has write permissions but its checked-in apply paths push proposal branches and open PRs rather than directly updating main. PR merge actors and workspace `/pushall` remain potential main writers; do not suspend unrelated sibling work merely because it exists.

Choose a prospective hold interval `2026-11-17T07:45:00Z`–`2026-11-17T16:00:00Z`, ending earlier at final/terminal disposition, urgent fix or drift. For each actual writer, obtain a dated, retrievable acknowledgment naming its owner, FrontComposer-main write scope, authorized hold action and that interval. Explicitly cover main commits/pushes, dependency/gitlink/catalog/policy changes, PR merges and `/pushall` syncs. The acknowledgment must describe actual owner action; the selected hold text is not evidence that an action occurred. No contact or suspension is performed here.

### Readiness result and acceptance

Prepared choices now replace the need for the user to invent the candidate plan, schedule, ref, locations, appointments or calendar. Remaining no-go evidence is specific: live candidate/integration/rules checks; complete writer/app/job coverage and actual dated acknowledgments; actual reviewer capacity; scoped Git authorization and completed remotely durable record storage. No one is represented as having acknowledged or approved something they did not state. A proposed Git object or appointment cannot satisfy those rows. G-8's independent production halt remains, and source changes needed by any prerequisite invalidate the planned candidate.

Given delegation, when preparing this handoff, then all selectable details above are concrete and reproducible. Given missing actual receipts or an unmaterialized candidate, when assessing execution, then no qualification starts. Given any changed base, required tree change, expired slot or unmet preflight cutoff, then stop without a replacement candidate, automatic retry, deadline renewal or inherited evidence. Given later actual successful capture and decisions, then the approved exact-T/Q/S/F controls still govern. This turn supplies no migration decision; C's three statements remain nonoperative. Story 16.1/16.2 remain done, 16.3/Epic 16 in-progress and G-3 open.

## Tasks & Acceptance — delegated preflight preparation

- [x] Select and record a reproducible planned candidate, exact push ordering, future window, role appointments, record-ref/worktree, retention proposal, decision channel and incident calendar/rule.
- [x] Inspect available session/worktree/process and tracked workflow scope; distinguish observed leads from complete coverage and actual acknowledgments.
- [x] Append preparation pointers to Epic 16 context, deferred work and sprint comments, preserving prior bytes, frozen intent, baseline, story states, code and gitlinks.
- [x] Verify current-round artifact checks, planned commit identity, whitespace, links, CRLF and preservation; record only actual results.

## File List

- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`


### Delegated preflight verification — 2026-11-16

The owning artifact command `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0` passed, as did the global form without `--story`/`--base` and `git diff --check`. That explicit base includes the already committed adoption; separate current-round assertions checked exactly four changed implementation-artifact paths against current HEAD `bc9dbedc6141b750ca92f01b40ac840dc2066704`.

Disposable read-only assertions under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` passed: three full Markdown HEAD prefixes preserved; unchanged frozen/prior spec bytes and Stories 16.1/16.2; Story 16.1 SHA-256 still `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`; sprint YAML unchanged except ordinary `last_updated`; CRLF/final newlines; all three added relative links/anchors resolve; HEAD and staged entries unchanged, with no gitlink changes. Rehashing the documented exact LF commit payload produces planned T `ffb75718c02dafd9b7f0ae777e937d34893962da`; `git cat-file -e` confirms that object does not exist. The pinned local commitlint CLI 21.2.4 accepted the exact candidate message using `node_modules/.bin/commitlint --verbose`, reporting zero problems and zero warnings. No bypass or dependency installation was used.

This verifies preparation and preservation only. No runtime code changed, and no runtime test, new test, Git object/ref mutation, push, workflow dispatch, calendar booking, external coordination message, hold/suspension or human migration decision occurred. Actual execution prerequisites remain no-go; 16.3/Epic 16 remain in-progress and G-3 open.


## Review Triage Log — delegated preflight preparation

All three layers completed on the current preparation delta from HEAD `bc9dbedc6141b750ca92f01b40ac840dc2066704`: Blind Hunter floor 6, six findings; Edge Case Hunter returned no findings; Verification Gap returned no gaps. The preserved historical-baseline diff was also generated, but its 54 files include unrelated already committed delivery; this review assesses the four current preparation paths. No unrelated historical source is attributed to this turn.

| Finding | Verdict and route | Verified evidence |
| --- | --- | --- |
| DP-BH-1 | low; patch | Proposed step 3 checks live main before the push, while step 4 does not explicitly check T before admission. Approved §3.3 requires that post-push comparison. Add it directly to the operational handoff. |
| DP-BH-2 | low; patch | Step 4 names CI/Quality without their committed workflow paths. Approved §3.5 requires full workflow identity; bind to `.github/workflows/ci.yml` and `.github/workflows/quality.yml` at T as well as exact event/ref/SHA/attempt. |
| DP-BH-3 | low; patch | The handoff forbids a main record merge but does not explicitly name cherry-pick/automatic synchronization after hold termination. Approved §3.4 keeps every integration path off main through consumption. State that existing retention rule in full. |
| DP-BH-4 | low; patch | The handoff stops on failure and gives a due rule, but omits its terminal-record action. Approved §3.3 already requires a named bounded disposition, retained records and hold termination. Add those existing actions without inventing a decision. |
| DP-BH-5 | low; patch | Planned T has the base tree, so its worktree omits this new uncommitted handoff. Step 2 must explicitly copy the complete preparation additions plus actual receipts onto the record ref before start, preserving histories and keeping them off main. |
| DP-BH-6 | low; patch | Session/process/worktree leads are transient and the prose has no observation boundary/source commands. Cite this turn's read-only tools and local-index query with the actual clock bounds; require a fresh execution-time inventory. |

All are direct corrections to the deliverable operational handoff, preserving the approved rules and frozen requirements. Grouping after the individual verdicts: admission detail (1/2), record lifecycle (3/4/5), observation provenance (6). No requirement changes, new approval, test framework or unrelated deferred item are needed.


### Delegated preflight review completion

The six retained direct handoff corrections are applied: immediate post-push main/T check, committed workflow identities, complete transfer of this preparation to the record ref, all-path record integration prohibition, terminal record/disposition actions including storage failure, and dated observation sources with fresh execution inventory. Edge Case Hunter returned no findings and Verification Gap found no gaps. Nothing is deferred. The final story state remains in-progress because preparation supplies no actual qualification or operative migration approval; no Git action is performed under the generic workflow's completion instructions.


Final parent verification after the six review corrections passed the owning/global artifact validators and whitespace checks, and the disposable preservation/identity/link assertions passed again with the final in-progress state restored. The parent directly read all six patched provisions. A first disposable phrase assertion expected “Immediately after the push”; the handoff correctly says “Immediately after that push”, so the unnecessary phrase check was removed without changing the document. All four current files remain unstaged and code, HEAD, gitlinks, the frozen block and historical bytes remain unchanged. Preparation is complete; actual qualification and migration acceptance remain unperformed.


## Authorized preflight execution and stop — 2026-11-16

**Actual instruction and receipt.** The requesting user invoked `bmad-build 16.3`, accepted the proposed schedule, appointments, calendar and scoped Git plan, and explicitly authorized read-only readiness/live-main/workflow/rule checks, contacting actual writer/session owners including `/pushall`, preparing the exact candidate and isolated record worktree/ref, validated commits and ordinary pushes limited to that ref and the exact planned candidate/main push. The first UTC read after this instruction was `2026-11-16T14:35:53Z`; it is a receipt observation, not a supplied authoring time. Current decision conversation: Codex thread `01a1e4c9-0aad-7c03-ab11-b6b4dd06c3e8`, superseding the earlier handoff's channel for this execution only. This actual operational authorization supersedes the earlier no-commit restriction only within the stated scope. It is no dated writer hold, availability confirmation, retention acceptance or migration decision.

The user required every prerequisite durably complete before `2026-11-17T07:45:00Z`, qualification only at `08:00:00Z` if ready, and a stop with a concrete blocker/accountable next action if main changed, the cutoff passed or any prerequisite could not be satisfied. All other authorized preparation must finish first. No force push, gitlink/workflow change or production release is authorized. No historical deadline or C/R/M evidence is renewed.

### Candidate preservation and actual Git preparation

The first repository inspection found a clean local main at `eddca353d566554af527064925798ec827e07c36`, one commit ahead of live `origin/main` `bc9dbedc6141b750ca92f01b40ac840dc2066704`. The former, with committer metadata `2026-11-16T14:35:53Z`, already stores all four preflight files described as uncommitted in the prior handoff. This build did not create that commit and cannot identify its actor from metadata. Preserve it; do not reset, rebase, move, push or integrate it. This is local divergence and a sync hazard, not an observed remote-main drift. The read-only `git ls-remote origin refs/heads/main refs/heads/docs/epic-16-ffb75718-records`, fetch without submodule recursion, and GitHub branch API all reported live main still at the planned base. The record ref was absent locally and remotely before creation.

The pinned local `node_modules/.bin/commitlint --verbose` accepted the exact candidate message `chore(16.3): prepare prospective runtime qualification`: zero problems and warnings. A disposable `run-with-temp` job rehashed the documented exact LF payload and verified base tree `c8aa041081065627e8320bb7dfd99f5950b92a10`; `git hash-object -t commit -w --stdin` then materialized exactly T `ffb75718c02dafd9b7f0ae777e937d34893962da`. No other candidate was selected. `git worktree add -b docs/epic-16-ffb75718-records /home/administrator/projects/hexalith/.frontcomposer-records/epic-16-ffb75718 ffb75718c02dafd9b7f0ae777e937d34893962da` created the exact persistent worktree/ref based on T. Both actions had completed by `2026-11-16T14:39:17Z`.

The four complete current files (this spec, Epic 16 context, deferred ledger and sprint status) were copied byte-for-byte from the original checkout to that worktree. Every new record in this execution belongs there. T retains its base tree and EventStore `97c02f7bf99a694528a2b15f0b3b0a6e5fb10e9d`, Builds `65fd22f37b9fad64b595841923cdbe050563ab99`, default EventStore `5.1.1`, conditional package SDK42 `4.2.0`, and absent root preset. No gitlink, dependency, source, workflow or seal transition was made. Main was not changed by this build.

### Mandatory pre-push failure and terminal operator stop

The actual command from the original module root was:

```bash
run-with-temp env PYTHONDONTWRITEBYTECODE=1 python3 references/Hexalith.Builds/check.py
```

It exited **1** with `/home/administrator/projects/hexalith/frontcomposer/.github/hexalith-ci.json: [Errno 2] No such file or directory: '/home/administrator/projects/hexalith/frontcomposer/.github/hexalith-ci.json'`. The first post-failure UTC read was `2026-11-16T14:39:17Z`. This is an observed prerequisite failure before the future cutoff, not a cutoff expiry or failed qualification run.

The same failure was independently reproduced at exact T in a disposable workspace: under `run-with-temp bash -s`, clone the module locally with `--shared --no-checkout`, set its origin to the canonical GitHub repository, detach at T, clone only the root-declared Builds repository locally with `--shared --no-checkout`, detach Builds at its exact pin, then run `PYTHONDONTWRITEBYTECODE=1 python3 references/Hexalith.Builds/check.py` from the temporary module root. Exit **1**: `/tmp/hexalith-job.MZpxJ5Y3/candidate/.github/hexalith-ci.json: [Errno 2] No such file or directory`. The wrapper supplied that private TMPDIR and removed it after completion; no nested submodule was initialized. The second command completed before the subsequent preparation below. Adding the missing CiCd-owned configuration would change the frozen candidate tree and requires separate authority; it is not an authorized repair here.

The baseline requires a successful `python3 references/Hexalith.Builds/check.py` before **every push**, including a record ref. Therefore this build stops qualification and all pushes; no remote record durability can be claimed. Local validated commits remain authorized and will preserve preparation on the selected local ref. No workflow, candidate push, capture Q, role receipt S, final review F, runtime test or release occurred. No background qualification timer is installed. The November 17 slot must not start or automatically retry. Story 16.1/16.2 remain done, 16.3/Epic 16 in-progress and G-3 open; G-8 remains independent.

**Accountable next action.** At `2026-11-16 14:39 UTC`, CiCd owner/session `01a1e494-3a92-70f3-8aec-5092e973ca15` acknowledged: `.github/hexalith-ci.json` and the delivered Builds perimeter belong to CiCd; the missing-config check blocks every FrontComposer push including record refs. Its current story authorizes only Commons/PolymorphicSerializations and cannot extend their pre-adoption exception to FrontComposer. Its bounded routing is to preserve local candidate/evidence and obtain jpiquot's separately authorized CiCd adoption/compatibility decision, with no leaf edit, check bypass, credential switch or push. The acknowledgment explicitly relies on this build's exact-tree check rather than claiming independent verification.

jpiquot as Release Owner with Architect involvement must record the actual bounded stop/corrective disposition in this current decision conversation, route the exact pin/config mismatch to CiCd ownership, and accept or revise the concrete next action. This build's observed operator stop is not a human migration rejection or owner disposition. Using the accepted Monday–Friday 09:00–17:00 Europe/Paris calendar and the `14:39:17Z` post-failure incident observation, four working hours end at **`2026-11-17T10:39:17Z`** (before the selected `16:00Z` cap). Original historical deadlines and the `07:45Z` preflight cutoff are unchanged. Preserve local records and any already reachable history; report missing new-record durability now in this decision channel. No repair, replacement T, renewed schedule or resumed attempt is implied.

### Actual writer contacts and bounded acknowledgments

The current `list_threads(limit=50)` response is truncated; a request for 200 was refused because the tool accepts only 1–50. Managed worktree attachments were empty. Read-only local session-index joins identified the current execution and prior FrontComposer parent/child lineage, while `/proc` cwd/comm observation identified human/editor shells, `lazygit`, `micro` and Codex. These are leads, not complete coverage. Other hosts, children not disclosed by their owners, humans, installed apps and scheduled jobs remain outside proven coverage. Current-user crontab is absent; visible system timers do not establish absence of other jobs.

The following are **actual received statements**, retrievable by source thread ID. Each is dated 2026-11-16 UTC by its sender, covers only that owner's task/children/jobs, explicitly excludes global/human coverage, and either enforces abstention or reports no actual FrontComposer-main writer scope for `2026-11-17T07:45:00Z`–`16:00:00Z`. The requested hold included main commits/pushes, dependency/gitlink/catalog/policy edits, PR merges and `/pushall`; it ends earlier on terminal disposition, drift or urgent fix. No wider suspension or acknowledgment is inferred.

| Source thread / owner scope | Sender UTC date/time | Actual bounded action and limits |
| --- | --- | --- |
| `01a1e4a5-b0fb-7a62-b6e3-49e901e6cebe`, Projects 6.7 | 14:37:49Z | No FrontComposer writer; sole implementation child notified, builds/Dapr jobs private; parent/child abstain from FrontComposer and `/pushall`. |
| `01a1e4c2-4140-7270-8854-2f7929183cfb`, Folders 1.17 | 14:37:54Z | Diagnostic-only task and two read-only children; no writer/job/host; enforces no FrontComposer or `/pushall` initiation. |
| `01a1e4c8-7606-74c1-9660-752b02872e1a`, AI.Tools | 14:37:49Z | Standalone AI.Tools scope only, no FrontComposer children/jobs/hosts; enforces session abstention, no scheduled sync. |
| `01a1e4a4-8ffa-76d3-b88b-f2286af166f2`, Memories | 14:38:01Z | Writes only Memories; children finished; no FrontComposer writer/job/host or `/pushall`; enforces task exclusion. |
| `01a1e4c6-b44b-7bc2-80b6-716e30875aa2`, retained CI review | 14:37:55Z | Review scope forbids owner Git/remotes, no children/hosts, disposable jobs complete; enforces exclusion in continuations. |
| `01a1e4a4-fd45-7bb3-b27b-a21258575ecf`, EventStore 3.19 | 14:37:53Z | Only EventStore construction; implementation/review children confined there and disposable copies; enforces no FrontComposer or `/pushall`. |
| `01a1e4c6-7f0e-7d43-9083-b0825bb1ec88`, retained evidence review | 14:38:02Z | Three read-only reviewers, isolated tests, Tenants-local evidence; no FrontComposer writer or `/pushall`. |
| `01a1e43d-5f43-71a2-a044-14fd8b078f48`, EventStore 9.2 | 14:37:57Z | Scope forbids Git/remote/dependency writes, children finished; source copies read-only/private; enforces exclusion. |
| `01a1e450-5cc6-7b93-94da-9121cc1354c0`, Conversations | 14:37:58Z | Tooling/tests only, children/jobs finished, no FrontComposer writer/host; enforces task/child exclusion. |
| `01a1e495-7a17-7bf1-992e-92a6b748a87e`, prior FrontComposer parent | 14:38:08Z | Enforces hold on its own workspace actions/children; all children returned and no background job or `/pushall` remains; did not create local eddca353 commit. |
| `01a1e491-a25f-7533-81a3-642d8f17cf37`, McpCli 5.10 | 14:38:43Z | McpCli packet and ChatBot/Memories README scope; no FrontComposer writer/children/jobs/hosts; stops scope expansion for coordination. |
| `01a1e3e8-b603-76b1-bc31-0061c1ba8da6`, Parties | 14:38:58Z | Only Parties publication; sole active child instructed to keep FrontComposer read-only and prohibit `/pushall`; parent enforces that exclusion. |
| `01a1e494-3a92-70f3-8aec-5092e973ca15`, CiCd | 14:39 UTC (minute precision supplied) | No FrontComposer writer/merge/workflow or `/pushall` job; bounded local diagnostic job and prospective observer limited to CiCd/Builds/Commons/PolymorphicSerializations; enforces task/job abstention. |

The Agents task `01a1e433-7040-7171-8290-a83e9b971e68` and older workspace sync/push lead `01a1e038-b2f9-7831-9b9d-be983eedb85e` were also contacted. Their actual acknowledgment is not claimed until received. Current executor/its implementation and review children remain confined to this isolated record worktree; parent performs the separately authorized candidate/ref/local-commit actions, never main or `/pushall`. Human/editor/wider automation controls cannot be signed by an agent.

The GitHub collaborator API returned admin/push accounts `jpiquot` and `tinouit`. `tinouit`'s public profile supplies no name; no identity mapping is invented. A structured GitHub issue contact addressed to these accounts was attempted but **refused with HTTP 403, Resource not accessible by integration**; no issue was created. No credential switching/retry/broader access was used. The current owner was separately asked in this actual conversation for dated human/editor/other-host/app/job/`/pushall` scope and hold action, actual review/decision capacity and retention acceptance. No answer is fabricated. Authorization to contact and accepted appointments do not answer that request.

### Live rules, triggers and readiness assessment

Read-only GitHub checks returned no repository/inherited rulesets, no open PRs, and no effective rules for `docs/epic-16-ffb75718-records`. The plugin's branch-protection read was refused 403; the existing maintainer CLI's read-only fallback returned 404 `Branch not protected`, consistent with branch metadata `protected: false`. The plugin could not access the workflow-list endpoint; CLI read-only fallback lists `.github/workflows/ci.yml` ID `260846665`, Quality ID `312355133`, other tracked workflows and dynamic Copilot/Dependabot entries as active. Repository hooks list was empty. The installation API returned 401 `A JSON web token could not be decoded`, so installed-app inventory is incomplete. Default Actions token permission is read; PR-review approval is allowed. These observations do not establish app/job hold coverage.

The exact committed CI/Quality push triggers admit main only. Commitlint/CodeQL also restrict push to main; Release is manual and its publisher reusable; Release Evidence push is main/path restricted. Flaky/quarantine governance has write permission but inspected apply paths propose branches/PRs, not a direct main update. No record-ref PR or dispatch is created. None of these reads substitutes for writer/app completeness or a successful pre-push gate.

Base `bc9dbedc6141b750ca92f01b40ac840dc2066704` has successful first push attempts: CI `43880864299` and Quality `43880863888`, both created `2026-11-16T13:57:47Z`; Quality last updated `14:25:29Z`. This corrects no historical red outcome. They belong to the old base, are ineligible for planned T and admit no qualification. No T push run exists or is claimed. Source execution proves no package-mode runtime compatibility.

Readiness rows: planning selection and selected schedule/calendar/appointments accepted; exact T and persistent local ref/worktree prepared; scoped Git authorization received; tested live main unchanged at observation; mandatory pre-push gate failed and remote storage unperformed; writer/app/job coverage incomplete; actual human capacity/retention receipts absent; integration readiness cannot be satisfied by changing the exact candidate under this task. **No-go/terminal operator stop before start.** Any future correction needs a separately authorized owner decision and cannot silently substitute a candidate or revive this slot.

## Tasks & Acceptance — authorized preflight stop record

Implementation handoff for the isolated worktree `/home/administrator/projects/hexalith/.frontcomposer-records/epic-16-ffb75718` only; `{project-root}` in this handoff/context resolves to that worktree. Preserve all preceding content, frozen block and existing frontmatter baseline. Do not execute historical procedures or repeat contacts. No Git commit/push, main edit, dependency initialization, source/workflow change or new test is delegated to the implementation agent; the parent owns already authorized local Git actions and validation evidence.

- [ ] Append the concrete failure/no-go, candidate/ref preparation, actual scoped contact evidence, missing durable storage and bounded jpiquot/CiCd next action above to this worktree's `epic-16-context.md` and `deferred-work.md` (one dated human-owned ledger entry, preserving the existing format). Link this owning section with a relative anchor. Preserve every previous byte.
- [ ] Append dated comments to this worktree's `sprint-status.yaml`, updating only its ordinary `last_updated` value if needed. Keep all YAML semantic states: 16.1/16.2 done, 16.3/Epic 16 in-progress, G-3 open, E11R-AI-1 open and E12R-AI-2 in-progress. State the selected slot does not start, local-only storage and named owner next action; no automatic retry or invented owner decision.
- [ ] Keep this spec in-progress with G-3 open and preserve all historical evidence, Story 16.1/16.2 bytes and the four transferred preflight prefixes. Record actual preparation validation; do not claim remote durability or test runtime behavior. Add no new schema, validator, bundle or workflow.

Given the actual authority, when preparing T/ref and local records, then preserve the user's prior work and restrict new content to the four owning artifact paths on the named record worktree. Given the mandatory checker failure at both original root and exact T, when assessing the authorized future start, then stop qualification and all pushes and report storage as local-only. Given only scoped session statements and unconfirmed human/app/job facts, then claim only their actual coverage. Given no T evidence, then obtain no migration decisions and leave G-3 open. Given this operator stop, then request the actual jpiquot Release Owner/Architect disposition by the existing due rule rather than signing it for them.


## Tasks & Acceptance — authorized preflight stop record completion

These completion entries reconcile the latest three-item implementation handoff without changing its preserved original bytes. They complete local stop-record preparation only; Story 16.3 remains in-progress and G-3 remains open.

- [x] Appended the actual prerequisite failure/no-go, candidate/ref preparation, scoped contact evidence, local-only storage and bounded jpiquot/CiCd next action to `_bmad-output/implementation-artifacts/epic-16-context.md` and one human-owned entry in `_bmad-output/implementation-artifacts/deferred-work.md`, each linking the owning stop section.
- [x] Appended dated stop comments to `_bmad-output/implementation-artifacts/sprint-status.yaml` and updated only its ordinary `last_updated` value; all story, epic, gate and action-item semantic states remain unchanged.
- [x] Preserved this spec's in-progress/open frontmatter, frozen block, historical evidence, Stories 16.1/16.2 and all four transferred prefixes; recorded actual local artifact/preservation validation below, with no remote durability or runtime-behavior claim.

## File List

Authorized preflight stop-record implementation — 2026-11-16; only these four owning artifact paths change in this handoff.

- `_bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md`
- `_bmad-output/implementation-artifacts/epic-16-context.md`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`


### Authorized preflight stop-record verification — 2026-11-16

Local implementation checks completed by `2026-11-16T14:50:22Z` in `/home/administrator/projects/hexalith/.frontcomposer-records/epic-16-ffb75718`. The complete spec and its frontmatter context were read before editing. The original handoff checkboxes remain unchanged history; the completion list above reconciles their local preparation result.

- `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 eng/validate-story-artifacts.py --story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0` — passed: “Story artifact validation passed.” That existing explicit base includes already committed adoption work; separate current-round assertions bound this handoff to the four owning artifact paths.
- The global artifact-validator form without `--story`/`--base` and `git diff --check` — passed.
- Disposable assertions under `run-with-temp env PYTHONDONTWRITEBYTECODE=1 GIT_OPTIONAL_LOCKS=0 python3 -` — passed: the four transferred preflight prefixes remain byte-preserved except the ordinary sprint `last_updated` value; the implementation appends only after the pre-implementation spec/context/ledger content. All parsed sprint YAML data is unchanged after excluding `last_updated`, including 16.1/16.2 done, 16.3/Epic 16 in-progress, E11R-AI-1 open/G-3 and E12R-AI-2 in-progress. Story 16.1/16.2 remain byte-identical to the original checkout, and the Story 16.1 SHA-256 remains `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`.
- HEAD is exact T and the selected record branch remains checked out; the original local main ref remains `eddca353d566554af527064925798ec827e07c36`. Index bytes/staged entries (including gitlinks) are unchanged across these checks, and nothing is staged. Working-tree scope is exactly the four pre-existing modified artifact paths, with no untracked files. All four files are CRLF-only with final newlines and no trailing whitespace in new text. All 4 added relative Markdown links (2 unique targets/anchors) resolve.

The first disposable preservation assertion stopped on an older action item without an `id`; the scratch check was corrected to select identified action items and then passed. No repository validator or test was changed. The parent owns final review, full preservation validation and any separately authorized local commit.

This verifies local stop-record preparation only. The parent-supplied missing-config failure remains the push/qualification blocker and was not rerun here. No runtime test, new test, source/workflow/dependency change, remote call, contact, Git mutation, qualification, owner decision, operative migration approval or remote-record durability is claimed. Story 16.3 remains in-progress and G-3 remains open.


### Workspace sync acknowledgment and hold cancellation — 2026-11-16

A further actual acknowledgment arrived from workspace sync lead `01a1e038-b2f9-7831-9b9d-be983eedb85e`, dated **2026-11-16 14:42 UTC** (minute precision supplied). It owns the Agents sync/release task, reports that the original task is complete, never invoked `/pushall`, and has three completed review children with no pending owned sync/write/build/deploy jobs or running AppHosts. Its separate FrontComposer checkout is `11c8377a858a756c4e4069bf08e62db1f58e1733`; its read of live remote main returned the planned base. It does not own the canonical checkout. Its actual commitment covers only its own thread/children: no FrontComposer-main commits/pushes/PR merges, dependency/gitlink/catalog/policy changes or `/pushall` affecting FrontComposer during the selected interval, ending earlier under the stated conditions. It will not push the local eddca353 record commit to main and recognizes that no push may precede a successful mandatory Builds check. It excludes unrelated threads/siblings and supplies no global lock or human acknowledgment. This supersedes only that contact's earlier pending state; the Agents task `01a1e433-7040-7171-8290-a83e9b971e68` has supplied no actual acknowledgment to this execution.

After the observed prerequisite failure and operator stop, terminal notices were sent successfully to all **15** contacted thread IDs by the post-send UTC read **`2026-11-16T14:45:28Z`**. They cancel this attempt's prospective November 17 hold, preserve existing work and sibling autonomy, prohibit an automatic qualification/retry or unauthorized repair/push, and explicitly retain the separate all-path restriction against moving the eddca353 commit or record-ref content onto main. Tool delivery is not a claimed acknowledgment or action by every recipient. Actual cancellation acknowledgments include Projects (child notified), Folders, Conversations (`14:45:42Z`), EventStore 3.19, Parties, CiCd (`14:46 UTC`) and the workspace sync lead (`14:46:08Z`); each remains bounded to its own scope. No future hold is claimed active. The qualification stop is the user's conditional instruction applied to the observed failure, not an inferred human migration decision or Release Owner/Architect disposition. Retention and record-placement restrictions survive hold cancellation.


## Review Triage Log — authorized preflight stop record

All three independent review layers completed on the four-path delta from `bc9dbedc6141b750ca92f01b40ac840dc2066704`. Blind Hunter used 69,782 bytes / 1,000 = 69.782 kB and floor min(floor(sqrt(69.782) + 1), 10) = 9, returning nine findings. Edge Case Hunter returned one transient-state finding; Verification Gap found no gaps. The preserved frontmatter baseline also generated a 2,317,241-byte historical diff containing unrelated already committed work; this review attributes only the selected current preflight/stop-record delta to this execution. No historical source change is attributed to it.

| Finding | Verdict and route | Verified evidence |
| --- | --- | --- |
| AP-BH-1 | low; patch | The preserved plan names the exact candidate but omits a literal source/destination refspec. Local main contains the separate eddca353 record commit. Append the unexecuted exact refspec and explicitly keep this stopped slot non-executable. |
| AP-BH-2 | low; patch | The exact-T failure is independently real, but the stored account paraphrases its disposable command. Append the actual reproducible shell sequence and complete stderr; no new script, bundle or gate is needed. |
| AP-BH-3 | low; patch | Live-rule observations have source descriptions but no bounded observation interval or literal request list. The actual reads all fit the parent's UTC bounds 14:35:53Z–14:41:08Z. Record those bounds and the API/CLI requests without inventing precise individual times. |
| AP-BH-4 | low; patch | The read-only CLI fallback used the already configured `jpiquot` account reported by the initial `gh auth status`. No profile/environment credential change occurred. State that actual boundary explicitly; do not equate the plugin's integration credentials with the CLI. |
| AP-BH-5 | low; patch | Original Folders `01a1e499-eeb0-7623-abd9-c16f714fd0e9` and Tenants `01a1e033-c7ef-77f2-9d9c-8549e82613ba` leads were not individually assessed in this execution. Differently identified later contacts supply no mapping. Name them as unassessed, leaving coverage incomplete. |
| AP-BH-6 | false; rejected | Each receipt is already identified by actual source thread ID, supplied UTC timestamp/precision and scope, and preserved in this execution conversation; those identify retrievable source statements through thread reads/pagination. The active rule requires dated retrievable evidence, not a new coordinate schema or full verbatim transcript. No complete coverage or operative approval is claimed from the summaries. |
| AP-BH-7 | low; patch | Projects/Folders/EventStore 3.19/Parties cancellation messages supplied no authoring time. Add that fact and the parent's post-receipt observation bound 14:49:53Z; do not copy supplied times from other owners or equate tool delivery with acknowledgment. |
| AP-BH-8 | low; patch | The checker mismatch has an explicit owner/action, while other still-missing readiness rows are summarized. Append the accountable routing for writer/human/app/job coverage, capacity and retention; none clears the stopped slot or creates a new due date. |
| AP-BH-9 | low; patch | Numeric old-base run IDs are correct; direct primary-attempt links make their stated scope immediately reviewable. Add those links while retaining old-base ineligibility for T. |
| AP-ECH-1 | low; rejected (carried historical BH-1 / TD-VG-O1) | carried — `in-review` is the workflow's transient review state. The parent restores `in-progress` before final validation/commit; sprint and G-3 remain unchanged. It supplies no qualification or operative acceptance. |

After individual verdicts, the eight surviving items are direct corrections/additions to this operational record: explicit push source, reproduction, observation/authentication provenance, unassessed leads, cancellation precision, residual-owner routing and primary-attempt links. No frozen requirement, source implementation, approval, deadline, workflow or test changes are needed. Re-engage the same implementation agent once for these patches; keep all preserved historical prefixes intact. No unrelated item is deferred.


### Review corrections to the execution and stop record — 2026-11-16

**Exact intended push source (AP-BH-1).** The unexecuted intended candidate push was `git push origin ffb75718c02dafd9b7f0ae777e937d34893962da:refs/heads/main`. This is non-executable for the stopped November 17 slot. It names exact T as the source, never a branch-name push: local `main` carries the distinct `eddca353d566554af527064925798ec827e07c36` record commit, and neither it nor record-ref content may be pushed or integrated onto main under this plan. This clarification preserves the historical handoff and authorizes no push, replacement candidate or resumed attempt.

**Actual exact-T reproduction (AP-BH-2).** The parent's actual disposable reproduction command was the following. It is recorded as completed historical evidence and was not rerun for this correction.

```bash
run-with-temp bash -s <<'SH'
set -euo pipefail
origin_root=/home/administrator/projects/hexalith/frontcomposer
candidate_root="$TMPDIR/candidate"
git clone --quiet --shared --no-checkout "$origin_root" "$candidate_root"
git -C "$candidate_root" remote set-url origin https://github.com/Hexalith/Hexalith.FrontComposer.git
git -C "$candidate_root" checkout --quiet --detach ffb75718c02dafd9b7f0ae777e937d34893962da
git clone --quiet --shared --no-checkout "$origin_root/references/Hexalith.Builds" "$candidate_root/references/Hexalith.Builds"
git -C "$candidate_root/references/Hexalith.Builds" checkout --quiet --detach 65fd22f37b9fad64b595841923cdbe050563ab99
cd "$candidate_root"
PYTHONDONTWRITEBYTECODE=1 python3 references/Hexalith.Builds/check.py
SH
```

Actual exit: **1**. Complete stderr:

```text
/tmp/hexalith-job.MZpxJ5Y3/candidate/.github/hexalith-ci.json: [Errno 2] No such file or directory: '/tmp/hexalith-job.MZpxJ5Y3/candidate/.github/hexalith-ci.json'
```

The private TMPDIR and its contents were removed when `run-with-temp` returned. This evidence preserves the existing missing-config blocker; no repair, dependency initialization or check bypass follows.

**Live-read provenance and authentication boundary (AP-BH-3/4).** The actual readiness/rule observations above fall within the parent's UTC observation bounds **`2026-11-16T14:35:53Z`–`2026-11-16T14:41:08Z`**. These are batch bounds, not individual request timestamps. Literal read requests were:

```text
GET /repos/Hexalith/Hexalith.FrontComposer/branches/main
GET /repos/Hexalith/Hexalith.FrontComposer/rulesets?includes_parents=true
GET /repos/Hexalith/Hexalith.FrontComposer/branches/main/protection
GET /repos/Hexalith/Hexalith.FrontComposer/actions/workflows
GET /repos/Hexalith/Hexalith.FrontComposer/pulls?state=open&per_page=100
GET /repos/Hexalith/Hexalith.FrontComposer/rules/branches/docs%2Fepic-16-ffb75718-records
GET /repos/Hexalith/Hexalith.FrontComposer/installation
GET /repos/Hexalith/Hexalith.FrontComposer/collaborators
GET /repos/Hexalith/Hexalith.FrontComposer/hooks
GET /repos/Hexalith/Hexalith.FrontComposer/actions/permissions
GET /repos/Hexalith/Hexalith.FrontComposer/actions/permissions/workflow
```

The initial `gh auth status` reported the already configured CLI principal `jpiquot`. No `GH_CONFIG_DIR`, `GH_TOKEN`, `GITHUB_TOKEN` or profile modification, or `setup-git`, occurred. The CLI fallback was a read-only capability fallback with no new scope or write retry. Plugin integration credentials and existing CLI credentials are distinct; this record does not equate them. The previously recorded refusals and incomplete app/job inventory remain limitations.

**Unassessed original leads (AP-BH-5).** The original Folders lead `01a1e499-eeb0-7623-abd9-c16f714fd0e9` and Tenants lead `01a1e033-c7ef-77f2-9d9c-8549e82613ba` remain **unassessed in this execution**. Differently identified contacts establish no supersession, exclusion or acknowledgment for either original lead. Writer coverage remains incomplete.

**Cancellation receipt precision (AP-BH-7).** Projects, Folders, EventStore 3.19 and Parties supplied no authoring time for their cancellation acknowledgments. Those acknowledgments had arrived by the parent's UTC read **`2026-11-16T14:49:53Z`**. That is a receipt-observation bound, separate from authoring time and from the earlier successful tool delivery of terminal notices. Supplied cancellation timestamps for Conversations, CiCd and the workspace sync lead remain unchanged; no timestamp is copied to another owner.

**Accountable routing for the remaining readiness rows (AP-BH-8).** These rows remain missing:

| Missing readiness evidence | Accountable next action |
| --- | --- |
| Complete human/editor/other-host/children and `/pushall` scope, inventory and dated owner actions | jpiquot as workspace automation owner works with the actual writer owners to supply their inventory, scope and actual dated actions. |
| Refused `tinouit` contact and incomplete installed-app/scheduled-job inventory | Repository owner jpiquot routes the contact refusal and app/job inventory to the responsible owners; refusal supplies no acknowledgment or broader credential authority. |
| Actual capture/review and three-capacity decision availability | The maintainer, Architect and Release Owner establish actual capture and decision availability; accepted proposed appointments supply no availability receipt. |
| Accepted repository retention responsibility | Repository owner jpiquot explicitly accepts retention of all owning records under the existing same-repository preservation rule. |

These routes do not reopen the stopped slot, authorize repairs or create deadlines beyond the existing bounded disposition due `2026-11-17T10:39:17Z`. jpiquot as Release Owner with Architect involvement still records that actual disposition and accepts or revises the next action in the current decision conversation. No missing receipt, inventory or decision is supplied by this routing text.

**Old-base run links (AP-BH-9).** [CI 43880864299 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43880864299/attempts/1) and [Quality 43880863888 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43880863888/attempts/1) belong only to old base `bc9dbedc6141b750ca92f01b40ac840dc2066704`. Both are ineligible for exact T `ffb75718c02dafd9b7f0ae777e937d34893962da`; neither supplies T qualification, capture or approval.


### Final parent verification and local-record handoff — 2026-11-16

The parent read all eight retained review corrections after the same implementation agent returned, restored the required final `in-progress` state and verified the current tree by `2026-11-16T14:59:18Z`. No review item is deferred. The direct corrections preserve the original preflight/historical bytes and change no operational permission, deadline, evidence admission or approval.

Passed after the corrections: owning artifact validation with `--story _bmad-output/implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md --base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0`; the global artifact validator; `git diff --check`; disposable four-path/prefix/frozen/history/CRLF/YAML/Story 16.1+16.2 preservation assertions; exact T/base-tree equality; unchanged original clean main at `eddca353d566554af527064925798ec827e07c36`; no staged or gitlink changes at those checks; final in-progress/G-3-open state and original baseline/deadlines. Story 16.1 SHA-256 remains `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`. The added relative-link check passed for 8 raw links / 5 unique target-and-anchor pairs; review corrections add only the two known old-base primary-attempt URLs.

The parent's attempted narrower owning-validator command with `--base bc9dbedc6141b750ca92f01b40ac840dc2066704` exited 1 for seven inherited File List entries with no matching story-owned change/accepted classification (five earlier planning files, the M lineage procedure and the November 16 proposal). The documented adoption-base form above passed. No historical File List, validator or source was changed to hide that result; separate assertions prove the current four-path scope.

Only the separately authorized four-file local record commit follows these checks. Its exact full message must pass the pinned commitlint 21.2.4 CLI before use, and installed pre-commit/commit-msg hooks must run. Existing pinned CLI dependencies are reused in the worktree's ignored `node_modules`, without a package install/version change. The local commit's own Git metadata and this execution conversation identify it after creation, avoiding a self-referential commit ID in the committed document. No push follows: the independently reproduced missing `.github/hexalith-ci.json` still blocks every push. The selected record ref/worktree and all original user work are retained locally; new-record remote durability remains missing.

The actual jpiquot Release Owner/Architect disposition remains outstanding, due `2026-11-17T10:39:17Z`; this execution's authorization, operator stop and local commit do not sign it. The owner must accept or revise the concrete CiCd routing and remaining-readiness actions above. No November 17 qualification starts, no T evidence or three migration decisions are fabricated, and G-3/Story 16.3 remain open/in-progress. No release or automatic retry is scheduled.


## Human stop disposition and accepted next action — 2026-11-16

**Actual answer and capacity.** The requesting user answered **“Record the stop and accept this next action”** to the explicit question addressed to jpiquot as Release Owner with Architect involvement. This records Jérôme Piquot's actual bounded owner disposition in that stated capacity, using the already established authority; it does not infer an owner decision from the earlier execution authorization. The first UTC read after the answer arrived was **`2026-11-16T15:02:34Z`**, a receipt observation, not a supplied authoring time. Decision date: 2026-11-16 UTC. Source: this owner decision conversation, Codex thread `01a1e4c9-0aad-7c03-ab11-b6b4dd06c3e8`, replying to the concrete disposition request following local record commit `264411d10d270ee566359cf1f6753d57223b4a9f`.

**Recorded stop.** The selected November 17 `08:00Z`–`16:00Z` qualification slot remains stopped before start for exact T `ffb75718c02dafd9b7f0ae777e937d34893962da`. Its prospective hold was already cancelled. No automatic retry or restart follows. The concrete blocker remains the mandatory `references/Hexalith.Builds/check.py` exit 1 for missing CiCd-owned `.github/hexalith-ci.json` at the exact pinned Builds/candidate tree. All pushes, including the record ref, remain blocked by the successful-check-before-every-push rule.

**Accepted accountable next action.** jpiquot as Release Owner with Architect involvement accepts routing that exact missing-configuration / pinned-Builds mismatch to CiCd ownership for a **separately authorized adoption or compatibility decision**, while preserving local evidence and keeping all pushes blocked. The actual CiCd ownership acknowledgment and bounds are preserved in the [execution record](#authorized-preflight-execution-and-stop--2026-11-16). This accepts that concrete next action; it does not authorize adoption/configuration/workflow edits, a bypass, credential changes, a replacement candidate, a new schedule or execution of the stopped slot. The accepted routing can be communicated to the already identified CiCd owner without asking that session to change the repository.

**Timing and closure boundary.** The actual owner answer was received before the existing disposition due **`2026-11-17T10:39:17Z`**; the pending-human-answer state in earlier sections is superseded from this receipt onward. That due time and every historical deadline remain preserved. The November 17 `07:45Z` preflight cutoff is not extended. The accepted CiCd action remains to be resolved under separate authority. New preflight records and this receipt remain local-only on `refs/heads/docs/epic-16-ffb75718-records`; remote repository durability remains unfulfilled and is reported here. The existing local record commit is preserved, and a subsequent validated local receipt commit may append this answer without modifying main. No local commit is represented as a remote durable record.

The answer supplies no migration approve/reject decision, qualification evidence, final G-3 review, writer availability/hold acknowledgment or general retention acceptance. Complete human/app/job coverage and capacity/retention receipts remain missing; [their accountable routing](#review-corrections-to-the-execution-and-stop-record--2026-11-16) remains unchanged. Stories 16.1/16.2 stay done, Story 16.3 and Epic 16 stay in-progress, G-3/E11R-AI-1 stay open, and E12R-AI-2 stays in-progress. C/R/M history and C's nonoperative statements are untouched; G-8 remains independent. No production release follows.


### Stop receipt routing and verification — 2026-11-16

The accepted routing was sent to the already identified CiCd owner thread `01a1e494-3a92-70f3-8aec-5092e973ca15`; the tool accepted delivery before the parent's next UTC read **`2026-11-16T15:04:32Z`**. The notice quotes the actual owner answer, names the exact mismatch/candidate and local record ref, and explicitly requests no new implementation task, edit, bypass, push or adoption. Delivery is not represented as the recipient's acknowledgment or completion of the separate CiCd decision.

The owning artifact validator with documented adoption base `f26304dabf7153c7075fa8c4a82dc8fc537c5ed0`, the global validator and `git diff --check` passed for this receipt. Disposable `run-with-temp` checks passed for four-path scope, append-only prior/frozen bytes (apart from ordinary sprint `last_updated`), CRLF/final newlines, unchanged YAML semantic data/statuses, unchanged Stories 16.1/16.2 and the new receipt's relative-link target headings. Story 16.1 SHA-256 remains `f2754e0e24d56913ea757054f54ea77604eeba3000317abfe1040c73a61b651c`. No runtime behavior changed, so no runtime test or known-failing pre-push check was repeated. The exact receipt-commit message is validated with pinned commitlint before committing, with installed hooks enabled; its resulting local Git metadata identifies that storage commit without a self-reference. Qualification and all pushes remain stopped, and remote durability remains missing.

## Current main and the stopped preflight — 2026-11-22

**Disposition.** Story 16.3 and Epic 16 remain in-progress. G-3 remains open. The signed November 16 stop ended the November 17 slot for `ffb75718c02dafd9b7f0ae777e937d34893962da` before qualification. C's three statements remain nonoperative. The November 21 planning decision retains the existing G-3 terms. [Owner's stop receipt](#human-stop-disposition-and-accepted-next-action--2026-11-16) · [November 21 proposal](../planning-artifacts/sprint-change-proposal-2026-11-21.md)

**What reached origin.** A read of the remote heads completed by `2026-11-22T08:50:20Z`. Origin advertised main at `f73a8f25d5e7b6ddd5f2cfd9f76f08ef0b75d290` and returned no head for `docs/epic-16-ffb75718-records`. Both the preflight record `264411d10d270ee566359cf1f6753d57223b4a9f` and the owner's stop receipt `c52a1208f87f686dbbb762a831013b3096b100d9` are reachable through the second parent of merge `66250f89230e1b261b51ce873c83d96176a9d325`. This resolves their earlier local-only status. The merge put the records on main after the slot stopped and before any recorded consumption of T. The record contains no separately authorized preservation disposition accepting main as a replacement for the selected ref. That retention obligation stays open. [Merge and parents](https://github.com/Hexalith/Hexalith.FrontComposer/commit/66250f89230e1b261b51ce873c83d96176a9d325) · [Adopted record-retention rule](../planning-artifacts/sprint-change-proposal-2026-11-16.md#34-record-lineage-and-retention)

**Which checker rule applies.** T pinned AI.Tools `7ef8166e`, whose guidance required the Builds checker before every push. The recorded exit 1 for the missing `.github/hexalith-ci.json` stands for that preflight. Main now pins AI.Tools `87a738ce`, which requires the checker for modules that have committed the CiCd configuration. The file is absent from the observed main tree, so FrontComposer's existing CI gates apply there. CiCd places FrontComposer after EventStore in its adoption order. Its November 20 workflow-pin proposal treats registration as future work. Neither record contains a CiCd adoption or compatibility decision for the stopped tuple. [Current checker policy](https://github.com/Hexalith/Hexalith.AI.Tools/blob/87a738ceeba8975c0b087bc5ecee7dfbc14bf73c/hexalith-llm-instructions.md#cicd) · [CiCd adoption order](https://github.com/Hexalith/Hexalith.CiCd/blob/b7102103ebbe2d3644f9c080834306d890ccbc3d/docs/ordered-module-adoption.md) · [CiCd workflow-pin proposal](https://github.com/Hexalith/Hexalith.CiCd/blob/b7102103ebbe2d3644f9c080834306d890ccbc3d/docs/proposals/frontcomposer-workflow-pins-20261120/README.md)

**Remaining decision.** The observed main tree pins EventStore `147159db98074b7769645ccc4aaaf0099df03326` and Builds `d532b1e9b1f595e2c9867bfa9780dccd78c9a2d9`. Its pinned catalog defaults to `5.4.0`. These Git inputs establish drift from T. The read-only command `python3 eng/dependency_graph.py validate --commit f73a8f25d5e7b6ddd5f2cfd9f76f08ef0b75d290` exited 1 with `github.com/hexalith/hexalith.platform@0aacf13244fa6b1d9e08d627149d72cf12229167: consumer source projection owner pin changed`. The Architect and Release Owner must decide how to recover this base before selecting it for a future preflight. [Approved recovery requirement](../planning-artifacts/sprint-change-proposal-2026-11-21.md#1-admit-a-platform-revision-change)

jpiquot, as Release Owner with Architect involvement, owns follow-through with CiCd on the accepted compatibility or adoption route and with the repository owner on retention. A future qualification needs separate candidate and window authorization, writer coverage, available reviewers and approvers, and durable off-main records. G-3 then requires that candidate's timely evidence, separate EventStore-maintainer, FrontComposer-maintainer and Release Owner decisions, and final Release Owner comparison. The owning records contain no such capture, decisions or comparison for this main. [Exact-candidate requirements](../planning-artifacts/prd.md#approved-g-3-candidate-identity-and-record-storage--2026-11-16)

**Verification.** The global story-artifact validator and `git diff --check` passed. The owning validator with `--base f26304dabf7153c7075fa8c4a82dc8fc537c5ed0` reported 135 additional paths from the intervening main history. It passed with those exact paths classified using `--unrelated` and `--reason`. They were derived from the adoption-base-to-main diff after removing the adoption-base-to-stop-receipt paths, and verified as inherited from main. Checks also confirmed the four edited records, their historical prefixes, the Story 16.1 and 16.2 file hashes, relative links and unchanged tracker data apart from `last_updated`.

## Dependency restoration on November 22

FrontComposer restored seven direct dependency pins at `a79cc810fafbebcb392a413d24b99ffa70326d58`. The dependency graph validates all 76 edges at that commit and at its documentation-only successor `8199bcf7def7d9ddb39b1704429ba611d59fc08e`. The parent `4cecae4e44a98ccd379c0327f258217c010046da` fails its own check. With that parent as the base and the restoration as the candidate, both push and PR comparisons exit 1 with `consumer source projection owner pin changed` for Platform `0f6468edbb8efd4e47fb272286cb1bfd5baa3255`. Hosted CI failed at “Collect exact graph diff and affected-module proof.” The comparison from the restoration to the documentation successor passes with zero dependency changes and zero affected modules. [Restoration CI](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/45018052464/attempts/1) · [Verification table](../planning-artifacts/sprint-change-proposal-2026-11-22.md#1-what-changed-on-main)

The Architect and Release Owner own the recovery decision. Record the actual event base and candidate, the disposition of the failed restoration, and the affected-module proof required by the approved recovery rule. If the original transition cannot qualify, retain its failure and assess any later candidate on its own evidence. The November 17 slot ended before qualification. A future attempt needs its own authorized window, exact runtime evidence and named owner decisions. Story 16.3 remains in progress and G-3 is open. [Recovery rule](../planning-artifacts/sprint-change-proposal-2026-11-21.md#1-admit-a-platform-revision-change) · [G-3 requirements](../planning-artifacts/prd.md#approved-g-3-candidate-identity-and-record-storage--2026-11-16)
