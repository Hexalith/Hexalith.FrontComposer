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

The technical record and three named migration decisions remain separate. This proposal changes no policy, gitlink, workflow, acceptance criterion, or story status. The Story 16.1 file currently says `done`; that status establishes no migration approval or historical-scope waiver.

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
