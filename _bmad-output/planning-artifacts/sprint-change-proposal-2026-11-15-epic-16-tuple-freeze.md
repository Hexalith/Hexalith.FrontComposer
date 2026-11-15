---
title: 'Epic 16: Bound tuple capture and approval under continuous drift'
created: '2026-11-15'
status: 'approved'
mode: 'batch'
change_scope: 'moderate'
approval: 'requesting user, 2026-11-15'
g_3: 'open'
---

# Sprint Change Proposal — Epic 16 tuple capture

## Issue and evidence

Story 16.1 review deferrals D1–D4 expose a coordination gap: workspace commits move gitlinks before an exact tuple can receive evidence and approval. Platform seal transitions make Quality red. Runs [43702436144](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43702436144), [43710270227](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43710270227), [43728107585](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43728107585), and [43736184654](https://github.com/Hexalith/Hexalith.FrontComposer/actions/runs/43736184654) fail `build-and-test` step 14 with `consumer source projection owner pin changed`, skipping Gate 2c. A green AppHost lane alone does not qualify.

The technical record and three named migration decisions remain separate. This proposal changes no policy, gitlink, workflow, acceptance criterion, or story status. At proposal drafting, the Story 16.1 file said `done`; that historical status established no migration approval or historical-scope waiver. Review now sets it to `in-review`.

## Impact

| Artifact | Proposed impact |
|---|---|
| PRD §0.1 G-3 and D-12 | Add capture deadline and Release Owner escalation; preserve FR-29.7 and exact-tuple approval. |
| Epic 16 / Story 16.1 / epic context | Add bounded scheduling, record/commit boundaries, per-project version evaluation, and per-lane identities. Preserve old records byte-for-byte. |
| Story 16.2 / OI-18 | Invoke only when EventStore approval authority must transfer; retain the existing Product/Architecture/Release decision requirement. |
| Story 16.3 / Epic 17 | Bind receipts to the tested commit; leave G-3 and publication blocked on mismatch or missing authority. |
| Architecture: EventStore Active-Identity Invariant | Explicitly distinguish tested candidate from later documentation commits. |
| UX, runtime, APIs, schemas | No change; no MVP reduction. |

## Recommended approach

Choose direct adjustment: one qualification window of **at most 24 hours**, coordinated with all active sessions and the workspace automation owner. Estimated effort is low for coordination and moderate across owners; CI and approver availability determine elapsed time. Rollback contradicts the selected-gitlink constraint; a periodic capture without a reserved decision window can still chase a moving main.

1. The Release Owner identifies the accountable EventStore maintainer, FrontComposer maintainer, and Release Owner and records the start/deadline in the existing story. Arrange any necessary Story 16.2 transfer first; never infer role equivalence.
2. Begin the window before the seal push. Hold unrelated FrontComposer main commits and all gitlink/catalog/policy changes; coordinate automation suspension without changing its configuration here. Only the planned seal, separate non-seal activation, and necessary record/receipt documentation proceed. An urgent fix ends this attempt.
3. The separate dependency-governance build owns D2: reseal to committed selectors, run the requested exact-object proofs, and push seal and activation separately. The seal's expected red runs are not evidence. Candidate C is the later non-seal commit tested under the landed policy.
4. Once C has the required same-attempt success results, append the capture within **two hours**. Schedule the three named decisions within the same 24-hour window. Each decision cites C, the exact source/catalog/package identities and the Quality run. Preserve `Migration approval: not claimed · Sign-off: none · G-3: open` in the immutable capture; decisions occupy separate append-only sections.
5. Drift, missing authority, failed qualification, or timeout goes to the **Release Owner**, with **Architect** escalation. Within **four working hours**, record one bounded disposition: schedule one fresh attempt, reject with corrective work, or request an explicit planning amendment. End the hold at decision or deadline; never extend it silently. G-3 remains open without valid evidence and decisions.

### Candidate and record lineage

A record commit R advances FrontComposer HEAD even when only Markdown changes. C remains the tested tuple; R is a distinct tuple until independently evidenced. The existing 16.1 boundary explicitly says the record commit is not the tuple. This window stabilizes dependencies but does not resolve a release selector requiring the latest-main descendant R. Release Owner and Architect must explicitly resolve that requirement before asserting readiness. No documentation-only exemption, semantic-compatibility exception, receipt transfer, or release-selector change is authorized here. The named escalation prevents an endless unowned loop; it does not claim G-3 is closed.

## Specific edit proposals

**PRD G-3/D-12 and Epic 16:** current text requires recapture after drift without timing/ownership. Append the 24-hour window, two-hour capture cadence, and four-working-hour Release Owner/Architect escalation above. Keep exact identity, evidence and named approval requirements unchanged.

**Architecture and epic context:** current text compares the record with the release candidate. Add: “Approval covers only the exact tested commit C. Record/receipt descendants are distinct commits. A latest-main mismatch requires evidence or an explicitly approved planning resolution; no prior receipt transfers automatically.”

**Story 16.1, D4:** current capture names catalog default/root preset. For the new record, evaluate `HexalithEventStoreVersion` per consuming project using `dotnet msbuild -getProperty:HexalithEventStoreVersion`, with configuration, relevant target framework and source/package mode. Record each lane's exercised EventStore identity and any catalog divergence. Builds `73e60af8a1fcdf3f06726d5d1b6e1199cce778ee` selects default `5.1.0` and opt-in SDK 4.2 views `4.2.0`; these are investigation inputs, not evaluation results. Source execution does not prove package-mode runtime compatibility.

**Story 16.1, D1:** AC 3 requires only `_bmad-output/` changes, but pushed `dd575b67c82c377bd17c34fb3d14366277d2ac62` moved Platform from `8ff6db8feb5b3165bdaaf90899778199823bc59e` to `9e01ffacc45cb6a5ee12853d44f4b7f6e1f4084c`; `fbb9c3822dfa407e72739238cd472567f45f1e82` also landed the record with gitlink moves. Propose explicit FrontComposer-maintainer/Architect acceptance of this bounded historical exception while preserving the strict attribution failure and pushed history. All new capture commits must satisfy the declared documentation scope. Alternatively decline the exception and retain/reopen acceptance pending separately authorized guard work. No D1 decision or validator bypass is approved here.

## Handoff and approval

Scope: **moderate**, requiring FrontComposer/dependency-governance implementation, Release Owner scheduling/escalation, Architect planning resolution, EventStore identity review, and Product approval of any requirement amendment. Success requires the later non-seal commit's step 14, all Gate 2c steps, and every AppHost acceptance step successful; a new immutable capture with per-project/per-lane identities; and exact-tuple decisions or a bounded blocking disposition.

Checklist: [x] 1.1–1.3 trigger/evidence; [x] 2.1–2.5 epic dependencies/order; [x] 3.1–3.2 and 3.4 artifact impacts; [N/A] 3.3 UX; [x] 4.1–4.4 alternatives; [x] 5.1–5.5 proposal/handoff; [x] 6.1–6.2 consistency; [x] 6.3 explicit approval; [x] 6.4 approved planning/status edits; [!] 6.5 accountable people, window timing, D1 disposition and lineage resolution.

The complete batch proposal was presented for approval under `bmad-correct-course` Step 5 (“Get explicit user approval for complete proposal”). The requesting user's approval is recorded below; no migration or publication authorization is claimed.

## Approval receipt — 2026-11-15

The requesting user approved this proposal and the repair spec together: “approve both and continue.” Implement the recommended bounded window, cadence, escalation and historical D1 exception. This approves the proposed engineering/planning changes; it supplies no EventStore migration decision, transfer of authority, or publication authorization. Named migration approvers remain subject to Story 16.2/16.3. Window start/deadline and actual qualification results will be recorded in the repair spec.

## Implementation handoff — 2026-11-15

The approved scheduling and candidate-lineage rules are applied to PRD G-3/D-12, Epic 16, architecture and epic context. Story 16.1 contains the bounded D1 exception receipt; historical AC 3 still failed. D2/D4 remain evidence-gated. The qualification window starts `2026-11-15T17:27:10Z` and ends `2026-11-16T17:27:10Z`. Only this session's unrelated commits are held; no external automation was disabled or coordination message sent. Named migration authority and latest-main lineage resolution remain open to the Release Owner with Architect involvement.

## Execution outcome — 2026-11-15

The original attempt and one fresh observation were cancelled by unrelated concurrent commits `923988d855cd6131b4287e3da763f12baf60ab22` and `8ce46cfc1293925459c8b6af15ef7719cf0fc071`; both remain outside repair scope. The parent authorized bounded read-only observation of the already-running current run within the original deadline, without a new seal/push or a named owner rescheduling receipt.

The current `8ce46cfc1293925459c8b6af15ef7719cf0fc071` capture at `2026-11-15T18:44:11Z` follows all required successes in Quality 43744582233 attempt 1 (last `18:26:36Z`). Overall Quality fails later default tests: five failed, 6,680 succeeded, zero skipped. This is explicitly not an overall-green run. D2 repair evidence is complete; D4 is scoped to evaluated immediate hosted contexts and remains open for completeness, as corrected by the append-only Story 16.1 Review Addendum. The separate bounded residuals and Story 16.1 acceptance remain for parent review. G-3, named migration authority/receipts and latest-main/record-descendant lineage resolution remain open. The original outer deadline is still `2026-11-16T17:27:10Z`; no external automation was disabled and no approval is transferred to a descendant.

## Coordination and incident clarification — 2026-11-15

The approved repository-wide coordination of all active sessions and the workspace automation owner was **not achieved**. The observed session-only hold did not implement, waive or silently remove that prerequisite. Global coordination remains an open Release Owner action, with Architect involvement, before claiming a coordinated capture/approval freeze. No external automation was disabled and no named owner coordination/disposition receipt is invented.

The approved 24-hour outer deadline `2026-11-16T17:27:10Z`, two-hour capture cadence and owner roles remain unchanged. No business calendar was supplied for four working hours. The current incident therefore has conservative UTC disposition due **`2026-11-15T21:49:35Z`**, no later than four elapsed hours after first drift at `2026-11-15T17:49:35Z`. This supplies an explicit incident due time without redefining working hours or asserting a receipt. “Failed qualification” means failed successor required steps; expected refusal during the planned old-policy seal transition is not itself failed successor qualification. Drift, missing authority, timeout and lineage triggers still apply.

Story 16.1's capture meets only its individual-step evidence rule. Overall Quality remains failed, and G-3's unchanged passing-overall-Quality release condition remains unmet. D4's earlier blanket closure is withdrawn in the append-only review addendum: inherited hosted source contexts and standalone SDK 4.2 views are distinguished, the CounterFixture Release/package interpretation is invalid, and transitive/launch completeness remains open.

## Later green successor and engineering outcome — 2026-11-15

The later main successor is `e0eef14ddbbcd03823863a4f417a4b07f6f5bafc`. Primary automatic Quality43755163930 attempt1, supplemental isolated main manual Quality43755164106 and shared CI43755164227 all succeed at that exact commit. The new immutable same-day record is labeled the later green successor and uses only primary run metadata/counts/artifacts. The prior failed/cancelled records and D4 Review Addendum remain unchanged.

The source-fix outcomes from984b6be79a09e5e5796f75c9c0eb35ab4ccc2707/a6ed4b94b74aacf833c76ba1391a51e383bd6478 are verified by the primary default lane's6685 passes and Passed outcomes for all five formerly failed invocations. Protected manual concurrency in e0 preserves the automatic group, main-only acquisition, event-base policy and execution pins/gates. Primary/supplemental success demonstrates current hosted behavior; durable regression-harness integration remains separate future work.

Parent engineering acceptance of Story16.1 capture and the requesting-user-approved D1 historical exception is recorded outside immutable records; Story16.1 and the repair spec are done for their engineering scope. D4 has current scoped35-source-consumer/27-project evidence, seven standalone4.2 controls versus inherited5.1, and valid own AppHost/UI Release controls; CounterFixture Release remains excluded. Nonconsumer framework negotiations, launch-time contexts, restored package assets and package-mode runtime coverage stay explicit limits. The primary browser's configured retry is disclosed rather than replaced with another run's outcome.

Repository-wide coordination remains unachieved/open; no named Release Owner/Architect scheduling receipt or approval transfer is fabricated. Original incident/outer deadlines are unchanged. Migration authority, role-transfer decisions, named migration receipts and candidate/record-descendant lineage remain separate requirements; G-3 remains open. The green-workflow condition is now evidenced only for the exact tested tuple, without weakening the release gate.

## Adopted approval-lineage amendment and coordination — 2026-11-15

The [approved amendment and actual receipt](sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#planning-approval-receipt--2026-11-15) record “1 approve, 2 jpiquot and confirm” at `2026-11-15T21:58:49Z`. jpiquot approves separately as Product Owner, Architect and Release Owner, explicitly holds the additional workspace automation-owner capacity, and attests actual prospective session/automation coordination. Effective start is the receipt time, with maximum end **`2026-11-16T17:27:10Z`**, or earlier on decision, drift or urgent fix. No earlier effective time or individual roster was supplied; no independently observed acknowledgment or historical repository-wide hold is claimed. This supersedes the earlier open coordination assessment prospectively only and preserves the earlier failure to coordinate C's qualification.

The incident amendment-request disposition was already recorded timely at `2026-11-15T21:49:02Z`, before `2026-11-15T21:49:35Z`. Adoption supplies the planning answer without renewing that incident allowance or the original outer window. The prior two-hour capture rule, bounded scheduling and escalation roles remain. No fresh qualification attempt or publication is authorized.

C's received migration approvals remain nonoperative and do not activate or transfer to current main R or any descendant. G-3 requires a proposed current candidate's independent exact four-part capture, required same-attempt live-lane success, overall green Quality and three valid named receipts within an authorized window. Record/receipt commits provide provenance with no documentation exemption. Production retains exact-current-main, successful push CI/Quality and the current-main recheck before protected credentials. Existing tuple, D1, source/package, property/runtime, browser-retry and deferred-work limits remain untouched.

jpiquot as Release Owner, with Architect involvement, records readiness or the concrete remaining current-candidate blocker and next owner action by `2026-11-16T17:27:10Z`. If the attempt cannot finish then, end it with G-3 open; no documentation commit or independent green run starts an automatic recapture loop. Any fresh attempt needs its own explicit named schedule of at most 24 hours and qualifying evidence/decisions. Story 16.1 stays done, 16.2 not invoked/backlog, 16.3 in-progress, and **Epic 16 / G-3 open**.

### Review clarification of coordination, lineage and cadence — 2026-11-15

The [review clarifications](sprint-change-proposal-2026-11-15-epic-16-approval-lineage.md#review-clarifications--2026-11-15) preserve the approved rule and earlier records. Accept jpiquot's resolved automation-owner identity and coordination attestation from `2026-11-15T21:58:49Z`; provision 4's active-session roster/acknowledgments remain undocumented and required. Hold-ending “decision” means final migration disposition or named terminal blocker disposition, with drift, urgent fix or original deadline `2026-11-16T17:27:10Z` also ending it; the planning approval at hold start does not end it.

The structural record-descendant/current-main cycle remains blocked: committing another capture/receipt advances main, so another capture alone is insufficient. jpiquot as Release Owner with Architect must bound its resolution under unchanged exact-current-main/evidence/receipt and release guards, or record a terminal blocker and next action by that outer deadline. The supplied parent GitHub main and C/R run queries completed by `2026-11-15T21:59:14Z`; separate parent review metadata at `2026-11-15T22:13:43Z` reports R's last required success `2026-11-15T20:57:29Z` and capture deadline **`2026-11-15T22:57:29Z`**. This establishes time-only eligibility at that observation, without capture/receipt transfer, deadline extension, recapture, retry or new-attempt authorization. No new remote check is made. **16.3 remains in-progress; G-3 remains open**, with 16.1 done and 16.2 not invoked/backlog.
