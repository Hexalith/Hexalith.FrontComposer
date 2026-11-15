---
title: 'Epic 16: Decide migration approval separately from latest-main release readiness'
created: '2026-11-15'
status: 'approved'
requested_by: 'jpiquot as Release Owner with Architect'
request_recorded_at: '2026-11-15T21:49:02Z'
decision_due: '2026-11-16T17:27:10Z'
g_3: 'open'
---

# Proposed planning amendment — Epic 16 approval lineage

## Named request and scope

Jérôme Piquot (`jpiquot`/Administrator) explicitly confirmed all five requested roles and replied “2 do recommended” to the recommendation to **request** this amendment. The selected Release Owner/Architect disposition was recorded at `2026-11-15T21:49:02Z`, before incident due `2026-11-15T21:49:35Z`. This fulfills the incident's request-disposition step; it does not approve the amendment text below. Product Owner, Architect and Release Owner for this decision are each explicitly confirmed as jpiquot.

This proposal implements no runtime, release-selector, dependency, workflow or policy-code change. All work stays under `_bmad-output/`. It preserves every recorded tuple, engineering acceptance and D1 historical exception. The requesting user's “3 approve” is preserved for each migration role in Story 16.3, with prerequisite validity disclosed.

## Exact evidence and conflict

| Identity | Accepted tested candidate C | Capture/main commit R |
|---|---|---|
| FrontComposer | `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc` | `695d23149c56a6104e81d23a54116b4fb914df95` |
| EventStore source | `fdb59895975e86a1e234325a01237f259cdbd53b` | Same committed selection |
| Builds | `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` | Same committed selection |
| Catalog default EventStore | `5.1.0` | `5.1.0` |
| Quality evidence | [43755163930 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1), accepted 16.1 capture | [43759663406 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1), now successful, no new 16.1 capture or owner decisions |

R descends through `902fe13fc411c9eb7cd3786180761a904a3b47fb` from C; only nine `_bmad-output/` paths differ. Documentation ancestry establishes record provenance, not runtime identity equality. Any future commit of this proposal or receipts is likewise distinct. No run was dispatched here.

The [existing qualification proposal](sprint-change-proposal-2026-11-15-epic-16-tuple-freeze.md) requires exact-tuple approval and a coordinated hold, allows an explicitly approved planning resolution for lineage, and grants no documentation exemption or release-selector change. The active release workflow independently requires the exact current main SHA, its successful push CI and Quality, and another current-main check before protected credentials. Merely choosing C for release would fail these unchanged controls. A successful R run cannot inherit C's capture or approvals.

## Proposed amendment text — requires explicit decision

1. **Record three distinct facts.** The story must distinguish (a) the exact tuple an owner actually judged, (b) the commit that stores the record or receipt, and (c) the current release candidate. A record descendant never changes an earlier tuple's identity. Preserve received human decisions with their actual date and role; distinguish them from an operative approval receipt eligible for G-3.
2. **Preserve the named C decisions without declaring readiness.** Record jpiquot's three actual approve statements only against C and its accepted evidence and disclosed limitations. Because the required coordination and latest-main lineage prerequisites were unresolved when received, those statements remain ineligible for G-3. Do not backdate, automatically activate them later, or transfer them to R or another candidate.
3. **Keep current-main release gates exact.** G-3 can close for a proposed release candidate only when that candidate independently has the exact four-part 16.1 capture, passing required same-attempt live lanes and passing overall Quality, and all three valid owner receipts for that candidate within an authorized qualification/decision window. Record/receipt commits may serve as provenance references; they have no documentation exemption from this candidate comparison. Keep the production workflow's current-main, successful push CI/Quality and protected-credential recheck intact.
4. **Close the coordination gap prospectively.** jpiquot as Release Owner, with Architect involvement, must identify the workspace automation owner and all active sessions, record actual owner-authorized coordination and its effective start/end, and arrange any further qualification under the existing rules. A local ListAgents result, stable main, all-five-role confirmation or green run does not establish repository-wide coordination. No earlier global freeze is claimed. Do not contact others or suspend automation without its owner's explicit authorization.
5. **End the moving-target loop with a named terminal disposition.** By the unchanged outer deadline `2026-11-16T17:27:10Z`, jpiquot records either fully satisfied current-candidate readiness or the concrete remaining blocker and next owner action. A further documentation commit or independent green run alone does not schedule an automatic recapture/retry loop. If a valid attempt cannot finish within that deadline, end it and keep G-3 open; any fresh attempt needs an explicit named schedule of at most 24 hours and its own qualifying evidence and decisions. No extension is implied.
6. **Separate task states.** 16.1 stays engineering-done; 16.2 is not invoked because the original EventStore maintainer is available. 16.3 stays pending while its received approvals lack prerequisite validity. G-3 and Epic 16 stay open. A migration approval grants no publication authority and closes no other release gate.

This amendment resolves how to record the already received C decisions and how to report the C/R conflict without treating documentation lineage as compatibility. It deliberately leaves current-main readiness blocked until the existing safety requirements are actually met. It supplies no semantic exception, no package-mode runtime evidence, no replay/bundle/schema, and no new evidence workflow.

## Evidence and limits retained

Use the [accepted 16.1 record](../implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md#tuple-record--2026-11-15-1) as stored at R, primary Quality43755163930 attempt1, provider/build job132937646575 and Epic9 job132937646633. Retain the browser configured retry, source-versus-package distinction, 35-context/27-project D4 scope, standalone Parties4.2.0/inherited5.1.0 divergence, excluded invalid CounterFixture standalone Release, nonconsumer framework/launch/restored-assets limits and BH2/BH3/QC-BH1/QC-VG1 deferrals. This proposal creates no missing runtime proof.

## Required planning decision

| Confirmed role/person | Decision required on the proposed text | State |
|---|---|---|
| Product Owner — jpiquot | Approve or reject this bounded planning amendment | Pending |
| Architect — jpiquot | Approve or reject this bounded planning amendment | Pending |
| Release Owner — jpiquot | Approve or reject this bounded planning amendment and own the prospective coordination action | Pending |

Approval adopts only this fail-closed planning resolution. Rejection leaves the existing rules and received C decisions unchanged, with G-3 open. Neither choice renews the outer window or converts the earlier request into an amendment approval.

For execution of prospective coordination, supply the actual workspace automation owner and active-session roster/acknowledgments, or record that coordination is unavailable. No assumption that jpiquot holds the additional automation-owner capacity is made from the five confirmed roles.

## Application after explicit approval

Append this dated resolution to active PRD G-3/D-12, architecture's EventStore Active-Identity Invariant, Epic16 and the existing qualification proposal; update the cached epic context and owning Story16.3 with the decision link. Preserve historical text/tuple sections. Reconcile the deferred ledger and sprint tracking with operative receipts versus received decisions. Do not edit release configuration, source, gitlinks or submodules.

Until the named planning decision exists, this file remains proposed and the active requirements are unchanged.

## Planning approval receipt — 2026-11-15

**Signer:** Jérôme Piquot (`jpiquot`/Administrator). **UTC decision date:** 2026-11-15. **Received at:** `2026-11-15T21:58:49Z`; no separate authoring timestamp was supplied. **Actual answer:** “1 approve, 2 jpiquot and confirm.” The [authorized Story 16.3 adoption](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#authorized-adoption-handoff--2026-11-15) explicitly records this answer as approval of the concrete amendment above in each planning capacity. Authority comes from the user's separately confirmed current Product Owner, Architect and Release Owner capacities recorded at `2026-11-15T21:49:02Z`, supplemented by this explicit decision; no capacity is inferred from another.

| Signer and separately confirmed capacity | Actual planning decision | Scope and rationale presented |
|---|---|---|
| jpiquot — Product Owner | Approve | Adopt all six numbered amendment provisions, preserving received decisions, exact-candidate evidence and open readiness gates. |
| jpiquot — Architect | Approve | Adopt the same text, retaining candidate/record identity distinctions and exact-current-main release guards. |
| jpiquot — Release Owner | Approve | Adopt the same text and own prospective coordination and the terminal disposition by the original deadline. |

The complete proposed wording and pending-decision preparation above remain historical text. This receipt adopts that exact wording as of receipt; it supersedes the earlier pending-amendment state prospectively. The rationale/evidence presented was the documented C/R lineage conflict and unmet coordination, with the existing accepted capture and disclosed limitations. No additional waiver or rationale is inferred from “approve.” This planning decision grants no migration or publication authority and schedules no fresh attempt.

### Prospective owner coordination attestation

The user's “2 jpiquot and confirm” explicitly identifies **jpiquot as workspace automation owner**, an additional capacity to the five already confirmed roles, and confirms actual session/automation coordination. Record this as the accountable owner's attestation, effective **`2026-11-15T21:58:49Z`**, ending no later than **`2026-11-16T17:27:10Z`**, or earlier on decision, drift or urgent fix. No earlier effective timestamp, individual active-session roster or independently observed acknowledgments were supplied. The attestation resolves the requested prospective coordination answer; it does not establish a historical repository-wide hold, change the original window, or show that C was qualified under that later coordination. This adoption contacts no one and suspends no automation.

### Operative state and bounded next action

The approved text preserves the exact accepted C tuple: FrontComposer `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`, EventStore `fdb59895975e86a1e234325a01237f259cdbd53b`, Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee`, catalog default `5.1.0`. Its [immutable 16.1 capture](../implementation-artifacts/spec-16-1-capture-the-current-eventstore-runtime-tuple.md#tuple-record--2026-11-15-1) and [primary Quality 43755163930 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1) remain evidence only for C. The three [migration decisions received](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#migration-decisions-received--2026-11-15) remain nonoperative for G-3; amendment approval and later coordination never activate, backdate or transfer them.

The handoff's immediately preceding live observation identified current main R `695d23149c56a6104e81d23a54116b4fb914df95` and successful independent [Quality 43759663406 attempt 1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1), with no exact R capture or R owner decisions. This is the supplied handoff observation, not a new remote check or readiness assertion. Record/receipt descendants are provenance only and gain no documentation exemption. G-3 requires the proposed current release candidate's own exact four-part capture, passing required same-attempt live lanes and overall Quality, and three valid named migration receipts within an authorized window. Production continues to require exact current main, successful push CI and Quality for it, and a current-main recheck before protected credentials.

**Accountable next action:** jpiquot as Release Owner, with Architect involvement, records either satisfied current-candidate readiness or the concrete remaining blocker and next owner action by unchanged outer deadline `2026-11-16T17:27:10Z`. R or any different candidate needs its own independently captured evidence and valid decisions within an authorized window. This adoption authorizes no recapture, workflow dispatch, fresh attempt or publication. If a valid attempt cannot finish by the deadline, end it with G-3 open; any fresh attempt needs a separately explicit named schedule of at most 24 hours and its own evidence and decisions. Further documentation or a green run starts no automatic retry loop.

Story 16.1 remains done; original EventStore authority is available, so 16.2 is not invoked and remains backlog with that annotation; 16.3 remains in-progress on operative current-candidate receipts. **Epic 16 and G-3 remain open.** All earlier tuple records, D1 acceptance, source/property/package/runtime/browser limits and unrelated residuals remain unchanged.

## Review clarifications — 2026-11-15

These clarifications preserve the approved six provisions, receipt and preceding wording as history; they narrow earlier adoption summaries without adding a decision or changing a rule.

1. **Coordination remains partly undocumented.** Accept jpiquot's explicit automation-owner identity and actual coordination attestation from `2026-11-15T21:58:49Z`. The identity and planning-decision questions are resolved, and the coordination answer is received. Provision 4 still requires identification of all active sessions and their coordination roster/acknowledgments; those details were not supplied and remain an explicit unsatisfied prerequisite. The attestation alone does not document them or establish a historical global hold. No session identities or acknowledgments are invented; G-3 remains open.
2. **The structural lineage blocker remains.** Committing a new capture or migration receipt advances current main to a distinct record descendant, which then differs from the candidate whose evidence/decisions it records. Missing R capture/receipts are therefore not the whole blocker: another capture alone does not resolve this cycle. jpiquot as Release Owner, with Architect involvement, must record a bounded resolution consistent with unchanged exact-current-main, candidate/evidence/receipt identity, successful push CI/Quality and protected-credential recheck gates, or a named terminal blocker disposition and next action by `2026-11-16T17:27:10Z`. No documentation exemption, transferred evidence, automatic retry, recapture or fresh attempt is authorized.
3. **R's capture cadence is separate from the outer deadline.** The parent's separately dated [review-time metadata observation](../implementation-artifacts/spec-16-3-approve-or-reject-the-exact-runtime-migration.md#review-time-cadence-observation--2026-11-15) at `2026-11-15T22:13:43Z` reports R's last required success at `2026-11-15T20:57:29Z`, so its two-hour capture deadline is **`2026-11-15T22:57:29Z`**. Only that time condition was unexpired at observation. This supplies no capture, owner receipt, session roster/acknowledgment or structural-lineage resolution; the outer deadline `2026-11-16T17:27:10Z` cannot extend it. No C/R capture or receipt transfers, extension, recapture or new attempt follows from this observation.
4. **Hold termination means final disposition.** Preserve the owner-attested start `2026-11-15T21:58:49Z`. “Decision” in earlier prospective-hold summaries means the final migration disposition or a named terminal blocker disposition, alongside earlier termination on drift or urgent fix and the original maximum end `2026-11-16T17:27:10Z`. The planning approval at hold start is not itself that final disposition. This clarifies the summary without asserting that all active-session coordination details are documented.
5. **Observation source and dates are distinct.** The supplied pre-implementation observation is the parent's read-only GitHub main-branch and [C Quality43755163930 attempt1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43755163930/attempts/1)/[R Quality43759663406 attempt1](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1) workflow-run queries completed by **`2026-11-15T21:59:14Z`**, as recorded in the linked spec observation. The later **`2026-11-15T22:13:43Z`** read separately checked main R and [R's attempt1 jobs](https://api.github.com/repos/Hexalith/Hexalith.FrontComposer/actions/runs/43759663406/attempts/1/jobs?per_page=100). Neither is a fresh implementation-agent remote check or a new tuple record. Both preserve R `695d23149c56a6104e81d23a54116b4fb914df95` and C's distinct identity.

**16.1 done; 16.2 not invoked/backlog; 16.3 in-progress; Epic 16 / G-3 open.** No operative approval or publication is claimed.
